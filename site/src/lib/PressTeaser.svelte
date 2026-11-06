<script>
  import media from './media.json';
  import { asset } from './site.js';
  import { mb } from './format.js';

  // two tall ones behind, the main capsule over them
  const peek = ['vertical_capsule', 'main_capsule', 'library_capsule']
    .map((n) => media.capsules.find((c) => c.name === n))
    .filter(Boolean);
</script>

<section class="section" id="press">
  <div class="wrap">
    <div class="press panel">
      <div class="text">
        <h2>Press Kit</h2>
        <p>Writing about the game? It's all here, full size: the fact sheet, trailer, clips, shorts, screenshots and store art.</p>
        <div class="links">
          <a class="cta" href={asset('press/')}>Open the press kit</a>
          <a class="item" href={asset(media.zip.src)} download>Every picture, zipped <span class="small">({mb(media.zip.bytes)})</span></a>
        </div>
      </div>
      <div class="fan" aria-hidden="true">
        {#each peek as c, i}
          <img src={asset(c.preview)} alt="" style:--i={i - (peek.length - 1) / 2} loading="lazy" />
        {/each}
      </div>
    </div>
  </div>
</section>

<style>
  .press {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 32px;
    align-items: center;
    padding: clamp(24px, 5vw, 52px);
    overflow: hidden;
  }
  h2 { margin-bottom: 12px; }
  .links { display: flex; flex-wrap: wrap; align-items: center; gap: 20px 10px; margin-top: 24px; }
  .links .cta { font-size: 1.4rem; }
  .links .item { font-size: 1.15rem; }
  .fan { position: relative; height: 280px; }
  .fan img {
    position: absolute;
    left: 50%; top: 50%;
    height: 92%;
    width: auto;
    border: 1px solid var(--panel-edge);
    box-shadow: 0 14px 30px rgba(0, 0, 0, 0.6);
    transform: translate(-50%, -50%) translateX(calc(var(--i) * 70%)) rotate(calc(var(--i) * 6deg));
    transition: transform 0.5s;
  }
  .fan img:nth-child(2) { height: auto; width: 78%; z-index: 1; }
  .press:hover .fan img { transform: translate(-50%, -50%) translateX(calc(var(--i) * 82%)) rotate(calc(var(--i) * 9deg)); }
  .press:hover .fan img:nth-child(2) { transform: translate(-50%, -50%) scale(1.04); }
  @media (max-width: 760px) {
    .press { grid-template-columns: 1fr; }
    .fan { height: 200px; }
  }
</style>
