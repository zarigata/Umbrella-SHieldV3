using System;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using ZariVirusKiller.Engine;
using ZariVirusKiller.KeyVerification;
using ZariVirusKiller.Updates;

namespace ZariVirusKiller
{
    /// <summary>
    /// Simplified main form for beta testing
    /// </summary>
    public partial class BetaMainForm : Form
    {
        private readonly BetaScanEngine _scanEngine;
        private readonly BetaLicenseManager _licenseManager;
        private readonly BetaUpdateManager _updateManager;
        private string _currentDefinitionsVersion;
        
        public BetaMainForm()
        {
            InitializeComponent();
            
            // Initialize components with a default server URL
            string serverUrl = "https://api.zariantivirus.com";
            _scanEngine = new BetaScanEngine();
            _licenseManager = new BetaLicenseManager(serverUrl);
            _updateManager = new BetaUpdateManager(serverUrl);
            
            // Set up event handlers
            _scanEngine.ScanProgress += OnScanProgress;
            _scanEngine.ScanCompleted += OnScanCompleted;
            _updateManager.UpdateProgress += OnUpdateProgress;
            _updateManager.UpdateCompleted += OnUpdateCompleted;
            
            // Get current definitions version
            _currentDefinitionsVersion = _updateManager.GetCurrentDefinitionsVersion();
            
            // Update UI with current version
            UpdateDefinitionsInfo();
        }
        
        /// <summary>
        /// Handles form load event
        /// </summary>
        private async void BetaMainForm_Load(object sender, EventArgs e)
        {
            // Check license status
            await CheckLicenseStatus();
            
            // Check for updates
            await CheckForUpdates();
        }
        
        /// <summary>
        /// Checks the license status
        /// </summary>
        private async Task CheckLicenseStatus()
        {
            bool isValid = await _licenseManager.ValidateLicenseAsync();
            
            if (!isValid)
            {
                // Show license activation dialog
                ShowLicenseDialog();
            }
        }
        
        /// <summary>
        /// Shows the license activation dialog
        /// </summary>
        private void ShowLicenseDialog()
        {
            // In a real implementation, this would show a dialog
            // For beta, we'll use a simple input box
            string licenseKey = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter your license key (use BETA-TEST for beta testing):",
                "License Activation",
                "");
                
            if (!string.IsNullOrEmpty(licenseKey))
            {
                ActivateLicense(licenseKey);
            }
            else
            {
                MessageBox.Show("A valid license key is required to use this software.",
                    "License Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        /// <summary>
        /// Activates the license
        /// </summary>
        private async void ActivateLicense(string licenseKey)
        {
            bool activated = await _licenseManager.ActivateLicenseAsync(licenseKey);
            
            if (activated)
            {
                MessageBox.Show("License activated successfully!",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to activate license. Please check your key and try again.",
                    "Activation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                // Show the dialog again
                ShowLicenseDialog();
            }
        }
        
        /// <summary>
        /// Checks for definition updates
        /// </summary>
        private async Task CheckForUpdates()
        {
            var result = await _updateManager.CheckForUpdatesAsync(_currentDefinitionsVersion);
            
            if (result.UpdateAvailable)
            {
                var response = MessageBox.Show(
                    $"A new virus definitions update is available.\n\n" +
                    $"Current version: {result.CurrentVersion}\n" +
                    $"New version: {result.NewVersion}\n" +
                    $"Signatures: {result.SignatureCount}\n\n" +
                    $"Do you want to download and install it now?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                    
                if (response == DialogResult.Yes)
                {
                    await _updateManager.DownloadAndInstallUpdateAsync(result.DownloadUrl);
                }
            }
        }
        
        /// <summary>
        /// Updates the definitions info in the UI
        /// </summary>
        private void UpdateDefinitionsInfo()
        {
            // In a real implementation, this would update UI controls
            // For beta, we'll just log to console
            Console.WriteLine($"Current definitions version: {_currentDefinitionsVersion}");
        }
        
        /// <summary>
        /// Starts a scan of the selected directory
        /// </summary>
        private async void StartScan(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                MessageBox.Show("Please select a valid directory to scan.",
                    "Invalid Directory", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Disable scan button during scan
            // btnScan.Enabled = false;
            
            // Start the scan
            await _scanEngine.ScanDirectoryAsync(directoryPath, true);
        }
        
        /// <summary>
        /// Handles scan progress events
        /// </summary>
        private void OnScanProgress(object sender, ScanProgressEventArgs e)
        {
            // Update progress UI
            // In a real implementation, this would update progress bars and labels
            // For beta, we'll just log to console
            Console.WriteLine($"Scanning: {e.CurrentFile}, {e.ScannedFiles}/{e.TotalFiles}, Threats: {e.ThreatsFound}");
        }
        
        /// <summary>
        /// Handles scan completed events
        /// </summary>
        private void OnScanCompleted(object sender, ScanCompletedEventArgs e)
        {
            // Enable scan button
            // btnScan.Enabled = true;
            
            // Show results
            MessageBox.Show(
                $"Scan completed!\n\n" +
                $"Files scanned: {e.Result.ScannedFiles}\n" +
                $"Threats found: {e.Result.ThreatCount}\n" +
                $"Duration: {e.Result.Duration.TotalSeconds:F1} seconds",
                "Scan Complete",
                MessageBoxButtons.OK,
                e.Result.ThreatCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// Handles update progress events
        /// </summary>
        private void OnUpdateProgress(object sender, UpdateProgressEventArgs e)
        {
            // Update progress UI
            // In a real implementation, this would update progress bars and labels
            // For beta, we'll just log to console
            Console.WriteLine($"Update progress: {e.Status}, {e.ProgressPercentage}%");
        }
        
        /// <summary>
        /// Handles update completed events
        /// </summary>
        private void OnUpdateCompleted(object sender, UpdateCompletedEventArgs e)
        {
            if (e.Success)
            {
                _currentDefinitionsVersion = e.Version;
                UpdateDefinitionsInfo();
                
                MessageBox.Show(
                    $"Update completed successfully!\n\n" +
                    $"New version: {e.Version}\n" +
                    $"Signatures: {e.SignatureCount}",
                    "Update Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "Failed to download and install the update. Please try again later.",
                    "Update Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Handles the scan button click event
        /// </summary>
        private void btnScan_Click(object sender, EventArgs e)
        {
            // Show folder browser dialog
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a directory to scan";
                dialog.ShowNewFolderButton = false;
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    StartScan(dialog.SelectedPath);
                }
            }
        }
        
        /// <summary>
        /// Handles the update button click event
        /// </summary>
        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            await CheckForUpdates();
        }
        
        /// <summary>
        /// Handles the license button click event
        /// </summary>
        private void btnLicense_Click(object sender, EventArgs e)
        {
            ShowLicenseDialog();
        }
    }
}