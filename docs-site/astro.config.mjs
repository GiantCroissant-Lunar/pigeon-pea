import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// Astro configuration for the Pigeon Pea documentation site using
// Astro's official Starlight docs theme.
export default defineConfig({
  integrations: [
    starlight({
      title: 'Pigeon Pea Docs',
      description: 'Project documentation for the Pigeon Pea repository.',
      sidebar: [
        { label: 'RFCs', autogenerate: { directory: 'rfc' } },
        { label: 'Guides', autogenerate: { directory: 'guide' } },
        {
          label: 'Architecture Decision Records',
          autogenerate: { directory: 'adr' },
        },
        { label: 'Plans', autogenerate: { directory: 'plan' } },
        { label: 'Specifications', autogenerate: { directory: 'spec' } },
        { label: 'Reference', autogenerate: { directory: 'reference' } },
      ],
    }),
  ],
  srcDir: './src',
  outDir: './dist',
});
