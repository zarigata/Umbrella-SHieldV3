using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ZariVirusKiller
{
    public static class TranslationManager
    {
        private static Dictionary<string, string> _translations;
        private static string _currentLanguage = "pt-BR";
        
        static TranslationManager()
        {
            LoadTranslations();
        }
        
        public static void SetLanguage(string languageCode)
        {
            _currentLanguage = languageCode;
            LoadTranslations();
        }
        
        public static string GetTranslation(string key)
        {
            if (_translations == null || !_translations.ContainsKey(key))
            {
                return key;
            }
            
            return _translations[key];
        }
        
        private static void LoadTranslations()
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string translationsPath = Path.Combine(appDir, "data", "translations.json");
                
                if (!File.Exists(translationsPath))
                {
                    // Try to find in development environment
                    string devPath = Path.Combine(
                        Directory.GetCurrentDirectory(), 
                        "client", 
                        "data", 
                        "translations.json");
                    
                    if (File.Exists(devPath))
                    {
                        translationsPath = devPath;
                    }
                    else
                    {
                        Console.WriteLine($"Translation file not found at: {translationsPath}");
                        _translations = new Dictionary<string, string>();
                        return;
                    }
                }
                
                string json = File.ReadAllText(translationsPath);
                var allTranslations = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
                
                if (allTranslations.ContainsKey(_currentLanguage))
                {
                    _translations = allTranslations[_currentLanguage];
                }
                else if (allTranslations.ContainsKey("en-US"))
                {
                    // Fallback to English
                    _translations = allTranslations["en-US"];
                }
                else
                {
                    // Use first available language
                    foreach (var lang in allTranslations.Keys)
                    {
                        _translations = allTranslations[lang];
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading translations: {ex.Message}");
                _translations = new Dictionary<string, string>();
            }
        }
    }
}