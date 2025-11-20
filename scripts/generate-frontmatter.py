#!/usr/bin/env python3
"""
Automated front-matter generation for documentation files.

Scans markdown files without front-matter and generates suggested metadata
based on content analysis and file location.
"""
import argparse
import hashlib
import json
import re
import sys
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional, Tuple

import yaml

# Valid doc types and their typical locations
DOC_TYPE_LOCATIONS = {
    "rfc": ["rfcs"],
    "guide": ["guides", "dotnet/guides"],
    "adr": ["adr", "architecture"],
    "plan": ["planning"],
    "finding": ["planning", "notes"],
    "spec": ["fonts", "terminals"],
    "reference": [".", "dotnet"],
    "glossary": ["."],
}

# Common tag patterns
TAG_PATTERNS = {
    r"\b(rendering|graphics|skiasharp|kitty|sixel)\b": "rendering",
    r"\b(plugin|extensibility|alc)\b": "plugins",
    r"\b(test|testing|verification|qa)\b": "testing",
    r"\b(agent|automation|infrastructure)\b": "agents",
    r"\b(ecs|entity|component|system)\b": "ecs",
    r"\b(font|cjk|pua|glyph)\b": "fonts",
    r"\b(terminal|console|rio)\b": "terminal",
    r"\b(ci|cd|github|actions)\b": "ci-cd",
    r"\b(architecture|design|pattern)\b": "architecture",
    r"\b(documentation|docs|schema)\b": "documentation",
}


def extract_title_from_content(content: str) -> Optional[str]:
    """Extract title from markdown content (first H1 heading)."""
    lines = content.split("\n")
    for line in lines:
        # Check for # Title format
        if line.startswith("# "):
            return line[2:].strip()
        # Check for Title\n=== format
        if line.strip() and len(lines) > lines.index(line) + 1:
            next_line = lines[lines.index(line) + 1]
            if re.match(r"^=+$", next_line.strip()):
                return line.strip()
    return None


def infer_doc_type(file_path: Path, content: str) -> str:
    """Infer document type based on file location and content."""
    rel_path = str(file_path).lower()
    
    # Check location-based inference
    for doc_type, locations in DOC_TYPE_LOCATIONS.items():
        for location in locations:
            if f"/{location}/" in rel_path or f"\\{location}\\" in rel_path:
                return doc_type
    
    # Content-based inference
    content_lower = content.lower()
    if "rfc-" in content_lower or "request for comment" in content_lower:
        return "rfc"
    if "architecture decision" in content_lower or "adr-" in content_lower:
        return "adr"
    if "how to" in content_lower or "guide" in content_lower:
        return "guide"
    if "specification" in content_lower or "spec:" in content_lower:
        return "spec"
    if "plan" in content_lower or "roadmap" in content_lower:
        return "plan"
    
    # Default to guide for general documentation
    return "guide"


def extract_tags(content: str, doc_type: str) -> List[str]:
    """Extract relevant tags from content."""
    tags = set()
    content_lower = content.lower()
    
    # Add doc_type as a tag
    tags.add(doc_type)
    
    # Pattern-based tag extraction
    for pattern, tag in TAG_PATTERNS.items():
        if re.search(pattern, content_lower):
            tags.add(tag)
    
    return sorted(list(tags))


def generate_doc_id(doc_type: str, existing_ids: set) -> str:
    """Generate a unique doc_id."""
    prefix_map = {
        "rfc": "RFC",
        "adr": "ADR",
        "guide": "GUIDE",
        "plan": "PLAN",
        "finding": "FIND",
        "spec": "SPEC",
        "glossary": "GLOSSARY",
        "reference": "REFERENCE",
    }
    
    prefix = prefix_map.get(doc_type, "DOC")
    year = datetime.now().year
    
    # Find next available number
    number = 1
    while True:
        doc_id = f"{prefix}-{year}-{number:05d}"
        if doc_id not in existing_ids:
            return doc_id
        number += 1


def generate_summary(content: str, title: str) -> str:
    """Generate a brief summary from content."""
    # Remove front-matter if present
    if content.startswith("---"):
        parts = content.split("---", 2)
        if len(parts) >= 3:
            content = parts[2]
    
    # Extract first paragraph after title
    lines = content.split("\n")
    summary_lines = []
    found_content = False
    
    for line in lines:
        line = line.strip()
        # Skip empty lines and markdown headers
        if not line or line.startswith("#"):
            if found_content:
                break
            continue
        
        # Skip markdown formatting
        if line.startswith("```") or line.startswith("---"):
            continue
        
        found_content = True
        summary_lines.append(line)
        
        # Stop at first paragraph (max 2 sentences)
        if len(summary_lines) >= 2 or len(" ".join(summary_lines)) > 150:
            break
    
    summary = " ".join(summary_lines)
    # Clean up markdown formatting
    summary = re.sub(r"\[([^\]]+)\]\([^\)]+\)", r"\1", summary)  # Remove links
    summary = re.sub(r"[*_`]", "", summary)  # Remove emphasis
    summary = summary[:200]  # Limit length
    
    return summary if summary else f"Documentation for {title}"


def scan_existing_doc_ids(docs_dir: Path) -> set:
    """Scan existing documents to get used doc_ids."""
    existing_ids = set()
    
    for md_file in docs_dir.rglob("*.md"):
        try:
            with open(md_file, encoding="utf-8") as f:
                content = f.read()
            
            if content.startswith("---"):
                parts = content.split("---", 2)
                if len(parts) >= 3:
                    front_matter = yaml.safe_load(parts[1])
                    if front_matter and "doc_id" in front_matter:
                        existing_ids.add(front_matter["doc_id"])
        except Exception:
            continue
    
    return existing_ids


