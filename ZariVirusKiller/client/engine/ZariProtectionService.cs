using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;

namespace ZariVirusKiller.Engine
{
    /// <summary>
    /// Windows service for providing real-time protection
    /// </summary>
    public partial class ZariProtectionService : ServiceBase
    {
        private ScanEngine _scanEngine;
        private PatternScanner _patternScanner;
        private HeuristicAnalyzer _heuristicAnalyzer;
        private RealTimeProtection _realTimeProtection;
        private string _serverUrl;
        private List<string> _monitoredPaths;
        private List<string> _exclusions;
        private bool _isRunning;
        private Thread _serviceThread;
        private CancellationTokenSource _cancellationTokenSource;
        
        public ZariProtectionService()
        {
            InitializeComponent();
            _monitoredPaths = new List<string>();
            _exclusions = new List<string>();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Service initialization
        /// </summary>
        private void InitializeComponent()
        {
            this.ServiceName = "ZariVirusKillerProtection";
            this.CanStop = true;
            this.CanPauseAndContinue = true;
            this.AutoLog = true;
        }
        
        /// <summary>
        /// Service startup
        /// </summary>
        protected override void OnStart(string[] args)
        {
            try
            {
                // Load configuration
                LoadConfiguration();
                
                // Initialize scan engine
                _scanEngine = new ScanEngine(_serverUrl);
                _patternScanner = new PatternScanner();
                _heuristicAnalyzer = new HeuristicAnalyzer();
                
                // Initialize real-time protection
                _realTimeProtection = new RealTimeProtection(_scanEngine, _patternScanner, _heuristicAnalyzer);
                _realTimeProtection.ThreatDetected += RealTimeProtection_ThreatDetected;
                
                // Start service thread
                _isRunning = true;
                _serviceThread = new Thread(ServiceThreadProc);
                _serviceThread.Start();
                
                // Log service start
                EventLog.WriteEntry("ZariVirusKiller", "Real-time protection service started", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ZariVirusKiller", $"Error starting service: {ex.Message}", EventLogEntryType.Error);
                throw;
            }
        }
        
        /// <summary>
        /// Service stop
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                // Stop real-time protection
                if (_realTimeProtection != null)
                {
                    _realTimeProtection.Stop();
                }
                
                // Stop service thread
                _isRunning = false;
                _cancellationTokenSource.Cancel();
                
                if (_serviceThread != null && _serviceThread.IsAlive)
                {
                    _serviceThread.Join(5000);
                    if (_serviceThread.IsAlive)
                    {
                        _serviceThread.Abort();
                    }
                }
                
                // Log service stop
                EventLog.WriteEntry("ZariVirusKiller", "Real-time protection service stopped", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ZariVirusKiller", $"Error stopping service: {ex.Message}", EventLogEntryType.Error);
            }
        }
        
        /// <summary>
        /// Service pause
        /// </summary>
        protected override void OnPause()
        {
            try
            {
                if (_realTimeProtection != null)
                {
                    _realTimeProtection.Stop();
                }
                
                EventLog.WriteEntry("ZariVirusKiller", "Real-time protection service paused", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ZariVirusKiller", $"Error pausing service: {ex.Message}", EventLogEntryType.Error);
            }
        }
        
        /// <summary>
        /// Service continue
        /// </summary>
        protected override void OnContinue()
        {
            try
            {
                if (_realTimeProtection != null)
                {
                    _realTimeProtection.Start(_monitoredPaths, _exclusions);
                }
                
                EventLog.WriteEntry("ZariVirusKiller", "Real-time protection service continued", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ZariVirusKiller", $"Error continuing service: {ex.Message}", EventLogEntryType.Error);
            }
        }
        
        /// <summary>
        /// Service thread procedure
        /// </summary>
        private void ServiceThreadProc()
        {
            try
            {
                // Initialize scan engine
                Task.Run(async () => await _scanEngine.InitializeAsync()).Wait();
                
                // Start real-time protection
                _realTimeProtection.Start(_monitoredPaths, _exclusions);
                
                // Keep service running
                while (_isRunning)
                {
                    try
                    {
                        // Check for updates periodically
                        Task.Run(async () => await _scanEngine.UpdateDefinitionsAsync()).Wait();
                        
                        // Sleep for 1 hour before checking again
                        Thread.Sleep(3600000);
                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        EventLog.WriteEntry("ZariVirusKiller", $"Error in service thread: {ex.Message}", EventLogEntryType.Error);
                        Thread.Sleep(60000); // Sleep for 1 minute before retrying
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ZariVirusKiller", $"Fatal error in service thread: {ex.Message}", EventLogEntryType.Error);
            }
        }
        
        /// <summary>
        /// Loads service configuration
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // In a real implementation, this would load from app.config or registry
                _serverUrl = "https://api.zarivirus.com";
                
                // Add default monitored paths
                _monitoredPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
                _monitoredPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                _monitoredPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                _monitoredPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                _monitoredPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments));
                _monitoredPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles));
                
                // Add default exclusions
                string windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                _exclusions.Add(Path.Combine(windowsDir, "SoftwareDistribution"));
                _exclusions.Add(Path.Combine(windowsDir, "Temp"));
                _exclusions.Add(Path.Combine(windowsDir, "WinSxS"));
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ZariVirusKiller", $"Error loading configuration: {ex.Message}", EventLogEntryType.Error);
            }
        }
        
        /// <summary>
        /// Handles threat detection events
        /// </summary>
        private void RealTimeProtection_ThreatDetected(object sender, ThreatDetectedEventArgs e)
        {
            try
            {
                // Log the threat
                EventLog.WriteEntry("ZariVirusKiller", 
                    $"Threat detected: {e.ThreatName} in {e.FilePath} (Method: {e.DetectionMethod})", 
                    EventLogEntryType.Warning);
                
                // Quarantine the file
                Task.Run(async () => await _scanEngine.QuarantineFileAsync(e.FilePath, e.ThreatName, e.DetectionMethod));
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ZariVirusKiller", $"Error handling threat: {ex.Message}", EventLogEntryType.Error);
            }
        }
        
        /// <summary>
        /// Main entry point for the service
        /// </summary>
        public static void Main()
        {
            ServiceBase.Run(new ZariProtectionService());
        }
    }
}