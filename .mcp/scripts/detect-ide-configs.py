#!/usr/bin/env python3
"""
IDE MCP Config Path Detection Script

This script detects which AI coding assistants/IDEs are installed and
locates their global MCP server configuration paths.

Supports:
- GitHub Copilot (VS Code Extension)
- Cursor
- Windsurf
- Cline (VS Code Extension)
- Zed
- Claude Desktop (for reference)

Usage:
    python detect-ide-configs.py [--json] [--verbose]

    --json      Output results as JSON
    --verbose   Show detailed detection info
"""

import os
import sys
import json
import platform
from pathlib import Path
from typing import Dict, List, Optional, Tuple


class IDEConfigDetector:
    """Detects installed IDEs and their MCP config paths."""

    def __init__(self, verbose: bool = False):
        self.verbose = verbose
        self.os_name = platform.system().lower()
        self.home = Path.home()

    def log(self, message: str):
        """Print verbose log message."""
        if self.verbose:
            print(f"[DEBUG] {message}", file=sys.stderr)

    def get_config_paths(self) -> Dict[str, Dict[str, any]]:
        """Get all IDE config paths based on OS.

        Returns:
            Dict mapping IDE name to config info:
            {
                "ide_name": {
                    "installed": bool,
                    "global_config_path": str or None,
                    "config_exists": bool,
                    "supports_project_config": bool,
                    "project_config_path": str or None (relative to project root)
                }
            }
        """
        detectors = {
            "GitHub Copilot": self._detect_github_copilot,
            "Cursor": self._detect_cursor,
            "Windsurf": self._detect_windsurf,
            "Cline": self._detect_cline,
            "Zed": self._detect_zed,
            "Claude Desktop": self._detect_claude_desktop,
            "VS Code": self._detect_vscode,
        }

        results = {}
        for ide_name, detector in detectors.items():
            self.log(f"Detecting {ide_name}...")
            results[ide_name] = detector()

        return results

    def _check_path_exists(self, path: Optional[Path]) -> Tuple[bool, bool]:
        """Check if path and its parent directory exist.

        Returns:
            (parent_exists, file_exists)
        """
        if path is None:
            return False, False

        parent_exists = path.parent.exists()
        file_exists = path.exists() if parent_exists else False
        return parent_exists, file_exists

    def _detect_github_copilot(self) -> Dict:
        """Detect GitHub Copilot (VS Code extension)."""
        if self.os_name == "windows":
            config_path = self.home / ".config" / "github-copilot" / "mcp" / "servers.json"
        elif self.os_name == "darwin":  # macOS
            config_path = self.home / ".config" / "github-copilot" / "mcp" / "servers.json"
        else:  # Linux
            config_path = self.home / ".config" / "github-copilot" / "mcp" / "servers.json"

        parent_exists, config_exists = self._check_path_exists(config_path)

        # Check if VS Code is installed (indirect detection)
        vscode_installed = self._is_vscode_installed()

        return {
            "installed": vscode_installed or parent_exists,
            "global_config_path": str(config_path) if config_path else None,
            "config_exists": config_exists,
            "supports_project_config": False,
            "project_config_path": None,
            "notes": "Requires VS Code with GitHub Copilot extension"
        }

    def _detect_cursor(self) -> Dict:
        """Detect Cursor IDE."""
        if self.os_name == "windows":
            config_path = self.home / "AppData" / "Roaming" / "Cursor" / "User" / "globalStorage" / "mcp" / "servers.json"
            install_path = self.home / "AppData" / "Local" / "Programs" / "Cursor"
        elif self.os_name == "darwin":  # macOS
            config_path = self.home / "Library" / "Application Support" / "Cursor" / "User" / "globalStorage" / "mcp" / "servers.json"
            install_path = Path("/Applications/Cursor.app")
        else:  # Linux
            config_path = self.home / ".config" / "Cursor" / "User" / "globalStorage" / "mcp" / "servers.json"
            install_path = self.home / ".local" / "share" / "Cursor"

        parent_exists, config_exists = self._check_path_exists(config_path)
        installed = install_path.exists() or parent_exists

        return {
            "installed": installed,
            "global_config_path": str(config_path) if config_path else None,
            "config_exists": config_exists,
            "supports_project_config": True,
            "project_config_path": ".cursor/mcp.json",
            "notes": "Supports both global and project-level MCP configs"
        }

    def _detect_windsurf(self) -> Dict:
        """Detect Windsurf IDE."""
        if self.os_name == "windows":
            config_path = self.home / "AppData" / "Roaming" / "Windsurf" / "mcp" / "servers.json"
            install_path = self.home / "AppData" / "Local" / "Programs" / "Windsurf"
        elif self.os_name == "darwin":  # macOS
            config_path = self.home / "Library" / "Application Support" / "Windsurf" / "mcp" / "servers.json"
            install_path = Path("/Applications/Windsurf.app")
        else:  # Linux
            config_path = self.home / ".config" / "Windsurf" / "mcp" / "servers.json"
            install_path = self.home / ".local" / "share" / "Windsurf"

        parent_exists, config_exists = self._check_path_exists(config_path)
        installed = install_path.exists() or parent_exists

        return {
            "installed": installed,
            "global_config_path": str(config_path) if config_path else None,
            "config_exists": config_exists,
            "supports_project_config": True,
            "project_config_path": ".mcp/servers/",
            "notes": "May support project-level configs (check Windsurf docs)"
        }

    def _detect_cline(self) -> Dict:
        """Detect Cline (VS Code extension)."""
        # Cline primarily uses project-level config
        project_config = ".cline/mcp.json"

        # Check if VS Code is installed (Cline requires it)
        vscode_installed = self._is_vscode_installed()

        return {
            "installed": vscode_installed,
            "global_config_path": None,
            "config_exists": False,
            "supports_project_config": True,
            "project_config_path": project_config,
            "notes": "Uses project-level config only (no global config)"
        }

    def _detect_zed(self) -> Dict:
        """Detect Zed editor."""
        if self.os_name == "windows":
            config_path = self.home / "AppData" / "Roaming" / "Zed" / "settings.json"
            install_path = self.home / "AppData" / "Local" / "Programs" / "Zed"
        elif self.os_name == "darwin":  # macOS
            config_path = self.home / "Library" / "Application Support" / "Zed" / "settings.json"
            install_path = Path("/Applications/Zed.app")
        else:  # Linux
            config_path = self.home / ".config" / "zed" / "settings.json"
            install_path = self.home / ".local" / "share" / "zed"

        parent_exists, config_exists = self._check_path_exists(config_path)
        installed = install_path.exists() or parent_exists

        return {
            "installed": installed,
            "global_config_path": str(config_path) if config_path else None,
            "config_exists": config_exists,
            "supports_project_config": False,
            "project_config_path": None,
            "notes": "MCP support may vary - check Zed documentation"
        }

    def _detect_claude_desktop(self) -> Dict:
        """Detect Claude Desktop app (for reference)."""
        if self.os_name == "windows":
            config_path = self.home / "AppData" / "Roaming" / "Claude" / "claude_desktop_config.json"
            install_path = self.home / "AppData" / "Local" / "Programs" / "Claude"
        elif self.os_name == "darwin":  # macOS
            config_path = self.home / "Library" / "Application Support" / "Claude" / "claude_desktop_config.json"
            install_path = Path("/Applications/Claude.app")
        else:  # Linux
            config_path = self.home / ".config" / "Claude" / "claude_desktop_config.json"
            install_path = self.home / ".local" / "share" / "Claude"

        parent_exists, config_exists = self._check_path_exists(config_path)
        installed = install_path.exists() or parent_exists

        return {
            "installed": installed,
            "global_config_path": str(config_path) if config_path else None,
            "config_exists": config_exists,
            "supports_project_config": False,
            "project_config_path": None,
            "notes": "Not a coding IDE, included for reference"
        }

    def _detect_vscode(self) -> Dict:
        """Detect VS Code (base editor)."""
        if self.os_name == "windows":
            config_path = self.home / "AppData" / "Roaming" / "Code" / "User" / "settings.json"
            install_path = self.home / "AppData" / "Local" / "Programs" / "Microsoft VS Code"
        elif self.os_name == "darwin":  # macOS
            config_path = self.home / "Library" / "Application Support" / "Code" / "User" / "settings.json"
            install_path = Path("/Applications/Visual Studio Code.app")
        else:  # Linux
            config_path = self.home / ".config" / "Code" / "User" / "settings.json"
            install_path = self.home / ".local" / "share" / "code"

        parent_exists, config_exists = self._check_path_exists(config_path)
        installed = install_path.exists() or parent_exists or self._is_vscode_installed()

        return {
            "installed": installed,
            "global_config_path": str(config_path) if config_path else None,
            "config_exists": config_exists,
            "supports_project_config": True,
            "project_config_path": ".vscode/settings.json",
            "notes": "Base editor - install extensions like Copilot or Cline for MCP support"
        }

    def _is_vscode_installed(self) -> bool:
        """Check if VS Code is installed by looking for the 'code' command."""
        import shutil
        return shutil.which("code") is not None


