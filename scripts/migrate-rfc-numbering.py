#!/usr/bin/env python3
"""
RFC numbering migration script.

Migrates doc_ids from PREFIX-YYYY-NNNNN to PREFIX-NNNNN format
and resolves duplicate RFC numbers by reassignment.
"""
import argparse
import re
import sys
from pathlib import Path
from typing import Dict, List, Tuple

import yaml


# Duplicate resolution plan
DUPLICATE_REASSIGNMENTS = {
    # RFC-00014 duplicates
    "014-adopt-dispose-pattern-generator.md": 26,  # Reassign to 026
    # RFC-00020 duplicates  
    "020-nexus-goap-ai-system.md": 27,  # Reassign to 027
    # RFC-00021 duplicates
    "021-nexus-perception-system.md": 28,  # Reassign to 028
}


def load_document(file_path: Path) -> Tuple[Dict, str, str]:
    """Load document and return (front_matter, content, full_text)."""
    with open(file_path, encoding="utf-8") as f:
        full_text = f.read()
    
    if not full_text.startswith("---"):
        return None, None, full_text
    
    parts = full_text.split("---", 2)
    if len(parts) < 3:
        return None, None, full_text
    
    front_matter = yaml.safe_load(parts[1])
    content = parts[2]
    
    return front_matter, content, full_text


def update_doc_id_format(doc_id: str) -> str:
    """Convert doc_id from PREFIX-YYYY-NNNNN to PREFIX-NNNNN."""
    # Match PREFIX-YYYY-NNNNN format
    match = re.match(r"([A-Z]+)-\d{4}-(\d{5})", doc_id)
    if match:
        prefix = match.group(1)
        number = match.group(2)
        return f"{prefix}-{number}"
    
    # Already in new format or invalid
    return doc_id


