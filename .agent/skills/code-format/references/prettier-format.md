# Format with Prettier - Detailed Procedure

## Overview

This guide provides step-by-step instructions for formatting JSON, YAML, Markdown, JavaScript, and TypeScript files using Prettier. Prettier applies consistent formatting rules defined in `.prettierrc.json` across the PigeonPea repository.

## Prerequisites

- **Node.js** and **npm** installed (check: `node --version`, `npm --version`)
- Prettier configuration: `.prettierrc.json` at repository root
- Prettier ignore file: `.prettierignore` at repository root
- Files to format: JSON, YAML, Markdown, JavaScript, TypeScript

## Standard Format Flow

### Step 1: Navigate to Repository Root

```bash
cd /home/runner/work/pigeon-pea/pigeon-pea
```

Prettier commands should be run from the repository root.

### Step 2: Format All Supported Files

```bash
npx prettier --write "**/*.{json,yml,yaml,md,js,jsx,ts,tsx}"
```

Formats all matching files in the repository. Modifies files in-place.

### Step 3: Verify Format (Check-Only Mode)

```bash
npx prettier --check "**/*.{json,yml,yaml,md,js,jsx,ts,tsx}"
```

Checks if files are formatted correctly without modifying them. Returns:

- Exit code 0: All files properly formatted
- Exit code non-zero: Formatting violations found

## Format Options

```bash
# Usage: npx prettier [options] [file/dir/glob ...]

# Common flags
--write                          # Format and save files
--check                          # Check if files are formatted (no modifications)
--list-different                 # List files that differ from formatted version
--config <path>                  # Path to config file (default: .prettierrc.json)
--ignore-path <path>             # Path to ignore file (default: .prettierignore)
--no-config                      # Disable config file lookup
--no-error-on-unmatched-pattern  # Don't fail if pattern matches no files

# Output control
--loglevel <level>               # Log level: error, warn, log, debug, silent
```

## Common Use Cases

### Format All JSON Files

```bash
npx prettier --write "**/*.json"
```

Formats all JSON files in the repository.

### Format All YAML Files

```bash
npx prettier --write "**/*.{yml,yaml}"
```

Formats all YAML files (.yml and .yaml extensions).

### Format All Markdown Files

```bash
npx prettier --write "**/*.md"
```

Formats all Markdown files.

### Format Specific File

```bash
npx prettier --write ./README.md
npx prettier --write ./package.json
```

Formats a single specific file.

### Format Specific Directory

```bash
npx prettier --write "./.agent/**/*.{json,yml,yaml,md}"
```

Formats files in a specific directory tree.

### Verify Without Modifying

```bash
npx prettier --check "**/*.{json,yml,yaml,md}"
```

Checks formatting without making changes. Useful for CI/CD pipelines.

### List Unformatted Files

```bash
npx prettier --list-different "**/*.{json,yml,yaml,md}"
```

Lists files that need formatting without modifying them.

### Format with Custom Config

```bash
npx prettier --write --config ./custom-prettier.json "**/*.json"
```

Uses a custom configuration file instead of `.prettierrc.json`.

## What Gets Formatted

Prettier formats files according to `.prettierrc.json`:

```json
{
  "semi": true, // Add semicolons
  "trailingComma": "es5", // Trailing commas where valid in ES5
  "singleQuote": true, // Use single quotes
  "printWidth": 100, // Wrap at 100 characters
  "tabWidth": 2, // 2 spaces for indentation
  "useTabs": false, // Use spaces, not tabs
  "arrowParens": "always", // Always add parens around arrow function params
  "endOfLine": "lf" // Unix-style line endings
}
```

**Formatting applies to:**

- **JSON**: Indentation, spacing, property order
- **YAML**: Indentation, spacing, line breaks
- **Markdown**: Line wrapping, list formatting, code block formatting
- **JavaScript/TypeScript**: Code style, quotes, semicolons, spacing

## Common Errors and Solutions

### Error: No files matching pattern

**Full error:**

```
[error] No files matching the pattern were found: "**/*.json"
```

**Cause:** Pattern doesn't match any files or incorrect working directory.

**Fix:**

```bash
# Check current directory
pwd

# Use correct pattern
npx prettier --write "**/*.json" --no-error-on-unmatched-pattern
```

### Error: Unexpected token

**Full error:**

```
[error] src/file.json: SyntaxError: Unexpected token
```

**Cause:** Invalid JSON/YAML syntax. Prettier can't parse malformed files.

**Fix:** Manually fix syntax errors before formatting:

```bash
# Validate JSON
node -e "JSON.parse(require('fs').readFileSync('./file.json'))"

# Then format
npx prettier --write ./file.json
```

### Error: Permission denied

**Cause:** Files locked or no write permissions.

**Fix:** Close editors, check permissions:

```bash
chmod u+w ./file.json
npx prettier --write ./file.json
```

### Warning: Ignored files

**Not an error**: Files listed in `.prettierignore` are skipped.

**Example `.prettierignore`:**

```
node_modules/
dist/
build/
*.min.js
package-lock.json
```

