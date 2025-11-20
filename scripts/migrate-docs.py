#!/usr/bin/env python3
"""
Batch migration tool for documentation files.

Applies generated front-matter, relocates files to correct locations,
and updates cross-references.
"""
import argparse
import json
import re
import shutil
import sys
from pathlib import Path
from typing import Dict, List, Set, Tuple

import yaml


# Doc type to directory mapping
DOC_TYPE_DIRS = {
    "rfc": "rfcs",
    "guide": "guides",
    "adr": "architecture",
    "plan": "planning",
    "finding": "planning",
    "spec": ".",
    "reference": ".",
    "glossary": ".",
}


def load_migration_report(report_path: Path) -> Dict:
    """Load the front-matter generation report."""
    with open(report_path, encoding="utf-8") as f:
        return json.load(f)


def determine_target_location(
    file_path: Path,
    doc_type: str,
    docs_dir: Path,
) -> Path:
    """Determine the target location for a document based on its type."""
    target_subdir = DOC_TYPE_DIRS.get(doc_type, ".")
    
    # Special handling for dotnet docs
    if "dotnet" in file_path.parts:
        # Keep dotnet docs in their subdirectories
        if "guides" in file_path.parts:
            return file_path
        if "architecture" in file_path.parts:
            return file_path
        # Move other dotnet docs to appropriate dotnet subdirs
        if doc_type == "guide":
            return docs_dir / "dotnet" / "guides" / file_path.name
        if doc_type == "reference":
            return docs_dir / "dotnet" / file_path.name
    
    # For inbox files, move to appropriate location
    if "_inbox" in file_path.parts:
        if target_subdir == ".":
            return docs_dir / file_path.name
        return docs_dir / target_subdir / file_path.name
    
    # Check if already in correct location
    if target_subdir == ".":
        # Root level docs
        if file_path.parent == docs_dir:
            return file_path
        return docs_dir / file_path.name
    
    # Check if in correct subdirectory
    if target_subdir in file_path.parts:
        return file_path
    
    # Move to correct subdirectory
    return docs_dir / target_subdir / file_path.name


def find_cross_references(content: str) -> List[str]:
    """Find all markdown links to other documentation files."""
    # Pattern: [text](path/to/file.md) or [text](file.md)
    pattern = r"\[([^\]]+)\]\(([^\)]+\.md)\)"
    matches = re.findall(pattern, content)
    return [match[1] for match in matches]


def update_cross_reference(
    ref: str,
    old_path: Path,
    new_path: Path,
    docs_dir: Path,
) -> str:
    """Update a cross-reference to reflect new file location."""
    # Handle absolute paths
    if ref.startswith("/"):
        return ref
    
    # Handle relative paths
    if ref.startswith("../") or ref.startswith("./"):
        # Resolve relative to old location
        old_dir = old_path.parent
        ref_path = (old_dir / ref).resolve()
        
        # Make relative to new location
        new_dir = new_path.parent
        try:
            new_ref = ref_path.relative_to(new_dir)
            return str(new_ref).replace("\\", "/")
        except ValueError:
            # Can't make relative, use absolute from docs root
            try:
                abs_ref = ref_path.relative_to(docs_dir)
                return "/" + str(abs_ref).replace("\\", "/")
            except ValueError:
                return ref
    
    # Simple filename reference - search for it
    return ref


def update_file_references(
    content: str,
    old_path: Path,
    new_path: Path,
    docs_dir: Path,
) -> str:
    """Update all cross-references in content."""
    refs = find_cross_references(content)
    updated_content = content
    
    for ref in refs:
        new_ref = update_cross_reference(ref, old_path, new_path, docs_dir)
        if new_ref != ref:
            updated_content = updated_content.replace(f"]({ref})", f"]({new_ref})")
    
    return updated_content


def migrate_file(
    file_info: Dict,
    docs_dir: Path,
    dry_run: bool = True,
) -> Dict:
    """Migrate a single file to its correct location."""
    old_path = Path(file_info["path"])
    doc_type = file_info["doc_type"]
    
    # Determine target location
    new_path = determine_target_location(old_path, doc_type, docs_dir)
    
    # Check if move is needed
    needs_move = old_path != new_path
    
    result = {
        "old_path": str(old_path),
        "new_path": str(new_path),
        "doc_type": doc_type,
        "needs_move": needs_move,
        "success": False,
    }
    
    if not dry_run:
        try:
            # Read current content
            with open(old_path, encoding="utf-8") as f:
                content = f.read()
            
            # Update cross-references if moving
            if needs_move:
                content = update_file_references(content, old_path, new_path, docs_dir)
            
            # Ensure target directory exists
            new_path.parent.mkdir(parents=True, exist_ok=True)
            
            # Write to new location
            with open(new_path, "w", encoding="utf-8") as f:
                f.write(content)
            
            # Remove old file if moved
            if needs_move and old_path != new_path:
                old_path.unlink()
            
            result["success"] = True
        except Exception as e:
            result["error"] = str(e)
    else:
        result["success"] = True  # Dry-run always succeeds
    
    return result


