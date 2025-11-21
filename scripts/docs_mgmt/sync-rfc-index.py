#!/usr/bin/env python3
"""
RFC indexing synchronization and validation tool.

Ensures RFC filenames match their doc_ids and identifies gaps in numbering.
Also provides PRD-RFC relationship management.
"""
import argparse
import re
import sys
from pathlib import Path
from typing import Dict, Optional

import yaml


def load_rfc_metadata(rfc_file: Path) -> Optional[Dict]:
    """Load RFC front-matter and extract metadata."""
    try:
        with open(rfc_file, encoding="utf-8") as f:
            content = f.read()

        if not content.startswith("---"):
            return None

        parts = content.split("---", 2)
        if len(parts) < 3:
            return None

        front_matter = yaml.safe_load(parts[1])

        # Extract filename number (e.g., "015" from "015-fantasy-calendar.md")
        filename_match = re.match(r"(\d{3})-", rfc_file.name)
        filename_number = int(filename_match.group(1)) if filename_match else None

        # Extract doc_id number (e.g., "00015" from "RFC-00015")
        doc_id = front_matter.get("doc_id", "")
        doc_id_match = re.match(r"RFC-(\d{5})$", doc_id)
        doc_id_number = int(doc_id_match.group(1)) if doc_id_match else None

        return {
            "path": rfc_file,
            "filename": rfc_file.name,
            "filename_number": filename_number,
            "doc_id": doc_id,
            "doc_id_number": doc_id_number,
            "title": front_matter.get("title", "Untitled"),
            "front_matter": front_matter,
        }
    except Exception as e:
        print(f"Warning: Failed to load {rfc_file}: {e}", file=sys.stderr)
        return None


def analyze_rfc_indexing(rfcs_dir: Path) -> Dict:
    """Analyze RFC indexing and identify issues."""
    rfcs = []

    for rfc_file in sorted(rfcs_dir.glob("*.md")):
        if rfc_file.name == "README.md":
            continue

        metadata = load_rfc_metadata(rfc_file)
        if metadata:
            rfcs.append(metadata)

    # Identify issues
    issues = {
        "mismatches": [],
        "missing_doc_id": [],
        "missing_filename_number": [],
        "gaps": [],
        "duplicates": [],
    }

    # Check for mismatches
    for rfc in rfcs:
        if rfc["filename_number"] and rfc["doc_id_number"]:
            if rfc["filename_number"] != rfc["doc_id_number"]:
                issues["mismatches"].append(
                    {
                        "file": rfc["filename"],
                        "filename_number": rfc["filename_number"],
                        "doc_id": rfc["doc_id"],
                        "doc_id_number": rfc["doc_id_number"],
                    }
                )
        elif not rfc["doc_id_number"]:
            issues["missing_doc_id"].append(rfc["filename"])
        elif not rfc["filename_number"]:
            issues["missing_filename_number"].append(rfc["filename"])

    # Check for gaps in numbering
    if rfcs:
        filename_numbers = sorted(
            [r["filename_number"] for r in rfcs if r["filename_number"]]
        )
        doc_id_numbers = sorted(
            [r["doc_id_number"] for r in rfcs if r["doc_id_number"]]
        )

        # Check filename gaps
        for i in range(len(filename_numbers) - 1):
            if filename_numbers[i + 1] - filename_numbers[i] > 1:
                issues["gaps"].append(
                    {
                        "type": "filename",
                        "from": filename_numbers[i],
                        "to": filename_numbers[i + 1],
                        "missing": list(
                            range(filename_numbers[i] + 1, filename_numbers[i + 1])
                        ),
                    }
                )

        # Check doc_id gaps
        for i in range(len(doc_id_numbers) - 1):
            if doc_id_numbers[i + 1] - doc_id_numbers[i] > 1:
                issues["gaps"].append(
                    {
                        "type": "doc_id",
                        "from": doc_id_numbers[i],
                        "to": doc_id_numbers[i + 1],
                        "missing": list(
                            range(doc_id_numbers[i] + 1, doc_id_numbers[i + 1])
                        ),
                    }
                )

    # Check for duplicates
    filename_counts = {}
    doc_id_counts = {}

    for rfc in rfcs:
        if rfc["filename_number"]:
            filename_counts[rfc["filename_number"]] = (
                filename_counts.get(rfc["filename_number"], 0) + 1
            )
        if rfc["doc_id_number"]:
            doc_id_counts[rfc["doc_id_number"]] = (
                doc_id_counts.get(rfc["doc_id_number"], 0) + 1
            )

    for num, count in filename_counts.items():
        if count > 1:
            issues["duplicates"].append(
                {"type": "filename", "number": num, "count": count}
            )

    for num, count in doc_id_counts.items():
        if count > 1:
            issues["duplicates"].append(
                {"type": "doc_id", "number": num, "count": count}
            )

    return {
        "rfcs": rfcs,
        "total": len(rfcs),
        "issues": issues,
    }


