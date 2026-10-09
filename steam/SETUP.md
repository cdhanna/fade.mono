# Fish Fish Pop on Steam: setup guide

Everything you need to do once so that **Actions → Steam (Fish Fish Pop) → Run workflow**
builds the game for Windows and macOS and uploads both to Steam.

The code side is done. What is left is accounts, ids and secrets that only you can create.
Budget about an hour, most of it waiting on Steamworks.

## What the workflow does

`.github/workflows/steam.yml`, on one Linux runner:

1. Downloads the textures, audio and fonts from a Cloudflare R2 bucket (`build/fetch-assets.sh`),
   at the exact version pinned in `build/assets.lock`.
2. Publishes `Fade.MonoGame/Fade.MonoGame.csproj` in Release for `win-x64` and `osx-arm64`,
   self-contained, so players need no .NET install. The Release build compiles `Assets/` into
   `Content/*.xnb` and runs the Fade program that was compiled at build time.
3. Wraps the macOS build in `FishFishPop.app` (`build/make-macos-app.sh`).
4. Uploads both to Steam with `steamcmd` (`steam/*.vdf`) and sets the build live on a beta
   branch.

## Everything you have to swap in

| What | Where it goes | Kind |
|---|---|---|
| Steam app id | GitHub variable `STEAM_APP_ID` | required |
| Windows depot id | GitHub variable `STEAM_DEPOT_WINDOWS` | required |
| macOS depot id | GitHub variable `STEAM_DEPOT_MAC` | required |
| Builder account login name | GitHub secret `STEAM_USERNAME` | required |
| Builder account `config.vdf`, base64 | GitHub secret `STEAM_CONFIG_VDF` | required |
| Cloudflare R2 token (read only) | GitHub secret `CLOUDFLARE_API_TOKEN` | required |
| Cloudflare account id | GitHub secret `CLOUDFLARE_ACCOUNT_ID` | required |
| Leaderboard score key, made up by you | GitHub secret `FISH_SCORE_KEY` | required to deploy (see "Leaderboards") |
| R2 bucket name | `build/assets-config.sh`, `build/push-assets.ps1`, `build/fetch-assets.ps1` (all three say `fish-game-assets`) | only if you pick another name |
| macOS bundle id | `BUNDLE_ID` in `build/make-macos-app.sh` (`com.fishfishpop.game`) | optional |

Nothing in `steam/*.vdf` needs editing. The ids are filled in from the GitHub variables when
the workflow runs.

In Steamworks, the launch options must name these two files:

| OS | Executable |
|---|---|
| Windows | `Fade.MonoGame.exe` |
| macOS | `FishFishPop.app` |

---

## 1. Cloudflare bucket, and moving the assets out of git

`Fade.MonoGame/Assets/Fish/Audio`, `Fonts` and `Textures` are now ignored by git. They travel
as one zip in an R2 bucket, named by its own sha256, and `build/assets.lock` records which zip
a commit builds against. Shaders stay in git.

> **Until you finish this step, most of those files exist only on your disk.** The music,
> card art, logos and menu art were never committed, and git no longer lists them as
> untracked. Do this step before anything happens to this machine.

1. Create a Cloudflare account and turn on **R2**. It asks for a payment method; the free
   tier (10 GB) is far more than the ~11 MB this needs.
2. Create a bucket named `fish-game-assets`. If you choose another name, change it in the
   three files listed in the table above.
3. Install Node.js, which the scripts use to run Cloudflare's `wrangler`:

   ```powershell
   winget install OpenJS.NodeJS.LTS
   ```

   Open a new terminal afterwards, then log in once:

   ```powershell
   npx wrangler login
   ```

4. Publish the assets and pin them:

   ```powershell
   .\build\push-assets.ps1
   ```

   It prints `published v1` and rewrites `build/assets.lock`.

