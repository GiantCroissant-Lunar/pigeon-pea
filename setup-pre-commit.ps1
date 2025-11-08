# Pre-commit setup script for pigeon-pea project (PowerShell)
# This script helps set up the development environment with pre-commit hooks

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Pre-commit Setup Script" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Python is installed
try {
    $pythonVersion = python --version 2>&1
    Write-Host "✓ Python is installed ($pythonVersion)" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Python is not installed." -ForegroundColor Red
    Write-Host "Please install Python 3.6+ from https://www.python.org/" -ForegroundColor Yellow
    exit 1
}

# Check if pip is installed
try {
    $pipVersion = pip --version 2>&1
    Write-Host "✓ pip is installed" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: pip is not installed." -ForegroundColor Red
    Write-Host "Please install pip" -ForegroundColor Yellow
    exit 1
}

# Install pre-commit
Write-Host ""
Write-Host "Installing pre-commit..." -ForegroundColor Yellow
try {
    pip install pre-commit
    Write-Host "✓ pre-commit installed" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Failed to install pre-commit" -ForegroundColor Red
    exit 1
}

# Install pre-commit hooks
Write-Host ""
Write-Host "Installing pre-commit hooks..." -ForegroundColor Yellow
try {
    pre-commit install
    Write-Host "✓ Pre-commit hooks installed" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Failed to install pre-commit hooks" -ForegroundColor Red
    exit 1
}

# Optional checks for language-specific tools
Write-Host ""
Write-Host "Checking optional dependencies..." -ForegroundColor Yellow

# Check .NET
try {
    $dotnetVersion = dotnet --version 2>&1
    Write-Host "✓ .NET SDK is installed ($dotnetVersion)" -ForegroundColor Green
}
catch {
    Write-Host "⚠ .NET SDK is not installed (optional for C# formatting)" -ForegroundColor Yellow
    Write-Host "  Download from: https://dotnet.microsoft.com/download" -ForegroundColor Gray
}

# Check Node.js
try {
    $nodeVersion = node --version 2>&1
    Write-Host "✓ Node.js is installed ($nodeVersion)" -ForegroundColor Green
}
catch {
    Write-Host "⚠ Node.js is not installed (optional for JavaScript/TypeScript formatting)" -ForegroundColor Yellow
    Write-Host "  Download from: https://nodejs.org/" -ForegroundColor Gray
}

# Run pre-commit on all files (optional)
Write-Host ""
$runAllFiles = Read-Host "Do you want to run pre-commit on all existing files now? (y/n)"
if ($runAllFiles -eq "y" -or $runAllFiles -eq "Y") {
    Write-Host "Running pre-commit on all files..." -ForegroundColor Yellow
    try {
        pre-commit run --all-files
    }
    catch {
        Write-Host ""
        Write-Host "⚠ Some hooks failed. This is normal for a new setup." -ForegroundColor Yellow
        Write-Host "  Please review the output above and fix any issues." -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Pre-commit hooks are now installed and will run automatically on every commit." -ForegroundColor White
Write-Host ""
Write-Host "Useful commands:" -ForegroundColor Yellow
Write-Host "  pre-commit run --all-files    # Run all hooks on all files" -ForegroundColor Gray
Write-Host "  pre-commit run <hook-id>      # Run a specific hook" -ForegroundColor Gray
Write-Host "  pre-commit autoupdate         # Update hooks to latest versions" -ForegroundColor Gray
Write-Host "  git commit --no-verify        # Skip hooks (not recommended)" -ForegroundColor Gray
Write-Host ""
Write-Host "For more information, see README.md or visit https://pre-commit.com/" -ForegroundColor White
