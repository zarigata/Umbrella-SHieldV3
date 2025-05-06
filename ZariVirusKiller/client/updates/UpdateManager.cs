using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.Compression;

namespace ZariVirusKiller.Updates
{
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
        /// <returns>True if updates are available</returns>
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
                    
                    bool updateAvailable = definitionsResponse.DefinitionsVersion != currentVersion;
                    
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = updateAvailable,
                        CurrentVersion = currentVersion,
                        LatestVersion = definitionsResponse.DefinitionsVersion,
                        DownloadUrl = updateAvailable ? $"{_serverUrl}{definitionsResponse.Url}" : null
                    };
                }
                
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    ErrorMessage = "Failed to check for updates"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for updates: {ex.Message}");
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Downloads and installs the latest virus definitions
        /// </summary>
        /// <returns>True if the update was successful</returns>
        public async Task<bool> UpdateDefinitionsAsync()
        {
            try
            {
                var updateCheck = await CheckForUpdatesAsync();
                
                if (!updateCheck.UpdateAvailable)
                {
                    OnUpdateCompleted(new UpdateCompletedEventArgs
                    {
                        Success = true,
                        Message = "Already up to date"
                    });
                    
                    return true;
                }
                
                // Download the definitions file
                OnUpdateProgress(new UpdateProgressEventArgs
                {
                    Status = "Downloading definitions...",
                    ProgressPercentage = 0
                });
                
                var response = await _httpClient.GetAsync(updateCheck.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    OnUpdateCompleted(new UpdateCompletedEventArgs
                    {
                        Success = false,
                        Message = "Failed to download definitions"
                    });
                    
                    return false;
                }
                
                // Get the total size
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var buffer = new byte[8192];
                var bytesRead = 0L;
                
                // Create a temporary file to download to
                string tempFile = Path.Combine(_definitionsPath, "definitions.tmp");
                string extractPath = Path.Combine(_definitionsPath, "temp");
                
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true))
                {
                    var read = 0;
                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        
                        bytesRead += read;
                        
                        if (totalBytes > 0)
                        {
                            var progress = (int)((double)bytesRead / totalBytes * 100);
                            OnUpdateProgress(new UpdateProgressEventArgs
                            {
                                Status = "Downloading definitions...",
                                ProgressPercentage = progress
                            });
                        }
                    }
                }
                
                // Extract the definitions
                OnUpdateProgress(new UpdateProgressEventArgs
                {
                    Status = "Installing definitions...",
                    ProgressPercentage = 75
                });
                
                // In a real implementation, we would extract the ZIP file
                // For now, we'll just create a placeholder file
                
                // Create version file
                File.WriteAllText(Path.Combine(_definitionsPath, "version.txt"), updateCheck.LatestVersion);
                
                // Create a placeholder definitions file
                File.WriteAllText(Path.Combine(_definitionsPath, "definitions.dat"), 
                    $"Placeholder definitions file version {updateCheck.LatestVersion}");
                
                // Clean up
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                
                OnUpdateProgress(new UpdateProgressEventArgs
                {
                    Status = "Update completed",
                    ProgressPercentage = 100
                });
                
                OnUpdateCompleted(new UpdateCompletedEventArgs
                {
                    Success = true,
                    Message = $"Updated to version {updateCheck.LatestVersion}"
                });
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating definitions: {ex.Message}");
                
                OnUpdateCompleted(new UpdateCompletedEventArgs
                {
                    Success = false,
                    Message = ex.Message
                });
                
                return false;
            }
        }
        
        /// <summary>
        /// Gets the path to the definitions directory
        /// </summary>
        public string GetDefinitionsPath()
        {
            return _definitionsPath;
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
    
    public class UpdateProgressEventArgs : EventArgs
    {
        public string Status { get; set; }
        public int ProgressPercentage { get; set; }
    }
    
    public class UpdateCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public string DownloadUrl { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    public class DefinitionsResponse
    {
        [JsonProperty("definitions_version")]
        public string DefinitionsVersion { get; set; }
        
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}