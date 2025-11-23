#!/usr/bin/env python3
"""
Generate comprehensive documentation index with hierarchical TOC.

Creates INDEX.md with documents grouped by type and status, recently updated
sections, and most referenced documents.
"""
import argparse
import sys
from collections import defaultdict
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional

import yaml


def load_document(file_path: Path, docs_dir: Path) -> Optional[Dict]:
    """Load document front-matter."""
    try:
        with open(file_path, encoding="utf-8") as f:
            content = f.read()

        if not content.startswith("---"):
            return None

        parts = content.split("---", 2)
        if len(parts) < 3:
            return None

        front_matter = yaml.safe_load(parts[1])

        return {
            "path": file_path,
            "rel_path": file_path.relative_to(docs_dir),
            "front_matter": front_matter,
            "content": parts[2],
        }
    except Exception:
        return None


def collect_documents(docs_dir: Path) -> List[Dict]:
    """Collect all documents with front-matter."""
    documents = []

    for md_file in docs_dir.rglob("*.md"):
        # Skip excluded patterns
        rel_path = md_file.relative_to(docs_dir)
        if any(
            str(rel_path).startswith(pattern)
            for pattern in ["index/", "archive/", "_inbox/"]
        ):
            continue

        # Skip README files
        if md_file.name == "README.md":
            continue

        doc = load_document(md_file, docs_dir)
        if doc:
            documents.append(doc)

    return documents


def count_references(documents: List[Dict]) -> Dict[str, int]:
    """Count how many times each doc_id is referenced."""
    ref_counts = defaultdict(int)

    for doc in documents:
        fm = doc["front_matter"]

        # Count related references
        for ref in fm.get("related", []):
            ref_counts[ref] += 1

        # Count supersedes references
        for ref in fm.get("supersedes", []):
            ref_counts[ref] += 1

        # Count dependency references
        deps = fm.get("dependencies") or {}
        for ref in deps.get("rfcs", []):
            ref_counts[ref] += 1

    return dict(ref_counts)


def generate_index(documents: List[Dict], docs_dir: Path) -> str:
    """Generate comprehensive index markdown."""
    index = []

    # Header
    index.append("# Documentation Index\n")
    index.append(f"*Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}*\n")
    index.append(f"*Total Documents: {len(documents)}*\n")

    # Quick navigation
    index.append("\n## Quick Navigation\n")
    index.append("- [By Document Type](#by-document-type)\n")
    index.append("- [By Status](#by-status)\n")
    index.append("- [Recently Updated](#recently-updated)\n")
    index.append("- [Most Referenced](#most-referenced)\n")
    index.append("- [By Topic](#by-topic)\n")

    # Group by document type
    by_type = defaultdict(list)
    for doc in documents:
        doc_type = doc["front_matter"].get("doc_type", "unknown")
        by_type[doc_type].append(doc)

    index.append("\n## By Document Type\n")

    type_order = [
        "rfc",
        "guide",
        "adr",
        "plan",
        "spec",
        "reference",
        "finding",
        "glossary",
    ]
    type_names = {
        "rfc": "RFCs (Requests for Comments)",
        "guide": "Guides",
        "adr": "Architecture Decision Records",
        "plan": "Planning Documents",
        "spec": "Specifications",
        "reference": "Reference Documentation",
        "finding": "Findings & Analysis",
        "glossary": "Glossaries",
    }

    for doc_type in type_order:
        if doc_type not in by_type:
            continue

        docs = sorted(
            by_type[doc_type], key=lambda d: d["front_matter"].get("doc_id", "")
        )
        index.append(
            f"\n### {type_names.get(doc_type, doc_type.title())} ({len(docs)})\n"
        )

        for doc in docs:
            fm = doc["front_matter"]
            doc_id = fm.get("doc_id", "N/A")
            title = fm.get("title", "Untitled")
            status = fm.get("status", "unknown")
            rel_path = doc["rel_path"]

            # Status emoji
            status_emoji = {
                "active": "✅",
                "draft": "📝",
                "superseded": "🔄",
                "archived": "📦",
            }.get(status, "❓")

            index.append(f"- {status_emoji} **[{doc_id}]({rel_path})**: {title}\n")

    # Group by status
    by_status = defaultdict(list)
    for doc in documents:
        status = doc["front_matter"].get("status", "unknown")
        by_status[status].append(doc)

    index.append("\n## By Status\n")

    status_order = ["active", "draft", "superseded", "archived"]
    for status in status_order:
        if status not in by_status:
            continue

        docs = sorted(
            by_status[status], key=lambda d: d["front_matter"].get("doc_id", "")
        )
        index.append(f"\n### {status.title()} ({len(docs)})\n")

        for doc in docs[:20]:  # Limit to 20 per status
            fm = doc["front_matter"]
            doc_id = fm.get("doc_id", "N/A")
            title = fm.get("title", "Untitled")
            doc_type = fm.get("doc_type", "unknown")
            rel_path = doc["rel_path"]

            index.append(f"- [{doc_id}]({rel_path}) - {title} *({doc_type})*\n")

        if len(docs) > 20:
            index.append(f"\n*...and {len(docs) - 20} more*\n")

    # Recently updated
    docs_with_dates = [
        d
        for d in documents
        if d["front_matter"].get("updated") or d["front_matter"].get("created")
    ]

    recently_updated = sorted(
        docs_with_dates,
        key=lambda d: str(
            d["front_matter"].get("updated") or d["front_matter"].get("created", "")
        ),
        reverse=True,
    )[:15]

    if recently_updated:
        index.append("\n## Recently Updated\n")

        for doc in recently_updated:
            fm = doc["front_matter"]
            doc_id = fm.get("doc_id", "N/A")
            title = fm.get("title", "Untitled")
            updated = fm.get("updated") or fm.get("created", "N/A")
            rel_path = doc["rel_path"]

            index.append(f"- **{updated}**: [{doc_id}]({rel_path}) - {title}\n")

    # Most referenced
    ref_counts = count_references(documents)
    if ref_counts:
        most_referenced = sorted(ref_counts.items(), key=lambda x: x[1], reverse=True)[
            :15
        ]

        index.append("\n## Most Referenced\n")
        index.append("*Documents frequently referenced by others*\n\n")

        for doc_id, count in most_referenced:
            # Find the document
            doc = next(
                (d for d in documents if d["front_matter"].get("doc_id") == doc_id),
                None,
            )
            if doc:
                title = doc["front_matter"].get("title", "Untitled")
                rel_path = doc["rel_path"]
                index.append(
                    f"- [{doc_id}]({rel_path}) - {title} *({count} references)*\n"
                )

    # By topic (tags)
    all_tags = defaultdict(list)
    for doc in documents:
        tags = doc["front_matter"].get("tags", [])
        for tag in tags:
            all_tags[tag].append(doc)

    if all_tags:
        index.append("\n## By Topic\n")

        # Sort tags by document count
        sorted_tags = sorted(all_tags.items(), key=lambda x: len(x[1]), reverse=True)

        for tag, docs in sorted_tags[:20]:  # Top 20 tags
            index.append(f"\n### {tag.title()} ({len(docs)})\n")

            for doc in sorted(docs, key=lambda d: d["front_matter"].get("doc_id", ""))[
                :10
            ]:
                fm = doc["front_matter"]
                doc_id = fm.get("doc_id", "N/A")
                title = fm.get("title", "Untitled")
                rel_path = doc["rel_path"]

                index.append(f"- [{doc_id}]({rel_path}) - {title}\n")

            if len(docs) > 10:
                index.append(f"\n*...and {len(docs) - 10} more*\n")

    return "".join(index)


