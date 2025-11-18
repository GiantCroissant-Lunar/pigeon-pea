# Windsurf Workflows

This directory contains Windsurf Cascade workflows for the PigeonPea project.

## What Are Workflows?

Workflows are structured, step-by-step guides that help Windsurf's Cascade AI autonomously complete complex, multi-step tasks. They provide a consistent, repeatable process for common development activities.

## Available Workflows

### `/build-and-test`
**Purpose:** Autonomously build, test, and fix the console application

**When to use:**
- After making code changes
- Before committing
- To verify everything works

**What it does:**
1. Builds the console app
2. Fixes any build errors
3. Runs tests
4. Fixes any test failures
5. Runs the application
6. Verifies everything works

### `/feature`
**Purpose:** Complete feature development workflow from planning to PR

**When to use:**
- Starting a new feature
- Need structured guidance through feature development

**What it does:**
1. Planning: analyzes requirements, reviews code, creates task breakdown
2. Implementation: creates branch, implements code, writes tests
3. Quality checks: formatting, pre-commit, testing
4. Integration: merges main, creates pull request
5. Reports completion status

### `/fix-bug`
**Purpose:** Systematic bug investigation and fixing

**When to use:**
- Bug reports from users
- Reproducing and fixing issues
- Need structured debugging workflow

**What it does:**
1. Reproduces the bug
2. Investigates and identifies root cause
3. Implements the fix
4. Adds regression test
5. Verifies the fix works
6. Commits the changes

### `/commit`
**Purpose:** Autonomous commit workflow with full verification

**When to use:**
- Ready to commit changes
- Want automated pre-commit verification
- Need help with commit messages

**What it does:**
1. Formats code
2. Runs pre-commit hooks
3. Builds and tests
4. Runs the application
5. Creates proper commit message
6. Commits the changes

### `/pr`
**Purpose:** Create well-formatted pull request

**When to use:**
- Feature or fix is complete
- Ready to create pull request
- Need help with PR description

**What it does:**
1. Syncs with main branch
2. Runs quality checks
3. Reviews all changes
4. Pushes branch
5. Creates detailed PR with proper format
6. Reports PR URL

## How to Use Workflows

### Basic Usage

In Windsurf Cascade chat, type:
```
/build-and-test
```

Windsurf will then follow all the steps defined in that workflow autonomously.

### Combining Workflows

You can chain workflows:
```
/build-and-test then /commit
```

Or reference them in custom instructions:
```
Please implement the login feature, then run /build-and-test
```

### Viewing Workflows

To see all available workflows:
1. Click the `Customizations` icon (top-right)
2. Select `Workflows` tab
3. Browse available workflows

### Creating New Workflows

1. Create a new `.md` file in this directory
2. Add clear title and description
3. Structure as numbered steps
4. Use code blocks for commands
5. Workflow name = filename without `.md`

**Example:**
- File: `deploy.md`
- Command: `/deploy`

## Workflow Best Practices

### For Users

- **Start with workflows** for common tasks instead of ad-hoc instructions
- **Trust the process** - workflows are designed to be thorough
- **Review results** - workflows report what they did
- **Customize as needed** - edit workflow files to match your process

### For Workflow Authors

- **Be specific** - include exact commands
- **Be autonomous** - don't ask user for input unless necessary
- **Be thorough** - include verification steps
- **Be clear** - use numbered steps and clear descriptions
- **Use code blocks** - for all commands
- **Report results** - always end with a summary

## Integration with Rules

Workflows complement the rules in `.windsurf/rules.md`:

- **Rules:** Persistent context, how to behave
- **Workflows:** Step-by-step processes, what to do

Workflows should follow the autonomous development principles from the rules:
- Build and run autonomously
- Fix errors yourself
- Only ask user for design decisions

## File Format

Workflows are Markdown files with:
- Clear title (H1)
- Brief description
- Numbered steps (H2 or H3)
- Code blocks for commands
- Verification steps
- Summary/report section

**Max size:** 12,000 characters per workflow

## Workflow Discovery

Windsurf automatically discovers workflows in:
- `.windsurf/workflows/` in current workspace
- `.windsurf/workflows/` in subdirectories
- `.windsurf/workflows/` in parent directories (up to git root)

## Related Documentation

- **Rules:** `.windsurf/rules.md` - Core behavior and principles
- **Agent Rules:** `.agent/rules/autonomous-development.md` - Detailed autonomous dev guidelines
- **Provider Config:** `.agent/providers/windsurf.yaml` - Windsurf-specific configuration
- **Task Commands:** `Taskfile.yml` - All available task commands

## Examples

### Quick Fix Workflow
```
User: "There's a bug in GameView rendering"
Cascade: "/fix-bug"
[Windsurf reproduces, investigates, fixes, tests, commits autonomously]
```

### Feature Development
```
User: "Add pause menu to the game"
Cascade: "/feature"
[Windsurf plans, implements, tests, creates PR autonomously]
```

### Before Committing
```
User: "I've made some changes, ready to commit"
Cascade: "/commit"
[Windsurf formats, tests, builds, commits autonomously]
```

## Tips

1. **Use workflows for consistency** - Same process every time
2. **Combine with custom instructions** - "Implement X then /build-and-test"
3. **Edit workflows** - Customize to match your team's process
4. **Create project-specific workflows** - For unique project needs
5. **Keep workflows focused** - One clear purpose per workflow

## Contributing

To add new workflows:
1. Identify a repetitive multi-step process
2. Document the steps clearly
3. Test the workflow
4. Add to this README
5. Consider adding examples
