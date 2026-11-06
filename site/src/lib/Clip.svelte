<script>
  // A clip (build/render-clips.ps1), as Steam plays them: muted, looping. It only loads and plays
  // while it is on screen; with reduced motion it waits for a click.
  import media from './media.json';
  import { asset } from './site.js';

  let { name, label = '' } = $props();
  const clip = $derived(media.clips?.[name]);
  let video = $state();
  let still = $state(false);

  $effect(() => {
    if (!video) return;
    still = matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (still) return;
    const io = new IntersectionObserver(([e]) => {
      if (e.isIntersecting) video.play().catch(() => {});
      else video.pause();
    }, { threshold: 0.25 });
    io.observe(video);
    return () => io.disconnect();
  });
</script>

{#if clip}
  <div class="clip">
    <video
      bind:this={video}
      poster={clip.jpg ? asset(clip.jpg) : undefined}
      muted
      loop
      playsinline
      preload="none"
      controls={still}
      aria-label={label}
    >
      {#if clip.webm}<source src={asset(clip.webm)} type="video/webm" />{/if}
      {#if clip.mp4}<source src={asset(clip.mp4)} type="video/mp4" />{/if}
    </video>
  </div>
{/if}

<style>
  .clip {
    border: 1px solid var(--panel-edge);
    background: #000;
    box-shadow: 0 20px 50px rgba(0, 0, 0, 0.6);
  }
  video { width: 100%; height: auto; aspect-ratio: 16 / 9; }
</style>
