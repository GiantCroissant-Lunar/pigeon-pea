#!/bin/bash
# Get semantic version from GitVersion for Linux/macOS

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "Error: dotnet is not installed. GitVersion requires .NET SDK." >&2
    exit 1
fi

# Check if GitVersion tool is installed
if ! dotnet tool list -g | grep -q "gitversion.tool"; then
    echo "Error: GitVersion tool is not installed." >&2
    echo "Install with: dotnet tool install --global GitVersion.Tool" >&2
    exit 1
fi

# Run GitVersion and get SemVer
VERSION=$(dotnet gitversion /showvariable SemVer 2>/dev/null)

if [ -z "$VERSION" ]; then
    echo "Error: Could not get version from GitVersion" >&2
    exit 1
fi

echo "$VERSION"
