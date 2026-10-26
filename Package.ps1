# Developer tool: packs everything a player needs into release\BSnonvrmod-<version>.zip.
# Refuses to pack if a build in dist\ is older than the source (a stale DLL would ship old bugs).
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

$manifest = Get-Content (Join-Path $here "manifest.json") -Raw | ConvertFrom-Json
$version = $manifest.version

$builds = @(Get-ChildItem (Join-Path $here "dist") -Directory | Where-Object { Test-Path (Join-Path $_.FullName "NonVRMod.dll") })
if ($builds.Count -eq 0) { throw "dist\ has no builds. Run Build.ps1 for each supported game version first." }

# Only the code and the manifest decide what ends up in the DLL (build-script plumbing in the .csproj does not).
$newestSource = Get-ChildItem $here -Recurse -File -Include *.cs, manifest.json |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|dist|release)\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
foreach ($b in $builds) {
    $dll = Get-Item (Join-Path $b.FullName "NonVRMod.dll")
    if ($dll.LastWriteTime -lt $newestSource.LastWriteTime) {
        throw "dist\$($b.Name)\NonVRMod.dll is older than $($newestSource.Name). Rebuild it with Build.ps1 (with game version $($b.Name) installed)."
    }
}

$stage = Join-Path $env:TEMP "bsnonvrmod_pack"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
$root = Join-Path $stage "BSnonvrmod"
New-Item -ItemType Directory -Path $root | Out-Null
foreach ($f in "Install.bat", "Install.ps1", "GameVersion.ps1", "Launch.bat", "README.md", "TUTORIAL.md", "LICENSE") { Copy-Item (Join-Path $here $f) $root }
Copy-Item (Join-Path $here "dist") $root -Recurse

$outDir = Join-Path $here "release"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$zip = Join-Path $outDir "BSnonvrmod-$version.zip"
if (Test-Path $zip) { Remove-Item $zip }
# Compress-Archive in Windows PowerShell 5.1 writes backslash paths, which break some extractors. Write '/' paths instead.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($f in Get-ChildItem $root -Recurse -File) {
        $name = $f.FullName.Substring($stage.Length + 1).Replace('\', '/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $f.FullName, $name, [IO.Compression.CompressionLevel]::Optimal)
    }
} finally { $archive.Dispose() }
Remove-Item $stage -Recurse -Force
Write-Host "Packed $zip" -ForegroundColor Green
Write-Host "Game versions included: $(($builds | ForEach-Object { $_.Name }) -join ', ')"
