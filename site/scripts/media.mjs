// Makes the site's media from what the game renders: the store art and screenshots (capsules/,
// build/render-capsules.ps1 and render-screenshots.ps1), the clips (capsules/clips/, render-clips.ps1), the
// shorts (shorts/, render-shorts.ps1), the trailer (trailer-clips/trailer_cut.mp4, render-trailer.ps1 and
// trailer_cut.py), and the game's font and fish. None of it is in git, so neither is what this makes:
// public/media, public/fonts and src/lib/media.json.
//
// Every output is only made again when its source is newer. It needs ffmpeg on the PATH.
//
//   node scripts/media.mjs           what is out of date
//   node scripts/media.mjs --force   all of it

import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { zipSync } from 'fflate';

const site = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repo = path.resolve(site, '..');
const force = process.argv.includes('--force');

const CAPSULES = path.join(repo, 'capsules');
const CLIPS = path.join(CAPSULES, 'clips');
const SHORTS = path.join(repo, 'shorts');
const TRAILER = path.join(repo, 'trailer-clips', 'trailer_cut.mp4');
const ASSETS = path.join(repo, 'Fade.MonoGame', 'Assets', 'Fish');
const FONT = path.join(ASSETS, 'Fonts', 'ZeroCool.ttf');
const ART = path.join(ASSETS, 'Textures', 'Art32');
const MEDIA = path.join(site, 'public', 'media');
const FONTS = path.join(site, 'public', 'fonts');
const MANIFEST = path.join(site, 'src', 'lib', 'media.json');

// what the fish on the backdrop are drawn from, in gl/backdrop.js's order; _b has the black line
const FISH = ['Goldfish', 'Catfish', 'Angelfish', 'Bass', 'Pufferfish', 'Jellyfish', 'Crab'];

// what each of capsule_cut.py's pictures is, in Steam's words
const CAPSULE_LABELS = {
  header_capsule: 'Header capsule',
  small_capsule: 'Small capsule',
  main_capsule: 'Main capsule',
  vertical_capsule: 'Vertical capsule',
  library_capsule: 'Library capsule',
  library_header: 'Library header',
  library_hero: 'Library hero',
  library_logo: 'Logo',
  event_header: 'Event header',
  event_cover: 'Event cover',
  page_background: 'Page background',
  shortcut_icon: 'Icon',
};
const SHOT_TITLES = {
  combo: 'A combo rolls on',
  upgrade: 'Pick a card',
  night: 'Night: nothing pops',
  coral: 'Coral grows with a song',
  quest: 'A favor for a local',
  big_combo: 'A big combo',
  horror: 'Something in the deep',
  angler: 'The angler explains',
};
const CLIP_TITLES = {
  swap_pop: 'Swap and pop',
  cards: 'Pick a card',
  coral: 'Coral grows',
  quests: 'A quest',
  night: 'Night',
  horror: 'The deep',
};

function stale(out, ...srcs) {
  if (force || !fs.existsSync(out)) return true;
  const t = fs.statSync(out).mtimeMs;
  return srcs.some((s) => fs.statSync(s).mtimeMs > t);
}

function ffmpeg(args) {
  const r = spawnSync('ffmpeg', ['-hide_banner', '-loglevel', 'error', '-y', ...args], { stdio: 'inherit' });
  if (r.error) throw new Error(`ffmpeg did not run (${r.error.message}); it must be on the PATH`);
  if (r.status !== 0) throw new Error(`ffmpeg failed: ffmpeg ${args.join(' ')}`);
}

function seconds(file) {
  const r = spawnSync('ffprobe', ['-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', file], { encoding: 'utf8' });
  return parseFloat(r.stdout);
}

// a picture scaled to width w (never up), as a JPEG
function jpeg(src, out, w) {
  if (!stale(out, src)) return;
  ffmpeg(['-i', src, '-vf', `scale='min(${w},iw)':-2:flags=lanczos`, '-q:v', '3', out]);
}

function pngSize(file) {
  const b = fs.readFileSync(file);
  return { w: b.readUInt32BE(16), h: b.readUInt32BE(20) };
}

