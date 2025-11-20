#!/usr/bin/env python3
"""
Inbox cleanup automation.

Identifies stale inbox documents, suggests graduation or archival,
and auto-archives conversation dumps.
"""
import argparse
import json
import shutil
import sys
from datetime import datetime, timedelta
from pathlib import Path
from typing import Dict, List, Optional

import yaml


# Inbox retention policy
DEFAULT_RETENTION_DAYS = 30

# Patterns for conversation dumps
CONVERSATION_PATTERNS = [
    "caveat-the-messages",
    "conversation-",
    "chat-",
    "session-",
]


def parse_front_matter(file_path: Path) -> Optional[Dict]:
    """Parse YAML front-matter from a file."""
    try:
        with open(file_path, encoding="utf-8") as f:
            content = f.read()
        
        if not content.startswith("---"):
            return None
        
        parts = content.split("---", 2)
        if len(parts) < 3:
            return None
        
        return yaml.safe_load(parts[1])
    except Exception:
        return None


def is_conversation_dump(file_path: Path) -> bool:
    """Check if file is a conversation dump."""
    filename = file_path.name.lower()
    
    # Check filename patterns
    for pattern in CONVERSATION_PATTERNS:
        if pattern in filename:
            return True
    
    # Check file extension
    if file_path.suffix == ".txt":
        return True
    
    # Check file size (conversation dumps tend to be large)
    try:
        size_kb = file_path.stat().st_size / 1024
        if size_kb > 50:  # > 50KB
            return True
    except Exception:
        pass
    
    return False


def get_file_age(file_path: Path) -> int:
    """Get file age in days."""
    try:
        mtime = datetime.fromtimestamp(file_path.stat().st_mtime)
        age = datetime.now() - mtime
        return age.days
    except Exception:
        return 0


def analyze_inbox_file(file_path: Path, retention_days: int) -> Dict:
    """Analyze a single inbox file."""
    front_matter = parse_front_matter(file_path)
    age_days = get_file_age(file_path)
    is_conv_dump = is_conversation_dump(file_path)
    
    # Determine status
    status = "keep"
    reason = ""
    suggested_action = ""
    
    if is_conv_dump:
        status = "archive"
        reason = "Conversation dump"
        suggested_action = "Move to docs/archive/conversations/"
    elif age_days > retention_days:
        if front_matter:
            # Has front-matter, ready to graduate
            status = "graduate"
            reason = f"Stale ({age_days} days old) but has front-matter"
            doc_type = front_matter.get("doc_type", "unknown")
            suggested_action = f"Move to appropriate location for {doc_type}"
        else:
            # No front-matter, should archive or delete
            status = "archive"
            reason = f"Stale ({age_days} days old) without front-matter"
            suggested_action = "Archive or delete"
    elif not front_matter:
        status = "warn"
        reason = "Missing front-matter"
        suggested_action = "Add front-matter or archive"
    
    return {
        "path": str(file_path),
        "name": file_path.name,
        "age_days": age_days,
        "has_frontmatter": front_matter is not None,
        "is_conversation_dump": is_conv_dump,
        "status": status,
        "reason": reason,
        "suggested_action": suggested_action,
        "doc_type": front_matter.get("doc_type") if front_matter else None,
    }


