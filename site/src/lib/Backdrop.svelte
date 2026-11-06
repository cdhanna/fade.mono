<script>
  // The sea behind the page (gl/backdrop.js). Going down the page is a dive, and on the press kit it stays
  // at the bottom of it: the night. A still of the sea stands in until it starts, and for good without
  // WebGL2; with reduced motion the live one is one picture that only changes with the scroll. The ripples of
  // light go over the whole page, as they go over the whole screen in the game, and they get out of the way
  // of a video with its controls.
  import { onMount } from 'svelte';
  import { startBackdrop } from './gl/backdrop.js';
  import { asset } from './site.js';

  let { deep = false } = $props();

  let canvas;
  let lightCanvas;
  let api = null;
  let live = $state(false);
  let watching = $state(false);

  // the page's depth, from 0 at the top to 1 at the bottom
  function depth() {
    const room = document.documentElement.scrollHeight - innerHeight;
    const t = room > 0 ? scrollY / room : 0;
    return deep ? 0.75 + 0.25 * t : t;
  }

  $effect(() => {
    deep;
    api?.setDepth(depth());
  });

  onMount(() => {
    let gone = false;
    let cleanup = () => {};
    start(() => gone).then((c) => { if (c) cleanup = c; });
    return () => { gone = true; cleanup(); };
  });

  async function start(isGone) {
    const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
    await new Promise((r) => requestAnimationFrame(() => setTimeout(r, 0)));
    try {
      api = await startBackdrop(canvas, { lightCanvas, still: reduce, base: asset('') });
    } catch (e) {
      console.warn('backdrop:', e);
    }
    if (!api) return null;
    if (isGone()) { api.stop(); return null; }
    live = true;

    let lastScroll = scrollY;
    const onScroll = () => {
      api.scrollBy(scrollY - lastScroll);
      lastScroll = scrollY;
      api.setDepth(depth());
    };
    const onResize = () => api.setDepth(depth());
    // a media event does not bubble, so it is caught on the way down
    const onVideo = (e) => {
      if (e.target.controls) watching = e.type === 'play';
    };
    for (const type of ['play', 'pause', 'ended']) document.addEventListener(type, onVideo, true);
    addEventListener('scroll', onScroll, { passive: true });
    addEventListener('resize', onResize);
    onScroll();
    return () => {
      for (const type of ['play', 'pause', 'ended']) document.removeEventListener(type, onVideo, true);
      removeEventListener('scroll', onScroll);
      removeEventListener('resize', onResize);
      api.stop();
    };
  }
</script>

<div class="backdrop" aria-hidden="true">
  <div class="still" style:background-image="url({asset('media/backdrop.jpg')})" class:gone={live}></div>
  <canvas bind:this={canvas} class:live></canvas>
</div>
<canvas class="light" bind:this={lightCanvas} class:live={live && !watching} aria-hidden="true"></canvas>

<style>
  .backdrop {
    position: fixed;
    inset: 0;
    z-index: -1;
    background: var(--abyss);
    overflow: hidden;
  }
  .still, canvas {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
  }
  .still {
    background-size: cover;
    background-position: center;
    image-rendering: pixelated;
    transition: opacity 1.2s;
  }
  .still.gone { opacity: 0; }
  canvas { opacity: 0; transition: opacity 1.2s; image-rendering: pixelated; }
  canvas.live { opacity: 1; }
  .light {
    position: fixed;
    inset: 0;
    width: 100%;
    height: 100%;
    z-index: 5;
    pointer-events: none;
    mix-blend-mode: screen;
    image-rendering: auto;
    transition: opacity 0.5s;
  }
</style>
