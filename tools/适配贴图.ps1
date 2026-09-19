# adapt_image.ps1 - Convert generated jpg to Elin-ready size
# Usage:
#   cover:   .\adapt_image.ps1 -Src cover.jpg -Out preview.jpg -Mode cover -Size 512
#   texture: .\adapt_image.ps1 -Src item.jpg -Out ut_condenser.png -Mode texture
param(
    [Parameter(Mandatory=$true)][string]$Src,
    [string]$Out,
    [ValidateSet("texture","cover")][string]$Mode = "texture",
    [int]$Size = 32,
    [int]$Threshold = 235   # brightness threshold (texture mode): > this = background
)

Add-Type -AssemblyName System.Drawing

$path = (Resolve-Path $Src).Path
$img = [System.Drawing.Image]::FromFile($path)
$work = $null

if ($Mode -eq "cover") {
    # cover: scale to Size x Size, output jpg
    if (-not $Out) { $Out = [System.IO.Path]::ChangeExtension($path, ".cover.jpg") }
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::White)
    $g.DrawImage($img, 0, 0, $Size, $Size)
    $g.Dispose()
    $bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $bmp.Dispose()
    Write-Output "cover saved: $Out ($Size x $Size)"
}
else {
    # texture: remove white bg -> transparent -> auto crop -> center scale to Size x Size PNG
    if (-not $Out) { $Out = [System.IO.Path]::ChangeExtension($path, ".png") }

    # 1. scale to 256x256 for pixel processing
    $work = New-Object System.Drawing.Bitmap(256, 256)
    $g = [System.Drawing.Graphics]::FromImage($work)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::White)
    $g.DrawImage($img, 0, 0, 256, 256)
    $g.Dispose()

    # 2. remove background: pixels brighter than Threshold become transparent
    $trans = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
    for ($y = 0; $y -lt 256; $y++) {
        for ($x = 0; $x -lt 256; $x++) {
            $p = $work.GetPixel($x, $y)
            if (($p.R -ge $Threshold) -and ($p.G -ge $Threshold) -and ($p.B -ge $Threshold)) {
                $work.SetPixel($x, $y, $trans)
            }
        }
    }

    # 3. find bounding box of the subject (non-transparent pixels)
    $minX = 256; $minY = 256; $maxX = -1; $maxY = -1
    for ($y = 0; $y -lt 256; $y++) {
        for ($x = 0; $x -lt 256; $x++) {
            if ($work.GetPixel($x, $y).A -gt 0) {
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }
    if ($maxX -lt 0) {
        Write-Output "no subject found (whole image is background). lower -Threshold or use a cutout tool first"
        $img.Dispose()
        $work.Dispose()
        return
    }

    $w = $maxX - $minX + 1
    $h = $maxY - $minY + 1

    # 4. crop subject, scale to Size x Size with 1px padding, centered
    $outBmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g2 = [System.Drawing.Graphics]::FromImage($outBmp)
    $g2.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g2.Clear($trans)
    $pad = 1
    $scale = [Math]::Min(($Size - 2 * $pad) / $w, ($Size - 2 * $pad) / $h)
    $dw = [int]($w * $scale)
    $dh = [int]($h * $scale)
    $dx = [int](($Size - $dw) / 2)
    $dy = [int](($Size - $dh) / 2)
    $srcRect = New-Object System.Drawing.Rectangle($minX, $minY, $w, $h)
    $dstRect = New-Object System.Drawing.Rectangle($dx, $dy, $dw, $dh)
    $g2.DrawImage($work, $dstRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $g2.Dispose()
    $outBmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
    $outBmp.Dispose()

    Write-Output "texture saved: $Out ($Size x $Size, transparent PNG)"
}

$img.Dispose()
if ($work) { $work.Dispose() }
