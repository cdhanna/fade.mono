// Every picture Steamworks asks for, its size, and the file that build/capsule_cut.py makes for it
// (build/capsules.json). From partner.steamgames.com/doc/store/assets. The event art and the icons are not
// on that page: they are the sizes Steamworks' own upload boxes ask for.

export const STORE_ART = [
  { box: 'Header Capsule', file: 'header_capsule.png', w: 920, h: 430 },
  { box: 'Small Capsule', file: 'small_capsule.png', w: 462, h: 174, note: 'The logo must nearly fill it.' },
  { box: 'Main Capsule', file: 'main_capsule.png', w: 1232, h: 706 },
  { box: 'Vertical Capsule', file: 'vertical_capsule.png', w: 748, h: 896 },
  { box: 'Page Background', file: 'page_background.png', w: 1438, h: 810, note: 'Optional.' },
];

export const LIBRARY_ART = [
  { box: 'Library Capsule', file: 'library_capsule.png', w: 600, h: 900 },
  { box: 'Library Header', file: 'library_header.png', w: 920, h: 430 },
  { box: 'Library Hero', file: 'library_hero.png', w: 3840, h: 1240, note: 'No text.' },
  { box: 'Library Logo', file: 'library_logo.png', w: 1280, h: 720, alpha: true, note: 'Transparent.' },
];

export const OTHER_ART = [
  { box: 'Community Icon', file: 'community_icon.jpg', w: 184, h: 184, jpg: true },
  { box: 'Shortcut Icon', file: 'shortcut_icon.ico', w: 256, h: 256, ico: true },
  { box: 'Event Header', file: 'event_header.png', w: 1920, h: 622, note: 'For announcements.' },
  { box: 'Event Cover', file: 'event_cover.png', w: 800, h: 450, note: 'For announcements.' },
];
