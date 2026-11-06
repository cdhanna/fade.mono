<script>
  import { site, steamStore, asset } from './site.js';

  // home: the links are the page's sections; on the press page they go back to them
  let { home = true } = $props();
  let solid = $state(false);
  let open = $state(false);
  // the round bar of the game, filling up as the page goes down
  let progress = $state(0);

  $effect(() => {
    const on = () => {
      solid = scrollY > 40;
      const room = document.documentElement.scrollHeight - innerHeight;
      progress = room > 0 ? scrollY / room : 0;
    };
    on();
    addEventListener('scroll', on, { passive: true });
    addEventListener('resize', on);
    return () => {
      removeEventListener('scroll', on);
      removeEventListener('resize', on);
    };
  });

  const links = $derived.by(() => {
    const base = home ? '' : asset('');
    return [
      ['Trailer', `${base}#trailer`],
      ['About', `${base}#about`],
      ['Screens', `${base}#screens`],
      ['Press kit', asset('press/')],
    ];
  });
</script>

<header class:solid class:away={!home}>
  <nav class="wrap" aria-label="Main">
    <a class="mark" href={home ? '#top' : asset('')}>{site.name}</a>
    <button class="menu" aria-expanded={open} aria-controls="nav-links" onclick={() => (open = !open)}>
      <span class="visually-hidden">Menu</span>
      <span class="bars" aria-hidden="true"></span>
    </button>
    <ul id="nav-links" class:open>
      {#each links as [label, href]}
        <li><a class="item" {href} onclick={() => (open = false)}>{label}</a></li>
      {/each}
      {#if site.discord}
        <li><a class="item" href={site.discord} target="_blank" rel="noopener">Discord</a></li>
      {/if}
      <li><a class="item steam" href={steamStore} target="_blank" rel="noopener">Steam</a></li>
    </ul>
  </nav>
  <div class="bar" aria-hidden="true"><span style:transform="scaleX({progress})"></span></div>
</header>

<style>
  header {
    position: fixed;
    top: 0; left: 0; right: 0;
    z-index: 10;
    transition: background 0.4s;
  }
  header.solid { background: rgba(6, 16, 38, 0.86); }
  nav {
    display: flex;
    align-items: center;
    justify-content: space-between;
    height: 60px;
  }
  .mark {
    font-family: var(--display);
    font-size: 1.6rem;
    text-transform: uppercase;
    color: var(--foam);
    text-decoration: none;
    text-shadow: 2px 2px 0 var(--shadow);
    opacity: 0;
    transition: opacity 0.4s;
  }
  /* on the home page the name shows once the hero's has scrolled away; elsewhere always */
  header.solid .mark, header.away .mark { opacity: 1; }
  ul {
    display: flex;
    align-items: center;
    gap: 2px;
    margin: 0;
    padding: 0;
    list-style: none;
  }
  ul .item { font-size: 1.2rem; }
  .steam { color: var(--gold); }
  .bar { height: 3px; background: rgba(64, 128, 255, 0.18); }
  .bar span {
    display: block;
    height: 100%;
    background: var(--hud);
    transform-origin: left;
  }
  .menu {
    display: none;
    width: 44px; height: 44px;
    background: none;
    border: 0;
    cursor: pointer;
  }
  .bars, .bars::before, .bars::after {
    display: block;
    width: 24px; height: 3px;
    margin: 0 auto;
    background: var(--foam);
    position: relative;
  }
  .bars::before, .bars::after { content: ''; position: absolute; left: 0; }
  .bars::before { top: -8px; }
  .bars::after { top: 8px; }

  @media (max-width: 760px) {
    .mark { opacity: 1; }
    .menu { display: block; }
    ul {
      display: none;
      position: absolute;
      top: 60px; left: 0; right: 0;
      flex-direction: column;
      padding: 12px 0 20px;
      background: rgba(6, 16, 38, 0.96);
    }
    ul.open { display: flex; }
  }
</style>
