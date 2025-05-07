using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ZariVirusKiller.Engine
{
    /// <summary>
    /// Provides functionality for installing, uninstalling, starting, and stopping the ZariVirusKiller protection service
    /// </summary>
    public class ServiceInstaller
    {
        private const string ServiceName = "ZariVirusKillerProtection";
        
        /// <summary>
        /// Installs the ZariVirusKiller protection service
        /// </summary>
        public static bool InstallService()
        {
            try
            {
                // Check if service already exists
                if (ServiceExists())
                {
                    return true;
                }
                
                // Get the path to the service executable
                string servicePath = GetServicePath();
                if (!File.Exists(servicePath))
                {
                    throw new FileNotFoundException("Service executable not found", servicePath);
                }
                
                // Install the service
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"create {ServiceName} binPath= \"{servicePath}\" start= auto DisplayName= \"ZariVirusKiller Protection Service\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                
                process.Start();
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new Exception($"Failed to install service: {error}");
                }
                
                // Set service description
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"description {ServiceName} \"Provides real-time protection against malware and viruses for ZariVirusKiller\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error installing service: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Uninstalls the ZariVirusKiller protection service
        /// </summary>
        public static bool UninstallService()
        {
            try
            {
                // Check if service exists
                if (!ServiceExists())
                {
                    return true;
                }
                
                // Stop the service if it's running
                StopService();
                
                // Uninstall the service
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"delete {ServiceName}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                
                process.Start();
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new Exception($"Failed to uninstall service: {error}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error uninstalling service: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts the ZariVirusKiller protection service
        /// </summary>
        public static bool StartService()
        {
            try
            {
                // Check if service exists
                if (!ServiceExists())
                {
                    return false;
                }
                
                // Check if service is already running
                ServiceController sc = new ServiceController(ServiceName);
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    return true;
                }
                
                // Start the service
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                
                return sc.Status == ServiceControllerStatus.Running;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting service: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stops the ZariVirusKiller protection service
        /// </summary>
        public static bool StopService()
        {
            try
            {
                // Check if service exists
                if (!ServiceExists())
                {
                    return true;
                }
                
                // Check if service is already stopped
                ServiceController sc = new ServiceController(ServiceName);
                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    return true;
                }
                
                // Stop the service
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                
                return sc.Status == ServiceControllerStatus.Stopped;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping service: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if the ZariVirusKiller protection service is installed
        /// </summary>
        public static bool ServiceExists()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == ServiceName);
        }
        
        /// <summary>
        /// Gets the status of the ZariVirusKiller protection service
        /// </summary>
        public static ServiceControllerStatus GetServiceStatus()
        {
            try
            {
                if (!ServiceExists())
                {
                    return ServiceControllerStatus.Stopped;
                }
                
                ServiceController sc = new ServiceController(ServiceName);
                return sc.Status;
            }
            catch
            {
                return ServiceControllerStatus.Stopped;
            }
        }
        
        /// <summary>
        /// Gets the path to the service executable
        /// </summary>
        private static string GetServicePath()
        {
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string directory = Path.GetDirectoryName(assemblyPath);
            return Path.Combine(directory, "ZariVirusKillerService.exe");
        }
    }
}