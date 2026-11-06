<script>
  // The description, as on the Steam page (site.js; npm run steam writes Steam's): each section with its
  // clip beside it, turn and turn about, then the features.
  import { site } from './site.js';
  import Clip from './Clip.svelte';
</script>

<section class="section" id="about">
  <div class="wrap">
    {#each site.sections as s, i}
      <div class="row" class:flip={i % 2 === 1} class:bare={!s.clip}>
        {#if s.clip}<Clip name={s.clip} label={s.title} />{/if}
        <div class="words panel">
          <h2>{s.title}</h2>
          {#each s.lines as line}<p>{line}</p>{/each}
        </div>
      </div>
    {/each}

    <div class="features panel">
      <h2>In the box</h2>
      <ul>
        {#each site.features as f}<li>{f}</li>{/each}
      </ul>
    </div>
  </div>
</section>

<style>
  .row {
    display: grid;
    grid-template-columns: 1.5fr 1fr;
    gap: clamp(20px, 3vw, 40px);
    align-items: center;
    margin-bottom: clamp(44px, 8vh, 80px);
  }
  .row.flip { grid-template-columns: 1fr 1.5fr; }
  .row.flip :global(.clip) { order: 2; }
  .row.bare { grid-template-columns: 1fr; text-align: center; }
  h2 { font-size: clamp(1.8rem, 3.2vw, 2.4rem); margin-bottom: 12px; }
  .words { padding: clamp(18px, 2.5vw, 30px); }
  .words p { margin: 0; color: var(--mist); }
  .words p + p { margin-top: 0.8em; }

  .features { max-width: 820px; margin: 0 auto; padding: clamp(22px, 4vw, 38px); }
  .features h2 { text-align: center; margin-bottom: 18px; }
  ul {
    list-style: none;
    margin: 0;
    padding: 0;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 10px 32px;
  }
  li { position: relative; padding-left: 24px; color: var(--mist); }
  li::before {
    content: '';
    position: absolute;
    left: 3px; top: 0.6em;
    width: 8px; height: 8px;
    background: var(--hud-light);
    transform: rotate(45deg);
  }

  @media (max-width: 760px) {
    .row, .row.flip { grid-template-columns: 1fr; }
    .row.flip :global(.clip) { order: 0; }
    ul { grid-template-columns: 1fr; }
  }
</style>
