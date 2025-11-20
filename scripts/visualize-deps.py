#!/usr/bin/env python3
"""
Generate dependency visualization diagrams.

Creates Mermaid diagrams showing RFC dependencies, implementation status,
and document relationships.
"""
import argparse
import json
import sys
from collections import defaultdict
from pathlib import Path
from typing import Dict, List, Optional, Set

import yaml


def load_document(file_path: Path) -> Optional[Dict]:
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
        return {"path": file_path, "front_matter": front_matter}
    except Exception:
        return None


def collect_rfcs(docs_dir: Path) -> List[Dict]:
    """Collect all RFC documents."""
    rfcs = []
    rfcs_dir = docs_dir / "rfcs"
    
    if not rfcs_dir.exists():
        return rfcs
    
    for rfc_file in rfcs_dir.glob("*.md"):
        if rfc_file.name == "README.md":
            continue
        
        doc = load_document(rfc_file)
        if doc and doc["front_matter"].get("doc_type") == "rfc":
            rfcs.append(doc)
    
    return rfcs


def generate_dependency_graph(rfcs: List[Dict]) -> str:
    """Generate Mermaid dependency graph."""
    graph = []
    graph.append("```mermaid\n")
    graph.append("graph TD\n")
    
    # Add nodes with styling based on status
    for rfc in rfcs:
        fm = rfc["front_matter"]
        doc_id = fm.get("doc_id", "N/A")
        title = fm.get("title", "Untitled")[:30]  # Truncate
        status = fm.get("status", "draft")
        
        # Node styling
        if status == "active":
            graph.append(f"    {doc_id}[\"{doc_id}<br/>{title}\"]:::active\n")
        elif status == "draft":
            graph.append(f"    {doc_id}[\"{doc_id}<br/>{title}\"]:::draft\n")
        else:
            graph.append(f"    {doc_id}[\"{doc_id}<br/>{title}\"]\n")
    
    # Add dependency edges
    for rfc in rfcs:
        fm = rfc["front_matter"]
        doc_id = fm.get("doc_id")
        deps = fm.get("dependencies", {})
        
        for dep_id in deps.get("rfcs", []):
            graph.append(f"    {dep_id} --> {doc_id}\n")
    
    # Add related edges (dotted)
    for rfc in rfcs:
        fm = rfc["front_matter"]
        doc_id = fm.get("doc_id")
        
        for related_id in fm.get("related", []):
            # Only add if both are RFCs
            if any(r["front_matter"].get("doc_id") == related_id for r in rfcs):
                graph.append(f"    {doc_id} -.-> {related_id}\n")
    
    # Styling
    graph.append("\n    classDef active fill:#90EE90,stroke:#006400,stroke-width:2px\n")
    graph.append("    classDef draft fill:#FFE4B5,stroke:#FFA500,stroke-width:1px\n")
    graph.append("```\n")
    
    return "".join(graph)


def generate_implementation_flowchart(rfcs: List[Dict]) -> str:
    """Generate implementation status flowchart."""
    flowchart = []
    flowchart.append("```mermaid\n")
    flowchart.append("flowchart LR\n")
    
    # Group by implementation status
    by_status = defaultdict(list)
    for rfc in rfcs:
        impl = rfc["front_matter"].get("implementation", {})
        status = impl.get("status", "not-started")
        by_status[status].append(rfc)
    
    # Create status nodes
    statuses = ["not-started", "in-progress", "blocked", "completed"]
    status_labels = {
        "not-started": "Not Started",
        "in-progress": "In Progress",
        "blocked": "Blocked",
        "completed": "Completed",
    }
    
    for status in statuses:
        if status in by_status:
            count = len(by_status[status])
            label = status_labels.get(status, status)
            flowchart.append(f"    {status}[\"{label}<br/>({count} RFCs)\"]:::{status}\n")
    
    # Add flow
    flowchart.append("\n    not-started --> in-progress\n")
    flowchart.append("    in-progress --> completed\n")
    flowchart.append("    in-progress --> blocked\n")
    flowchart.append("    blocked --> in-progress\n")
    
    # Styling
    flowchart.append("\n    classDef not-started fill:#E0E0E0,stroke:#808080\n")
    flowchart.append("    classDef in-progress fill:#87CEEB,stroke:#4682B4,stroke-width:2px\n")
    flowchart.append("    classDef blocked fill:#FFB6C1,stroke:#DC143C,stroke-width:2px\n")
    flowchart.append("    classDef completed fill:#90EE90,stroke:#006400,stroke-width:2px\n")
    flowchart.append("```\n")
    
    return "".join(flowchart)


def generate_relationship_map(documents: List[Dict]) -> str:
    """Generate document relationship map."""
    graph = []
    graph.append("```mermaid\n")
    graph.append("graph LR\n")
    
    # Group by doc_type
    by_type = defaultdict(list)
    for doc in documents:
        doc_type = doc["front_matter"].get("doc_type", "unknown")
        by_type[doc_type].append(doc)
    
    # Create type clusters
    for doc_type, docs in by_type.items():
        graph.append(f"\n    subgraph {doc_type.upper()}\n")
        
        for doc in docs[:5]:  # Limit to 5 per type
            doc_id = doc["front_matter"].get("doc_id", "N/A")
            title = doc["front_matter"].get("title", "Untitled")[:20]
            graph.append(f"        {doc_id}[\"{title}\"]\n")
        
        graph.append("    end\n")
    
    # Add cross-references
    for doc in documents:
        doc_id = doc["front_matter"].get("doc_id")
        if not doc_id:
            continue
        
        for related_id in doc["front_matter"].get("related", []):
            graph.append(f"    {doc_id} --> {related_id}\n")
    
    graph.append("```\n")
    
    return "".join(graph)


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Generate dependency visualization diagrams"
    )
    parser.add_argument(
        "--docs-dir",
        type=Path,
        help="Documentation directory (default: docs/)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Output file (default: docs/DEPENDENCIES.md)",
    )
    args = parser.parse_args()
    
    # Resolve paths
    repo_root = Path(__file__).parent.parent
    docs_dir = args.docs_dir or repo_root / "docs"
    output_file = args.output or docs_dir / "DEPENDENCIES.md"
    
    if not docs_dir.exists():
        print(f"Error: Documentation directory not found: {docs_dir}")
        return 1
    
    print(f"Generating dependency visualizations from {docs_dir}...")
    
    # Collect RFCs
    rfcs = collect_rfcs(docs_dir)
    print(f"Found {len(rfcs)} RFCs")
    
    # Generate visualizations
    content = []
    content.append("# Documentation Dependencies\n")
    content.append(f"*Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}*\n")
    
    # RFC dependency graph
    rfcs_with_deps = [r for r in rfcs if r["front_matter"].get("dependencies", {}).get("rfcs")]
    if rfcs_with_deps:
        content.append("\n## RFC Dependency Graph\n")
        content.append("*Shows which RFCs depend on others*\n\n")
        content.append(generate_dependency_graph(rfcs))
    
    # Implementation status flowchart
    content.append("\n## Implementation Status Flow\n")
    content.append("*Current distribution of RFCs by implementation status*\n\n")
    content.append(generate_implementation_flowchart(rfcs))
    
    # Write output
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("".join(content))
    
    print(f"Dependencies visualization generated: {output_file}")
    
    return 0


if __name__ == "__main__":
    from datetime import datetime
    sys.exit(main())
