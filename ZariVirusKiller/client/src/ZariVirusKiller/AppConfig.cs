using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using ZariVirusKiller.KeyVerification;
using ZariVirusKiller.Engine;
using ZariVirusKiller.Updates;

namespace ZariVirusKiller
{
    public class AppConfig
    {
        private static AppConfig _instance;
        private string _configPath;
        
        // Core components
        public LicenseManager LicenseManager { get; private set; }
        public ScanEngine ScanEngine { get; private set; }
        public UpdateManager UpdateManager { get; private set; }
        
        // Configuration settings
        public string ServerUrl { get; set; } = "http://localhost:5000";
        public bool RealTimeProtection { get; set; } = true;
        public bool StartWithWindows { get; set; } = true;
        public bool MinimizeToTray { get; set; } = true;
        public string Language { get; set; } = "pt-BR";
        public List<string> ExcludedPaths { get; set; } = new List<string>();
        public DateTime LastScanDate { get; set; } = DateTime.MinValue;
        
        private AppConfig()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "ZariVirusKiller");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            _configPath = Path.Combine(appFolder, "config.json");
            
            // Initialize components
            LicenseManager = new LicenseManager(ServerUrl);
            ScanEngine = new ScanEngine(ServerUrl);
            UpdateManager = new UpdateManager(ServerUrl);
        }
        
        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppConfig();
                    _instance.LoadConfig();
                }
                
                return _instance;
            }
        }
        
        public void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);
                    
                    // Update properties
                    ServerUrl = config.ServerUrl;
                    RealTimeProtection = config.RealTimeProtection;
                    StartWithWindows = config.StartWithWindows;
                    MinimizeToTray = config.MinimizeToTray;
                    Language = config.Language;
                    ExcludedPaths = config.ExcludedPaths;
                    LastScanDate = config.LastScanDate;
                    
                    // Reinitialize components with updated server URL
                    LicenseManager = new LicenseManager(ServerUrl);
                    ScanEngine = new ScanEngine(ServerUrl);
                    UpdateManager = new UpdateManager(ServerUrl);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                // Use default values
            }
        }
        
        public void SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }
        
        public async void InitializeComponents()
        {
            // Initialize scan engine
            await ScanEngine.InitializeAsync();
            
            // Check for updates
            var updateCheck = await UpdateManager.CheckForUpdatesAsync();
            if (updateCheck.UpdateAvailable)
            {
                // Notify user about available updates
                // This would be handled by the UI
            }
            
            // Validate license
            await LicenseManager.ValidateLicenseAsync();
        }
    }
}