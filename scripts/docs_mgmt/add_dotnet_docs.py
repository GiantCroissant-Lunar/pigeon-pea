#!/usr/bin/env python3
"""
Simple script to add dotnet documentation to the registry.
"""

import hashlib
import json
from datetime import datetime
from pathlib import Path


def calculate_simhash(content):
    """Calculate a simple simhash for content."""
    return str(hashlib.md5(content.encode()).hexdigest())


def get_dotnet_docs():
    """Get all dotnet documentation files."""
    dotnet_dir = Path("docs/dotnet")
    docs = []

    # Architecture documents
    arch_dir = dotnet_dir / "architecture"
    if arch_dir.exists():
        for file_path in arch_dir.glob("*.md"):
            if file_path.name != "README.md":  # Skip if it's the main README
                with open(file_path, "r", encoding="utf-8") as f:
                    content = f.read()
                    # Extract frontmatter
                    if content.startswith("---"):
                        try:
                            frontmatter_end = content.find("---", 3)
                            if frontmatter_end != -1:
                                frontmatter = content[: frontmatter_end + 3]
                                body = content[frontmatter_end + 3 :]

                                # Parse YAML frontmatter
                                import yaml

                                try:
                                    metadata = yaml.safe_load(frontmatter)
                                    doc_id = metadata.get("doc_id", "")
                                    title = metadata.get("title", "")
                                    doc_type = metadata.get("doc_type", "")
                                    status = metadata.get("status", "")
                                    canonical = metadata.get("canonical", False)
                                    created = metadata.get("created", "")
                                    tags = metadata.get("tags", [])
                                    related = metadata.get("related", [])
                                    summary = metadata.get("summary", "")

                                    if doc_id and title:
                                        docs.append(
                                            {
                                                "path": str(
                                                    file_path.absolute()
                                                ).replace("\\", "/"),
                                                "sha256": hashlib.sha256(
                                                    content.encode()
                                                ).hexdigest(),
                                                "doc_id": doc_id,
                                                "title": title,
                                                "doc_type": doc_type,
                                                "status": status,
                                                "canonical": canonical,
                                                "created": created,
                                                "tags": tags,
                                                "summary": summary,
                                                "supersedes": [],
                                                "related": related,
                                                "simhash": calculate_simhash(body),
                                            }
                                        )
                                except yaml.YAMLError:
                                    continue
                        except Exception:
                            continue

    # Guide documents
    guides_dir = dotnet_dir / "guides"
    if guides_dir.exists():
        for file_path in guides_dir.glob("*.md"):
            with open(file_path, "r", encoding="utf-8") as f:
                content = f.read()
                if content.startswith("---"):
                    try:
                        frontmatter_end = content.find("---", 3)
                        if frontmatter_end != -1:
                            frontmatter = content[: frontmatter_end + 3]
                            body = content[frontmatter_end + 3 :]

                            import yaml

                            try:
                                metadata = yaml.safe_load(frontmatter)
                                doc_id = metadata.get("doc_id", "")
                                title = metadata.get("title", "")
                                doc_type = metadata.get("doc_type", "")
                                status = metadata.get("status", "")
                                canonical = metadata.get("canonical", False)
                                created = metadata.get("created", "")
                                tags = metadata.get("tags", [])
                                related = metadata.get("related", [])
                                summary = metadata.get("summary", "")

                                if doc_id and title:
                                    docs.append(
                                        {
                                            "path": str(file_path.absolute()).replace(
                                                "\\", "/"
                                            ),
                                            "sha256": hashlib.sha256(
                                                content.encode()
                                            ).hexdigest(),
                                            "doc_id": doc_id,
                                            "title": title,
                                            "doc_type": doc_type,
                                            "status": status,
                                            "canonical": canonical,
                                            "created": created,
                                            "tags": tags,
                                            "summary": summary,
                                            "supersedes": [],
                                            "related": related,
                                            "simhash": calculate_simhash(body),
                                        }
                                    )
                            except yaml.YAMLError:
                                continue
                    except Exception:
                        continue

    return docs


def update_registry():
    """Update the registry with dotnet documentation."""
    registry_path = Path("docs/index/registry.json")

    # Ensure directory exists
    registry_path.parent.mkdir(parents=True, exist_ok=True)

    # Read existing registry
    if registry_path.exists():
        with open(registry_path, "r", encoding="utf-8") as f:
            registry = json.load(f)
    else:
        registry = {
            "generated_at": "",
            "total_docs": 0,
            "by_type": {},
            "by_status": {},
            "docs": [],
        }

    # Get dotnet docs
    dotnet_docs = get_dotnet_docs()

    # Add new docs to registry
    existing_doc_ids = {doc["doc_id"] for doc in registry["docs"]}

    for doc in dotnet_docs:
        if doc["doc_id"] not in existing_doc_ids:
            registry["docs"].append(doc)

    # Update counts
    registry["total_docs"] = len(registry["docs"])

    # Update by_type counts
    registry["by_type"] = {}
    for doc in registry["docs"]:
        doc_type = doc["doc_type"]
        registry["by_type"][doc_type] = registry["by_type"].get(doc_type, 0) + 1

    # Update by_status counts
    registry["by_status"] = {}
    for doc in registry["docs"]:
        status = doc["status"]
        registry["by_status"][status] = registry["by_status"].get(status, 0) + 1

    # Update generated_at timestamp
    registry["generated_at"] = datetime.datetime.now(datetime.timezone.utc).isoformat()

    # Write updated registry
    with open(registry_path, "w", encoding="utf-8") as f:
        json.dump(registry, f, indent=2)

    print(f"Updated registry with {len(dotnet_docs)} new dotnet documents")
    print(f"Total docs: {registry['total_docs']}")
    print(f"By type: {registry['by_type']}")
    print(f"By status: {registry['by_status']}")


if __name__ == "__main__":
    update_registry()
