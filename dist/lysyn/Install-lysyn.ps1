<#
.SYNOPSIS
    Installs lysyn - Windows Dynamic Lyrics Widget to user profile.
#>

$ErrorActionPreference = "Stop"

function Path-Combine {
    $result = $args[0]
    for ($i = 1; $i -lt $args.Count; $i++) {
        $result = [System.IO.Path]::Combine($result, $args[$i])
    }
    return $result
}

$AppName = "lysyn"
$TargetDir = Path-Combine $env:LocalAppData "Programs" $AppName
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Locate source binaries (either in current directory, dist\lysyn, or bin\Release)
$SourceDir = $ScriptDir
if (-not (Test-Path (Path-Combine $SourceDir "lysyn.exe"))) {
    if (Test-Path (Path-Combine $ScriptDir "dist" "lysyn" "lysyn.exe")) {
        $SourceDir = Path-Combine $ScriptDir "dist" "lysyn"
    } elseif (Test-Path (Path-Combine $ScriptDir "bin" "Release" "net9.0-windows10.0.19041.0" "lysyn.exe")) {
        $SourceDir = Path-Combine $ScriptDir "bin" "Release" "net9.0-windows10.0.19041.0"
    }
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "        lysyn - Windows Dynamic Lyrics Widget Setup       " -ForegroundColor Magenta
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# Stop existing processes if running
foreach ($p in @("lysyn", "LyricIsland")) {
    $existing = Get-Process -Name $p -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "Stopping running instance of $p..." -ForegroundColor Yellow
        Stop-Process -Name $p -Force
        Start-Sleep -Seconds 1
    }
}

# Ensure destination directory exists
if (-not (Test-Path $TargetDir)) {
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
}

Write-Host "Copying application files from $SourceDir to $TargetDir..." -ForegroundColor Green
Copy-Item -Path "$SourceDir\*" -Destination $TargetDir -Recurse -Force -Exclude "Install-lysyn.ps1"

# Create Start Menu shortcut
$WshShell = New-Object -ComObject WScript.Shell
$StartMenuDir = Path-Combine $env:APPDATA "Microsoft" "Windows" "Start Menu" "Programs"
$ShortcutPath = Path-Combine $StartMenuDir "$AppName.lnk"
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = Path-Combine $TargetDir "lysyn.exe"
$Shortcut.WorkingDirectory = $TargetDir
$IconPath = Path-Combine $TargetDir "Assets" "logo.ico"
if (Test-Path $IconPath) {
    $Shortcut.IconLocation = "$IconPath,0"
}
$Shortcut.Description = "lysyn - Windows Dynamic Lyrics Widget"
$Shortcut.Save()

Write-Host "Created Start Menu shortcut: $ShortcutPath" -ForegroundColor Green

# Create Desktop shortcut
$DesktopShortcutPath = Path-Combine $env:USERPROFILE "Desktop" "$AppName.lnk"
$DesktopShortcut = $WshShell.CreateShortcut($DesktopShortcutPath)
$DesktopShortcut.TargetPath = Path-Combine $TargetDir "lysyn.exe"
$DesktopShortcut.WorkingDirectory = $TargetDir
if (Test-Path $IconPath) {
    $DesktopShortcut.IconLocation = "$IconPath,0"
}
$DesktopShortcut.Description = "lysyn - Windows Dynamic Lyrics Widget"
$DesktopShortcut.Save()

Write-Host "Created Desktop shortcut: $DesktopShortcutPath" -ForegroundColor Green

Write-Host ""
Write-Host "Installation completed successfully!" -ForegroundColor Cyan
Write-Host "Target directory: $TargetDir" -ForegroundColor Gray
