#ifndef AppVersion
  #error AppVersion must be defined
#endif
#ifndef SourceDir
  #error SourceDir must be defined
#endif
#ifndef OutputDir
  #error OutputDir must be defined
#endif
#ifndef OutputBaseFilename
  #error OutputBaseFilename must be defined
#endif

[Setup]
AppId={{7E219012-52BC-49C1-A2F8-F3444F234D84}
AppName=Al Ikhsan Media (Drone Version)
AppVersion={#AppVersion}
AppPublisher=Al Ikhsan Media
DefaultDirName={localappdata}\Programs\AlIkhsanMedia\DroneVersion
DefaultGroupName=Al Ikhsan Media
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19045
UninstallDisplayIcon={app}\AlIkhsanMedia.Drone.App.exe
SetupLogging=yes
WizardStyle=modern

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Al Ikhsan Media (Drone Version)"; Filename: "{app}\AlIkhsanMedia.Drone.App.exe"

[Run]
Filename: "{app}\AlIkhsanMedia.Drone.App.exe"; Description: "Jalankan Al Ikhsan Media (Drone Version)"; Flags: nowait postinstall skipifsilent
