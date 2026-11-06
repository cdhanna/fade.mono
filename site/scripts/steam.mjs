// Writes the Steam page's words from the site's own (src/lib/site.js), so the two say the same thing:
//
//   steam/store-description.txt     all of it, to read
//   steam/store-short.txt           Store Page > Description > Short Description
//   steam/store-about.txt           Store Page > Description > About This Game, in Steam's markup
//   steam/store-requirements.txt    Store Page > System Requirements
//   steam/store-tags.txt            Store Page > Basic Info > Tags
//   steam/store-checklist.md        every box in Steamworks and the file that goes in it
//
// The clips go in where the text has an [img] line. Upload capsules/clips/<clip>.mp4 under Store Page Extra
// Asset Management; Steam gives each its own tag, and if it differs from the one here, keep Steam's.
//
//   npm run steam             write them all
//   npm run steam -- --copy   and put About This Game on the clipboard (Windows)
//   npm run steam:check       check the files against Steam's rules (scripts/steam-check.mjs)

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { site, steamStore } from '../src/lib/site.js';
import { STORE_ART, LIBRARY_ART, OTHER_ART } from './steam-assets.mjs';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const steam = path.join(repo, 'steam');
const clip = (name) => `[img]{STEAM_APP_IMAGE}/extras/${name}.mp4[/img]`;

if (site.short.length > 300) {
  console.error(`steam: the short description is ${site.short.length} characters; Steam takes 300`);
  process.exit(1);
}

// ---- About This Game -----------------------------------------------------------------------------
const lines = [];
for (const s of site.sections) {
  if (s.clip) lines.push(clip(s.clip), '');
  lines.push(`[h2]${s.title}[/h2]`, s.lines.join('\n'), '');
}
lines.push('[list]', ...site.features.map((f) => `[*]${f}`), '[/list]');
const about = lines.join('\n') + '\n';

// ---- System requirements -------------------------------------------------------------------------
const req = (label, r) => [
  label,
  `OS: ${r.os}`,
  `Processor: ${r.processor}`,
  `Memory: ${r.memory}`,
  `Graphics: ${r.graphics}`,
  `Storage: ${r.storage}`,
].join('\n');
const requirements = `${req('WINDOWS (minimum)', site.requirements.windows)}\n\n${req('macOS (minimum)', site.requirements.mac)}\n`;

// ---- The checklist -------------------------------------------------------------------------------
const row = (a) => `| ${a.box} | ${a.w} x ${a.h} | \`capsules/${a.file}\` | ${a.note ?? ''} |`;
const table = (rows) => ['| Box | Size | File | Note |', '|---|---|---|---|', ...rows.map(row)].join('\n');
const checklist = `# Steamworks checklist: ${site.name}

Written by \`npm run steam\` from \`site/src/lib/site.js\`. Do not edit it here. App ${site.steamAppId}, ${steamStore}

\`npm run steam:check\` checks everything below that can be checked from here.

## Store Page > Description

| Box | Paste |
|---|---|
| Short Description (${site.short.length}/300) | \`steam/store-short.txt\` |
| About This Game | \`steam/store-about.txt\` (\`npm run steam -- --copy\`) |

Before pasting, upload each \`capsules/clips/<clip>.mp4\` in **Store Page Extra Asset Management**: at most
12 s and 100 MB. If Steam gives a clip another tag, use Steam's in place of the \`[img]\` line.

## Store Page > Graphical Assets

${table(STORE_ART)}

Screenshots: upload every \`capsules/screenshot_*.png\` (at least 5, 16:9, at least 1920 x 1080). Mark **at least
four as suitable for all ages**, or the game can be left out of parts of Steam. Do not mark the horror shots.
Text on the capsules is the game's title only.

## Store Page > Trailers

\`trailer-clips/trailer_cut.mp4\` (1080p60, H.264). Upload the cut itself, not the site's lighter copy.

## Community and Library Assets

${table(LIBRARY_ART)}

${table(OTHER_ART)}

Hero: no text; keep what matters inside the centered 860 x 380. Logo: transparent PNG, the logotype only.

## Store Page > Basic Info, and the rest

| Box | Value |
|---|---|
| Developer, Publisher | ${site.developer} |
| Release date | ${site.release} |
| Languages (interface) | ${site.languages.join(', ')} (full audio: none) |
| Controller support | Check that it is true on a pad, then set it |
| Tags | \`steam/store-tags.txt\` |
| System Requirements | \`steam/store-requirements.txt\` |

## SteamPipe

The depots and launch options are in \`steam/SETUP.md\`; \`steam/app_build.vdf\`, \`depot_windows.vdf\` and
\`depot_mac.vdf\` are what \`.github/workflows/steam.yml\` uploads with.
`;

// ---- Write them ----------------------------------------------------------------------------------
const description = [
  `==== SHORT DESCRIPTION (${site.short.length} / 300) ====`,
  site.short,
  '',
  '==== ABOUT THIS GAME ====',
  about,
].join('\n');

const write = (name, text) => fs.writeFileSync(path.join(steam, name), text);
write('store-description.txt', description);
write('store-short.txt', site.short + '\n');
write('store-about.txt', about);
write('store-requirements.txt', requirements);
write('store-tags.txt', site.tags.join('\n') + '\n');
write('store-checklist.md', checklist);
console.log('steam: steam/store-description, store-short, store-about, store-requirements, store-tags and store-checklist');

if (process.argv.includes('--copy')) {
  const { spawnSync } = await import('node:child_process');
  const r = spawnSync('clip', { input: about.replace(/\n/g, '\r\n'), shell: true });
  console.log(r.status === 0 ? 'steam: About This Game is on the clipboard' : 'steam: could not copy to the clipboard');
}
