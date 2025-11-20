#!/usr/bin/env python3
"""
Generate implementation dashboard from RFC implementation status.

Creates a comprehensive dashboard showing RFC implementation progress,
blocked items, and dependency relationships.
"""
import argparse
import sys
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional

import yaml


def load_rfc(rfc_path: Path) -> Optional[Dict]:
    """Load RFC front-matter."""
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
        }
    except Exception:
        return None


def collect_rfcs(docs_dir: Path) -> List[Dict]:
    """Collect all RFCs with front-matter."""
    rfcs = []
    rfcs_dir = docs_dir / "rfcs"

    if not rfcs_dir.exists():
        return rfcs

    for rfc_file in rfcs_dir.glob("*.md"):
        if rfc_file.name == "README.md":
            continue

        rfc = load_rfc(rfc_file)
        if rfc and rfc["front_matter"].get("doc_type") == "rfc":
            rfcs.append(rfc)

    return rfcs


def generate_dashboard(rfcs: List[Dict]) -> str:
    """Generate markdown dashboard."""
    dashboard = []

    # Header
    dashboard.append("# RFC Implementation Dashboard\n")
    dashboard.append(f"*Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}*\n")

    # Summary statistics
    total = len(rfcs)
    with_impl = sum(1 for r in rfcs if "implementation" in r["front_matter"])

    statuses = {}
    for rfc in rfcs:
        impl = rfc["front_matter"].get("implementation", {})
        status = impl.get("status", "not-started")
        statuses[status] = statuses.get(status, 0) + 1

    dashboard.append("\n## Summary\n")
    dashboard.append(f"- **Total RFCs**: {total}\n")
    dashboard.append(f"- **With Implementation Tracking**: {with_impl}\n")
    dashboard.append(f"- **Not Started**: {statuses.get('not-started', 0)}\n")
    dashboard.append(f"- **In Progress**: {statuses.get('in-progress', 0)}\n")
    dashboard.append(f"- **Completed**: {statuses.get('completed', 0)}\n")
    dashboard.append(f"- **Blocked**: {statuses.get('blocked', 0)}\n")
    dashboard.append(f"- **Deferred**: {statuses.get('deferred', 0)}\n")

    # Active RFCs (in-progress)
    active = [
        r
        for r in rfcs
        if r["front_matter"].get("implementation", {}).get("status") == "in-progress"
    ]
    if active:
        dashboard.append("\n## 🚀 Active RFCs\n")
        dashboard.append("| RFC | Title | Completion | Tasks | Started |\n")
        dashboard.append("|-----|-------|------------|-------|----------|\n")

        for rfc in sorted(active, key=lambda r: r["front_matter"].get("doc_id", "")):
            fm = rfc["front_matter"]
            impl = fm.get("implementation", {})
            doc_id = fm.get("doc_id", "N/A")
            title = fm.get("title", "Untitled")
            completion = impl.get("completion", 0)
            tasks = impl.get("tasks", [])
            started = impl.get("started", "N/A")

            # Create progress bar
            progress_bar = "█" * (completion // 10) + "░" * (10 - completion // 10)

            dashboard.append(
                f"| {doc_id} | {title} | {progress_bar} {completion}% | "
                f"{len(tasks)} | {started} |\n"
            )

    # Blocked RFCs
    blocked = [
        r
        for r in rfcs
        if r["front_matter"].get("implementation", {}).get("status") == "blocked"
    ]
    if blocked:
        dashboard.append("\n## ⚠️ Blocked RFCs\n")
        dashboard.append("| RFC | Title | Tasks | Dependencies |\n")
        dashboard.append("|-----|-------|-------|---------------|\n")

        for rfc in sorted(blocked, key=lambda r: r["front_matter"].get("doc_id", "")):
            fm = rfc["front_matter"]
            impl = fm.get("implementation", {})
            deps = fm.get("dependencies", {})
            doc_id = fm.get("doc_id", "N/A")
            title = fm.get("title", "Untitled")
            tasks = impl.get("tasks", [])
            dep_rfcs = deps.get("rfcs", [])

            dashboard.append(
                f"| {doc_id} | {title} | {len(tasks)} | "
                f"{', '.join(dep_rfcs) if dep_rfcs else 'None'} |\n"
            )

    # Recently completed
    completed = [
        r
        for r in rfcs
        if r["front_matter"].get("implementation", {}).get("status") == "completed"
    ]
    recent_completed = sorted(
        completed,
        key=lambda r: r["front_matter"].get("implementation", {}).get("completed", ""),
        reverse=True,
    )[:5]

    if recent_completed:
        dashboard.append("\n## ✅ Recently Completed\n")
        dashboard.append("| RFC | Title | Completed | Tasks |\n")
        dashboard.append("|-----|-------|-----------|-------|\n")

        for rfc in recent_completed:
            fm = rfc["front_matter"]
            impl = fm.get("implementation", {})
            doc_id = fm.get("doc_id", "N/A")
            title = fm.get("title", "Untitled")
            completed_date = impl.get("completed", "N/A")
            tasks = impl.get("tasks", [])

            dashboard.append(
                f"| {doc_id} | {title} | {completed_date} | {len(tasks)} |\n"
            )

    # Not started
    not_started = [
        r
        for r in rfcs
        if r["front_matter"].get("implementation", {}).get("status", "not-started")
        == "not-started"
    ]
    if not_started:
        dashboard.append(f"\n## 📋 Not Started ({len(not_started)} RFCs)\n")
        dashboard.append("| RFC | Title | Status |\n")
        dashboard.append("|-----|-------|--------|\n")

        for rfc in sorted(
            not_started, key=lambda r: r["front_matter"].get("doc_id", "")
        )[:10]:
            fm = rfc["front_matter"]
            doc_id = fm.get("doc_id", "N/A")
            title = fm.get("title", "Untitled")
            status = fm.get("status", "draft")

            dashboard.append(f"| {doc_id} | {title} | {status} |\n")

        if len(not_started) > 10:
            dashboard.append(f"\n*...and {len(not_started) - 10} more*\n")

    # Dependency graph (Mermaid)
    rfcs_with_deps = [
        r for r in rfcs if r["front_matter"].get("dependencies", {}).get("rfcs")
    ]
    if rfcs_with_deps:
        dashboard.append("\n## 🔗 Dependency Graph\n")
        dashboard.append("```mermaid\n")
        dashboard.append("graph TD\n")

        for rfc in rfcs_with_deps:
            fm = rfc["front_matter"]
            doc_id = fm.get("doc_id", "N/A")
            deps = fm.get("dependencies", {})
            dep_rfcs = deps.get("rfcs", [])

            for dep in dep_rfcs:
                dashboard.append(f"    {dep} --> {doc_id}\n")

        dashboard.append("```\n")

    # Implementation timeline
    rfcs_with_dates = [
        r for r in rfcs if r["front_matter"].get("implementation", {}).get("started")
    ]
    if rfcs_with_dates:
        dashboard.append("\n## 📅 Implementation Timeline\n")
        dashboard.append("```mermaid\n")
        dashboard.append("gantt\n")
        dashboard.append("    title RFC Implementation Timeline\n")
        dashboard.append("    dateFormat YYYY-MM-DD\n")

        for rfc in sorted(
            rfcs_with_dates,
            key=lambda r: r["front_matter"]
            .get("implementation", {})
            .get("started", ""),
        ):
            fm = rfc["front_matter"]
            impl = fm.get("implementation", {})
            doc_id = fm.get("doc_id", "N/A")
            title = fm.get("title", "Untitled")[:30]  # Truncate long titles
            started = impl.get("started")
            completed = impl.get("completed")
            status = impl.get("status")

            if completed:
                dashboard.append(f"    {title} :{doc_id}, {started}, {completed}\n")
            elif status == "in-progress":
                # Estimate end date as 30 days from start
                dashboard.append(f"    active, {title} :{doc_id}, {started}, 30d\n")

        dashboard.append("```\n")

    return "".join(dashboard)


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Generate RFC implementation dashboard"
    )
    parser.add_argument(
        "--docs-dir",
        type=Path,
        help="Documentation directory (default: docs/)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Output file (default: docs/DASHBOARD.md)",
    )
    args = parser.parse_args()

    # Resolve paths
    repo_root = Path(__file__).parent.parent
    docs_dir = args.docs_dir or repo_root / "docs"
    output_file = args.output or docs_dir / "DASHBOARD.md"

    if not docs_dir.exists():
        print(f"Error: Documentation directory not found: {docs_dir}")
        return 1

    print(f"Generating dashboard from {docs_dir}...")

    # Collect RFCs
    rfcs = collect_rfcs(docs_dir)
    print(f"Found {len(rfcs)} RFCs")

    # Generate dashboard
    dashboard = generate_dashboard(rfcs)

    # Write to file
    with open(output_file, "w", encoding="utf-8") as f:
        f.write(dashboard)

    print(f"\nDashboard generated: {output_file}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
