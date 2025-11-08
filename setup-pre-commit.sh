#!/bin/bash

# Pre-commit setup script for pigeon-pea project
# This script helps set up the development environment with pre-commit hooks

set -e

echo "========================================="
echo "Pre-commit Setup Script"
echo "========================================="
echo ""

# Check if Python is installed
if ! command -v python3 &> /dev/null; then
    echo "ERROR: Python 3 is not installed."
    echo "Please install Python 3.6+ from https://www.python.org/"
    exit 1
fi

echo "✓ Python 3 is installed ($(python3 --version))"

# Check if pip is installed
if ! command -v pip3 &> /dev/null; then
    echo "ERROR: pip3 is not installed."
    echo "Please install pip3"
    exit 1
fi

echo "✓ pip3 is installed"

# Install pre-commit
echo ""
echo "Installing pre-commit..."
pip3 install pre-commit || {
    echo "WARNING: Failed to install pre-commit globally, trying with --user flag"
    pip3 install --user pre-commit
}

echo "✓ pre-commit installed"

# Install pre-commit hooks
echo ""
echo "Installing pre-commit hooks..."
pre-commit install || {
    echo "ERROR: Failed to install pre-commit hooks"
    exit 1
}

echo "✓ Pre-commit hooks installed"

# Optional checks for language-specific tools
echo ""
echo "Checking optional dependencies..."

# Check .NET
if command -v dotnet &> /dev/null; then
    echo "✓ .NET SDK is installed ($(dotnet --version))"
else
    echo "⚠ .NET SDK is not installed (optional for C# formatting)"
    echo "  Download from: https://dotnet.microsoft.com/download"
fi

# Check Node.js
if command -v node &> /dev/null; then
    echo "✓ Node.js is installed ($(node --version))"
else
    echo "⚠ Node.js is not installed (optional for JavaScript/TypeScript formatting)"
    echo "  Download from: https://nodejs.org/"
fi

# Run pre-commit on all files (optional)
echo ""
read -p "Do you want to run pre-commit on all existing files now? (y/n) " -n 1 -r
echo ""
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "Running pre-commit on all files..."
    pre-commit run --all-files || {
        echo ""
        echo "⚠ Some hooks failed. This is normal for a new setup."
        echo "  Please review the output above and fix any issues."
    }
fi

echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Pre-commit hooks are now installed and will run automatically on every commit."
echo ""
echo "Useful commands:"
echo "  pre-commit run --all-files    # Run all hooks on all files"
echo "  pre-commit run <hook-id>      # Run a specific hook"
echo "  pre-commit autoupdate         # Update hooks to latest versions"
echo "  git commit --no-verify        # Skip hooks (not recommended)"
echo ""
echo "For more information, see README.md or visit https://pre-commit.com/"
