# Real-Time Protection Implementation Guide

## Overview

This document provides detailed implementation instructions for the real-time protection component of the ZariVirusKiller antivirus solution. Real-time protection is a critical feature that monitors file system activities and processes to prevent malware execution before it can cause damage.

## Architecture

The real-time protection system consists of several components:

1. **File System Monitor** - Watches for file creation, modification, and access events
2. **Process Monitor** - Tracks process creation and termination
3. **Scan Dispatcher** - Coordinates scanning requests to minimize performance impact
4. **Windows Service** - Ensures protection runs at system startup

## Implementation Details

### 1. File System Monitor

#### Implementation Steps

1. Create a `RealTimeProtection` class that uses `FileSystemWatcher`:

```csharp
using System.IO;
using System.Security.Permissions;

public class FileSystemMonitor
{
    private readonly ScanEngine _scanEngine;
    private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
    private readonly ConcurrentQueue<string> _scanQueue = new ConcurrentQueue<string>();
    private readonly HashSet<string> _exclusions = new HashSet<string>();
    private readonly Thread _processingThread;
    private bool _isRunning;
    
    public FileSystemMonitor(ScanEngine scanEngine)
    {
        _scanEngine = scanEngine;
        _processingThread = new Thread(ProcessScanQueue);
    }
    
    public void Start(IEnumerable<string> pathsToMonitor, IEnumerable<string> exclusions = null)
    {
        if (_isRunning)
            return;
            
        // Add exclusions
        if (exclusions != null)
        {
            foreach (var exclusion in exclusions)
            {
                _exclusions.Add(Path.GetFullPath(exclusion).ToLowerInvariant());
            }
        }
        
        // Create watchers for each path
        foreach (var path in pathsToMonitor)
        {
            if (!Directory.Exists(path))
                continue;
                
            var watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                Filter = "*.*",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            
            watcher.Created += OnFileChanged;
            watcher.Changed += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            
            _watchers.Add(watcher);
        }
        
        _isRunning = true;
        _processingThread.Start();
    }
    
    public void Stop()
    {
        if (!_isRunning)
            return;
            
        _isRunning = false;
        
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        
        _watchers.Clear();
        _processingThread.Join(1000);
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (ShouldScanFile(e.FullPath))
        {
            _scanQueue.Enqueue(e.FullPath);
        }
    }
    
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (ShouldScanFile(e.FullPath))
        {
            _scanQueue.Enqueue(e.FullPath);
        }
    }
    
    private bool ShouldScanFile(string filePath)
    {
        // Skip directories
        if (Directory.Exists(filePath))
            return false;
            
        // Check file extension (optional)
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            return false;
            
        // Check exclusions
        string normalizedPath = Path.GetFullPath(filePath).ToLowerInvariant();
        foreach (var exclusion in _exclusions)
        {
            if (normalizedPath.StartsWith(exclusion))
                return false;
        }
        
        return true;
    }
    
    private void ProcessScanQueue()
    {
        while (_isRunning)
        {
            if (_scanQueue.TryDequeue(out string filePath))
            {
                try
                {
                    // Add a small delay to avoid scanning files that are still being written
                    Thread.Sleep(100);
                    
                    // Check if file still exists
                    if (!File.Exists(filePath))
                        continue;
                        
                    // Scan the file
                    bool isThreat = _scanEngine.ScanFileAsync(filePath).GetAwaiter().GetResult();
                    
                    if (isThreat)
                    {
                        // Handle threat (quarantine, delete, notify)
                        OnThreatDetected(filePath);
                    }
                }
                catch (IOException)
                {
                    // File might be locked, retry later
                    _scanQueue.Enqueue(filePath);
                    Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    // Log error
                    Console.WriteLine($"Error scanning file {filePath}: {ex.Message}");
                }
            }
            else
            {
                // No files to scan, sleep for a bit
                Thread.Sleep(100);
            }
        }
    }
    
    private void OnThreatDetected(string filePath)
    {
        // Implement threat handling logic
        // Options: quarantine, delete, block access, notify user
    }
}
```

### 2. Process Monitor

Implement process monitoring using Windows Management Instrumentation (WMI):