def print_results_table(results: Dict[str, Dict]):
    """Print results in a formatted table."""
    # Use ASCII fallbacks on Windows to avoid encoding issues
    is_windows = platform.system().lower() == "windows"
    check_mark = "[+]" if is_windows else "✓"
    cross_mark = "[-]" if is_windows else "✗"
    warning_mark = "[!]" if is_windows else "⚠"

    # Try to set UTF-8 encoding on Windows
    if is_windows:
        try:
            import sys
            sys.stdout.reconfigure(encoding='utf-8')
            # If successful, use Unicode
            check_mark = "✓"
            cross_mark = "✗"
            warning_mark = "⚠"
        except:
            pass  # Keep ASCII fallbacks

    print("\n" + "=" * 80)
    print("IDE MCP Config Detection Results")
    print("=" * 80 + "\n")

    for ide_name, info in results.items():
        status = f"{check_mark} INSTALLED" if info["installed"] else f"{cross_mark} Not Found"
        color = "\033[92m" if info["installed"] else "\033[90m"
        reset = "\033[0m"

        print(f"{color}{ide_name}: {status}{reset}")

        if info["installed"]:
            if info["global_config_path"]:
                config_status = f"{check_mark} EXISTS" if info["config_exists"] else f"{warning_mark} Not Created"
                print(f"  Global Config: {info['global_config_path']}")
                print(f"  Config Status: {config_status}")

            if info["supports_project_config"]:
                print(f"  Project Config: {info['project_config_path']} (supported)")

            if info.get("notes"):
                print(f"  Notes: {info['notes']}")

        print()


def main():
    """Main entry point."""
    import argparse

    parser = argparse.ArgumentParser(
        description="Detect IDE MCP configuration paths"
    )
    parser.add_argument(
        "--json",
        action="store_true",
        help="Output results as JSON"
    )
    parser.add_argument(
        "--verbose",
        "-v",
        action="store_true",
        help="Show detailed detection info"
    )
    parser.add_argument(
        "--ide",
        help="Check specific IDE only",
        choices=[
            "GitHub Copilot", "Cursor", "Windsurf",
            "Cline", "Zed", "Claude Desktop", "VS Code"
        ]
    )

    args = parser.parse_args()

    detector = IDEConfigDetector(verbose=args.verbose)
    results = detector.get_config_paths()

    # Filter to specific IDE if requested
    if args.ide:
        results = {args.ide: results[args.ide]}

    if args.json:
        print(json.dumps(results, indent=2))
    else:
        print_results_table(results)

        # Summary
        installed_count = sum(1 for info in results.values() if info["installed"])
        print(f"Summary: {installed_count}/{len(results)} IDE(s) detected\n")

        if installed_count > 0:
            print("Next Steps:")
            print("  1. Run the MCP setup script for your IDE")
            print("  2. Or manually configure using the paths above")
            print("  3. See .mcp/README.md for detailed instructions")
            print()


if __name__ == "__main__":
    main()
