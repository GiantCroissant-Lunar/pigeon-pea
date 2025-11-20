#!/usr/bin/env python3
"""
Enhanced documentation validation with tiered levels and quality scoring.

Extends the existing validate-docs.py with:
- 4-tier validation levels (Inbox, Draft, Active, Canonical)
- Quality metrics (completeness, freshness, linkage, clarity)
- Automated quality reports
"""
import argparse
import json
import re
import sys
from datetime import datetime, timedelta
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple

import yaml


# Validation levels
class ValidationLevel:
    INBOX = 1      # Minimal validation
    DRAFT = 2      # Required fields + basic quality
    ACTIVE = 3     # Full validation + completeness
    CANONICAL = 4  # Strictest validation + uniqueness


# Quality thresholds
QUALITY_THRESHOLDS = {
    "excellent": 90,
    "good": 75,
    "acceptable": 60,
    "needs_improvement": 40,
}


def determine_validation_level(doc_path: Path, front_matter: Dict) -> int:
    """Determine validation level for a document."""
    # Inbox documents
    if "_inbox" in doc_path.parts:
        return ValidationLevel.INBOX
    
    # Canonical documents
    if front_matter.get("canonical"):
        return ValidationLevel.CANONICAL
    
    # Active vs draft
    status = front_matter.get("status", "draft")
    if status == "active":
        return ValidationLevel.ACTIVE
    
    return ValidationLevel.DRAFT


def calculate_completeness_score(front_matter: Dict) -> int:
    """Calculate completeness score (0-100)."""
    # All possible fields
    all_fields = {
        "doc_id", "title", "doc_type", "status", "canonical",
        "created", "updated", "author", "tags", "summary",
        "supersedes", "related", "implementation", "dependencies"
    }
    
    # Count filled fields
    filled = sum(1 for field in all_fields if field in front_matter and front_matter[field])
    
    # Weight required fields more heavily
    required_fields = {"doc_id", "title", "doc_type", "status", "canonical", "created", "tags", "summary"}
    required_filled = sum(1 for field in required_fields if field in front_matter and front_matter[field])
    
    # Score: 70% for required, 30% for optional
    required_score = (required_filled / len(required_fields)) * 70
    optional_score = ((filled - required_filled) / (len(all_fields) - len(required_fields))) * 30
    
    return int(required_score + optional_score)


def calculate_freshness_score(front_matter: Dict) -> int:
    """Calculate freshness score (0-100)."""
    created = front_matter.get("created")
    updated = front_matter.get("updated")
    
    if not created:
        return 0
    
    # Use updated if available, otherwise created
    date_str = updated or created
    
    try:
        doc_date = datetime.strptime(str(date_str), "%Y-%m-%d")
        age_days = (datetime.now() - doc_date).days
        
        # Scoring: 100 for recent, decreasing with age
        if age_days <= 7:
            return 100
        elif age_days <= 30:
            return 90
        elif age_days <= 90:
            return 75
        elif age_days <= 180:
            return 60
        elif age_days <= 365:
            return 40
        else:
            return 20
    except ValueError:
        return 0


def calculate_linkage_score(
    front_matter: Dict,
    content: str,
    all_doc_ids: Set[str],
) -> int:
    """Calculate linkage score (0-100)."""
    score = 0
    
    # Check for related/supersedes links
    related = front_matter.get("related", [])
    supersedes = front_matter.get("supersedes", [])
    
    if related or supersedes:
        score += 30
    
    # Check for RFC dependencies
    if front_matter.get("doc_type") == "rfc":
        deps = front_matter.get("dependencies", {})
        if deps.get("rfcs"):
            score += 20
    
    # Check for markdown links to other docs
    doc_links = re.findall(r"\[([^\]]+)\]\(([^\)]+\.md)\)", content)
    if doc_links:
        score += 30
    
    # Check for references to other doc_ids in content
    for doc_id in all_doc_ids:
        if doc_id in content and doc_id != front_matter.get("doc_id"):
            score += 20
            break
    
    return min(score, 100)


