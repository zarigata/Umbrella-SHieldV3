using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using ZariVirusKiller.KeyVerification;

namespace ZariVirusKiller
{
    public partial class LicenseForm : Form
    {
        private static readonly Color PastelGreen = ColorTranslator.FromHtml("#A8D5BA");
        private static readonly Color PastelPink = ColorTranslator.FromHtml("#FFD1DC");
        private static readonly Color BackgroundColor = Color.White;
        private static readonly Color TextColor = Color.Black;
        
        private LicenseManager _licenseManager;
        
        public bool ActivationSuccessful { get; private set; }
        
        public LicenseForm()
        {
            InitializeComponent();
            ApplyCustomStyle();
            LoadTranslations();
            
            _licenseManager = AppConfig.Instance.LicenseManager;
        }
        
        #region Form Initialization
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(400, 300);
            this.Text = "Ativação de Licença";
            this.BackColor = BackgroundColor;
            this.DoubleBuffered = true;
            
            // Event handlers
            this.Paint += LicenseForm_Paint;
            this.MouseDown += LicenseForm_MouseDown;
            this.MouseMove += LicenseForm_MouseMove;
            
            // Create controls
            CreateControls();
            
            this.ResumeLayout(false);
        }
        
        private void CreateControls()
        {
            // Title label
            Label titleLabel = new Label();
            titleLabel.Text = "Ativação de Licença";
            titleLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            titleLabel.ForeColor = TextColor;
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(20, 20);
            this.Controls.Add(titleLabel);
            
            // Instructions label
            Label instructionsLabel = new Label();
            instructionsLabel.Text = "Por favor, insira sua chave de licença para ativar o produto:";
            instructionsLabel.Font = new Font("Segoe UI", 10);
            instructionsLabel.ForeColor = TextColor;
            instructionsLabel.AutoSize = true;
            instructionsLabel.Location = new Point(20, 60);
            this.Controls.Add(instructionsLabel);
            
            // License key textbox
            TextBox licenseKeyTextBox = new TextBox();
            licenseKeyTextBox.Font = new Font("Segoe UI", 10);
            licenseKeyTextBox.Size = new Size(360, 25);
            licenseKeyTextBox.Location = new Point(20, 90);
            licenseKeyTextBox.Name = "licenseKeyTextBox";
            this.Controls.Add(licenseKeyTextBox);
            
            // Status label
            Label statusLabel = new Label();
            statusLabel.Text = "";
            statusLabel.Font = new Font("Segoe UI", 10);
            statusLabel.ForeColor = Color.Red;
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(20, 125);
            statusLabel.Name = "statusLabel";
            this.Controls.Add(statusLabel);
            
            // Activate button
            Button activateButton = new Button();
            activateButton.Text = "Ativar";
            activateButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            activateButton.ForeColor = Color.Black;
            activateButton.BackColor = PastelGreen;
            activateButton.FlatStyle = FlatStyle.Flat;
            activateButton.FlatAppearance.BorderSize = 0;
            activateButton.Size = new Size(120, 40);
            activateButton.Location = new Point(140, 160);
            activateButton.Click += async (s, e) => 
            {
                statusLabel.Text = "Ativando...";
                statusLabel.ForeColor = Color.Blue;
                
                string licenseKey = licenseKeyTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(licenseKey))
                {
                    statusLabel.Text = "Por favor, insira uma chave de licença válida.";
                    statusLabel.ForeColor = Color.Red;
                    return;
                }
                
                bool result = await _licenseManager.ActivateLicenseAsync(licenseKey);
                
                if (result)
                {
                    statusLabel.Text = "Ativação bem-sucedida!";
                    statusLabel.ForeColor = Color.Green;
                    ActivationSuccessful = true;
                    
                    // Close the form after a short delay
                    Timer timer = new Timer();
                    timer.Interval = 1500;
                    timer.Tick += (sender, args) => 
                    {
                        timer.Stop();
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    };
                    timer.Start();
                }
                else
                {
                    statusLabel.Text = "Falha na ativação. Verifique sua chave e tente novamente.";
                    statusLabel.ForeColor = Color.Red;
                }
            };
            
            // Custom rounded button
            activateButton.Paint += (s, e) => {
                using (GraphicsPath path = RoundedRectangle(activateButton.ClientRectangle, 10))
                using (SolidBrush brush = new SolidBrush(activateButton.BackColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                    
                    // Center the text
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    using (SolidBrush textBrush = new SolidBrush(activateButton.ForeColor))
                    {
                        e.Graphics.DrawString(activateButton.Text, activateButton.Font, textBrush, activateButton.ClientRectangle, sf);
                    }
                }
                e.Graphics.SmoothingMode = SmoothingMode.Default;
            };
            
            this.Controls.Add(activateButton);
            
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
        
        #endregion
        
        #region UI Customization
        
        private void ApplyCustomStyle()
        {
            // Custom form shadow and other visual effects could be added here
        }
        
        private void LoadTranslations()
        {
            // Update all control texts with translations
            // For now, we're using hardcoded Portuguese text
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
        
        private void LicenseForm_Paint(object sender, PaintEventArgs e)
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
        
        private void LicenseForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPoint = new Point(e.X, e.Y);
            }
        }
        
        private void LicenseForm_MouseMove(object sender, MouseEventArgs e)
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