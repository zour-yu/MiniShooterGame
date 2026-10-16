Add-Type -AssemblyName System.Drawing

$outDir = "C:\Users\User\MiniShooterGame\MiniShooterGame\Assets\_Game\Sprites\UI"
if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force }

function Create-CardFrame([string]$filename, [System.Drawing.Color]$neonColor, [System.Drawing.Color]$bgColor) {
    $w = 340; $h = 460
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pad = 12
    $cut = 24
    $tabW = 40
    $tabD = 6

    # Draw cyber card path with chamfered/stepped corners & top/bottom notches
    $left = $pad; $right = $w - $pad; $top = $pad; $bottom = $h - $pad
    $cx = [int]($w / 2)

    # Top-left to top-right
    $path.AddLine($left + $cut, $top, $cx - $tabW, $top)
    $path.AddLine($cx - $tabW, $top + $tabD, $cx + $tabW, $top + $tabD)
    $path.AddLine($cx + $tabW, $top, $right - $cut, $top)
    $path.AddLine($right, $top + $cut, $right, $bottom - $cut)
    $path.AddLine($right - $cut, $bottom, $cx + $tabW, $bottom)
    $path.AddLine($cx + $tabW, $bottom - $tabD, $cx - $tabW, $bottom - $tabD)
    $path.AddLine($cx - $tabW, $bottom, $left + $cut, $bottom)
    $path.AddLine($left, $bottom - $cut, $left, $top + $cut)
    $path.CloseFigure()

    # Fill card background
    $brush = New-Object System.Drawing.SolidBrush($bgColor)
    $g.FillPath($brush, $path)

    # Draw horizontal tech scanlines inside
    $scanPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(18, 255, 255, 255), 1)
    for ($y = $top + 20; $y -lt $bottom - 20; $y += 6) {
        $g.DrawLine($scanPen, $left + 14, $y, $right - 14, $y)
    }

    # Outer Neon Glow pass
    $glowPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(60, $neonColor.R, $neonColor.G, $neonColor.B), 6)
    $g.DrawPath($glowPen, $path)

    # Outer Crisp Neon Line
    $mainPen = New-Object System.Drawing.Pen($neonColor, 2.5)
    $g.DrawPath($mainPen, $path)

    # Inner subtle rim line
    $innerPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $iPad = $pad + 9
    $iCut = $cut - 6
    $iLeft = $iPad; $iRight = $w - $iPad; $iTop = $iPad; $iBottom = $h - $iPad
    $innerPath.AddLine($iLeft + $iCut, $iTop, $iRight - $iCut, $iTop)
    $innerPath.AddLine($iRight, $iTop + $iCut, $iRight, $iBottom - $iCut)
    $innerPath.AddLine($iRight - $iCut, $iBottom, $iLeft + $iCut, $iBottom)
    $innerPath.AddLine($iLeft, $iBottom - $iCut, $iLeft, $iTop + $iCut)
    $innerPath.CloseFigure()

    $innerPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(120, $neonColor.R, $neonColor.G, $neonColor.B), 1.2)
    $g.DrawPath($innerPen, $innerPath)

    $savePath = Join-Path $outDir $filename
    $bmp.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Generated: $filename"
}

