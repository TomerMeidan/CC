# EncoderHub Installer

Builds a single `setup.exe` for the EncoderHub WPF app using [Inno Setup](https://jrsoftware.org/isinfo.php).

## What it produces
`Output/EncoderHub-<version>-Setup.exe` — an installer that:
- Installs to `Program Files\EncoderHub` (64-bit; the native interop DLLs are x64-only).
- Ships the app plus all runtime dependencies (managed + `*_interface_64.dll` native libs).
- Creates Start Menu shortcuts and an optional desktop icon.
- Registers a proper uninstaller in *Apps & features*.
- Blocks install with a prompt if .NET Framework 4.7.2+ is missing.

## Prerequisites (one-time)
- Visual Studio / MSBuild (to build the app in Release).
- Inno Setup 6:
  ```
  winget install --id JRSoftware.InnoSetup -e
  ```

## Build
From this folder:
```
pwsh ./build-installer.ps1
```
This builds the solution in Release and compiles the installer. Use `-SkipBuild`
to only recompile the installer from existing Release binaries.

To compile the script directly:
```
ISCC.exe EncoderHub.iss
```

## Versioning
Bump `AppVersion` at the top of [`EncoderHub.iss`](EncoderHub.iss). Keep `AppId`
unchanged so upgrades replace the previous install rather than installing side-by-side.