def cleanup_inbox(
    inbox_dir: Path,
    archive_dir: Path,
    retention_days: int,
    dry_run: bool = True,
) -> Dict:
    """Clean up inbox directory."""
    results = {
        "keep": [],
        "graduate": [],
        "archive": [],
        "warn": [],
    }
    
    # Scan inbox
    for md_file in inbox_dir.glob("*.md"):
        # Skip README
        if md_file.name == "README.md":
            continue
        
        analysis = analyze_inbox_file(md_file, retention_days)
        status = analysis["status"]
        results[status].append(analysis)
        
        # Take action if not dry-run
        if not dry_run:
            if status == "archive":
                # Archive the file
                if analysis["is_conversation_dump"]:
                    target_dir = archive_dir / "conversations"
                else:
                    target_dir = archive_dir
                
                target_dir.mkdir(parents=True, exist_ok=True)
                target_path = target_dir / md_file.name
                
                # Handle name conflicts
                counter = 1
                while target_path.exists():
                    stem = md_file.stem
                    target_path = target_dir / f"{stem}-{counter}{md_file.suffix}"
                    counter += 1
                
                shutil.move(str(md_file), str(target_path))
                analysis["archived_to"] = str(target_path)
    
    # Also check .txt files
    for txt_file in inbox_dir.glob("*.txt"):
        if is_conversation_dump(txt_file):
            analysis = {
                "path": str(txt_file),
                "name": txt_file.name,
                "age_days": get_file_age(txt_file),
                "has_frontmatter": False,
                "is_conversation_dump": True,
                "status": "archive",
                "reason": "Conversation dump (.txt)",
                "suggested_action": "Move to docs/archive/conversations/",
            }
            results["archive"].append(analysis)
            
            if not dry_run:
                target_dir = archive_dir / "conversations"
                target_dir.mkdir(parents=True, exist_ok=True)
                target_path = target_dir / txt_file.name
                
                counter = 1
                while target_path.exists():
                    stem = txt_file.stem
                    target_path = target_dir / f"{stem}-{counter}{txt_file.suffix}"
                    counter += 1
                
                shutil.move(str(txt_file), str(target_path))
                analysis["archived_to"] = str(target_path)
    
    return results


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Clean up documentation inbox"
    )
    parser.add_argument(
        "--inbox-dir",
        type=Path,
        help="Inbox directory (default: docs/_inbox/)",
    )
    parser.add_argument(
        "--archive-dir",
        type=Path,
        help="Archive directory (default: docs/archive/)",
    )
    parser.add_argument(
        "--retention-days",
        type=int,
        default=DEFAULT_RETENTION_DAYS,
        help=f"Retention period in days (default: {DEFAULT_RETENTION_DAYS})",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Apply cleanup actions (default: dry-run)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Output report file (default: stdout)",
    )
    args = parser.parse_args()
    
    # Resolve paths
    repo_root = Path(__file__).parent.parent
    inbox_dir = args.inbox_dir or repo_root / "docs" / "_inbox"
    archive_dir = args.archive_dir or repo_root / "docs" / "archive"
    
    if not inbox_dir.exists():
        print(f"Error: Inbox directory not found: {inbox_dir}")
        return 1
    
    print(f"Analyzing inbox: {inbox_dir}")
    print(f"Retention policy: {args.retention_days} days")
    print(f"Mode: {'APPLY' if args.apply else 'DRY-RUN'}")
    
    # Clean up inbox
    results = cleanup_inbox(
        inbox_dir,
        archive_dir,
        args.retention_days,
        dry_run=not args.apply,
    )
    
    # Generate report
    report = {
        "timestamp": datetime.now().isoformat(),
        "mode": "apply" if args.apply else "dry-run",
        "retention_days": args.retention_days,
        "summary": {
            "keep": len(results["keep"]),
            "graduate": len(results["graduate"]),
            "archive": len(results["archive"]),
            "warn": len(results["warn"]),
            "total": sum(len(v) for v in results.values()),
        },
        "details": results,
    }
    
    # Output report
    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            json.dump(report, f, indent=2, ensure_ascii=False)
        print(f"\nReport written to: {args.output}")
    
    # Print summary
    print(f"\n=== SUMMARY ===")
    print(f"Total files analyzed: {report['summary']['total']}")
    print(f"Keep: {report['summary']['keep']}")
    print(f"Ready to graduate: {report['summary']['graduate']}")
    print(f"Should archive: {report['summary']['archive']}")
    print(f"Warnings: {report['summary']['warn']}")
    
    # Print details
    if results["graduate"]:
        print(f"\n=== READY TO GRADUATE ({len(results['graduate'])}) ===")
        for item in results["graduate"]:
            print(f"  {item['name']}: {item['reason']}")
            print(f"    → {item['suggested_action']}")
    
    if results["archive"]:
        print(f"\n=== SHOULD ARCHIVE ({len(results['archive'])}) ===")
        for item in results["archive"]:
            print(f"  {item['name']}: {item['reason']}")
            if "archived_to" in item:
                print(f"    → Archived to: {item['archived_to']}")
    
    if results["warn"]:
        print(f"\n=== WARNINGS ({len(results['warn'])}) ===")
        for item in results["warn"]:
            print(f"  {item['name']}: {item['reason']}")
            print(f"    → {item['suggested_action']}")
    
    if args.apply:
        print("\nCleanup complete!")
    else:
        print("\nDRY-RUN: No files modified. Use --apply to apply changes.")
    
    return 0


if __name__ == "__main__":
    sys.exit(main())