def migrate_file(
    file_path: Path,
    new_number: int = None,
    dry_run: bool = True,
) -> Dict:
    """Migrate a single file to new format."""
    result = {
        "path": str(file_path),
        "old_doc_id": None,
        "new_doc_id": None,
        "old_filename": file_path.name,
        "new_filename": None,
        "renumbered": False,
        "format_updated": False,
        "success": False,
    }
    
    try:
        front_matter, content, full_text = load_document(file_path)
        
        if not front_matter:
            result["error"] = "No front-matter found"
            return result
        
        old_doc_id = front_matter.get("doc_id", "")
        result["old_doc_id"] = old_doc_id
        
        # Determine new doc_id
        if new_number:
            # Reassigning number
            prefix = old_doc_id.split("-")[0] if "-" in old_doc_id else "RFC"
            new_doc_id = f"{prefix}-{new_number:05d}"
            result["renumbered"] = True
        else:
            # Just updating format
            new_doc_id = update_doc_id_format(old_doc_id)
            result["format_updated"] = (new_doc_id != old_doc_id)
        
        result["new_doc_id"] = new_doc_id
        
        # Update front-matter
        front_matter["doc_id"] = new_doc_id
        
        # Update related/supersedes/implements fields
        for field in ["related", "supersedes"]:
            if field in front_matter and front_matter[field]:
                front_matter[field] = [
                    update_doc_id_format(ref) for ref in front_matter[field]
                ]
        
        if "implements" in front_matter and front_matter["implements"]:
            front_matter["implements"] = update_doc_id_format(front_matter["implements"])
        
        # Update dependencies
        if "dependencies" in front_matter:
            deps = front_matter["dependencies"]
            if "rfcs" in deps and deps["rfcs"]:
                deps["rfcs"] = [update_doc_id_format(ref) for ref in deps["rfcs"]]
            if "prds" in deps and deps["prds"]:
                deps["prds"] = [update_doc_id_format(ref) for ref in deps["prds"]]
        
        # Update implementation.rfcs
        if "implementation" in front_matter:
            impl = front_matter["implementation"]
            if "rfcs" in impl and impl["rfcs"]:
                impl["rfcs"] = [update_doc_id_format(ref) for ref in impl["rfcs"]]
        
        # Determine new filename
        if new_number:
            # Extract title slug from old filename
            old_match = re.match(r"\d{3}-(.+)", file_path.name)
            if old_match:
                title_slug = old_match.group(1)
                new_filename = f"{new_number:03d}-{title_slug}"
            else:
                new_filename = f"{new_number:03d}-{file_path.name}"
        else:
            new_filename = file_path.name
        
        result["new_filename"] = new_filename
        
        # Update content references (doc_ids in markdown)
        updated_content = content
        # Find all doc_id references in content
        doc_id_pattern = r"([A-Z]+)-\d{4}-(\d{5})"
        
        def replace_doc_id(match):
            return f"{match.group(1)}-{match.group(2)}"
        
        updated_content = re.sub(doc_id_pattern, replace_doc_id, updated_content)
        
        if not dry_run:
            # Write updated file
            new_content = "---\n"
            new_content += yaml.dump(front_matter, default_flow_style=False, allow_unicode=True)
            new_content += "---\n"
            new_content += updated_content
            
            # Write to new location if renaming
            new_path = file_path.parent / new_filename
            with open(new_path, "w", encoding="utf-8") as f:
                f.write(new_content)
            
            # Remove old file if renamed
            if new_filename != file_path.name:
                file_path.unlink()
        
        result["success"] = True
        
    except Exception as e:
        result["error"] = str(e)
    
    return result


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Migrate RFC numbering to new format"
    )
    parser.add_argument(
        "--docs-dir",
        type=Path,
        help="Documentation directory (default: docs/)",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Apply migration (default: dry-run)",
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
    
    print(f"RFC Numbering Migration")
    print(f"Mode: {'APPLY' if args.apply else 'DRY-RUN'}")
    print()
    
    results = []
    
    # Phase 1: Reassign duplicates
    print("Phase 1: Reassigning duplicate numbers...")
    rfcs_dir = docs_dir / "rfcs"
    
    for filename, new_number in DUPLICATE_REASSIGNMENTS.items():
        file_path = rfcs_dir / filename
        if file_path.exists():
            print(f"  Reassigning {filename} → {new_number:03d}")
            result = migrate_file(file_path, new_number, dry_run=not args.apply)
            results.append(result)
        else:
            print(f"  Warning: {filename} not found")
    
    # Phase 2: Update all docs to new format
    print("\nPhase 2: Updating doc_id format for all documents...")
    
    for md_file in docs_dir.rglob("*.md"):
        # Skip if already processed
        if md_file.name in DUPLICATE_REASSIGNMENTS:
            continue
        
        # Skip certain patterns
        rel_path = md_file.relative_to(docs_dir)
        if any(str(rel_path).startswith(pattern) for pattern in ["index/", "archive/"]):
            continue
        
        # Skip specific files
        if md_file.name in ["README.md", "IMPLEMENTATION_PLAN.md"]:
            continue
        
        result = migrate_file(md_file, dry_run=not args.apply)
        if result["format_updated"] or result["renumbered"]:
            print(f"  {md_file.name}: {result['old_doc_id']} → {result['new_doc_id']}")
            results.append(result)
    
    # Generate migration log
    migration_log = {
        "mode": "apply" if args.apply else "dry-run",
        "total_files": len(results),
        "renumbered": sum(1 for r in results if r.get("renumbered")),
        "format_updated": sum(1 for r in results if r.get("format_updated")),
        "successful": sum(1 for r in results if r.get("success")),
        "failed": sum(1 for r in results if not r.get("success")),
        "migrations": results,
    }
    
    # Output log
    if args.output:
        import json
        with open(args.output, "w", encoding="utf-8") as f:
            json.dump(migration_log, f, indent=2, ensure_ascii=False)
        print(f"\nMigration log written to: {args.output}")
    
    # Summary
    print(f"\n=== SUMMARY ===")
    print(f"Total files processed: {migration_log['total_files']}")
    print(f"Renumbered (duplicates): {migration_log['renumbered']}")
    print(f"Format updated: {migration_log['format_updated']}")
    print(f"Successful: {migration_log['successful']}")
    print(f"Failed: {migration_log['failed']}")
    
    if args.apply:
        print("\n✅ Migration complete!")
        print("\nNext steps:")
        print("1. Run: python scripts/validate-docs.py")
        print("2. Regenerate registry and indexes")
        print("3. Commit changes with: git commit -m 'docs: migrate to RFC-00026 numbering format'")
    else:
        print("\nDRY-RUN: No files modified. Use --apply to apply migration.")
    
    return 0 if migration_log["failed"] == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
