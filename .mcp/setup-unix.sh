#!/usr/bin/env bash
#
# Automated setup script for MCP Project Launcher on Linux/Mac
#
# This script automates the installation and configuration of the MCP Project Launcher
# for use with GitHub Copilot and other AI coding assistants.
#
# Usage:
#   ./setup-unix.sh [--launcher-type python|node] [--skip-env-vars] [--skip-copilot-config]
#
# Examples:
#   ./setup-unix.sh
#   ./setup-unix.sh --launcher-type node --skip-copilot-config
#

set -e  # Exit on error

# Default options
LAUNCHER_TYPE="python"
SKIP_ENV_VARS=false
SKIP_COPILOT_CONFIG=false

# Color output functions
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

success() { echo -e "${GREEN}✓${NC} $1"; }
info() { echo -e "${CYAN}ℹ${NC} $1"; }
warning() { echo -e "${YELLOW}⚠${NC} $1"; }
error() { echo -e "${RED}✗${NC} $1"; }

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --launcher-type)
            LAUNCHER_TYPE="$2"
            shift 2
            ;;
        --skip-env-vars)
            SKIP_ENV_VARS=true
            shift
            ;;
        --skip-copilot-config)
            SKIP_COPILOT_CONFIG=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [--launcher-type python|node] [--skip-env-vars] [--skip-copilot-config]"
            exit 0
            ;;
        *)
            error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Banner
cat << "EOF"

╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║     MCP Project Launcher - Linux/Mac Setup Script        ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝

EOF

# Check prerequisites
info "Checking prerequisites..."

PYTHON_AVAILABLE=false
NODE_AVAILABLE=false

if command -v python3 &> /dev/null; then
    PYTHON_VERSION=$(python3 --version 2>&1)
    PYTHON_AVAILABLE=true
    success "Python found: $PYTHON_VERSION"
elif command -v python &> /dev/null; then
    PYTHON_VERSION=$(python --version 2>&1)
    PYTHON_AVAILABLE=true
    success "Python found: $PYTHON_VERSION"
else
    warning "Python not found"
fi

if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version 2>&1)
    NODE_AVAILABLE=true
    success "Node.js found: $NODE_VERSION"
else
    warning "Node.js not found"
fi

if [ "$PYTHON_AVAILABLE" = false ] && [ "$NODE_AVAILABLE" = false ]; then
    error "Neither Python nor Node.js found!"
    info "Please install Python (https://python.org) or Node.js (https://nodejs.org)"
    exit 1
fi

# Validate launcher type
if [ "$LAUNCHER_TYPE" = "python" ] && [ "$PYTHON_AVAILABLE" = false ]; then
    if [ "$NODE_AVAILABLE" = true ]; then
        warning "Python not found, switching to Node.js launcher"
        LAUNCHER_TYPE="node"
    else
        error "Python launcher requested but Python not found"
        exit 1
    fi
fi

if [ "$LAUNCHER_TYPE" = "node" ] && [ "$NODE_AVAILABLE" = false ]; then
    if [ "$PYTHON_AVAILABLE" = true ]; then
        warning "Node.js not found, switching to Python launcher"
        LAUNCHER_TYPE="python"
    else
        error "Node.js launcher requested but Node.js not found"
        exit 1
    fi
fi

info "Using $LAUNCHER_TYPE launcher"

# Detect OS
OS="unknown"
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    OS="linux"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    OS="mac"
fi

# Step 1: Create launcher directory
echo ""
info "Step 1: Installing launcher globally..."

LAUNCHER_DIR="$HOME/.local/mcp-launcher"
mkdir -p "$LAUNCHER_DIR"
success "Created directory: $LAUNCHER_DIR"

# Copy launcher file
LAUNCHER_EXT=$( [ "$LAUNCHER_TYPE" = "python" ] && echo "py" || echo "js" )
LAUNCHER_FILE="launcher.$LAUNCHER_EXT"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOURCE_PATH="$SCRIPT_DIR/launcher/$LAUNCHER_FILE"
DEST_PATH="$LAUNCHER_DIR/$LAUNCHER_FILE"

if [ ! -f "$SOURCE_PATH" ]; then
    error "Launcher file not found: $SOURCE_PATH"
    exit 1
fi

cp "$SOURCE_PATH" "$DEST_PATH"
chmod +x "$DEST_PATH"
success "Installed launcher: $DEST_PATH"

