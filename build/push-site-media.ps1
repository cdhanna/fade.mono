# Make the website's media, bundle it, upload the bundle to R2, and pin it in site/media.lock: the same
# way build/push-assets.ps1 does the game's assets, so the site workflow (.github/workflows/site.yml) can
# build the site without the renders, which only exist on the machine that made them.
#
#   .\build\push-site-media.ps1
#
# site/scripts/media.mjs makes the media first (with ffmpeg), from the store art and screenshots
# (capsules/, build/render-capsules.ps1 and render-screenshots.ps1), the trailer (trailer-clips/trailer_cut.mp4,
# build/trailer_cut.py), the description's clips (capsules/clips/, build/render-clips.ps1), the shorts (shorts/) and
# the font and fish in Fade.MonoGame/Assets. The bundle is what it made, ready to build with: site/public/media,
# site/public/fonts and site/src/lib/media.json. So CI needs no ffmpeg and encodes nothing. Run this
# after rendering any of them again, and commit site/media.lock.
#
# The CI side is build/fetch-site-media.sh. Locally there is nothing to fetch: the media is here.
#
# Needs: node (for the media and for npx wrangler), ffmpeg, and `npx wrangler login` done once.
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

# the game's assets bucket (build/assets-config.sh), under site/
$Bucket   = if ($env:R2_BUCKET) { $env:R2_BUCKET } else { "fish-game-assets" }
$LockPath = "site/media.lock"
$Wrangler = "wrangler@4.126.0"

# npx.cmd, not npx: see build/push-assets.ps1
$Npx = if (Get-Command npx.cmd -ErrorAction SilentlyContinue) { "npx.cmd" } else { "npx" }
function Invoke-Wrangler {
    & $Npx --yes $Wrangler @args
    if ($LASTEXITCODE -ne 0) { throw "wrangler failed (exit $LASTEXITCODE): $args" }
}
function Write-Lock($ver, $sha, $key) {
    $comment = "Which bundle of the website's media this commit builds against. Written by build/push-site-media.ps1, read by build/fetch-site-media.sh. Named by its own sha256, as build/assets.lock is."
    $text = "{`n  `"_comment`": `"$comment`",`n  `"version`": $ver,`n  `"sha256`": `"$sha`",`n  `"key`": `"$key`"`n}`n"
    [IO.File]::WriteAllText((Join-Path (Get-Location) $LockPath), $text, (New-Object Text.UTF8Encoding $false))
}

# the media, up to date with the renders
& node site/scripts/media.mjs
if ($LASTEXITCODE -ne 0) { throw "site/scripts/media.mjs failed" }

$root = (Get-Location).Path
$files = @(Get-ChildItem "site/public/media" -Recurse -File) + @(Get-ChildItem "site/public/fonts" -File) + @(Get-Item "site/src/lib/media.json")
$byName = @{}
foreach ($f in $files) { $byName[$f.FullName.Substring($root.Length + 1).Replace('\', '/')] = $f.FullName }
$names = [string[]]$byName.Keys
[Array]::Sort($names, [StringComparer]::Ordinal)

$tmp = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid())
New-Item -ItemType Directory -Path $tmp | Out-Null
try {
    $zip = Join-Path $tmp "site-media.zip"
    # entry by entry, in a fixed order with a fixed time, so the same files give the same hash
    # (build/push-assets.ps1 says why)
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $stamp = New-Object DateTimeOffset 2000, 1, 1, 0, 0, 0, ([TimeSpan]::Zero)
    $stream = [IO.File]::Create($zip)
    $archive = New-Object IO.Compression.ZipArchive $stream, ([IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($name in $names) {
            $entry = $archive.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $stamp
            $out = $entry.Open()
            try {
                $in = [IO.File]::OpenRead($byName[$name])
                try { $in.CopyTo($out) } finally { $in.Dispose() }
            } finally { $out.Dispose() }
        }
    } finally { $archive.Dispose(); $stream.Dispose() }

    $sha = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
    Write-Host ("push-site-media: {0} files, {1:N0} KB" -f $names.Count, ((Get-Item $zip).Length / 1KB))
    Write-Host "                 sha256 $sha"

    $ver = 1
    if (Test-Path $LockPath) {
        $locked = Get-Content $LockPath -Raw | ConvertFrom-Json
        if ($sha -eq $locked.sha256) {
            Write-Host "push-site-media: unchanged, already pinned as v$($locked.version). Nothing to do."
            return
        }
        $ver = [int]$locked.version + 1
    }
    $key = "site/$sha.zip"

    Invoke-Wrangler r2 object put "$Bucket/$key" --file $zip --remote
    # read it back before pinning it, as push-assets does
    $check = Join-Path $tmp "check.zip"
    Invoke-Wrangler r2 object get "$Bucket/$key" --file $check --remote
    if (-not (Test-Path $check) -or (Get-FileHash $check -Algorithm SHA256).Hash.ToLower() -ne $sha) {
        throw "the bundle did not arrive in $Bucket intact. Nothing was pinned."
    }

    Write-Lock $ver $sha $key
    Write-Host "push-site-media: published v$ver -- commit site/media.lock to pin it"
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }
