# Restore the bucket-managed asset folders from R2. Windows twin of fetch-assets.sh -- keep
# them in step.
#
#   .\build\fetch-assets.ps1              # the version this commit is pinned to
#   .\build\fetch-assets.ps1 -Latest      # newest published, and rewrite the lock
#   .\build\fetch-assets.ps1 -Clean       # also delete local files that are not in the bundle
#
# Without -Clean, local files the bundle does not have are LEFT ALONE and listed, because they
# are most likely new art that has not been pushed yet. Files the bundle does have are
# overwritten.
param([switch]$Latest, [switch]$Clean)
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

# SWAP IN: the name of your Cloudflare R2 bucket. Same value as build/assets-config.sh.
$Bucket     = if ($env:R2_BUCKET) { $env:R2_BUCKET } else { "fish-game-assets" }
$AssetRoot  = "Fade.MonoGame/Assets"
$BucketDirs = @("Fish/Audio", "Fish/Fonts", "Fish/Textures")
$LockPath   = "build/assets.lock"
$LatestKey  = "assets/latest.json"
$Wrangler   = "wrangler@4.126.0"

# npx.cmd, not npx: in PowerShell plain `npx` is Node's npx.ps1, which rebuilds its arguments
# from the TEXT of the calling line. Called from a function, that text is `$Wrangler @args`,
# so wrangler gets no real arguments, prints its help, and exits 0 without doing anything.
$Npx = if (Get-Command npx.cmd -ErrorAction SilentlyContinue) { "npx.cmd" } else { "npx" }
function Invoke-Wrangler {
    & $Npx --yes $Wrangler @args
    if ($LASTEXITCODE -ne 0) { throw "wrangler failed (exit $LASTEXITCODE): $args" }
}

if (-not (Get-Command npx -ErrorAction SilentlyContinue)) { throw "npx not found. Install Node.js (wrangler needs it)." }

$tmp = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid())
New-Item -ItemType Directory -Path $tmp | Out-Null
try {
    if ($Latest) {
        Invoke-Wrangler r2 object get "$Bucket/$LatestKey" --file "$tmp/latest.json" --remote
        $meta = Get-Content "$tmp/latest.json" -Raw | ConvertFrom-Json
    } else {
        $meta = Get-Content $LockPath -Raw | ConvertFrom-Json
    }
    if (-not $meta.sha256) {
        throw "$LockPath pins no bundle. Nothing has been published yet: run build\push-assets.ps1 on a machine that has the assets, and commit $LockPath."
    }
    $key = "assets/$($meta.sha256).zip"
    Write-Host "fetch-assets: v$($meta.version) $($meta.sha256)"
    Invoke-Wrangler r2 object get "$Bucket/$key" --file "$tmp/assets.zip" --remote

    $got = (Get-FileHash "$tmp/assets.zip" -Algorithm SHA256).Hash.ToLower()
    if ($got -ne $meta.sha256) {
        throw "HASH MISMATCH`n  wanted $($meta.sha256)`n  got    $got`n  Refusing to unpack. The bundle was replaced, or the download is corrupt."
    }

    if (-not (Test-Path $AssetRoot)) { New-Item -ItemType Directory -Path $AssetRoot | Out-Null }
    $root = (Resolve-Path $AssetRoot).Path
    if ($Clean) {
        foreach ($d in $BucketDirs) {
            $dir = Join-Path $root $d
            if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
        }
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $inBundle = New-Object 'System.Collections.Generic.HashSet[string]'
    $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path "$tmp/assets.zip").Path)
    try {
        foreach ($entry in $archive.Entries) {
            if ($entry.FullName.EndsWith('/')) { continue }
            $rel = $entry.FullName.Replace('\', '/')
            # A bundle is only ever unpacked inside the asset folder.
            if ($rel.StartsWith('/') -or $rel.Split('/') -contains '..') { throw "unsafe path in bundle: $rel" }
            [void]$inBundle.Add($rel)
            $dest = Join-Path $root $rel.Replace('/', '\')
            New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $dest, $true)
        }
    } finally { $archive.Dispose() }
    Write-Host "fetch-assets: restored $($inBundle.Count) files to $AssetRoot"

    # Anything local that the bundle does not have. Left in place; see the header.
    $extra = foreach ($d in $BucketDirs) {
        $dir = Join-Path $root $d
        if (Test-Path $dir) {
            Get-ChildItem $dir -Recurse -File |
                ForEach-Object { $_.FullName.Substring($root.Length + 1).Replace('\', '/') } |
                Where-Object { -not $inBundle.Contains($_) }
        }
    }
    if ($extra) {
        Write-Host "fetch-assets: these local files are NOT in the bundle, and were left alone:"
        $extra | ForEach-Object { Write-Host "    $_" }
        Write-Host "  New art? Run build\push-assets.ps1. Stale? Re-run with -Clean."
    }

    if ($Latest) {
        $comment = "Which asset bundle this commit builds against. Written by build/push-assets, read by build/fetch-assets. The bundle is named by its own sha256, so a commit names its assets exactly and identical bundles dedupe. An empty sha256 means nothing has been published yet."
        $text = "{`n  `"_comment`": `"$comment`",`n  `"version`": $($meta.version),`n  `"sha256`": `"$($meta.sha256)`",`n  `"key`": `"$key`"`n}`n"
        [IO.File]::WriteAllText((Join-Path (Get-Location) $LockPath), $text, (New-Object Text.UTF8Encoding $false))
        Write-Host "fetch-assets: lock updated to v$($meta.version) -- commit it to move the build"
    }
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }
