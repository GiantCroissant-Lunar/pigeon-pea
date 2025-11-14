#!/usr/bin/env node
/**
 * Universal MCP Project Launcher (Node.js version)
 *
 * This script enables per-project MCP server configurations for editors
 * that only support global configs (like GitHub Copilot).
 *
 * How it works:
 * 1. Reads the current working directory (project root)
 * 2. Loads project-specific MCP server configs from `.mcp/servers/`
 * 3. Launches the appropriate MCP server based on the config
 *
 * Setup:
 * 1. Copy this script to: ~/.local/mcp-launcher/launcher.js (Linux/Mac)
 *    or %USERPROFILE%\.local\mcp-launcher\launcher.js (Windows)
 * 2. Make it executable: chmod +x ~/.local/mcp-launcher/launcher.js
 * 3. Configure your editor's global MCP config to point to this script
 *
 * Example global config (GitHub Copilot):
 * ~/.config/github-copilot/mcp/servers.json:
 * {
 *   "mcp-project-launcher": {
 *     "command": "node",
 *     "args": ["~/.local/mcp-launcher/launcher.js", "github"]
 *   }
 * }
 *
 * The second argument ("github") specifies which server config to load from
 * `.mcp/servers/github.json` in your project.
 */

const fs = require('fs');
const path = require('path');
const { spawn } = require('child_process');

/**
 * Expand environment variables in strings.
 * Supports both ${VAR} and $VAR syntax.
 * Special variable: ${PROJECT_ROOT} expands to the project root path.
 */
function expandEnvVars(value, projectRoot) {
  if (typeof value !== 'string') {
    return value;
  }

  // Handle ${PROJECT_ROOT}
  value = value.replace(/\$\{PROJECT_ROOT\}/g, projectRoot);

  // Handle ${VAR} syntax
  value = value.replace(/\$\{([^}]+)\}/g, (match, varName) => {
    return process.env[varName] || match;
  });

  // Handle $VAR syntax (without braces)
  value = value.replace(/\$([A-Za-z_][A-Za-z0-9_]*)/g, (match, varName) => {
    return process.env[varName] || match;
  });

  return value;
}

/**
 * Recursively expand environment variables in config objects.
 */
function expandEnvVarsRecursive(obj, projectRoot) {
  if (Array.isArray(obj)) {
    return obj.map(item => expandEnvVarsRecursive(item, projectRoot));
  } else if (obj !== null && typeof obj === 'object') {
    const result = {};
    for (const [key, value] of Object.entries(obj)) {
      result[key] = expandEnvVarsRecursive(value, projectRoot);
    }
    return result;
  } else if (typeof obj === 'string') {
    return expandEnvVars(obj, projectRoot);
  } else {
    return obj;
  }
}

/**
 * Find the project root directory.
 * Returns the current working directory.
 */
function findProjectRoot() {
  return process.cwd();
}

/**
 * Load MCP server configuration from project.
 */
function loadServerConfig(projectRoot, serverName = null) {
  const configDir = path.join(projectRoot, '.mcp', 'servers');

  if (!fs.existsSync(configDir)) {
    console.error(`ERROR: No .mcp/servers directory found in ${projectRoot}`);
    console.error(`Expected path: ${configDir}`);
    process.exit(1);
  }

  // Find config file
  let configPath;
  if (serverName) {
    configPath = path.join(configDir, `${serverName}.json`);
    if (!fs.existsSync(configPath)) {
      console.error(`ERROR: Server config '${serverName}' not found`);
      console.error(`Expected path: ${configPath}`);
      console.error('\nAvailable configs:');
      fs.readdirSync(configDir)
        .filter(f => f.endsWith('.json') && !f.startsWith('_'))
        .forEach(f => console.error(`  - ${path.basename(f, '.json')}`));
      process.exit(1);
    }
  } else {
    // Load first available config (excluding templates)
    const configs = fs.readdirSync(configDir)
      .filter(f => f.endsWith('.json') && !f.startsWith('_'));

    if (configs.length === 0) {
      console.error(`ERROR: No server configs found in ${configDir}`);
      process.exit(1);
    }
    configPath = path.join(configDir, configs[0]);
  }

  // Load and parse config
  try {
    const configData = fs.readFileSync(configPath, 'utf-8');
    const config = JSON.parse(configData);
    return { config, configPath };
  } catch (e) {
    console.error(`ERROR: Invalid JSON in ${configPath}: ${e.message}`);
    process.exit(1);
  }
}

/**
 * Launch the MCP server based on configuration.
 */
function launchServer(config, projectRoot) {
  // Expand environment variables in config
  config = expandEnvVarsRecursive(config, projectRoot);

  // Extract command and args
  const command = config.command;
  const args = config.args || [];
  const envVars = config.env || {};
  const cwd = expandEnvVars(config.cwd || projectRoot, projectRoot);

  if (!command) {
    console.error('ERROR: No "command" specified in server config');
    process.exit(1);
  }

  // Prepare environment
  const env = { ...process.env, ...envVars };

  // Debug output
  console.error(`[MCP Launcher] Starting: ${config.name || 'unknown'}`);
  console.error(`[MCP Launcher] Command: ${command} ${args.join(' ')}`);
  console.error(`[MCP Launcher] CWD: ${cwd}`);

  // Launch the server
  try {
    const child = spawn(command, args, {
      cwd,
      env,
      stdio: 'inherit',
      shell: process.platform === 'win32'
    });

    child.on('error', (err) => {
      console.error(`ERROR: Failed to launch server: ${err.message}`);
      process.exit(1);
    });

    child.on('exit', (code) => {
      process.exit(code || 0);
    });
  } catch (e) {
    console.error(`ERROR: Failed to launch server: ${e.message}`);
    process.exit(1);
  }
}

/**
 * Main entry point.
 */
function main() {
  // Parse arguments
  const serverName = process.argv[2] || null;

  // Find project root
  const projectRoot = findProjectRoot();

  // Load server config
  const { config } = loadServerConfig(projectRoot, serverName);

  // Launch the server
  launchServer(config, projectRoot);
}

main();
