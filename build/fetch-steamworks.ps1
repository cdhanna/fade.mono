# Fetch Facepunch.Steamworks into External/, at the commit pinned in build/steamworks.lock.
# Windows twin of fetch-steamworks.sh -- keep them in step.
#
# Fade.MonoGame.Steam references it BY SOURCE, and it is not in git, so nothing that
# references Fade.MonoGame.Steam builds until this has run. It also carries the native Steam
# libraries for macOS and Linux, at the version its interop layer was generated for.
#
#   .\build\fetch-steamworks.ps1
#
# Safe to run again: it moves an existing checkout to the pinned commit. To move to a newer
# Facepunch.Steamworks, change the commit in build/steamworks.lock and run this again.
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

$Repo = "https://github.com/Facepunch/Facepunch.Steamworks.git"
$Dir  = "External/Facepunch.Steamworks"
$Sha  = (Get-Content "build/steamworks.lock" -Raw).Trim()

if (-not (Test-Path "$Dir/.git")) {
    New-Item -ItemType Directory -Force "External" | Out-Null
    git clone --quiet $Repo $Dir
    if ($LASTEXITCODE -ne 0) { throw "git clone failed (exit $LASTEXITCODE)" }
}
git -C $Dir cat-file -e "$Sha^{commit}" 2>$null
if ($LASTEXITCODE -ne 0) {
    git -C $Dir fetch --quiet origin
    if ($LASTEXITCODE -ne 0) { throw "git fetch failed (exit $LASTEXITCODE)" }
}
git -C $Dir checkout --quiet $Sha
if ($LASTEXITCODE -ne 0) { throw "git checkout failed (exit $LASTEXITCODE)" }
Write-Host "Facepunch.Steamworks is at $Sha"
