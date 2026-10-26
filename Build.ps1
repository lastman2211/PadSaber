# Builds the mod against a Beat Saber install and stores the DLL as dist\<game version>\NonVRMod.dll.
# Each supported game version gets its own folder; Install.ps1 picks the one that matches the player's game.
#   .\Build.ps1                                (default Steam folder)
#   .\Build.ps1 -GameDir "D:\Games\Beat Saber"
param([string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Beat Saber")
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

$verFile = Join-Path $GameDir "BeatSaberVersion.txt"
if (-not (Test-Path $verFile)) { throw "No BeatSaberVersion.txt in '$GameDir'. Is that the Beat Saber folder?" }
$ver = [regex]::Match((Get-Content $verFile -Raw), '^\d+\.\d+\.\d+').Value
if (-not $ver) { throw "Could not read the game version from $verFile" }

Write-Host "Building for Beat Saber $ver ..."
Push-Location $here
try {
    dotnet build -c Release "-p:BeatSaberDir=$GameDir"
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
    $out = Join-Path $here "dist\$ver"
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    Copy-Item (Join-Path $here "bin\Release\net472\NonVRMod.dll") $out -Force
    Remove-Item (Join-Path $here "bin"), (Join-Path $here "obj") -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Done: dist\$ver\NonVRMod.dll" -ForegroundColor Green
} finally { Pop-Location }
