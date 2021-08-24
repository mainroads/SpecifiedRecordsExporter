#define MyAppName "SpecifiedRecordsExporter"
#define MyExeNameUI "SpecifiedRecordsExporter"
#define MyAppParentDir "SpecifiedRecordsExporter\FolderStructureKiller\bin\Release\net5.0-windows7.0\win-x64\"
#define MyAppPath MyAppParentDir + MyExeNameUI + ".exe"
#dim Version[4]
#expr ParseVersion(MyAppPath, Version[0], Version[1], Version[2], Version[3])
#define MyAppVersion Str(Version[0]) + "." + Str(Version[1]) + "." + Str(Version[2])
#define MyAppPublisher "ShareX Developers"


[Setup]
AllowNoIcons=true
AppId={#MyAppName}
AppMutex={{007a739a-834a-4b9a-8eff-22cf8fdc90a8}}
AppName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/McoreD/SpecifiedRecordsExporter
AppSupportURL=https://github.com/McoreD/SpecifiedRecordsExporter/issues
AppUpdatesURL=https://github.com/McoreD/SpecifiedRecordsExporter/releases
AppVerName={#MyAppName} {#MyAppVersion}
AppVersion={#MyAppVersion}
ArchitecturesAllowed=x86 x64 ia64
ArchitecturesInstallIn64BitMode=x64 ia64
Compression=lzma/ultra64
CreateAppDir=true
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DirExistsWarning=no
InternalCompressLevel=ultra64
LanguageDetectionMethod=uilanguage
MinVersion=6
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-setup
OutputDir=Output\
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ShowLanguageDialog=auto
ShowUndisplayableLanguages=false
SignedUninstaller=false
SolidCompression=true
Uninstallable=true
UninstallDisplayIcon={app}\{#MyExeNameUI}
UsePreviousAppDir=yes
UsePreviousGroup=yes
VersionInfoCompany={#MyAppName}
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
Name: "{group}\{#MyExeNameUI}"; Filename: "{app}\{#MyExeNameUI}.exe"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyExeNameUI}.exe"; Tasks: desktopicon

[Run]
Filename: {app}\{#MyExeNameUI}.exe; Description: {cm:LaunchProgram,{#MyAppName} UI}; Flags: nowait postinstall skipifsilent