const bytes = (file) => fs.statSync(file).size;

function need(p, how) {
  if (!fs.existsSync(p)) {
    console.error(`media: ${path.relative(repo, p)} is missing. ${how}`);
    process.exit(1);
  }
}

need(CAPSULES, 'Run build/render-capsules.ps1 and build/render-screenshots.ps1 first.');
need(TRAILER, 'Run build/render-trailer.ps1 and py -3 build/trailer_cut.py first.');
need(FONT, 'Run build/fetch-assets.ps1 first.');
need(ART, 'Run build/fetch-assets.ps1 first.');

for (const d of [MEDIA, path.join(MEDIA, 'shots'), path.join(MEDIA, 'press'), path.join(MEDIA, 'fish'), FONTS, path.dirname(MANIFEST)]) fs.mkdirSync(d, { recursive: true });

// ---- the font ------------------------------------------------------------------------------------
// the game's own, Zero Cool, which is under the SIL Open Font License
const woff2 = path.join(FONTS, 'ZeroCool.woff2');
if (stale(woff2, FONT)) {
  const { default: wawoff2 } = await import('wawoff2');
  fs.writeFileSync(woff2, await wawoff2.compress(fs.readFileSync(FONT)));
  console.log('media: fonts/ZeroCool.woff2');
}

// ---- the fish ------------------------------------------------------------------------------------
const fishList = {};
for (const name of FISH) {
  const lined = path.join(ART, `${name}_b.png`);
  const src = fs.existsSync(lined) ? lined : path.join(ART, `${name}.png`);
  need(src, 'Run build/fetch-assets.ps1 first.');
  const out = path.join(MEDIA, 'fish', `${name}.png`);
  if (stale(out, src)) fs.copyFileSync(src, out);
  fishList[name] = `media/fish/${name}.png`;
}

// ---- the logo's words ------------------------------------------------------------------------------
// each word of the logo by itself, with no splash, for the hero to float (build/capsule_drafts.py --words)
const WORDS = path.join(CAPSULES, 'logo_words');
let logoWords = null;
if (fs.existsSync(path.join(WORDS, 'words.json'))) {
  const whole = JSON.parse(fs.readFileSync(path.join(WORDS, 'words.json'), 'utf8'));
  fs.mkdirSync(path.join(MEDIA, 'logo'), { recursive: true });
  for (const w of whole.words) {
    const out = path.join(MEDIA, 'logo', w.file);
    if (stale(out, path.join(WORDS, w.file))) fs.copyFileSync(path.join(WORDS, w.file), out);
  }
  logoWords = { w: whole.w, h: whole.h, words: whole.words.map(({ file, ...at }) => ({ src: `media/logo/${file}`, ...at })) };
} else {
  console.warn('media: capsules/logo_words is missing, so the hero shows the whole logo. Run py -3 build/capsule_drafts.py capsules/drafts --words capsules.');
}

// ---- the trailer ---------------------------------------------------------------------------------
// The cut is about 200 MB, and Cloudflare Pages serves no file over 25 MiB. So the page plays a copy made to
// fit, in two passes so that it lands under the limit whatever is in the picture.
const trailer = path.join(MEDIA, 'trailer.mp4');
if (stale(trailer, TRAILER)) {
  console.log('media: trailer.mp4 (a few minutes)');
  const AUDIO_K = 128;
  const video = Math.floor(((23.5 * 1024 * 1024 * 8) / seconds(TRAILER)) / 1000 - AUDIO_K);
  const logs = path.join(MEDIA, 'trailer-pass');
  const common = ['-i', TRAILER, '-c:v', 'libx264', '-preset', 'slow', '-b:v', `${video}k`, '-pix_fmt', 'yuv420p', '-passlogfile', logs];
  ffmpeg([...common, '-pass', '1', '-an', '-f', 'mp4', process.platform === 'win32' ? 'NUL' : '/dev/null']);
  ffmpeg([...common, '-pass', '2', '-c:a', 'aac', '-b:a', `${AUDIO_K}k`, '-movflags', '+faststart', trailer]);
  for (const f of fs.readdirSync(MEDIA)) if (f.startsWith('trailer-pass')) fs.rmSync(path.join(MEDIA, f));
}
// the poster: the key art with the name on it
const poster = path.join(MEDIA, 'trailer-poster.jpg');
const mainCapsule = path.join(CAPSULES, 'main_capsule.png');
need(mainCapsule, 'Run build/render-capsules.ps1 first.');
jpeg(mainCapsule, poster, 1232);

