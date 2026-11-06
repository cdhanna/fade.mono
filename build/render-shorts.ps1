# Makes the shorts: 1080 x 1920 videos for YouTube Shorts and TikTok, cut from the trailer's recordings by
# build/shorts_cut.py (the shorts are in build/shorts.json). They go to shorts/ at the top of the repo.
#
# Nothing has to be recorded to cut them, and the game does not run, so by default this only cuts. -Record
# records the shots they use first, with build/render-trailer.ps1, because a shot that changed in the game
# only shows up in a short once it is recorded again.
#
#   .\build\render-shorts.ps1                        cut every short from the recordings already made
#   .\build\render-shorts.ps1 -Short relax,deep      only the shorts (or shots) with these in the name
#   .\build\render-shorts.ps1 -Preview               quickly, at half the size
#   .\build\render-shorts.ps1 -CutOnly               the same as the default, for when it has to be said
#   .\build\render-shorts.ps1 -Record                build, record the shots they use, then cut them
#   .\build\render-shorts.ps1 -Record -NoBuild       record them with the game as it was last built
#   .\build\render-shorts.ps1 -Record -Exe <path>    record them with another build of the game
#
# ffmpeg and ffprobe have to be on the PATH, and Python 3 (py -3). The game window opens while -Record
# records. The exit code is the recording's, or the cutter's.
param(
    [string[]]$Short,
    [switch]$CutOnly,
    [switch]$Record,
    [switch]$NoBuild,
    [switch]$Preview,
    [string]$Exe
)

$ErrorActionPreference = 'Stop'
$cutter = Join-Path $PSScriptRoot 'shorts_cut.py'
$only = @()
if ($Short) { $only = @('--only') + $Short }

if ($Record -and -not $CutOnly) {
    # the shorts say which tests record their shots, the end card and the transitions with them
    $shots = @(& py -3 $cutter --clips @only)
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    if (-not $shots) {
        Write-Host 'There are no shots to record.'
        exit 0
    }
    $forward = @{ Shot = $shots }
    if ($NoBuild) { $forward.NoBuild = $true }
    if ($Exe) { $forward.Exe = $Exe }
    & (Join-Path $PSScriptRoot 'render-trailer.ps1') @forward
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$options = @($only)
if ($Preview) { $options += '--preview' }
& py -3 $cutter @options
exit $LASTEXITCODE
