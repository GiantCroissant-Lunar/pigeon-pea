#!/usr/bin/env python3
"""
Universal MCP Project Launcher

This script enables per-project MCP server configurations for editors
that only support global configs (like GitHub Copilot).

How it works:
1. Reads the current working directory (project root)
2. Loads project-specific MCP server configs from `.mcp/servers/`
3. Launches the appropriate MCP server based on the config

Setup:
1. Copy this script to: ~/.local/mcp-launcher/launcher.py (Linux/Mac)
   or %USERPROFILE%\\.local\\mcp-launcher\\launcher.py (Windows)
2. Make it executable: chmod +x ~/.local/mcp-launcher/launcher.py
3. Configure your editor's global MCP config to point to this script

Example global config (GitHub Copilot):
~/.config/github-copilot/mcp/servers.json:
{
  "mcp-project-launcher": {
    "command": "python3",
    "args": ["~/.local/mcp-launcher/launcher.py", "github"]
  }
}

The second argument ("github") specifies which server config to load from
`.mcp/servers/github.json` in your project.
"""

import json
import os
import re
import sys
from pathlib import Path


def expand_env_vars(value, project_root):
    """Expand environment variables in strings.

    Supports both ${VAR} and $VAR syntax.
    Special variable: ${PROJECT_ROOT} expands to the project root path.
    """
    if not isinstance(value, str):
        return value

    # Handle ${PROJECT_ROOT}
    value = value.replace("${PROJECT_ROOT}", str(project_root))

    # Handle ${VAR} syntax
    def replace_var(match):
        var_name = match.group(1)
        return os.environ.get(var_name, match.group(0))

    value = re.sub(r"\$\{([^}]+)\}", replace_var, value)

    # Handle $VAR syntax (without braces)
    value = re.sub(
        r"\$([A-Za-z_][A-Za-z0-9_]*)",
        lambda m: os.environ.get(m.group(1), m.group(0)),
        value,
    )

    return value


def expand_env_vars_recursive(obj, project_root):
    """Recursively expand environment variables in config objects."""
    if isinstance(obj, dict):
        return {k: expand_env_vars_recursive(v, project_root) for k, v in obj.items()}
    elif isinstance(obj, list):
        return [expand_env_vars_recursive(item, project_root) for item in obj]
    elif isinstance(obj, str):
        return expand_env_vars(obj, project_root)
    else:
        return obj


def find_project_root():
    """Find the project root directory.

    Returns the current working directory, which should be the project
    the editor is working in.
    """
    return Path.cwd()


def load_server_config(project_root, server_name=None):
    """Load MCP server configuration from project.

    Args:
        project_root: Path to project root
        server_name: Name of server to load (without .json extension)
                    If None, loads the first available server config

    Returns:
        Tuple of (config_dict, config_path)
    """
    config_dir = project_root / ".mcp" / "servers"

    if not config_dir.exists():
        print(
            f"ERROR: No .mcp/servers directory found in {project_root}", file=sys.stderr
        )
        print(f"Expected path: {config_dir}", file=sys.stderr)
        sys.exit(1)

    # Find config file
    if server_name:
        config_path = config_dir / f"{server_name}.json"
        if not config_path.exists():
            print(f"ERROR: Server config '{server_name}' not found", file=sys.stderr)
            print(f"Expected path: {config_path}", file=sys.stderr)
            print("\nAvailable configs:", file=sys.stderr)
            for cfg in config_dir.glob("*.json"):
                if not cfg.name.startswith("_"):
                    print(f"  - {cfg.stem}", file=sys.stderr)
            sys.exit(1)
    else:
        # Load first available config (excluding templates)
        configs = [f for f in config_dir.glob("*.json") if not f.name.startswith("_")]
        if not configs:
            print(f"ERROR: No server configs found in {config_dir}", file=sys.stderr)
            sys.exit(1)
        config_path = configs[0]

    # Load and parse config
    try:
        with open(config_path, "r", encoding="utf-8") as f:
            config = json.load(f)
    except json.JSONDecodeError as e:
        print(f"ERROR: Invalid JSON in {config_path}: {e}", file=sys.stderr)
        sys.exit(1)

    return config, config_path


def launch_server(config, project_root):
    """Launch the MCP server based on configuration.

    Args:
        config: Server configuration dictionary
        project_root: Path to project root
    """
    # Expand environment variables in config
    config = expand_env_vars_recursive(config, project_root)

    # Extract command and args
    command = config.get("command")
    args = config.get("args", [])
    env_vars = config.get("env", {})
    cwd = config.get("cwd", str(project_root))

    if not command:
        print("ERROR: No 'command' specified in server config", file=sys.stderr)
        sys.exit(1)

    # Prepare environment
    env = os.environ.copy()
    env.update(env_vars)

    # Expand environment variables in cwd
    cwd = expand_env_vars(cwd, project_root)

    # Build full command
    full_command = [command] + args

    # Debug output (optional - comment out for production)
    print(f"[MCP Launcher] Starting: {config.get('name', 'unknown')}", file=sys.stderr)
    print(f"[MCP Launcher] Command: {' '.join(full_command)}", file=sys.stderr)
    print(f"[MCP Launcher] CWD: {cwd}", file=sys.stderr)

    # Launch the server
    try:
        # Use os.execvpe to replace current process with the server
        # This ensures the server runs as if it was launched directly
        os.execvpe(command, full_command, env)
    except FileNotFoundError:
        print(f"ERROR: Command not found: {command}", file=sys.stderr)
        print("Make sure the required tools are installed", file=sys.stderr)
        sys.exit(1)
    except Exception as e:
        print(f"ERROR: Failed to launch server: {e}", file=sys.stderr)
        sys.exit(1)


def main():
    """Main entry point."""
    # Parse arguments
    server_name = sys.argv[1] if len(sys.argv) > 1 else None

    # Find project root
    project_root = find_project_root()

    # Load server config
    config, config_path = load_server_config(project_root, server_name)

    # Launch the server
    launch_server(config, project_root)


if __name__ == "__main__":
    main()
