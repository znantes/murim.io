from dataclasses import dataclass, asdict
import json
from pathlib import Path

@dataclass(frozen=True)
class CareerChoice:
    id: str
    title: str
    description: str
    consequence: str
    reputation: int
    money: float
    fans: int
    xp: int

CHOICES = [
    CareerChoice("risk", "Accepter le duel", "Affronter le vétéran qui te provoque.", "Risque élevé · réputation +", 3, 0.0, 180, 45),
    CareerChoice("mentor", "Suivre le maître", "Sacrifier du temps libre pour progresser.", "GEN +2 · fans stables", 1, 0.0, 20, 90),
    CareerChoice("money", "Signer maintenant", "Choisir la sécurité financière.", "Argent +0.8 M€ · potentiel -1", 0, 0.8, 35, 35),
]


def build_catalog():
    return {
        "title": "Murim Legends",
        "pillars": [
            "carrière saison par saison", "matchs interactifs", "entraînement", "choix narratifs",
            "réputation", "fans", "fortune", "relations", "arts martiaux", "exploration du monde",
            "mémoire fiable", "autonomie des PNJ", "économie", "survie", "événements dynamiques"
        ],
        "choices": [asdict(x) for x in CHOICES],
    }


def main():
    out = Path(__file__).with_name("web_catalog.json")
    out.write_text(json.dumps(build_catalog(), ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"generated {out}")


if __name__ == "__main__":
    main()
