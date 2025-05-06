# ZariVirusKiller Deployment Guide

This document provides detailed instructions for deploying both the client and server components of the ZariVirusKiller antivirus solution.

## Client Deployment

### Prerequisites

- Visual Studio 2019 or later
- .NET Framework 4.8 SDK
- WiX Toolset v3.11 or later (for creating MSI installers)
- Code signing certificate (recommended for production)

### Building the Client

#### Manual Build Process

1. Open the solution in Visual Studio:
   ```
   cd client
   start ZariVirusKiller.sln
   ```

2. Restore NuGet packages:
   - Right-click on the solution in Solution Explorer
   - Select "Restore NuGet Packages"

3. Build the solution:
   - Set the configuration to "Release"
   - Build > Build Solution (or press F6)

4. The compiled application will be in `client\bin\Release\`

#### Using the Build Script

The project includes a build script that automates the build and packaging process:

```batch
@echo off
echo Building ZariVirusKiller Client...

:: Set paths
set MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
set SOLUTION="%~dp0ZariVirusKiller.sln"
set WIX_CANDLE="C:\Program Files (x86)\WiX Toolset v3.11\bin\candle.exe"
set WIX_LIGHT="C:\Program Files (x86)\WiX Toolset v3.11\bin\light.exe"
set OUTPUT_DIR="%~dp0bin\Release"
set INSTALLER_DIR="%~dp0installer"

:: Build solution
echo Building solution...
%MSBUILD% %SOLUTION% /p:Configuration=Release /t:Rebuild /p:Platform="Any CPU"
if %ERRORLEVEL% NEQ 0 goto error

:: Create installer
echo Creating installer...
cd %INSTALLER_DIR%
%WIX_CANDLE% Product.wxs -out Product.wixobj
if %ERRORLEVEL% NEQ 0 goto error

%WIX_LIGHT% Product.wixobj -out "%OUTPUT_DIR%\ZariVirusKiller-Setup.msi"
if %ERRORLEVEL% NEQ 0 goto error

echo Build completed successfully.
echo Installer created at %OUTPUT_DIR%\ZariVirusKiller-Setup.msi
goto end

:error
echo Build failed with error %ERRORLEVEL%.

:end
```

### Creating the Installer

#### WiX Configuration

Create a `Product.wxs` file in the `client\installer` directory:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="ZariVirusKiller" Language="1033" Version="1.0.0.0" Manufacturer="YourCompany" UpgradeCode="PUT-GUID-HERE">
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />

    <MajorUpgrade DowngradeErrorMessage="A newer version of ZariVirusKiller is already installed." />
    <MediaTemplate EmbedCab="yes" />

    <Feature Id="ProductFeature" Title="ZariVirusKiller" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
      <ComponentRef Id="ApplicationShortcut" />
      <ComponentRef Id="ApplicationShortcutDesktop" />
    </Feature>

    <UIRef Id="WixUI_InstallDir" />
    <Property Id="WIXUI_INSTALLDIR" Value="INSTALLFOLDER" />
    <WixVariable Id="WixUILicenseRtf" Value="License.rtf" />
    <WixVariable Id="WixUIBannerBmp" Value="banner.bmp" />
    <WixVariable Id="WixUIDialogBmp" Value="dialog.bmp" />

    <InstallExecuteSequence>
      <Custom Action="LaunchApplication" After="InstallFinalize">NOT Installed</Custom>
    </InstallExecuteSequence>
  </Product>

  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="ZariVirusKiller" />
      </Directory>
      <Directory Id="ProgramMenuFolder">
        <Directory Id="ApplicationProgramsFolder" Name="ZariVirusKiller" />
      </Directory>
      <Directory Id="DesktopFolder" Name="Desktop" />
    </Directory>
  </Fragment>

  <Fragment>
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <Component Id="ProductComponent" Guid="*">
        <File Id="ZariVirusKillerEXE" Source="$(var.SolutionDir)\bin\Release\ZariVirusKiller.exe" KeyPath="yes" />
        <File Id="NewtonsoftJson" Source="$(var.SolutionDir)\bin\Release\Newtonsoft.Json.dll" />
        <ServiceInstall Id="ServiceInstaller" Type="ownProcess" Name="ZariVirusKillerService" DisplayName="ZariVirusKiller Protection Service" Description="Provides real-time protection against malware and viruses" Start="auto" Account="LocalSystem" ErrorControl="normal" />
        <ServiceControl Id="StartService" Start="install" Stop="both" Remove="uninstall" Name="ZariVirusKillerService" Wait="yes" />
      </Component>
    </ComponentGroup>

    <DirectoryRef Id="ApplicationProgramsFolder">
      <Component Id="ApplicationShortcut" Guid="*">
        <Shortcut Id="ApplicationStartMenuShortcut" Name="ZariVirusKiller" Description="ZariVirusKiller Antivirus" Target="[#ZariVirusKillerEXE]" WorkingDirectory="INSTALLFOLDER" />
        <RemoveFolder Id="CleanUpShortCut" Directory="ApplicationProgramsFolder" On="uninstall" />
        <RegistryValue Root="HKCU" Key="Software\ZariVirusKiller" Name="installed" Type="integer" Value="1" KeyPath="yes" />
      </Component>
    </DirectoryRef>

    <DirectoryRef Id="DesktopFolder">
      <Component Id="ApplicationShortcutDesktop" Guid="*">
        <Shortcut Id="ApplicationDesktopShortcut" Name="ZariVirusKiller" Description="ZariVirusKiller Antivirus" Target="[#ZariVirusKillerEXE]" WorkingDirectory="INSTALLFOLDER" />
        <RegistryValue Root="HKCU" Key="Software\ZariVirusKiller" Name="installed" Type="integer" Value="1" KeyPath="yes" />
      </Component>
    </DirectoryRef>
  </Fragment>

  <Fragment>
    <CustomAction Id="LaunchApplication" FileKey="ZariVirusKillerEXE" ExeCommand="" Return="asyncNoWait" />
  </Fragment>
</Wix>
```

