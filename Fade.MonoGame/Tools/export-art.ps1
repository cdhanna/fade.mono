# Copies every art asset the fish game loads into an export folder, with a manifest.csv describing each one.
# "Loads" means a "Fish/Textures/..." or "Fish/Fonts/..." path on a line of the .fbasic code that is not a comment,
# so pictures that sit in Assets but are never loaded are left out.
#
#   powershell -File Fade.MonoGame/Tools/export-art.ps1            (writes <repo>/export)
#   powershell -File Fade.MonoGame/Tools/export-art.ps1 -Out D:\x
param([string]$Out)

$ErrorActionPreference = 'Stop'
$gameDir = Split-Path $PSScriptRoot -Parent
$repoDir = Split-Path $gameDir -Parent
$assetDir = Join-Path $gameDir 'Assets'
if (-not $Out) { $Out = Join-Path $repoDir 'export' }

# what to say about a picture, when its folder and name do not say enough
$notes = @{
    'Textures/Anchovy'                     = 'Board fish: anchovy'
    'Textures/Bones'                       = 'Pile of bones left where a fish died'
    'Textures/Ghost'                       = 'Fish ghost, white so it can be tinted to any fish color'
    'Textures/Bottle'                      = 'Board piece: message in a bottle'
    'Textures/Diver'                       = 'Diver board kind (intentionally empty; the diver is drawn from DiverSheet)'
    'Textures/DiverSheet'                  = 'Diver sprite sheet: swimming, swimming with camera, etc.'
    'Textures/DiverFocus'                  = 'Camera viewfinder corners for the diver photo'
    'Textures/DiverPhoto'                  = 'Little photo paper the diver takes'
    'Textures/Angler'                      = 'Anglerfish rules guide body'
    'Textures/AnglerBulb'                  = 'Anglerfish lure bulb'
    'Textures/AnglerGlow'                  = 'Anglerfish lure glow'
    'Textures/AnglerIcon'                  = 'Anglerfish small icon'
    'Textures/Keys'                        = 'Sheet of keyboard key glyphs used as inline text icons'
    'Textures/KeyTab'                      = 'Tab key glyph'
    'Textures/KeyEsc'                      = 'Escape key glyph (pause)'
    'Textures/Pad'                         = 'Sheet of controller button glyphs'
    'Textures/Turbulence_13-256x256'       = 'Noise texture used by shaders'
    'Textures/Turbulence_14-256x256'       = 'Noise texture used by shaders'
    'Textures/Free Smoke Fx  Pixel 04'     = 'Smoke puff animation sheet'
    'Textures/Free Smoke Fx  Pixel 05'     = 'Smoke puff animation sheet'
    'Textures/Free Smoke Fx  Pixel 06'     = 'Smoke puff animation sheet'
    'Textures/Free Smoke Fx  Pixel 07'     = 'Smoke puff animation sheet'
    'Textures/BadgeOath'                   = 'Run modifier badge: Oceanic Oath'
    'Textures/BadgeEverCoral'              = 'Run modifier badge: Ever Coral'
    'Textures/BadgeNight'                  = 'Run modifier badge: Night Dive'
    'Textures/BadgeBeat'                   = 'Run modifier badge: Beat'
    'Textures/badge_coral'                 = 'High score table badge: coral'
    'Textures/badge_tentacle'              = 'High score table badge: tentacle'
    'Textures/badge_mask'                  = 'High score table badge: game ended with the album full'
    'Textures/codec_diver'                 = 'Codec (radio call) portrait: diver'
    'Textures/ending_home'                 = 'Ending picture: goodbye (a house)'
    'Textures/ending_reef'                 = 'Ending picture: reef (a heart)'
    'Textures/ending_ancient'              = 'Ending picture: ancient (an eye)'
    'Textures/ending_album'                = 'Ending picture: album'
    'Textures/Menu/dive_boat'              = 'Surface screen dive boat (night)'
    'Textures/Menu/dive_boat_day'          = 'Surface screen dive boat (day)'
    'Textures/Menu/dive_boat_hunched'      = 'Dive boat with the diver hunched over (night)'
    'Textures/Menu/dive_boat_day_hunched'  = 'Dive boat with the diver hunched over (day)'
    'Textures/Logos/fade_ghost'            = 'Credits logo: Fade'
    'Textures/Logos/monogame'              = 'Credits logo: MonoGame'
    'Textures/Logos/chris_credit'          = 'Credits portrait of Chris (96x96 downscale of chris_placeholder)'
    'Textures/Cards/clock'                 = 'Clock icon marking the cooldown column on cards'
    'Textures/Cards/type_passive'          = 'Card type icon: passive'
    'Textures/Cards/type_move'             = 'Card type icon: move'
    'Textures/Cards/type_instant'          = 'Card type icon: instant'
    'Textures/Cards/question'              = 'Question mark shown on a locked / unknown card'
    'Textures/Cards/radio'                 = 'Codec (radio call) picture: radio'
    'Textures/Cards/crooked_bar'           = 'Codec (radio call) picture: crooked bar'
    'Textures/Cards/night_dive'            = 'Codec (radio call) picture: night dive'
    'Textures/Clownfish'                   = 'Board fish: clownfish (also a Morse cameo)'
    'Textures/Surgeonfish'                 = 'Board fish: surgeonfish / blue tang (also a Morse cameo)'
    'Textures/Quests/Cross'                = 'Quest UI: X that drops a quest'
    'Textures/Quests/Check'                = 'Quest UI: check mark for a finished quest'
    'Textures/Quests/Dot'                  = 'Quest UI: dot'
    'Textures/Quests/Ring'                 = 'Quest UI: ring'
}

