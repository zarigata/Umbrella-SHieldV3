using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZariVirusKiller.Engine
{
    /// <summary>
    /// Manages the quarantine functionality for infected files
    /// </summary>
    public class QuarantineManager
    {
        private readonly string _quarantineFolder;
        private readonly string _quarantineIndexFile;
        private List<QuarantineEntry> _quarantineIndex;
        private readonly byte[] _encryptionKey;
        
        /// <summary>
        /// Initializes a new instance of the QuarantineManager class
        /// </summary>
        /// <param name="quarantineFolder">The folder where quarantined files will be stored</param>
        public QuarantineManager(string quarantineFolder)
        {
            _quarantineFolder = quarantineFolder;
            _quarantineIndexFile = Path.Combine(_quarantineFolder, "quarantine_index.json");
            _quarantineIndex = new List<QuarantineEntry>();
            
            // Create a fixed encryption key for quarantine files
            // In a production environment, this should be securely stored
            _encryptionKey = Encoding.UTF8.GetBytes("ZariVirusKillerQuarantineKey123456");
            
            // Ensure quarantine folder exists
            if (!Directory.Exists(_quarantineFolder))
            {
                Directory.CreateDirectory(_quarantineFolder);
            }
            
            // Load existing quarantine index if it exists
            LoadQuarantineIndex();
        }
        
        /// <summary>
        /// Loads the quarantine index from disk
        /// </summary>
        private void LoadQuarantineIndex()
        {
            try
            {
                if (File.Exists(_quarantineIndexFile))
                {
                    string json = File.ReadAllText(_quarantineIndexFile);
                    _quarantineIndex = JsonConvert.DeserializeObject<List<QuarantineEntry>>(json) ?? new List<QuarantineEntry>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading quarantine index: {ex.Message}");
                _quarantineIndex = new List<QuarantineEntry>();
            }
        }
        
        /// <summary>
        /// Saves the quarantine index to disk
        /// </summary>
        private void SaveQuarantineIndex()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_quarantineIndex, Formatting.Indented);
                File.WriteAllText(_quarantineIndexFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving quarantine index: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Quarantines an infected file
        /// </summary>
        /// <param name="filePath">The path to the infected file</param>
        /// <param name="threatInfo">Information about the detected threat</param>
        /// <returns>True if quarantine was successful, false otherwise</returns>
        public async Task<bool> QuarantineFileAsync(string filePath, ThreatInfo threatInfo)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;
                    
                // Generate a unique ID for the quarantined file
                string quarantineId = Guid.NewGuid().ToString();
                string quarantineFilePath = Path.Combine(_quarantineFolder, $"{quarantineId}.quar");
                
                // Read the original file
                byte[] fileContent = await File.ReadAllBytesAsync(filePath);
                
                // Create quarantine metadata
                var metadata = new QuarantineMetadata
                {
                    OriginalPath = filePath,
                    OriginalFileName = Path.GetFileName(filePath),
                    QuarantineDate = DateTime.UtcNow,
                    FileSize = fileContent.Length,
                    ThreatName = threatInfo.ThreatName,
                    ThreatType = threatInfo.ThreatType,
                    DetectionMethod = threatInfo.DetectionMethod
                };
                
                // Serialize metadata
                string metadataJson = JsonConvert.SerializeObject(metadata);
                byte[] metadataBytes = Encoding.UTF8.GetBytes(metadataJson);
                
                // Create quarantine file structure: [4-byte metadata length][metadata][encrypted file content]
                using (FileStream fs = new FileStream(quarantineFilePath, FileMode.Create, FileAccess.Write))
                {
                    // Write metadata length as 4 bytes
                    byte[] metadataLength = BitConverter.GetBytes(metadataBytes.Length);
                    await fs.WriteAsync(metadataLength, 0, metadataLength.Length);
                    
                    // Write metadata
                    await fs.WriteAsync(metadataBytes, 0, metadataBytes.Length);
                    
                    // Encrypt and write file content
                    byte[] encryptedContent = EncryptData(fileContent);
                    await fs.WriteAsync(encryptedContent, 0, encryptedContent.Length);
                }
                
                // Add entry to quarantine index
                var entry = new QuarantineEntry
                {
                    Id = quarantineId,
                    OriginalPath = filePath,
                    OriginalFileName = Path.GetFileName(filePath),
                    QuarantineDate = DateTime.UtcNow,
                    ThreatName = threatInfo.ThreatName,
                    FileSize = fileContent.Length
                };
                
                _quarantineIndex.Add(entry);
                SaveQuarantineIndex();
                
                // Delete the original file
                File.Delete(filePath);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error quarantining file: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Restores a file from quarantine
        /// </summary>
        /// <param name="quarantineId">The ID of the quarantined file</param>
        /// <param name="restorePath">Optional path to restore the file to. If null, the original path is used.</param>
        /// <returns>True if restoration was successful, false otherwise</returns>
        public async Task<bool> RestoreFileAsync(string quarantineId, string restorePath = null)
        {
            try
            {
                // Find the quarantine entry
                var entry = _quarantineIndex.Find(e => e.Id == quarantineId);
                if (entry == null)
                    return false;
                    
                string quarantineFilePath = Path.Combine(_quarantineFolder, $"{quarantineId}.quar");
                if (!File.Exists(quarantineFilePath))
                    return false;
                    
                // Determine restore path
                string targetPath = restorePath ?? entry.OriginalPath;
                string targetDir = Path.GetDirectoryName(targetPath);
                
                // Ensure target directory exists
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);
                    
                // Read quarantine file
                using (FileStream fs = new FileStream(quarantineFilePath, FileMode.Open, FileAccess.Read))
                {
                    // Read metadata length
                    byte[] metadataLengthBytes = new byte[4];
                    await fs.ReadAsync(metadataLengthBytes, 0, 4);
                    int metadataLength = BitConverter.ToInt32(metadataLengthBytes, 0);
                    
                    // Skip metadata
                    fs.Seek(metadataLength, SeekOrigin.Current);
                    
                    // Read encrypted file content
                    byte[] encryptedContent = new byte[fs.Length - fs.Position];
                    await fs.ReadAsync(encryptedContent, 0, encryptedContent.Length);
                    
                    // Decrypt file content
                    byte[] decryptedContent = DecryptData(encryptedContent);
                    
                    // Write to restore path
                    await File.WriteAllBytesAsync(targetPath, decryptedContent);
                }
                
                // Remove from quarantine index
                _quarantineIndex.Remove(entry);
                SaveQuarantineIndex();
                
                // Delete quarantine file
                File.Delete(quarantineFilePath);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring file: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Permanently deletes a file from quarantine
        /// </summary>
        /// <param name="quarantineId">The ID of the quarantined file</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        public bool DeleteQuarantinedFile(string quarantineId)
        {
            try
            {
                // Find the quarantine entry
                var entry = _quarantineIndex.Find(e => e.Id == quarantineId);
                if (entry == null)
                    return false;
                    
                string quarantineFilePath = Path.Combine(_quarantineFolder, $"{quarantineId}.quar");
                if (File.Exists(quarantineFilePath))
                    File.Delete(quarantineFilePath);
                    
                // Remove from quarantine index
                _quarantineIndex.Remove(entry);
                SaveQuarantineIndex();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting quarantined file: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets a list of all quarantined files
        /// </summary>
        public List<QuarantineEntry> GetQuarantinedFiles()
        {
            return new List<QuarantineEntry>(_quarantineIndex);
        }
        
        /// <summary>
        /// Encrypts data using AES encryption
        /// </summary>
        private byte[] EncryptData(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.GenerateIV();
                
                using (MemoryStream ms = new MemoryStream())
                {
                    // Write the IV to the beginning of the encrypted data
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }
                    
                    return ms.ToArray();
                }
            }
        }
        
        /// <summary>
        /// Decrypts data using AES encryption
        /// </summary>
        private byte[] DecryptData(byte[] encryptedData)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                
                // Get the IV from the beginning of the encrypted data
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(encryptedData, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedData, iv.Length, encryptedData.Length - iv.Length);
                        cs.FlushFinalBlock();
                    }
                    
                    return ms.ToArray();
                }
            }
        }
    }
    
    #region Models
    
    public class QuarantineEntry
    {
        public string Id { get; set; }
        public string OriginalPath { get; set; }
        public string OriginalFileName { get; set; }
        public DateTime QuarantineDate { get; set; }
        public string ThreatName { get; set; }
        public long FileSize { get; set; }
    }
    
    public class QuarantineMetadata
    {
        public string OriginalPath { get; set; }
        public string OriginalFileName { get; set; }
        public DateTime QuarantineDate { get; set; }
        public long FileSize { get; set; }
        public string ThreatName { get; set; }
        public string ThreatType { get; set; }
        public string DetectionMethod { get; set; }
    }
    
    public class ThreatInfo
    {
        public string ThreatName { get; set; }
        public string ThreatType { get; set; }
        public string DetectionMethod { get; set; }
    }
    
    #endregion
}