using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.IO;

namespace ZariVirusKiller
{
    public partial class SettingsForm : Form
    {
        private static readonly Color PastelGreen = ColorTranslator.FromHtml("#A8D5BA");
        private static readonly Color PastelPink = ColorTranslator.FromHtml("#FFD1DC");
        private static readonly Color BackgroundColor = Color.White;
        private static readonly Color TextColor = Color.Black;
        
        private AppConfig _config;
        
        public SettingsForm()
        {
            InitializeComponent();
            ApplyCustomStyle();
            LoadTranslations();
            
            _config = AppConfig.Instance;
            LoadSettings();
        }
        
        #region Form Initialization
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(500, 400);
            this.Text = TranslationManager.GetTranslation("Settings");
            this.BackColor = BackgroundColor;
            this.DoubleBuffered = true;
            
            // Event handlers
            this.Paint += SettingsForm_Paint;
            this.MouseDown += SettingsForm_MouseDown;
            this.MouseMove += SettingsForm_MouseMove;
            
            // Create controls
            CreateControls();
            
            this.ResumeLayout(false);
        }
        
        private void CreateControls()
        {
            // Title label
            Label titleLabel = new Label();
            titleLabel.Text = TranslationManager.GetTranslation("Settings");
            titleLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            titleLabel.ForeColor = TextColor;
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(20, 20);
            this.Controls.Add(titleLabel);
            
            // Server URL
            Label serverUrlLabel = new Label();
            serverUrlLabel.Text = "URL do Servidor:";
            serverUrlLabel.Font = new Font("Segoe UI", 10);
            serverUrlLabel.ForeColor = TextColor;
            serverUrlLabel.AutoSize = true;
            serverUrlLabel.Location = new Point(20, 70);
            this.Controls.Add(serverUrlLabel);
            
            TextBox serverUrlTextBox = new TextBox();
            serverUrlTextBox.Font = new Font("Segoe UI", 10);
            serverUrlTextBox.Size = new Size(300, 25);
            serverUrlTextBox.Location = new Point(170, 70);
            serverUrlTextBox.Name = "serverUrlTextBox";
            this.Controls.Add(serverUrlTextBox);
            
            // Real-time protection
            CheckBox realTimeProtectionCheckbox = new CheckBox();
            realTimeProtectionCheckbox.Text = TranslationManager.GetTranslation("RealTimeProtection");
            realTimeProtectionCheckbox.Font = new Font("Segoe UI", 10);
            realTimeProtectionCheckbox.ForeColor = TextColor;
            realTimeProtectionCheckbox.Location = new Point(20, 110);
            realTimeProtectionCheckbox.AutoSize = true;
            realTimeProtectionCheckbox.Name = "realTimeProtectionCheckbox";
            this.Controls.Add(realTimeProtectionCheckbox);
            
            // Start with Windows
            CheckBox startWithWindowsCheckbox = new CheckBox();
            startWithWindowsCheckbox.Text = "Iniciar com o Windows";
            startWithWindowsCheckbox.Font = new Font("Segoe UI", 10);
            startWithWindowsCheckbox.ForeColor = TextColor;
            startWithWindowsCheckbox.Location = new Point(20, 140);
            startWithWindowsCheckbox.AutoSize = true;
            startWithWindowsCheckbox.Name = "startWithWindowsCheckbox";
            this.Controls.Add(startWithWindowsCheckbox);
            
            // Minimize to tray
            CheckBox minimizeToTrayCheckbox = new CheckBox();
            minimizeToTrayCheckbox.Text = "Minimizar para a bandeja";
            minimizeToTrayCheckbox.Font = new Font("Segoe UI", 10);
            minimizeToTrayCheckbox.ForeColor = TextColor;
            minimizeToTrayCheckbox.Location = new Point(20, 170);
            minimizeToTrayCheckbox.AutoSize = true;
            minimizeToTrayCheckbox.Name = "minimizeToTrayCheckbox";
            this.Controls.Add(minimizeToTrayCheckbox);
            
            // Language selection
            Label languageLabel = new Label();
            languageLabel.Text = "Idioma:";
            languageLabel.Font = new Font("Segoe UI", 10);
            languageLabel.ForeColor = TextColor;
            languageLabel.AutoSize = true;
            languageLabel.Location = new Point(20, 210);
            this.Controls.Add(languageLabel);
            
            ComboBox languageComboBox = new ComboBox();
            languageComboBox.Font = new Font("Segoe UI", 10);
            languageComboBox.Size = new Size(150, 25);
            languageComboBox.Location = new Point(170, 210);
            languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageComboBox.Name = "languageComboBox";
            languageComboBox.Items.AddRange(new object[] { "Português (Brasil)", "English" });
            this.Controls.Add(languageComboBox);
            
            // Excluded paths
            Label excludedPathsLabel = new Label();
            excludedPathsLabel.Text = "Caminhos excluídos:";
            excludedPathsLabel.Font = new Font("Segoe UI", 10);
            excludedPathsLabel.ForeColor = TextColor;
            excludedPathsLabel.AutoSize = true;
            excludedPathsLabel.Location = new Point(20, 250);
            this.Controls.Add(excludedPathsLabel);
            
            ListBox excludedPathsListBox = new ListBox();
            excludedPathsListBox.Font = new Font("Segoe UI", 9);
            excludedPathsListBox.Size = new Size(300, 80);
            excludedPathsListBox.Location = new Point(170, 250);
            excludedPathsListBox.Name = "excludedPathsListBox";
            this.Controls.Add(excludedPathsListBox);
            
            Button addExcludedPathButton = new Button();
            addExcludedPathButton.Text = "+";
            addExcludedPathButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            addExcludedPathButton.Size = new Size(30, 30);
            addExcludedPathButton.Location = new Point(480, 250);
            addExcludedPathButton.Click += (s, e) => {
                using (FolderBrowserDialog dialog = new FolderBrowserDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string path = dialog.SelectedPath;
                        if (!excludedPathsListBox.Items.Contains(path))
                        {
                            excludedPathsListBox.Items.Add(path);
                        }
                    }
                }
            };
            this.Controls.Add(addExcludedPathButton);
            