def generate_front_matter(
    file_path: Path,
    content: str,
    existing_ids: set,
    is_inbox: bool = False,
) -> Dict:
    """Generate front-matter for a document."""
    # Extract or infer title
    title = extract_title_from_content(content)
    if not title:
        title = file_path.stem.replace("-", " ").replace("_", " ").title()
    
    # Infer doc type
    doc_type = infer_doc_type(file_path, content)
    
    # Generate doc_id (skip for inbox)
    doc_id = None if is_inbox else generate_doc_id(doc_type, existing_ids)
    
    # Extract tags
    tags = extract_tags(content, doc_type)
    
    # Generate summary
    summary = generate_summary(content, title)
    
    # Determine status
    status = "draft" if is_inbox else "active"
    
    # Build front-matter
    front_matter = {
        "title": title,
        "doc_type": doc_type,
        "status": status,
        "created": datetime.now().strftime("%Y-%m-%d"),
        "tags": tags,
        "summary": summary,
    }
    
    if doc_id:
        front_matter["doc_id"] = doc_id
        front_matter["canonical"] = True
        front_matter["supersedes"] = []
        front_matter["related"] = []
    
    return front_matter


def format_front_matter(front_matter: Dict) -> str:
    """Format front-matter as YAML."""
    # Custom ordering for readability
    ordered_keys = [
        "doc_id",
        "title",
        "doc_type",
        "status",
        "canonical",
        "created",
        "updated",
        "author",
        "tags",
        "summary",
        "supersedes",
        "related",
    ]
    
    ordered_fm = {}
    for key in ordered_keys:
        if key in front_matter:
            ordered_fm[key] = front_matter[key]
    
    # Add any remaining keys
    for key, value in front_matter.items():
        if key not in ordered_fm:
            ordered_fm[key] = value
    
    yaml_str = yaml.dump(ordered_fm, default_flow_style=False, allow_unicode=True)
    return f"---\n{yaml_str}---\n"


def process_file(
    file_path: Path,
    existing_ids: set,
    dry_run: bool = True,
) -> Optional[Dict]:
    """Process a single file and generate front-matter."""
    try:
        with open(file_path, encoding="utf-8") as f:
            content = f.read()
    except Exception as e:
        print(f"Error reading {file_path}: {e}")
        return None
    
    # Skip if already has front-matter
    if content.startswith("---"):
        return None
    
    # Check if in inbox
    is_inbox = "_inbox" in file_path.parts
    
    # Generate front-matter
    front_matter = generate_front_matter(file_path, content, existing_ids, is_inbox)
    
    # Format result
    result = {
        "path": str(file_path),
        "front_matter": front_matter,
        "title": front_matter["title"],
        "doc_type": front_matter["doc_type"],
        "doc_id": front_matter.get("doc_id", "N/A"),
    }
    
    if not dry_run:
        # Apply front-matter to file
        new_content = format_front_matter(front_matter) + "\n" + content
        with open(file_path, "w", encoding="utf-8") as f:
            f.write(new_content)
        result["applied"] = True
    
    return result


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Generate front-matter for documentation files"
    )
    parser.add_argument(
        "--docs-dir",
        type=Path,
        help="Documentation directory (default: docs/)",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Apply generated front-matter to files (default: dry-run)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Output report file (default: stdout)",
    )
    parser.add_argument(
        "--exclude",
        nargs="+",
        default=["index/", "archive/"],
        help="Patterns to exclude (default: index/ archive/)",
    )
    args = parser.parse_args()
    
    # Resolve paths
    repo_root = Path(__file__).parent.parent
    docs_dir = args.docs_dir or repo_root / "docs"
    
    if not docs_dir.exists():
        print(f"Error: Documentation directory not found: {docs_dir}")
        return 1
    
    print(f"Scanning documentation in {docs_dir}...")
    print(f"Mode: {'APPLY' if args.apply else 'DRY-RUN'}")
    
    # Scan existing doc_ids
    existing_ids = scan_existing_doc_ids(docs_dir)
    print(f"Found {len(existing_ids)} existing doc_ids")
    
    # Process files
    results = []
    for md_file in docs_dir.rglob("*.md"):
        # Skip excluded patterns
        rel_path = md_file.relative_to(docs_dir)
        if any(str(rel_path).startswith(pattern) for pattern in args.exclude):
            continue
        
        result = process_file(md_file, existing_ids, dry_run=not args.apply)
        if result:
            results.append(result)
            if result.get("doc_id") != "N/A":
                existing_ids.add(result["doc_id"])
    
    # Generate report
    report = {
        "generated_at": datetime.now().isoformat(),
        "mode": "apply" if args.apply else "dry-run",
        "total_processed": len(results),
        "by_type": {},
        "files": results,
    }
    
    for result in results:
        doc_type = result["doc_type"]
        report["by_type"][doc_type] = report["by_type"].get(doc_type, 0) + 1
    
    # Output report
    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            json.dump(report, f, indent=2, ensure_ascii=False)
        print(f"\nReport written to: {args.output}")
    else:
        print("\n=== REPORT ===")
        print(json.dumps(report, indent=2, ensure_ascii=False))
    
    # Summary
    print(f"\n=== SUMMARY ===")
    print(f"Files processed: {len(results)}")
    print(f"By type: {report['by_type']}")
    if args.apply:
        print("\nFront-matter applied to all files!")
    else:
        print("\nDRY-RUN: No files modified. Use --apply to apply changes.")
    
    return 0


if __name__ == "__main__":
    sys.exit(main())
