; Inno Setup script for Canon Print Bridge.
; Builds "Setup PrintBridge <version>.exe" from the published self-contained bundle.
; Compile:  ISCC.exe installer\PrintBridge.iss   (run `dotnet publish ... -o publish` first)

#define AppName    "Canon Print Bridge"
#define AppVersion "0.1.0"
#define AppExe     "CanonPrintBridge.exe"

[Setup]
AppId={{9E1D5C2A-1B7E-4C4E-9F3A-CB1120CANON}}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=
DefaultDirName={autopf}\Canon Print Bridge
DefaultGroupName=Canon Print Bridge
DisableProgramGroupPage=yes
; Per-user install to a writable location so the app can rewrite appsettings.json
; without elevation; the user can still switch to Program Files in the wizard.
PrivilegesRequired=lowest
; Allow both the interactive mode dialog and command-line override (/CURRENTUSER,
; /ALLUSERS) so scripted/silent installs aren't blocked by the mode prompt.
PrivilegesRequiredOverridesAllowed=commandline dialog
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
OutputDir=..\dist
OutputBaseFilename=Setup PrintBridge {#AppVersion}
SetupIconFile=..\printer-xp-icon.ico
UninstallDisplayIcon={app}\{#AppExe}
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
en.PathsCaption=Paths
en.PathsDesc=Confirm the shared print-queue folder and the path to VBoxManage.exe.
en.QueueLabel=Print queue folder (shared with the Windows XP VM):
en.VBoxLabel=VBoxManage.exe:
en.VBoxMissing=VBoxManage.exe was not found at the given path. VirtualBox may not be installed. You can continue and fix this later in Settings.
ru.PathsCaption=Пути
ru.PathsDesc=Подтвердите общую папку очереди печати и путь к VBoxManage.exe.
ru.QueueLabel=Папка очереди печати (общая с виртуальной машиной Windows XP):
ru.VBoxLabel=VBoxManage.exe:
ru.VBoxMissing=VBoxManage.exe не найден по указанному пути. Возможно, VirtualBox не установлен. Можно продолжить и указать путь позже в настройках.

[Files]
; The whole published bundle except appsettings.json, which [Code] writes with the chosen values.
Source: "..\publish\*"; DestDir: "{app}"; Excludes: "appsettings.json,*.pdb"; \
  Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\Canon Print Bridge"; Filename: "{app}\{#AppExe}"; IconFilename: "{app}\{#AppExe}"
Name: "{autodesktop}\Canon Print Bridge"; Filename: "{app}\{#AppExe}"; IconFilename: "{app}\{#AppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\appsettings.json"

[Code]
var
  PathsPage: TInputQueryWizardPage;

function JsonEsc(S: String): String;
begin
  StringChangeEx(S, '\', '\\', True);
  StringChangeEx(S, '"', '\"', True);
  Result := S;
end;

procedure InitializeWizard;
begin
  PathsPage := CreateInputQueryPage(wpSelectDir,
    CustomMessage('PathsCaption'), CustomMessage('PathsCaption'), CustomMessage('PathsDesc'));
  PathsPage.Add(CustomMessage('QueueLabel'), False);
  PathsPage.Add(CustomMessage('VBoxLabel'), False);
  PathsPage.Values[0] := 'C:\Virtualization\Shared\Queue';
  PathsPage.Values[1] := ExpandConstant('{commonpf}\Oracle\VirtualBox\VBoxManage.exe');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = PathsPage.ID then
  begin
    if not FileExists(PathsPage.Values[1]) then
      MsgBox(CustomMessage('VBoxMissing'), mbInformation, MB_OK);
  end;
end;

procedure WriteAppSettings();
var
  Lang, S: String;
begin
  if ActiveLanguage = 'ru' then Lang := 'ru' else Lang := 'en';
  S :=
    '{' + #13#10 +
    '  "QueueRoot": "' + JsonEsc(PathsPage.Values[0]) + '",' + #13#10 +
    '  "LauncherPath": "Print-Canon.ps1",' + #13#10 +
    '  "VmName": "Microelectronics",' + #13#10 +
    '  "VBoxManagePath": "' + JsonEsc(PathsPage.Values[1]) + '",' + #13#10 +
    '  "OfficeToPdfPath": "OfficeToPDF.exe",' + #13#10 +
    '  "Language": "' + Lang + '"' + #13#10 +
    '}' + #13#10;
  SaveStringToFile(ExpandConstant('{app}\appsettings.json'), S, False);
  ForceDirectories(PathsPage.Values[0]);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    WriteAppSettings();
end;
