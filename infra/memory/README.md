# Memory Infrastructure

This directory contains the memory system infrastructure for pigeon-pea project.

## Status: PREPARED BUT NOT ACTIVATED

**Do NOT activate yet** - Wait until the dotnet reorganization is complete and merged.

## What's Here

```
infra/memory/
├── data/                      # Memory storage (gitignored)
│   ├── project-memory.jsonl   # File-based memory store
│   └── memory-test.jsonl      # Test data
├── logs/                      # Operation logs (gitignored)
│   └── memory-mcp.log         # MCP server activity log
├── scripts/                   # Executable scripts
│   ├── mcp-server.py          # MCP server implementation
│   └── test-visibility.py     # Validation test script
├── docker-compose.yml         # Qdrant vector database (for future semantic search)
└── README.md                  # This file
```

## Architecture

### Phase 1: File-based Memory (Current)
- **Storage**: Simple JSONL file (`data/project-memory.jsonl`)
- **Search**: Basic keyword matching
- **Cost**: $0
- **Complexity**: Minimal
- **Visibility**: Full logging to `logs/memory-mcp.log`

### Phase 2: Semantic Search (Future)
- **Storage**: Qdrant vector database
- **Search**: Semantic similarity using embeddings
- **Embedding model**: OpenAI `text-embedding-3-small`
- **Cost**: ~$0.10-$1.00/month (extremely cheap)
- **When to upgrade**: When keyword search becomes insufficient

## How to Activate

### Step 1: Test the File-Based System

**After dotnet reorg is merged:**

1. **Add MCP server to Claude Code**

   Edit `.claude/mcp_servers.json`:
   ```json
   {
     "project-memory": {
       "command": "python",
       "args": ["infra/memory/scripts/mcp-server.py"],
       "env": {
         "PYTHONIOENCODING": "utf-8"
       }
     }
   }
   ```

2. **Restart Claude Code** to load the MCP server

3. **Verify tools are available**
   - You should see: `save_memory`, `search_memory`, `list_recent_memories`

4. **Test with explicit prompts**
   ```
   User: "Use the save_memory tool to remember: We use GoRogue for FOV in Dungeon.Core"

   (New session)

   User: "Use the search_memory tool to find info about FOV"
   ```

5. **Check the logs**
   ```bash
   tail -f infra/memory/logs/memory-mcp.log
   ```

   You should see:
   ```
   [INFO] SAVE: key=tech.fov, content_len=47, metadata={}
   [INFO] SEARCH: query='FOV', found=1, returned=1
   ```

### Step 2: Validate It's Actually Being Used

**Key validation points:**

- [ ] Can see save operations in `memory-mcp.log`
- [ ] Can see search operations in `memory-mcp.log`
- [ ] Agent's responses include retrieved memories
- [ ] Memories persist across Claude Code sessions
- [ ] Search hit rate >10% (agent queries memory in relevant contexts)

**If any of these fail, STOP and debug before proceeding.**

### Step 3: (Optional) Add Semantic Search

**Only if file-based search is insufficient:**

1. **Start Qdrant**
   ```bash
   cd infra/memory
   docker compose up -d
   ```

2. **Get OpenAI API key**
   - Sign up at https://platform.openai.com/
   - Add $5 credit (will last months)
   - Generate API key

3. **Upgrade memory system** to use Qdrant + embeddings
   - Migrate existing memories from JSONL to Qdrant
   - Configure embedding model in MCP server
   - Test semantic search vs keyword search

## Cost Estimates

### File-based (Phase 1)
- **Storage**: Free (local disk)
- **Compute**: Free (local CPU)
- **Total**: $0/month

### With Semantic Search (Phase 2)
- **Storage**: Free (self-hosted Qdrant)
- **Embeddings**: $0.02 per 1M tokens
- **Realistic usage**: $0.10-$1.00/month
- **Heavy usage**: ~$2/month max
- **Total**: < $2/month

## What to Store

### ✅ Good candidates for memory:

- **Architecture decisions**
  - "Map and Dungeon are separate domains"
  - "We use Arch ECS for entity management"

- **Technology choices**
  - "FOV uses GoRogue library"
  - "Terminal rendering uses Terminal.Gui v2 + SadConsole"

- **Project conventions**
  - "Bean naming: chickpea, pigeon-pea, etc."
  - "RFCs follow RFC-012 doc management system"

- **Integration patterns**
  - "Map.Rendering → Shared.Rendering abstraction"
  - "Console app uses plugin architecture"

### ❌ Don't store:

- Code currently in your context window
- Information well-documented in RFCs (just reference the RFC)
- Implementation details that change frequently
- Temporary scratch notes

## Troubleshooting

### Memory not being retrieved

1. **Check logs**: `tail -f infra/memory/logs/memory-mcp.log`
   - If no SEARCH entries → MCP tool not being called
   - If SEARCH returns 0 results → Bad keywords or empty store

2. **Verify MCP server is running**
   - Restart Claude Code
   - Check Claude Code logs for MCP errors

3. **Test manually**
   ```bash
   python infra/memory/scripts/test-visibility.py
   ```

### Poor search results

- **Keyword search** (Phase 1) only matches exact text
- Use descriptive keys: `tech.fov-library` instead of just `fov`
- Store multiple entries for the same concept with different keywords
- **Or upgrade to semantic search** (Phase 2) if this becomes a problem

### Logs not appearing

- Check encoding: Windows may need `PYTHONIOENCODING=utf-8`
- Check permissions: `infra/memory/logs/` should be writable
- Check path: MCP server might be running from different directory

## Integration with Windsurf

Windsurf can use the same MCP server:

1. Add to Windsurf's MCP config (location varies by version)
2. Same commands work across both editors
3. Shared memory between Claude Code and Windsurf

## Next Steps

1. ✅ Infrastructure prepared (you are here)
2. ⏸️ Wait for dotnet reorganization to complete
3. ⏳ Add MCP server to `.claude/mcp_servers.json`
4. ⏳ Test with explicit save/search commands
5. ⏳ Validate agent actually uses it
6. ⏳ Decide: keep file-based or upgrade to semantic search
7. ⏳ If upgrading: start Qdrant, add embeddings, migrate data

## Questions?

See the full validation plan: `docs/_inbox/memory-system-validation-plan.md`

## References

- MCP Server: `infra/memory/scripts/mcp-server.py`
- Test script: `infra/memory/scripts/test-visibility.py`
- Validation plan: `docs/_inbox/memory-system-validation-plan.md`
- Docker Compose: `infra/memory/docker-compose.yml`
