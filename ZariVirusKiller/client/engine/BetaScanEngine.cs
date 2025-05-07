using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZariVirusKiller.Engine
{
    public class BetaScanEngine
    {
        private readonly BetaPatternScanner _patternScanner;
        private CancellationTokenSource _scanCancellationTokenSource;
        
        public event EventHandler<ScanProgressEventArgs> ScanProgress;
        public event EventHandler<ScanCompletedEventArgs> ScanCompleted;
        
        public BetaScanEngine()
        {
            _patternScanner = new BetaPatternScanner();
            _scanCancellationTokenSource = new CancellationTokenSource();
            
            // Ensure directories exist
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string definitionsPath = Path.Combine(appData, "ZariVirusKiller", "Definitions");
            Directory.CreateDirectory(definitionsPath);
            
            // Load pattern signatures if available
            string signatureFile = Path.Combine(definitionsPath, "patterns.json");
            if (File.Exists(signatureFile))
            {
                _patternScanner.LoadSignatures(signatureFile);
            }
        }
        
        /// <summary>
        /// Scans a directory for threats
        /// </summary>
        public async Task<ScanResult> ScanDirectoryAsync(string directoryPath, bool includeSubdirectories = true)
        {
            var result = new ScanResult
            {
                ThreatCount = 0,
                ScannedFiles = 0,
                ScanDate = DateTime.Now,
                Completed = false
            };
            
            var startTime = DateTime.Now;
            _scanCancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                // Get all files to scan
                var filesToScan = new List<string>();
                var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                
                foreach (var file in Directory.GetFiles(directoryPath, "*.*", searchOption))
                {
                    filesToScan.Add(file);
                }
                
                int totalFiles = filesToScan.Count;
                int scannedFiles = 0;
                int threatsFound = 0;
                
                // Scan each file
                foreach (var file in filesToScan)
                {
                    if (_scanCancellationTokenSource.Token.IsCancellationRequested)
                        break;
                        
                    // Report progress
                    OnScanProgress(new ScanProgressEventArgs
                    {
                        CurrentFile = file,
                        ScannedFiles = scannedFiles,
                        TotalFiles = totalFiles,
                        ThreatsFound = threatsFound
                    });
                    
                    try
                    {
                        var scanResult = await _patternScanner.ScanFileAsync(file);
                        scannedFiles++;
                        
                        if (scanResult.IsInfected)
                        {
                            threatsFound += scanResult.MatchedSignatures.Count;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error scanning file {file}: {ex.Message}");
                    }
                }
                
                result.ThreatCount = threatsFound;
                result.ScannedFiles = scannedFiles;
                result.Duration = DateTime.Now - startTime;
                result.Completed = true;
                
                OnScanCompleted(new ScanCompletedEventArgs { Result = result });
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during scan: {ex.Message}");
                result.Duration = DateTime.Now - startTime;
                OnScanCompleted(new ScanCompletedEventArgs { Result = result });
                return result;
            }
        }
        
        /// <summary>
        /// Scans a single file for threats
        /// </summary>
        public async Task<PatternScanResult> ScanFileAsync(string filePath)
        {
            try
            {
                return await _patternScanner.ScanFileAsync(filePath);
            }
            catch (Exception ex)
            {
                return new PatternScanResult
                {
                    FilePath = filePath,
                    IsInfected = false,
                    Error = ex.Message,
                    MatchedSignatures = new List<SignatureMatch>()
                };
            }
        }
        
        /// <summary>
        /// Cancels an ongoing scan
        /// </summary>
        public void CancelScan()
        {
            _scanCancellationTokenSource.Cancel();
        }
        
        /// <summary>
        /// Loads virus definitions from a file
        /// </summary>
        public bool LoadDefinitions(string filePath)
        {
            return _patternScanner.LoadSignatures(filePath);
        }
        
        protected virtual void OnScanProgress(ScanProgressEventArgs e)
        {
            ScanProgress?.Invoke(this, e);
        }
        
        protected virtual void OnScanCompleted(ScanCompletedEventArgs e)
        {
            ScanCompleted?.Invoke(this, e);
        }
    }
}