### Code Signing

For production releases, sign the executable and installer:

```batch
:: Sign the executable
signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a "%OUTPUT_DIR%\ZariVirusKiller.exe"

:: Sign the installer
signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a "%OUTPUT_DIR%\ZariVirusKiller-Setup.msi"
```

## Server Deployment

### Prerequisites

- Docker and Docker Compose
- PostgreSQL (if not using Docker)
- Python 3.9 or later
- Node.js 16 or later (for building the admin dashboard)

### Local Deployment

#### Manual Setup

1. Set up a virtual environment:
   ```bash
   cd server
   python -m venv venv
   source venv/bin/activate  # On Windows: .\venv\Scripts\activate
   pip install -r requirements.txt
   ```

2. Configure the database:
   ```bash
   cd scripts
   python init_db.py
   ```

3. Build the admin dashboard:
   ```bash
   cd web
   npm install
   npm run build
   ```

4. Start the Flask server:
   ```bash
   cd app
   flask run --host=0.0.0.0 --port=5000
   ```

#### Using Docker Compose

1. Configure environment variables:
   Create a `.env` file in the `server` directory:
   ```
   DATABASE_URL=postgresql://zarivirus:password@db:5432/zarivirus
   JWT_SECRET_KEY=your-secure-secret-key
   ```

2. Build and start the containers:
   ```bash
   cd server
   docker-compose up -d
   ```

3. Initialize the database (first time only):
   ```bash
   docker-compose run migration
   ```

### Production Deployment

#### Server Hardening

1. Use a reverse proxy (Nginx or Apache) with SSL:
   ```nginx
   server {
       listen 443 ssl;
       server_name api.zarivirus.com;

       ssl_certificate /etc/letsencrypt/live/api.zarivirus.com/fullchain.pem;
       ssl_certificate_key /etc/letsencrypt/live/api.zarivirus.com/privkey.pem;

       location / {
           proxy_pass http://localhost:5000;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

2. Set up firewall rules:
   ```bash
   # Allow only necessary ports
   ufw allow 22/tcp
   ufw allow 80/tcp
   ufw allow 443/tcp
   ufw enable
   ```

3. Configure secure environment variables:
   - Use a secrets management service or environment variables
   - Never hardcode sensitive information

#### Automated Deployment

Create a deployment script:

```bash
#!/bin/bash

# Pull latest changes
git pull

# Build and restart containers
docker-compose down
docker-compose build
docker-compose up -d

# Run migrations if needed
docker-compose run migration

echo "Deployment completed successfully"
```

## Backup and Recovery

### Database Backup

Set up automated PostgreSQL backups:

```bash
#!/bin/bash

# Configuration
BACKUP_DIR="/var/backups/zarivirus"
DB_NAME="zarivirus"
DB_USER="zarivirus"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Create backup directory if it doesn't exist
mkdir -p $BACKUP_DIR

# Create backup
pg_dump -U $DB_USER $DB_NAME | gzip > "$BACKUP_DIR/$DB_NAME-$TIMESTAMP.sql.gz"

