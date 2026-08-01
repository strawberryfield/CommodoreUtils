# build-installer.ps1
# Script di automazione per la build dell'installer MSI per ImageConverter e ImageConverterGUI

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot

Write-Host "=== 1. Generazione License.rtf ===" -ForegroundColor Cyan
pwsh -File (Join-Path $rootDir "ImageConverterSetup\convert_license.ps1")

Write-Host "`n=== 2. Publish ImageConverter (CLI) ===" -ForegroundColor Cyan
dotnet publish (Join-Path $rootDir "ImageConverter\ImageConverter.csproj") -c Release -r win-x64 --no-self-contained -o (Join-Path $rootDir "temp_publish\ImageConverter")

Write-Host "`n=== 3. Publish ImageConverterGUI (GUI) ===" -ForegroundColor Cyan
dotnet publish (Join-Path $rootDir "ImageConverterGUI\ImageConverterGUI.csproj") -c Release -r win-x64 --no-self-contained -o (Join-Path $rootDir "temp_publish\ImageConverterGUI")

Write-Host "`n=== 4. Compilazione Installer WiX (ImageConverterSetup.msi) ===" -ForegroundColor Cyan
dotnet build (Join-Path $rootDir "ImageConverterSetup\ImageConverterSetup.wixproj") -c Release

$msiPath = Join-Path $rootDir "publish\ImageConverterSetup.msi"
if (Test-Path $msiPath) {
    $fileInfo = Get-Item $msiPath
    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host " Installer MSI generato con successo!" -ForegroundColor Green
    Write-Host " PerCORSO: $msiPath" -ForegroundColor Green
    Write-Host " DIMENSIONE: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
} else {
    Write-Error "Impossibile trovare l'installer generato in $msiPath"
}
