<#
.SYNOPSIS
    Uninstalls lysyn from user profile.
#>

$ErrorActionPreference = "SilentlyContinue"

$AppName = "lysyn"
$TargetDir = Join-Path $env:LocalAppData "Programs\$AppName"

Write-Host "Stopping lysyn..." -ForegroundColor Yellow
Stop-Process -Name "lysyn" -Force
Stop-Process -Name "LyricIsland" -Force
Start-Sleep -Seconds 1

Write-Host "Removing application files from $TargetDir..." -ForegroundColor Yellow
if (Test-Path $TargetDir) {
    Remove-Item -Path $TargetDir -Recurse -Force
}

# Remove Shortcuts
$StartMenuShortcut = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\$AppName.lnk"
if (Test-Path $StartMenuShortcut) {
    Remove-Item -Path $StartMenuShortcut -Force
}

$DesktopShortcut = Join-Path $env:USERPROFILE "Desktop\$AppName.lnk"
if (Test-Path $DesktopShortcut) {
    Remove-Item -Path $DesktopShortcut -Force
}

# Remove Run at Startup registry value
$regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
if (Test-Path $regPath) {
    Remove-ItemProperty -Path $regPath -Name "lysyn" -ErrorAction SilentlyContinue
    Remove-ItemProperty -Path $regPath -Name "LyricIsland" -ErrorAction SilentlyContinue
}

Write-Host "lysyn has been removed from your system." -ForegroundColor Green