function Create-ShoeIcon([string]$filename) {
    $s = 140
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Plate rounded rect
    $pRect = New-Object System.Drawing.Rectangle(12, 12, 116, 116)
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 10, 18, 30))
    $g.FillEllipse($bgBrush, $pRect) # fallback plate
    
    $neonCyan = [System.Drawing.Color]::FromArgb(255, 0, 240, 255)
    $pen = New-Object System.Drawing.Pen($neonCyan, 2.5)
    $g.DrawRectangle($pen, 16, 16, 108, 108)

    # Draw Shoe Body (Cyan)
    $shoeBrush = New-Object System.Drawing.SolidBrush($neonCyan)
    $shoePoints = @(
        (New-Object System.Drawing.Point(46, 75)),
        (New-Object System.Drawing.Point(54, 52)),
        (New-Object System.Drawing.Point(74, 58)),
        (New-Object System.Drawing.Point(92, 72)),
        (New-Object System.Drawing.Point(104, 76)),
        (New-Object System.Drawing.Point(104, 86)),
        (New-Object System.Drawing.Point(46, 86))
    )
    $g.FillPolygon($shoeBrush, $shoePoints)

    # White sole
    $soleBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillRectangle($soleBrush, 44, 84, 62, 7)

    # Speed lines trailing behind heel
    $g.DrawLine($pen, 22, 60, 42, 60)
    $g.DrawLine($pen, 20, 70, 40, 70)
    $g.DrawLine($pen, 24, 80, 42, 80)
    $g.DrawLine($pen, 28, 90, 44, 90)

    $savePath = Join-Path $outDir $filename
    $bmp.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Generated: $filename"
}

function Create-RunnerIcon([string]$filename) {
    $s = 140
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $neonPurple = [System.Drawing.Color]::FromArgb(255, 185, 45, 255)
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 22, 10, 34))
    $g.FillRectangle($bgBrush, 16, 16, 108, 108)
    $pen = New-Object System.Drawing.Pen($neonPurple, 2.5)
    $g.DrawRectangle($pen, 16, 16, 108, 108)

    # Runner Silhouette (Purple & White)
    $bodyBrush = New-Object System.Drawing.SolidBrush($neonPurple)
    # Head
    $g.FillEllipse($bodyBrush, 76, 32, 18, 18)

    # Torso (angled polygon)
    $torsoPts = @(
        (New-Object System.Drawing.Point(74, 50)),
        (New-Object System.Drawing.Point(88, 52)),
        (New-Object System.Drawing.Point(76, 78)),
        (New-Object System.Drawing.Point(62, 74))
    )
    $g.FillPolygon($bodyBrush, $torsoPts)

    # Front bent leg
    $legPen = New-Object System.Drawing.Pen($neonPurple, 6)
    $legPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $legPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($legPen, 72, 74, 90, 84)
    $g.DrawLine($legPen, 90, 84, 96, 102)

    # Back kicking leg
    $g.DrawLine($legPen, 64, 74, 46, 86)
    $g.DrawLine($legPen, 46, 86, 38, 72)

    # Forward arm
    $g.DrawLine($legPen, 82, 54, 98, 62)
    # Back arm
    $g.DrawLine($legPen, 72, 54, 56, 60)

    # Speed motion lines trailing
    $trailPen = New-Object System.Drawing.Pen($neonPurple, 2.5)
    $g.DrawLine($trailPen, 20, 50, 48, 50)
    $g.DrawLine($trailPen, 18, 62, 44, 62)
    $g.DrawLine($trailPen, 22, 74, 42, 74)
    $g.DrawLine($trailPen, 24, 88, 40, 88)

    $savePath = Join-Path $outDir $filename
    $bmp.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Generated: $filename"
}

function Create-BulletIcon([string]$filename) {
    $s = 140
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $neonGold = [System.Drawing.Color]::FromArgb(255, 255, 205, 10)
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 30, 24, 8))
    $g.FillRectangle($bgBrush, 16, 16, 108, 108)
    $pen = New-Object System.Drawing.Pen($neonGold, 2.5)
    $g.DrawRectangle($pen, 16, 16, 108, 108)

    # Bullet Projectile (Gold)
    $bulletBrush = New-Object System.Drawing.SolidBrush($neonGold)
    $bulletPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bulletPath.AddLine(62, 58, 88, 58)
    $bulletPath.AddBezier(88, 58, 108, 64, 108, 76, 88, 82)
    $bulletPath.AddLine(88, 82, 62, 82)
    $bulletPath.CloseFigure()
    $g.FillPath($bulletBrush, $bulletPath)

    # Speed trails streaming left
    $trailPen = New-Object System.Drawing.Pen($neonGold, 2.5)
    $g.DrawLine($trailPen, 20, 60, 54, 60)
    $g.DrawLine($trailPen, 18, 70, 56, 70)
    $g.DrawLine($trailPen, 22, 80, 54, 80)
    $g.DrawLine($trailPen, 30, 52, 50, 52)
    $g.DrawLine($trailPen, 30, 88, 50, 88)

    $savePath = Join-Path $outDir $filename
    $bmp.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Generated: $filename"
}

