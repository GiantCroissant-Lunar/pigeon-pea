#!/usr/bin/env python3
"""Validate skill manifests against schema and size limits."""
import json
import sys
from pathlib import Path

import yaml
from jsonschema import ValidationError, validate


def extract_frontmatter(skill_md_path):
    """Extract YAML front-matter from SKILL.md"""
    with open(skill_md_path) as f:
        content = f.read()

    if not content.startswith("---"):
        raise ValueError(f"Missing front-matter in {skill_md_path}")

    parts = content.split("---", 2)
    if len(parts) < 3:
        raise ValueError(f"Invalid front-matter format in {skill_md_path}")

    return yaml.safe_load(parts[1])


def validate_skill(skill_path, schema_path):
    """Validate a single skill against schema and size limits."""
    skill_md = skill_path / "SKILL.md"

    # Extract and validate front-matter
    front_matter = extract_frontmatter(skill_md)

    with open(schema_path) as f:
        schema = json.load(f)

    try:
        validate(instance=front_matter, schema=schema)
        print(f"✓ {skill_md}: Schema valid")
    except ValidationError as e:
        print(f"✗ {skill_md}: {e.message}")
        return False

    # Check size limits
    with open(skill_md) as f:
        entry_lines = len(f.readlines())

    if entry_lines > 220:
        print(f"✗ {skill_md}: Entry too large ({entry_lines} lines, max 220)")
        return False

    print(f"✓ {skill_md}: Size OK ({entry_lines} lines)")

    # Check references
    ref_dir = skill_path / "references"
    if ref_dir.exists():
        refs = list(ref_dir.glob("*.md"))
        if refs:
            # Check first reference for cold-start budget
            with open(refs[0]) as f:
                ref_lines = len(f.readlines())

            if ref_lines > 320:
                print(f"✗ {refs[0]}: Reference too large ({ref_lines} lines, max 320)")
                return False

            total = entry_lines + ref_lines
            if total > 550:
                print(f"✗ Cold-start budget exceeded: {total} lines (max 550)")
                return False

            print(f"✓ Cold-start budget OK: {total} lines")

            # Check all other references
            for ref in refs[1:]:
                with open(ref) as f:
                    ref_lines = len(f.readlines())

                if ref_lines > 320:
                    print(f"✗ {ref}: Reference too large ({ref_lines} lines, max 320)")
                    return False

                print(f"✓ {ref}: Size OK ({ref_lines} lines)")

    return True


def main():
    skills_dir = Path(".agent/skills")
    schema_path = Path(".agent/schemas/skill.schema.json")

    if not skills_dir.exists():
        print("Error: .agent/skills directory not found")
        return 1

    if not schema_path.exists():
        print("Error: skill schema not found")
        return 1

    all_valid = True
    for skill_dir in skills_dir.iterdir():
        if skill_dir.is_dir() and (skill_dir / "SKILL.md").exists():
            print(f"\nValidating {skill_dir.name}...")
            if not validate_skill(skill_dir, schema_path):
                all_valid = False

    return 0 if all_valid else 1


if __name__ == "__main__":
    sys.exit(main())
