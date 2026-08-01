$licensePath = Join-Path $PSScriptRoot "..\LICENSE"
$outputPath = Join-Path $PSScriptRoot "License.rtf"

if (Test-Path $licensePath) {
    $text = Get-Content -Raw -Path $licensePath
    # Escape special RTF characters
    $text = $text.Replace("\", "\\").Replace("{", "\{").Replace("}", "\}")
    # Replace line breaks with \par
    $text = $text -replace "\r?\n", "\par`r`n"
    
    $rtf = "{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Consolas;}}`r`n\f0\fs18 " + $text + "`r`n}"
    Set-Content -Path $outputPath -Value $rtf -Encoding Ascii
    Write-Host "Generated License.rtf successfully."
} else {
    Write-Error "LICENSE file not found at $licensePath"
}
