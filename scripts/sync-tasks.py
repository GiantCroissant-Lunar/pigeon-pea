#!/usr/bin/env python3
"""
RFC-Task synchronization script.

Reads task-master tasks.json and updates RFC implementation status,
generating implementation dashboard and dependency graphs.
"""
import argparse
import json
import sys
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional

import yaml


def load_tasks(tasks_file: Path) -> Dict:
    """Load tasks from task-master tasks.json."""
    if not tasks_file.exists():
        return {"tasks": []}

    with open(tasks_file, encoding="utf-8") as f:
        return json.load(f)


def load_rfc(rfc_path: Path) -> Optional[Dict]:
    """Load RFC front-matter and content."""
    try:
        with open(rfc_path, encoding="utf-8") as f:
            content = f.read()

        if not content.startswith("---"):
            return None

        parts = content.split("---", 2)
        if len(parts) < 3:
            return None

        front_matter = yaml.safe_load(parts[1])
        return {
            "path": rfc_path,
            "front_matter": front_matter,
            "content": parts[2],
            "full_content": content,
        }
    except Exception as e:
        print(f"Warning: Failed to load {rfc_path}: {e}", file=sys.stderr)
        return None


def extract_rfc_references(task: Dict) -> List[str]:
    """Extract RFC references from a task."""
    refs = set()

    # Check task description
    desc = task.get("description", "")
    # Pattern: RFC-YYYY-NNNNN or RFC-NNN
    import re

    matches = re.findall(r"RFC-\d{4}-\d{5}|RFC-\d{3}", desc)
    refs.update(matches)

    # Check task metadata
    metadata = task.get("metadata", {})
    if "rfc" in metadata:
        refs.add(metadata["rfc"])
    if "rfcs" in metadata:
        refs.update(metadata["rfcs"])

    return list(refs)


def calculate_task_completion(task: Dict) -> int:
    """Calculate task completion percentage."""
    if task.get("status") == "completed":
        return 100

    subtasks = task.get("subtasks", [])
    if not subtasks:
        return 0 if task.get("status") == "pending" else 50

    completed = sum(1 for st in subtasks if st.get("completed", False))
    return int((completed / len(subtasks)) * 100)


def update_rfc_implementation(
    rfc: Dict,
    tasks: List[Dict],
) -> Dict:
    """Update RFC implementation status based on tasks."""
    doc_id = rfc["front_matter"].get("doc_id")

    # Find tasks referencing this RFC
    related_tasks = []
    for task in tasks:
        rfc_refs = extract_rfc_references(task)
        if doc_id in rfc_refs:
            related_tasks.append(task)

    if not related_tasks:
        # No tasks, keep existing or set not-started
        existing_impl = rfc["front_matter"].get("implementation", {})
        return existing_impl or {
            "status": "not-started",
            "completion": 0,
            "tasks": [],
            "issues": [],
        }

    # Calculate overall status
    task_ids = [t.get("id") for t in related_tasks if t.get("id")]
    issue_numbers = []

    # Extract GitHub issue numbers
    for task in related_tasks:
        metadata = task.get("metadata", {})
        if "issue" in metadata:
            issue_numbers.append(metadata["issue"])

    # Calculate completion
    completions = [calculate_task_completion(t) for t in related_tasks]
    avg_completion = int(sum(completions) / len(completions)) if completions else 0

    # Determine status
    all_completed = all(t.get("status") == "completed" for t in related_tasks)
    any_blocked = any(t.get("status") == "blocked" for t in related_tasks)
    any_in_progress = any(t.get("status") == "in-progress" for t in related_tasks)

    if all_completed:
        status = "completed"
    elif any_blocked:
        status = "blocked"
    elif any_in_progress:
        status = "in-progress"
    else:
        status = "not-started"

    # Build implementation object
    implementation = {
        "status": status,
        "completion": avg_completion,
        "tasks": task_ids,
        "issues": issue_numbers,
    }

    # Add timestamps if available
    existing_impl = rfc["front_matter"].get("implementation", {})
    if "started" in existing_impl:
        implementation["started"] = existing_impl["started"]
    elif status in ["in-progress", "completed", "blocked"]:
        implementation["started"] = datetime.now().strftime("%Y-%m-%d")

    if status == "completed":
        implementation["completed"] = existing_impl.get(
            "completed", datetime.now().strftime("%Y-%m-%d")
        )

    return implementation


