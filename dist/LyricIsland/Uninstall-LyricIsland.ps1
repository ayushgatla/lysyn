<#
.SYNOPSIS
    Uninstalls LyricIsland from user profile.
#>

$ErrorActionPreference = "SilentlyContinue"

$AppName = "LyricIsland"
$TargetDir = [System.IO.Path]::Combine($env:LocalAppData, "Programs", $AppName)

Write-Host "Stopping LyricIsland..." -ForegroundColor Yellow
Stop-Process -Name "LyricIsland" -Force

# Remove Shortcuts
$StartMenuDir = [System.IO.Path]::Combine($env:APPDATA, "Microsoft", "Windows", "Start Menu", "Programs", "$AppName.lnk")
Remove-Item -Path $StartMenuDir -Force -ErrorAction SilentlyContinue

$DesktopShortcut = [System.IO.Path]::Combine($env:USERPROFILE, "Desktop", "$AppName.lnk")
Remove-Item -Path $DesktopShortcut -Force -ErrorAction SilentlyContinue

# Remove Registry Startup Entry
try {
    $regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    Remove-ItemProperty -Path $regPath -Name "LyricIsland" -ErrorAction SilentlyContinue
} catch {}

# Remove Program Files
if (Test-Path $TargetDir) {
    Remove-Item -Path $TargetDir -Recurse -Force
}

Write-Host "LyricIsland has been removed from your system." -ForegroundColor Green
