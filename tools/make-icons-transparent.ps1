Add-Type -AssemblyName System.Drawing

function Convert-Icon([string]$path) {
    $bmp = [System.Drawing.Bitmap]::FromFile($path)
    $bmp.MakeTransparent([System.Drawing.Color]::FromArgb(0, 0, 0))
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Updated: $path"
}

$root = Split-Path $PSScriptRoot -Parent
Convert-Icon (Join-Path $root "Assets\Textures\Gui\CloseLine.png")
Convert-Icon (Join-Path $root "Assets\Textures\Gui\SaveFill.png")
