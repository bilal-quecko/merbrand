; Requires Inno Setup 6.3 or newer. Compile only after making a fresh Windows build.
#define AppName "Mera Brand Pakistan Simulation"
#define AppVersion "1.0"
#define AppPublisher "BnM Technologies"
#define AppExeName "MeraBrandPakistan.exe"
#ifndef BuildSource
  #define BuildSource AddBackslash(SourcePath) + "..\Builds\Windows"
#endif
#if !FileExists(AddBackslash(BuildSource) + AppExeName)
  #error Missing Windows build. Build Builds/Windows/MeraBrandPakistan.exe first.
#endif
#if !FileExists(AddBackslash(BuildSource) + "UnityPlayer.dll")
  #error Missing UnityPlayer.dll. Supply the complete Windows build folder.
#endif

[Setup]
; Keep this ID unchanged for future releases so upgrades find the same installation.
AppId={{E3501F43-9A40-4C63-8CE0-47CF02A83DB7}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://merabrandpakistan.org
AppSupportURL=https://merabrandpakistan.org
DefaultDirName={localappdata}\Programs\MeraBrandPakistanSimulation
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UsePreviousAppDir=yes
DisableProgramGroupPage=yes
SourceDir={#BuildSource}
OutputDir={#SourcePath}..\Builds\Installer
OutputBaseFilename=MeraBrandPakistan-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=yes
RestartApplications=no
VersionInfoVersion=1.0.0.0

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "*"; DestDir: "{app}"; Excludes: "*.pdb,*_DoNotShip,*_DoNotShip\*,*_BackUpThisFolder_ButDontShipItWithYourGame,*_BackUpThisFolder_ButDontShipItWithYourGame\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

; Intentionally do not delete Unity user data or Windows Credential Manager entries.
