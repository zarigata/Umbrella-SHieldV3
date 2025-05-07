using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ZariVirusKiller.Engine
{
    /// <summary>
    /// Represents a container for signature definitions
    /// </summary>
    public class SignatureContainer
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        
        [JsonProperty("signatures")]
        public List<SignatureDefinition> Signatures { get; set; }
        
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [JsonProperty("signature_count")]
        public int SignatureCount => Signatures?.Count ?? 0;
    }
    
    /// <summary>
    /// Represents a virus signature definition with multiple patterns
    /// </summary>
    public class SignatureDefinition
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("severity")]
        public string Severity { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("patterns")]
        public List<PatternDefinition> Patterns { get; set; }
        
        [JsonProperty("logic")]
        public string Logic { get; set; } = "all";
    }
    
    /// <summary>
    /// Represents a specific pattern within a signature definition
    /// </summary>
    public class PatternDefinition
    {
        [JsonProperty("offset")]
        public string Offset { get; set; } = "any";
        
        [JsonProperty("hex_pattern")]
        public string HexPattern { get; set; }
        
        [JsonProperty("ascii_pattern")]
        public string AsciiPattern { get; set; }
        
        [JsonProperty("wildcard")]
        public string Wildcard { get; set; }
        
        [JsonProperty("match_type")]
        public string MatchType { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("value")]
        public string Value { get; set; }
    }
    
    /// <summary>
    /// Represents the result of a pattern-based scan
    /// </summary>
    public class PatternScanResult
    {
        public string FilePath { get; set; }
        public bool IsInfected { get; set; }
        public List<SignatureMatch> MatchedSignatures { get; set; }
        public string Error { get; set; }
    }
    
    /// <summary>
    /// Represents a matched signature in a file
    /// </summary>
    public class SignatureMatch
    {
        public string SignatureId { get; set; }
        public string SignatureName { get; set; }
        public string Severity { get; set; }
        public long Offset { get; set; }
        public string MatchedPattern { get; set; }
    }
}