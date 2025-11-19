# Yazi Integration for Dungeon Dev Tool

This document describes how to integrate the `dev-tool` CLI with Yazi for a seamless dungeon development workflow.

## Overview

The integration allows you to:
- Launch dungeon commands directly from Yazi
- Edit game data files with automatic hot-reload
- Use Yazi as your primary file browser for dungeon assets
- Stream game logs and state updates while browsing files

## Setup

### 1. Install the Dev Tool

```bash
cargo install --path dev-tool
```

### 2. Configure Yazi

Add to your `~/.config/yazi/keymap.toml`:

```toml
[manager]
keymap = [
  { on = [ "d" ], run = "spawn 'dev-tool --server ws://localhost:5007' --", desc = "Open dungeon dev tool" },
  { on = [ "D" ], run = "spawn 'dev-tool --server ws://localhost:5007 --'", desc = "Open dev tool with current file" },
  { on = [ "g", "s" ], run = "spawn 'dev-tool spawn goblin --x 10 --y 5 --server ws://localhost:5007'", desc = "Spawn goblin" },
  { on = [ "g", "t" ], run = "spawn 'dev-tool teleport --x 20 --y 15 --server ws://localhost:5007'", desc = "Teleport player" },
  { on = [ "g", "r" ], run = "spawn 'dev-tool reload --server ws://localhost:5007'", desc = "Reload game" },
  { on = [ "g", "l" ], run = "spawn 'dev-tool log --follow --server ws://localhost:5007'", desc = "Follow game logs" },
]
```

### 3. Create Yazi Plugin for Dungeon Commands

Create `~/.config/yazi/plugins/dungeon.yazi/init.lua`:

```lua
local function setup()
	-- Add dungeon-specific file preview handlers
	ya.add_custom_previewer("dungeon_map", function(path)
		return ya.preview_image(path, ya.image_size_for("width=80,height=24"))
	end)
	
	-- Add dungeon commands to the command line
	ya.add_custom_command("dungeon-spawn", function(args)
		local entity = args[1] or "goblin"
		local x = args[2] or "10"
		local y = args[3] or "5"
		ya.spawn(string.format("dev-tool spawn %s --x %s --y %s --server ws://localhost:5007", entity, x, y))
	end)
	
	ya.add_custom_command("dungeon-teleport", function(args)
		local x = args[1] or "0"
		local y = args[2] or "0"
		ya.spawn(string.format("dev-tool teleport --x %s --y %s --server ws://localhost:5007", x, y))
	end)
end

return { setup = setup }
```

## Usage Examples

### Basic Workflow

1. **Start your game** with WebSocket server enabled
2. **Open Yazi** in your dungeon data directory
3. **Press `d`** to open the dev tool
4. **Edit JSON files** and they'll auto-reload in the game
5. **Use keybindings** for common GM commands

### File-Based Operations

When you have a map file selected in Yazi:

- Press `D` to open dev tool with that file pre-loaded
- Use `:dungeon-spawn goblin 15 20` to spawn at specific coordinates
- Use `:dungeon-teleport 10 10` to teleport player

### Log Monitoring

- Press `g, l` to start following game logs
- Logs appear in a split terminal while you continue browsing
- Filter logs by level: `dev-tool log --level error --follow`

### State Inspection

```bash
# Monitor player state
dev-tool state --filter player --follow

# Watch specific entities
dev-tool state --filter enemies --follow

# Get full state dump
dev-tool state --format json
```

## Advanced Configuration

### Rio Terminal Integration

For the best experience, use Rio terminal with this layout:

```toml
# ~/.config/rio/config.toml
[profiles.dungeon]
font-family = "JetBrainsMono Nerd Font"
font-size = 14
padding-x = 8
padding-y = 8

[bindings]
keys = [
  { key = "F5", action = "OpenWindow", args = { profile = "dungeon", directory = "~/dungeon", command = "dotnet run" } },
  { key = "F6", action = "OpenWindow", args = { profile = "dungeon", directory = "~/dungeon/data", command = "yazi" } },
  { key = "F7", action = "OpenWindow", args = { profile = "dungeon", directory = "~/dungeon", command = "dev-tool log --follow" } },
]
```

### Multiple Game Instances

For multiple game instances, configure different server ports:

```bash
# Development instance
dev-tool --server ws://localhost:5007

# Testing instance  
dev-tool --server ws://localhost:5008

# Production instance
dev-tool --server ws://localhost:5009
```

## File Type Integration

### Map Files (.json, .map)

Yazi will automatically preview map files with the dungeon previewer. Edit them and the game will hot-reload.

### Entity Definitions (.entity, .json)

Select entity files and press `e` to spawn them in the current map.

### Configuration Files (.toml, .yaml)

Edit config files and use `dev-tool reload` to apply changes without restarting.

## Troubleshooting

### Connection Issues

If the dev tool can't connect:

1. Verify the game is running with WebSocket server enabled
2. Check the port matches (default: 5007)
3. Ensure no firewall is blocking the connection

### Hot Reload Not Working

1. Verify the game has FileSystemWatcher enabled
2. Check file paths match the game's data directory
3. Use `dev-tool reload` as a fallback

### Performance Issues

1. Use `--format compact` for faster log output
2. Filter logs with `--level info` or higher
3. Limit state monitoring with `--filter`

## Example Commands

```bash
# Spawn multiple entities
dev-tool spawn goblin --x 10 --y 5
dev-tool spawn orc --x 15 --y 8  
dev-tool spawn treasure --x 20 --y 12

# Teleport and inspect
dev-tool teleport --x 25 --y 30
dev-tool state --filter player

# Map operations
dev-tool load-map dungeon_level_3.json
dev-tool regen-map --seed 12345 --size 50x50

# Log monitoring
dev-tool log --level debug --follow
dev-tool log --filter combat --since 1h

# Raw commands
dev-tool send '{"type":"custom","action":"clear_inventory"}'
```

This integration provides a powerful, keyboard-driven development environment that combines the file management strengths of Yazi with the real-time control capabilities of the dungeon dev tool.
