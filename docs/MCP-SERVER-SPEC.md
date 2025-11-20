# Documentation Management MCP Server Specification

**Purpose**: Provide AI agents with programmatic access to documentation management functions through the Model Context Protocol (MCP).

## Overview

The Documentation MCP Server exposes the documentation organization system's functionality through a standardized interface, allowing agents to:
- Search and discover existing documentation
- Create and update documentation with proper validation
- Check quality scores and implementation status
- Sync RFC status with task-master tasks

## Server Configuration

### Installation

```json
{
  "mcpServers": {
    "documentation": {
      "command": "python",
      "args": ["-m", "mcp_servers.documentation"],
      "cwd": "${workspaceFolder}",
      "env": {
        "DOCS_DIR": "${workspaceFolder}/docs",
        "SCRIPTS_DIR": "${workspaceFolder}/scripts"
      }
    }
  }
}
```

## Resources

### 1. Documentation Registry

**URI**: `docs://registry`

**Description**: Machine-readable registry of all documentation

**Returns**:
```json
{
  "documents": [
    {
      "path": "docs/rfcs/012-documentation-organization-management.md",
      "doc_id": "RFC-00012",
      "title": "Documentation Organization Management",
      "doc_type": "rfc",
      "status": "active",
      "tags": ["infrastructure", "documentation"],
      "summary": "Structured documentation management system"
    }
  ],
  "summary": {
    "total": 59,
    "by_type": {"rfc": 18, "guide": 8, "adr": 7},
    "by_status": {"active": 41, "draft": 14}
  }
}
```

### 2. Quality Report

**URI**: `docs://quality-report`

**Description**: Current documentation quality metrics

**Returns**:
```json
{
  "average_score": 75,
  "total_documents": 59,
  "by_grade": {
    "excellent": 0,
    "good": 41,
    "acceptable": 14,
    "needs_improvement": 4
  },
  "top_quality": [...],
  "needs_improvement": [...],
  "orphaned": [...]
}
```

### 3. Implementation Dashboard

**URI**: `docs://dashboard`

**Description**: RFC implementation status

**Returns**:
```json
{
  "total_rfcs": 18,
  "by_status": {
    "not-started": 18,
    "in-progress": 0,
    "completed": 0,
    "blocked": 0
  },
  "active_rfcs": [...],
  "blocked_rfcs": [...]
}
```

### 4. Document by ID

**URI**: `docs://doc/{doc_id}`

**Example**: `docs://doc/RFC-00012`

**Description**: Retrieve specific document by doc_id

**Returns**:
```json
{
  "doc_id": "RFC-00012",
  "title": "Documentation Organization Management",
  "path": "docs/rfcs/012-documentation-organization-management.md",
  "front_matter": {...},
  "content": "...",
  "quality_score": 88,
  "related_docs": ["RFC-00004"]
}
```

## Tools

### 1. search_documentation

**Description**: Search documentation by query

**Parameters**:
- `query` (string, required): Search query
- `doc_type` (string, optional): Filter by document type
- `tags` (array, optional): Filter by tags
- `status` (string, optional): Filter by status

**Returns**: Array of matching documents

**Example**:
```json
{
  "query": "plugin architecture",
  "doc_type": "rfc",
  "tags": ["architecture"]
}
```

### 2. create_documentation

**Description**: Create new documentation with validation

**Parameters**:
- `title` (string, required): Document title
- `doc_type` (string, required): Document type
- `content` (string, required): Document content
- `tags` (array, required): Topic tags
- `summary` (string, required): One-sentence summary
- `related` (array, optional): Related doc_ids
- `implements` (string, optional): PRD this implements (for RFCs)

**Returns**: Created document with doc_id

**Validation**: Automatically validates and assigns next doc_id

**Example**:
```json
{
  "title": "Game Services Architecture",
  "doc_type": "rfc",
  "content": "# RFC: Game Services Architecture\n\n...",
  "tags": ["architecture", "services"],
  "summary": "Six core game services with tiered plugin architecture"
}
```

### 3. update_documentation

**Description**: Update existing documentation

**Parameters**:
- `doc_id` (string, required): Document to update
- `content` (string, optional): New content
- `front_matter` (object, optional): Front-matter updates
- `implementation_status` (string, optional): Update implementation status

**Returns**: Updated document

**Example**:
```json
{
  "doc_id": "RFC-00042",
  "implementation_status": "in-progress",
  "front_matter": {
    "implementation": {
      "completion": 50,
      "tasks": ["task-456"]
    }
  }
}
```

### 4. validate_documentation

**Description**: Validate documentation without saving

**Parameters**:
- `doc_id` (string, optional): Validate specific document
- `content` (string, optional): Validate draft content
- `front_matter` (object, optional): Validate front-matter

**Returns**: Validation results with errors/warnings

**Example**:
```json
{
  "front_matter": {
    "doc_id": "RFC-00043",
    "title": "My RFC",
    "doc_type": "rfc",
    "status": "draft"
  }
}
```

### 5. check_quality

**Description**: Check quality score for document

**Parameters**:
- `doc_id` (string, optional): Check specific document
- `content` (string, optional): Check draft content

