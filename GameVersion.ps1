# Dot-sourced by Install.ps1 and Build.ps1.
# Returns the game's version as "major.minor.patch" (e.g. 1.45.3), or "" if it cannot be read.
# The version comes from Unity's own globalgamemanagers file, which always matches the installed game.
# BeatSaberVersion.txt is only a fallback, because Steam updates do not always refresh it.
function Get-GameVersion([string]$GameDir) {
    $ggm = Join-Path $GameDir "Beat Saber_Data\globalgamemanagers"
    if (Test-Path -LiteralPath $ggm) {
        $text = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($ggm))
        $m = [regex]::Match($text, '(\d+\.\d+\.\d+)_\d{3,}')
        if ($m.Success) { return $m.Groups[1].Value }
    }
    $txt = Join-Path $GameDir "BeatSaberVersion.txt"
    if (Test-Path -LiteralPath $txt) {
        $m = [regex]::Match((Get-Content -LiteralPath $txt -Raw), '^\s*(\d+\.\d+\.\d+)')
        if ($m.Success) { return $m.Groups[1].Value }
    }
    return ""
}