```csharp
using System.Management;

public class ProcessMonitor
{
    private readonly ScanEngine _scanEngine;
    private ManagementEventWatcher _processStartWatcher;
    private bool _isRunning;
    
    public ProcessMonitor(ScanEngine scanEngine)
    {
        _scanEngine = scanEngine;
    }
    
    public void Start()
    {
        if (_isRunning)
            return;
            
        // Set up WMI query for process creation
        WqlEventQuery query = new WqlEventQuery("__InstanceCreationEvent", 
            TimeSpan.FromSeconds(1), 
            "TargetInstance ISA 'Win32_Process'");
            
        _processStartWatcher = new ManagementEventWatcher(query);
        _processStartWatcher.EventArrived += OnProcessStarted;
        _processStartWatcher.Start();
        
        _isRunning = true;
    }
    
    public void Stop()
    {
        if (!_isRunning)
            return;
            
        _processStartWatcher.Stop();
        _processStartWatcher.Dispose();
        _isRunning = false;
    }
    
    private void OnProcessStarted(object sender, EventArrivedEventArgs e)
    {
        ManagementBaseObject targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
        string processName = targetInstance["Name"].ToString();
        string executablePath = targetInstance["ExecutablePath"]?.ToString();
        
        if (string.IsNullOrEmpty(executablePath))
            return;
            
        // Scan the executable
        Task.Run(async () =>
        {
            try
            {
                bool isThreat = await _scanEngine.ScanFileAsync(executablePath);
                
                if (isThreat)
                {
                    // Get process ID
                    int processId = Convert.ToInt32(targetInstance["ProcessId"]);
                    
                    // Terminate the process
                    try
                    {
                        Process process = Process.GetProcessById(processId);
                        process.Kill();
                        
                        // Notify user
                        OnMaliciousProcessDetected(processName, executablePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error terminating process {processId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning process {processName}: {ex.Message}");
            }
        });
    }
    
    private void OnMaliciousProcessDetected(string processName, string executablePath)
    {
        // Implement notification logic
    }
}
```

### 3. Windows Service Integration

Create a Windows service to ensure real-time protection starts automatically:

```csharp
using System.ServiceProcess;

public class ZariProtectionService : ServiceBase
{
    private FileSystemMonitor _fileSystemMonitor;
    private ProcessMonitor _processMonitor;
    private ScanEngine _scanEngine;
    
    public ZariProtectionService()
    {
        ServiceName = "ZariVirusKillerProtection";
        CanStop = true;
        CanPauseAndContinue = false;
        AutoLog = true;
    }
    
    protected override void OnStart(string[] args)
    {
        // Initialize scan engine
        _scanEngine = new ScanEngine("https://api.zarivirus.com");
        _scanEngine.InitializeAsync().Wait();
        
        // Start file system monitoring
        _fileSystemMonitor = new FileSystemMonitor(_scanEngine);
        _fileSystemMonitor.Start(new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Path.GetPathRoot(Environment.SystemDirectory) // System drive
        }, 
        new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZariVirusKiller"),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows)
        });
        
        // Start process monitoring
        _processMonitor = new ProcessMonitor(_scanEngine);
        _processMonitor.Start();
    }
    
    protected override void OnStop()
    {
        _fileSystemMonitor?.Stop();
        _processMonitor?.Stop();
    }
    
    public static void Main()
    {
        ServiceBase.Run(new ZariProtectionService());
    }
}
```

### 4. Service Installation

Create an installer for the Windows service:

```csharp
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

[RunInstaller(true)]
public class ZariServiceInstaller : Installer
{
    public ZariServiceInstaller()
    {
        var serviceProcessInstaller = new ServiceProcessInstaller
        {
            Account = ServiceAccount.LocalSystem
        };
        
        var serviceInstaller = new ServiceInstaller
        {
            StartType = ServiceStartMode.Automatic,
            ServiceName = "ZariVirusKillerProtection",
            DisplayName = "ZariVirusKiller Protection Service",
            Description = "Provides real-time protection against malware and viruses."
        };
        
        Installers.Add(serviceProcessInstaller);
        Installers.Add(serviceInstaller);
    }
}
```

## Integration with Main Application

### 1. Service Control from UI

Add controls to the main application to manage the protection service:

```csharp
private void ToggleRealTimeProtection(bool enable)
{
    try
    {
        using (var serviceController = new ServiceController("ZariVirusKillerProtection"))
        {
            if (enable)
            {
                if (serviceController.Status != ServiceControllerStatus.Running)
                {
                    serviceController.Start();
                    serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }
            }
            else
            {
                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }
            }
            
            UpdateProtectionStatus();
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error controlling protection service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

private void UpdateProtectionStatus()
{
    try
    {
        using (var serviceController = new ServiceController("ZariVirusKillerProtection"))
        {
            bool isRunning = serviceController.Status == ServiceControllerStatus.Running;
            realTimeProtectionEnabled = isRunning;
            
            // Update UI
            toggleRealTimeProtectionButton.Text = isRunning ? "Disable Protection" : "Enable Protection";
            statusLabel.Text = isRunning ? "Protected" : "Not Protected";
            statusLabel.ForeColor = isRunning ? Color.Green : Color.Red;
        }
    }
    catch
    {
        // Service might not be installed
        realTimeProtectionEnabled = false;
        toggleRealTimeProtectionButton.Text = "Enable Protection";
        statusLabel.Text = "Not Protected";
        statusLabel.ForeColor = Color.Red;
    }
}
```

