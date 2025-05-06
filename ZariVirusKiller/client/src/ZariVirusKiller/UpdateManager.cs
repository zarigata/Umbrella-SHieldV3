using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.Compression;

namespace ZariVirusKiller
{
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string NewVersion { get; set; }
        public int SignatureCount { get; set; }
        public string DownloadUrl { get; set; }
    }

    public class DefinitionsResponse
    {
        [JsonProperty("definitions_version")]
        public string Version { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("signature_count")]
        public int SignatureCount { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }

    public class UpdateManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;
        private string _definitionsPath;
        
        public event EventHandler<UpdateProgressEventArgs> UpdateProgress;
        public event EventHandler<UpdateCompletedEventArgs> UpdateCompleted;
        
        public UpdateManager(string serverUrl)
        {
            _serverUrl = serverUrl;
            _httpClient = new HttpClient();
            
            // Set definitions path
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _definitionsPath = Path.Combine(appData, "ZariVirusKiller", "Definitions");
            
            // Ensure directory exists
            if (!Directory.Exists(_definitionsPath))
            {
                Directory.CreateDirectory(_definitionsPath);
            }
        }
        
        /// <summary>
        /// Checks if updates are available
        /// </summary>
        /// <returns>Update check result with information about available updates</returns>
        public async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/definitions");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var definitionsResponse = JsonConvert.DeserializeObject<DefinitionsResponse>(content);
                    
                    string versionFile = Path.Combine(_definitionsPath, "version.txt");
                    string currentVersion = "0.0";
                    
                    if (File.Exists(versionFile))
                    {
                        currentVersion = File.ReadAllText(versionFile).Trim();
                    }
                    
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = currentVersion != definitionsResponse.Version,
                        CurrentVersion = currentVersion,
                        NewVersion = definitionsResponse.Version,
                        SignatureCount = definitionsResponse.SignatureCount,
                        DownloadUrl = definitionsResponse.Url
                    };
                }
                
                return new UpdateCheckResult { UpdateAvailable = false };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for updates: {ex.Message}");
                return new UpdateCheckResult { UpdateAvailable = false };
            }
        }
        
        /// <summary>
        /// Downloads and installs the latest virus definitions
        /// </summary>
        public async Task<bool> UpdateDefinitionsAsync()
        {
            try
            {
                // Check for updates
                var updateCheck = await CheckForUpdatesAsync();
                
                if (!updateCheck.UpdateAvailable)
                {
                    OnUpdateCompleted(new UpdateCompletedEventArgs
                    {
                        Success = true,
                        Version = updateCheck.CurrentVersion,
                        SignatureCount = updateCheck.SignatureCount
                    });
                    
                    return true;
                }
                
                // Report progress
                OnUpdateProgress(new UpdateProgressEventArgs
                {
                    Status = "Downloading definitions",
                    ProgressPercentage = 10
                });
                
                // Download definitions
                var response = await _httpClient.GetAsync($"{_serverUrl}{updateCheck.DownloadUrl}");
                
                if (!response.IsSuccessStatusCode)
                {
                    OnUpdateCompleted(new UpdateCompletedEventArgs { Success = false });
                    return false;
                }
                
                OnUpdateProgress(new UpdateProgressEventArgs
                {
                    Status = "Processing definitions",
                    ProgressPercentage = 50
                });
                
                // Save definitions file
                string definitionsZip = Path.Combine(_definitionsPath, $"definitions_{updateCheck.NewVersion}.zip");
                using (var fileStream = File.Create(definitionsZip))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
                
                // Extract definitions
                string extractPath = Path.Combine(_definitionsPath, "temp");
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }
                
                Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(definitionsZip, extractPath);
                
                OnUpdateProgress(new UpdateProgressEventArgs
                {
                    Status = "Installing definitions",
                    ProgressPercentage = 80
                });
                
                // Move files to definitions directory
                string signaturesFile = Path.Combine(extractPath, "signatures.json");
                if (File.Exists(signaturesFile))
                {
                    File.Copy(signaturesFile, Path.Combine(_definitionsPath, "signatures.json"), true);
                }
                
                // Update version file
                File.WriteAllText(Path.Combine(_definitionsPath, "version.txt"), updateCheck.NewVersion);
                
                // Clean up
                try
                {
                    Directory.Delete(extractPath, true);
                    File.Delete(definitionsZip);
                }
                catch
                {
                    // Ignore cleanup errors
                }
                
                OnUpdateProgress(new UpdateProgressEventArgs
                {
                    Status = "Update complete",
                    ProgressPercentage = 100
                });
                
                OnUpdateCompleted(new UpdateCompletedEventArgs
                {
                    Success = true,
                    Version = updateCheck.NewVersion,
                    SignatureCount = updateCheck.SignatureCount
                });
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating definitions: {ex.Message}");
                OnUpdateCompleted(new UpdateCompletedEventArgs { Success = false });
                return false;
            }
        }
        
        /// <summary>
        /// Gets the current virus definitions information
        /// </summary>
        public VirusDefinition GetCurrentDefinitions()
        {
            try
            {
                string versionFile = Path.Combine(_definitionsPath, "version.txt");
                string signaturesFile = Path.Combine(_definitionsPath, "signatures.json");
                
                if (!File.Exists(versionFile) || !File.Exists(signaturesFile))
                {
                    return null;
                }
                
                string version = File.ReadAllText(versionFile).Trim();
                string json = File.ReadAllText(signaturesFile);
                var signatures = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                
                return new VirusDefinition
                {
                    Version = version,
                    Date = File.GetLastWriteTime(signaturesFile),
                    SignatureCount = signatures?.Count ?? 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current definitions: {ex.Message}");
                return null;
            }
        }
        
        protected virtual void OnUpdateProgress(UpdateProgressEventArgs e)
        {
            UpdateProgress?.Invoke(this, e);
        }
        
        protected virtual void OnUpdateCompleted(UpdateCompletedEventArgs e)
        {
            UpdateCompleted?.Invoke(this, e);
        }
    }
}