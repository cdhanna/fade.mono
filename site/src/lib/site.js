// What the site says, and what the Steam page says: npm run steam writes steam/store-*.txt from this file,
// so the two never drift. The name, the date and the tagline are the trailer's last card and the caption
// before it (build/trailer_captions.py, fish_tests_trailer.fbasic): keep them in step.

export const site = {
  name: 'Fish Fish Pop',
  tagline: 'Relax, you cannot lose.',
  // the public address, for the link previews (og:image wants it whole). Cloudflare Pages, or a custom
  // domain once there is one.
  url: 'https://fish-fish-pop.pages.dev/',
  steamAppId: '5416340',
  developer: 'Chris Hanna',
  developerUrl: '',
  // support and press. Empty leaves it out everywhere. Make it a forwarding address: it is public.
  contact: '',
  // the community; empty leaves it out
  discord: '',
  release: 'December 2, 2026',
  platforms: ['Windows', 'macOS (Apple Silicon)'],
  players: '1',
  // the game's languages (strings.sheet.json), by their own names (lang.csv)
  languages: ['English', 'Português (Brasil)', 'Français', 'Deutsch', 'Polski', 'Español', 'Русский', 'Türkçe', '日本語', '简体中文', '한국어'],
  price: 'TBA',

  // THE DESCRIPTION. Chris's own voice: first person, plain, a little wry, short sentences.
  // Steam's short description, 300 characters at most
  short: 'A slow, cozy match-three under the sea. Swap fish, pop schools, pick a new card every round, and help out the locals. Thirty rounds, no timer, and you can\'t lose. The only question is how few moves you need. Oh, and the sea has secrets.',

  // each with its clip (build/steam_media.json, capsules/clips/), or none
  sections: [
    {
      title: 'Swap. Pop. Take your time.',
      clip: 'swap_pop',
      lines: ['I wanted a match-three you can\'t lose. There\'s no timer and nothing chasing you. Swap two fish, make a group of three, and watch the combo roll. Thirty rounds, and the only question is how few moves it takes you.'],
    },
    {
      title: 'A new card every round',
      clip: 'cards',
      lines: ['Between rounds you pick a card, and the card changes the rules. Stingrays shoot up their column. Jellyfish sting. A diver takes pictures of the crabs. Stack them up and find a hand that plays the way you like.'],
    },
    {
      title: 'Meet the locals',
      clip: 'quests',
      lines: ['A shark, a tangled tail, and a lot more. Everybody on the reef wants a favor, and every favor plays a little differently. Some of them ask nicer than others.'],
    },
    {
      title: 'The sea keeps its secrets',
      clip: 'night',
      lines: [
        'There\'s a lot going on down here that the game won\'t tell you about. Some of it you\'ll trip over. Some of it you\'ll have to go looking for. And once you find it, the way you play changes.',
        'Songs grow coral. Fish sing. The night comes and nothing pops. There are secret words, too. And there are more than five endings. I\'m not going to tell you how to get them.',
      ],
    },
  ],

  features: [
    '30 rounds, no timer, and you can\'t lose',
    'A new card every round that changes the rules',
    'Favors for the creatures of the reef',
    'Hidden mechanics that change how you play',
    'More than five endings',
    'Day, night, and something in the deep',
    'Leaderboards',
    'Keyboard, mouse or controller',
    '11 languages',
  ],

  // Steam's system requirements. Windows and macOS are what the workflow builds; the rest is a draft
  // for a 2D game of this size. Check it on the slowest machine you have.
  requirements: {
    windows: {
      os: 'Windows 10 64-bit',
      processor: 'Dual-core 2 GHz',
      memory: '4 GB RAM',
      graphics: 'Integrated graphics with OpenGL 3.3',
      storage: '400 MB available space',
    },
    mac: {
      os: 'macOS 12 Monterey',
      processor: 'Apple Silicon (M1 or newer)',
      memory: '8 GB RAM',
      graphics: 'Apple Silicon integrated graphics',
      storage: '400 MB available space',
    },
  },

  // the tags to pick in Steamworks, the first few weighing most
  tags: ['Match 3', 'Puzzle', 'Relaxing', 'Cozy', 'Casual', 'Pixel Graphics', 'Card Game', 'Singleplayer', 'Underwater', 'Indie', 'Colorful', 'Cute', '2D', 'Atmospheric', 'Controller'],

  // the credits, as the game rolls them (fish_routines_credits.fbasic). [words, link]; no link is just the words.
  roll: [
    { role: 'Created by', names: [['Chris Hanna']] },
    { role: 'Dedicated to my wife,', names: [['Ana']] },
    { role: 'Fish art by', names: [['Ana Hanna']] },
    { role: 'Additional fish art by', names: [['pixelGnome', 'https://pixelgnome.itch.io/fish']] },
    { role: 'Diver by', names: [['dkproductions', 'https://dkproductions.itch.io/pixel-art-diver']] },
    {
      role: 'Music adapted from the works of',
      names: [['Claude Debussy'], ['Erik Satie'], ['Frederic Chopin'], ['Ludwig van Beethoven'], ['Pyotr Ilyich Tchaikovsky'], ['Sergei Rachmaninoff'], ['Gustav Holst']],
    },
    { role: 'Font', names: [['Zero Cool', 'https://ggbot.net']], note: [['GGBotNet', 'https://ggbot.net'], ['SIL Open Font License', 'https://scripts.sil.org/OFL']] },
    { role: 'Made with', names: [['Fade Basic', 'https://fadebasic.com'], ['MonoGame', 'https://monogame.net']] },
  ],
};

export const steamStore = `https://store.steampowered.com/app/${site.steamAppId}/`;
export const steamWidget = `https://store.steampowered.com/widget/${site.steamAppId}/`;

// a path under the site's base, for what is in public/. (npm run steam runs this file in plain node.)
export const asset = (p) => (import.meta.env?.BASE_URL ?? '/') + p;
