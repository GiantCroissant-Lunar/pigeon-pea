# Format All Code - Comprehensive Procedure

## Overview

This guide provides instructions for formatting all code in the PigeonPea repository using both `dotnet format` (for C# code) and `prettier` (for JSON, YAML, Markdown, and other files). This is the most comprehensive formatting option and should be used before commits, before PR creation, or when doing repository-wide cleanup.

## Prerequisites

- **.NET SDK 9.0** or later (for dotnet format)
- **Node.js** and **npm** (for prettier)
- Solution file: `./dotnet/PigeonPea.sln`
- Configuration files: `.editorconfig`, `.prettierrc.json`, `.prettierignore`

## Standard Format All Flow

### Step 1: Navigate to Repository Root

```bash
# Navigate to repository root (if not already there)
cd $(git rev-parse --show-toplevel)
```

The format-all script should be run from the repository root.

### Step 2: Run Format All Script

```bash
./.agent/skills/code-format/scripts/format-all.sh
```

Runs both dotnet format and prettier in the correct sequence.

**Alternative: Manual Steps**

If script is not available or you want manual control:

```bash
# Step 1: Format .NET code
cd ./dotnet
dotnet format PigeonPea.sln
cd ..

# Step 2: Format non-.NET files
npx prettier --write "**/*.{json,yml,yaml,md,js,jsx,ts,tsx}"
```

### Step 3: Verify All Formatting

```bash
# Verify .NET
cd ./dotnet
dotnet format PigeonPea.sln --verify-no-changes
cd ..

# Verify Prettier
npx prettier --check "**/*.{json,yml,yaml,md,js,jsx,ts,tsx}"
```

Both commands should return exit code 0 if all files are properly formatted.

## What Gets Formatted

### .NET Files (dotnet format)

- **Extensions**: `.cs` (C# source files)
- **Projects**: console-app, shared-app, windows-app, test projects
- **Rules**: `.editorconfig` + .NET code style rules
- **Location**: `./dotnet/**/*.cs`

### Non-.NET Files (prettier)

- **JSON**: `**/*.json` (package.json, config files, etc.)
- **YAML**: `**/*.yml`, `**/*.yaml` (configs, workflows, etc.)
- **Markdown**: `**/*.md` (documentation, README files)
- **JavaScript/TypeScript**: `**/*.{js,jsx,ts,tsx}` (if present)
- **Rules**: `.prettierrc.json`
- **Exclusions**: Files in `.prettierignore`

## Format All Options

### Using the Script

```bash
# Default: format everything
./.agent/skills/code-format/scripts/format-all.sh

# Verify only (no modifications)
./.agent/skills/code-format/scripts/format-all.sh --verify

# Verbose output
./.agent/skills/code-format/scripts/format-all.sh --verbose

# Skip .NET formatting
./.agent/skills/code-format/scripts/format-all.sh --skip-dotnet

# Skip Prettier formatting
./.agent/skills/code-format/scripts/format-all.sh --skip-prettier
```

### Manual Format Sequence

```bash
# 1. Format .NET code first
cd ./dotnet
dotnet format PigeonPea.sln
cd ..

# 2. Format JSON files
npx prettier --write "**/*.json"

# 3. Format YAML files
npx prettier --write "**/*.{yml,yaml}"

# 4. Format Markdown files
npx prettier --write "**/*.md"

# 5. Format JavaScript/TypeScript (if present)
npx prettier --write "**/*.{js,jsx,ts,tsx}"
```

## Common Use Cases

### Before Commit (Quick Format)

```bash
# Format everything before committing
./.agent/skills/code-format/scripts/format-all.sh

# Review changes
git diff

# Stage and commit
git add .
git commit -m "Your message"
```

### Before PR Creation

```bash
# Comprehensive format and verify
./.agent/skills/code-format/scripts/format-all.sh
./.agent/skills/code-format/scripts/format-all.sh --verify

# Check for any remaining issues
git status
git diff

# Push to PR branch
git push
```

### CI/CD Verification

```bash
# Run in verify mode (fails if not formatted)
./.agent/skills/code-format/scripts/format-all.sh --verify

# Exit code non-zero = formatting violations
```

### Repository Cleanup

```bash
# Format entire repository
./.agent/skills/code-format/scripts/format-all.sh

# Review all changes carefully
git diff --stat
git diff

# Create cleanup commit
git add .
git commit -m "chore: format all code"
```

## Common Errors and Solutions

### Error: dotnet format fails

**Cause**: .NET SDK not installed or solution file not found.

**Fix**:

```bash
# Check .NET SDK
dotnet --version

# Verify solution exists
ls ./dotnet/PigeonPea.sln

# Navigate to repository root
cd $(git rev-parse --show-toplevel)
```

### Error: prettier not found

**Cause**: Node.js/npm not installed or prettier not available.

**Fix**:

```bash
# Check Node.js
node --version
npm --version

# Install prettier if needed
npm install -g prettier

# Or use npx (downloads temporarily)
npx prettier --version
```

### Error: Some files not formatted

**Cause**: Files excluded in `.prettierignore` or not matching patterns.

**Fix**:

```bash
# Check ignore file
cat .prettierignore

# Manually format excluded files if needed
npx prettier --write ./specific-file.json
```

### Error: Conflicting changes

**Cause**: Multiple formatters modifying same files differently.

**Fix**: This shouldn't happen. `.editorconfig` ensures consistency. If it does:

```bash
# .editorconfig takes precedence
# Check configuration files
cat .editorconfig
cat .prettierrc.json
```

### Warning: Large number of files changed

**Expected behavior** when running format-all for first time. Review carefully:

```bash
# See summary of changes
git diff --stat

# Review specific changes
git diff ./dotnet
git diff ./.agent
```

## Integration with Pre-commit Hooks

Pre-commit hooks automatically format code on commit:

```yaml
# .pre-commit-config.yaml
repos:
  - repo: local
    hooks:
      - id: dotnet-format
        # Formats .NET code

  - repo: https://github.com/pre-commit/mirrors-prettier
    hooks:
      - id: prettier
        # Formats JSON, YAML, Markdown
```

**Setup pre-commit hooks:**

```bash
# One-time setup
./setup-pre-commit.sh

# Hooks run automatically on every commit
git commit -m "Message"
# Auto-formats staged files before commit
```

**Manual pre-commit run:**

```bash
# Run all hooks on all files
pre-commit run --all-files

# Run specific hook
pre-commit run dotnet-format --all-files
pre-commit run prettier --all-files
```

## Verification Steps

### 1. Check Exit Codes

```bash
# Format all
./.agent/skills/code-format/scripts/format-all.sh
echo $?  # Should be 0

# Verify all
./.agent/skills/code-format/scripts/format-all.sh --verify
echo $?  # Should be 0 if properly formatted
```

### 2. Review Changes

```bash
# See what changed
git status
git diff --stat
git diff

# Review .NET changes
git diff ./dotnet

# Review config changes
git diff ./.agent
```

### 3. Verify Pre-commit

```bash
# Test pre-commit hooks
pre-commit run --all-files

# Should pass with no errors
```

### 4. Verify Individual Formatters

```bash
# Verify .NET formatting
cd ./dotnet
dotnet format PigeonPea.sln --verify-no-changes
cd ..

# Verify Prettier formatting
npx prettier --check "**/*.{json,yml,yaml,md}"
```

## Before Commit Checklist

- [ ] Run format-all: `./.agent/skills/code-format/scripts/format-all.sh`
- [ ] Verify formatting: `./.agent/skills/code-format/scripts/format-all.sh --verify`
- [ ] Review all changes: `git diff`
- [ ] Test pre-commit: `pre-commit run --all-files`
- [ ] Stage files: `git add .`
- [ ] Commit: `git commit -m "Your message"`
- [ ] Pre-commit hooks run automatically
- [ ] Push: `git push`

## Performance Tips

1. **Format incrementally**: Format changed files only during development
2. **Use format-all**: Run comprehensive format before commits/PRs
3. **Enable IDE format-on-save**: Reduces manual formatting
4. **Cache in CI**: Cache formatter results and formatted state
5. **Parallel execution**: Script runs formatters in parallel when possible

## Advanced Scenarios

### Format Only Changed Files

```bash
# Get list of changed files
changed_cs_files=$(git diff --name-only --diff-filter=ACMR | grep '\.cs$')
changed_other_files=$(git diff --name-only --diff-filter=ACMR | grep -E '\.(json|yml|yaml|md)$')

# Format only changed .NET files
if [ -n "$changed_cs_files" ]; then
  cd ./dotnet
  echo "$changed_cs_files" | xargs dotnet format --include
  cd ..
fi

# Format only changed non-.NET files
if [ -n "$changed_other_files" ]; then
  echo "$changed_other_files" | xargs npx prettier --write
fi
```

### Format Specific Subsystems

```bash
# Format only .agent directory (from repository root)
cd $(git rev-parse --show-toplevel)
npx prettier --write "./.agent/**/*.{json,yml,yaml,md}"

# Format only dotnet directory
cd ./dotnet
dotnet format PigeonPea.sln
```

### Dry Run (Show What Would Change)

```bash
# .NET: verify mode shows violations
cd ./dotnet
dotnet format PigeonPea.sln --verify-no-changes --verbosity detailed

# Prettier: check mode lists unformatted files
npx prettier --list-different "**/*.{json,yml,yaml,md}"
```

## Script Details

The `format-all.sh` script:

- Checks prerequisites (.NET SDK, Node.js)
- Runs dotnet format on .NET solution
- Runs prettier on supported file types
- Provides colored output (success/warning/error)
- Supports verify mode (--verify flag)
- Handles errors gracefully
- Returns appropriate exit codes

See: `.agent/skills/code-format/scripts/format-all.sh`

## Related Procedures

- **Format .NET only**: See [`dotnet-format.md`](dotnet-format.md)
- **Format non-.NET only**: See [`prettier-format.md`](prettier-format.md)
- **Build after format**: Use `dotnet-build` skill
- **Test after format**: Use `dotnet-test` skill
- **Pre-commit setup**: See `setup-pre-commit.sh` in repository root

## Quick Reference

```bash
# Format everything
./.agent/skills/code-format/scripts/format-all.sh

# Verify everything (no modifications)
./.agent/skills/code-format/scripts/format-all.sh --verify

# Manual sequence
cd ./dotnet && dotnet format PigeonPea.sln && cd ..
npx prettier --write "**/*.{json,yml,yaml,md}"

# Verify manual
cd ./dotnet && dotnet format PigeonPea.sln --verify-no-changes && cd ..
npx prettier --check "**/*.{json,yml,yaml,md}"

# Pre-commit hooks
pre-commit run --all-files
```

## Summary

- **Script**: `./.agent/skills/code-format/scripts/format-all.sh`
- **Formats**: All .NET code (C#) + all JSON/YAML/Markdown files
- **Tools**: `dotnet format` + `prettier`
- **Configuration**: `.editorconfig`, `.prettierrc.json`
- **Verify mode**: Use `--verify` flag for check-only
- **Integration**: Pre-commit hooks run automatically on commit
- **Use case**: Before commits, before PRs, repository cleanup
