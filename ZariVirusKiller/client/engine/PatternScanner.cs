using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZariVirusKiller.Engine
{
    /// <summary>
    /// Handles pattern-based virus detection using signature definitions
    /// </summary>
    public class PatternScanner
    {
        private List<SignatureDefinition> _signatures;
        
        public PatternScanner()
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
            
            if (_signatures.Count == 0)
                return result;
                
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[4096]; // Read in chunks
                    int bytesRead;
                    long filePosition = 0;
                    
                    // For each signature, check if it matches the file
                    foreach (var signature in _signatures)
                    {
                        bool allPatternsMatched = true;
                        List<string> matchedPatterns = new List<string>();
                        
                        // Reset file position for each signature
                        fs.Position = 0;
                        filePosition = 0;
                        
                        // For each pattern in the signature
                        foreach (var pattern in signature.Patterns)
                        {
                            bool patternMatched = false;
                            
                            // If pattern has a specific offset
                            if (pattern.Offset != "any")
                            {
                                if (int.TryParse(pattern.Offset, out int offset))
                                {
                                    fs.Position = offset;
                                    bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length);
                                    
                                    if (bytesRead > 0)
                                    {
                                        patternMatched = MatchPattern(buffer, bytesRead, pattern);
                                    }
                                }
                            }
                            else // Search entire file
                            {
                                fs.Position = 0;
                                patternMatched = false;
                                
                                while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    if (MatchPattern(buffer, bytesRead, pattern))
                                    {
                                        patternMatched = true;
                                        break;
                                    }
                                    
                                    // If we're at the end of the file, break
                                    if (bytesRead < buffer.Length)
                                        break;
                                }
                            }
                            
                            if (patternMatched)
                            {
                                matchedPatterns.Add(pattern.Value);
                            }
                            else if (signature.Logic == "all")
                            {
                                allPatternsMatched = false;
                                break;
                            }
                        }
                        
                        // Check if the signature matched based on logic
                        bool signatureMatched = false;
                        
                        if (signature.Logic == "all" && allPatternsMatched && matchedPatterns.Count == signature.Patterns.Count)
                        {
                            signatureMatched = true;
                        }
                        else if (signature.Logic == "any" && matchedPatterns.Count > 0)
                        {
                            signatureMatched = true;
                        }
                        
                        if (signatureMatched)
                        {
                            result.IsInfected = true;
                            result.MatchedSignatures.Add(new SignatureMatch
                            {
                                SignatureId = signature.Id,
                                SignatureName = signature.Name,
                                Severity = signature.Severity,
                                MatchedPatterns = matchedPatterns
                            });
                            
                            // If we only need to find one match, we can stop here
                            if (!result.ScanAllSignatures)
                                break;
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning file {filePath}: {ex.Message}");
                result.Error = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Matches a pattern against a buffer of bytes
        /// </summary>
        private bool MatchPattern(byte[] buffer, int bytesRead, PatternDefinition pattern)
        {
            if (pattern.Type == "hex")
            {
                // Convert hex string to byte array
                byte[] patternBytes = HexStringToByteArray(pattern.Value);
                
                // Simple Boyer-Moore search would be implemented here
                // For now, using a simple search
                for (int i = 0; i <= bytesRead - patternBytes.Length; i++)
                {
                    bool match = true;
                    for (int j = 0; j < patternBytes.Length; j++)
                    {
                        if (buffer[i + j] != patternBytes[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    
                    if (match)
                        return true;
                }
            }
            else if (pattern.Type == "ascii")
            {
                string bufferString = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                return bufferString.Contains(pattern.Value);
            }
            else if (pattern.Type == "regex")
            {
                string bufferString = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                return Regex.IsMatch(bufferString, pattern.Value);
            }
            
            return false;
        }
        
        /// <summary>
        /// Converts a hex string to a byte array
        /// </summary>
        private byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even length");
                
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            
            return bytes;
        }
    }
    
    // Models are now defined in PatternDefinition.cs
    
    public class PatternScanResult
    {
        public string FilePath { get; set; }
        public bool IsInfected { get; set; }
        public List<SignatureMatch> MatchedSignatures { get; set; }
        public string Error { get; set; }
        public bool ScanAllSignatures { get; set; } = false;
    }
    
    public class SignatureMatch
    {
        public string SignatureId { get; set; }
        public string SignatureName { get; set; }
        public string Severity { get; set; }
        public List<string> MatchedPatterns { get; set; }
    }
    
    #endregion
}