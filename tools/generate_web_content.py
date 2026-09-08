"""Deterministic content tooling for the Murim browser build."""
from __future__ import annotations
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "web-client" / "wwwroot" / "data" / "content.json"
CONTENT = {
    "version": 2,
    "world": "Murim",
    "pillars": ["monde vivant", "arts martiaux", "relations", "commerce", "voyage", "survie", "information", "autonomie des PNJ", "evenements", "histoire emergente"],
    "starter_actions": ["observe", "enquête", "détecte les dangers", "cherche du travail", "travaille", "étudie", "patrouille", "médite", "dors"],
}
OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_text(json.dumps(CONTENT, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Generated {OUT}")
