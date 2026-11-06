#define AppName "StreamMultiviewer"
#define AppExeName "StreamMultiviewer.exe"

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{E57BEA98-0411-4B4D-870A-4EABCA167220}
AppName={#AppName}
AppVerName={#AppName}
AppVersion={#AppVersion}
UninstallDisplayName={#AppName}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
OutputDir=..\artifacts\installer
OutputBaseFilename=StreamMultiviewer-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\artifacts\WebView2Bootstrapper.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\WebView2Bootstrapper.exe"; Parameters: "/silent /install"; StatusMsg: "Installing the Microsoft Edge WebView2 Runtime..."; Flags: waituntilterminated
