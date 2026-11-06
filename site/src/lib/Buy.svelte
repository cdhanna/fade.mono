<script>
  // Steam's own buy box (its widget), under a big key to the store page. The widget shows the price and
  // "Add to Cart" once the game is out, and "Coming soon" with a wishlist link before.
  import { site, steamStore, steamWidget } from './site.js';
</script>

<section class="section" id="buy">
  <div class="wrap">
    <div class="box panel">
      <h2>Come, friend.</h2>
      <p class="lead">{site.short}</p>
      <a class="cta big" href={steamStore} target="_blank" rel="noopener">Wishlist on Steam</a>
      <div class="widget">
        <iframe src={steamWidget} title="{site.name} on the Steam store" width="646" height="190" loading="lazy" frameborder="0"></iframe>
      </div>
    </div>
  </div>
</section>

<style>
  .box {
    max-width: 860px;
    margin: 0 auto;
    padding: clamp(28px, 6vw, 56px) clamp(16px, 5vw, 56px);
    text-align: center;
  }
  h2 { margin-bottom: 16px; }
  .lead { max-width: 34em; margin: 0 auto 32px; }
  .big { font-size: clamp(1.7rem, 5vw, 2.5rem); }
  .widget { margin: 44px auto 0; width: 100%; max-width: 646px; }
  iframe { width: 100%; height: 190px; border: 0; color-scheme: normal; }
</style>
