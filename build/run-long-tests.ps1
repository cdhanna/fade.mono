# Runs only the long tests of the fish game: the tests whose name starts with long_.
#
# A long test ends at once in an ordinary test run, because it takes minutes. It only does
# its work when the FISH_TESTS_LONG environment variable is 1, which this script sets. See
# THE LONG TESTS in Fade.MonoGame/fish_tests_seed.fbasic.
#
#   .\build\run-long-tests.ps1                 build, then run every long test
#   .\build\run-long-tests.ps1 -NoBuild        run them with the game as it was last built
#   .\build\run-long-tests.ps1 -Exe <path>     run them with another build of the game
#
# The game window opens while they run. The exit code is the test run's: 0 when they all pass.
param(
    [switch]$NoBuild,
    [string]$Exe
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\Fade.MonoGame'

# the names come from the test files themselves
$names = Get-ChildItem $project -Filter 'fish_tests*.fbasic' |
    Select-String -Pattern '^\s*test\s+(long_\w+)' |
    ForEach-Object { $_.Matches[0].Groups[1].Value } |
    Sort-Object -Unique

if (-not $names) {
    Write-Host 'There are no long tests.'
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

Write-Host "Running $(@($names).Count) long test(s): $($names -join ', ')"

$before = $env:FISH_TESTS_LONG
$env:FISH_TESTS_LONG = '1'
Push-Location (Split-Path $Exe)
try {
    & $Exe @filters
    $code = $LASTEXITCODE
}
finally {
    Pop-Location
    $env:FISH_TESTS_LONG = $before
}
exit $code
