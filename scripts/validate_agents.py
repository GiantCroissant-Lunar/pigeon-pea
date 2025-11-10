#!/usr/bin/env python3
"""Validate agent manifests against schemas."""
import json
import sys
from pathlib import Path

import yaml
from jsonschema import ValidationError, validate


def validate_agent(agent_path, schema_path, agent_type):
    """Validate a single agent YAML against schema."""
    try:
        with open(agent_path) as f:
            agent = yaml.safe_load(f)

        with open(schema_path) as f:
            schema = json.load(f)

        try:
            validate(instance=agent, schema=schema)
            print(f"✓ {agent_path.name}: Valid {agent_type}")
            return True
        except ValidationError as e:
            print(f"✗ {agent_path.name}: {e.message}")
            return False

    except (OSError, IOError) as e:
        print(f"✗ {agent_path.name}: Error reading file: {e}")
        return False
    except Exception as e:
        print(f"✗ {agent_path.name}: Unexpected error: {e}")
        return False


def main():
    agents_dir = Path(".agent/agents")
    schemas_dir = Path(".agent/schemas")

    if not agents_dir.exists():
        print("Error: .agent/agents directory not found")
        return 1

    if not schemas_dir.exists():
        print("Error: .agent/schemas directory not found")
        return 1

    all_valid = True

    # Validate orchestrator
    orchestrator = agents_dir / "orchestrator.yaml"
    if orchestrator.exists():
        orchestrator_schema = schemas_dir / "orchestrator.schema.json"
        if not orchestrator_schema.exists():
            print(f"Warning: orchestrator schema not found at {orchestrator_schema}")
        else:
            print(f"\nValidating orchestrator...")
            if not validate_agent(orchestrator, orchestrator_schema, "orchestrator"):
                all_valid = False

    # Validate sub-agents
    subagent_schema = schemas_dir / "subagent.schema.json"
    if not subagent_schema.exists():
        print(f"Warning: subagent schema not found at {subagent_schema}")
    else:
        for agent_file in sorted(agents_dir.glob("*.yaml")):
            if agent_file.name != "orchestrator.yaml":
                print(f"\nValidating {agent_file.name}...")
                if not validate_agent(agent_file, subagent_schema, "sub-agent"):
                    all_valid = False

    return 0 if all_valid else 1


if __name__ == "__main__":
    sys.exit(main())
