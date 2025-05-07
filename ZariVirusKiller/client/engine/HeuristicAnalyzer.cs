using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ZariVirusKiller.Engine
{
    /// <summary>
    /// Provides heuristic analysis capabilities to detect potentially malicious files
    /// that don't match known signatures
    /// </summary>
    public class HeuristicAnalyzer
    {
        // Suspicious API calls that might indicate malicious behavior
        private readonly List<string> _suspiciousApiCalls = new List<string>
        {
            "CreateRemoteThread",
            "VirtualAllocEx",
            "WriteProcessMemory",
            "SetWindowsHookEx",
            "GetProcAddress",
            "LoadLibrary",
            "WinExec",
            "ShellExecute",
            "CreateProcess",
            "ReadProcessMemory"
        };
        
        // File extensions that are commonly executable
        private readonly List<string> _executableExtensions = new List<string>
        {
            ".exe", ".dll", ".bat", ".cmd", ".vbs", ".js", ".ps1", ".msi", ".scr"
        };
        
        /// <summary>
        /// Analyzes a file for suspicious characteristics
        /// </summary>
        public async Task<HeuristicScanResult> AnalyzeFileAsync(string filePath)
        {
            var result = new HeuristicScanResult
            {
                FilePath = filePath,
                RiskScore = 0,
                Findings = new List<HeuristicFinding>()
            };
            
            try
            {
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    result.Error = "File not found";
                    return result;
                }
                
                // Get file info
                var fileInfo = new FileInfo(filePath);
                string extension = fileInfo.Extension.ToLower();
                
                // Check if file is executable
                bool isExecutable = _executableExtensions.Contains(extension);
                if (isExecutable)
                {
                    result.RiskScore += 10;
                    result.Findings.Add(new HeuristicFinding
                    {
                        Type = "FileType",
                        Description = $"Executable file type: {extension}",
                        RiskLevel = 10
                    });
                }
                
                // Check file size
                long fileSize = fileInfo.Length;
                if (fileSize < 1024 && isExecutable) // Suspiciously small executable
                {
                    result.RiskScore += 15;
                    result.Findings.Add(new HeuristicFinding
                    {
                        Type = "FileSize",
                        Description = $"Suspiciously small executable: {fileSize} bytes",
                        RiskLevel = 15
                    });
                }
                
                // Calculate entropy to detect packed/encrypted files
                double entropy = await CalculateFileEntropyAsync(filePath);
                if (entropy > 7.5) // High entropy indicates encryption or packing
                {
                    int riskLevel = (int)((entropy - 7.5) * 20); // Scale from 0-10
                    result.RiskScore += riskLevel;
                    result.Findings.Add(new HeuristicFinding
                    {
                        Type = "Entropy",
                        Description = $"High entropy ({entropy:F2}) suggests encryption or packing",
                        RiskLevel = riskLevel
                    });
                }
                
                // Check for suspicious strings in executable files
                if (isExecutable)
                {
                    var suspiciousStrings = await FindSuspiciousStringsAsync(filePath);
                    if (suspiciousStrings.Count > 0)
                    {
                        int riskLevel = Math.Min(suspiciousStrings.Count * 5, 30);
                        result.RiskScore += riskLevel;
                        result.Findings.Add(new HeuristicFinding
                        {
                            Type = "SuspiciousAPI",
                            Description = $"Found {suspiciousStrings.Count} suspicious API calls: {string.Join(", ", suspiciousStrings.Take(5))}",
                            RiskLevel = riskLevel
                        });
                    }
                }
                
                // Determine risk level based on score
                if (result.RiskScore >= 50)
                    result.RiskLevel = RiskLevel.High;
                else if (result.RiskScore >= 25)
                    result.RiskLevel = RiskLevel.Medium;
                else if (result.RiskScore > 0)
                    result.RiskLevel = RiskLevel.Low;
                else
                    result.RiskLevel = RiskLevel.None;
                
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }
        
        /// <summary>
        /// Calculates the entropy of a file to detect encryption or packing
        /// </summary>
        private async Task<double> CalculateFileEntropyAsync(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    Dictionary<byte, int> byteFrequency = new Dictionary<byte, int>();
                    long totalBytes = 0;
                    
                    // Count frequency of each byte
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < bytesRead; i++)
                        {
                            byte b = buffer[i];
                            if (byteFrequency.ContainsKey(b))
                                byteFrequency[b]++;
                            else
                                byteFrequency[b] = 1;
                        }
                        
                        totalBytes += bytesRead;
                    }
                    
                    // Calculate entropy
                    double entropy = 0;
                    foreach (var kvp in byteFrequency)
                    {
                        double probability = (double)kvp.Value / totalBytes;
                        entropy -= probability * Math.Log(probability, 2);
                    }
                    
                    return entropy;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating entropy: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Searches for suspicious API calls in the file
        /// </summary>
        private async Task<List<string>> FindSuspiciousStringsAsync(string filePath)
        {
            List<string> foundStrings = new List<string>();
            
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    StringBuilder sb = new StringBuilder();
                    
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // Convert bytes to string for searching
                        string chunk = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        sb.Append(chunk);
                    }
                    
                    string fileContent = sb.ToString();
                    
                    // Search for suspicious API calls
                    foreach (string api in _suspiciousApiCalls)
                    {
                        if (fileContent.Contains(api))
                        {
                            foundStrings.Add(api);
                        }
                    }
                }
                
                return foundStrings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching for suspicious strings: {ex.Message}");
                return foundStrings;
            }
        }
    }
    
    #region Models
    
    public enum RiskLevel
    {
        None,
        Low,
        Medium,
        High
    }
    
    public class HeuristicScanResult
    {
        public string FilePath { get; set; }
        public int RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public List<HeuristicFinding> Findings { get; set; }
        public string Error { get; set; }
    }
    
    public class HeuristicFinding
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public int RiskLevel { get; set; }
    }
    
    #endregion
}