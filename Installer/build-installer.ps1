<#
    build-installer.ps1
    Builds EncoderHub in Release and compiles the Inno Setup installer.

    Usage:
        pwsh ./build-installer.ps1                # build Release + installer
        pwsh ./build-installer.ps1 -SkipBuild     # only compile the installer
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir
$solution  = Join-Path $repoRoot 'EncoderHub.sln'
$issFile   = Join-Path $scriptDir 'EncoderHub.iss'

function Find-First($paths) {
    foreach ($p in $paths) { if ($p -and (Test-Path $p)) { return $p } }
    return $null
}

# --- 1. Build the solution in Release -------------------------------------
if (-not $SkipBuild) {
    $msbuild = Find-First @(
        (Get-Command msbuild -ErrorAction SilentlyContinue).Source,
        'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\MSBuild.exe'
    )
    if (-not $msbuild) { throw 'MSBuild not found. Install Visual Studio or the Build Tools.' }

    Write-Host "==> Building Release with $msbuild" -ForegroundColor Cyan
    & $msbuild $solution -t:Restore,Build -p:Configuration=Release -m -v:minimal -nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }
}

# --- 2. Locate the Inno Setup compiler ------------------------------------
$iscc = Find-First @(
    (Get-Command ISCC -ErrorAction SilentlyContinue).Source,
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
)
if (-not $iscc) {
    throw @'
Inno Setup compiler (ISCC.exe) not found.
Install it once, then re-run this script:
    winget install --id JRSoftware.InnoSetup -e
or download from https://jrsoftware.org/isdl.php
'@
}

# --- 3. Compile the installer ---------------------------------------------
Write-Host "==> Compiling installer with $iscc" -ForegroundColor Cyan
& $iscc $issFile
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed (exit $LASTEXITCODE)." }

$out = Join-Path $scriptDir 'Output'
Write-Host "`nDone. Installer written to: $out" -ForegroundColor Green
Get-ChildItem $out -Filter *.exe | ForEach-Object { Write-Host "  $($_.Name)  ($([math]::Round($_.Length/1MB,2)) MB)" }
