<script>
  // The site, both pages of it: the home page and the press kit, at / and /press/. The backdrop, the nav
  // and the footer stay; between the pages only the page in <main> changes, with no new load, so the
  // backdrop (whose shaders take a moment to compile) goes on as it was. A link to the other page goes
  // into the history as a page would; the old page fades up and away, and the new one comes up into its
  // place (app.css). press/index.html is the same app, for a link straight to the press kit.
  import { onMount } from 'svelte';
  import Backdrop from './lib/Backdrop.svelte';
  import Nav from './lib/Nav.svelte';
  import Footer from './lib/Footer.svelte';
  import Home from './Home.svelte';
  import Press from './Press.svelte';
  import { site } from './lib/site.js';

  const routeOf = (path) => (/\/press\/?$/.test(path) ? 'press' : 'home');
  const TITLE = { home: `${site.name} · ${site.tagline}`, press: `Press Kit · ${site.name}` };
  // how long the old page takes to go (app.css, main.leaving)
  const OUT_MS = 260;

  let route = $state(routeOf(location.pathname));
  let main;
  let busy = false;

  // to the page at url: the other page, or a place on this one
  async function go(url) {
    const next = routeOf(url.pathname);
    if (next === route) {
      jump(url.hash);
      return;
    }
    if (busy) return;
    busy = true;
    const still = matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (!still) {
      main.classList.add('leaving');
      await new Promise((r) => setTimeout(r, OUT_MS));
    }
    route = next;
    document.title = TITLE[next];
    // the new page laid out where the old one went, then up into its place
    await new Promise((r) => requestAnimationFrame(r));
    main.classList.remove('leaving');
    if (!still) main.classList.add('coming');
    jump(url.hash, true);
    if (!still) {
      main.getBoundingClientRect();
      main.classList.remove('coming');
    }
    busy = false;
  }

  function jump(hash, top = false) {
    const el = hash && document.getElementById(decodeURIComponent(hash.slice(1)));
    if (el) el.scrollIntoView({ behavior: top ? 'instant' : 'smooth' });
    else if (top) scrollTo({ top: 0, behavior: 'instant' });
  }

  onMount(() => {
    // a click on a link to the other page of the site
    const onClick = (e) => {
      if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
      const a = e.target.closest?.('a[href]');
      if (!a || a.target === '_blank' || a.hasAttribute('download')) return;
      const url = new URL(a.href, location.href);
      if (url.origin !== location.origin || routeOf(url.pathname) === route) return;
      e.preventDefault();
      history.pushState({}, '', url.href);
      go(url);
    };
    const onPop = () => go(new URL(location.href));
    addEventListener('click', onClick);
    addEventListener('popstate', onPop);
    return () => {
      removeEventListener('click', onClick);
      removeEventListener('popstate', onPop);
    };
  });
</script>

<Backdrop deep={route === 'press'} />
<Nav home={route === 'home'} />
<main bind:this={main}>
  {#if route === 'press'}
    <Press />
  {:else}
    <Home />
  {/if}
</main>
<Footer />
