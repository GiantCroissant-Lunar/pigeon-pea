#!/usr/bin/env python3
"""Validate agent manifests against schemas."""
import json
import sys
from pathlib import Path

import yaml
from jsonschema import ValidationError, validate

def validate_agent(agent_path, schema, agent_type):
    """Validate a single agent YAML against a loaded schema (UTF-8)."""
    try:
        with open(agent_path, encoding="utf-8") as f:
            agent = yaml.safe_load(f)

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
    except yaml.YAMLError as e:
        print(f"✗ {agent_path.name}: Invalid YAML: {e}")
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
    # Get files to validate from arguments, or all files if none provided
    files_to_validate = []
    if len(sys.argv) > 1:
        # Validate only specified files (from pre-commit)
        files_to_validate = [Path(arg) for arg in sys.argv[1:]]
    else:
        # Validate all agent files
        files_to_validate = sorted(agents_dir.glob("*.yaml"))

    all_valid = True

    # Pre-load schemas (UTF-8)
    orchestrator_schema = None
    subagent_schema = None

    orchestrator_schema_path = schemas_dir / "orchestrator.schema.json"
    subagent_schema_path = schemas_dir / "subagent.schema.json"

    if orchestrator_schema_path.exists():
        with open(orchestrator_schema_path, encoding="utf-8") as f:
            orchestrator_schema = json.load(f)
    else:
        print(f"Warning: orchestrator schema not found at {orchestrator_schema_path}")

    if subagent_schema_path.exists():
        with open(subagent_schema_path, encoding="utf-8") as f:
            subagent_schema = json.load(f)
    else:
        print(f"Warning: subagent schema not found at {subagent_schema_path}")

    # Validate files
    for agent_file in files_to_validate:
        agent_path = Path(agent_file)
        if not agent_path.exists():
            print(f"✗ {agent_file}: File not found")
            all_valid = False
            continue

        if agent_path.name == "orchestrator.yaml":
            if orchestrator_schema is None:
                continue
            print("\nValidating orchestrator...")
            if not validate_agent(agent_path, orchestrator_schema, "orchestrator"):
                all_valid = False
        else:
            if subagent_schema is None:
                continue
            print(f"\nValidating {agent_path.name}...")
            if not validate_agent(agent_path, subagent_schema, "sub-agent"):
                all_valid = False

    if all_valid:
        print("\n✓ All agent manifests are valid")
    else:
        print("\n✗ Some agent manifests have validation errors")
    return 0 if all_valid else 1


if __name__ == "__main__":
    sys.exit(main())
