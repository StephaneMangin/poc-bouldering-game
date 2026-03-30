"""Scan images for sector/bloc names via OCR and visual cues."""
from __future__ import annotations

import json
from pathlib import Path

import cv2

try:
    import pytesseract
    HAS_OCR = True
except ImportError:
    HAS_OCR = False

IMAGES_DIR = Path(__file__).parent.parent / "images"

# Known bloc names in the gym (lowercase for matching)
KNOWN_BLOCS = [
    "pan güllich", "pan gullich", "pan gullish",
    "réacteur", "reacteur",
    "je s'appelle groot", "groot",
    "rampes de lancement", "rampe de lancement",
    "réservoir", "reservoir",
    "gravité", "gravite",
    "dalouse",
    "bloc de secours",
    "star wars", "starbloc",
    "galaxie",
    "big bang",
    "dièdre", "diedre",
    "x-wing", "xwing",
    "vagues",
    "tour de piz",
    "cubicube",
    "brouille art", "brouillart",
    "sablier",
    "petit'oie", "petitoie", "petite oie",
    "i-luzion", "iluzion",
    "grand bleu",
    "cheminée", "cheminee",
    "lance-pierre", "lance pierre",
    "enclume",
    "dalle",
    "dolmen",
    "tortue",
    "dodécaèdre", "dodecaedre",
    "point and clic",
    "cyrano",
    "bloc à pic", "bloc a pic",
    "entre deux",
    "quinquin",
    "oublié", "oublie",
    "la grille",
    "spinaker", "spinnaker",
    "inception",
    "aileron",
    "taverne",
    "the last",
    "island",
    # Structure-level
    "mur nord", "mur sud", "périphérique",
    # Generic markers
    "nouveau secteur", "recap", "ouverture",
    "block'out", "blockout", "block out",
]


def ocr_image(path: Path) -> str:
    """Run OCR on an image, return lowercased text."""
    if not HAS_OCR:
        return ""
    img = cv2.imread(str(path))
    if img is None:
        return ""
    # Focus on bottom third where labels usually appear
    h = img.shape[0]
    bottom = img[h * 2 // 3:, :]
    gray = cv2.cvtColor(bottom, cv2.COLOR_BGR2GRAY)
    text_bottom = pytesseract.image_to_string(gray, lang="fra", config="--psm 6").strip().lower()

    # Also scan full image for broader context
    gray_full = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    text_full = pytesseract.image_to_string(gray_full, lang="fra", config="--psm 6").strip().lower()

    return text_bottom + "\n" + text_full


def find_matches(text: str) -> list[str]:
    """Find known bloc/structure names in OCR text."""
    matches = []
    for name in KNOWN_BLOCS:
        if name in text:
            matches.append(name)
    return matches


def detect_island_visuals(path: Path) -> bool:
    """Detect if image shows Island structure (teal + orange curved blob)."""
    img = cv2.imread(str(path))
    if img is None:
        return False
    hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)

    # Teal: H~85-100, high S
    teal_mask = cv2.inRange(hsv, (75, 80, 60), (100, 255, 255))
    # Orange-red: H~5-20, high S
    orange_mask = cv2.inRange(hsv, (5, 100, 100), (20, 255, 255))

    total = img.shape[0] * img.shape[1]
    teal_pct = cv2.countNonZero(teal_mask) / total * 100
    orange_pct = cv2.countNonZero(orange_mask) / total * 100

    return teal_pct > 8 and orange_pct > 8


def main() -> None:
    results: dict[str, dict] = {}

    for path in sorted(IMAGES_DIR.iterdir()):
        if path.suffix.lower() not in {".jpg", ".jpeg", ".png", ".webp"}:
            continue

        entry: dict = {"file": path.name, "ocr_matches": [], "has_island_colors": False}

        # OCR
        text = ocr_image(path)
        matches = find_matches(text)
        entry["ocr_matches"] = matches
        if text.strip():
            # Keep first 200 chars for debugging
            entry["ocr_excerpt"] = text[:200].replace("\n", " | ")

        # Color analysis
        entry["has_island_colors"] = detect_island_visuals(path)

        results[path.name] = entry
        status = "OCR:" + ",".join(matches) if matches else "no-match"
        island = " [ISLAND-colors]" if entry["has_island_colors"] else ""
        print(f"{path.name:60s} → {status}{island}")

    out = IMAGES_DIR.parent / "scene" / "image_classification.json"
    out.write_text(json.dumps(results, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"\nWrote {out}")


if __name__ == "__main__":
    main()
