# Builds the mod and stores the DLL as dist\<game version>\NonVRMod.dll.
# Each supported game version gets its own folder; Install.ps1 picks the one that matches the player's game.
#   .\Build.ps1                                (default Steam folder)
#   .\Build.ps1 -GameDir "D:\Games\Beat Saber"
#   .\Build.ps1 -FromRefs 1.40.8               (no game needed: uses the files saved in refs\1.40.8\)
# Every build against an installed game also saves the game files it compiled against to refs\<version>\ (not committed),
# so that version can be rebuilt later without installing it again.
param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Beat Saber",
    [string]$FromRefs = ""
)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

. (Join-Path $here "GameVersion.ps1")
$msbuildArgs = @("-c", "Release")
if ($FromRefs) {
    $ver = $FromRefs
    $refs = Join-Path $here "refs\$ver"
    if (-not (Test-Path (Join-Path $refs "Main.dll"))) { throw "No saved game files for $ver in $refs. Build once with that game version installed." }
    $msbuildArgs += "-p:RefsDir=$refs"
} else {
    $ver = Get-GameVersion $GameDir
    if (-not $ver) { throw "Could not read the game version from '$GameDir'. Is that the Beat Saber folder?" }
    $msbuildArgs += "-p:BeatSaberDir=$GameDir"
}
$msbuildArgs += "-p:GameVersion=$ver"

Write-Host "Building for Beat Saber $ver ..."
Push-Location $here
try {
    dotnet build @msbuildArgs
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
    $out = Join-Path $here "dist\$ver"
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    Copy-Item (Join-Path $here "bin\Release\net472\NonVRMod.dll") $out -Force

    if (-not $FromRefs) {
        # Save exactly the game files the project references (names are read from the .csproj).
        $refs = Join-Path $here "refs\$ver"
        New-Item -ItemType Directory -Path $refs -Force | Out-Null
        $managed = Join-Path $GameDir "Beat Saber_Data\Managed"
        foreach ($m in [regex]::Matches((Get-Content (Join-Path $here "NonVRMod.csproj") -Raw), '\\([A-Za-z0-9_.]+\.dll)</HintPath>')) {
            $name = $m.Groups[1].Value
            $src = if ($name -eq "0Harmony.dll") { Join-Path $GameDir "Libs\$name" } else { Join-Path $managed $name }
            if (Test-Path $src) { Copy-Item $src $refs -Force }
        }
        Copy-Item (Join-Path $GameDir "Libs\0Harmony.dll") $refs -Force   # referenced through $(HarmonyDll), so not matched above
        Write-Host "Saved reference files to refs\$ver"
    }

    Remove-Item (Join-Path $here "bin"), (Join-Path $here "obj") -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Done: dist\$ver\NonVRMod.dll" -ForegroundColor Green
} finally { Pop-Location }
