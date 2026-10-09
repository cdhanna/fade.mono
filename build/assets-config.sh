# Shared settings for the asset scripts. Sourced, not run.
# The Windows twins (fetch-assets.ps1, push-assets.ps1) repeat these values -- keep them in step.
#
# The bucket holds ZIPS ONLY, never loose files. A zip is an atomic, single-hash snapshot, which
# is what a build needs. Locally you always work with loose files -- the scripts move between
# the two.

# SWAP IN: the name of your Cloudflare R2 bucket. See steam/SETUP.md.
R2_BUCKET="${R2_BUCKET:-fish-game-assets}"

# The bundle is rooted here, and holds only the folders in BUCKET_DIRS. Shaders are not in it:
# they are source, they stay in git.
ASSET_ROOT="Fade.MonoGame/Assets"
BUCKET_DIRS="Fish/Audio Fish/Fonts Fish/Textures"

LOCK="build/assets.lock"
LATEST_KEY="assets/latest.json"

# Wrangler, PINNED. A major bump can change CLI flags, and the first place that would show up
# is a deploy failing for reasons unrelated to the commit that triggered it.
# Override with WRANGLER=... to test a newer one.
WRANGLER="${WRANGLER:-npx --yes wrangler@4.126.0}"

# Auth. Locally these are unset and wrangler uses the login from `npx wrangler login`.
# On CI set both from GitHub Secrets; wrangler picks them up with no login step:
#
#   CLOUDFLARE_API_TOKEN    an R2 token, Object Read only for CI
#   CLOUDFLARE_ACCOUNT_ID   the account the bucket lives in