def update_references_in_corpus(
    moves: List[Dict],
    docs_dir: Path,
    dry_run: bool = True,
) -> int:
    """Update references in all other files to reflect moved files."""
    # Build old -> new path mapping
    path_map = {}
    for move in moves:
        if move["needs_move"] and move["success"]:
            path_map[Path(move["old_path"])] = Path(move["new_path"])
    
    if not path_map:
        return 0
    
    updated_count = 0
    
    # Scan all markdown files
    for md_file in docs_dir.rglob("*.md"):
        if md_file in path_map.values():
            continue  # Skip files we just moved
        
        try:
            with open(md_file, encoding="utf-8") as f:
                content = f.read()
            
            updated_content = content
            
            # Update references to moved files
            for old_path, new_path in path_map.items():
                # Find references to old path
                old_name = old_path.name
                new_name = new_path.name
                
                # Update simple filename references
                if old_name in content:
                    # Calculate relative path from this file to new location
                    try:
                        rel_path = new_path.relative_to(md_file.parent)
                        updated_content = re.sub(
                            rf"\](\([./]*{re.escape(old_name)})\)",
                            f"]({rel_path})",
                            updated_content,
                        )
                    except ValueError:
                        pass
            
            # Write if changed
            if updated_content != content and not dry_run:
                with open(md_file, "w", encoding="utf-8") as f:
                    f.write(updated_content)
                updated_count += 1
        
        except Exception as e:
            print(f"Warning: Failed to update references in {md_file}: {e}")
    
    return updated_count


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Migrate documentation files to correct locations"
    )
    parser.add_argument(
        "--report",
        type=Path,
        required=True,
        help="Front-matter generation report (JSON)",
    )
    parser.add_argument(
        "--docs-dir",
        type=Path,
        help="Documentation directory (default: docs/)",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Apply migrations (default: dry-run)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Output migration log (default: stdout)",
    )
    args = parser.parse_args()
    
    # Resolve paths
    repo_root = Path(__file__).parent.parent
    docs_dir = args.docs_dir or repo_root / "docs"
    
    if not docs_dir.exists():
        print(f"Error: Documentation directory not found: {docs_dir}")
        return 1
    
    if not args.report.exists():
        print(f"Error: Report file not found: {args.report}")
        return 1
    
    print(f"Loading migration report from {args.report}...")
    report = load_migration_report(args.report)
    
    print(f"Mode: {'APPLY' if args.apply else 'DRY-RUN'}")
    print(f"Processing {len(report['files'])} files...")
    
    # Migrate files
    results = []
    for file_info in report["files"]:
        result = migrate_file(file_info, docs_dir, dry_run=not args.apply)
        results.append(result)
    
    # Update cross-references
    if args.apply:
        print("\nUpdating cross-references in corpus...")
        updated_count = update_references_in_corpus(results, docs_dir, dry_run=False)
        print(f"Updated references in {updated_count} files")
    
    # Generate migration log
    migration_log = {
        "timestamp": report["generated_at"],
        "mode": "apply" if args.apply else "dry-run",
        "total_files": len(results),
        "moved_files": sum(1 for r in results if r["needs_move"]),
        "successful": sum(1 for r in results if r["success"]),
        "failed": sum(1 for r in results if not r["success"]),
        "migrations": results,
    }
    
    # Output log
    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            json.dump(migration_log, f, indent=2, ensure_ascii=False)
        print(f"\nMigration log written to: {args.output}")
    else:
        print("\n=== MIGRATION LOG ===")
        print(json.dumps(migration_log, indent=2, ensure_ascii=False))
    
    # Summary
    print(f"\n=== SUMMARY ===")
    print(f"Total files: {migration_log['total_files']}")
    print(f"Files moved: {migration_log['moved_files']}")
    print(f"Successful: {migration_log['successful']}")
    print(f"Failed: {migration_log['failed']}")
    
    if args.apply:
        print("\nMigration complete!")
    else:
        print("\nDRY-RUN: No files modified. Use --apply to apply changes.")
    
    return 0 if migration_log["failed"] == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
