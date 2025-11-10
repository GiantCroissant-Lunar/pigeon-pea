#!/bin/bash
# Build all projects in PigeonPea solution
# This script provides a convenient way to build the entire solution with common configurations

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get the script directory and navigate to dotnet folder
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"
DOTNET_DIR="$REPO_ROOT/dotnet"

echo "Building PigeonPea Solution..."
echo "Working directory: $DOTNET_DIR"
echo ""

# Navigate to dotnet directory
cd "$DOTNET_DIR"

# Parse command line arguments
CONFIGURATION="${1:-Debug}"
CLEAN="${2:-false}"
RESTORE="${3:-true}"

echo "Configuration: $CONFIGURATION"
echo "Clean before build: $CLEAN"
echo "Restore dependencies: $RESTORE"
echo ""

# Clean if requested
if [ "$CLEAN" = "true" ]; then
    echo -e "${YELLOW}Cleaning solution...${NC}"
    dotnet clean PigeonPea.sln --configuration "$CONFIGURATION"
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Clean succeeded${NC}"
    else
        echo -e "${RED}✗ Clean failed${NC}"
        exit 1
    fi
    echo ""
fi

# Restore dependencies if requested
if [ "$RESTORE" = "true" ]; then
    echo -e "${YELLOW}Restoring dependencies...${NC}"
    dotnet restore PigeonPea.sln
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Restore succeeded${NC}"
    else
        echo -e "${RED}✗ Restore failed${NC}"
        exit 1
    fi
    echo ""
fi

# Build solution
echo -e "${YELLOW}Building solution ($CONFIGURATION)...${NC}"
if [ "$RESTORE" = "true" ]; then
    dotnet build PigeonPea.sln --configuration "$CONFIGURATION"
else
    dotnet build PigeonPea.sln --configuration "$CONFIGURATION" --no-restore
fi

BUILD_EXIT_CODE=$?

echo ""
if [ $BUILD_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✓ Build succeeded!${NC}"
    echo ""
    echo "Build artifacts located in:"
    echo "  - console-app/bin/$CONFIGURATION/"
    echo "  - shared-app/bin/$CONFIGURATION/"
    echo "  - windows-app/bin/$CONFIGURATION/"
    echo "  - Test projects/bin/$CONFIGURATION/"
    exit 0
else
    echo -e "${RED}✗ Build failed${NC}"
    echo "Check the output above for errors"
    exit 1
fi