// ---- the clips -----------------------------------------------------------------------------------
// the store description's videos (build/steam_media.py), already cut: copied as they are, with a poster
const clips = {};
if (fs.existsSync(CLIPS)) {
  fs.mkdirSync(path.join(MEDIA, 'clips'), { recursive: true });
  for (const f of fs.readdirSync(CLIPS).filter((f) => f.endsWith('.mp4'))) {
    const name = f.replace(/\.mp4$/, '');
    const entry = { title: CLIP_TITLES[name] ?? name };
    const mp4 = path.join(CLIPS, f);
    for (const ext of ['mp4', 'webm']) {
      const src = path.join(CLIPS, `${name}.${ext}`);
      if (!fs.existsSync(src)) continue;
      const out = path.join(MEDIA, 'clips', `${name}.${ext}`);
      if (stale(out, src)) fs.copyFileSync(src, out);
      entry[ext] = `media/clips/${name}.${ext}`;
    }
    const jpg = path.join(MEDIA, 'clips', `${name}.jpg`);
    if (stale(jpg, mp4)) ffmpeg(['-ss', '0.6', '-i', mp4, '-frames:v', '1', '-q:v', '3', jpg]);
    entry.jpg = `media/clips/${name}.jpg`;
    entry.bytes = bytes(mp4);
    clips[name] = entry;
  }
}
if (!Object.keys(clips).length) console.warn('media: no clips (build/render-clips.ps1); the sections show words alone');

// ---- the shorts ----------------------------------------------------------------------------------
const shorts = [];
if (fs.existsSync(SHORTS)) {
  fs.mkdirSync(path.join(MEDIA, 'shorts'), { recursive: true });
  for (const f of fs.readdirSync(SHORTS).filter((f) => f.endsWith('.mp4') && !f.endsWith('_preview.mp4')).sort()) {
    const name = f.replace(/\.mp4$/, '');
    const src = path.join(SHORTS, f);
    const out = path.join(MEDIA, 'shorts', f);
    if (stale(out, src)) fs.copyFileSync(src, out);
    const jpg = path.join(MEDIA, 'shorts', `${name}.jpg`);
    if (stale(jpg, src)) ffmpeg(['-ss', '2', '-i', src, '-frames:v', '1', '-vf', 'scale=360:-2', '-q:v', '3', jpg]);
    shorts.push({ name, title: name.replace(/^\d+_/, '').replaceAll('_', ' '), mp4: `media/shorts/${f}`, jpg: `media/shorts/${name}.jpg`, bytes: bytes(src) });
  }
}

// ---- the screenshots and the capsules ------------------------------------------------------------
const pngs = fs.readdirSync(CAPSULES).filter((f) => f.endsWith('.png')).sort();
const shots = [];
const capsules = [];
for (const f of pngs) {
  const src = path.join(CAPSULES, f);
  const name = f.replace(/\.png$/, '');
  const { w, h } = pngSize(src);
  const press = path.join(MEDIA, 'press', f);
  if (stale(press, src)) fs.copyFileSync(src, press);
  const m = name.match(/^screenshot_(\d+)_(\w+)$/);
  if (m) {
    jpeg(src, path.join(MEDIA, 'shots', `${name}.jpg`), 1920);
    jpeg(src, path.join(MEDIA, 'shots', `${name}-960.jpg`), 960);
    shots.push({
      name,
      title: SHOT_TITLES[m[2]] ?? m[2],
      full: `media/shots/${name}.jpg`,
      thumb: `media/shots/${name}-960.jpg`,
      png: `media/press/${f}`,
      w, h, bytes: bytes(src),
    });
  } else {
    jpeg(src, path.join(MEDIA, 'press', `${name}-preview.jpg`), 720);
    capsules.push({
      name,
      label: CAPSULE_LABELS[name] ?? name,
      png: `media/press/${f}`,
      preview: `media/press/${name}-preview.jpg`,
      w, h, bytes: bytes(src),
    });
  }
}
const order = Object.keys(CAPSULE_LABELS);
capsules.sort((a, b) => order.indexOf(a.name) - order.indexOf(b.name));

