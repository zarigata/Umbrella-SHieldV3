using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZariVirusKiller.Engine
{
    /// <summary>
    /// Simplified pattern scanner for beta testing
    /// </summary>
    public class BetaPatternScanner
    {
        private List<SignatureDefinition> _signatures;
        
        public BetaPatternScanner()
        {
            _signatures = new List<SignatureDefinition>();
        }
        
        /// <summary>
        /// Loads signature definitions from a JSON file
        /// </summary>
        public bool LoadSignatures(string signatureFilePath)
        {
            try
            {
                if (!File.Exists(signatureFilePath))
                    return false;
                    
                string json = File.ReadAllText(signatureFilePath);
                var signatureContainer = JsonConvert.DeserializeObject<SignatureContainer>(json);
                
                if (signatureContainer?.Signatures != null)
                {
                    _signatures = signatureContainer.Signatures;
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading signatures: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Scans a file for pattern matches against loaded signatures
        /// </summary>
        public async Task<PatternScanResult> ScanFileAsync(string filePath)
        {
            var result = new PatternScanResult
            {
                FilePath = filePath,
                IsInfected = false,
                MatchedSignatures = new List<SignatureMatch>()
            };
            
            if (_signatures.Count == 0 || !File.Exists(filePath))
                return result;
                
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[4096]; // Read in chunks
                    int bytesRead;
                    
                    // For each signature, check if it matches the file
                    foreach (var signature in _signatures)
                    {
                        // Skip signatures without patterns
                        if (signature.Patterns == null || signature.Patterns.Count == 0)
                            continue;
                            
                        // Reset file position for each signature
                        fs.Position = 0;
                        
                        // For each pattern in the signature
                        foreach (var pattern in signature.Patterns)
                        {
                            // Skip invalid patterns
                            if (string.IsNullOrEmpty(pattern.HexPattern) && 
                                string.IsNullOrEmpty(pattern.AsciiPattern) &&
                                string.IsNullOrEmpty(pattern.Value))
                                continue;
                                
                            bool patternMatched = false;
                            string patternValue = pattern.HexPattern ?? pattern.AsciiPattern ?? pattern.Value;
                            
                            // Convert hex pattern to bytes if needed
                            byte[] searchBytes = null;
                            if (!string.IsNullOrEmpty(pattern.HexPattern))
                            {
                                searchBytes = HexStringToByteArray(pattern.HexPattern);
                            }
                            else if (!string.IsNullOrEmpty(pattern.AsciiPattern))
                            {
                                searchBytes = Encoding.ASCII.GetBytes(pattern.AsciiPattern);
                            }
                            else if (!string.IsNullOrEmpty(pattern.Value))
                            {
                                searchBytes = Encoding.UTF8.GetBytes(pattern.Value);
                            }
                            
                            if (searchBytes == null || searchBytes.Length == 0)
                                continue;
                                
                            // If pattern has a specific offset
                            if (!string.IsNullOrEmpty(pattern.Offset) && pattern.Offset != "any")
                            {
                                if (int.TryParse(pattern.Offset, out int offset))
                                {
                                    fs.Position = offset;
                                    bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length);
                                    
                                    if (bytesRead > 0)
                                    {
                                        patternMatched = SearchBuffer(buffer, bytesRead, searchBytes);
                                    }
                                }
                            }
                            else // Search entire file
                            {
                                fs.Position = 0;
                                while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    if (SearchBuffer(buffer, bytesRead, searchBytes))
                                    {
                                        patternMatched = true;
                                        break;
                                    }
                                }
                            }
                            
                            if (patternMatched)
                            {
                                result.IsInfected = true;
                                result.MatchedSignatures.Add(new SignatureMatch
                                {
                                    SignatureId = signature.Id,
                                    SignatureName = signature.Name,
                                    Severity = signature.Severity,
                                    Offset = fs.Position,
                                    MatchedPattern = patternValue
                                });
                                
                                // If we only need one match per file, break
                                if (!result.ScanAllSignatures)
                                    return result;
                                    
                                break; // Move to next signature
                            }
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Searches for a pattern in a buffer
        /// </summary>
        private bool SearchBuffer(byte[] buffer, int bytesRead, byte[] pattern)
        {
            // Simple Boyer-Moore-like search
            for (int i = 0; i <= bytesRead - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                
                if (found)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Converts a hex string to a byte array
        /// </summary>
        private byte[] HexStringToByteArray(string hex)
        {
            // Remove any spaces or other formatting
            hex = hex.Replace(" ", "").Replace("-", "");
            
            if (hex.Length % 2 != 0)
                hex = "0" + hex; // Ensure even length
                
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            
            return bytes;
        }
    }
}