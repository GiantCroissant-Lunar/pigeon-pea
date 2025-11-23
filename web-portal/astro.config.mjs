import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import react from '@astrojs/react';
import node from '@astrojs/node';

export default defineConfig({
  output: 'server', // Required for WebSocket support

  adapter: node({
    mode: 'standalone',
  }),

  integrations: [
    react(), // For interactive components (WorkspaceManager, etc.)

    starlight({
      title: 'PigeonPea',
      description: 'Roguelike Game • Documentation • Interactive Portal',

      customCss: ['./src/styles/terminal.css', './src/styles/portal.css'],

      sidebar: [
        {
          label: '🎮 Play',
          items: [
            { label: 'Browser Terminal', link: '/play' },
            { label: 'Recordings Gallery', link: '/recordings' },
          ],
        },
        {
          label: '📚 Documentation',
          items: [
            { label: 'Getting Started', link: '/docs/getting-started' },
            {
              label: 'RFCs',
              autogenerate: { directory: 'rfc' },
              collapsed: true,
            },
            {
              label: 'Guides',
              autogenerate: { directory: 'guide' },
              collapsed: true,
            },
            {
              label: 'ADRs',
              autogenerate: { directory: 'adr' },
              collapsed: true,
            },
          ],
        },
      ],

      components: {
        Header: './src/components/layout/CustomHeader.astro',
      },

      social: {
        github: 'https://github.com/your-org/pigeon-pea',
      },

      favicon: '/favicon.svg',
    }),
  ],

  vite: {
    optimizeDeps: {
      exclude: ['node-pty'],
    },
  },
});
