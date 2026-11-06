# Makes the Steam page's screenshots: frames of the trailer's shots, chosen in build/steam_media.json, saved as
# capsules/screenshot_<n>_<name>.png, 1920 x 1080. The trailer records near-lossless, so a frame of it is as
# good as a screenshot, and it has the water's wobble on it, as the game looks.
#
#   .\build\render-screenshots.ps1                  from the recordings already in trailer-clips/
#   .\build\render-screenshots.ps1 -Only coral      only the ones with "coral" in the name
#   .\build\render-screenshots.ps1 -Record          record the shots again first (the game window opens)
#   .\build\render-screenshots.ps1 -Record -NoBuild ...with the game as it was last built
#
# To pick another frame: py -3 build/steam_media.py --sheet trailer_night_held --every 30, then change
# the frame in steam_media.json.
param(
    [string[]]$Only,
    [switch]$Record,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$media = Get-Content (Join-Path $PSScriptRoot 'steam_media.json') -Raw | ConvertFrom-Json
$items = @($media.screenshots)
if ($Only) { $items = $items | Where-Object { $n = $_.name; @($Only | Where-Object { $n -like "*$_*" }).Count -gt 0 } }
if (-not $items) {
    Write-Host 'There are no screenshots to make.'
    exit 0
}

if ($Record) {
    $shots = @($items | ForEach-Object { $_.clip -replace '^trailer_', '' } | Sort-Object -Unique)
    $pass = @{ Shot = $shots }
    if ($NoBuild) { $pass.NoBuild = $true }
    & (Join-Path $PSScriptRoot 'render-trailer.ps1') @pass
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$names = ($items | ForEach-Object { $_.name }) -join ','
& py -3 (Join-Path $PSScriptRoot 'steam_media.py') --only $names --no-clips
exit $LASTEXITCODE
