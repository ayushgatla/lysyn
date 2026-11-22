<#
.SYNOPSIS
    Installs LyricIsland - Windows Dynamic Lyrics Widget to user profile.
#>

$ErrorActionPreference = "Stop"

function Path-Combine {
    $result = $args[0]
    for ($i = 1; $i -lt $args.Count; $i++) {
        $result = [System.IO.Path]::Combine($result, $args[$i])
    }
    return $result
}

$AppName = "LyricIsland"
$TargetDir = Path-Combine $env:LocalAppData "Programs" $AppName
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Locate source binaries (either in current directory, dist\LyricIsland, or bin\Publish)
$SourceDir = $ScriptDir
if (-not (Test-Path (Path-Combine $SourceDir "LyricIsland.exe"))) {
    if (Test-Path (Path-Combine $ScriptDir "dist" "LyricIsland" "LyricIsland.exe")) {
        $SourceDir = Path-Combine $ScriptDir "dist" "LyricIsland"
    } elseif (Test-Path (Path-Combine $ScriptDir "bin" "Publish" "LyricIsland.exe")) {
        $SourceDir = Path-Combine $ScriptDir "bin" "Publish"
    }
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   LyricIsland - Windows Dynamic Lyrics Widget Installer  " -ForegroundColor Magenta
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# Stop existing process if running
$existing = Get-Process -Name "LyricIsland" -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Stopping running instance of LyricIsland..." -ForegroundColor Yellow
    Stop-Process -Name "LyricIsland" -Force
    Start-Sleep -Seconds 1
}

# Ensure destination directory exists
if (-not (Test-Path $TargetDir)) {
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
}

Write-Host "Copying application files from $SourceDir to $TargetDir..." -ForegroundColor Green
Copy-Item -Path "$SourceDir\*" -Destination $TargetDir -Recurse -Force -Exclude "Install-LyricIsland.ps1"

# Create Start Menu shortcut
$WshShell = New-Object -ComObject WScript.Shell
$StartMenuDir = Path-Combine $env:APPDATA "Microsoft" "Windows" "Start Menu" "Programs"
$ShortcutPath = Path-Combine $StartMenuDir "$AppName.lnk"
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = Path-Combine $TargetDir "LyricIsland.exe"
$Shortcut.WorkingDirectory = $TargetDir
$Shortcut.Description = "LyricIsland - Windows Dynamic Lyrics Widget"
$Shortcut.Save()

Write-Host "Created Start Menu shortcut: $ShortcutPath" -ForegroundColor Green

# Create Desktop shortcut
$DesktopShortcutPath = Path-Combine $env:USERPROFILE "Desktop" "$AppName.lnk"
$DesktopShortcut = $WshShell.CreateShortcut($DesktopShortcutPath)
$DesktopShortcut.TargetPath = Path-Combine $TargetDir "LyricIsland.exe"
$DesktopShortcut.WorkingDirectory = $TargetDir
$DesktopShortcut.Description = "LyricIsland - Windows Dynamic Lyrics Widget"
$DesktopShortcut.Save()

Write-Host "Created Desktop shortcut: $DesktopShortcutPath" -ForegroundColor Green

Write-Host ""
Write-Host "Installation completed successfully!" -ForegroundColor Cyan
Write-Host "Target directory: $TargetDir" -ForegroundColor Gray
