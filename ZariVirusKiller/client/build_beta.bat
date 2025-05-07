@echo off
echo Building ZariVirusKiller Beta...

set CONFIG=Release
if not "%1"=="" set CONFIG=%1

echo Configuration: %CONFIG%

:: Create output directories
if not exist .\bin\%CONFIG% mkdir .\bin\%CONFIG%
if not exist .\Output mkdir .\Output

:: Build the solution
echo Building solution...
msbuild ZariVirusKiller.sln /p:Configuration=%CONFIG% /t:Rebuild

if %ERRORLEVEL% neq 0 (
    echo Build failed with error code %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

:: Copy default definitions to bin directory
echo Copying default definitions...
if not exist .\bin\%CONFIG%\Definitions mkdir .\bin\%CONFIG%\Definitions
copy /Y .\data\default_definitions.json .\bin\%CONFIG%\Definitions\patterns.json

:: Create installer
echo Creating installer...
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" BetaInstaller.iss /Q

if %ERRORLEVEL% neq 0 (
    echo Installer creation failed with error code %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

echo Build completed successfully!
echo Installer created at: Output\ZariVirusKiller_Beta_Setup.exe

echo.
echo ZariVirusKiller Beta is ready for deployment.
echo.