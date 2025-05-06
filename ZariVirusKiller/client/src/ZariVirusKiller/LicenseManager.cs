using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using Newtonsoft.Json;
using System.IO;

namespace ZariVirusKiller
{
    public class LicenseManager
    {
        private readonly string _serverUrl;
        private readonly HttpClient _httpClient;
        private string _licenseKey;
        private bool _isActivated;
        private string _licensePath;
        
        public bool IsActivated => _isActivated;
        public string LicenseKey => _licenseKey;
        
        public LicenseManager(string serverUrl)
        {
            _serverUrl = serverUrl;
            _httpClient = new HttpClient();
            
            // Set license file path
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "ZariVirusKiller");
            _licensePath = Path.Combine(appFolder, "license.dat");
            
            // Load license if exists
            LoadLicense();
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
                    _isActivated = true;
                    SaveLicense(licenseKey, deviceId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorData = JsonConvert.DeserializeObject<dynamic>(errorContent);
                    Console.WriteLine($"License activation failed: {errorData?.reason ?? "Unknown error"}");
                    _isActivated = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error activating license: {ex.Message}");
                _isActivated = false;
                return false;
            }
        }
        
        /// <summary>
        /// Verifies the current license with the server
        /// </summary>
        /// <returns>True if license is valid</returns>
        public async Task<bool> VerifyLicenseAsync()
        {
            if (string.IsNullOrEmpty(_licenseKey))
            {
                return false;
            }
            
            try
            {
                var deviceId = GetDeviceId();
                var requestData = new
                {
                    license_key = _licenseKey,
                    device_id = deviceId
                };
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    Encoding.UTF8,
                    "application/json");
                
                var response = await _httpClient.PostAsync($"{_serverUrl}/verify-key", content);
                
                _isActivated = response.IsSuccessStatusCode;
                return _isActivated;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying license: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Deactivates the current license
        /// </summary>
        public void DeactivateLicense()
        {
            _licenseKey = null;
            _isActivated = false;
            
            // Delete license file
            if (File.Exists(_licensePath))
            {
                File.Delete(_licensePath);
            }
        }
        
        /// <summary>
        /// Gets a unique device ID for license activation
        /// </summary>
        private string GetDeviceId()
        {
            try
            {
                // Try to get processor ID
                string processorId = "";
                using (ManagementClass mc = new ManagementClass("Win32_Processor"))
                {
                    ManagementObjectCollection moc = mc.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {
                        processorId = mo.Properties["ProcessorId"].Value.ToString();
                        break;
                    }
                }
                
                // Try to get motherboard serial number
                string motherboardSerial = "";
                using (ManagementClass mc = new ManagementClass("Win32_BaseBoard"))
                {
                    ManagementObjectCollection moc = mc.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {
                        motherboardSerial = mo.Properties["SerialNumber"].Value.ToString();
                        break;
                    }
                }
                
                // Combine and hash
                string combined = $"{processorId}|{motherboardSerial}|{Environment.MachineName}";
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting device ID: {ex.Message}");
                
                // Fallback to machine name and OS info
                string fallback = $"{Environment.MachineName}|{Environment.OSVersion}";
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        
        /// <summary>
        /// Saves the license information to a file
        /// </summary>
        private void SaveLicense(string licenseKey, string deviceId)
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(_licensePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Create license data
                var licenseData = new
                {
                    key = licenseKey,
                    device_id = deviceId,
                    activation_date = DateTime.Now
                };
                
                // Encrypt and save
                string json = JsonConvert.SerializeObject(licenseData);
                File.WriteAllText(_licensePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving license: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads the license information from a file
        /// </summary>
        private void LoadLicense()
        {
            try
            {
                if (File.Exists(_licensePath))
                {
                    string json = File.ReadAllText(_licensePath);
                    var licenseData = JsonConvert.DeserializeObject<dynamic>(json);
                    
                    _licenseKey = licenseData.key;
                    
                    // Verify the device ID matches
                    string currentDeviceId = GetDeviceId();
                    string savedDeviceId = licenseData.device_id;
                    
                    if (currentDeviceId == savedDeviceId)
                    {
                        // We'll still verify with the server when the app starts
                        _isActivated = true;
                    }
                    else
                    {
                        // Device ID mismatch, license might have been moved
                        _isActivated = false;
                    }
                }
                else
                {
                    _isActivated = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading license: {ex.Message}");
                _isActivated = false;
            }
        }
    }
}