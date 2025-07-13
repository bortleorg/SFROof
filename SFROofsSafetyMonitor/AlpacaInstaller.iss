[Setup]
AppName=SFROofs Safety Monitor
AppVersion={#GetEnv('VERSION') != '' ? GetEnv('VERSION') : '1.0.0'}
AppPublisher=SFROof Development
DefaultDirName={pf}\SFROof\SFROofsSafetyMonitor
DefaultGroupName=SFROofs Safety Monitor
OutputDir=.
OutputBaseFilename=SFROofsSafetyMonitorSetup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\SFROofs Safety Monitor"; Filename: "{app}\SFROofsSafetyMonitor.exe"
Name: "{group}\{cm:UninstallProgram,SFROofs Safety Monitor}"; Filename: "{uninstallexe}"

[Run]
Filename: "sc"; Parameters: "create ""SFROofsSafetyMonitor"" binPath= ""{app}\SFROofsSafetyMonitor.exe"" start= auto"; Flags: runhidden; Description: "Install as Windows Service"; Check: InstallAsService
Filename: "sc"; Parameters: "start ""SFROofsSafetyMonitor"""; Flags: runhidden; Check: InstallAsService and StartServiceNow
Filename: "{app}\SFROofsSafetyMonitor.exe"; Description: "{cm:LaunchProgram,SFROofs Safety Monitor}"; Flags: nowait postinstall skipifsilent; Check: not InstallAsService

[UninstallRun]
Filename: "sc"; Parameters: "stop ""SFROofsSafetyMonitor"""; Flags: runhidden; RunOnceId: "StopService"
Filename: "sc"; Parameters: "delete ""SFROofsSafetyMonitor"""; Flags: runhidden; RunOnceId: "DeleteService"

[Code]
var
  ServicePage: TInputOptionWizardPage;

procedure InitializeWizard;
begin
  ServicePage := CreateInputOptionPage(wpSelectTasks,
    'Service Installation', 'Choose how to run SFROofs Safety Monitor',
    'You can run the Safety Monitor as a Windows Service (recommended) or as a regular application.',
    True, False);
  ServicePage.Add('Install as Windows Service (recommended - runs automatically)');
  ServicePage.Add('Run as regular application');
  ServicePage.Values[0] := True;
end;

function InstallAsService: Boolean;
begin
  Result := ServicePage.Values[0];
end;

function StartServiceNow: Boolean;
begin
  Result := MsgBox('Start the SFROofs Safety Monitor service now?', mbConfirmation, MB_YESNO) = IDYES;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if MsgBox('This will install the SFROof Alpaca Safety Monitor.' + #13#10 + 
            'The service will run on http://localhost:11111' + #13#10 + 
            'Configure your ASCOM client to connect to this Alpaca device.' + #13#10 + #13#10 + 
            'Continue with installation?', mbConfirmation, MB_YESNO) = IDNO then
    Result := False;
end;
