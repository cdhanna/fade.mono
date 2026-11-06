# Makes the short silent clips for the store's description, the website and the press kit: cuts of the
# trailer's shots, chosen in build/steam_media.json, each at most 12 s (Steam's limit), with a short fade from
# and to black so the loop's seam is soft:
#   capsules/clips/<name>.mp4    H.264: upload these to Steam, and the site plays them
#   capsules/clips/<name>.webm   VP9, for the site
#   capsules/clips/<name>.gif    for places that cannot play a video
#
#   .\build\render-clips.ps1                  from the recordings already in trailer-clips/
#   .\build\render-clips.ps1 -Only coral      only the ones with "coral" in the name
#   .\build\render-clips.ps1 -Record          record the shots again first (the game window opens)
#   .\build\render-clips.ps1 -Record -NoBuild ...with the game as it was last built
param(
    [string[]]$Only,
    [switch]$Record,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$media = Get-Content (Join-Path $PSScriptRoot 'steam_media.json') -Raw | ConvertFrom-Json
$items = @($media.clips)
if ($Only) { $items = $items | Where-Object { $n = $_.name; @($Only | Where-Object { $n -like "*$_*" }).Count -gt 0 } }
if (-not $items) {
    Write-Host 'There are no clips to make.'
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
& py -3 (Join-Path $PSScriptRoot 'steam_media.py') --only $names --no-shots
exit $LASTEXITCODE