# Step 2: Configure environment variables (optional)
if [ "$SKIP_ENV_VARS" = false ]; then
    echo ""
    info "Step 2: Environment Variables Setup"
    info "Some MCP servers require environment variables (e.g., API tokens)"
    info "You can set them now or skip and set them later"
    echo ""

    read -p "Do you want to configure environment variables now? (y/N): " setup_env_vars

    if [[ "$setup_env_vars" =~ ^[Yy]$ ]]; then
        echo ""
        info "Common environment variables:"
        echo "  - GITHUB_PERSONAL_ACCESS_TOKEN (for GitHub MCP server)"
        echo "  - QDRANT_API_KEY (for Memory/Qdrant MCP server)"
        echo ""

        # Detect shell
        SHELL_RC=""
        if [ -n "$BASH_VERSION" ]; then
            SHELL_RC="$HOME/.bashrc"
        elif [ -n "$ZSH_VERSION" ]; then
            SHELL_RC="$HOME/.zshrc"
        else
            SHELL_RC="$HOME/.profile"
        fi

        read -p "Enter GitHub Personal Access Token (or press Enter to skip): " github_token
        if [ -n "$github_token" ]; then
            echo "export GITHUB_PERSONAL_ACCESS_TOKEN=\"$github_token\"" >> "$SHELL_RC"
            export GITHUB_PERSONAL_ACCESS_TOKEN="$github_token"
            success "Set GITHUB_PERSONAL_ACCESS_TOKEN in $SHELL_RC"
        fi

        read -p "Enter Qdrant API Key (or press Enter to skip): " qdrant_key
        if [ -n "$qdrant_key" ]; then
            echo "export QDRANT_API_KEY=\"$qdrant_key\"" >> "$SHELL_RC"
            export QDRANT_API_KEY="$qdrant_key"
            success "Set QDRANT_API_KEY in $SHELL_RC"
        fi

        warning "Note: Run 'source $SHELL_RC' or restart your terminal for changes to take effect"
    else
        info "Skipped environment variable setup"
        info "You can set them later in your shell config (~/.bashrc, ~/.zshrc, etc.)"
    fi
fi

# Step 3: Configure GitHub Copilot (optional)
if [ "$SKIP_COPILOT_CONFIG" = false ]; then
    echo ""
    info "Step 3: GitHub Copilot Configuration"
    info "This will create/update your GitHub Copilot MCP config"
    echo ""

    read -p "Do you want to configure GitHub Copilot now? (y/N): " setup_copilot

    if [[ "$setup_copilot" =~ ^[Yy]$ ]]; then
        COPILOT_CONFIG_DIR="$HOME/.config/github-copilot/mcp"
        mkdir -p "$COPILOT_CONFIG_DIR"

        COPILOT_CONFIG_PATH="$COPILOT_CONFIG_DIR/servers.json"

        COMMAND=$( [ "$LAUNCHER_TYPE" = "python" ] && echo "python3" || echo "node" )

        cat > "$COPILOT_CONFIG_PATH" << EOF
{
  "github": {
    "command": "$COMMAND",
    "args": [
      "$DEST_PATH",
      "github"
    ]
  },
  "sequential-thinking": {
    "command": "$COMMAND",
    "args": [
      "$DEST_PATH",
      "sequential-thinking"
    ]
  },
  "memory": {
    "command": "$COMMAND",
    "args": [
      "$DEST_PATH",
      "memory-qdrant"
    ]
  }
}
EOF

        success "Created GitHub Copilot config: $COPILOT_CONFIG_PATH"
        info "Restart GitHub Copilot for changes to take effect"
    else
        info "Skipped GitHub Copilot configuration"
        info "You can configure it later manually"
    fi
fi

# Step 4: Verify setup
echo ""
info "Step 4: Verifying setup..."

if [ -f "$DEST_PATH" ]; then
    success "Launcher installed successfully"
fi

PROJECT_MCP_DIR="$SCRIPT_DIR/servers"
if [ -d "$PROJECT_MCP_DIR" ]; then
    SERVER_COUNT=$(find "$PROJECT_MCP_DIR" -name "*.json" ! -name "_*" | wc -l)
    success "Found $SERVER_COUNT MCP server config(s) in project"
    find "$PROJECT_MCP_DIR" -name "*.json" ! -name "_*" -exec basename {} .json \; | while read config; do
        echo -e "  ${GRAY}- $config${NC}"
    done
fi

# Summary
cat << "EOF"

╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║                  Setup Complete! 🎉                       ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝

EOF

echo -e "${CYAN}Next Steps:${NC}"
echo "  1. Restart your editor (GitHub Copilot, Windsurf, etc.)"
echo "  2. Open this project"
echo "  3. Check MCP connection status in your editor"
echo "  4. Test by asking AI to use GitHub integration"
echo ""
echo -e "${CYAN}Documentation:${NC}"
echo "  • General: .mcp/README.md"
echo ""
echo -e "${CYAN}Troubleshooting:${NC}"
echo "  • Test launcher manually:"
echo "    cd \"$SCRIPT_DIR\""
echo "    $COMMAND \"$DEST_PATH\" github"
echo ""

exit 0
