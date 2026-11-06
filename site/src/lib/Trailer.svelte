<script>
  import media from './media.json';
  import { asset } from './site.js';

  let video;
  let started = $state(false);

  function play() {
    started = true;
    video.play();
  }
</script>

<section class="section" id="trailer">
  <div class="wrap">
    <div class="heading">
      <h2>Trailer</h2>
      <div class="rule"><span></span></div>
    </div>
    <div class="frame panel">
      <!-- svelte-ignore a11y_media_has_caption -->
      <video
        bind:this={video}
        src={asset(media.trailer.src)}
        poster={asset(media.trailer.poster)}
        preload="none"
        playsinline
        controls={started}
        onplay={() => (started = true)}
      ></video>
      {#if !started}
        <button class="play" onclick={play} aria-label="Play the trailer">
          <span class="key"><span class="tri"></span></span>
        </button>
      {/if}
    </div>
  </div>
</section>

<style>
  .frame {
    position: relative;
    padding: 8px;
    max-width: 1100px;
    margin: 0 auto;
  }
  video {
    width: 100%;
    aspect-ratio: 16 / 9;
    object-fit: cover;
    background: #000;
  }
  .play {
    position: absolute;
    inset: 8px;
    display: grid;
    place-items: center;
    background: none;
    border: 0;
    cursor: pointer;
  }
  .key {
    width: 92px; height: 86px;
    display: grid;
    place-items: center;
    background: var(--key);
    border: 2px solid #15233f;
    box-shadow: 0 6px 0 var(--key-edge), 0 6px 0 2px #15233f;
    transition: transform 0.08s, box-shadow 0.08s;
  }
  .tri {
    width: 0; height: 0;
    margin-left: 7px;
    border-left: 28px solid #15233f;
    border-top: 18px solid transparent;
    border-bottom: 18px solid transparent;
  }
  .play:hover .key { transform: translateY(-3px); box-shadow: 0 9px 0 var(--key-edge), 0 9px 0 2px #15233f; }
  @media (max-width: 600px) {
    .frame { padding: 4px; }
    .play { inset: 4px; }
    .key { transform: scale(0.75); }
  }
</style>
