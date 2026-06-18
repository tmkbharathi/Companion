[Setup]
AppName=tmkbCompanion
AppVersion=1.1.2
DefaultDirName={pf}\tmkbCompanion
DefaultGroupName=tmkbCompanion
OutputBaseFilename=tmkbCompanion-setup
OutputDir=.
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern
AppCopyright=Copyright (c) 2026
SetupIconFile=tmkbCompanion\Resources\Icons\favicon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "tmkbCompanion\\bin\\Release\\net8.0-windows10.0.19041.0\\win-x64\\publish\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\\tmkbCompanion"; Filename: "{app}\\tmkbCompanion.exe"

[Run]
Filename: "{app}\\tmkbCompanion.exe"; Description: "{cm:LaunchProgram,tmkbCompanion}"; Flags: nowait postinstall skipifsilent