function Create-CyberFrame([string]$filename, [int]$w, [int]$h, [System.Drawing.Color]$neonColor) {
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 10, 16, 26))
    $g.FillRectangle($bgBrush, 4, 4, $w - 8, $h - 8)

    # Glowing outer border
    $glowPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70, $neonColor.R, $neonColor.G, $neonColor.B), 5)
    $g.DrawRectangle($glowPen, 4, 4, $w - 8, $h - 8)

    $pen = New-Object System.Drawing.Pen($neonColor, 2.5)
    $g.DrawRectangle($pen, 4, 4, $w - 8, $h - 8)

    $savePath = Join-Path $outDir $filename
    $bmp.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Generated: $filename"
}

function Create-HealthFill([string]$filename) {
    $w = 360; $h = 42
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $limeGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, 0)),
        (New-Object System.Drawing.Point(0, $h)),
        [System.Drawing.Color]::FromArgb(255, 110, 245, 65),
        [System.Drawing.Color]::FromArgb(255, 45, 195, 80)
    )
    $g.FillRectangle($limeGrad, 0, 0, $w, $h)

    $savePath = Join-Path $outDir $filename
    $bmp.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Generated: $filename"
}

function Create-Padlock([string]$filename) {
    $s = 64
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $cyan = [System.Drawing.Color]::FromArgb(255, 0, 240, 255)
    $brush = New-Object System.Drawing.SolidBrush($cyan)
    $pen = New-Object System.Drawing.Pen($cyan, 3.5)

    # Shackle
    $g.DrawArc($pen, 20, 10, 24, 24, 180, 180)
    $g.DrawLine($pen, 20, 22, 20, 32)
    $g.DrawLine($pen, 44, 22, 44, 32)

    # Body
    $g.FillRectangle($brush, 14, 28, 36, 26)

    # Keyhole (dark)
    $darkBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 10, 16, 26))
    $g.FillEllipse($darkBrush, 29, 34, 6, 6)
    $g.FillRectangle($darkBrush, 30.5, 38, 3, 7)

    $savePath = Join-Path $outDir $filename
    $bmp.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Generated: $filename"
}

# Run generation
Create-CardFrame "Card_Frame_Cyan.png" ([System.Drawing.Color]::FromArgb(0, 240, 255)) ([System.Drawing.Color]::FromArgb(245, 10, 16, 26))
Create-CardFrame "Card_Frame_Purple.png" ([System.Drawing.Color]::FromArgb(185, 45, 255)) ([System.Drawing.Color]::FromArgb(245, 20, 12, 32))
Create-CardFrame "Card_Frame_Yellow.png" ([System.Drawing.Color]::FromArgb(255, 205, 10)) ([System.Drawing.Color]::FromArgb(245, 26, 20, 8))

Create-ShoeIcon "Icon_Swift_Stride.png"
Create-RunnerIcon "Icon_Flash_Step.png"
Create-BulletIcon "Icon_Rapid_Trigger.png"

Create-CyberFrame "Health_Frame_Cyan.png" 380 52 ([System.Drawing.Color]::FromArgb(0, 240, 255))
Create-HealthFill "Health_Fill_Lime.png"
Create-CyberFrame "Weapon_Frame_Cyan.png" 300 44 ([System.Drawing.Color]::FromArgb(0, 240, 255))
Create-Padlock "Padlock_Cyan.png"

Write-Output "ALL ASSETS GENERATED SUCCESSFULLY!"