def generate_directory_readme(
    directory: Path, documents: List[Dict], docs_dir: Path
) -> str:
    """Generate README for a specific directory."""
    # Filter documents in this directory
    dir_docs = [d for d in documents if d["path"].parent == directory]

    if not dir_docs:
        return ""

    readme = []
    dir_name = directory.name.title()

    readme.append(f"# {dir_name}\n")
    readme.append(f"*{len(dir_docs)} documents*\n")

    # Group by status
    by_status = defaultdict(list)
    for doc in dir_docs:
        status = doc["front_matter"].get("status", "unknown")
        by_status[status].append(doc)

    for status in ["active", "draft", "superseded", "archived"]:
        if status not in by_status:
            continue

        docs = sorted(
            by_status[status], key=lambda d: d["front_matter"].get("doc_id", "")
        )
        readme.append(f"\n## {status.title()} ({len(docs)})\n")

        for doc in docs:
            fm = doc["front_matter"]
            doc_id = fm.get("doc_id", "N/A")
            title = fm.get("title", "Untitled")
            summary = fm.get("summary", "")
            filename = doc["path"].name

            readme.append(f"### [{doc_id}]({filename})\n")
            readme.append(f"**{title}**\n\n")
            if summary:
                readme.append(f"{summary}\n\n")

    return "".join(readme)


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description="Generate documentation index")
    parser.add_argument(
        "--docs-dir",
        type=Path,
        help="Documentation directory (default: docs/)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Output file (default: docs/INDEX.md)",
    )
    parser.add_argument(
        "--generate-dir-readmes",
        action="store_true",
        help="Generate README.md for each subdirectory",
    )
    args = parser.parse_args()

    # Resolve paths
    repo_root = Path(__file__).resolve().parents[2]
    docs_dir = args.docs_dir or repo_root / "docs"
    output_file = args.output or docs_dir / "INDEX.md"

    if not docs_dir.exists():
        print(f"Error: Documentation directory not found: {docs_dir}")
        return 1

    print(f"Generating index from {docs_dir}...")

    # Collect documents
    documents = collect_documents(docs_dir)
    print(f"Found {len(documents)} documents")

    # Generate main index
    index_content = generate_index(documents, docs_dir)

    with open(output_file, "w", encoding="utf-8") as f:
        f.write(index_content)

    print(f"Index generated: {output_file}")

    # Generate directory READMEs if requested
    if args.generate_dir_readmes:
        directories = set(doc["path"].parent for doc in documents)

        for directory in directories:
            if directory == docs_dir:
                continue  # Skip root

            readme_content = generate_directory_readme(directory, documents, docs_dir)
            if readme_content:
                readme_path = directory / "README.md"

                # Don't overwrite existing READMEs with custom content
                if readme_path.exists():
                    print(f"Skipping {readme_path} (already exists)")
                    continue

                with open(readme_path, "w", encoding="utf-8") as f:
                    f.write(readme_content)

                print(f"Generated: {readme_path}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
