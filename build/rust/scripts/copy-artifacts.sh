#!/bin/bash
# Copy built artifacts to versioned directory for Linux/macOS

set -e

# Get version from GitVersion
if ! command -v dotnet &> /dev/null; then
    echo "Error: dotnet is not installed. GitVersion requires .NET SDK." >&2
    exit 1
fi

VERSION=$(dotnet gitversion /showvariable SemVer 2>/dev/null)

if [ -z "$VERSION" ]; then
    echo "Error: Could not get version from GitVersion" >&2
    exit 1
fi

# Get environment variables
PROJECT_NAME="${PROJECT_NAME:-dev-tool-server}"
PROJECT_PATH="${PROJECT_PATH:-../../projects/dungeon/rust/dev-tool-server}"
ARTIFACTS_BASE="${ARTIFACTS_BASE:-../../build/_artifacts}"
BINARY_EXT="${BINARY_EXT:-}"

# Construct paths
SOURCE_BINARY="${PROJECT_PATH}/target/release/${PROJECT_NAME}${BINARY_EXT}"
TARGET_DIR="${ARTIFACTS_BASE}/${VERSION}/${PROJECT_NAME}"

# Create artifact directory
echo "Creating artifact directory: $TARGET_DIR"
mkdir -p "$TARGET_DIR"

# Copy binary
echo "Copying binary from: $SOURCE_BINARY"
echo "             to: $TARGET_DIR"
cp "$SOURCE_BINARY" "$TARGET_DIR/"

# Make binary executable on Unix systems
chmod +x "${TARGET_DIR}/${PROJECT_NAME}${BINARY_EXT}"

# Create version.txt
VERSION_FILE="${TARGET_DIR}/version.txt"
cat > "$VERSION_FILE" <<EOF
Version: $VERSION
Built: $(date +"%Y-%m-%d %H:%M:%S")
Platform: $(uname -s)
Binary: ${PROJECT_NAME}${BINARY_EXT}
EOF

# Create README.md
README_FILE="${TARGET_DIR}/README.md"
cat > "$README_FILE" <<EOF
# $PROJECT_NAME

Version: $VERSION

## Usage

Run the binary:

\`\`\`bash
./${PROJECT_NAME}${BINARY_EXT}
\`\`\`

## Build Information

- Built: $(date +"%Y-%m-%d %H:%M:%S")
- Platform: $(uname -s)
- Build Type: Release
EOF

echo "Artifacts published to: $TARGET_DIR"
echo "  - ${PROJECT_NAME}${BINARY_EXT}"
echo "  - version.txt"
echo "  - README.md"