5. Take the 26 files that are still tracked out of git. They stay on your disk:

   ```powershell
   git rm -r -q --cached Fade.MonoGame/Assets/Fish/Audio Fade.MonoGame/Assets/Fish/Fonts Fade.MonoGame/Assets/Fish/Textures
   git add build/assets.lock
   ```

   Commit with the rest in step 5 of this guide.

6. Make the read-only token for CI. In the Cloudflare dashboard: **R2 → Manage API tokens →
   Create API token**, permission **Object Read only**, limited to this bucket. Keep the
   **Token value** (not the S3 access key pair). The **Account ID** is on the R2 overview
   page. Both go into GitHub in step 4.

The old copies stay in git history. That is about 6 MB, so it is not worth rewriting history
for.

### Day to day, after this

| You did | Run |
|---|---|
| Changed, added or deleted art, audio or fonts | `.\build\push-assets.ps1`, then commit `build/assets.lock` |
| Cloned the repo, or pulled a commit with a new lock | `.\build\fetch-assets.ps1` |
| Cloned the repo, or pulled a commit that changes `build/steamworks.lock` | `.\build\fetch-steamworks.ps1` |
| Want to drop local files that are not in the bundle | `.\build\fetch-assets.ps1 -Clean` |

A build fails with a message naming `fetch-assets` if the folders are missing. `fetch-assets`
overwrites files the bundle has, and leaves alone (and lists) local files it does not have,
so unpublished new art is safe. A commit that changes assets but not the lock will build with
the old assets in CI.

On macOS or Linux use the `.sh` twins of both scripts.

---

## 2. Steamworks: the app, two depots, launch options

1. In Steamworks, create the app (this is the step with the Steam Direct fee). Note the
   **App ID**.
2. **SteamPipe → Depots**: you need two depots. A new app usually comes with one already,
   numbered app id + 1. Add a second. Set one to **Windows** and the other to **macOS**.
   Note both ids.
3. **Installation → General Installation → Launch Options**, add two:
   - Executable `Fade.MonoGame.exe`, operating system Windows.
   - Executable `FishFishPop.app`, operating system macOS. The build is Apple Silicon only,
     so if the page offers a CPU architecture for macOS, choose Apple Silicon / ARM64.
4. Check that both depots are in the app's packages (the developer comp package at least),
   or testers on one OS will get nothing to download.
5. **Publish** the Steamworks changes. Unpublished depot and launch settings do nothing.

---

## 3. A Steam builder account, and its login for CI

`steamcmd` cannot answer a Steam Guard prompt inside CI. So you log in once on your own
machine, and CI reuses the saved login.

1. Create a **new** Steam account for builds, with Steam Guard by email. Do not use your own:
   CI would hold a login to everything you own, and logging in elsewhere can end its session.
2. In Steamworks: **Users & Permissions → Add user**, invite that account, accept, and give
   it access to this app only, with **Edit App Metadata** and **Publish App Changes To
   Steam**.
3. Get steamcmd into its own folder and log in. On Windows, steamcmd keeps everything in its
   own folder and does not touch your installed Steam client.

   ```powershell
   mkdir C:\steamcmd-ci; cd C:\steamcmd-ci
   curl.exe -sSLO https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip
   Expand-Archive steamcmd.zip .
   .\steamcmd.exe +login YOUR_BUILDER_LOGIN +quit
   ```

   Enter the password, then the Steam Guard code from the email.

4. **Run the same login command a second time.** It must log in without asking for a code.
   If it asks again, the login was not saved and CI will fail the same way.

