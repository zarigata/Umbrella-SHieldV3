using System;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZariVirusKiller
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Load translations
            try
            {
                string translationsPath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "..\\..\\..\\..\\data\\translations.json");
                
                if (File.Exists(translationsPath))
                {
                    string json = File.ReadAllText(translationsPath);
                    TranslationManager.Translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }
                else
                {
                    MessageBox.Show("Arquivo de traduções não encontrado.", "Erro", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar traduções: {ex.Message}", "Erro", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            // Initialize application configuration
            AppConfig.Instance.InitializeComponents();
            
            // Check license activation
            if (!AppConfig.Instance.LicenseManager.IsActivated)
            {
                using (var licenseForm = new LicenseForm())
                {
                    DialogResult result = licenseForm.ShowDialog();
                    
                    if (result != DialogResult.OK || !licenseForm.ActivationSuccessful)
                    {
                        // License activation failed or was cancelled
                        MessageBox.Show("O produto precisa ser ativado para continuar.", "Ativação Necessária", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }
            
            // Start the main application
            Application.Run(new MainForm());
        }
    }
    
    /// <summary>
    /// Simple translation manager
    /// </summary>
    public static class TranslationManager
    {
        public static Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
        
        public static string GetTranslation(string key)
        {
            if (Translations.TryGetValue(key, out string value))
            {
                return value;
            }
            return key; // Return the key itself if translation not found
        }
    }
}