[Setup]
AppName=ZariVirusKiller Beta
AppVersion=0.9.0
DefaultDirName={autopf}\ZariVirusKiller Beta
DefaultGroupName=ZariVirusKiller Beta
UninstallDisplayIcon={app}\ZariVirusKiller.exe
OutputDir=.\Output
OutputBaseFilename=ZariVirusKiller_Beta_Setup
Compression=lzma
SolidCompression=yes

[Files]
; Main executable and required files
Source: ".\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Default definitions file
Source: ".\data\default_definitions.json"; DestDir: "{localappdata}\ZariVirusKiller\Definitions"; DestName: "patterns.json"; Flags: onlyifdoesntexist

[Icons]
Name: "{group}\ZariVirusKiller Beta"; Filename: "{app}\ZariVirusKiller.exe"
Name: "{group}\Uninstall ZariVirusKiller Beta"; Filename: "{uninstallexe}"
Name: "{commondesktop}\ZariVirusKiller Beta"; Filename: "{app}\ZariVirusKiller.exe"

[Run]
Filename: "{app}\ZariVirusKiller.exe"; Description: "Launch ZariVirusKiller Beta"; Flags: nowait postinstall skipifsilent

[Dirs]
Name: "{localappdata}\ZariVirusKiller"
Name: "{localappdata}\ZariVirusKiller\Definitions"
Name: "{localappdata}\ZariVirusKiller\Quarantine"
Name: "{localappdata}\ZariVirusKiller\License"
Name: "{localappdata}\ZariVirusKiller\Logs"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  // Display beta warning
  if MsgBox('This is a BETA version of ZariVirusKiller for testing purposes only. ' +
            'It may contain bugs and is not recommended for production use. ' +
            'Do you want to continue?', mbConfirmation, MB_YESNO) = IDNO then
    Result := False;
end;