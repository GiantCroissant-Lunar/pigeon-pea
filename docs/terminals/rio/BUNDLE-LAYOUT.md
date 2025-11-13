# Rio Bundle Layout (Template)

This folder structure is a template for assembling a developer or player bundle. Do not check in Rio binaries.

```
bundle/
  rio/
    (rio.exe and related files)        # user-provided
  fonts/
    LunarSarasaMono-Regular.ttf
    LunarSarasaMono-Bold.ttf
  config/
    rio.toml
  app/
    (your game binaries / scripts)
  README.md
```

`config/rio.toml` should reference the font family:

```
[fonts]
normal = { family = "Lunar Sarasa Mono" }
bold   = { family = "Lunar Sarasa Mono" }
```

Add LICENSE files for fonts you distribute.

