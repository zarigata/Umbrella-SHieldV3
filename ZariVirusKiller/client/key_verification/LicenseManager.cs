using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using Newtonsoft.Json;
using System.IO;

namespace ZariVirusKiller.KeyVerification
{
    public class LicenseManager
    {
        private readonly string _serverUrl;
        private readonly HttpClient _httpClient;
        private string _licenseKey;
        private bool _isActivated;
        
        public bool IsActivated => _isActivated;
        
        public LicenseManager(string serverUrl)
        {
            _serverUrl = serverUrl;
            _httpClient = new HttpClient();
        }
        
        /// <summary>
        /// Activates the license with the server
        /// </summary>
        /// <param name="licenseKey">The license key to activate</param>
        /// <returns>True if activation was successful</returns>
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
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error activating license: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if the current license is valid
        /// </summary>
        /// <returns>True if the license is valid</returns>
        public async Task<bool> ValidateLicenseAsync()
        {
            try
            {
                // Try to load saved license info
                var licenseInfo = LoadLicenseInfo();
                if (licenseInfo == null)
                {
                    return false;
                }
                
                return await ActivateLicenseAsync(licenseInfo.LicenseKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating license: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets a unique device ID for this machine
        /// </summary>
        /// <returns>A unique device ID</returns>
        private string GetDeviceId()
        {
            try
            {
                // Get processor ID
                string processorId = "";
                using (ManagementClass mc = new ManagementClass("Win32_Processor"))
                using (ManagementObjectCollection moc = mc.GetInstances())
                {
                    foreach (ManagementObject mo in moc)
                    {
                        processorId = mo.Properties["ProcessorId"].Value.ToString();
                        break;
                    }
                }
                
                // Get motherboard serial number
                string motherboardSerial = "";
                using (ManagementClass mc = new ManagementClass("Win32_BaseBoard"))
                using (ManagementObjectCollection moc = mc.GetInstances())
                {
                    foreach (ManagementObject mo in moc)
                    {
                        motherboardSerial = mo.Properties["SerialNumber"].Value.ToString();
                        break;
                    }
                }
                
                // Combine and hash
                string combined = $"{processorId}|{motherboardSerial}";
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting device ID: {ex.Message}");
                return Guid.NewGuid().ToString(); // Fallback to a random GUID
            }
        }
        
        private void SaveLicenseInfo(string licenseKey, string deviceId)
        {
            try
            {
                var licenseInfo = new LicenseInfo
                {
                    LicenseKey = licenseKey,
                    DeviceId = deviceId,
                    ActivationDate = DateTime.Now
                };
                
                string licenseFile = GetLicenseFilePath();
                string directory = Path.GetDirectoryName(licenseFile);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(licenseFile, JsonConvert.SerializeObject(licenseInfo));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving license info: {ex.Message}");
            }
        }
        
        private LicenseInfo LoadLicenseInfo()
        {
            try
            {
                string licenseFile = GetLicenseFilePath();
                
                if (File.Exists(licenseFile))
                {
                    string json = File.ReadAllText(licenseFile);
                    return JsonConvert.DeserializeObject<LicenseInfo>(json);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading license info: {ex.Message}");
                return null;
            }
        }
        
        private string GetLicenseFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appData, "ZariVirusKiller", "license.json");
        }
    }
    
    public class LicenseInfo
    {
        public string LicenseKey { get; set; }
        public string DeviceId { get; set; }
        public DateTime ActivationDate { get; set; }
    }
    
    public class ActivationResponse
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }
        
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}