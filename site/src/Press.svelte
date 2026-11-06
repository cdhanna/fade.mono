<script>
  // The press kit page (App.svelte's route 'press'): the facts, the words, the trailer, the clips, the
  // shorts, every screenshot and all the store art, each at full size.
  import Clip from './lib/Clip.svelte';
  import media from './lib/media.json';
  import { site, steamStore, asset } from './lib/site.js';
  import { mb } from './lib/format.js';

  const facts = [
    ['Developer', site.developer],
    ['Release', site.release],
    ['Platforms', site.platforms.join(', ')],
    ['Players', site.players],
    ['Price', site.price],
    ['Languages', site.languages.join(', ')],
  ];
</script>

<div class="page wrap">
  <header class="top">
    <h1>Press Kit</h1>
    <p class="name">{site.name} <span>{site.tagline}</span></p>
    <div class="rule"><span></span></div>
    <a class="cta" href={asset(media.zip.src)} download>Every picture, zipped <span class="size">{mb(media.zip.bytes)}</span></a>
  </header>

  <section class="sheet">
    <dl class="facts panel">
      {#each facts as [k, v]}
        <div><dt>{k}</dt><dd>{v}</dd></div>
      {/each}
      <div><dt>Store</dt><dd><a href={steamStore} target="_blank" rel="noopener">{steamStore.replace('https://', '')}</a></dd></div>
      {#if site.contact}
        <div><dt>Contact</dt><dd><a href="mailto:{site.contact}">{site.contact}</a></dd></div>
      {/if}
    </dl>
    <div class="words panel">
      <h2>In Short</h2>
      <p class="short">{site.short}</p>
      {#each site.sections as s}
        <h2>{s.title}</h2>
        {#each s.lines as line}<p>{line}</p>{/each}
      {/each}
      <h2>In the box</h2>
      <ul>
        {#each site.features as f}<li>{f}</li>{/each}
      </ul>
    </div>
  </section>

  <section>
    <h2 class="band">Trailer</h2>
    <div class="trailer panel">
      <!-- svelte-ignore a11y_media_has_caption -->
      <video src={asset(media.trailer.src)} poster={asset(media.trailer.poster)} controls preload="none" playsinline></video>
      <p><a href={asset(media.trailer.src)} download>Download the trailer</a> (MP4, 1080p, {mb(media.trailer.bytes)}).</p>
    </div>
  </section>

  {#if Object.keys(media.clips ?? {}).length}
    <section>
      <h2 class="band">Clips</h2>
      <ul class="tiles shots">
        {#each Object.entries(media.clips) as [name, c]}
          <li class="panel">
            <Clip {name} label={c.title} />
            <p>{c.title}</p>
            <p class="small"><a href={asset(c.mp4)} download>MP4</a> · silent · {mb(c.bytes)}</p>
          </li>
        {/each}
      </ul>
    </section>
  {/if}

  {#if media.shorts?.length}
    <section>
      <h2 class="band">Shorts</h2>
      <ul class="tiles tall">
        {#each media.shorts as s}
          <li class="panel">
            <a href={asset(s.mp4)} download><img src={asset(s.jpg)} alt={s.title} width="360" height="640" loading="lazy" /></a>
            <p>{s.title}</p>
            <p class="small"><a href={asset(s.mp4)} download>MP4</a> · 1080 × 1920 · {mb(s.bytes)}</p>
          </li>
        {/each}
      </ul>
    </section>
  {/if}

  <section>
    <h2 class="band">Screenshots</h2>
    <ul class="tiles shots">
      {#each media.shots as s}
        <li class="panel">
          <a href={asset(s.png)} download><img src={asset(s.thumb)} alt={s.title} width={s.w} height={s.h} loading="lazy" /></a>
          <p>{s.title}</p>
          <p class="small"><a href={asset(s.png)} download>PNG</a> · {s.w} × {s.h} · {mb(s.bytes)}</p>
        </li>
      {/each}
    </ul>
  </section>

  <section>
    <h2 class="band">Store Art and Logo</h2>
    <ul class="tiles art">
      {#each media.capsules as c}
        <li class="panel" class:wide={c.w / c.h > 3}>
          <a href={asset(c.png)} download class="pic" class:logo={c.name === 'library_logo'}>
            <img src={asset(c.preview)} alt={c.label} width={c.w} height={c.h} loading="lazy" />
          </a>
          <p>{c.label}</p>
          <p class="small"><a href={asset(c.png)} download>PNG</a> · {c.w} × {c.h} · {mb(c.bytes)}</p>
        </li>
      {/each}
    </ul>
  </section>
</div>

<style>
  .page { padding-top: 110px; }
  .top { text-align: center; margin-bottom: 56px; }
  h1 { font-size: clamp(3.2rem, 9vw, 6rem); text-shadow: 4px 4px 0 var(--shadow); }
  .name {
    margin: 0.3em 0 1em;
    font-family: var(--display);
    font-size: 1.5rem;
    text-transform: uppercase;
    color: var(--foam);
  }
  .name span { display: block; font-size: 1.2rem; color: var(--gold); }
  .top .cta { margin-top: 30px; font-size: 1.5rem; }
  .size { font-family: var(--body); font-weight: 700; font-size: 0.6em; text-transform: none; opacity: 0.7; }

  section { margin-bottom: 72px; }
  .sheet {
    display: grid;
    grid-template-columns: 320px 1fr;
    gap: 28px;
    align-items: start;
  }
  .facts { margin: 0; padding: 24px; display: grid; gap: 14px; position: sticky; top: 84px; }
  .facts dt { font-family: var(--display); text-transform: uppercase; color: var(--hud-light); font-size: 1.05rem; }
  .facts dd { margin: 0; color: var(--foam); font-size: 1rem; overflow-wrap: anywhere; }
  .words { padding: clamp(20px, 3vw, 32px); }
  .words h2 { font-size: 1.7rem; margin: 1.3em 0 0.4em; }
  .words h2:first-child { margin-top: 0; }
  .short { font-size: 1.22rem; color: var(--foam); }
  .words ul { padding-left: 1.1em; margin: 0; }
  .words li { margin-bottom: 0.3em; }

  .band {
    font-size: clamp(1.9rem, 4vw, 2.4rem);
    padding-bottom: 10px;
    margin-bottom: 22px;
    border-bottom: 1px solid var(--panel-edge);
  }
  .trailer { padding: 8px; max-width: 980px; }
  .trailer video { width: 100%; aspect-ratio: 16 / 9; background: #000; }
  .trailer p { margin: 10px 6px 4px; font-size: 1rem; }

  .tiles {
    list-style: none;
    margin: 0;
    padding: 0;
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 16px;
  }
  .tiles.tall { grid-template-columns: repeat(auto-fill, minmax(170px, 1fr)); }
  .tiles li { padding: 8px 8px 4px; }
  .tiles li.wide { grid-column: 1 / -1; }
  .tiles img { width: 100%; height: auto; }
  .pic { display: grid; place-items: center; height: 220px; background: #000; }
  .wide .pic { height: auto; }
  .tiles .pic img { width: auto; height: auto; max-width: 100%; max-height: 220px; }
  .tiles .wide .pic img { max-height: none; }
  .tiles .logo img { max-height: 172px; }
  .pic.logo { background: repeating-conic-gradient(#16263f 0% 25%, #0c1a30 0% 50%) 0 0 / 20px 20px; padding: 24px; }
  .tiles p { margin: 8px 4px 0; font-family: var(--display); text-transform: uppercase; color: var(--foam); font-size: 1.1rem; }
  .tiles p.small { margin-top: 0; margin-bottom: 6px; font-family: var(--body); text-transform: none; font-size: 0.9rem; }

  @media (max-width: 860px) {
    .sheet { grid-template-columns: 1fr; }
    .facts { position: static; }
  }
</style>
