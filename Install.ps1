param([string]$GameDir = "")

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-GameDir {
    $steam = (Get-ItemProperty "HKCU:\Software\Valve\Steam" -ErrorAction SilentlyContinue).SteamPath
    $libs = @()
    if ($steam) {
        $libs += $steam
        $vdf = Join-Path $steam "steamapps\libraryfolders.vdf"
        if (Test-Path $vdf) {
            foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $libs += ($m.Groups[1].Value -replace '\\\\', '\')
            }
        }
    }
    $libs += "C:\Program Files (x86)\Steam"
    foreach ($l in ($libs | Select-Object -Unique)) {
        $p = Join-Path $l "steamapps\common\Beat Saber"
        if (Test-Path (Join-Path $p "Beat Saber.exe")) { return $p }
    }
    return $null
}

if (-not $GameDir) { $GameDir = Find-GameDir }
if (-not $GameDir -or -not (Test-Path (Join-Path $GameDir "Beat Saber.exe"))) {
    Write-Host "Could not find Beat Saber. Run:  Install.bat `"D:\path\to\Beat Saber`"" -ForegroundColor Red
    exit 1
}
Write-Host "Game folder: $GameDir"

if (Get-Process -Name "Beat Saber" -ErrorAction SilentlyContinue) {
    Write-Host "Close Beat Saber first, then run the installer again." -ForegroundColor Red
    exit 1
}

$verFile = Join-Path $GameDir "BeatSaberVersion.txt"
$ver = if (Test-Path $verFile) { (Get-Content $verFile -Raw).Trim() } else { "" }
if ($ver -and -not $ver.StartsWith("1.40.8")) {
    Write-Host "Warning: this mod targets Beat Saber 1.40.8 but found '$ver'. It may not work." -ForegroundColor Yellow
}

# BSIPA (the mod loader) - installed from its official release if missing.
if (-not (Test-Path (Join-Path $GameDir "winhttp.dll")) -or -not (Test-Path (Join-Path $GameDir "IPA"))) {
    Write-Host "Installing BSIPA (mod loader)..."
    $tmp = Join-Path $env:TEMP "bsnonvr_bsipa"
    if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
    New-Item -ItemType Directory -Path $tmp | Out-Null
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $rel = Invoke-RestMethod "https://api.github.com/repos/bsmg/BeatSaber-IPA-Reloaded/releases/latest" -Headers @{ "User-Agent" = "BSnonvrmod-installer" }
    $asset = $rel.assets | Where-Object { $_.name -like "*net472-x64.zip" } | Select-Object -First 1
    if (-not $asset) { Write-Host "Could not find the BSIPA download." -ForegroundColor Red; exit 1 }
    $zip = Join-Path $tmp "bsipa.zip"
    Invoke-WebRequest $asset.browser_download_url -OutFile $zip -UseBasicParsing
    Expand-Archive $zip -DestinationPath (Join-Path $tmp "x") -Force
    Copy-Item (Join-Path $tmp "x\*") $GameDir -Recurse -Force
    # IPA.exe ends with a "press any key" prompt; feed it a newline so it can exit without a console.
    $nl = Join-Path $tmp "nl.txt"
    "`r`n" | Set-Content $nl
    Start-Process (Join-Path $GameDir "IPA.exe") -WorkingDirectory $GameDir -Wait -NoNewWindow -RedirectStandardInput $nl -RedirectStandardOutput (Join-Path $tmp "ipa.log") -RedirectStandardError (Join-Path $tmp "ipa.err") | Out-Null
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

# The mod itself.
$plugins = Join-Path $GameDir "Plugins"
New-Item -ItemType Directory -Path $plugins -Force | Out-Null
Copy-Item (Join-Path $here "dist\NonVRMod.dll") $plugins -Force
Write-Host ""
Write-Host "Done. Start the game with Launch.bat (or Steam's Play button)." -ForegroundColor Green
