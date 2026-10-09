# Bundle the bucket-managed asset folders, upload the bundle to R2, and pin it in
# build/assets.lock. Windows twin of push-assets.sh -- keep them in step.
#
#   .\build\push-assets.ps1
#
# Needs: node (for npx wrangler), and `npx wrangler login` done once.
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

# SWAP IN: the name of your Cloudflare R2 bucket. Same value as build/assets-config.sh.
$Bucket     = if ($env:R2_BUCKET) { $env:R2_BUCKET } else { "fish-game-assets" }
$AssetRoot  = "Fade.MonoGame/Assets"
$BucketDirs = @("Fish/Audio", "Fish/Fonts", "Fish/Textures")
$LockPath   = "build/assets.lock"
$Wrangler   = "wrangler@4.126.0"

# npx.cmd, not npx: in PowerShell plain `npx` is Node's npx.ps1, which rebuilds its arguments
# from the TEXT of the calling line. Called from a function, that text is `$Wrangler @args`,
# so wrangler gets no real arguments, prints its help, and exits 0 without doing anything.
$Npx = if (Get-Command npx.cmd -ErrorAction SilentlyContinue) { "npx.cmd" } else { "npx" }
function Invoke-Wrangler {
    & $Npx --yes $Wrangler @args
    if ($LASTEXITCODE -ne 0) { throw "wrangler failed (exit $LASTEXITCODE): $args" }
}
function Write-Lock($ver, $sha, $key) {
    $comment = "Which asset bundle this commit builds against. Written by build/push-assets, read by build/fetch-assets. The bundle is named by its own sha256, so a commit names its assets exactly and identical bundles dedupe. An empty sha256 means nothing has been published yet."
    $text = "{`n  `"_comment`": `"$comment`",`n  `"version`": $ver,`n  `"sha256`": `"$sha`",`n  `"key`": `"$key`"`n}`n"
    # No BOM and LF endings, so the bash scripts read the same file.
    [IO.File]::WriteAllText((Join-Path (Get-Location) $LockPath), $text, (New-Object Text.UTF8Encoding $false))
}

if (-not (Get-Command npx -ErrorAction SilentlyContinue)) { throw "npx not found. Install Node.js (wrangler needs it)." }

$root = (Resolve-Path $AssetRoot).Path
$files = foreach ($d in $BucketDirs) {
    $dir = Join-Path $root $d
    if (-not (Test-Path $dir)) { throw "$AssetRoot/$d does not exist" }
    Get-ChildItem $dir -Recurse -File | Where-Object { $_.Name -ne ".DS_Store" -and $_.Name -ne "Thumbs.db" }
}
$entries = $files | ForEach-Object {
    [pscustomobject]@{ Path = $_.FullName; Name = $_.FullName.Substring($root.Length + 1).Replace('\', '/') }
}
$names = [string[]]($entries | ForEach-Object Name)
[Array]::Sort($names, [StringComparer]::Ordinal)
$byName = @{}; $entries | ForEach-Object { $byName[$_.Name] = $_.Path }

$tmp = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid())
New-Item -ItemType Directory -Path $tmp | Out-Null
try {
    $zip = Join-Path $tmp "assets.zip"

    # Written entry by entry rather than with Compress-Archive, for two reasons. Windows
    # PowerShell's Compress-Archive stores paths with backslashes, which unzip on the Linux build
    # machine treats as an error. And a fixed order and timestamp make the bytes depend only on
    # the files, so pushing unchanged assets again produces the same hash and uploads nothing.
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
    Write-Host ("push-assets: {0} files, {1:N0} KB" -f $names.Count, ((Get-Item $zip).Length / 1KB))
    Write-Host "             sha256 $sha"

    # Nothing to do if the working set already matches the pin -- otherwise a re-run bumps the
    # version and dirties the lock for byte-identical assets.
    $locked = Get-Content $LockPath -Raw | ConvertFrom-Json
    if ($sha -eq $locked.sha256) {
        Write-Host "push-assets: unchanged, already pinned as v$($locked.version). Nothing to do."
        return
    }

    $ver = [int]$locked.version + 1
    $key = "assets/$sha.zip"

    Invoke-Wrangler r2 object put "$Bucket/$key" --file $zip --remote

    # Read it back before pinning it. The lock must never name a bundle the bucket does not
    # hold: every later build would fail, on a machine that may not have the files to push again.
    $check = Join-Path $tmp "check.zip"
    Invoke-Wrangler r2 object get "$Bucket/$key" --file $check --remote
    if (-not (Test-Path $check) -or (Get-FileHash $check -Algorithm SHA256).Hash.ToLower() -ne $sha) {
        throw "the bundle did not arrive in $Bucket intact. Nothing was pinned."
    }

    # The mutable pointer, for `fetch-assets -Latest`. Builds never read this -- they read the lock.
    $latest = Join-Path $tmp "latest.json"
    [IO.File]::WriteAllText($latest, "{`"version`":$ver,`"sha256`":`"$sha`",`"key`":`"$key`"}`n", (New-Object Text.UTF8Encoding $false))
    Invoke-Wrangler r2 object put "$Bucket/assets/latest.json" --file $latest --remote

    Write-Lock $ver $sha $key
    Write-Host "push-assets: published v$ver -- commit build/assets.lock to pin it"
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }
