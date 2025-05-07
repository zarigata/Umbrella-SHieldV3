using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace ZariVirusKiller.Updates
{
    /// <summary>
    /// Simplified update manager for beta testing
    /// </summary>
    public class BetaUpdateManager
    {
        private readonly string _serverUrl;
        private readonly HttpClient _httpClient;
        
        public event EventHandler<UpdateProgressEventArgs> UpdateProgress;
        public event EventHandler<UpdateCompletedEventArgs> UpdateCompleted;
        
        public BetaUpdateManager(string serverUrl)
        {
            _serverUrl = serverUrl;
            _httpClient = new HttpClient();
        }
        
        /// <summary>
        /// Checks for definition updates
        /// </summary>
        public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/api/definitions/latest");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var definitionsResponse = JsonConvert.DeserializeObject<DefinitionsResponse>(content);
                    
                    bool updateAvailable = !string.IsNullOrEmpty(definitionsResponse.Version) && 
                                         definitionsResponse.Version != currentVersion;
                    
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = updateAvailable,
                        CurrentVersion = currentVersion,
                        NewVersion = definitionsResponse.Version,
                        SignatureCount = definitionsResponse.SignatureCount,
                        DownloadUrl = definitionsResponse.Url
                    };
                }
                
                // For beta testing, provide a fallback
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = currentVersion,
                    NewVersion = currentVersion
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for updates: {ex.Message}");
                
                // For beta testing, provide a fallback
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = currentVersion,
                    NewVersion = currentVersion
                };
            }
        }
        
        /// <summary>
        /// Downloads and installs definition updates
        /// </summary>
        public async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl)
        {
            try
            {
                OnUpdateProgress(new UpdateProgressEventArgs { Status = "Downloading definitions...", ProgressPercentage = 0 });
                
                // Download the update file
                var response = await _httpClient.GetAsync(downloadUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    OnUpdateProgress(new UpdateProgressEventArgs { Status = "Download complete, installing...", ProgressPercentage = 50 });
                    
                    // Get the content
                    var content = await response.Content.ReadAsStringAsync();
                    var signatureContainer = JsonConvert.DeserializeObject<Engine.SignatureContainer>(content);
                    
                    if (signatureContainer != null)
                    {
                        // Save to definitions directory
                        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string definitionsPath = Path.Combine(appData, "ZariVirusKiller", "Definitions");
                        Directory.CreateDirectory(definitionsPath);
                        
                        string signatureFile = Path.Combine(definitionsPath, "patterns.json");
                        File.WriteAllText(signatureFile, content);
                        
                        OnUpdateProgress(new UpdateProgressEventArgs { Status = "Installation complete", ProgressPercentage = 100 });
                        
                        OnUpdateCompleted(new UpdateCompletedEventArgs
                        {
                            Success = true,
                            Version = signatureContainer.Version,
                            SignatureCount = signatureContainer.SignatureCount
                        });
                        
                        return true;
                    }
                }
                
                OnUpdateCompleted(new UpdateCompletedEventArgs { Success = false });
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading update: {ex.Message}");
                OnUpdateCompleted(new UpdateCompletedEventArgs { Success = false });
                return false;
            }
        }
        
        /// <summary>
        /// Gets the current installed definitions version
        /// </summary>
        public string GetCurrentDefinitionsVersion()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string signatureFile = Path.Combine(appData, "ZariVirusKiller", "Definitions", "patterns.json");
                
                if (File.Exists(signatureFile))
                {
                    string json = File.ReadAllText(signatureFile);
                    var signatureContainer = JsonConvert.DeserializeObject<Engine.SignatureContainer>(json);
                    
                    return signatureContainer?.Version ?? "0.0.0";
                }
                
                return "0.0.0";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current definitions version: {ex.Message}");
                return "0.0.0";
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
    
    /// <summary>
    /// Represents the result of an update check
    /// </summary>
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string NewVersion { get; set; }
        public int SignatureCount { get; set; }
        public string DownloadUrl { get; set; }
    }
    
    /// <summary>
    /// Represents a response from the definitions server
    /// </summary>
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
    
    /// <summary>
    /// Event arguments for update progress
    /// </summary>
    public class UpdateProgressEventArgs : EventArgs
    {
        public string Status { get; set; }
        public int ProgressPercentage { get; set; }
    }
    
    /// <summary>
    /// Event arguments for update completion
    /// </summary>
    public class UpdateCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Version { get; set; }
        public int SignatureCount { get; set; }
    }
}