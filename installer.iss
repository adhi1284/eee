; Build with Inno Setup: ISCC.exe installer.iss

#define AppName "OVFTEAM.COM - DEADSHOT.io"
#define AppVersion "1.0.0"
#define AppExeName "OVFTEAM.COM - DEADSHOT.io.exe"

[Setup]
AppId={{F99888D1-DBB2-4C31-92E3-AC1195672C0D}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=OVFTEAM.COM
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename=OVFTEAM.COM-Setup
SetupIconFile=assets\favicon.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
