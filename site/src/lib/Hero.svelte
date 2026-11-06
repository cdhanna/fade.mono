<script>
  // The title: the logo's three words, each a picture of its own (build/capsule_drafts.py --words), drifting a
  // little in the water, each at its own pace. Without them, the whole logo.
  import media from './media.json';
  import { site, steamStore, asset } from './site.js';

  const words = media.logoWords;
  const pct = (v, of) => `${((v / of) * 100).toFixed(3)}%`;
</script>

<section class="hero" id="top">
  {#if words}
    <h1 class="words" style:aspect-ratio="{words.w} / {words.h}">
      <span class="name">{site.name}</span>
      {#each words.words as w, i}
        <img
          class="word w{i}"
          src={asset(w.src)}
          alt=""
          width={w.w}
          height={w.h}
          style:left={pct(w.x, words.w)}
          style:top={pct(w.y, words.h)}
          style:width={pct(w.w, words.w)}
        />
      {/each}
    </h1>
  {:else}
    <h1 class="whole">
      {#if media.logo}
        <img src={asset(media.logo)} alt={site.name} width="1280" height="720" />
      {:else}
        {site.name}
      {/if}
    </h1>
  {/if}
  <p class="tagline">{site.tagline}</p>
  <div class="menu">
    <a class="cta" href={steamStore} target="_blank" rel="noopener">Wishlist on Steam</a>
    <a class="item" href="#trailer">Watch the trailer</a>
  </div>
  <p class="date">{site.release}</p>
</section>

<style>
  .hero {
    min-height: 100vh;
    min-height: 100svh;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 76px var(--gutter) 24px;
    text-align: center;
  }

  /* the words, where the logo has them, as big as the screen allows and leaving room for the menu */
  .words {
    position: relative;
    width: min(500px, 76vw, 42svh * 1.2);
    margin: 0 0 clamp(28px, 5svh, 52px);
  }
  .name {
    position: absolute;
    width: 1px; height: 1px;
    overflow: hidden;
    clip-path: inset(50%);
    white-space: nowrap;
  }
  .word {
    position: absolute;
    height: auto;
    max-width: none;
    animation: drift 6s ease-in-out infinite;
  }
  /* each word bobs and leans on its own swell, never in step with the others */
  .w0 { animation-duration: 5.3s; animation-delay: -1.1s; }
  .w1 { animation-duration: 6.4s; animation-delay: -3.7s; --lean: -1; }
  .w2 { animation-duration: 7.2s; animation-delay: -2.2s; --rise: 0.7; }
  @keyframes drift {
    0%, 100% { transform: translate(0, 0) rotate(0deg); }
    25% { transform: translate(calc(2px * var(--lean, 1)), calc(-6px * var(--rise, 1))) rotate(calc(0.8deg * var(--lean, 1))); }
    50% { transform: translate(0, calc(-2px * var(--rise, 1))) rotate(0deg); }
    75% { transform: translate(calc(-2px * var(--lean, 1)), calc(4px * var(--rise, 1))) rotate(calc(-0.8deg * var(--lean, 1))); }
  }
  @media (prefers-reduced-motion: reduce) {
    .word { animation: none; }
  }

  /* the logo's picture is 16:9 with the name across its middle, so the box is only as tall as the name */
  .whole {
    width: min(860px, 94vw);
    aspect-ratio: 5.6 / 1;
    position: relative;
    margin: 0 0 6px;
  }
  .whole img {
    position: absolute;
    left: 0; top: 50%;
    width: 100%;
    height: auto;
    max-width: none;
    transform: translateY(-50%);
    image-rendering: pixelated;
  }
  .tagline {
    position: relative;
    margin: 0 0 1.4em;
    font-family: var(--display);
    font-size: clamp(1.5rem, 3.4vw, 2.4rem);
    line-height: 1.1;
    text-transform: uppercase;
    color: var(--gold);
    text-shadow: 3px 3px 0 var(--shadow);
  }
  .menu { display: flex; flex-direction: column; align-items: center; gap: 18px; }
  .date {
    margin: 14px 0 0;
    font-family: var(--display);
    font-size: 1.2rem;
    text-transform: uppercase;
    color: var(--foam);
    text-shadow: 2px 2px 0 var(--shadow);
  }
</style>
