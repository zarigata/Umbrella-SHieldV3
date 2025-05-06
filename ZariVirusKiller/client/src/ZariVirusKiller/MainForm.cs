using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Linq;

namespace ZariVirusKiller
{
    // Define data structures for the application
    public class ScanResult
    {
        public int ThreatCount { get; set; }
        public int ScannedFiles { get; set; }
        public DateTime ScanDate { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Completed { get; set; }
    }
    
    public class VirusDefinition
    {
        public string Version { get; set; }
        public DateTime Date { get; set; }
        public int SignatureCount { get; set; }
    }
    
    public partial class MainForm : Form
    {
        // Pastel colors as specified in the building plan
        private static readonly Color PastelGreen = ColorTranslator.FromHtml("#A8D5BA");
        private static readonly Color PastelPink = ColorTranslator.FromHtml("#FFD1DC");
        private static readonly Color BackgroundColor = Color.White;
        private static readonly Color TextColor = Color.Black;
        
        private NotifyIcon trayIcon;
        private bool realTimeProtectionEnabled = true;
        
        // Properties to store real application data
        private string _systemStatus;
        private DateTime? _lastScanDate;
        private ScanResult _lastScanResult;
        private VirusDefinition _currentDefinitions;
        
        public MainForm()
        {
            InitializeComponent();
            InitializeTrayIcon();
            ApplyCustomStyle();
            LoadTranslations();
            
            // Initialize with real data from application components
            LoadApplicationData();
        }
        
        private void LoadApplicationData()
        {
            // This method would load real data from various application components
            // For example:
            
            // Get real-time protection status from configuration
            if (AppConfig.Instance != null)
            {
                realTimeProtectionEnabled = AppConfig.Instance.RealTimeProtection;
            }
            
            // Load virus definitions information
            // _currentDefinitions = DefinitionManager.GetCurrentDefinitions();
            
            // Load last scan information
            // var scanHistory = ScanEngine.GetScanHistory();
            // if (scanHistory != null && scanHistory.Count > 0)
            // {
            //     _lastScanResult = scanHistory.OrderByDescending(s => s.ScanDate).FirstOrDefault();
            //     _lastScanDate = _lastScanResult?.ScanDate;
            // }
            
            // Update UI with loaded data
            UpdateStatusDisplay();
        }
        
        #region Form Initialization
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(800, 500);
            this.Text = TranslationManager.GetTranslation("AppTitle");
            this.BackColor = BackgroundColor;
            this.DoubleBuffered = true;
            
            // Event handlers
            this.Paint += MainForm_Paint;
            this.MouseDown += MainForm_MouseDown;
            this.MouseMove += MainForm_MouseMove;
            
            // Create dashboard controls
            CreateDashboardControls();
            
            this.ResumeLayout(false);
        }
        
        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();
            // Load application icon from resources
            try
            {
                // This would be replaced with actual icon loading from resources
                // trayIcon.Icon = Properties.Resources.AppIcon;
                // Fallback to system icon if custom icon is not available
                trayIcon.Icon = SystemIcons.Shield;
            }
            catch (Exception ex)
            {
                // Log the error and use system icon as fallback
                Console.WriteLine($"Failed to load tray icon: {ex.Message}");
                trayIcon.Icon = SystemIcons.Shield;
            }
            
            trayIcon.Text = TranslationManager.GetTranslation("AppTitle");
            trayIcon.Visible = true;
            
            // Create context menu for tray icon
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            
            ToolStripMenuItem openItem = new ToolStripMenuItem(TranslationManager.GetTranslation("AppTitle"));
            openItem.Click += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };
            
            ToolStripMenuItem scanItem = new ToolStripMenuItem(TranslationManager.GetTranslation("ScanNow"));
            scanItem.Click += (s, e) => { StartScan(); };
            
            ToolStripMenuItem updateItem = new ToolStripMenuItem(TranslationManager.GetTranslation("UpdateDefinitions"));
            updateItem.Click += (s, e) => { UpdateDefinitions(); };
            
            ToolStripMenuItem exitItem = new ToolStripMenuItem(TranslationManager.GetTranslation("Exit"));
            exitItem.Click += (s, e) => { Application.Exit(); };
            