def generate_sync_report(analysis: Dict) -> str:
    """Generate markdown report of RFC indexing issues."""
    report = []

    report.append("# RFC Indexing Synchronization Report\n")
    report.append(f"*Total RFCs: {analysis['total']}*\n")

    issues = analysis["issues"]
    total_issues = (
        len(issues["mismatches"])
        + len(issues["missing_doc_id"])
        + len(issues["missing_filename_number"])
        + len(issues["duplicates"])
    )

    if total_issues == 0:
        report.append("\n✅ **All RFCs are properly indexed!**\n")
        return "".join(report)

    report.append(f"\n⚠️ **Found {total_issues} issues**\n")

    # Mismatches
    if issues["mismatches"]:
        report.append(
            f"\n## ❌ Filename/Doc ID Mismatches ({len(issues['mismatches'])})\n"
        )
        report.append(
            "| Filename | Filename # | Doc ID | Doc ID # | Recommended Action |\n"
        )
        report.append(
            "|----------|------------|--------|----------|--------------------|\n"
        )

        for mismatch in issues["mismatches"]:
            filename = mismatch["file"]
            fn_num = mismatch["filename_number"]
            doc_id = mismatch["doc_id"]
            did_num = mismatch["doc_id_number"]

            # Recommend using doc_id as source of truth
            new_filename = f"{did_num:03d}-" + filename.split("-", 1)[1]

            report.append(
                f"| `{filename}` | {fn_num} | `{doc_id}` | {did_num} | "
                f"Rename to `{new_filename}` |\n"
            )

    # Missing doc_id
    if issues["missing_doc_id"]:
        report.append(f"\n## ❌ Missing Doc ID ({len(issues['missing_doc_id'])})\n")
        for filename in issues["missing_doc_id"]:
            report.append(f"- `{filename}` - Add proper doc_id to front-matter\n")

    # Missing filename number
    if issues["missing_filename_number"]:
        report.append(
            "\n## ❌ Missing Filename Number "
            f"({len(issues['missing_filename_number'])})\n"
        )
        for filename in issues["missing_filename_number"]:
            report.append(f"- `{filename}` - Rename with proper number prefix\n")

    # Duplicates
    if issues["duplicates"]:
        report.append(f"\n## ❌ Duplicate Numbers ({len(issues['duplicates'])})\n")
        for dup in issues["duplicates"]:
            report.append(
                f"- {dup['type'].title()} #{dup['number']} appears "
                f"{dup['count']} times\n"
            )

    # Gaps (informational, not necessarily errors)
    if issues["gaps"]:
        report.append(f"\n## ℹ️ Numbering Gaps ({len(issues['gaps'])})\n")
        for gap in issues["gaps"]:
            missing_str = ", ".join(str(n) for n in gap["missing"])
            report.append(
                f"- {gap['type'].title()}: Gap from {gap['from']} to {gap['to']} "
                f"(missing: {missing_str})\n"
            )

    return "".join(report)


