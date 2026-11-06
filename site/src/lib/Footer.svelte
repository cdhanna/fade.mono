<script>
  import { site, steamStore, asset } from './site.js';
</script>

<footer>
  <div class="wrap">
    <div class="rule"><span></span></div>
    <p class="name">{site.name}</p>
    <p class="links">
      <a href={steamStore} target="_blank" rel="noopener">Steam</a>
      <a href={asset('press/')}>Press kit</a>
      {#if site.discord}<a href={site.discord} target="_blank" rel="noopener">Discord</a>{/if}
      {#if site.contact}<a href="mailto:{site.contact}">Contact</a>{/if}
    </p>
    <p class="small">© {new Date().getFullYear()} {#if site.developerUrl}<a href={site.developerUrl} target="_blank" rel="noopener">{site.developer}</a>{:else}{site.developer}{/if}</p>
  </div>
</footer>

<style>
  footer { padding: 36px 0 48px; text-align: center; font-size: 1rem; }
  .rule { margin-bottom: 24px; }
  .name {
    font-family: var(--display);
    font-size: 2rem;
    text-transform: uppercase;
    color: var(--foam);
    text-shadow: 3px 3px 0 var(--shadow);
    margin-bottom: 6px;
  }
  .links { display: flex; justify-content: center; gap: 22px; }
</style>
