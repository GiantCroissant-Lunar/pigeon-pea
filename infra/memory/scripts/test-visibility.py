#!/usr/bin/env python3
"""
Quick memory system visibility test
Tests if memory save/retrieve actually works and is observable
"""
import json
import sys
from datetime import datetime
from pathlib import Path

# Simple file-based "memory" to prove the concept
# (parent.parent because we're in scripts/ subdirectory)
MEMORY_FILE = Path(__file__).parent.parent / "data" / "memory-test.jsonl"

def save_memory(key: str, content: str, metadata: dict = None):
    """Save a memory and LOG IT"""
    MEMORY_FILE.parent.mkdir(exist_ok=True)

    entry = {
        "timestamp": datetime.now().isoformat(),
        "key": key,
        "content": content,
        "metadata": metadata or {}
    }

    with open(MEMORY_FILE, "a") as f:
        f.write(json.dumps(entry) + "\n")

    print(f"[SAVED] {key}")
    print(f"  Content: {content[:100]}...")
    print(f"  Location: {MEMORY_FILE}")
    return entry

def retrieve_memory(key: str) -> list:
    """Retrieve memories and LOG IT"""
    if not MEMORY_FILE.exists():
        print(f"[NOT FOUND] NO MEMORY FILE: {MEMORY_FILE}")
        return []

    matches = []
    with open(MEMORY_FILE, "r") as f:
        for line in f:
            entry = json.loads(line)
            if key.lower() in entry["key"].lower() or key.lower() in entry["content"].lower():
                matches.append(entry)

    print(f"[RETRIEVED] {len(matches)} matches for '{key}'")
    for m in matches:
        print(f"  - {m['timestamp']}: {m['content'][:80]}...")

    return matches

def test_memory_cycle():
    """Run a full save -> retrieve cycle"""
    print("\n=== MEMORY VISIBILITY TEST ===\n")

    # Test 1: Save
    print("TEST 1: Save memory")
    save_memory(
        key="architecture.domain-separation",
        content="Map and Dungeon are separate domains. Map.Core handles world generation, Dungeon.Core handles local dungeons.",
        metadata={"project": "pigeon-pea", "type": "architecture-decision"}
    )

    save_memory(
        key="tech.fov-library",
        content="We use GoRogue for field-of-view calculations in Dungeon.Core",
        metadata={"project": "pigeon-pea", "type": "tech-choice"}
    )

    # Test 2: Retrieve exact
    print("\nTEST 2: Retrieve by key")
    retrieve_memory("fov")

    # Test 3: Retrieve semantic
    print("\nTEST 3: Retrieve by concept")
    retrieve_memory("domain")

    # Test 4: Show all
    print("\nTEST 4: Show all memories")
    if MEMORY_FILE.exists():
        with open(MEMORY_FILE, "r") as f:
            entries = [json.loads(line) for line in f]
            print(f"Total memories: {len(entries)}")
            for e in entries[-5:]:  # Last 5
                print(f"  - {e['key']}: {e['content'][:60]}...")

    print("\n=== TEST COMPLETE ===")
    print(f"Memory file: {MEMORY_FILE}")
    print("Next: Check if an AGENT can use this same pattern")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        if sys.argv[1] == "save":
            save_memory(sys.argv[2], sys.argv[3])
        elif sys.argv[1] == "get":
            retrieve_memory(sys.argv[2])
    else:
        test_memory_cycle()