function Get-Category($key) {
    switch -Wildcard ($key) {
        'Fonts/*'            { return 'Font' }
        'Textures/Cards/radio'       { return 'Codec' }
        'Textures/Cards/crooked_bar' { return 'Codec' }
        'Textures/Cards/night_dive'  { return 'Codec' }
        'Textures/Cards/*'   { return 'Card' }
        'Textures/Quests/*'  { return 'Quest' }
        'Textures/Cameos/*'  { return 'Cameo' }
        'Textures/Logos/*'   { return 'Credits' }
        'Textures/Menu/*'    { return 'Surface' }
        'Textures/Angler*'   { return 'Angler' }
        'Textures/Diver*'    { return 'Diver' }
        'Textures/Key*'      { return 'Input glyph' }
        'Textures/Pad'       { return 'Input glyph' }
        'Textures/*adge*'    { return 'Badge' }
        'Textures/ending_*'  { return 'Ending' }
        'Textures/codec_*'   { return 'Codec' }
        'Textures/Turbulence*' { return 'Effect' }
        'Textures/Free Smoke*' { return 'Effect' }
        default              { return 'Board' }
    }
}

function Title($s) {
    $words = ($s -replace '^ABILITY_INDEX_(MOVE_|PASSIVE_)?', '').ToLower() -split '_'
    return ($words | ForEach-Object { if ($_) { $_.Substring(0,1).ToUpper() + $_.Substring(1) } }) -join ' '
}

function Get-Description($key, $texId, $comment) {
    if ($notes.ContainsKey($key)) { return $notes[$key] }
    $name = Split-Path $key -Leaf
    switch (Get-Category $key) {
        'Card'  {
            if ($texId -match 'ABILITY_INDEX_\w+') {
                $card = $cards[$matches[0]]
                if ($card) { return "Ability card art: $($card.name.ToUpper()) - $($card.desc)" }
                return "Ability card art: $(Title $matches[0])"
            }
        }
        'Quest' { return "Quest piece / icon: $name" }
        'Cameo' { return "Morse easter-egg cameo: $name" }
        'Board' { return "Board fish: $name" }
    }
    if ($comment) { return $comment }
    return $name
}

# trailing comments on "global TEX_..." lines, as a last resort for descriptions
$globalComments = @{}
foreach ($f in Get-ChildItem $gameDir -Filter *.fbasic) {
    foreach ($line in [IO.File]::ReadAllLines($f.FullName)) {
        if ($line -match '^\s*global\s+(\w+)[^`]*`(.+)$') { $globalComments[$matches[1]] = $matches[2].Trim() }
    }
}

# the name and rules text of each card, from its CARDS(...) = { name$ = ... desc$ = ... } block.
# a desc$ that splices in numbers shows those parts as "#".
$cards = @{}
$abilitySrc = [IO.File]::ReadAllText((Join-Path $gameDir 'fish_routines_ability.fbasic'))
foreach ($m in [regex]::Matches($abilitySrc, '(?s)CARDS\((ABILITY_INDEX_\w+)\)\s*=\s*\{\s*name\$\s*=\s*"([^"]*)"\s*,?\s*desc\$\s*=\s*([^\r\n]*)')) {
    $desc = $m.Groups[3].Value -replace '^[^"]+\+\s*"', '#' -replace '"\s*\+[^"]*\+\s*"', '#' -replace '^"|"\s*,?\s*$', '' -replace '"\s*\+.*$', ' #'
    $cards[$m.Groups[1].Value] = @{ name = $m.Groups[2].Value; desc = $desc.Trim() }
}

