; ============================================================================
;  EncoderHub - Inno Setup installer script
;  Builds a single setup.exe that installs the WPF app + native interop DLLs.
;  Compile with:  ISCC.exe EncoderHub.iss   (or use build-installer.ps1)
; ============================================================================

#define AppName        "EncoderHub"
#define AppVersion      "1.0.0"
#define AppPublisher    "EncoderHub"
#define AppExeName      "EncoderHub.App.exe"
; Payload comes from the App's Release output, one level up from this script.
#define SourceDir       "..\EncoderHub.App\bin\Release"

[Setup]
; A stable, unique GUID for this product (keep constant across versions so
; upgrades replace the previous install instead of installing side-by-side).
AppId={{7F3C1E4A-2B9D-4E6F-9A1C-0D5E8B2F4A61}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=Output
OutputBaseFilename=EncoderHub-{#AppVersion}-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; The native interop DLLs are 64-bit only -> require 64-bit Windows and
; install into the real 64-bit Program Files.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Writing to Program Files requires elevation.
PrivilegesRequired=admin
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Ship every build output except debug symbols, XML docs, and link-time .lib files.
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion; \
    Excludes: "*.pdb,*.xml,*.lib"

[Icons]
Name: "{group}\{#AppName}";              Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}";    Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";        Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; \
    Flags: nowait postinstall skipifsilent

; ============================================================================
;  Preinstall check: require .NET Framework 4.7.2 (Release >= 461808).
; ============================================================================
[Code]
function IsDotNet472OrLater(): Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM,
       'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
    Result := (Release >= 461808);
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  if not IsDotNet472OrLater() then
  begin
    if MsgBox('EncoderHub requires the Microsoft .NET Framework 4.7.2 or later, '
      + 'which does not appear to be installed.' + #13#10#13#10
      + 'Open the Microsoft download page now?',
      mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open',
        'https://dotnet.microsoft.com/download/dotnet-framework/net472',
        '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
    Result := False;  { Abort setup; user installs .NET first. }
  end;
end;
