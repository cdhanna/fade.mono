<script>
  // The screenshots: the first large, the rest under it; any opens big, and the arrows go through
  // them.
  import media from './media.json';
  import { asset } from './site.js';

  const shots = media.shots;
  let dialog;
  let at = $state(0);

  function show(i) {
    at = i;
    dialog.showModal();
  }
  function go(d) {
    at = (at + d + shots.length) % shots.length;
  }
  function onKey(e) {
    if (e.key === 'ArrowRight') go(1);
    if (e.key === 'ArrowLeft') go(-1);
  }
</script>

<section class="section" id="screens">
  <div class="wrap">
    <div class="heading">
      <h2>Screenshots</h2>
      <div class="rule"><span></span></div>
    </div>
    <div class="grid">
      {#each shots as s, i}
        <button class="shot" class:lead={i === 0} onclick={() => show(i)}>
          <img src={asset(i === 0 ? s.full : s.thumb)} alt={s.title} width={s.w} height={s.h} loading="lazy" />
          <span class="cap">{s.title}</span>
        </button>
      {/each}
    </div>
  </div>
</section>

<dialog bind:this={dialog} onkeydown={onKey} onclick={(e) => e.target === dialog && dialog.close()}>
  {#if shots[at]}
    <figure>
      <img src={asset(shots[at].full)} alt={shots[at].title} width={shots[at].w} height={shots[at].h} />
      <figcaption>{shots[at].title} <span class="small">{at + 1} / {shots.length}</span></figcaption>
    </figure>
  {/if}
  <button class="nav prev" onclick={() => go(-1)} aria-label="Previous screenshot"><span></span></button>
  <button class="nav next" onclick={() => go(1)} aria-label="Next screenshot"><span></span></button>
  <button class="close item" onclick={() => dialog.close()}>Close</button>
</dialog>

<style>
  .grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 14px;
  }
  .shot {
    position: relative;
    padding: 0;
    border: 1px solid var(--panel-edge);
    background: #000;
    cursor: zoom-in;
    overflow: hidden;
    box-shadow: 0 20px 50px rgba(0, 0, 0, 0.6);
  }
  .shot.lead { grid-column: 1 / -1; }
  .shot img {
    width: 100%;
    height: auto;
    transition: transform 0.6s, filter 0.6s;
    filter: brightness(0.92);
  }
  .shot:hover img { transform: scale(1.03); filter: brightness(1.05); }
  .cap {
    position: absolute;
    left: 0; right: 0; bottom: 0;
    padding: 28px 14px 10px;
    font-family: var(--display);
    font-size: 1.05rem;
    text-transform: uppercase;
    color: var(--foam);
    text-align: left;
    background: linear-gradient(transparent, rgba(0, 0, 0, 0.85));
    opacity: 0;
    transition: opacity 0.3s;
  }
  .lead .cap { font-size: 1.5rem; padding: 40px 22px 16px; opacity: 1; }
  .shot:hover .cap, .shot:focus-visible .cap { opacity: 1; }
  @media (max-width: 760px) {
    .grid { grid-template-columns: 1fr 1fr; gap: 8px; }
    .cap { display: none; }
    .lead .cap { display: block; font-size: 1.1rem; padding: 24px 12px 8px; }
  }

  dialog {
    width: 100vw;
    height: 100vh;
    max-width: none;
    max-height: none;
    margin: 0;
    padding: 0;
    border: 0;
    background: rgba(3, 3, 6, 0.94);
    color: var(--mist);
  }
  dialog::backdrop { background: rgba(0, 0, 0, 0.7); }
  figure {
    margin: 0;
    height: 100%;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 14px;
    padding: 56px 72px 40px;
  }
  figure img {
    max-height: calc(100% - 50px);
    width: auto;
    object-fit: contain;
    border: 1px solid var(--panel-edge);
  }
  figcaption {
    font-family: var(--display);
    font-size: 1.4rem;
    text-transform: uppercase;
    color: var(--foam);
  }
  figcaption .small { margin-left: 14px; font-family: var(--body); letter-spacing: 0; }
  .nav {
    position: absolute;
    top: 50%;
    width: 56px; height: 56px;
    margin-top: -28px;
    display: grid;
    place-items: center;
    background: none;
    border: 0;
    cursor: pointer;
  }
  .nav span {
    width: 18px; height: 18px;
    border-left: 2px solid var(--hud-light);
    border-bottom: 2px solid var(--hud-light);
  }
  .prev { left: 10px; }
  .prev span { transform: rotate(45deg); }
  .next { right: 10px; }
  .next span { transform: rotate(-135deg); }
  .nav:hover span { border-color: var(--gold); }
  .close { position: absolute; top: 10px; right: 16px; font-size: 1.3rem; }
  @media (max-width: 600px) {
    figure { padding: 56px 8px 24px; }
    .nav { top: auto; bottom: 8px; margin-top: 0; }
  }
</style>