**Returns**: Quality metrics and score

**Example**:
```json
{
  "doc_id": "RFC-00012"
}
```

**Returns**:
```json
{
  "overall": 88,
  "grade": "good",
  "metrics": {
    "completeness": 90,
    "freshness": 85,
    "linkage": 80,
    "clarity": 95
  },
  "recommendations": [...]
}
```

### 6. sync_rfc_tasks

**Description**: Sync RFC implementation status with task-master

**Parameters**:
- `tasks_file` (string, optional): Path to tasks.json
- `rfc_id` (string, optional): Sync specific RFC

**Returns**: Sync results

**Example**:
```json
{
  "tasks_file": ".taskmaster/tasks.json"
}
```

### 7. find_next_doc_id

**Description**: Find next available doc_id number

**Parameters**:
- `prefix` (string, required): Doc ID prefix (RFC, ADR, PRD, etc.)

**Returns**: Next available number

**Example**:
```json
{
  "prefix": "RFC"
}
```

**Returns**:
```json
{
  "next_number": 43,
  "next_doc_id": "RFC-00043",
  "suggested_filename": "043-your-title.md"
}
```

### 8. list_related_docs

**Description**: Find documents related to a topic or doc_id

**Parameters**:
- `doc_id` (string, optional): Find docs related to this one
- `topic` (string, optional): Find docs about this topic
- `include_dependencies` (boolean, optional): Include dependency graph

**Returns**: Related documents with relationship types

**Example**:
```json
{
  "doc_id": "RFC-00013",
  "include_dependencies": true
}
```

### 9. regenerate_indexes

**Description**: Regenerate all documentation indexes

**Parameters**: None

**Returns**: Status of regeneration

**Side Effects**:
- Regenerates `docs/INDEX.md`
- Regenerates `docs/DASHBOARD.md`
- Regenerates `docs/DEPENDENCIES.md`
- Updates `docs/index/registry.json`

## Prompts

### 1. create_rfc

**Description**: Guide agent through RFC creation process

**Arguments**:
- `topic` (string): RFC topic

**Flow**:
1. Search for existing RFCs on topic
2. Check for near-duplicates
3. Find next RFC number
4. Generate front-matter template
5. Provide RFC structure template
6. Validate before saving

### 2. update_rfc_status

**Description**: Update RFC implementation status

**Arguments**:
- `rfc_id` (string): RFC to update
- `status` (string): New status
- `completion` (number): Completion percentage

### 3. find_documentation

**Description**: Help agent find relevant documentation

**Arguments**:
- `query` (string): What to search for

**Flow**:
1. Search registry
2. Check quality scores
3. Show related documents
4. Suggest most relevant

## Implementation Notes

### Server Structure

```
mcp_servers/
└── documentation/
    ├── __init__.py
    ├── __main__.py          # MCP server entry point
    ├── resources.py         # Resource handlers
    ├── tools.py             # Tool implementations
    ├── prompts.py           # Prompt templates
    └── utils.py             # Helper functions
```

### Dependencies

```python
# requirements.txt
mcp>=0.1.0
pyyaml>=6.0
```

### Error Handling

All tools should return structured errors:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "doc_id format invalid",
    "details": {
      "field": "doc_id",
      "expected": "PREFIX-NNNNN",
      "received": "RFC-2025-00042"
    }
  }
}
```

## Usage Examples

### Agent Creating New RFC

```python
# 1. Search for existing docs
results = await mcp.call_tool("search_documentation", {
    "query": "plugin architecture",
    "doc_type": "rfc"
})

# 2. Find next RFC number
next_id = await mcp.call_tool("find_next_doc_id", {"prefix": "RFC"})

# 3. Create RFC
doc = await mcp.call_tool("create_documentation", {
    "title": "Enhanced Plugin System",
    "doc_type": "rfc",
    "content": rfc_content,
    "tags": ["architecture", "plugins"],
    "summary": "Tiered plugin architecture with contracts"
})

# 4. Check quality
quality = await mcp.call_tool("check_quality", {"doc_id": doc["doc_id"]})
```

### Agent Updating Implementation Status

```python
# Update RFC implementation
await mcp.call_tool("update_documentation", {
    "doc_id": "RFC-00042",
    "implementation_status": "in-progress",
    "front_matter": {
        "implementation": {
            "completion": 75,
            "tasks": ["task-789"]
        }
    }
})

# Sync with task-master
await mcp.call_tool("sync_rfc_tasks", {
    "rfc_id": "RFC-00042"
})

# Regenerate dashboard
await mcp.call_tool("regenerate_indexes")
```

## Future Enhancements

1. **Real-time Validation**: WebSocket support for live validation
2. **Diff Generation**: Show changes before applying
3. **Batch Operations**: Update multiple docs at once
4. **Template Management**: Custom templates for different doc types
5. **Analytics**: Usage patterns and documentation health metrics

## See Also

- [MCP Specification](https://modelcontextprotocol.io/)
- [Documentation Schema](../docs/DOCUMENTATION-SCHEMA.md)
- [Agent Rules](../docs/AGENT-RULES-QUICK-REF.md)
