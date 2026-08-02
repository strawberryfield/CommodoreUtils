# build-tools.ps1
# Script di automazione per la creazione di pacchetti zip
# dei tool a riga di comando (A2Petscii, Pet2Ascii, Text2Prg, Prg2Data, Img2Prg)
# per win-x64, linux-x64, osx-arm64.

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot

# ── Parametri ──────────────────────────────────────────────────────
$configuration = "Release"
$runtimes      = @("win-x64", "linux-x64", "osx-arm64")
$projects      = @(
    @{ Name = "A2Petscii"; Csproj = "A2Petscii\A2Petscii.csproj" }
    @{ Name = "Pet2Ascii"; Csproj = "Pet2Ascii\Pet2Ascii.csproj" }
    @{ Name = "Text2Prg";  Csproj = "Text2Prg\Text2Prg.csproj" }
    @{ Name = "Prg2Data";  Csproj = "Prg2Data\Prg2Data.csproj" }
    @{ Name = "Img2Prg";   Csproj = "Img2Prg\Img2Prg.csproj" }
)

$publishRoot = Join-Path $rootDir "temp_publish"
$outputDir   = Join-Path $rootDir "publish"
$licenseFile = Join-Path $rootDir "LICENSE"

# ── Pulizia precedente ────────────────────────────────────────────
if (Test-Path $publishRoot) {
    Write-Host "Pulizia cartella temporanea: $publishRoot" -ForegroundColor Yellow
    Remove-Item $publishRoot -Recurse -Force
}
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# ── Publish per ogni runtime ──────────────────────────────────────
foreach ($rid in $runtimes) {
    Write-Host "`n===============================================================" -ForegroundColor Cyan
    Write-Host " Publish per runtime: $rid" -ForegroundColor Cyan
    Write-Host "===============================================================" -ForegroundColor Cyan

    $ridPublishDir = Join-Path $publishRoot $rid

    foreach ($proj in $projects) {
        $projName   = $proj.Name
        $csprojPath = Join-Path $rootDir $proj.Csproj
        $projOutDir = $ridPublishDir

        Write-Host "`n--- $projName ($rid) ---" -ForegroundColor White
        dotnet publish $csprojPath `
            -c $configuration `
            -r $rid `
            -o $projOutDir

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Publish fallito per $projName ($rid)"
        }
    }
    # Rimuovi i file .pdb e .xml (non servono nella distribuzione)
    Get-ChildItem -Path $ridPublishDir -Filter "*.pdb" -Recurse | Remove-Item -Force
    Get-ChildItem -Path $ridPublishDir -Filter "*.xml" -Recurse | Remove-Item -Force

    # ── Copia la licenza nella cartella del runtime ────────────────
    if (Test-Path $licenseFile) {
        Copy-Item $licenseFile -Destination $ridPublishDir
    }
}

# ── Creazione zip ─────────────────────────────────────────────────
Write-Host "`n===============================================================" -ForegroundColor Green
Write-Host " Creazione pacchetti zip" -ForegroundColor Green
Write-Host "===============================================================" -ForegroundColor Green

foreach ($rid in $runtimes) {
    $ridPublishDir = Join-Path $publishRoot $rid
    $zipName = "CommodoreTools-$rid.zip"
    $zipPath = Join-Path $outputDir $zipName

    # Rimuovi eventuale zip precedente
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Write-Host "  Creazione $zipName ..." -ForegroundColor White
    Compress-Archive -Path (Join-Path $ridPublishDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

    $fileInfo = Get-Item $zipPath
    Write-Host "  -> $zipPath ($([math]::Round($fileInfo.Length / 1MB, 2)) MB)" -ForegroundColor Green
}

# ── Pulizia ───────────────────────────────────────────────────────
Write-Host "`nPulizia cartella temporanea..." -ForegroundColor Yellow
Remove-Item $publishRoot -Recurse -Force

# ── Riepilogo ─────────────────────────────────────────────────────
Write-Host "`n========================================================" -ForegroundColor Green
Write-Host " Pacchetti Commodore Tools generati con successo!" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host " Cartella di output: $outputDir" -ForegroundColor Green
foreach ($rid in $runtimes) {
    $zipPath = Join-Path $outputDir "CommodoreTools-$rid.zip"
    if (Test-Path $zipPath) {
        $fileInfo = Get-Item $zipPath
        Write-Host "  - CommodoreTools-$rid.zip ($([math]::Round($fileInfo.Length / 1MB, 2)) MB)" -ForegroundColor Green
    }
}
Write-Host "========================================================" -ForegroundColor Green
