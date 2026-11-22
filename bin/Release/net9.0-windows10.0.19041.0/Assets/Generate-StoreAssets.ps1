Add-Type -AssemblyName System.Drawing

$sourcePath = Join-Path (Get-Location) "Assets\logo.jpg"
$outDir = Join-Path (Get-Location) "Assets\Store"

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

$srcBmp = [System.Drawing.Bitmap]::FromFile($sourcePath)

function Save-ResizedSquare([string]$name, [int]$size) {
    $dest = Join-Path $outDir $name
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.Clear([System.Drawing.Color]::FromArgb(16, 16, 21)) # Dark background #101015
    $g.DrawImage($srcBmp, 0, 0, $size, $size)
    $g.Dispose()
    $bmp.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Generated: $name ($size x $size)" -ForegroundColor Green
}

function Save-Letterbox([string]$name, [int]$w, [int]$h, [int]$logoSize) {
    $dest = Join-Path $outDir $name
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.Clear([System.Drawing.Color]::FromArgb(16, 16, 21)) # Dark background #101015
    $x = ($w - $logoSize) / 2
    $y = ($h - $logoSize) / 2
    $g.DrawImage($srcBmp, $x, $y, $logoSize, $logoSize)
    $g.Dispose()
    $bmp.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Generated: $name ($w x $h)" -ForegroundColor Green
}

Save-ResizedSquare "StoreLogo.png" 50
Save-ResizedSquare "Square44x44Logo.png" 44
Save-ResizedSquare "Square150x150Logo.png" 150
Save-ResizedSquare "Square310x310Logo.png" 310
Save-Letterbox "Wide310x150Logo.png" 310 150 120
Save-Letterbox "SplashScreen.png" 620 300 180

$srcBmp.Dispose()
Write-Host "All Microsoft Store assets generated in Assets\Store!" -ForegroundColor Cyan
