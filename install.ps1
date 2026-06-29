# Installs Screenshot Faster to %LOCALAPPDATA%\ScreenshotFaster and creates a
# Start Menu shortcut you can pin to the taskbar.
#
# Usage:  powershell -ExecutionPolicy Bypass -File install.ps1
#         (run build/publish first, or this script will publish for you)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$publishExe = Join-Path $root 'bin\Release\net8.0-windows\win-x64\publish\ScreenshotFaster.exe'

if (-not (Test-Path $publishExe)) {
    Write-Host 'Published exe not found - publishing now...' -ForegroundColor Yellow
    $dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }
    & $dotnet publish (Join-Path $root 'ScreenshotFaster.csproj') -c Release -r win-x64 `
        --self-contained true -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
}

# Install to a stable per-user location.
$installDir = Join-Path $env:LOCALAPPDATA 'ScreenshotFaster'
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
$targetExe = Join-Path $installDir 'ScreenshotFaster.exe'
Copy-Item $publishExe $targetExe -Force
Write-Host "Installed to $targetExe" -ForegroundColor Green

# Create a Start Menu shortcut (searchable + right-click pinnable).
$startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$lnk = Join-Path $startMenu 'Screenshot Faster.lnk'
$ws = New-Object -ComObject WScript.Shell
$sc = $ws.CreateShortcut($lnk)
$sc.TargetPath = $targetExe
$sc.WorkingDirectory = $installDir
$sc.IconLocation = "$targetExe,0"
$sc.Description = 'Fast region screenshot and screen recording'
$sc.Save()
Write-Host "Start Menu shortcut created: $lnk" -ForegroundColor Green

Write-Host ''
Write-Host 'To pin to the taskbar:' -ForegroundColor Cyan
Write-Host '  1. Press Start and type "Screenshot Faster"'
Write-Host '  2. Right-click the result -> Pin to taskbar'
Write-Host '     (or launch it, then right-click its taskbar icon -> Pin to taskbar)'
Write-Host ''
Write-Host "Config file: $env:APPDATA\ScreenshotFaster\config.json"

# Best-effort: open the Start Menu folder so the shortcut can be dragged to the taskbar.
explorer.exe "/select,`"$lnk`""
