# Records the shots of the fish game's trailer: the tests whose name starts with trailer_.
#
# Every shot is a test in Fade.MonoGame/fish_tests_trailer.fbasic that records itself to a video with
# `record start`. A shot does nothing in an ordinary test run. It only records when the FISH_TRAILER
# environment variable is 1, which this script sets. The videos go to the -Out folder.
#
#   .\build\render-trailer.ps1                       build, then record every shot
#   .\build\render-trailer.ps1 -Shot swap,tail       record only the shots with "swap" or "tail" in the name
#   .\build\render-trailer.ps1 -NoBuild              record them with the game as it was last built
#   .\build\render-trailer.ps1 -Exe <path>           record them with another build of the game
#   .\build\render-trailer.ps1 -Out <folder>         put the videos there (default: trailer-clips at the top of the repo)
#
# ffmpeg has to be on the PATH, or set FADE_FFMPEG to it. The game window opens while the shots record, and
# they record slower than they play. The exit code is the test run's: 0 when every shot recorded.
param(
    [string[]]$Shot,
    [switch]$NoBuild,
    [string]$Exe,
    [string]$Out
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$project = Join-Path $repo 'Fade.MonoGame'

# the names come from the test files themselves
$names = Get-ChildItem $project -Filter 'fish_tests*.fbasic' |
    Select-String -Pattern '^\s*test\s+(trailer_\w+)' |
    ForEach-Object { $_.Matches[0].Groups[1].Value } |
    Sort-Object -Unique
if ($Shot) { $names = $names | Where-Object { $n = $_; @($Shot | Where-Object { $n -like "*$_*" }).Count -gt 0 } }

if (-not $names) {
    Write-Host 'There are no shots to record.'
    exit 0
}

if (-not $Exe) {
    if (-not $NoBuild) {
        dotnet build (Join-Path $project 'Fade.MonoGame.csproj') -c Debug
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    $Exe = Join-Path $project 'bin\Debug\net8.0\Fade.MonoGame.exe'
}
$Exe = (Resolve-Path $Exe).Path

if (-not $Out) { $Out = Join-Path $repo 'trailer-clips' }
New-Item -ItemType Directory -Force $Out | Out-Null
$Out = (Resolve-Path $Out).Path

$filters = @()
foreach ($name in $names) {
    $filters += '--filter-uid'
    $filters += "fade::$name"
}

Write-Host "Recording $(@($names).Count) shot(s) to ${Out}: $($names -join ', ')"

$before = $env:FISH_TRAILER
$beforeOut = $env:FISH_TRAILER_OUT
$beforeClock = $env:FADE_STEPPED_CLOCK
$env:FISH_TRAILER = '1'
$env:FISH_TRAILER_OUT = $Out
# the game's clock moves one frame per frame from the start, so every take starts from the same moment
$env:FADE_STEPPED_CLOCK = '1'
# the sky of the surface would follow the clock of the computer
$beforeSky = $env:FISH_SKY
if (-not $env:FISH_SKY) { $env:FISH_SKY = 'evening' }
# the end title puts the Steam logo in its line when the logo is there (the official white mark)
$beforeSteam = $env:FISH_TRAILER_STEAM_ICON
$env:FISH_TRAILER_STEAM_ICON = if (Test-Path (Join-Path $project 'Assets\Fish\Textures\Logos\steam.png')) { '1' } else { '0' }
Push-Location (Split-Path $Exe)
try {
    & $Exe @filters
    $code = $LASTEXITCODE
}
finally {
    Pop-Location
    $env:FISH_TRAILER = $before
    $env:FISH_TRAILER_OUT = $beforeOut
    $env:FADE_STEPPED_CLOCK = $beforeClock
    $env:FISH_SKY = $beforeSky
    $env:FISH_TRAILER_STEAM_ICON = $beforeSteam
}

Get-ChildItem $Out -Filter 'trailer_*' | Sort-Object Name | Format-Table Name, @{ n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } }, LastWriteTime
exit $code
