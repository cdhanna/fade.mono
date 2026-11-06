<script>
  // The credits, as the game rolls them: each role above in the dim blue, its names under it.
  import { site } from './site.js';
</script>

<section class="section" id="credits">
  <div class="wrap">
    <div class="heading">
      <h2>Credits</h2>
      <div class="rule"><span></span></div>
    </div>
    <dl class="roll">
      {#each site.roll as c}
        <div class="row">
          <dt>{c.role}</dt>
          <dd>
            {#each c.names as [n, url]}
              {#if url}<a class="name" href={url} target="_blank" rel="noopener">{n}</a>{:else}<span class="name">{n}</span>{/if}
            {/each}
            {#if c.note}
              <span class="note">
                {#each c.note as [t, url], i}{i ? ', ' : ''}{#if url}<a href={url} target="_blank" rel="noopener">{t}</a>{:else}{t}{/if}{/each}
              </span>
            {/if}
          </dd>
        </div>
      {/each}
    </dl>
  </div>
</section>

<style>
  .roll { max-width: 820px; margin: 0 auto; display: grid; gap: 26px; text-align: center; }
  dt {
    font-family: var(--display);
    font-size: 1.05rem;
    text-transform: uppercase;
    color: var(--mist);
    text-shadow: 2px 2px 0 var(--shadow);
  }
  dd { margin: 2px 0 0; display: flex; flex-direction: column; gap: 2px; }
  .name {
    font-family: var(--display);
    font-size: 1.75rem;
    line-height: 1.15;
    text-transform: uppercase;
    color: var(--foam);
    text-shadow: 3px 3px 0 var(--shadow);
  }
  a.name { text-decoration: none; }
  a.name:hover, a.name:focus-visible { color: var(--gold); }
  .note { color: var(--dim); font-size: 0.95rem; }
  .note a { color: inherit; }
  .note a:hover { color: var(--gold); }
</style>