// the social card, the tab's icon and the still that stands in for the sea
const social = path.join(MEDIA, 'social.jpg');
jpeg(path.join(CAPSULES, 'header_capsule.png'), social, 920);
const icon = path.join(MEDIA, 'icon.png');
const iconSrc = path.join(CAPSULES, 'shortcut_icon.png');
if (stale(icon, iconSrc)) ffmpeg(['-i', iconSrc, '-vf', 'scale=64:64:flags=neighbor', icon]);
jpeg(path.join(CAPSULES, 'page_background.png'), path.join(MEDIA, 'backdrop.jpg'), 1438);

// ---- every picture at once, for the press --------------------------------------------------------
// Not the trailer: Cloudflare Pages takes no file over 25 MiB. The trailer is its own download.
const zipName = 'fish-fish-pop-press-kit.zip';
const zip = path.join(MEDIA, zipName);
if (stale(zip, ...pngs.map((f) => path.join(CAPSULES, f)))) {
  const files = {};
  for (const f of pngs) {
    const dir = f.startsWith('screenshot_') ? 'screenshots' : 'store-art';
    files[`fish-fish-pop-press-kit/${dir}/${f}`] = [fs.readFileSync(path.join(CAPSULES, f)), { level: 0 }];
  }
  fs.writeFileSync(zip, zipSync(files));
  console.log(`media: ${zipName}`);
}

// Cloudflare Pages refuses a deploy with any file over 25 MiB; say so here, not halfway up
const LIMIT = 25 * 1024 * 1024;
for (const dir of [MEDIA, ...['shots', 'press', 'clips', 'shorts'].map((d) => path.join(MEDIA, d))]) {
  if (!fs.existsSync(dir)) continue;
  for (const f of fs.readdirSync(dir)) {
    const p = path.join(dir, f);
    if (fs.statSync(p).isFile() && fs.statSync(p).size > LIMIT) {
      console.error(`media: ${path.relative(site, p)} is over 25 MiB, which Cloudflare Pages will not serve`);
      process.exit(1);
    }
  }
}

// Every file keeps its name from one render to the next, and a browser that has the old one may go on
// showing it. So each link carries a piece of its file's own hash. (The social card stays bare: the link
// previews are in the HTML.)
function fingerprint(v, key) {
  if (typeof v === 'string' && v.startsWith('media/') && key !== 'social') {
    const sum = createHash('md5').update(fs.readFileSync(path.join(site, 'public', v))).digest('hex');
    return `${v}?v=${sum.slice(0, 10)}`;
  }
  if (Array.isArray(v)) return v.map((x) => fingerprint(x));
  if (v && typeof v === 'object') return Object.fromEntries(Object.entries(v).map(([k, x]) => [k, fingerprint(x, k)]));
  return v;
}

fs.writeFileSync(MANIFEST, JSON.stringify(fingerprint({
  trailer: { src: 'media/trailer.mp4', poster: 'media/trailer-poster.jpg', bytes: bytes(trailer) },
  logo: capsules.find((c) => c.name === 'library_logo')?.png ?? null,
  logoWords,
  clips,
  shorts,
  shots,
  capsules,
  fish: fishList,
  zip: { src: `media/${zipName}`, bytes: bytes(zip) },
  social: 'media/social.jpg',
}), null, 2) + '\n');
console.log(`media: ${shots.length} screenshots, ${capsules.length} capsules, ${Object.keys(clips).length} clips, ${shorts.length} shorts, trailer, press kit`);