### 2. Notification System

Implement a notification system to alert users of detected threats:

```csharp
public class NotificationManager
{
    private readonly NotifyIcon _trayIcon;
    
    public NotificationManager(NotifyIcon trayIcon)
    {
        _trayIcon = trayIcon;
    }
    
    public void ShowThreatNotification(string filePath, string threatName)
    {
        _trayIcon.BalloonTipTitle = "Threat Detected!";
        _trayIcon.BalloonTipText = $"ZariVirusKiller detected {threatName} in {Path.GetFileName(filePath)}";
        _trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
        _trayIcon.ShowBalloonTip(5000);
        
        // Log the threat
        LogThreat(filePath, threatName);
    }
    
    private void LogThreat(string filePath, string threatName)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string logPath = Path.Combine(appData, "ZariVirusKiller", "Logs");
        
        Directory.CreateDirectory(logPath);
        
        string logFile = Path.Combine(logPath, "threats.log");
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {threatName} - {filePath}\r\n";
        
        File.AppendAllText(logFile, logEntry);
    }
}
```

## Performance Considerations

1. **Throttling** - Implement scan throttling to prevent high CPU usage:

```csharp
private readonly SemaphoreSlim _scanThrottle = new SemaphoreSlim(2); // Max 2 concurrent scans

private async Task ScanWithThrottlingAsync(string filePath)
{
    await _scanThrottle.WaitAsync();
    try
    {
        await _scanEngine.ScanFileAsync(filePath);
    }
    finally
    {
        _scanThrottle.Release();
    }
}
```

2. **Exclusions** - Allow users to configure exclusions for trusted applications and locations

3. **Smart Scanning** - Only scan relevant file types (executables, scripts, documents with macros)

## Security Considerations

1. **Service Hardening** - Protect the service from being terminated by malware:

```csharp
// In service initialization
using (var dacl = new DiscretionaryAcl(false, false, 1))
{
    // Only allow administrators to control the service
    dacl.AddAccess(AccessControlType.Allow, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
        ServiceAccessRights.AllAccess, InheritanceFlags.None, PropagationFlags.None);
    
    // Deny everyone else
    dacl.AddAccess(AccessControlType.Deny, new SecurityIdentifier(WellKnownSidType.WorldSid, null),
        ServiceAccessRights.Delete | ServiceAccessRights.WriteDac | ServiceAccessRights.WriteOwner,
        InheritanceFlags.None, PropagationFlags.None);
    
    // Apply the DACL
    var sd = new RawSecurityDescriptor(ControlFlags.DiscretionaryAclPresent, null, null, dacl, null);
    byte[] rawSd = new byte[sd.BinaryLength];
    sd.GetBinaryForm(rawSd, 0);
    
    // Set the security descriptor
    NativeMethods.SetServiceObjectSecurity(ServiceHandle, SecurityInfos.DiscretionaryAcl, rawSd);
}
```

2. **Self-Protection** - Monitor for attempts to disable the protection:

```csharp
private void MonitorSelfProtection()
{
    // Create a watcher for the service
    var query = new WqlEventQuery("__InstanceModificationEvent", 
        TimeSpan.FromSeconds(1), 
        "TargetInstance ISA 'Win32_Service' AND TargetInstance.Name = 'ZariVirusKillerProtection'");
        
    var watcher = new ManagementEventWatcher(query);
    watcher.EventArrived += (sender, e) =>
    {
        var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
        string state = targetInstance["State"].ToString();
        
        if (state != "Running")
        {
            // Service was stopped, attempt to restart it
            try
            {
                using (var serviceController = new ServiceController("ZariVirusKillerProtection"))
                {
                    if (serviceController.Status != ServiceControllerStatus.Running)
                    {
                        serviceController.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error restarting service: {ex.Message}");
            }
        }
    };
    
    watcher.Start();
}
```

## Testing Strategy

1. **Functional Testing**
   - Test file creation, modification, and deletion events
   - Test process creation monitoring
   - Verify threat detection and response

2. **Performance Testing**
   - Measure CPU and memory usage during normal operation
   - Test with high file I/O scenarios
   - Verify impact on system boot time

3. **Security Testing**
   - Attempt to disable or bypass protection
   - Test with EICAR test files
   - Verify protection against real malware samples in a controlled environment

## Next Steps

After implementing the real-time protection component:

1. Integrate with the main application UI
2. Implement user preferences for protection settings
3. Add telemetry to improve detection rates
4. Create automated tests for the protection components

This implementation will provide robust real-time protection against malware threats while maintaining good system performance.