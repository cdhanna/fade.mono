import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import { resolve } from 'node:path';
import { site } from './src/lib/site.js';

// the pages' titles and link previews come from src/lib/site.js
const fill = {
  name: 'site-meta',
  transformIndexHtml: (html) =>
    html
      .replaceAll('%NAME%', site.name)
      .replaceAll('%TAGLINE%', site.tagline)
      .replaceAll('%SHORT%', site.short.replaceAll('"', '&quot;'))
      .replaceAll('%SITE_URL%', site.url),
};

export default defineConfig({
  plugins: [svelte(), fill],
  build: {
    rollupOptions: {
      input: {
        main: resolve(import.meta.dirname, 'index.html'),
        press: resolve(import.meta.dirname, 'press/index.html'),
      },
    },
  },
});
