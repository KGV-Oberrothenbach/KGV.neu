#define MyAppName "KGV"
#ifndef AppVersion
  #define AppVersion "0.0.0-local"
#endif
#define MyAppPublisher "KGV Oberrothenbach"
#define MyAppPublisherUrl "https://kgv-oberrothenbach.github.io/KGV-WPF/"
#define MyAppExeName "KGV.Wpf.exe"
#define MyAppSourceDir AddBackslash(SourcePath) + "..\\bin\\Release\\net8.0-windows"

[Setup]
AppId={{6CDB1C7A-1F02-4C08-BD9B-1B9C521F4D15}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppVerName={#MyAppName} {#AppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppPublisherUrl}
AppSupportURL={#MyAppPublisherUrl}
AppUpdatesURL={#MyAppPublisherUrl}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=KGV-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} starten"; Flags: nowait postinstall skipifsilent