## Integration with .prettierrc.json

The `.prettierrc.json` file at repository root defines formatting rules:

```json
{
  "semi": true,
  "trailingComma": "es5",
  "singleQuote": true,
  "printWidth": 100,
  "tabWidth": 2,
  "useTabs": false,
  "arrowParens": "always",
  "endOfLine": "lf"
}
```

Prettier automatically uses this configuration. No additional setup needed.

## Integration with Pre-commit Hooks

The `.pre-commit-config.yaml` includes Prettier:

```yaml
- repo: https://github.com/pre-commit/mirrors-prettier
  rev: v4.0.0-alpha.8
  hooks:
    - id: prettier
      types_or: [javascript, jsx, ts, tsx, json, markdown, css, scss]
      exclude: '^\.pre-commit-config\.yaml$'
```

**Automatic formatting on commit:**

```bash
# Setup pre-commit (one-time)
./setup-pre-commit.sh

# Commit triggers auto-format
git add .
git commit -m "Your message"
# Prettier runs automatically on staged files
```

## CI/CD Integration

In CI pipelines, use check mode to enforce formatting:

```bash
# Fail build if code not formatted
npx prettier --check "**/*.{json,yml,yaml,md}"
```

Exit code non-zero = formatting violations = build failure.

## Performance Tips

1. **Format specific patterns**: Target only necessary file types
2. **Use .prettierignore**: Exclude large directories (node_modules, dist)
3. **Run in parallel**: Format different file types separately
4. **IDE integration**: Configure editor to format on save
5. **Cache results**: Some CI systems cache prettier results

## Advanced Scenarios

### Format Only Changed Files

```bash
# Git-based: format only modified files
git diff --name-only --diff-filter=ACMR | grep -E '\.(json|yml|yaml|md)$' | xargs npx prettier --write
```

### Format with EditorConfig Integration

Prettier respects `.editorconfig` settings. Properties like `indent_size`, `end_of_line` in `.editorconfig` override `.prettierrc.json`.

**Priority order:**

1. `.editorconfig` (if present)
2. `.prettierrc.json`
3. Prettier defaults

### Ignore Specific Files

Add to `.prettierignore`:

```
# Ignore build artifacts
dist/
build/
*.min.js

# Ignore generated files
**/generated/**
**/__generated__/**

# Ignore dependencies
node_modules/
package-lock.json
```

### Format from Any Directory

```bash
# Specify full path
npx prettier --write /home/runner/work/pigeon-pea/pigeon-pea/**/*.json

# Or use --cwd
npx prettier --write --cwd /home/runner/work/pigeon-pea/pigeon-pea "**/*.json"
```

## File Type Support

| File Type  | Extensions  | Formatted By Prettier  |
| ---------- | ----------- | ---------------------- |
| JSON       | .json       | ✅                     |
| YAML       | .yml, .yaml | ✅                     |
| Markdown   | .md         | ✅                     |
| JavaScript | .js, .jsx   | ✅                     |
| TypeScript | .ts, .tsx   | ✅                     |
| CSS        | .css, .scss | ✅                     |
| HTML       | .html       | ✅                     |
| C#         | .cs         | ❌ (use dotnet format) |
| Python     | .py         | ❌ (use black)         |

## Verification Steps

1. **Check exit code**: `echo $?` (should be 0)
2. **Review output**: Look for "✔ Formatted X files" or no output if already formatted
3. **Check git diff**: `git diff` to see what changed
4. **Verify pre-commit**: `pre-commit run prettier --all-files`

## Before Commit Checklist

- [ ] Format files: `npx prettier --write "**/*.{json,yml,yaml,md}"`
- [ ] Review changes: `git diff`
- [ ] Stage files: `git add .`
- [ ] Pre-commit runs automatically on commit
- [ ] If pre-commit fails, fix issues and re-commit

## Related Procedures

- **Format .NET files**: See [`dotnet-format.md`](dotnet-format.md)
- **Format everything**: See [`fix-all.md`](fix-all.md)
- **Configuration**: See `.prettierrc.json` and `.prettierignore` in repository root

## Quick Reference

```bash
# Format all supported files
npx prettier --write "**/*.{json,yml,yaml,md,js,jsx,ts,tsx}"

# Verify only (CI/CD)
npx prettier --check "**/*.{json,yml,yaml,md}"

# Format specific type
npx prettier --write "**/*.json"
npx prettier --write "**/*.{yml,yaml}"
npx prettier --write "**/*.md"

# Format specific file
npx prettier --write ./README.md

# List unformatted files
npx prettier --list-different "**/*.{json,yml,yaml,md}"
```

## Summary

- **Command**: `npx prettier --write "**/*.{json,yml,yaml,md}"`
- **Location**: Repository root
- **Modifies**: JSON, YAML, Markdown, JS, TS files in-place
- **Rules**: Defined in `.prettierrc.json`
- **Ignore**: Files in `.prettierignore` are skipped
- **Verify**: Use `--check` for check-only mode
- **Integration**: Pre-commit hooks auto-format on commit
