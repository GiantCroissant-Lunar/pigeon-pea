# Gemini Agent Configuration

This directory contains Gemini-specific agent configuration and workflows.

## Overview

Gemini (Google AI Studio / Gemini Code Assist) is configured as one of the supported AI platforms for this project.

## Key Features

### 1. Extended Context Window
- **1M tokens**: Largest context window available
- Can hold entire large codebases in memory
- Reduces need for repeated file reads

### 2. Multimodal Capabilities
- Process images (architecture diagrams, screenshots)
- Analyze video content
- Handle audio input

### 3. Grounding
- Access to Google Search for research
- Verify information about external libraries
- Get up-to-date API documentation

### 4. Extended Thinking
- Visible reasoning process
- Complex problem-solving
- Multi-step analysis

### 5. Code Execution
- Run Python code directly
- Validate complex algorithms
- Test logic before implementation

## Configuration Files

- **Adapter**: `.agent/adapters/gemini-adapter.yaml`
- **Provider**: `.agent/providers/gemini.yaml`
- **Workflows**: `.gemini/workflows/` (generated from `.agent/workflows/`)

## Workflow Invocation

Use workflows with:
```
@workflow-name
```

Example:
```
@create-rfc
@update-rfc-status
@sync-documentation
```

## MCP Integration

Gemini supports MCP servers. Configure in:
- `.gemini/mcp-config.json` (if using local Gemini setup)
- Or through Google AI Studio settings

## Generating Workflows

Convert canonical workflows to Gemini format:

```bash
python scripts/generate-workflows.py --platform gemini
```

This creates Gemini-specific workflow files in `.gemini/workflows/`.

## Best Practices

### Leverage Large Context
```yaml
# Instead of reading files repeatedly
# Load entire codebase once and keep in context
```

### Use Thinking Mode
```yaml
# For complex architectural decisions
# Enable extended thinking to see reasoning
```

### Enable Grounding When Needed
```yaml
# When researching unfamiliar libraries
# Or verifying external API documentation
```

### Multimodal Input
```yaml
# Share architecture diagrams
# Screenshot error messages
# Show UI mockups
```

## Comparison with Other Platforms

| Feature | Gemini | Claude | Windsurf | Cursor |
|---------|--------|--------|----------|--------|
| Context Window | 1M | 200K | Varies | Varies |
| Multimodal | ✅ | ✅ | ❌ | ❌ |
| Grounding | ✅ | ❌ | ❌ | ❌ |
| Code Execution | ✅ | ❌ | ❌ | ❌ |
| MCP Support | ✅ | ✅ | ✅ | ✅ |
| Thinking Mode | ✅ | ✅ | ❌ | ❌ |

## See Also

- [Agent Infrastructure](../.agent/README.md)
- [Workflow Schema](../.agent/workflows/SCHEMA.md)
- [Provider Configs](../.agent/providers/README.md)