def sync_rfc_tasks(
    docs_dir: Path,
    tasks_file: Path,
    dry_run: bool = True,
) -> Dict:
    """Sync RFC implementation status with tasks."""
    # Load tasks
    tasks_data = load_tasks(tasks_file)
    tasks = tasks_data.get("tasks", [])

    # Find all RFCs
    rfcs_dir = docs_dir / "rfcs"
    if not rfcs_dir.exists():
        print(f"Error: RFCs directory not found: {rfcs_dir}")
        return {"error": "RFCs directory not found"}

    results = {
        "timestamp": datetime.now().isoformat(),
        "mode": "apply" if not dry_run else "dry-run",
        "total_rfcs": 0,
        "updated_rfcs": 0,
        "rfcs": [],
    }

    # Process each RFC
    for rfc_file in rfcs_dir.glob("*.md"):
        if rfc_file.name == "README.md":
            continue

        rfc = load_rfc(rfc_file)
        if not rfc:
            continue

        # Only process RFCs (not other doc types in rfcs/)
        if rfc["front_matter"].get("doc_type") != "rfc":
            continue

        results["total_rfcs"] += 1

        # Update implementation status
        new_impl = update_rfc_implementation(rfc, tasks)
        old_impl = rfc["front_matter"].get("implementation", {})

        # Check if changed
        if new_impl != old_impl:
            results["updated_rfcs"] += 1

            rfc_result = {
                "doc_id": rfc["front_matter"].get("doc_id"),
                "title": rfc["front_matter"].get("title"),
                "old_status": old_impl.get("status", "not-started"),
                "new_status": new_impl["status"],
                "old_completion": old_impl.get("completion", 0),
                "new_completion": new_impl["completion"],
                "tasks": new_impl["tasks"],
            }
            results["rfcs"].append(rfc_result)

            # Apply changes if not dry-run
            if not dry_run:
                rfc["front_matter"]["implementation"] = new_impl
                rfc["front_matter"]["updated"] = datetime.now().strftime("%Y-%m-%d")

                # Write back to file
                new_content = "---\n"
                new_content += yaml.dump(
                    rfc["front_matter"],
                    default_flow_style=False,
                    allow_unicode=True,
                )
                new_content += "---\n"
                new_content += rfc["content"]

                with open(rfc["path"], "w", encoding="utf-8") as f:
                    f.write(new_content)

    return results


def generate_rfc_task_map(
    docs_dir: Path,
    tasks_file: Path,
    output_file: Path,
) -> None:
    """Generate RFC-task mapping registry."""
    tasks_data = load_tasks(tasks_file)
    tasks = tasks_data.get("tasks", [])

    # Build mapping
    rfc_to_tasks = {}
    task_to_rfcs = {}

    for task in tasks:
        task_id = task.get("id")
        rfc_refs = extract_rfc_references(task)

        for rfc_id in rfc_refs:
            if rfc_id not in rfc_to_tasks:
                rfc_to_tasks[rfc_id] = []
            rfc_to_tasks[rfc_id].append(task_id)

            if task_id not in task_to_rfcs:
                task_to_rfcs[task_id] = []
            task_to_rfcs[task_id].append(rfc_id)

    # Generate map
    mapping = {
        "generated_at": datetime.now().isoformat(),
        "total_rfcs": len(rfc_to_tasks),
        "total_tasks": len(task_to_rfcs),
        "rfc_to_tasks": rfc_to_tasks,
        "task_to_rfcs": task_to_rfcs,
    }

    # Write to file
    output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(output_file, "w", encoding="utf-8") as f:
        json.dump(mapping, f, indent=2, ensure_ascii=False)

    print(f"RFC-task map generated: {output_file}")
    print(f"  {len(rfc_to_tasks)} RFCs mapped to tasks")
    print(f"  {len(task_to_rfcs)} tasks mapped to RFCs")


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Sync RFC implementation status with task-master tasks"
    )
    parser.add_argument(
        "--docs-dir",
        type=Path,
        help="Documentation directory (default: docs/)",
    )
    parser.add_argument(
        "--tasks-file",
        type=Path,
        help="Task-master tasks.json file (default: .taskmaster/tasks.json)",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Apply updates to RFC files (default: dry-run)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Output sync report (default: stdout)",
    )
    parser.add_argument(
        "--generate-map",
        action="store_true",
        help="Generate RFC-task mapping registry",
    )
    args = parser.parse_args()

    # Resolve paths
    repo_root = Path(__file__).parent.parent
    docs_dir = args.docs_dir or repo_root / "docs"
    tasks_file = args.tasks_file or repo_root / ".taskmaster" / "tasks.json"

    if not docs_dir.exists():
        print(f"Error: Documentation directory not found: {docs_dir}")
        return 1

    print(f"Syncing RFCs with tasks from {tasks_file}...")
    print(f"Mode: {'APPLY' if args.apply else 'DRY-RUN'}")

    # Sync RFC implementation status
    results = sync_rfc_tasks(docs_dir, tasks_file, dry_run=not args.apply)

    # Output results
    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            json.dump(results, f, indent=2, ensure_ascii=False)
        print(f"\nSync report written to: {args.output}")
    else:
        print("\n=== SYNC RESULTS ===")
        print(json.dumps(results, indent=2, ensure_ascii=False))

    # Generate RFC-task map if requested
    if args.generate_map:
        map_file = docs_dir / "index" / "rfc-task-map.json"
        generate_rfc_task_map(docs_dir, tasks_file, map_file)

    # Summary
    print("\n=== SUMMARY ===")
    print(f"Total RFCs: {results['total_rfcs']}")
    print(f"Updated RFCs: {results['updated_rfcs']}")

    if results.get("rfcs"):
        print("\n=== UPDATED RFCs ===")
        for rfc in results["rfcs"]:
            print(f"  {rfc['doc_id']}: {rfc['old_status']} → {rfc['new_status']}")
            print(
                f"    Completion: {rfc['old_completion']}% → {rfc['new_completion']}%"
            )
            print(f"    Tasks: {', '.join(rfc['tasks']) if rfc['tasks'] else 'none'}")

    if args.apply:
        print("\nSync complete!")
    else:
        print("\nDRY-RUN: No files modified. Use --apply to apply changes.")

    return 0


if __name__ == "__main__":
    sys.exit(main())