# every asset path that the code loads, and where
$uses = [ordered]@{}
foreach ($f in Get-ChildItem $gameDir -Filter *.fbasic | Sort-Object Name) {
    $lines = [IO.File]::ReadAllLines($f.FullName)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        if ($line -match '^\s*(`|#)') { continue }
        foreach ($m in [regex]::Matches($line, '"Fish/((?:Textures|Fonts)/[^"]+)"')) {
            $key = $m.Groups[1].Value
            $texId = ''
            if ($line -match 'id\((.+)\)\s*,\s*"') { $texId = $matches[1] }
            elseif ($line -match '^\s*font\s+(\w+)\s*,') { $texId = $matches[1] }
            if (-not $uses.Contains($key)) { $uses[$key] = @{ ids = [Collections.Generic.List[string]]::new(); at = [Collections.Generic.List[string]]::new() } }
            if ($texId -and -not $uses[$key].ids.Contains($texId)) { $uses[$key].ids.Add($texId) }
            $uses[$key].at.Add("$($f.Name):$($i + 1)")
        }
    }
}

function Get-PngSize($path) {
    $b = [byte[]]::new(24)
    $s = [IO.File]::OpenRead($path); try { [void]$s.Read($b, 0, 24) } finally { $s.Close() }
    # big-endian ints at bytes 16 and 20 of the IHDR. cast first: a byte shifted in PowerShell stays a byte.
    $w = ([int]$b[16] -shl 24) -bor ([int]$b[17] -shl 16) -bor ([int]$b[18] -shl 8) -bor [int]$b[19]
    $h = ([int]$b[20] -shl 24) -bor ([int]$b[21] -shl 16) -bor ([int]$b[22] -shl 8) -bor [int]$b[23]
    return @($w, $h)
}

if (Test-Path $Out) { Remove-Item $Out -Recurse -Force }
New-Item -ItemType Directory $Out | Out-Null

$rows = [Collections.Generic.List[object]]::new()
$missing = @()
function Add-Asset($key, $src, $texIds, $loadedIn, $desc) {
    $rel = $src.Substring($assetDir.Length + 1).Replace('\', '/') -replace '^Fish/', ''
    $dest = Join-Path $Out $rel
    New-Item -ItemType Directory (Split-Path $dest -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $src $dest
    $w = ''; $h = ''
    if ($src -like '*.png') { $w, $h = Get-PngSize $src }
    $rows.Add([pscustomobject]@{
        file        = $rel
        category    = Get-Category $key
        description = $desc
        width       = $w
        height      = $h
        bytes       = (Get-Item -LiteralPath $src).Length
        texture_id  = $texIds
        loaded_in   = $loadedIn
        source_path = 'Fade.MonoGame/Assets/' + $src.Substring($assetDir.Length + 1).Replace('\', '/')
    })
}

foreach ($key in $uses.Keys) {
    $u = $uses[$key]
    $base = Join-Path $assetDir ('Fish/' + $key)
    $src = @('.png', '.spritefont', '') | ForEach-Object { "$base$_" } | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if (-not $src) { $missing += $key; continue }
    $ids = $u.ids -join '; '
    $at = $u.at -join '; '
    $comment = ($u.ids | ForEach-Object { $globalComments[$_] } | Where-Object { $_ } | Select-Object -First 1)
    if ($src -like '*.spritefont') {
        # the spritefont is a recipe; the picture of the letters comes from the .ttf that it names
        $fontName = ([xml](Get-Content -LiteralPath $src -Raw)).XnaContent.Asset.FontName
        Add-Asset $key $src $ids $at "Score font recipe (MonoGame spritefont, uses $fontName)"
        $ttf = Join-Path (Split-Path $src -Parent) "$fontName.ttf"
        if (Test-Path -LiteralPath $ttf) { Add-Asset $key $ttf $ids $at "Score font typeface ($fontName), used by $(Split-Path $src -Leaf)" }
        else { $missing += "$key -> $fontName.ttf" }
    } else {
        Add-Asset $key $src $ids $at (Get-Description $key $ids $comment)
    }
}

$rows | Sort-Object category, file | Export-Csv (Join-Path $Out 'manifest.csv') -NoTypeInformation -Encoding UTF8
Write-Host "exported $($rows.Count) files to $Out"
if ($missing) { Write-Warning ("referenced but not found: " + ($missing -join ', ')) }

# so a picture that should have been exported is easy to spot
$exported = $rows | ForEach-Object { $_.source_path }
$unused = Get-ChildItem (Join-Path $assetDir 'Fish') -Recurse -File -Include *.png, *.ttf, *.spritefont |
    ForEach-Object { 'Fade.MonoGame/Assets/' + $_.FullName.Substring($assetDir.Length + 1).Replace('\', '/') } |
    Where-Object { $exported -notcontains $_ }
if ($unused) { Write-Host "`nin Assets but not loaded by the game, so not exported:"; $unused | ForEach-Object { Write-Host "  $_" } }