            Button removeExcludedPathButton = new Button();
            removeExcludedPathButton.Text = "-";
            removeExcludedPathButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            removeExcludedPathButton.Size = new Size(30, 30);
            removeExcludedPathButton.Location = new Point(480, 290);
            removeExcludedPathButton.Click += (s, e) => {
                if (excludedPathsListBox.SelectedIndex >= 0)
                {
                    excludedPathsListBox.Items.RemoveAt(excludedPathsListBox.SelectedIndex);
                }
            };
            this.Controls.Add(removeExcludedPathButton);
            
            // Save button
            Button saveButton = CreateActionButton("Salvar", PastelGreen, new Point(170, 350), () => {
                SaveSettings();
                this.DialogResult = DialogResult.OK;
                this.Close();
            });
            
            // Cancel button
            Button cancelButton = CreateActionButton("Cancelar", PastelPink, new Point(340, 350), () => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            });
            
            // Close button
            Button closeButton = new Button();
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Text = "X";
            closeButton.Font = new Font("Arial", 12, FontStyle.Bold);
            closeButton.Size = new Size(30, 30);
            closeButton.Location = new Point(this.Width - 40, 10);
            closeButton.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(closeButton);
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
            button.Size = new Size(150, 40);
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
        
        #region Settings Management
        
        private void LoadSettings()
        {
            // Load settings from AppConfig
            var serverUrlTextBox = this.Controls["serverUrlTextBox"] as TextBox;
            var realTimeProtectionCheckbox = this.Controls["realTimeProtectionCheckbox"] as CheckBox;
            var startWithWindowsCheckbox = this.Controls["startWithWindowsCheckbox"] as CheckBox;
            var minimizeToTrayCheckbox = this.Controls["minimizeToTrayCheckbox"] as CheckBox;
            var languageComboBox = this.Controls["languageComboBox"] as ComboBox;
            var excludedPathsListBox = this.Controls["excludedPathsListBox"] as ListBox;
            
            if (serverUrlTextBox != null)
                serverUrlTextBox.Text = _config.ServerUrl;
                
            if (realTimeProtectionCheckbox != null)
                realTimeProtectionCheckbox.Checked = _config.RealTimeProtection;
                
            if (startWithWindowsCheckbox != null)
                startWithWindowsCheckbox.Checked = _config.StartWithWindows;
                
            if (minimizeToTrayCheckbox != null)
                minimizeToTrayCheckbox.Checked = _config.MinimizeToTray;
                
            if (languageComboBox != null)
            {
                if (_config.Language == "pt-BR")
                    languageComboBox.SelectedIndex = 0;
                else if (_config.Language == "en-US")
                    languageComboBox.SelectedIndex = 1;
                else
                    languageComboBox.SelectedIndex = 0; // Default to Portuguese
            }
            
            if (excludedPathsListBox != null)
            {
                excludedPathsListBox.Items.Clear();
                foreach (var path in _config.ExcludedPaths)
                {
                    excludedPathsListBox.Items.Add(path);
                }
            }
        }
        
        private void SaveSettings()
        {
            // Save settings to AppConfig
            var serverUrlTextBox = this.Controls["serverUrlTextBox"] as TextBox;
            var realTimeProtectionCheckbox = this.Controls["realTimeProtectionCheckbox"] as CheckBox;
            var startWithWindowsCheckbox = this.Controls["startWithWindowsCheckbox"] as CheckBox;
            var minimizeToTrayCheckbox = this.Controls["minimizeToTrayCheckbox"] as CheckBox;
            var languageComboBox = this.Controls["languageComboBox"] as ComboBox;
            var excludedPathsListBox = this.Controls["excludedPathsListBox"] as ListBox;
            
            if (serverUrlTextBox != null)
                _config.ServerUrl = serverUrlTextBox.Text;
                
            if (realTimeProtectionCheckbox != null)
                _config.RealTimeProtection = realTimeProtectionCheckbox.Checked;
                
            if (startWithWindowsCheckbox != null)
                _config.StartWithWindows = startWithWindowsCheckbox.Checked;
                
            if (minimizeToTrayCheckbox != null)
                _config.MinimizeToTray = minimizeToTrayCheckbox.Checked;
                
            if (languageComboBox != null)
            {
                if (languageComboBox.SelectedIndex == 0)
                    _config.Language = "pt-BR";
                else if (languageComboBox.SelectedIndex == 1)
                    _config.Language = "en-US";
            }
            
            if (excludedPathsListBox != null)
            {
                _config.ExcludedPaths.Clear();
                foreach (var item in excludedPathsListBox.Items)
                {
                    _config.ExcludedPaths.Add(item.ToString());
                }
            }
            
            // Save to file
            _config.SaveConfig();
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
            this.Text = TranslationManager.GetTranslation("Settings");
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
        
        private void SettingsForm_Paint(object sender, PaintEventArgs e)
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
        
        private void SettingsForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPoint = new Point(e.X, e.Y);
            }
        }
        
        private void SettingsForm_MouseMove(object sender, MouseEventArgs e)
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
    }
}