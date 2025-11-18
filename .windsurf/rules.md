# Windsurf Agent Rules for PigeonPea

## Core Directive

**BE AUTONOMOUS. Build, run, test, and fix code yourself. Never ask the user to do these tasks.**

## Quick Reference

You are working on the PigeonPea roguelike game project. This is a .NET/C# project with a console application.

### Console App Paths

- **Main Console App**: `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/PigeonPea.Console.csproj`
- **Demos**: `projects/dungeon/dotnet/console-app/demos/src/PigeonPea.Console.Demos/PigeonPea.Console.Demos.csproj`

### Essential Commands

```bash
# Build and run console app (RECOMMENDED)
task game:build-and-run-console

# Build console app only
task game:build-console

# Run console app only
task game:run-console

# Build entire .NET solution
task dotnet:build

# Run tests
task dotnet:test

# Format code
task dotnet:format

# Run pre-commit hooks
pre-commit run --all-files

# List all available tasks
task --list
```

## Autonomous Workflow

### 1. Build and Run Yourself

When the user asks you to modify code or reports an issue:

1. **Build the project**
   ```bash
   task game:build-console
   ```

2. **Check build logs** - Always read build logs for errors
   ```bash
   cat "$(ls -t build/_artifacts/latest/build-logs/*.log | head -1)"
   ```

3. **Fix build issues autonomously**
   - Read error messages
   - Extract file paths and line numbers
   - Open relevant files
   - Make corrections
   - Rebuild

4. **Run the application**
   ```bash
   task game:run-console
   ```

5. **Check runtime logs** - Always read runtime logs
   ```bash
   cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"
   ```

6. **Fix runtime issues** - Analyze stack traces and errors

7. **Iterate** - Fix, rebuild, rerun until working

### 2. Never Ask User To Do This

❌ **DON'T SAY:**
- "Can you build the project and paste the errors?"
- "Please run the app and tell me what happens"
- "Try this and let me know if it works"
- "Build it and share the output"

✅ **INSTEAD DO:**
- Build it yourself
- Run it yourself
- Read the errors yourself
- Fix the issues yourself
- Verify it works yourself
- Then tell user: "Fixed and verified working"

### 3. When to Ask the User

Only ask for:
- **Design decisions**: "Should we use approach A or B?"
- **Requirements**: "What should the behavior be when X happens?"
- **Preferences**: "Do you want feature X or Y?"
- **When stuck**: "I've tried A, B, C and still seeing issue D. Need direction."

## Error Handling

### Compiler Errors

Example error:
```
GameView.cs(42,15): error CS0103: The name 'renderer' does not exist
```

Your response:
1. Parse: File=GameView.cs, Line=42, Error=CS0103 (name doesn't exist)
2. Read `GameView.cs`
3. Find line 42
4. Identify issue (missing field declaration)
5. Fix it (add `private IRenderer _renderer;`)
6. Rebuild
7. Continue

### Runtime Errors

Example:
```
System.NullReferenceException: Object reference not set to an instance
   at PigeonPea.Console.GameView.Render() in GameView.cs:line 67
```

Your response:
1. Read stack trace
2. Identify: GameView.cs, line 67
3. Read the file
4. Find the null reference
5. Fix it (add null check or initialize)
6. Rebuild and rerun
7. Verify fixed

## Agent Infrastructure

This project has comprehensive agent infrastructure in `.agent/`:

### Rules (Read These!)

- **`.agent/rules/autonomous-development.md`** - **START HERE** - Detailed autonomous development guidelines
- **`.agent/rules/code-quality.md`** - Code quality standards
- **`.agent/rules/git-commit-rules.md`** - Git and commit requirements
- **`.agent/rules/documentation-management.md`** - Documentation workflow

### Provider Config

- **`.agent/providers/windsurf.yaml`** - Your specific configuration (GPT-5.1)
- Defines your capabilities, preferences, and workflow

### Skills Available

Check `.agent/skills/` for reusable procedures:
- `dotnet-build/` - Build workflows
- `dotnet-test/` - Testing workflows
- `code-format/` - Formatting tools
- `code-analyze/` - Static analysis

### Workflows

Check `.agent/workflows/` for structured processes:
- `feature-development.yaml` - Feature development workflow

## Architecture Overview

### Domain Organization

- **Map Domain** (`dotnet/Map/`): World map generation
- **Dungeon Domain** (`dotnet/Dungeon/`): Dungeon generation, FOV, pathfinding
- Each domain: Core / Control / Rendering layers

### Shared Infrastructure

- `Shared.ECS`: Arch components (Position, Renderable, Health, etc.)
- `Shared.Rendering`: Rendering contracts, primitives, tiles

### Tech Stack

- **.NET 8.0** with C# 12
- **Arch ECS**: Entity Component System
- **GoRogue**: FOV, pathfinding, map generation
- **SkiaSharp**: Graphics rendering
- **Terminal.Gui**: Terminal UI framework

## Pre-commit Requirements

**CRITICAL**: All commits must pass pre-commit hooks.

Before committing:

```bash
# Run hooks
pre-commit run --all-files

# Fix any issues
# Commit only when hooks pass
```

Never use `--no-verify` to skip hooks!

## Communication Style

Be concise and results-focused:

✅ **Good:**
- "Built successfully. App runs without errors."
- "Fixed 3 compilation errors. Verified working."
- "Implemented feature X. Tested with scenario Y."

❌ **Bad:**
- "I've made changes. Can you test?"
- "This should work. Let me know if errors."
- "Try building and tell me what happens."

## Verification Before "Done"

Always verify:
- ✅ Builds without errors
- ✅ Runs without crashes
- ✅ Produces expected output
- ✅ No runtime exceptions
- ✅ Feature works as designed

## Example Session

```
User: "Fix the rendering bug in GameView"

You:
[run: task game:build-console]
[see error: CS0103 in GameView.cs:42]
[read GameView.cs]
[identify: missing field _renderer]
[add: private IRenderer _renderer;]
[run: task game:build-console - success]
[run: task game:run-console]
[check output - working correctly]

Response: "Fixed GameView.cs line 42 - added missing _renderer field. Built and tested successfully. Rendering now works correctly."
```

## Log Locations

### Build Logs (Compilation)
```bash
# Latest build log
cat "$(ls -t build/_artifacts/latest/build-logs/*.log | head -1)"

# Search for errors
grep -i "error" build/_artifacts/latest/build-logs/*.log
```

### Runtime Logs (Execution)
```bash
# Latest runtime log
cat "$(ls -t build/_artifacts/latest/PigeonPea.Console/logs/*.log 2>/dev/null | head -1)"

# Search for exceptions
grep -i "exception" build/_artifacts/latest/PigeonPea.Console/logs/*.log
```

**CRITICAL:** Always check logs after build/run, even if commands succeed. Hidden errors may exist in logs.

See `.agent/rules/build-artifacts-logs.md` for complete log location guide.

## Additional Resources

- **`CLAUDE.md`** - Main agent configuration
- **`.agent/rules/build-artifacts-logs.md`** - Log locations and debugging
- **`.agent/rules/autonomous-development.md`** - Detailed autonomous dev guidelines
- **`AGENTS.md`** - Complete agent infrastructure
- **`README.md`** - Project setup
- **`docs/`** - Project documentation

## Remember

**You have the tools. You have the access. Build it. Run it. Fix it. Verify it. Be autonomous.**

The user trusts you to handle the technical execution. Focus on delivering working results, not asking for help with tasks you can do yourself.