def calculate_clarity_score(front_matter: Dict, content: str) -> int:
    """Calculate clarity score (0-100) based on summary and content quality."""
    score = 0
    
    # Summary quality
    summary = front_matter.get("summary", "")
    if summary:
        # Good length (50-200 chars)
        if 50 <= len(summary) <= 200:
            score += 30
        elif len(summary) > 0:
            score += 15
        
        # Contains key terms
        if any(term in summary.lower() for term in ["implement", "design", "architecture", "system"]):
            score += 10
    
    # Content structure
    if content:
        # Has headings
        headings = re.findall(r"^#+\s+(.+)$", content, re.MULTILINE)
        if len(headings) >= 3:
            score += 20
        elif headings:
            score += 10
        
        # Has code blocks
        code_blocks = re.findall(r"```", content)
        if len(code_blocks) >= 2:  # At least one complete block
            score += 15
        
        # Has lists
        lists = re.findall(r"^[\-\*]\s+", content, re.MULTILINE)
        if len(lists) >= 5:
            score += 15
        elif lists:
            score += 10
        
        # Reasonable length (not too short)
        if len(content) > 500:
            score += 10
    
    return min(score, 100)


def calculate_quality_score(
    doc_path: Path,
    front_matter: Dict,
    content: str,
    all_doc_ids: Set[str],
) -> Dict:
    """Calculate overall quality score and metrics."""
    completeness = calculate_completeness_score(front_matter)
    freshness = calculate_freshness_score(front_matter)
    linkage = calculate_linkage_score(front_matter, content, all_doc_ids)
    clarity = calculate_clarity_score(front_matter, content)
    
    # Overall score (weighted average)
    overall = int(
        completeness * 0.3 +
        freshness * 0.2 +
        linkage * 0.2 +
        clarity * 0.3
    )
    
    # Determine grade
    if overall >= QUALITY_THRESHOLDS["excellent"]:
        grade = "excellent"
    elif overall >= QUALITY_THRESHOLDS["good"]:
        grade = "good"
    elif overall >= QUALITY_THRESHOLDS["acceptable"]:
        grade = "acceptable"
    else:
        grade = "needs_improvement"
    
    return {
        "overall": overall,
        "grade": grade,
        "metrics": {
            "completeness": completeness,
            "freshness": freshness,
            "linkage": linkage,
            "clarity": clarity,
        }
    }


def load_document(file_path: Path) -> Optional[Tuple[Dict, str]]:
    """Load document front-matter and content."""
    try:
        with open(file_path, encoding="utf-8") as f:
            content = f.read()
        
        if not content.startswith("---"):
            return None
        
        parts = content.split("---", 2)
        if len(parts) < 3:
            return None
        
        front_matter = yaml.safe_load(parts[1])
        return front_matter, parts[2]
    except Exception:
        return None


