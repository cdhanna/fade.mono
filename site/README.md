# The website

A static site for Fish Fish Pop (Svelte 5 and Vite): the trailer, the description with its clips, the
screenshots, Steam's buy box, and a press kit page (`press/`) with the fact sheet and every clip, short,
screenshot and piece of store art to download, alone or zipped.

The page is a dive. Behind it is the game's own sea: `src/lib/gl/shaders.js` ports `SeabedEffect.fx`,
`SpecksEffect.fx`, `FlockEffect.fx`, `BubbleEffect.fx` and `ScreenEffect.fx` to WebGL2, at the game's own
chunky size. Going down the page, the camera sinks, the ridges come up, and the day turns to night, with the
game's night colours, moon and plankton. The ripples of light are a canvas of their own over the whole page,
as they go over the whole screen in the game, and they fade away while a video with controls plays. The sea
takes no input: the pointer and clicks do nothing to it.

The title is the logo's three words, each a picture of its own with no splash, drifting in the water out of step
with each other. They come from the `capsule_draft_words` test (`render:capsules -- -Shot draft_words -Out
capsules/drafts -NoCut`), keyed out by `py -3 build/capsule_drafts.py capsules/drafts --words capsules` into
`capsules/logo_words/` (also part of `--steam`). Without them the hero shows the whole library logo.

The schools are the game's boids (`stepFish` in `gl/backdrop.js`, from THE SCHOOLS IN THE BACKGROUND in
`fish_headers_school.fbasic`), a long way off: small, hazy and behind every ridge. They keep to the page's
margins, where they can be seen.

Without WebGL2 a still of the sea stands in (`capsules/page_background.png`). With reduced motion the sea
is one picture that only changes with the scroll. On a slow device the water drops its resolution.

## The words

Everything the site says is in `src/lib/site.js`. Its description is also the Steam page's, so edit it there
and run `npm run steam`.

```
npm install
npm run steam              # writes steam/store-*.txt and steam/store-checklist.md from site.js
npm run steam -- --copy    # and puts About This Game on the clipboard
npm run steam:check        # checks every file against Steam's rules; fails if one is wrong
npm run dev                # the media, then a dev server
npm run build              # the media, then dist/
npm run deploy             # build, and deploy to Cloudflare Pages by hand (wrangler login first)
```

`steam/store-checklist.md` says which file goes in which box of Steamworks. Steam's rules are in
`scripts/steam-assets.mjs` and `scripts/steam-check.mjs`.

## Rendering

Each is a script in `build/`, and each has an npm name here. Pass flags after `--`.

| npm run | Script | Makes |
|---|---|---|
| `render:capsules` | `build/render-capsules.ps1` | the Steam art in `capsules/`. `-- -CutOnly` cuts again without the game (layouts: `build/capsules.json`) |
| `render:screenshots` | `build/render-screenshots.ps1` | `capsules/screenshot_*.png` from the trailer's shots (`build/steam_media.json`). `-- -Record` records them first |
| `render:clips` | `build/render-clips.ps1` | `capsules/clips/`, the silent clips for Steam's description and the site. `-- -Record` records them first |
| `render:shorts` | `build/render-shorts.ps1` | `shorts/`, 1080 x 1920 (`build/shorts.json`) |
| `render:trailer` | `build/render-trailer.ps1` | the shots in `trailer-clips/` |
| `cut:trailer` | `build/trailer_cut.py` | `trailer-clips/trailer_cut.mp4` |

Anything that records opens the game's window. A game left open locks the build, so close it first.

## The media

`scripts/media.mjs` makes `public/media`, `public/fonts` and `src/lib/media.json` from all of that, none of
which is in git. It needs ffmpeg, and only redoes what is older than its source, so render again and build
again. Cloudflare Pages serves no file over 25 MiB, so the page plays a copy of the trailer made to fit in
two passes, the zip holds pictures only, and the script fails if any file is over. Steam gets the real cut.

The font is the game's, Zero Cool, under the SIL Open Font License; the body text is Nunito, also OFL, served
from here and not from Google.

## Hosting

`.github/workflows/site.yml` deploys to Cloudflare Pages on a push to main that touches `site/`, and by hand.
CI has no renders, so the media goes through R2 the way the game's assets do, made and ready to build with:

```
.\build\push-site-media.ps1    # after rendering anything again
git add site/media.lock        # and commit it: that push deploys
```

Once:
1. A Cloudflare API token with Account / Cloudflare Pages / Edit, as the `CLOUDFLARE_PAGES_TOKEN` repository
   secret. The workflow fetches with the R2 token CI already has (`CLOUDFLARE_API_TOKEN`,
   `CLOUDFLARE_ACCOUNT_ID`, see `steam/SETUP.md`).
2. Nothing else: the first run makes the Pages project (`fish-fish-pop`, or the `PAGES_PROJECT` variable), at
   https://fish-fish-pop.pages.dev/. A custom domain goes in the Pages project's settings and in `url` in
   `site.js`.
