[Setup]
AppName=SFROofs Safety Monitor
AppVersion=1.0.0
AppPublisher=SFROof Development
DefaultDirName={pf}\SFROof\SFROofsSafetyMonitor
DefaultGroupName=SFROofs Safety Monitor
OutputDir=.
OutputBaseFilename=SFROofsSafetyMonitorSetup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\SFROofs Safety Monitor"; Filename: "{app}\AlpacaSafetyMonitor.exe"
Name: "{group}\{cm:UninstallProgram,SFROofs Safety Monitor}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\AlpacaSafetyMonitor.exe"; Description: "{cm:LaunchProgram,SFROofs Safety Monitor}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  if MsgBox('This will install the SFROof Alpaca Safety Monitor.' + #13#10 + 
            'The service will run on http://localhost:11111' + #13#10 + 
            'Configure your ASCOM client to connect to this Alpaca device.' + #13#10 + #13#10 + 
            'Continue with installation?', mbConfirmation, MB_YESNO) = IDNO then
    Result := False;
end;
