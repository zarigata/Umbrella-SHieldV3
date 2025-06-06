using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;

namespace ZariVirusKiller.Engine
{
    public class ScanProgressEventArgs : EventArgs
    {
        public string CurrentFile { get; set; }
        public int ScannedFiles { get; set; }
        public int TotalFiles { get; set; }
        public int ThreatsFound { get; set; }
    }

    public class ScanCompletedEventArgs : EventArgs
    {
        public ScanResult Result { get; set; }
    }

    public class UpdateProgressEventArgs : EventArgs
    {
        public string Status { get; set; }
        public int ProgressPercentage { get; set; }
    }

    public class UpdateCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Version { get; set; }
        public int SignatureCount { get; set; }
    }
    
    public class ScanEngine
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;
        private Dictionary<string, string> _virusDefinitions;
        private bool _isInitialized;
        private CancellationTokenSource _scanCancellationTokenSource;
        private readonly PatternScanner _patternScanner;
        private readonly HeuristicAnalyzer _heuristicAnalyzer;
        private readonly QuarantineManager _quarantineManager;
        
        public event EventHandler<ScanProgressEventArgs> ScanProgress;
        public event EventHandler<ScanCompletedEventArgs> ScanCompleted;
        public event EventHandler<UpdateProgressEventArgs> UpdateProgress;
        public event EventHandler<UpdateCompletedEventArgs> UpdateCompleted;
        
        public ScanEngine(string serverUrl)
        {
            _serverUrl = serverUrl;
            _httpClient = new HttpClient();
            _virusDefinitions = new Dictionary<string, string>();
            _isInitialized = false;
            _scanCancellationTokenSource = new CancellationTokenSource();
            
            // Initialize components
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string definitionsPath = Path.Combine(appData, "ZariVirusKiller", "Definitions");
            string quarantinePath = Path.Combine(appData, "ZariVirusKiller", "Quarantine");
            
            // Ensure directories exist
            Directory.CreateDirectory(definitionsPath);
            Directory.CreateDirectory(quarantinePath);
            
            _patternScanner = new PatternScanner();
            _heuristicAnalyzer = new HeuristicAnalyzer();
            _quarantineManager = new QuarantineManager(quarantinePath);
            
            // Load pattern signatures if available
            string signatureFile = Path.Combine(definitionsPath, "patterns.json");
            if (File.Exists(signatureFile))
            {
                _patternScanner.LoadSignatures(signatureFile);
            }
        }
        
        /// <summary>
        /// Initializes the scan engine by loading virus definitions
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                await UpdateDefinitionsAsync();
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing scan engine: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Updates virus definitions from the server
        /// </summary>
        public async Task<bool> UpdateDefinitionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/definitions");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var definitionsResponse = JsonConvert.DeserializeObject<DefinitionsResponse>(content);
                    
                    // In a real implementation, we would download the definitions file
                    // and parse it. For now, we'll use a simple placeholder.
                    _virusDefinitions = new Dictionary<string, string>
                    {
                        { "eicar", "44d88612fea8a8f36de82e1278abb02f" },  // EICAR test file MD5
                        { "test_virus", "e1cce88c0cb4d42a1fe1609e9a4b2c41" } // Example
                    };
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating definitions: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Cancels the current scan operation
        /// </summary>
        public void CancelScan()
        {
            if (_scanCancellationTokenSource != null && !_scanCancellationTokenSource.IsCancellationRequested)
            {
                _scanCancellationTokenSource.Cancel();
                _scanCancellationTokenSource = new CancellationTokenSource();
            }
        }
        
        /// <summary>
        /// Quarantines an infected file
        /// </summary>
        public async Task<bool> QuarantineFileAsync(string filePath, string threatName, string detectionMethod)
        {
            var threatInfo = new ThreatInfo
            {
                ThreatName = threatName,
                ThreatType = "Malware",
                DetectionMethod = detectionMethod
            };
            
            return await _quarantineManager.QuarantineFileAsync(filePath, threatInfo);
        }
        
        /// <summary>
        /// Restores a file from quarantine
        /// </summary>
        public async Task<bool> RestoreFromQuarantineAsync(string quarantineId, string restorePath = null)
        {
            return await _quarantineManager.RestoreFileAsync(quarantineId, restorePath);
        }
        
        /// <summary>
        /// Gets a list of all quarantined files
        /// </summary>
        public List<QuarantineEntry> GetQuarantinedFiles()
        {
            return _quarantineManager.GetQuarantinedFiles();
        }
        
        /// <summary>
        /// Deletes a file from quarantine
        /// </summary>
        public bool DeleteFromQuarantine(string quarantineId)
        {
            return _quarantineManager.DeleteQuarantinedFile(quarantineId);
        }
        
        /// <summary>
        /// Scans a file for viruses using multiple detection methods
        /// </summary>
        /// <param name="filePath">The path to the file to scan</param>
        /// <returns>The scan result</returns>
        public async Task<ScanResult> ScanFileAsync(string filePath)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Scan engine not initialized");
            }
            
            try
            {
                if (!File.Exists(filePath))
                {
                    return new ScanResult
                    {
                        FilePath = filePath,
                        IsInfected = false,
                        ErrorMessage = "File not found"
                    };
                }
                
                var result = new ScanResult
                {
                    FilePath = filePath,
                    IsInfected = false,
                    DetectionMethod = "None"
                };
                
                // Step 1: Hash-based detection
                string hash = await CalculateFileHashAsync(filePath);
                
                // Check if the hash matches any known virus
                foreach (var definition in _virusDefinitions)
                {
                    if (definition.Value == hash)
                    {
                        result.IsInfected = true;
                        result.ThreatName = definition.Key;
                        result.DetectionMethod = "Hash-based";
                        return result;
                    }
                }
                
                // Step 2: Pattern-based detection
                var patternResult = await _patternScanner.ScanFileAsync(filePath);
                if (patternResult.IsInfected)
                {
                    result.IsInfected = true;
                    result.ThreatName = patternResult.MatchedSignatures[0].SignatureName;
                    result.DetectionMethod = "Pattern-based";
                    result.MatchedPatterns = patternResult.MatchedSignatures[0].MatchedPatterns;
                    result.SignatureId = patternResult.MatchedSignatures[0].SignatureId;
                    return result;
                }
                
                // Step 3: Heuristic analysis
                var heuristicResult = await _heuristicAnalyzer.AnalyzeFileAsync(filePath);
                if (heuristicResult.RiskLevel == RiskLevel.High)
                {
                    result.IsInfected = true;
                    result.ThreatName = $"Suspicious.{Path.GetExtension(filePath).TrimStart('.')}";
                    result.DetectionMethod = "Heuristic";
                    result.RiskLevel = heuristicResult.RiskLevel;
                    result.RiskScore = heuristicResult.RiskScore;
                    result.HeuristicFindings = heuristicResult.Findings;
                    return result;
                }
                else if (heuristicResult.RiskLevel == RiskLevel.Medium)
                {
                    // For medium risk, we don't mark as infected but include the findings
                    result.RiskLevel = heuristicResult.RiskLevel;
                    result.RiskScore = heuristicResult.RiskScore;
                    result.HeuristicFindings = heuristicResult.Findings;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new ScanResult
                {
                    FilePath = filePath,
                    IsInfected = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Scans a directory for viruses
        /// </summary>
        /// <param name="directoryPath">The path to the directory to scan</param>
        /// <param name="recursive">Whether to scan subdirectories</param>
        /// <returns>A list of scan results</returns>
        public async Task<List<ScanResult>> ScanDirectoryAsync(string directoryPath, bool recursive = true)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Scan engine not initialized");
            }
            
            List<ScanResult> results = new List<ScanResult>();
            
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    results.Add(new ScanResult
                    {
                        FilePath = directoryPath,
                        IsInfected = false,
                        ErrorMessage = "Directory not found"
                    });
                    
                    return results;
                }
                
                // Get all files in the directory
                var files = Directory.GetFiles(directoryPath, "*", 
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                
                int totalFiles = files.Length;
                int processedFiles = 0;
                
                foreach (var file in files)
                {
                    // Scan the file
                    var result = await ScanFileAsync(file);
                    results.Add(result);
                    
                    // Update progress
                    processedFiles++;
                    OnScanProgress(new ScanProgressEventArgs
                    {
                        CurrentFile = file,
                        ProcessedFiles = processedFiles,
                        TotalFiles = totalFiles,
                        ProgressPercentage = (int)((double)processedFiles / totalFiles * 100)
                    });
                }
                
                // Scan completed
                OnScanCompleted(new ScanCompletedEventArgs
                {
                    Results = results,
                    TotalFiles = totalFiles,
                    InfectedFiles = results.Count(r => r.IsInfected)
                });
                
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning directory: {ex.Message}");
                results.Add(new ScanResult
                {
                    FilePath = directoryPath,
                    IsInfected = false,
                    ErrorMessage = ex.Message
                });
                
                return results;
            }
        }
        
        /// <summary>
        /// Moves an infected file to quarantine
        /// </summary>
        /// <param name="filePath">The path to the infected file</param>
        /// <returns>True if the file was quarantined successfully</returns>
        public bool QuarantineFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }
                
                string quarantineDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ZariVirusKiller", "Quarantine");
                
                if (!Directory.Exists(quarantineDir))
                {
                    Directory.CreateDirectory(quarantineDir);
                }
                
                string fileName = Path.GetFileName(filePath);
                string quarantinePath = Path.Combine(quarantineDir, 
                    $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}.qtn");
                
                // In a real implementation, we would encrypt the file before moving it
                // For now, we'll just move it
                File.Move(filePath, quarantinePath);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error quarantining file: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Calculates the MD5 hash of a file
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The MD5 hash as a hex string</returns>
        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = await Task.Run(() => md5.ComputeHash(stream));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
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
    
    public class ScanResult
    {
        public string FilePath { get; set; }
        public bool IsInfected { get; set; }
        public string ThreatName { get; set; }
        public string ErrorMessage { get; set; }
        public string DetectionMethod { get; set; }
        public string SignatureId { get; set; }
        public List<string> MatchedPatterns { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public int RiskScore { get; set; }
        public List<HeuristicFinding> HeuristicFindings { get; set; }
        
        public ScanResult()
        {
            MatchedPatterns = new List<string>();
            HeuristicFindings = new List<HeuristicFinding>();
        }
    }
    
    public class ScanProgressEventArgs : EventArgs
    {
        public string CurrentFile { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        public int ProgressPercentage { get; set; }
    }
    
    public class ScanCompletedEventArgs : EventArgs
    {
        public List<ScanResult> Results { get; set; }
        public int TotalFiles { get; set; }
        public int InfectedFiles { get; set; }
    }
    
    public class DefinitionsResponse
    {
        [JsonProperty("definitions_version")]
        public string DefinitionsVersion { get; set; }
        
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}