5. Copy the saved login to the clipboard as base64:

   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\steamcmd-ci\config\config.vdf")) | Set-Clipboard
   ```

   That is the value of `STEAM_CONFIG_VDF`.

The saved login lasts months, not forever. When the workflow starts failing at login, repeat
steps 3 to 5 and replace the secret.

---

## 4. GitHub secrets and variables

**Repo → Settings → Secrets and variables → Actions.**

On the **Secrets** tab:

| Name | Value |
|---|---|
| `STEAM_USERNAME` | the builder account's login name (not its display name or email) |
| `STEAM_CONFIG_VDF` | the base64 from step 3 |
| `CLOUDFLARE_API_TOKEN` | the R2 token value from step 1 |
| `CLOUDFLARE_ACCOUNT_ID` | the Cloudflare account id from step 1 |

On the **Variables** tab:

| Name | Value |
|---|---|
| `STEAM_APP_ID` | the app id |
| `STEAM_DEPOT_WINDOWS` | the Windows depot id |
| `STEAM_DEPOT_MAC` | the macOS depot id |

---

## 5. Commit and push

The workflow builds what is on GitHub, not what is on your disk. Commit and push:

- the new files: `.github/workflows/steam.yml`, `build/`, `steam/`
- the changes to `.gitignore`, `Fade.MonoGame/Fade.MonoGame.csproj`, `Fade.MonoGame/Program.cs`
- `build/assets.lock` and the asset removals from step 1
- your game work, including the shader files under `Assets/Fish/Shaders` that are still
  untracked. A shader missing from git fails in CI with nothing wrong locally.

GitHub only offers **Run workflow** once `steam.yml` is on the default branch.

---

## 6. First run: build only

**Actions → Steam (Fish Fish Pop) → Run workflow**, and untick **deploy**.

This needs only the two Cloudflare secrets. It builds both platforms and attaches them to the
run as the artifact `fishfishpop-builds`. Download it and run both builds before anything
goes to Steam:

- Windows: unzip, run `Fade.MonoGame.exe`.
- macOS: `tar -xzf FishFishPop-macos-arm64.tar.gz`, then, because a downloaded app is
  quarantined and this one is not notarized: `xattr -dr com.apple.quarantine FishFishPop.app`
  and open it. Steam installs do not need that.

`FISH_SKIP_SPLASH=1` and `FISH_SKIP_MENU=1` still work for getting to the board quickly.

**Test the macOS build on a real Mac here.** It has never been launched on one. See "Not
verified" below.

---

## 7. First upload, then the real thing

Steamworks will not let you create a beta branch until the app has a build, so the first
upload sets nothing live.

1. Run the workflow with **deploy** ticked and **set_live** = `none`.
2. In Steamworks, **SteamPipe → Builds** now shows the build. Create a branch named `test`
   (with a password if you want it hidden).
3. Run the workflow again, leaving **set_live** as `test`. The build is described as
   `main <sha> (run N)` and is live on `test`.
4. Install it: in your Steam library, the game → Properties → Betas → `test`.

**set_live** is a dropdown: `test`, `default` or `none`. `default` is the branch every player
gets. Steam does not let the upload itself set it: an upload that asks for it is rejected
whole ("Failed to commit build"). So choosing `default` uploads the build with nothing set
live, and then:

- **With the `STEAM_PUBLISHER_KEY` secret**, the workflow sets the build live on default
  through the Steamworks Web API (`ISteamApps/SetAppBuildLive`).
- **Without it**, the run summary links to the Builds page, where you set it live by hand.

To get the key: in Steamworks, **Users & Permissions → Manage Groups**, create a group (or
open one), add this app to it, and use **Create WebAPI Key** on the group's page. Add it as
the GitHub secret `STEAM_PUBLISHER_KEY`. It is a powerful key; it goes nowhere else.

Once the game is **released**, Steam also wants a person to approve changes to default in
the Steam mobile app, and this call will be refused until the workflow is taught to name
that person. Before release it needs no approval.

---

## What changed in the game to make this possible

- **`Program.cs`**: the game used to always compile the `.fbasic` files from the project
  folder at startup, which cannot work on a player's machine. That path is now Debug only.
  Release runs `GeneratedFade`, the program compiled at build time.
- **Saves**: the engine writes `prefs.json` (the saved game and the options) to the working
  directory. A macOS app starts in `/`, which cannot be written, so a Release build first
  moves to its own folder: `%APPDATA%\FishFishPop` on Windows,
  `~/Library/Application Support/FishFishPop` on macOS. Debug builds are unchanged.
- **`Fade.MonoGame.csproj`**: Release builds the content (`BuildGameContent`), has no console
  window (`WinExe`), and includes MonoGame, SDL and OpenAL in a publish. They were being left
  out of `dotnet publish` output before, so a published build could not start.

## Not verified

Checked on this Windows machine: a clean Release publish for both platforms, the published
Windows build starting up to its window, the asset scripts round-tripping a bundle made on
Windows and unpacked by the Linux script, and the macOS bundle layout.

Not checked, because it needs your accounts or other hardware:

- **The workflow has never run on GitHub.** The Linux steps follow `ci.yml` (Wine for
  shaders) and the beatboys workflow (steamcmd), but this game's 15 shaders, its font and its
  mp3s have not been built on Linux. The build-only run in step 6 is where that gets proven.
- **The macOS build has never been launched.** In particular: that it finds its content from
  inside the `.app`, that the OpenGL shaders behave on Apple Silicon, and that saving works.
- `build/push-assets.sh` (the non-Windows push) was not run; this machine has no `zip`.

## The Steamworks API

The game links the Steam API through `Fade.MonoGame.Steam`, which gives the Fade program
the `steam ...` commands: who is playing, achievements, stats, leaderboards, rich presence
and the overlay. `Program.cs` registers a `SteamSystem`, which starts Steam before the first
frame and pumps it once a frame.

**Steam is optional at runtime.** Started outside Steam, or with the Steam client closed,
the game still runs: `steam available()` is 0 and every other Steam command does nothing.
`log.txt` says why, on a line that starts with `[steam]`.

**After cloning the repo**, fetch the Steamworks wrapper once, or nothing builds:

```powershell
.\build\fetch-steamworks.ps1
```

It clones [Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks) into
`External/`, which is not in git, at the commit in `build/steamworks.lock`. The workflow
runs the `.sh` twin. To move to a newer version, change the commit in the lock and run it
again. The native Steam libraries come from that same checkout, so they always match.

**To use Steam in a build that you start yourself**, the build has to be told which game
it is. Steam tells a game that it launches; a build from `dotnet run` gets it from a file:

```powershell
Set-Content Fade.MonoGame\steam_appid.txt YOUR_APP_ID
```

The file is ignored by git and is copied next to the executable when it exists. The Steam
client has to be running, logged in to an account that owns the game. Without the file a
local build runs with no Steam, which is fine for everything that is not a Steam feature.

> **`steam_appid.txt` must never reach a depot.** With it, the game starts without a
> license. A CI build never has one, and the workflow fails if one turns up in a payload.

What ships: `Facepunch.Steamworks.Win64.dll` and `steam_api64.dll` on Windows,
`Facepunch.Steamworks.Posix.dll` and `libsteam_api.dylib` (in `Contents/MacOS`) on macOS.
The workflow checks for all four.

Not verified: Steam has only been started from a Windows build. The macOS build gets the
right files, but whether it loads `libsteam_api.dylib` from inside the `.app` has not been
tried on a Mac.

---

## Leaderboards

Every highscore table has a leaderboard on Steam, and the Highscores page has a row of tabs
for whose scores to show: **Local**, **Friends** or **Global**. A game that ends is sent to
the leaderboard of its table. The game makes a leaderboard the first time it needs it, so
there is nothing to create in Steamworks.

**Which leaderboards a build uses depends on where it was built:**

| Build | Leaderboards | Score key |
|---|---|---|
| The workflow, **set_live** = `default` | `live_normal_short`, `live_zen_long`, ... | the secret + `.default` |
| The workflow, **set_live** = `test` or `none` | `test_normal_short`, `test_zen_long`, ... | the secret + `.test` |
| Anything else: a dev machine, or the workflow without the `FISH_SCORE_KEY` secret | `dev_normal_short`, `dev_zen_long`, ... | the word `dev` |

So scores from a dev machine or from the `test` branch never show up for players, and a
score from one kind of build also fails the check of the others.

> **A build belongs to the branch it was built for.** Setting a `test` build live on
> `default` by hand in Steamworks gives players the `test_` leaderboards. To release, run
> the workflow again with **set_live** = `default`. The same goes for `none`, which builds
> as `test`: that includes the manual step when there is no `STEAM_PUBLISHER_KEY`, where a
> `default` run is safe to set live by hand but a `none` run is not.

**Set the key once**, before the first deploy that has leaderboards. Make it up, 16 or more
letters and digits, and add it as the GitHub secret `FISH_SCORE_KEY`:

```powershell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 40 | ForEach-Object { [char]$_ })
```

The workflow refuses to deploy without it. **Do not change it afterwards**: every score is
signed with it, and a build with a new key hides every score that was made with the old one.
Keep a copy somewhere safe, because GitHub will not show it to you again.

**What the key is for.** Anyone can post any number to a Steam leaderboard with a tool. Every
score that the game sends carries a check that is worked out from the score, the player, the
leaderboard and the key, and the game leaves out the scores whose check is wrong. It does not
stop someone who takes the game apart to find the key.

**Moderating.** The game can only hide a bad score, not remove it. To remove one, or to see
every score: Steamworks, **App Admin, Stats & Achievements, Leaderboards**. To empty the
`dev_` leaderboards, delete them there; the next dev build makes them again.

How a game is packed into a score is written up at the top of
`Fade.MonoGame/fish_routines_highscores.fbasic`, under THE SCORES ON STEAM.

---

## Things you may want later

- **Steam Cloud**: Auto-Cloud can sync `prefs.json` with no code. Windows root
  `WinAppDataRoaming`, macOS root `MacAppSupport`, subdirectory `FishFishPop` for both.
- **Intel Macs**: only Apple Silicon is built. Intel needs a second publish with
  `-r osx-x64` and its own bundle.
- **macOS icon**: the bundle has no `.icns`, so it shows the generic app icon in the Dock.
- **Size**: each build is about 200 MB. Roughly 70 MB is the .NET runtime and 120 MB is audio,
  which the content pipeline stores uncompressed. The publish also carries the test-runner
  DLLs (`FadeBasic.Testing`, `testhost`), which are harmless but could be made Debug only.

## Troubleshooting

**`build/assets.lock pins no bundle`**: step 1 is not finished, or the lock was not
committed.

**`HASH MISMATCH` from fetch-assets**: the zip in the bucket is not the one the lock names.
Run `push-assets` again and commit the lock.

**`Fade.MonoGame/Assets/Fish/Textures is missing`** on a local build: run `fetch-assets`.

**`Facepunch.Steamworks is missing`**, or restore cannot find
`Facepunch.Steamworks.Win64.csproj`: run `fetch-steamworks`.

**The game runs but Steam features do nothing**: look for the `[steam]` line near the top of
`log.txt`. "No Steam app id" means the game was started outside Steam with no
`steam_appid.txt`. "Steam could not start" means the client is closed, or the account does
not own the game.

**Login asks for a Steam Guard code in CI, or `Two-factor code mismatch`**: the saved login
did not carry over or has expired. Redo step 3, and make sure the second local login was
silent.

**CI login fails although the local one was silent**: the login was saved by Windows
steamcmd and is being used by Linux steamcmd. That normally works. If it does not, make
`config.vdf` on Linux instead (WSL works: `~/Steam/config/config.vdf`).

**Permission or access denied on upload**: the builder account lacks rights on the app.
Recheck step 3.2.

**`setlive` fails but the upload succeeded**: the branch does not exist yet, or the account
cannot set builds live. The build is in Steamworks; set it live by hand on the Builds page.

**Mac testers get Windows files, or nothing**: the macOS depot is not in the package, its OS
is not set to macOS, or the change was not published.

**The game starts with no art or sound on macOS**: the content is not in
`FishFishPop.app/Contents/Resources/Content`. The workflow checks for this; if it happens
anyway, MonoGame is looking somewhere else, and that is the first thing to look at.
