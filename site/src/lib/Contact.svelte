<script>
  // Where to write: support and press, one address (contact in site.js). The page is drawn by script, so
  // the address is never in the HTML a scraper reads without running it.
  import { site } from './site.js';
</script>

{#if site.contact}
  <section class="section" id="contact">
    <div class="wrap">
      <div class="note panel">
        <h2>Contact</h2>
        <p>Found a bug? Got a question? Writing about the game? Send me a note.</p>
        <a class="cta" href="mailto:{site.contact}?subject={encodeURIComponent(site.name)}">{site.contact}</a>
        {#if site.discord}
          <p class="or"><a class="item" href={site.discord} target="_blank" rel="noopener">Join the Discord</a></p>
        {/if}
      </div>
    </div>
  </section>
{/if}

<style>
  .note {
    max-width: 760px;
    margin: 0 auto;
    padding: clamp(28px, 6vw, 52px) clamp(16px, 5vw, 48px);
    text-align: center;
  }
  h2 { margin-bottom: 14px; }
  p { margin: 0 auto 26px; }
  p.or { margin: 26px 0 0; }
  /* an address has to be read exactly */
  .cta {
    font-family: var(--body);
    font-weight: 700;
    text-transform: none;
    font-size: clamp(1.05rem, 4vw, 1.35rem);
    max-width: 100%;
    overflow-wrap: anywhere;
  }
</style>
