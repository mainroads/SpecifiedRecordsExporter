#define MyAppName "FolderStructureKiller"
#define MyExeNameUI "FolderStructureKiller"
#define MyAppParentDir "FolderStructureKiller\bin\Release\"
#define MyAppPath MyAppParentDir + MyExeNameUI + ".exe"
#dim Version[4]
#expr ParseVersion(MyAppPath, Version[0], Version[1], Version[2], Version[3])
#define MyAppVersion Str(Version[0]) + "." + Str(Version[1]) + "." + Str(Version[2])
#define MyAppPublisher "ShareX Developers"


[Setup]
AllowNoIcons=true
AppId={#MyAppName}
AppMutex={{825ED423-32F0-4CC3-A2C5-0E07ADD27C93}
AppName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/McoreD/FolderStructureKiller
AppSupportURL=https://github.com/McoreD/FolderStructureKiller/issues
AppUpdatesURL=https://github.com/McoreD/FolderStructureKiller/releases
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
Source: FolderStructureKillerUI\bin\Release\*.exe; Excludes: *.vshost.exe; DestDir: {app}; Flags: ignoreversion
Source: FolderStructureKillerUI\bin\Release\*.dll; DestDir: {app}; Flags: ignoreversion
Source: FolderStructureKillerUI\bin\Release\*.pdb; DestDir: {app}; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyExeNameUI}"; Filename: "{app}\{#MyExeNameUI}.exe"
Name: "{group}\{#MyExeNameCLI}"; Filename: "{app}\{#MyExeNameCLI}.exe"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyExeNameUI}.exe"; Tasks: desktopicon

[Run]
Filename: {app}\{#MyExeNameUI}.exe; Description: {cm:LaunchProgram,{#MyAppName} UI}; Flags: nowait postinstall skipifsilent
Filename: {app}\{#MyExeNameCLI}.exe; Description: {cm:LaunchProgram,{#MyAppName} Console}; Flags: nowait postinstall skipifsilent unchecked