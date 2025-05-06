using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Linq;
using System.Threading;

namespace ZariVirusKiller
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
        
        public event EventHandler<ScanProgressEventArgs> ScanProgress;
        public event EventHandler<ScanCompletedEventArgs> ScanCompleted;
        
        public ScanEngine(string serverUrl)
        {
            _serverUrl = serverUrl;
            _httpClient = new HttpClient();
            _virusDefinitions = new Dictionary<string, string>();
            _isInitialized = false;
        }
        
        /// <summary>
        /// Initializes the scan engine by loading virus definitions
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                // Load local definitions if available
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string definitionsPath = Path.Combine(appData, "ZariVirusKiller", "Definitions");
                
                if (Directory.Exists(definitionsPath))
                {
                    string versionFile = Path.Combine(definitionsPath, "version.txt");
                    if (File.Exists(versionFile))
                    {
                        string definitionsFile = Path.Combine(definitionsPath, "signatures.json");
                        if (File.Exists(definitionsFile))
                        {
                            string json = File.ReadAllText(definitionsFile);
                            _virusDefinitions = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                            _isInitialized = true;
                        }
                    }
                }
                
                return _isInitialized;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing scan engine: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts a scan of the specified path
        /// </summary>
        public async Task<ScanResult> ScanAsync(string path, bool scanSubdirectories = true)
        {
            if (!_isInitialized)
            {
                bool initialized = await InitializeAsync();
                if (!initialized)
                {
                    throw new InvalidOperationException("Scan engine not initialized");
                }
            }
            
            _scanCancellationTokenSource = new CancellationTokenSource();
            var token = _scanCancellationTokenSource.Token;
            
            DateTime startTime = DateTime.Now;
            ScanResult result = new ScanResult
            {
                ScanDate = startTime,
                ScannedFiles = 0,
                ThreatCount = 0,
                Completed = false
            };
            
            try
            {
                // Get all files to scan
                List<string> filesToScan = new List<string>();
                if (File.Exists(path))
                {
                    filesToScan.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    SearchOption searchOption = scanSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    filesToScan.AddRange(Directory.GetFiles(path, "*.*", searchOption));
                }
                else
                {
                    throw new FileNotFoundException("Specified path does not exist", path);
                }
                
                int totalFiles = filesToScan.Count;
                int scannedFiles = 0;
                int threatsFound = 0;
                
                foreach (string file in filesToScan)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    
                    // Report progress
                    scannedFiles++;
                    OnScanProgress(new ScanProgressEventArgs
                    {
                        CurrentFile = file,
                        ScannedFiles = scannedFiles,
                        TotalFiles = totalFiles,
                        ThreatsFound = threatsFound
                    });
                    
                    // Scan file
                    bool isThreat = await ScanFileAsync(file);
                    if (isThreat)
                    {
                        threatsFound++;
                    }
                }
                
                // Update result
                result.ScannedFiles = scannedFiles;
                result.ThreatCount = threatsFound;
                result.Duration = DateTime.Now - startTime;
                result.Completed = true;
                
                // Report completion
                OnScanCompleted(new ScanCompletedEventArgs { Result = result });
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during scan: {ex.Message}");
                result.Duration = DateTime.Now - startTime;
                return result;
            }
        }
        
        /// <summary>
        /// Cancels an ongoing scan
        /// </summary>
        public void CancelScan()
        {
            _scanCancellationTokenSource?.Cancel();
        }
        
        /// <summary>
        /// Scans a single file for threats
        /// </summary>
        private async Task<bool> ScanFileAsync(string filePath)
        {
            try
            {
                // For files smaller than 10MB, calculate hash and check locally
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length < 10 * 1024 * 1024) // 10MB
                {
                    string fileHash = CalculateFileHash(filePath);
                    
                    // Check against local definitions
                    if (_virusDefinitions.ContainsKey(fileHash))
                    {
                        return true; // Threat found
                    }
                    
                    // For small files, we can also check with the server
                    if (fileInfo.Length < 1 * 1024 * 1024) // 1MB
                    {
                        return await ScanFileWithServerAsync(filePath);
                    }
                    
                    return false; // No threat found
                }
                else
                {
                    // For larger files, use more sophisticated scanning techniques
                    // This would involve checking file headers, scanning for patterns, etc.
                    // For now, we'll just return false
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning file {filePath}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sends a file to the server for scanning
        /// </summary>
        private async Task<bool> ScanFileWithServerAsync(string filePath)
        {
            try
            {
                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                    content.Add(fileContent, "file", Path.GetFileName(filePath));
                    
                    var response = await _httpClient.PostAsync($"{_serverUrl}/scan-file", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var scanResult = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        
                        return scanResult.threats_detected > 0;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning file with server: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Calculates SHA-256 hash of a file
        /// </summary>
        private string CalculateFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        
        /// <summary>
        /// Gets scan history from the database
        /// </summary>
        public static List<ScanResult> GetScanHistory()
        {
            // This would be implemented to retrieve scan history from a local database
            // For now, we'll return a mock history
            return new List<ScanResult>
            {
                new ScanResult
                {
                    ScanDate = DateTime.Now.AddDays(-1),
                    ScannedFiles = 1000,
                    ThreatCount = 0,
                    Duration = TimeSpan.FromMinutes(5),
                    Completed = true
                }
            };
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