# Remove backups older than 30 days
find $BACKUP_DIR -type f -name "$DB_NAME-*.sql.gz" -mtime +30 -delete
```

Add this script to crontab to run daily:

```
0 2 * * * /path/to/backup-script.sh
```

### Definition Files Backup

Back up virus definition files:

```bash
#!/bin/bash

# Configuration
BACKUP_DIR="/var/backups/zarivirus/definitions"
DEFINITIONS_DIR="/app/definitions"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Create backup directory if it doesn't exist
mkdir -p $BACKUP_DIR

# Create backup
tar -czf "$BACKUP_DIR/definitions-$TIMESTAMP.tar.gz" -C "$(dirname $DEFINITIONS_DIR)" "$(basename $DEFINITIONS_DIR)"

# Remove backups older than 30 days
find $BACKUP_DIR -type f -name "definitions-*.tar.gz" -mtime +30 -delete
```

## Monitoring and Maintenance

### Health Checks

Implement health check endpoints:

```python
@app.route('/health', methods=['GET'])
def health_check():
    try:
        # Check database connection
        db.session.execute('SELECT 1').fetchall()
        
        # Check file system access
        os.access(app.config['DEFINITIONS_FOLDER'], os.R_OK | os.W_OK)
        
        return jsonify({'status': 'healthy'}), 200
    except Exception as e:
        return jsonify({'status': 'unhealthy', 'error': str(e)}), 500
```

### Log Rotation

Configure log rotation for application logs:

```
/var/log/zarivirus/*.log {
    daily
    missingok
    rotate 14
    compress
    delaycompress
    notifempty
    create 0640 www-data www-data
    sharedscripts
    postrotate
        systemctl reload zarivirus 2>/dev/null || true
    endscript
}
```

## Troubleshooting

### Common Issues

1. **Client Installation Fails**
   - Check Windows Event Viewer for MSI installation errors
   - Verify .NET Framework 4.8 is installed
   - Run the installer with logging: `msiexec /i ZariVirusKiller-Setup.msi /l*v install.log`

2. **Server Connection Issues**
   - Verify network connectivity: `ping api.zarivirus.com`
   - Check firewall settings
   - Verify SSL certificate is valid

3. **Database Connection Errors**
   - Check PostgreSQL service is running
   - Verify connection string and credentials
   - Check database permissions

### Diagnostic Tools

1. **Client Diagnostics**
   - Enable debug logging in `AppConfig.cs`
   - Check log files in `%LOCALAPPDATA%\ZariVirusKiller\Logs`

2. **Server Diagnostics**
   - Check Flask logs
   - Use Docker logs: `docker-compose logs -f web`
   - Monitor PostgreSQL logs

## Updating

### Client Updates

Implement an auto-update mechanism in the client:

```csharp
public async Task<bool> CheckForUpdates()
{
    try
    {
        var response = await _httpClient.GetAsync($"{_serverUrl}/client-version");
        if (response.IsSuccessStatusCode)
        {
            var versionInfo = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
            Version serverVersion = Version.Parse(versionInfo.version.ToString());
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            
            if (serverVersion > currentVersion)
            {
                return true; // Update available
            }
        }
        
        return false; // No update needed
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error checking for updates: {ex.Message}");
        return false;
    }
}

public async Task<bool> DownloadAndInstallUpdate()
{
    try
    {
        // Download update
        var response = await _httpClient.GetAsync($"{_serverUrl}/download-update");
        if (response.IsSuccessStatusCode)
        {
            string updatePath = Path.Combine(Path.GetTempPath(), "ZariVirusKiller-Update.exe");
            using (var fileStream = File.Create(updatePath))
            {
                await response.Content.CopyToAsync(fileStream);
            }
            
            // Launch updater
            Process.Start(updatePath, "/silent");
            return true;
        }
        
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error downloading update: {ex.Message}");
        return false;
    }
}
```

### Server Updates

Implement a zero-downtime update process:

```bash
#!/bin/bash

# Pull latest changes
git pull

# Build new containers
docker-compose build

# Update with zero downtime
docker-compose up -d --no-deps --scale web=2 --no-recreate web
sleep 10
docker-compose up -d --no-deps --scale web=1 --no-recreate web

echo "Update completed successfully"
```

## Conclusion

This deployment guide provides comprehensive instructions for deploying and maintaining the ZariVirusKiller antivirus solution. By following these guidelines, you can ensure a smooth deployment process and reliable operation of both the client and server components.

For additional support or questions, please contact support@zarivirus.com.