# Generates app.ico (multi-resolution, PNG-compressed entries) for Screenshot Faster.
# Draws a dark rounded tile with a green selection bracket motif and a red record dot.
Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $pad = [Math]::Max(1, [int]($size * 0.06))
    $r = [int]($size * 0.22)
    $rect = New-Object System.Drawing.Rectangle($pad, $pad, ($size - 2*$pad), ($size - 2*$pad))

    # rounded-rect background
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $bg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 26, 29, 33))
    $g.FillPath($bg, $path)

    # green selection brackets
    $penW = [Math]::Max(2, [int]($size * 0.07))
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 61, 220, 132)), $penW
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $m = [int]($size * 0.30)
    $L = [int]($size * 0.16)   # bracket arm length
    # top-left
    $g.DrawLine($pen, $m, $m, ($m + $L), $m)
    $g.DrawLine($pen, $m, $m, $m, ($m + $L))
    # bottom-right
    $br = $size - $m
    $g.DrawLine($pen, $br, $br, ($br - $L), $br)
    $g.DrawLine($pen, $br, $br, $br, ($br - $L))

    # red record dot (center)
    $dotR = [int]($size * 0.13)
    $cx = [int]($size / 2)
    $red = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 230, 48, 48))
    $g.FillEllipse($red, ($cx - $dotR), ($cx - $dotR), (2*$dotR), (2*$dotR))

    $g.Dispose()
    return $bmp
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($s in $sizes) {
    $bmp = New-IconBitmap $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += ,($ms.ToArray())
    $bmp.Dispose()
    $ms.Dispose()
}

$outPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'app.ico'
$fs = [System.IO.File]::Create($outPath)
$bw = New-Object System.IO.BinaryWriter($fs)

# ICONDIR
$bw.Write([UInt16]0)            # reserved
$bw.Write([UInt16]1)            # type = icon
$bw.Write([UInt16]$sizes.Count) # count

# directory entries
$offset = 6 + (16 * $sizes.Count)
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $data = $pngs[$i]
    $bw.Write([Byte]($(if ($s -ge 256) { 0 } else { $s })))  # width
    $bw.Write([Byte]($(if ($s -ge 256) { 0 } else { $s })))  # height
    $bw.Write([Byte]0)     # colors
    $bw.Write([Byte]0)     # reserved
    $bw.Write([UInt16]1)   # planes
    $bw.Write([UInt16]32)  # bpp
    $bw.Write([UInt32]$data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $data.Length
}
# image data
foreach ($data in $pngs) { $bw.Write($data) }

$bw.Flush(); $bw.Close(); $fs.Close()
Write-Output "Wrote $outPath"
