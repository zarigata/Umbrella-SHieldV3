using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace ZariVirusKiller.KeyVerification
{
    /// <summary>
    /// Simplified license manager for beta testing
    /// </summary>
    public class BetaLicenseManager
    {
        private readonly string _serverUrl;
        private readonly HttpClient _httpClient;
        private string _licenseKey;
        private bool _isActivated;
        
        public bool IsActivated => _isActivated;
        
        public BetaLicenseManager(string serverUrl)
        {
            _serverUrl = serverUrl;
            _httpClient = new HttpClient();
            
            // Try to load saved license info
            LoadLicenseInfo();
        }
        
        /// <summary>
        /// Activates the license with the server
        /// </summary>
        public async Task<bool> ActivateLicenseAsync(string licenseKey)
        {
            try
            {
                _licenseKey = licenseKey;
                
                var deviceId = GetDeviceId();
                var requestData = new
                {
                    license_key = licenseKey,
                    device_id = deviceId
                };
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    Encoding.UTF8,
                    "application/json");
                
                var response = await _httpClient.PostAsync($"{_serverUrl}/verify-key", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ActivationResponse>(responseContent);
                    
                    _isActivated = result.Valid;
                    
                    if (_isActivated)
                    {
                        SaveLicenseInfo(licenseKey, deviceId);
                    }
                    
                    return _isActivated;
                }
                
                // For beta testing, allow offline activation
                if (!response.IsSuccessStatusCode && IsBetaKey(licenseKey))
                {
                    _isActivated = true;
                    SaveLicenseInfo(licenseKey, deviceId);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error activating license: {ex.Message}");
                
                // For beta testing, allow activation even if server is unreachable
                if (IsBetaKey(licenseKey))
                {
                    _isActivated = true;
                    SaveLicenseInfo(licenseKey, deviceId);
                    return true;
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// Checks if the current license is valid
        /// </summary>
        public async Task<bool> ValidateLicenseAsync()
        {
            try
            {
                // If we have a beta key, consider it valid
                if (!string.IsNullOrEmpty(_licenseKey) && IsBetaKey(_licenseKey))
                {
                    _isActivated = true;
                    return true;
                }
                
                // Try online validation
                if (!string.IsNullOrEmpty(_licenseKey))
                {
                    return await ActivateLicenseAsync(_licenseKey);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating license: {ex.Message}");
                
                // For beta, if we have a key but can't validate online, consider it valid
                if (!string.IsNullOrEmpty(_licenseKey) && IsBetaKey(_licenseKey))
                {
                    _isActivated = true;
                    return true;
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// Gets a simplified device ID for this machine
        /// </summary>
        private string GetDeviceId()
        {
            // Simple device ID based on machine name and OS
            string machineName = Environment.MachineName;
            string osVersion = Environment.OSVersion.ToString();
            
            // Create a simple hash
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(machineName + osVersion);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                
                // Convert to hex string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Saves license information to a file
        /// </summary>
        private void SaveLicenseInfo(string licenseKey, string deviceId)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string licenseDir = Path.Combine(appData, "ZariVirusKiller", "License");
                Directory.CreateDirectory(licenseDir);
                
                var licenseInfo = new LicenseInfo
                {
                    LicenseKey = licenseKey,
                    DeviceId = deviceId,
                    ActivationDate = DateTime.Now
                };
                
                string json = JsonConvert.SerializeObject(licenseInfo);
                File.WriteAllText(Path.Combine(licenseDir, "license.json"), json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving license info: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads license information from a file
        /// </summary>
        private void LoadLicenseInfo()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string licenseFile = Path.Combine(appData, "ZariVirusKiller", "License", "license.json");
                
                if (File.Exists(licenseFile))
                {
                    string json = File.ReadAllText(licenseFile);
                    var licenseInfo = JsonConvert.DeserializeObject<LicenseInfo>(json);
                    
                    if (licenseInfo != null)
                    {
                        _licenseKey = licenseInfo.LicenseKey;
                        
                        // For beta, consider saved licenses valid
                        if (IsBetaKey(_licenseKey))
                        {
                            _isActivated = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading license info: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if a license key is a beta key
        /// </summary>
        private bool IsBetaKey(string key)
        {
            // Beta keys start with "BETA-"
            return !string.IsNullOrEmpty(key) && key.StartsWith("BETA-", StringComparison.OrdinalIgnoreCase);
        }
    }
    
    /// <summary>
    /// Represents license information stored locally
    /// </summary>
    public class LicenseInfo
    {
        public string LicenseKey { get; set; }
        public string DeviceId { get; set; }
        public DateTime ActivationDate { get; set; }
    }
    
    /// <summary>
    /// Represents a response from the license activation server
    /// </summary>
    public class ActivationResponse
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("expiration_date")]
        public DateTime? ExpirationDate { get; set; }
    }
}