def generate_quality_report(
    docs_dir: Path,
    output_file: Path,
    min_score: int = 0,
) -> None:
    """Generate quality report for all documents."""
    # Collect all documents
    documents = []
    all_doc_ids = set()
    
    for md_file in docs_dir.rglob("*.md"):
        # Skip excluded patterns
        rel_path = md_file.relative_to(docs_dir)
        if any(str(rel_path).startswith(pattern) for pattern in ["index/", "archive/"]):
            continue
        
        result = load_document(md_file)
        if result:
            front_matter, content = result
            doc_id = front_matter.get("doc_id")
            if doc_id:
                all_doc_ids.add(doc_id)
            
            documents.append({
                "path": md_file,
                "front_matter": front_matter,
                "content": content,
            })
    
    # Calculate quality scores
    quality_results = []
    for doc in documents:
        quality = calculate_quality_score(
            doc["path"],
            doc["front_matter"],
            doc["content"],
            all_doc_ids,
        )
        
        if quality["overall"] >= min_score:
            quality_results.append({
                "path": str(doc["path"].relative_to(docs_dir)),
                "doc_id": doc["front_matter"].get("doc_id", "N/A"),
                "title": doc["front_matter"].get("title", "Untitled"),
                "doc_type": doc["front_matter"].get("doc_type", "unknown"),
                "quality": quality,
            })
    
    # Sort by quality score
    quality_results.sort(key=lambda x: x["quality"]["overall"], reverse=True)
    
    # Generate markdown report
    report = []
    report.append("# Documentation Quality Report\n")
    report.append(f"*Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}*\n")
    
    # Summary statistics
    total = len(quality_results)
    by_grade = {}
    for result in quality_results:
        grade = result["quality"]["grade"]
        by_grade[grade] = by_grade.get(grade, 0) + 1
    
    avg_score = int(sum(r["quality"]["overall"] for r in quality_results) / total) if total > 0 else 0
    
    report.append("\n## Summary\n")
    report.append(f"- **Total Documents**: {total}\n")
    report.append(f"- **Average Quality Score**: {avg_score}/100\n")
    report.append(f"- **Excellent** (≥90): {by_grade.get('excellent', 0)}\n")
    report.append(f"- **Good** (≥75): {by_grade.get('good', 0)}\n")
    report.append(f"- **Acceptable** (≥60): {by_grade.get('acceptable', 0)}\n")
    report.append(f"- **Needs Improvement** (<60): {by_grade.get('needs_improvement', 0)}\n")
    
    # Top quality documents
    top_docs = quality_results[:10]
    if top_docs:
        report.append("\n## 🏆 Top Quality Documents\n")
        report.append("| Doc ID | Title | Score | Grade |\n")
        report.append("|--------|-------|-------|-------|\n")
        
        for doc in top_docs:
            report.append(
                f"| {doc['doc_id']} | {doc['title'][:50]} | "
                f"{doc['quality']['overall']}/100 | {doc['quality']['grade']} |\n"
            )
    
    # Documents needing improvement
    needs_improvement = [r for r in quality_results if r["quality"]["grade"] == "needs_improvement"]
    if needs_improvement:
        report.append(f"\n## ⚠️ Documents Needing Improvement ({len(needs_improvement)})\n")
        report.append("| Doc ID | Title | Score | Issues |\n")
        report.append("|--------|-------|-------|--------|\n")
        
        for doc in needs_improvement[:20]:
            metrics = doc["quality"]["metrics"]
            issues = []
            if metrics["completeness"] < 60:
                issues.append("incomplete")
            if metrics["freshness"] < 60:
                issues.append("stale")
            if metrics["linkage"] < 40:
                issues.append("isolated")
            if metrics["clarity"] < 60:
                issues.append("unclear")
            
            report.append(
                f"| {doc['doc_id']} | {doc['title'][:40]} | "
                f"{doc['quality']['overall']}/100 | {', '.join(issues)} |\n"
            )
    
    # Orphaned documents (low linkage)
    orphaned = [r for r in quality_results if r["quality"]["metrics"]["linkage"] < 20]
    if orphaned:
        report.append(f"\n## 🔗 Orphaned Documents ({len(orphaned)})\n")
        report.append("*Documents with few or no links to other documentation*\n\n")
        report.append("| Doc ID | Title | Linkage Score |\n")
        report.append("|--------|-------|---------------|\n")
        
        for doc in orphaned[:15]:
            report.append(
                f"| {doc['doc_id']} | {doc['title'][:50]} | "
                f"{doc['quality']['metrics']['linkage']}/100 |\n"
            )
    
    # Stale documents (low freshness)
    stale = [r for r in quality_results if r["quality"]["metrics"]["freshness"] < 40]
    if stale:
        report.append(f"\n## 📅 Stale Documents ({len(stale)})\n")
        report.append("*Documents not updated in 90+ days*\n\n")
        report.append("| Doc ID | Title | Freshness Score |\n")
        report.append("|--------|-------|------------------|\n")
        
        for doc in stale[:15]:
            report.append(
                f"| {doc['doc_id']} | {doc['title'][:50]} | "
                f"{doc['quality']['metrics']['freshness']}/100 |\n"
            )
    
    # Write report
    output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("".join(report))
    
    print(f"Quality report generated: {output_file}")
    print(f"  Total documents: {total}")
    print(f"  Average score: {avg_score}/100")
    print(f"  Needs improvement: {len(needs_improvement)}")


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Generate documentation quality report"
    )
    parser.add_argument(
        "--docs-dir",
        type=Path,
        help="Documentation directory (default: docs/)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Output file (default: docs/index/quality-report.md)",
    )
    parser.add_argument(
        "--min-score",
        type=int,
        default=0,
        help="Minimum quality score to include (default: 0)",
    )
    args = parser.parse_args()
    
    # Resolve paths
    repo_root = Path(__file__).parent.parent
    docs_dir = args.docs_dir or repo_root / "docs"
    output_file = args.output or docs_dir / "index" / "quality-report.md"
    
    if not docs_dir.exists():
        print(f"Error: Documentation directory not found: {docs_dir}")
        return 1
    
    print(f"Generating quality report for {docs_dir}...")
    generate_quality_report(docs_dir, output_file, args.min_score)
    
    return 0


if __name__ == "__main__":
    sys.exit(main())
