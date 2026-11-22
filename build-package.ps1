$ErrorActionPreference = "Stop"

$env:PATH = "$env:LocalAppData\dotnet;$env:PATH"

if (-not (Test-Path "dist")) {
    New-Item -ItemType Directory -Path "dist" -Force | Out-Null
}

Write-Host "1. Running Test Suite..." -ForegroundColor Cyan
dotnet test Tests\LyricIsland.Tests.csproj --nologo

Write-Host "2. Publishing lysyn (Release)..." -ForegroundColor Cyan
$publishDir = (Join-Path (Get-Location) "dist\lysyn")
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish LyricIsland.csproj -c Release -o $publishDir --nologo

Write-Host "3. Copying Scripts and Metadata..." -ForegroundColor Cyan
Copy-Item "Install-lysyn.ps1" -Destination $publishDir -Force
Copy-Item "Uninstall-lysyn.ps1" -Destination $publishDir -Force
Copy-Item "README.md" -Destination $publishDir -Force -ErrorAction SilentlyContinue

Write-Host "4. Packaging Installer Payload..." -ForegroundColor Cyan
$payloadZip = (Join-Path (Get-Location) "Installer\payload.zip")
if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }
Compress-Archive -Path "$publishDir\*" -DestinationPath $payloadZip -Force

Write-Host "5. Compiling Executable Installer (lysyn-setup.exe)..." -ForegroundColor Cyan
$installerPublishDir = (Join-Path (Get-Location) "dist\installer-tmp")
if (Test-Path $installerPublishDir) { Remove-Item $installerPublishDir -Recurse -Force }
dotnet publish Installer\Installer.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $installerPublishDir --nologo
Move-Item (Join-Path $installerPublishDir "lysyn-setup.exe") "dist\lysyn-setup.exe" -Force
Remove-Item $installerPublishDir -Recurse -Force

Write-Host "6. Creating Portable Zip Archive..." -ForegroundColor Cyan
$zipFile = (Join-Path (Get-Location) "dist\lysyn-v1.0.0-windows-x64.zip")
if (Test-Path $zipFile) { Remove-Item $zipFile -Force }
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipFile -Force

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Build & Packaging complete!" -ForegroundColor Green
Write-Host "  Executable Installer : dist\lysyn-setup.exe" -ForegroundColor Yellow
Write-Host "  Portable Archive     : dist\lysyn-v1.0.0-windows-x64.zip" -ForegroundColor Yellow
Write-Host "  App Output Folder    : dist\lysyn" -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Green
