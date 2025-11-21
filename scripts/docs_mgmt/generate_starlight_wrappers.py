#!/usr/bin/env python3
"""Generate Starlight wrapper MDX files from docs/index/registry.json.

For each canonical document in the registry, this script creates a
wrapper MDX file under:

    docs-site/src/content/docs/<doc_type>/<doc_id>.mdx

Each wrapper delegates rendering to the ExternalDoc component, which
loads and renders the original markdown content from docs/.
"""

import json
from pathlib import Path


def load_registry(registry_path: Path) -> dict:
  data = json.loads(registry_path.read_text(encoding="utf-8"))
  if not isinstance(data, dict):
    raise ValueError("Registry JSON must be an object at the top level")
  return data


def escape_yaml(value: str) -> str:
  """Escape a string for use in double-quoted YAML scalars."""
  return value.replace("\\", "\\\\").replace("\"", "\\\"")


def main() -> int:
  repo_root = Path(__file__).resolve().parents[2]
  docs_dir = repo_root / "docs"
  registry_path = docs_dir / "index" / "registry.json"

  if not registry_path.exists():
    raise SystemExit(f"Registry not found: {registry_path}")

  docs_site_dir = repo_root / "docs-site"
  content_root = docs_site_dir / "src" / "content" / "docs"

  registry = load_registry(registry_path)
  docs = registry.get("docs", [])

  generated = 0

  for doc in docs:
    if not doc.get("canonical"):
      continue

    doc_type = str(doc.get("doc_type", "other")).lower()
    doc_id = doc.get("doc_id")
    title = (doc.get("title") or doc_id or "Untitled").strip()
    summary = (doc.get("summary") or title).replace("\n", " ").strip()

    if not doc_id:
      # Skip malformed entries
      continue

    # Group wrappers by doc_type
    target_dir = content_root / doc_type
    target_dir.mkdir(parents=True, exist_ok=True)

    target_path = target_dir / f"{doc_id}.mdx"

    rel_import = "../../../components/ExternalDoc.astro"

    frontmatter = (
      "---\n"
      f"title: \"{escape_yaml(title)}\"\n"
      f"description: \"{escape_yaml(summary)}\"\n"
      "sidebar:\n"
      f"  label: \"{escape_yaml(doc_id)}\"\n"
      "---\n\n"
    )

    body = (
      f"import ExternalDoc from '{rel_import}';\n\n"
      f"<ExternalDoc docId=\"{escape_yaml(doc_id)}\" />\n"
    )

    target_path.write_text(frontmatter + body, encoding="utf-8")
    generated += 1

  print(f"Generated {generated} Starlight wrapper docs in {content_root}")
  return 0


if __name__ == "__main__":  # pragma: no cover
  raise SystemExit(main())
