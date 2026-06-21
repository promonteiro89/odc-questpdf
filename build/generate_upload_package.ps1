# Build and package the ODC External Library for upload to the ODC Portal.
# Produces ExternalLibrary_<tfm>.zip at the repo root.
#
# Usage:  .\build\generate_upload_package.ps1 [-Tfm net8.0]
#
param(
    [string]$Tfm = "net8.0"
)

$ErrorActionPreference = "Stop"
$Rid = "linux-x64"

$Root    = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root "src/OutSystems.QuestPdf/OutSystems.QuestPdf.csproj"
$PubDir  = Join-Path $Root "src/OutSystems.QuestPdf/bin/Release/$Tfm/$Rid/publish"
$Out     = Join-Path $Root "ExternalLibrary_$Tfm.zip"

Write-Host "==> Publishing ($Tfm / $Rid, framework-dependent)"
dotnet publish $Project -c Release -r $Rid --self-contained false -f $Tfm

Write-Host "==> Checking the native rendering engine is in the publish output"
# QuestPDF flattens its native engine to the publish root (libQuestPdfSkia.so,
# libqpdf.so), not under runtimes/<rid>/native.
$native = Get-ChildItem -Path $PubDir -Recurse -ErrorAction SilentlyContinue |
          Where-Object { $_.Name -like "*QuestPdfSkia*" -or $_.Name -like "libSkiaSharp*" }
if (-not $native) {
    Write-Warning "No native rendering library found in the publish output. The library would throw DllNotFoundException at runtime in ODC. Ensure QuestPDF is referenced directly and you published with -r $Rid."
}

Write-Host "==> Zipping publish output -> $Out"
if (Test-Path $Out) { Remove-Item $Out -Force }
Compress-Archive -Path (Join-Path $PubDir "*") -DestinationPath $Out -Force

Write-Host "==> Done: $Out"
Write-Host "    Upload it in the ODC Portal under External Logic."
