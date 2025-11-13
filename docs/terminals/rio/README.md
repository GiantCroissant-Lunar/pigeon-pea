# Rio on Windows — Using bundle/config/rio.toml

This note shows how to point Rio to the repo’s dev bundle config on Windows.

## Side‑by‑side (recommended for development)

- Place generated fonts under `bundle/fonts/` (use `task bundle:rio:dev`).
- Keep `bundle/config/rio.toml` as generated:

```toml
[fonts]
normal = { family = "Lunar Sarasa Mono" }
bold   = { family = "Lunar Sarasa Mono" }
italic = { family = "Lunar Sarasa Mono" }
```

### Launching Rio with a custom config

If Rio supports a `--config` flag (or similar), point it to `bundle/config/rio.toml`:

```cmd
rio.exe --config "D:\\path\\to\\repo\\bundle\\config\\rio.toml"
```

If not, copy `rio.toml` into Rio’s default config directory for Windows (check Rio docs), or install the TTFs system‑wide and select the family in Rio settings.

## Troubleshooting

- If PUA glyphs render as tofu boxes, the custom font isn’t active. Ensure the family name matches the generated fonts and that fallback fonts aren’t overriding.
- After installing fonts system‑wide, you may need to restart Rio or clear font caches.
