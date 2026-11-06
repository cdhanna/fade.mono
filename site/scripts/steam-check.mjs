// Checks what goes to Steam against Steam's rules, so a wrong size is found here and not in the upload box:
// the sizes of the art, the logo's transparency, the screenshots, the clips, the trailer, the words and their
// markup, and the SteamPipe files. It reads; it changes nothing. It fails if anything is wrong.
//
//   npm run steam:check

import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { site } from '../src/lib/site.js';
import { STORE_ART, LIBRARY_ART, OTHER_ART } from './steam-assets.mjs';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const capsules = path.join(repo, 'capsules');
let bad = 0;
const fail = (what) => { bad++; console.log(`  FAIL  ${what}`); };
const ok = (what) => console.log(`  ok    ${what}`);
const check = (good, what, why) => (good ? ok(what) : fail(`${what}: ${why}`));

function probe(file) {
  const r = spawnSync('ffprobe', ['-v', 'error', '-select_streams', 'v:0', '-show_entries', 'stream=width,height,pix_fmt:format=duration', '-of', 'json', file], { encoding: 'utf8' });
  if (r.error || r.status !== 0) return null;
  const j = JSON.parse(r.stdout);
  return { w: j.streams?.[0]?.width, h: j.streams?.[0]?.height, fmt: j.streams?.[0]?.pix_fmt, seconds: parseFloat(j.format?.duration) };
}

// the color type of a PNG: 6 has alpha
const pngHasAlpha = (file) => fs.readFileSync(file)[25] === 6;

console.log('Art');
for (const a of [...STORE_ART, ...LIBRARY_ART, ...OTHER_ART]) {
  const file = path.join(capsules, a.file);
  if (!fs.existsSync(file)) { fail(`${a.box}: capsules/${a.file} is missing`); continue; }
  const p = probe(file);
  if (!p) { fail(`${a.box}: could not read ${a.file}`); continue; }
  // a .ico holds several sizes, and ffprobe reports only one
  const size = a.ico ? true : p.w === a.w && p.h === a.h;
  check(size, `${a.box} ${a.w} x ${a.h}`, `it is ${p.w} x ${p.h}`);
  if (a.file.endsWith('.png')) check(pngHasAlpha(file) === !!a.alpha, `${a.box} ${a.alpha ? 'is see-through' : 'is opaque'}`, 'wrong transparency: run build/render-capsules.ps1 -CutOnly');
}

console.log('Screenshots');
const shots = fs.readdirSync(capsules).filter((f) => /^screenshot_.*\.png$/.test(f));
check(shots.length >= 5, `${shots.length} screenshots`, 'Steam wants at least 5');
for (const f of shots) {
  const p = probe(path.join(capsules, f));
  check(p && p.w >= 1920 && p.h >= 1080 && Math.abs(p.w / p.h - 16 / 9) < 0.01, f, `it is ${p?.w} x ${p?.h}; Steam wants 16:9, at least 1920 x 1080`);
}

console.log('Clips');
for (const s of site.sections.filter((s) => s.clip)) {
  const file = path.join(capsules, 'clips', `${s.clip}.mp4`);
  if (!fs.existsSync(file)) { fail(`capsules/clips/${s.clip}.mp4 is missing`); continue; }
  const p = probe(file);
  const mb = fs.statSync(file).size / 1e6;
  check(p.seconds <= 12 && mb <= 100, `${s.clip}.mp4 ${p.seconds.toFixed(1)} s, ${mb.toFixed(1)} MB`, 'Steam takes 12 s and 100 MB at most');
}

console.log('Trailer');
const trailer = path.join(repo, 'trailer-clips', 'trailer_cut.mp4');
if (fs.existsSync(trailer)) {
  const p = probe(trailer);
  check(p.w === 1920 && p.h === 1080, `trailer_cut.mp4 ${p.w} x ${p.h}, ${p.seconds.toFixed(1)} s`, 'expected 1080p');
} else fail('trailer-clips/trailer_cut.mp4 is missing');

console.log('Words');
check(site.short.length <= 300, `short description ${site.short.length} / 300`, 'too long');
const steam = path.join(repo, 'steam');
const about = path.join(steam, 'store-about.txt');
if (fs.existsSync(about)) {
  const text = fs.readFileSync(about, 'utf8');
  const tags = [...text.matchAll(/\[(\/?)(h2|b|i|list|img)\]/g)];
  const open = {};
  for (const [, close, tag] of tags) open[tag] = (open[tag] ?? 0) + (close ? -1 : 1);
  check(Object.values(open).every((n) => n === 0), 'the markup in store-about.txt is balanced', JSON.stringify(open));
  const current = fs.readFileSync(path.join(steam, 'store-short.txt'), 'utf8').trim() === site.short;
  check(current, 'steam/store-*.txt match site.js', 'run npm run steam');
} else fail('steam/store-about.txt is missing: run npm run steam');

console.log('SteamPipe');
for (const f of ['app_build.vdf', 'depot_windows.vdf', 'depot_mac.vdf', 'SETUP.md']) check(fs.existsSync(path.join(steam, f)), `steam/${f}`, 'missing');
const appid = path.join(repo, 'Fade.MonoGame', 'steam_appid.txt');
if (fs.existsSync(appid)) check(fs.readFileSync(appid, 'utf8').trim() === site.steamAppId, `app id ${site.steamAppId} matches steam_appid.txt`, 'site.js and steam_appid.txt differ');

console.log(bad ? `\n${bad} problem(s).` : '\nAll good.');
process.exit(bad ? 1 : 0);
