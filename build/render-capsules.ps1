# Renders the Steam page's art: the tests whose name starts with capsule_ in
# Fade.MonoGame/fish_tests_capsule.fbasic, each a scene of the game saved as frames, then cut to Steam's
# sizes by build/capsule_cut.py (the layouts are in build/capsules.json).
#
# The scenes are trailer shots that only render when FISH_CAPSULE is 1, which this sets, with
# FISH_RENDER_SCALE=7: the world (570 x 320) is drawn 3990 x 2240, so even the library hero (3840
# wide) is cut from real pixels. The frames go to <Out>/raw, what the tests printed to <Out>/raw/run.log,
# and the finished pictures to <Out>.
#
#   .\build\render-capsules.ps1                     build, render every scene, and cut them
#   .\build\render-capsules.ps1 -Shot logo          only the scenes with "logo" in the name
#   .\build\render-capsules.ps1 -NoBuild            with the game as it was last built
#   .\build\render-capsules.ps1 -Exe <path>         with another build of the game
#   .\build\render-capsules.ps1 -CutOnly            cut again from the frames already rendered
#   .\build\render-capsules.ps1 -NoCut              render only (the draft key art is cut by capsule_drafts.py)
#   .\build\render-capsules.ps1 -Out <folder>       put it all there (default: capsules at the top of the repo)
#
# Python 3 with Pillow cuts them (py -3). The game window opens while the scenes render. The exit code is
# the test run's, or the cutter's.
param(
    [string[]]$Shot,
    [switch]$NoBuild,
    [switch]$CutOnly,
    [switch]$NoCut,
    [string]$Exe,
    [int]$Scale = 7,
    [string]$Out
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$project = Join-Path $repo 'Fade.MonoGame'

if (-not $Out) { $Out = Join-Path $repo 'capsules' }
New-Item -ItemType Directory -Force (Join-Path $Out 'raw') | Out-Null
$Out = (Resolve-Path $Out).Path
$raw = Join-Path $Out 'raw'
$log = Join-Path $raw 'run.log'

if (-not $CutOnly) {
    # the names come from the scenes themselves
    $names = Select-String -Path (Join-Path $project 'fish_tests_capsule.fbasic') -Pattern '^\s*test\s+(capsule_\w+)' |
        ForEach-Object { $_.Matches[0].Groups[1].Value } |
        Sort-Object -Unique
    if ($Shot) { $names = $names | Where-Object { $n = $_; @($Shot | Where-Object { $n -like "*$_*" }).Count -gt 0 } }
    if (-not $names) {
        Write-Host 'There are no scenes to render.'
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

    $filters = @()
    foreach ($name in $names) {
        $filters += '--filter-uid'
        $filters += "fade::$name"
    }
    Write-Host "Rendering $(@($names).Count) scene(s) to ${raw}: $($names -join ', ')"

    $keep = @{}
    foreach ($k in 'FISH_TRAILER', 'FISH_CAPSULE', 'FISH_CAPSULE_OUT', 'FISH_RENDER_SCALE', 'FADE_STEPPED_CLOCK', 'FISH_SKY') {
        $keep[$k] = [Environment]::GetEnvironmentVariable($k)
    }
    $env:FISH_TRAILER = '1'
    $env:FISH_CAPSULE = '1'
    $env:FISH_CAPSULE_OUT = $Out
    $env:FISH_RENDER_SCALE = "$Scale"
    # the game's clock moves one frame per frame from the start, so every render is the same
    $env:FADE_STEPPED_CLOCK = '1'
    # the sky of the surface would follow the clock of the computer (as in render-trailer.ps1)
    if (-not $env:FISH_SKY) { $env:FISH_SKY = 'evening' }
    Push-Location (Split-Path $Exe)
    try {
        # the game writes warnings (strings, content) to stderr; Windows PowerShell would make each one a
        # stopping error under 'Stop', so the run is judged by its exit code alone
        $ErrorActionPreference = 'Continue'
        $lines = & $exe @filters 2>&1 | ForEach-Object { "$_" }
        $code = $LASTEXITCODE
        $lines | Where-Object { $_ -like '`[capsule`]*' -or $_ -match 'succeeded, \d+ failed' } | Write-Host
        # a line replaces the line before it of the same kind and name ("take keyart_ready", "region
        # fish"), so a run of only some scenes keeps what the others printed
        $kept = [ordered]@{}
        $all = @()
        if (Test-Path $log) { $all += Get-Content $log }
        $all += $lines | Where-Object { $_ -like '`[capsule`]*' }
        foreach ($line in $all) {
            $p = $line -split '\s+'
            $kept["$($p[1]) $($p[2])"] = $line
        }
        @($kept.Values) | Set-Content -Encoding utf8 $log
    }
    finally {
        $ErrorActionPreference = 'Stop'
        Pop-Location
        foreach ($k in $keep.Keys) { [Environment]::SetEnvironmentVariable($k, $keep[$k]) }
    }
    if ($code -ne 0) { exit $code }
}

if ($NoCut) { exit 0 }
py -3 (Join-Path $repo 'build\capsule_cut.py') $Out (Join-Path $repo 'build\capsules.json')
exit $LASTEXITCODE
