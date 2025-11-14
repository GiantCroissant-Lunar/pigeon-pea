# Memory System Validation Plan

## Problem Statement
Previous attempts at agent memory failed because:
- Unclear if memory was being retrieved
- No visibility into what was stored
- Couldn't validate if it improved agent performance

## Validation-First Approach

### Phase 1: Proof of Concept (Week 1)
**Goal:** Prove memory read/write works with full visibility

**Setup:**
```yaml
# docker-compose.memory-test.yml
services:
  qdrant:
    image: qdrant/qdrant:latest
    ports: ["6333:6333"]
    volumes:
      - ./data/qdrant:/qdrant/storage

  # Logging proxy - sits between agent and Qdrant
  memory-logger:
    # Logs all operations to ./logs/memory-ops.jsonl
```

**Test Cases:**
1. **Explicit Save Test**
   - Tell agent: "Remember: Dungeon.Core uses GoRogue for FOV"
   - Check log: Did it save?
   - Inspect Qdrant: Is the vector there?

2. **Explicit Retrieve Test**
   - New session: "What library do we use for FOV?"
   - Check log: Did it query memory?
   - Verify: Did response mention GoRogue?

3. **Implicit Retrieve Test**
   - New session: "I need to implement field-of-view"
   - Check: Did it auto-retrieve the GoRogue decision?

**Success Criteria:**
- [ ] Can see every memory operation in logs
- [ ] Can manually query Qdrant and see stored memories
- [ ] Agent's response demonstrates it used retrieved memory
- [ ] Hit rate >30% (agent queries memory in relevant contexts)

### Phase 2: Integration (Week 2)
**Only proceed if Phase 1 succeeds**

**Setup MCP Server:**
```json
// .claude/mcp_servers.json
{
  "memory": {
    "command": "node",
    "args": ["./scripts/memory-mcp-server.js"],
    "env": {
      "QDRANT_URL": "http://localhost:6333",
      "OPENAI_API_KEY": "${OPENAI_API_KEY}",
      "LOG_FILE": "./logs/memory-mcp.log"
    }
  }
}
```

**Test with Claude Code:**
- Configure MCP server
- Verify tool shows up in available tools
- Test explicit tool calls
- Monitor usage in logs

### Phase 3: Cost Monitoring (Week 3)
**Track actual costs:**
```bash
# Daily cost tracker
python scripts/track-embedding-costs.py
# Outputs:
# - Tokens embedded today
# - Estimated cost
# - ROI estimate (did memory save tokens in completions?)
```

**Expected costs (based on research):**
- Light usage: $0.10-0.30/month
- Heavy usage: $0.50-1.00/month
- Extreme: ~$2/month

## Red Flags to Abort
Stop and reassess if:
- ❌ Memory retrieval rate <10% (agent not using it)
- ❌ Retrieved memories not relevant >50% of time
- ❌ Can't easily see what's being stored/retrieved
- ❌ Embedding costs >$5/month
- ❌ More time spent managing memory than it saves

## Success Metrics
Continue if:
- ✅ Can demonstrate memory retrieval in agent responses
- ✅ Agent provides context from previous sessions
- ✅ Reduces need to re-explain project conventions
- ✅ Costs <$2/month
- ✅ Clear visibility into operations

## Technology Decisions

### Embedding Model
**Recommended:** `text-embedding-3-small`
- Cost: $0.02 per 1M tokens
- Quality: Good for semantic search
- Provider: OpenAI API (separate from ChatGPT Plus)

**Alternative (free):** Local embeddings via Ollama
- Model: `bge-small-en-v1.5`
- Cost: $0
- Quality: Slightly lower but acceptable
- Tradeoff: Need to run locally

### Vector Store
**Recommended:** Qdrant (self-hosted)
- Free, open source
- Easy Docker setup
- Good UI for inspection
- Works with Supermemory/Letta

### Memory Layer
**Start with:** Supermemory + MCP
- Simpler than Letta
- Just memory, not full agent runtime
- Works with Claude Code, Windsurf
- Can add Letta later if needed

## Next Steps

1. **Week 1:** Set up Qdrant + basic logging
2. **Week 2:** Create test MCP server with verbose logging
3. **Week 3:** Run validation tests
4. **Week 4:** Decide: continue, modify, or abandon

## Notes
- Don't over-invest before validating
- Observability is critical
- Start minimal, expand only if proven valuable
- Track costs from day 1