            trayMenu.Items.AddRange(new ToolStripItem[] { openItem, scanItem, updateItem, exitItem });
            trayIcon.ContextMenuStrip = trayMenu;
            
            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };
        }
        
        private void CreateDashboardControls()
        {
            // Title label
            Label titleLabel = new Label();
            titleLabel.Text = TranslationManager.GetTranslation("AppTitle");
            titleLabel.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            titleLabel.ForeColor = TextColor;
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(20, 20);
            this.Controls.Add(titleLabel);
            
            // Status panel
            Panel statusPanel = new Panel();
            statusPanel.BackColor = Color.FromArgb(240, 240, 240);
            statusPanel.Size = new Size(760, 100);
            statusPanel.Location = new Point(20, 60);
            statusPanel.Paint += (s, e) => {
                using (GraphicsPath path = RoundedRectangle(statusPanel.ClientRectangle, 10))
                using (SolidBrush brush = new SolidBrush(statusPanel.BackColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            };
            
            Label statusLabel = new Label();
            statusLabel.Name = "statusLabel";
            statusLabel.Text = GetStatusText();
            statusLabel.Font = new Font("Segoe UI", 12);
            statusLabel.ForeColor = TextColor;
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(20, 20);
            statusPanel.Controls.Add(statusLabel);
            
            Label lastScanLabel = new Label();
            lastScanLabel.Name = "lastScanLabel";
            lastScanLabel.Text = GetLastScanText();
            lastScanLabel.Font = new Font("Segoe UI", 10);
            lastScanLabel.ForeColor = TextColor;
            lastScanLabel.AutoSize = true;
            lastScanLabel.Location = new Point(20, 50);
            statusPanel.Controls.Add(lastScanLabel);
            
            Label definitionsLabel = new Label();
            definitionsLabel.Name = "definitionsLabel";
            definitionsLabel.Text = GetDefinitionsText();
            definitionsLabel.Font = new Font("Segoe UI", 10);
            definitionsLabel.ForeColor = TextColor;
            definitionsLabel.AutoSize = true;
            definitionsLabel.Location = new Point(20, 75);
            statusPanel.Controls.Add(definitionsLabel);
            
            this.Controls.Add(statusPanel);
            
            // Action buttons
            CreateActionButton(TranslationManager.GetTranslation("ScanNow"), PastelGreen, new Point(20, 180), StartScan);
            CreateActionButton(TranslationManager.GetTranslation("UpdateDefinitions"), PastelPink, new Point(200, 180), UpdateDefinitions);
            CreateActionButton(TranslationManager.GetTranslation("Settings"), Color.LightSkyBlue, new Point(380, 180), OpenSettings);
            
            // Real-time protection toggle
            CheckBox realTimeProtectionCheckbox = new CheckBox();
            realTimeProtectionCheckbox.Text = TranslationManager.GetTranslation("RealTimeProtection");
            realTimeProtectionCheckbox.Font = new Font("Segoe UI", 10);
            realTimeProtectionCheckbox.ForeColor = TextColor;
            realTimeProtectionCheckbox.Location = new Point(20, 240);
            realTimeProtectionCheckbox.AutoSize = true;
            realTimeProtectionCheckbox.Checked = realTimeProtectionEnabled;
            realTimeProtectionCheckbox.CheckedChanged += (s, e) => {
                realTimeProtectionEnabled = realTimeProtectionCheckbox.Checked;
                
                // Update application configuration
                if (AppConfig.Instance != null)
                {
                    AppConfig.Instance.RealTimeProtection = realTimeProtectionEnabled;
                    // Save configuration changes
                    // AppConfig.Instance.SaveConfig();
                }
                
                // Update UI to reflect new status
                UpdateStatusDisplay();
            };
            this.Controls.Add(realTimeProtectionCheckbox);
            
            // Close button
            Button closeButton = new Button();
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Text = "X";
            closeButton.Font = new Font("Arial", 12, FontStyle.Bold);
            closeButton.Size = new Size(30, 30);
            closeButton.Location = new Point(this.Width - 40, 10);
            closeButton.Click += (s, e) => { this.Hide(); };
            this.Controls.Add(closeButton);
            
            // Minimize button
            Button minimizeButton = new Button();
            minimizeButton.FlatStyle = FlatStyle.Flat;
            minimizeButton.FlatAppearance.BorderSize = 0;
            minimizeButton.Text = "_";
            minimizeButton.Font = new Font("Arial", 12, FontStyle.Bold);
            minimizeButton.Size = new Size(30, 30);
            minimizeButton.Location = new Point(this.Width - 80, 10);
            minimizeButton.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };
            this.Controls.Add(minimizeButton);
        }
        
        private Button CreateActionButton(string text, Color color, Point location, Action clickAction)
        {
            Button button = new Button();
            button.Text = text;
            button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            button.ForeColor = Color.Black;
            button.BackColor = color;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Size = new Size(160, 40);
            button.Location = location;
            button.Click += (s, e) => { clickAction?.Invoke(); };
            
            // Custom rounded button
            button.Paint += (s, e) => {
                using (GraphicsPath path = RoundedRectangle(button.ClientRectangle, 10))
                using (SolidBrush brush = new SolidBrush(button.BackColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                    
                    // Center the text
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    using (SolidBrush textBrush = new SolidBrush(button.ForeColor))
                    {
                        e.Graphics.DrawString(button.Text, button.Font, textBrush, button.ClientRectangle, sf);
                    }
                }
                e.Graphics.SmoothingMode = SmoothingMode.Default;
            };
            
            this.Controls.Add(button);
            return button;
        }
        
        #endregion
        
        #region UI Customization
        
        private void ApplyCustomStyle()
        {
            // Custom form shadow and other visual effects could be added here
        }
        
        private void LoadTranslations()
        {
            // Update all control texts with translations
            this.Text = TranslationManager.GetTranslation("AppTitle");
            // Other controls are updated during creation
        }
        
        private GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();
            
            // Top left arc
            path.AddArc(arc, 180, 90);
            
            // Top right arc
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            
            // Bottom right arc
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            
            // Bottom left arc
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            
            path.CloseFigure();
            return path;
        }
        
        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            // Draw rounded form border
            using (GraphicsPath path = RoundedRectangle(this.ClientRectangle, 15))
            {
                this.Region = new Region(path);
                using (Pen pen = new Pen(Color.LightGray, 1))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
        
        #endregion
        
        #region Form Dragging
        
        private bool isDragging = false;
        private Point dragStartPoint;
        
        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPoint = new Point(e.X, e.Y);
            }
        }
        
        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point p = PointToScreen(e.Location);
                Location = new Point(p.X - dragStartPoint.X, p.Y - dragStartPoint.Y);
            }
        }
        
        protected override void OnMouseUp(MouseEventArgs e)
        {
            isDragging = false;
            base.OnMouseUp(e);
        }
        
        #endregion
        
        #region Functionality
        
        private void StartScan()
        {
            // TODO: Implement scan functionality
            string scanMessage = TranslationManager.GetTranslation("StartingScan");
            string scanTitle = TranslationManager.GetTranslation("ScanTitle");
            MessageBox.Show(scanMessage, scanTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // This would be replaced with actual scan implementation
            // ScanEngine.StartScan(OnScanComplete, OnScanProgress);
        }
        
        private void UpdateDefinitions()
        {
            // TODO: Implement update functionality
            string updateMessage = TranslationManager.GetTranslation("UpdatingDefinitions");
            string updateTitle = TranslationManager.GetTranslation("UpdateTitle");
            MessageBox.Show(updateMessage, updateTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // This would be replaced with actual update implementation
            // DefinitionManager.UpdateDefinitions(OnUpdateComplete, OnUpdateProgress);
        }
        
        private void OpenSettings()
        {
            // Open settings form
            using (SettingsForm settingsForm = new SettingsForm())
            {
                settingsForm.ShowDialog(this);
                // Refresh UI if settings were changed
                UpdateUIFromSettings();
            }
        }
        
        private void UpdateUIFromSettings()
        {
            // Update UI elements based on current settings
            // This would be called after settings changes or on startup
            realTimeProtectionEnabled = AppConfig.Instance.RealTimeProtection;
            
            // Update UI elements
            UpdateStatusDisplay();
        }
        
        private void UpdateStatusDisplay()
        {
            // Update status display with current information
            Label statusLabel = Controls.Find("statusLabel", true).FirstOrDefault() as Label;
            if (statusLabel != null)
            {
                statusLabel.Text = GetStatusText();
            }
            
            Label lastScanLabel = Controls.Find("lastScanLabel", true).FirstOrDefault() as Label;
            if (lastScanLabel != null)
            {
                lastScanLabel.Text = GetLastScanText();
            }
            
            Label definitionsLabel = Controls.Find("definitionsLabel", true).FirstOrDefault() as Label;
            if (definitionsLabel != null)
            {
                definitionsLabel.Text = GetDefinitionsText();
            }
        }
        
        private string GetStatusText()
        {
            // Get current protection status text
            string statusKey = realTimeProtectionEnabled ? "ProtectionEnabled" : "ProtectionDisabled";
            return TranslationManager.GetTranslation("Status") + ": " + TranslationManager.GetTranslation(statusKey);
        }
        
        private string GetLastScanText()
        {
            // Get last scan information text
            string lastScanPrefix = TranslationManager.GetTranslation("LastScan") + ": ";
            
            if (_lastScanDate.HasValue)
            {
                return lastScanPrefix + _lastScanDate.Value.ToString("g");
            }
            else
            {
                return lastScanPrefix + TranslationManager.GetTranslation("Never");
            }
        }
        
        private string GetDefinitionsText()
        {
            // Get virus definitions information text
            string definitionsPrefix = TranslationManager.GetTranslation("Definitions") + ": ";
            
            if (_currentDefinitions != null)
            {
                return definitionsPrefix + _currentDefinitions.Version + " (" + _currentDefinitions.Date.ToString("d") + ")";
            }
            else
            {
                return definitionsPrefix + TranslationManager.GetTranslation("NotAvailable");
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                base.OnFormClosing(e);
            }
        }
        
        #endregion
    }
}