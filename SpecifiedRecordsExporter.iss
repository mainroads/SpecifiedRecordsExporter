#define MyAppDesc "Specified Records Exporter"
#define MyExeName "SpecifiedRecordsExporter"
#define MyAppParentDir "SpecifiedRecordsExporter\bin\Release\net8.0-windows10.0.19041.0\win10-x64\publish\"
#define MyAppFilePath MyAppParentDir + MyExeName + ".exe"
#define MyAppVersion GetStringFileInfo(MyAppFilePath, "ProductVersion")
#define MyAppPublisher "ShareX Team"

[Setup]
AllowNoIcons=true
AppId={#MyExeName}
AppMutex={{007a739a-834a-4b9a-8eff-22cf8fdc90a8}}
AppName={#MyAppDesc}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/McoreD/SpecifiedRecordsExporter
AppSupportURL=https://github.com/McoreD/SpecifiedRecordsExporter/issues
AppUpdatesURL=https://github.com/McoreD/SpecifiedRecordsExporter/releases
AppVerName={#MyAppDesc} {#MyAppVersion}
AppVersion={#MyAppVersion}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
Compression=lzma/ultra64
CreateAppDir=true
DefaultDirName={autopf}\{#MyAppDesc}
DefaultGroupName={#MyAppDesc}
DirExistsWarning=no
InternalCompressLevel=ultra64
LanguageDetectionMethod=uilanguage
MinVersion=6
OutputBaseFilename={#MyExeName}-{#MyAppVersion}-setup
OutputDir=Output\
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ShowLanguageDialog=auto
ShowUndisplayableLanguages=false
SignedUninstaller=false
SolidCompression=true
Uninstallable=true
UninstallDisplayIcon={app}\{#MyExeName}
UsePreviousAppDir=yes
UsePreviousGroup=yes
VersionInfoCompany={#MyAppDesc}
VersionInfoTextVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: {#MyAppParentDir}*.*; Excludes: *.vshost.exe; DestDir: {app}; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppDesc}"; Filename: "{app}\{#MyExeName}.exe"
Name: "{userdesktop}\{#MyAppDesc}"; Filename: "{app}\{#MyExeName}.exe"; Tasks: desktopicon

[Run]
Filename: {app}\{#MyExeName}.exe; Description: {cm:LaunchProgram,{#MyAppDesc} UI}; Flags: nowait postinstall skipifsilent