def generate_prd_rfc_schema() -> str:
    """Generate documentation for PRD-RFC relationship schema."""
    schema = []

    schema.append("# PRD-RFC Relationship Schema\n")
    schema.append("\n## Overview\n")
    schema.append(
        "Product Requirement Documents (PRDs) and RFCs serve different purposes:\n\n"
    )
    schema.append("- **PRD**: What to build and why (product perspective)\n")
    schema.append("- **RFC**: How to build it (technical perspective)\n\n")
    schema.append(
        "**Recommended Approach**: PRDs reference RFCs (one-to-many relationship)\n\n"
    )

    schema.append("\n## PRD Front-Matter Extension\n")
    schema.append("```yaml\n")
    schema.append("---\n")
    schema.append("doc_id: 'PRD-2025-00001'\n")
    schema.append("title: 'Fantasy Calendar System'\n")
    schema.append("doc_type: 'prd'\n")
    schema.append("status: 'active'\n")
    schema.append("canonical: true\n")
    schema.append("created: '2025-11-20'\n")
    schema.append("tags: ['product', 'calendar', 'time-system']\n")
    schema.append("summary: 'Product requirements for fantasy calendar system'\n")
    schema.append("implementation:\n")
    schema.append("  rfcs: ['RFC-2025-00015']  # RFCs that implement this PRD\n")
    schema.append("  status: 'in-progress'\n")
    schema.append("---\n")
    schema.append("```\n")

    schema.append("\n## RFC Front-Matter Extension\n")
    schema.append("```yaml\n")
    schema.append("---\n")
    schema.append("doc_id: 'RFC-2025-00015'\n")
    schema.append("title: 'Fantasy Calendar to Real-World Time Transformation'\n")
    schema.append("doc_type: 'rfc'\n")
    schema.append("# ... other fields ...\n")
    schema.append("implements: 'PRD-2025-00001'  # PRD this RFC implements\n")
    schema.append("dependencies:\n")
    schema.append("  rfcs: ['RFC-2025-00013']  # Other RFCs this depends on\n")
    schema.append("  prds: []  # Additional PRDs (if implementing multiple)\n")
    schema.append("---\n")
    schema.append("```\n")

    schema.append("\n## Relationship Types\n")
    schema.append("\n### 1. PRD → RFC (Implementation)\n")
    schema.append("- **Direction**: PRD references RFCs\n")
    schema.append("- **Cardinality**: One PRD → Many RFCs\n")
    schema.append("- **Field**: `implementation.rfcs` in PRD\n")
    schema.append(
        "- **Use Case**: Track which RFCs implement a product requirement\n\n"
    )

    schema.append("### 2. RFC → PRD (Implements)\n")
    schema.append("- **Direction**: RFC references PRD\n")
    schema.append("- **Cardinality**: One RFC → One PRD (typically)\n")
    schema.append("- **Field**: `implements` in RFC\n")
    schema.append("- **Use Case**: Show product context for technical design\n\n")

    schema.append("### 3. RFC → RFC (Dependencies)\n")
    schema.append("- **Direction**: RFC references other RFCs\n")
    schema.append("- **Cardinality**: Many-to-many\n")
    schema.append("- **Field**: `dependencies.rfcs` in RFC\n")
    schema.append("- **Use Case**: Technical dependencies between designs\n\n")

    schema.append("\n## Visualization\n")
    schema.append("```mermaid\n")
    schema.append("graph TD\n")
    schema.append(
        "    PRD1[PRD: Fantasy Calendar] --> RFC15[RFC-015: Time Transform]\n"
    )
    schema.append("    PRD1 --> RFC16[RFC-016: Calendar UI]\n")
    schema.append("    RFC15 --> RFC13[RFC-013: Plugin Architecture]\n")
    schema.append("    RFC16 --> RFC15\n")
    schema.append("    \n")
    schema.append("    style PRD1 fill:#E1BEE7,stroke:#7B1FA2\n")
    schema.append("    style RFC15 fill:#BBDEFB,stroke:#1976D2\n")
    schema.append("    style RFC16 fill:#BBDEFB,stroke:#1976D2\n")
    schema.append("    style RFC13 fill:#BBDEFB,stroke:#1976D2\n")
    schema.append("```\n")

    schema.append("\n## Benefits\n")
    schema.append(
        "1. **Traceability**: Track from product requirement "
        "to technical implementation\n"
    )
    schema.append(
        "2. **Impact Analysis**: See which RFCs are affected by PRD changes\n"
    )
    schema.append(
        "3. **Completeness**: Verify all PRD requirements have RFC coverage\n"
    )
    schema.append(
        "4. **Context**: Understand product rationale behind technical decisions\n"
    )

    return "".join(schema)


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="RFC indexing synchronization and PRD-RFC relationship tool"
    )
    parser.add_argument(
        "--rfcs-dir",
        type=Path,
        help="RFCs directory (default: docs/rfcs/)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Output report file (default: stdout)",
    )
    parser.add_argument(
        "--generate-prd-schema",
        action="store_true",
        help="Generate PRD-RFC relationship schema documentation",
    )
    args = parser.parse_args()

    # Resolve paths
    repo_root = Path(__file__).resolve().parents[2]
    rfcs_dir = args.rfcs_dir or repo_root / "docs" / "rfcs"

    if args.generate_prd_schema:
        schema = generate_prd_rfc_schema()
        if args.output:
            with open(args.output, "w", encoding="utf-8") as f:
                f.write(schema)
            print(f"PRD-RFC schema generated: {args.output}")
        else:
            print(schema)
        return 0

    if not rfcs_dir.exists():
        print(f"Error: RFCs directory not found: {rfcs_dir}")
        return 1

    print(f"Analyzing RFC indexing in {rfcs_dir}...")

    # Analyze indexing
    analysis = analyze_rfc_indexing(rfcs_dir)

    # Generate report
    report = generate_sync_report(analysis)

    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            f.write(report)
        print(f"Report generated: {args.output}")
    else:
        print("\n" + report)

    # Summary
    issues = analysis["issues"]
    total_issues = (
        len(issues["mismatches"])
        + len(issues["missing_doc_id"])
        + len(issues["missing_filename_number"])
        + len(issues["duplicates"])
    )

    print("\n=== SUMMARY ===")
    print(f"Total RFCs: {analysis['total']}")
    print(f"Issues found: {total_issues}")
    print(f"  - Mismatches: {len(issues['mismatches'])}")
    print(f"  - Missing doc_id: {len(issues['missing_doc_id'])}")
    print(f"  - Missing filename number: {len(issues['missing_filename_number'])}")
    print(f"  - Duplicates: {len(issues['duplicates'])}")
    print(f"  - Gaps (informational): {len(issues['gaps'])}")

    return 1 if total_issues > 0 else 0


if __name__ == "__main__":
    sys.exit(main())
