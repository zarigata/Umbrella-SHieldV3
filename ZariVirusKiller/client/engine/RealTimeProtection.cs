using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace ZariVirusKiller.Engine
{
    /// <summary>
    /// Provides real-time protection by monitoring file system activities and processes
    /// </summary>
    public class RealTimeProtection
    {
        private readonly ScanEngine _scanEngine;
        private readonly PatternScanner _patternScanner;
        private readonly HeuristicAnalyzer _heuristicAnalyzer;
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private readonly ConcurrentQueue<string> _scanQueue = new ConcurrentQueue<string>();
        private readonly HashSet<string> _exclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Thread _processingThread;
        private readonly ManagementEventWatcher _processWatcher;
        private bool _isRunning;
        private CancellationTokenSource _cancellationTokenSource;
        
        public event EventHandler<ThreatDetectedEventArgs> ThreatDetected;
        
        /// <summary>
        /// Initializes a new instance of the RealTimeProtection class
        /// </summary>
        public RealTimeProtection(ScanEngine scanEngine, PatternScanner patternScanner, HeuristicAnalyzer heuristicAnalyzer)
        {
            _scanEngine = scanEngine;
            _patternScanner = patternScanner;
            _heuristicAnalyzer = heuristicAnalyzer;
            _processingThread = new Thread(ProcessScanQueue);
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Initialize process watcher
            string query = "SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'";
            _processWatcher = new ManagementEventWatcher(query);
            _processWatcher.EventArrived += ProcessWatcher_EventArrived;
        }
        
        /// <summary>
        /// Starts real-time protection
        /// </summary>
        public void Start(IEnumerable<string> pathsToMonitor, IEnumerable<string> exclusions = null)
        {
            if (_isRunning)
                return;
                
            // Add exclusions
            if (exclusions != null)
            {
                foreach (string exclusion in exclusions)
                {
                    _exclusions.Add(exclusion);
                }
            }
            
            // Create file system watchers for each path
            foreach (string path in pathsToMonitor)
            {
                if (Directory.Exists(path))
                {
                    var watcher = new FileSystemWatcher(path)
                    {
                        IncludeSubdirectories = true,
                        EnableRaisingEvents = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
                    };
                    
                    watcher.Created += Watcher_FileEvent;
                    watcher.Changed += Watcher_FileEvent;
                    watcher.Renamed += Watcher_Renamed;
                    
                    _watchers.Add(watcher);
                }
            }
            
            // Start processing thread
            _isRunning = true;
            _processingThread.Start();
            
            // Start process monitoring
            _processWatcher.Start();
        }
        
        /// <summary>
        /// Stops real-time protection
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;
                
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            
            // Stop file system watchers
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            
            _watchers.Clear();
            
            // Stop process watcher
            _processWatcher.Stop();
            
            // Wait for processing thread to finish
            if (_processingThread.IsAlive)
            {
                _processingThread.Join(1000);
                if (_processingThread.IsAlive)
                    _processingThread.Abort();
            }
        }
        
        /// <summary>
        /// Adds a path to the exclusion list
        /// </summary>
        public void AddExclusion(string path)
        {
            _exclusions.Add(path);
        }
        
        /// <summary>
        /// Removes a path from the exclusion list
        /// </summary>
        public void RemoveExclusion(string path)
        {
            _exclusions.Remove(path);
        }
        
        /// <summary>
        /// Clears all exclusions
        /// </summary>
        public void ClearExclusions()
        {
            _exclusions.Clear();
        }
        
        /// <summary>
        /// Handles file system events
        /// </summary>
        private void Watcher_FileEvent(object sender, FileSystemEventArgs e)
        {
            if (ShouldScanFile(e.FullPath))
            {
                _scanQueue.Enqueue(e.FullPath);
            }
        }
        
        /// <summary>
        /// Handles file rename events
        /// </summary>
        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (ShouldScanFile(e.FullPath))
            {
                _scanQueue.Enqueue(e.FullPath);
            }
        }
        
        /// <summary>
        /// Handles process creation events
        /// </summary>
        private void ProcessWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                ManagementBaseObject targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                string processPath = targetInstance["ExecutablePath"]?.ToString();
                
                if (!string.IsNullOrEmpty(processPath) && ShouldScanFile(processPath))
                {
                    // Prioritize process scans by adding to front of queue
                    Task.Run(() => ScanFileAsync(processPath, true));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in process monitoring: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Processes the scan queue
        /// </summary>
        private void ProcessScanQueue()
        {
            while (_isRunning)
            {
                try
                {
                    if (_scanQueue.TryDequeue(out string filePath))
                    {
                        // Scan the file
                        Task.Run(() => ScanFileAsync(filePath));
                    }
                    else
                    {
                        // Sleep if queue is empty
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing scan queue: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Scans a file for threats
        /// </summary>
        private async Task ScanFileAsync(string filePath, bool isPriority = false)
        {
            try
            {
                // Check if file still exists
                if (!File.Exists(filePath))
                    return;
                    
                // Check if file is in use
                if (IsFileInUse(filePath))
                {
                    // If file is in use and not a priority scan, re-queue for later
                    if (!isPriority)
                    {
                        _scanQueue.Enqueue(filePath);
                        return;
                    }
                }
                
                // Perform hash-based scan
                var scanResult = await _scanEngine.ScanFileAsync(filePath);
                
                if (scanResult.IsInfected)
                {
                    OnThreatDetected(new ThreatDetectedEventArgs
                    {
                        FilePath = filePath,
                        ThreatName = scanResult.ThreatName,
                        DetectionMethod = "Hash-based"
                    });
                    return;
                }
                
                // Perform pattern-based scan
                var patternResult = await _patternScanner.ScanFileAsync(filePath);
                
                if (patternResult.IsInfected)
                {
                    OnThreatDetected(new ThreatDetectedEventArgs
                    {
                        FilePath = filePath,
                        ThreatName = patternResult.MatchedSignatures[0].SignatureName,
                        DetectionMethod = "Pattern-based"
                    });
                    return;
                }
                
                // Perform heuristic analysis
                var heuristicResult = await _heuristicAnalyzer.AnalyzeFileAsync(filePath);
                
                if (heuristicResult.RiskLevel == RiskLevel.High)
                {
                    OnThreatDetected(new ThreatDetectedEventArgs
                    {
                        FilePath = filePath,
                        ThreatName = $"Suspicious.{Path.GetExtension(filePath).TrimStart('.')}.",
                        DetectionMethod = "Heuristic",
                        RiskLevel = heuristicResult.RiskLevel
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning file {filePath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Determines if a file should be scanned based on exclusions and file type
        /// </summary>
        private bool ShouldScanFile(string filePath)
        {
            try
            {
                // Check exclusions
                foreach (string exclusion in _exclusions)
                {
                    if (filePath.StartsWith(exclusion, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                
                // Check file extension
                string extension = Path.GetExtension(filePath).ToLower();
                
                // Skip certain file types that are unlikely to be threats
                string[] skipExtensions = { ".jpg", ".png", ".gif", ".bmp", ".txt", ".log" };
                foreach (string skipExt in skipExtensions)
                {
                    if (extension == skipExt)
                        return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Checks if a file is currently in use
        /// </summary>
        private bool IsFileInUse(string filePath)
        {
            try
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return false;
                }
            }
            catch
            {
                return true;
            }
        }
        
        /// <summary>
        /// Raises the ThreatDetected event
        /// </summary>
        protected virtual void OnThreatDetected(ThreatDetectedEventArgs e)
        {
            ThreatDetected?.Invoke(this, e);
        }
    }
    
    /// <summary>
    /// Event arguments for threat detection
    /// </summary>
    public class ThreatDetectedEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public string ThreatName { get; set; }
        public string DetectionMethod { get; set; }
        public RiskLevel RiskLevel { get; set; }
    }
}