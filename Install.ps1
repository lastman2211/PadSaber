param([string]$GameDir = "")

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$pathFile = Join-Path $here "install_path.txt"

function Test-Game($p) { $p -and (Test-Path -LiteralPath (Join-Path $p "Beat Saber.exe")) }

# Accepts what people paste or browse to: quotes, trailing slashes, the Beat Saber_Data folder, or its parent folder.
function Resolve-Game($raw) {
    if (-not $raw) { return $null }
    $p = $raw.Trim().Trim('"').Trim("'").TrimEnd([char]92, [char]47)
    if (-not $p) { return $null }
    try {
        $parent = Split-Path -Parent $p -ErrorAction Stop
        foreach ($c in @($p, (Join-Path $p "Beat Saber"), $parent)) {
            if ($c -and (Test-Game $c)) { return $c }
        }
    } catch { }   # not a usable path (e.g. a bare drive letter)
    return $null
}

function Find-GameDir {
    $cands = @()
    # Steam: registry path, every library listed in libraryfolders.vdf, and the default location.
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
    foreach ($l in ($libs | Select-Object -Unique)) { $cands += (Join-Path $l "steamapps\common\Beat Saber") }
    # Oculus / Meta Store and the folder remembered from the last install.
    foreach ($d in (Get-PSDrive -PSProvider FileSystem).Root) {
        $cands += (Join-Path $d "Program Files\Oculus\Software\Software\hyperbolic-magnetism-beat-saber")
        $cands += (Join-Path $d "Oculus\Software\Software\hyperbolic-magnetism-beat-saber")
    }
    if (Test-Path $pathFile) { $cands = @((Get-Content $pathFile -Raw).Trim()) + $cands }
    foreach ($c in $cands) { if (Test-Game $c) { return $c } }
    return $null
}

function Browse-Folder {
    try {
        Add-Type -AssemblyName System.Windows.Forms
        $d = New-Object System.Windows.Forms.FolderBrowserDialog
        $d.Description = "Select your Beat Saber folder (the one that contains Beat Saber.exe)"
        $d.ShowNewFolderButton = $false
        if ($d.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { return $d.SelectedPath }
    } catch { Write-Host "Could not open the folder picker; type the path instead." -ForegroundColor Yellow }
    return $null
}

# Ask where the game is. Enter accepts the detected folder; otherwise paste a path or type B to browse.
function Ask-GameDir($found) {
    Write-Host ""
    Write-Host "Where is Beat Saber installed? (the folder that contains Beat Saber.exe)"
    if ($found) { Write-Host "  Detected: $found" -ForegroundColor Cyan }
    while ($true) {
        $prompt = if ($found) { "Press Enter to use the detected folder, paste a different path, or type B to browse" }
                  else { "Paste the folder path, or type B to browse" }
        $in = (Read-Host $prompt).Trim()
        if (-not $in) {
            if ($found) { return $found }
            continue
        }
        $raw = if ($in -ieq "b") { Browse-Folder } else { $in }
        if (-not $raw) { continue }   # picker cancelled
        $g = Resolve-Game $raw
        if ($g) { return $g }
        Write-Host "Beat Saber.exe was not found in '$raw'. Try again." -ForegroundColor Red
    }
}

if ($GameDir) {
    $GameDir = Resolve-Game $GameDir   # scripted use: Install.bat "D:\path	o\Beat Saber" skips the question
    if (-not $GameDir) { Write-Host "Beat Saber.exe was not found in the folder you gave." -ForegroundColor Red; exit 1 }
} else {
    $GameDir = Ask-GameDir (Find-GameDir)
}
$GameDir | Set-Content -Path $pathFile -Encoding ASCII   # remembered for next time and for Launch.bat
Write-Host "Game folder: $GameDir"

if (Get-Process -Name "Beat Saber" -ErrorAction SilentlyContinue) {
    Write-Host "Close Beat Saber first, then run the installer again." -ForegroundColor Red
    exit 1
}

# One build of the mod per supported game version: dist\<version>\NonVRMod.dll. Pick the one matching this game.
$supported = @(Get-ChildItem (Join-Path $here "dist") -Directory -ErrorAction SilentlyContinue |
    Where-Object { Test-Path (Join-Path $_.FullName "NonVRMod.dll") } | ForEach-Object { $_.Name } | Sort-Object)
. (Join-Path $here "GameVersion.ps1")
$ver = Get-GameVersion $GameDir
if (-not $ver) {
    Write-Host "Could not read the game version from $GameDir." -ForegroundColor Red
    exit 1
}
if ($supported -notcontains $ver) {
    Write-Host "Beat Saber $ver is not supported by this mod." -ForegroundColor Red
    Write-Host "Supported versions: $($supported -join ', ')"
    Write-Host "Use one of those game versions (Steam: right-click Beat Saber > Properties > Betas) and run the installer again."
    exit 1
}
Write-Host "Beat Saber ${ver}: supported."
$modDll = Join-Path $here "dist\$ver\NonVRMod.dll"

# BSIPA records the game version it last ran against in BeatSaberVersion.txt. If the game was updated since, BSIPA moves
# everything in Plugins to "Old <version> Plugins" on the next start, which would remove the mod we are about to install.
if ((Test-Path (Join-Path $GameDir "winhttp.dll")) -and (Test-Path (Join-Path $GameDir "IPA"))) {
    $txt = Join-Path $GameDir "BeatSaberVersion.txt"
    $seen = if (Test-Path $txt) { [regex]::Match((Get-Content $txt -Raw), '^\s*(\d+\.\d+\.\d+)').Groups[1].Value } else { "" }
    if ($seen -and $seen -ne $ver) {
        Write-Host ""
        Write-Host "The game was updated from $seen to $ver since the mod loader last ran." -ForegroundColor Yellow
        Write-Host "Start Beat Saber once, wait for the menu, close it, then run this installer again." -ForegroundColor Yellow
        Write-Host "(Otherwise the mod loader moves the mod out of the Plugins folder on that first start.)"
        exit 1
    }
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
Copy-Item $modDll $plugins -Force
Write-Host ""
Write-Host "Done. Start the game with Launch.bat (or from Steam / your launcher as usual)." -ForegroundColor Green
