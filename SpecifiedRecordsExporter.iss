#define MyAppDesc "Specified Records Exporter"
#define MyExeName "SpecifiedRecordsExporter"
#define MyAppParentDir "FolderStructureKiller\bin\Release\net5.0-windows7.0\"
#define MyAppPath MyAppParentDir + MyExeName + ".exe"
#dim Version[4]
#expr ParseVersion(MyAppPath, Version[0], Version[1], Version[2], Version[3])
#define MyAppVersion Str(Version[0]) + "." + Str(Version[1]) + "." + Str(Version[2])
#define MyAppPublisher "ShareX Developers"

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
ArchitecturesAllowed=x86 x64 ia64
ArchitecturesInstallIn64BitMode=x64 ia64
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
Source: {#MyAppParentDir}*.exe; Excludes: *.vshost.exe; DestDir: {app}; Flags: ignoreversion
Source: {#MyAppParentDir}*.dll; DestDir: {app}; Flags: ignoreversion
Source: {#MyAppParentDir}*.pdb; DestDir: {app}; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppDesc}"; Filename: "{app}\{#MyExeName}.exe"
Name: "{userdesktop}\{#MyAppDesc}"; Filename: "{app}\{#MyExeName}.exe"; Tasks: desktopicon

[Run]
Filename: {app}\{#MyExeName}.exe; Description: {cm:LaunchProgram,{#MyAppDesc} UI}; Flags: nowait postinstall skipifsilent
