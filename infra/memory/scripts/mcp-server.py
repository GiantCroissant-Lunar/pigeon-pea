#!/usr/bin/env python3
"""
Simple file-based memory MCP server
Provides full visibility into all memory operations

Usage:
  Add to .claude/mcp_servers.json:
  {
    "project-memory": {
      "command": "python",
      "args": ["infra/memory/scripts/mcp-server.py"]
    }
  }
"""
import json
import logging
import sys
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List

# Configuration (parent.parent because we're in scripts/ subdirectory)
MEMORY_FILE = Path(__file__).parent.parent / "data" / "project-memory.jsonl"
LOG_FILE = Path(__file__).parent.parent / "logs" / "memory-mcp.log"

# Setup logging
LOG_FILE.parent.mkdir(parents=True, exist_ok=True)
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[logging.FileHandler(LOG_FILE), logging.StreamHandler(sys.stderr)],
)
logger = logging.getLogger(__name__)


class MemoryStore:
    """Simple file-based memory storage with full visibility"""

    def __init__(self, storage_path: Path):
        self.storage_path = storage_path
        self.storage_path.parent.mkdir(parents=True, exist_ok=True)
        logger.info(f"Memory store initialized: {self.storage_path}")

    def save(self, key: str, content: str, metadata: Dict[str, Any] = None) -> Dict:
        """Save a memory entry"""
        entry = {
            "timestamp": datetime.now().isoformat(),
            "key": key,
            "content": content,
            "metadata": metadata or {},
        }

        with open(self.storage_path, "a", encoding="utf-8") as f:
            f.write(json.dumps(entry, ensure_ascii=False) + "\n")

        logger.info(f"SAVE: key={key}, content_len={len(content)}, metadata={metadata}")
        return entry

    def search(self, query: str, limit: int = 5) -> List[Dict]:
        """Search for memories (simple text matching for now)"""
        if not self.storage_path.exists():
            logger.warning("SEARCH: No memory file exists yet")
            return []

        matches = []
        query_lower = query.lower()

        with open(self.storage_path, "r", encoding="utf-8") as f:
            for line in f:
                entry = json.loads(line)
                # Simple keyword matching
                if (
                    query_lower in entry["key"].lower()
                    or query_lower in entry["content"].lower()
                ):
                    matches.append(entry)

        # Return most recent matches
        matches = sorted(matches, key=lambda x: x["timestamp"], reverse=True)[:limit]

        logger.info(
            f"SEARCH: query='{query}', found={len(matches)}, returned={len(matches)}"
        )
        return matches

    def list_recent(self, limit: int = 10) -> List[Dict]:
        """List recent memories"""
        if not self.storage_path.exists():
            return []

        entries = []
        with open(self.storage_path, "r", encoding="utf-8") as f:
            for line in f:
                entries.append(json.loads(line))

        # Return most recent
        recent = sorted(entries, key=lambda x: x["timestamp"], reverse=True)[:limit]
        logger.info(f"LIST_RECENT: returned={len(recent)} entries")
        return recent


def create_mcp_response(result: Any) -> Dict:
    """Create MCP-compliant response"""
    return {"jsonrpc": "2.0", "result": result}


def handle_mcp_request(request: Dict, memory: MemoryStore) -> Dict:
    """Handle incoming MCP requests"""
    method = request.get("method", "")
    params = request.get("params", {})

    logger.info(f"REQUEST: method={method}, params={params}")

    if method == "tools/list":
        return create_mcp_response(
            {
                "tools": [
                    {
                        "name": "save_memory",
                        "description": "Save important project knowledge",
                        "inputSchema": {
                            "type": "object",
                            "properties": {
                                "key": {
                                    "type": "string",
                                    "description": "Categorized key identifier",
                                },
                                "content": {
                                    "type": "string",
                                    "description": "The knowledge/decision to remember",
                                },
                                "metadata": {
                                    "type": "object",
                                    "description": (
                                        "Optional metadata (tags, project, type, etc.)"
                                    ),
                                },
                            },
                            "required": ["key", "content"],
                        },
                    },
                    {
                        "name": "search_memory",
                        "description": "Search saved project knowledge",
                        "inputSchema": {
                            "type": "object",
                            "properties": {
                                "query": {
                                    "type": "string",
                                    "description": "Query string",
                                },
                                "limit": {
                                    "type": "number",
                                    "description": "Max results to return (default: 5)",
                                },
                            },
                            "required": ["query"],
                        },
                    },
                    {
                        "name": "list_recent_memories",
                        "description": "List recent saved memories",
                        "inputSchema": {
                            "type": "object",
                            "properties": {
                                "limit": {
                                    "type": "number",
                                    "description": (
                                        "Number of recent entries (default: 10)"
                                    ),
                                }
                            },
                        },
                    },
                ]
            }
        )

    elif method == "tools/call":
        tool_name = params.get("name")
        arguments = params.get("arguments", {})

        if tool_name == "save_memory":
            result = memory.save(
                key=arguments["key"],
                content=arguments["content"],
                metadata=arguments.get("metadata", {}),
            )
            message = (
                f"✓ Saved memory: {arguments['key']}\n"
                f"Timestamp: {result['timestamp']}"
            )
            return create_mcp_response(
                {
                    "content": [
                        {
                            "type": "text",
                            "text": message,
                        }
                    ]
                }
            )

        elif tool_name == "search_memory":
            results = memory.search(
                query=arguments["query"], limit=arguments.get("limit", 5)
            )

            if not results:
                text = f"No memories found for: {arguments['query']}"
            else:
                text = f"Found {len(results)} memories:\n\n"
                for r in results:
                    text += f"[{r['timestamp']}] {r['key']}\n"
                    text += f"  {r['content'][:200]}...\n\n"

            return create_mcp_response({"content": [{"type": "text", "text": text}]})

        elif tool_name == "list_recent_memories":
            results = memory.list_recent(limit=arguments.get("limit", 10))

            text = f"Recent memories ({len(results)}):\n\n"
            for r in results:
                text += f"[{r['timestamp']}] {r['key']}\n"
                text += f"  {r['content'][:100]}...\n\n"

            return create_mcp_response({"content": [{"type": "text", "text": text}]})

        else:
            return create_mcp_response({"error": f"Unknown tool: {tool_name}"})

    else:
        return create_mcp_response({"error": f"Unknown method: {method}"})


def main():
    """Main MCP server loop"""
    logger.info("=" * 60)
    logger.info("Memory MCP Server Starting")
    logger.info(f"Memory file: {MEMORY_FILE}")
    logger.info(f"Log file: {LOG_FILE}")
    logger.info("=" * 60)

    memory = MemoryStore(MEMORY_FILE)

    # MCP protocol: read JSON-RPC requests from stdin
    for line in sys.stdin:
        try:
            request = json.loads(line)
            response = handle_mcp_request(request, memory)
            print(json.dumps(response))
            sys.stdout.flush()
        except Exception as e:
            logger.error(f"Error processing request: {e}", exc_info=True)
            error_response = {
                "jsonrpc": "2.0",
                "error": {"code": -32603, "message": str(e)},
            }
            print(json.dumps(error_response))
            sys.stdout.flush()


if __name__ == "__main__":
    main()
