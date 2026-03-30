"""Organize source images into images/{structure}/{bloc}/ via symlinks.

Uses relative symlinks to avoid duplicating ~52 Mo of photos.
Generates wireframe images showing only climbing panel edges.
"""
from __future__ import annotations

import os
import sys
from pathlib import Path

import cv2

# Allow importing scene modules from this directory
sys.path.insert(0, str(Path(__file__).parent))

from scene.rendering.wireframe import draw_benchmark, draw_wireframe_thin
from scene.wireframe.silhouette import _build_edge_map

BASE = Path(__file__).parent
IMAGES_SRC = BASE / "images"
IMAGES_DST = BASE / "tagged_images"

# ── Bloc → structure mapping ────────────────────────────────────────

BLOC_TO_STRUCTURE = {
    # Island
    "Quinquin": "island",
    "Cyrano": "island",
    "Entre Deux": "island",
    "Oublié": "island",
    "Bloc à Pic": "island",
    "Tortue": "island",
    "Point and Clic": "island",
    "Dodécaèdre": "island",
    # Mur périphérique Nord
    "Vagues": "mur_peripherique_nord",
    "Starbloc Droit": "mur_peripherique_nord",
    "Starbloc Gauche": "mur_peripherique_nord",
    "Bloc de secours": "mur_peripherique_nord",
    "Petit'Oie": "mur_peripherique_nord",
    "Dolmen": "mur_peripherique_nord",
    "Tour de Piz": "mur_peripherique_nord",
    "Brouille Art": "mur_peripherique_nord",
    "Sablier": "mur_peripherique_nord",
    "Cubicube": "mur_peripherique_nord",
    "Grand Bleu": "mur_peripherique_nord",
    "Dalle": "mur_peripherique_nord",
    "Enclume": "mur_peripherique_nord",
    "Cheminée": "mur_peripherique_nord",
    "I-Luzion": "mur_peripherique_nord",
    "Lance-Pierre": "mur_peripherique_nord",
    # Mur périphérique Sud
    "Inception": "mur_peripherique_sud",
    "La Grille": "mur_peripherique_sud",
    "Taverne": "mur_peripherique_sud",
    "Pan Gullish": "mur_peripherique_sud",
    "The Last": "mur_peripherique_sud",
    "Aileron": "mur_peripherique_sud",
    # Spinnaker
    "Spinaker Nord": "spinnaker",
    "Spinaker Sud": "spinnaker",
}

BLOC_TO_SLUG = {
    "Quinquin": "quinquin",
    "Cyrano": "cyrano",
    "Entre Deux": "entre_deux",
    "Oublié": "oublie",
    "Bloc à Pic": "bloc_a_pic",
    "Tortue": "tortue",
    "Point and Clic": "point_and_clic",
    "Dodécaèdre": "dodecaedre",
    "Vagues": "vagues",
    "Starbloc Droit": "starbloc_droit",
    "Starbloc Gauche": "starbloc_gauche",
    "Bloc de secours": "bloc_de_secours",
    "Petit'Oie": "petitoie",
    "Dolmen": "dolmen",
    "Tour de Piz": "tour_de_piz",
    "Brouille Art": "brouille_art",
    "Sablier": "sablier",
    "Cubicube": "cubicube",
    "Grand Bleu": "grand_bleu",
    "Dalle": "dalle",
    "Enclume": "enclume",
    "Cheminée": "cheminee",
    "I-Luzion": "i_luzion",
    "Lance-Pierre": "lance_pierre",
    "Inception": "inception",
    "La Grille": "la_grille",
    "Taverne": "taverne",
    "Pan Gullish": "pan_gullish",
    "The Last": "the_last",
    "Aileron": "aileron",
    "Spinaker Nord": "spinaker_nord",
    "Spinaker Sud": "spinaker_sud",
}

# ── Image → bloc(s) classification ──────────────────────────────────
# Format: "image_NNN" → [list of bloc names]
# "skip" = recap / plan / restaurant / duplicate without assignment

IMAGE_CLASSIFICATION: dict[str, list[str]] = {
    # OCR-identified (sector labels → bloc mapping)
    "image_004": ["Pan Gullish"],
    "image_005": ["Quinquin", "Oublié"],  # Réacteur sector + merged from image_078
    "image_006": ["Taverne"],  # Groot sector
    "image_008": ["Vagues"],  # Rampes de Lancement sector
    "image_009": ["Oublié"],  # Réservoir sector
    "image_010": ["Inception", "Aileron"],  # Gravité sector + merged from image_086
    "image_011": ["Dalle", "Enclume"],  # Dalouse sector
    "image_013": ["Bloc de secours"],
    "image_014": ["Petit'Oie"],  # Star Wars (Gauche)
    "image_015": ["Aileron"],  # Galaxie sector
    "image_017": ["I-Luzion", "Tour de Piz", "Cheminée"],  # Dièdre sector + merged from image_077
    "image_018": ["Spinaker Nord"],  # X-Wing 1
    "image_023": ["Petit'Oie"],  # Star Wars
    "image_024": ["Spinaker Nord"],  # X-Wing
    "image_034": ["Spinaker Nord"],  # X-Wing 2
    "image_035": ["Quinquin"],  # Réacteur
    # Removed duplicates: 031, 058, 059, 066, 068, 075, 076, 077, 078, 079, 080, 081, 082, 083, 084, 085, 086, 088, 089

    # User-classified photos
    "image_001": ["Starbloc Droit", "Starbloc Gauche", "Petit'Oie"],
    "image_002": ["Quinquin", "Bloc à Pic", "Tortue", "Point and Clic"],
    "image_003": ["Tortue", "Enclume", "Cheminée", "I-Luzion", "Tour de Piz", "Cubicube", "Brouille Art"],
    "image_016": ["Cubicube", "Brouille Art", "Sablier"],
    "image_020": ["Tortue", "Dodécaèdre"],
    "image_021": ["Starbloc Droit"],
    "image_022": ["Tortue", "Point and Clic"],
    "image_025": ["Entre Deux"],  # merged from image_080
    "image_029": ["Tortue", "Dodécaèdre", "Point and Clic"],
    "image_030": ["Cyrano", "Entre Deux", "Dodécaèdre"],
    # image_031 removed (duplicate of image_030)
    "image_036": ["Quinquin"],
    "image_037": ["Inception"],
    "image_038": ["Aileron", "The Last"],
    "image_040": ["Dalle", "Enclume", "Cheminée", "I-Luzion", "Tour de Piz", "Cubicube", "Brouille Art", "Sablier"],
    "image_041": ["Tortue", "Dodécaèdre", "Point and Clic"],
    "image_042": ["Cyrano", "Tortue", "Dodécaèdre", "Point and Clic", "Vagues", "Starbloc Droit", "Taverne", "Pan Gullish", "Inception"],
    "image_043": ["Starbloc Droit"],
    "image_045": ["Spinaker Nord", "Point and Clic", "Inception", "Aileron"],
    "image_046": ["Vagues", "Starbloc Droit"],
    "image_047": ["Vagues"],
    "image_048": ["Starbloc Gauche", "Starbloc Droit"],
    "image_049": ["Bloc à Pic"],
    "image_050": ["Bloc à Pic"],
    "image_051": ["Petit'Oie"],
    "image_052": ["Dalle", "Enclume"],
    "image_053": ["Dalle", "Enclume"],
    "image_054": ["Dalle", "Enclume"],
    "image_055": ["Dalle", "Enclume"],
    "image_056": ["Vagues", "Dolmen"],
    "image_057": ["Bloc de secours"],  # restaurant view, Bloc de secours visible
    # image_059 removed (duplicate of image_001)
    "image_060": ["Petit'Oie", "Starbloc Gauche", "Cyrano", "Dodécaèdre", "Lance-Pierre"],
    "image_061": ["Vagues"],
    "image_062": ["Tortue", "Point and Clic", "Vagues", "Taverne", "Inception", "Aileron", "Spinaker Nord"],
    "image_064": ["Tortue", "Lance-Pierre", "Bloc de secours", "Sablier", "Starbloc Gauche", "Spinaker Sud"],
    "image_065": ["Cyrano", "Lance-Pierre", "Petit'Oie", "Starbloc Gauche", "Starbloc Droit"],
    # image_066 removed (duplicate of image_065)
    "image_067": ["Point and Clic", "Spinaker Nord", "Taverne", "Inception", "Aileron"],
    "image_069": ["Spinaker Nord", "Point and Clic", "Quinquin"],
    "image_070": ["Bloc de secours", "Lance-Pierre", "Tortue", "Point and Clic", "Vagues", "Starbloc Droit"],
    "image_071": ["Entre Deux", "Cyrano", "Dodécaèdre", "Pan Gullish", "La Grille"],
    "image_072": ["Taverne"],
    "image_073": ["Cyrano", "Dodécaèdre"],
    # image_077-083, 086 removed (duplicates merged into kept images)
    "image_087": ["Aileron"],

    # image_028 was skipped but its duplicate image_082 had blocs
    "image_028": ["Entre Deux", "Oublié"],  # merged from image_082

    # Skipped (recap / plan / restaurant / unassigned duplicates)
    # image_007, 012, 019, 026, 027, 032, 033, 039, 044, 063, 074
}


def find_image_files(stem: str) -> Path | None:
    """Find image file by stem (image_NNN) regardless of extension."""
    for ext in (".jpg", ".jpeg", ".png", ".webp"):
        normal = IMAGES_SRC / f"{stem}{ext}"
        cleaned = IMAGES_SRC / f"{stem}_clean{ext}"
        if normal.exists():
            if cleaned.exists():
                return normal, cleaned
            return normal, None
    return None, None


# ── Wireframe generation ────────────────────────────────────────────


def generate_wireframe(src: Path) -> Path:
    """Generate a silhouette wireframe image for a clean source photo."""
    stem = src.stem.replace("_clean", "_wireframe")
    dst = src.parent / f"{stem}.png"

    img = cv2.imread(str(src))
    if img is None:
        msg = f"Cannot read image: {src}"
        raise FileNotFoundError(msg)

    from scene.colors import build_wall_mask_hsv

    wall = build_wall_mask_hsv(img)
    edge_map = _build_edge_map(img, wall)

    canvas = draw_wireframe_thin(edge_map, wall, img.shape[:2])
    cv2.imwrite(str(dst), canvas)

    # Generate benchmark comparison if a manual reference exists
    ref_stem = src.stem.replace("_clean", "_wireframe_manuel")
    ref_path = src.parent / f"{ref_stem}.png"
    bench = draw_benchmark(img, edge_map, ref_path)
    if bench is not None:
        bench_stem = src.stem.replace("_clean", "_benchmark")
        bench_path = src.parent / f"{bench_stem}.png"
        cv2.imwrite(str(bench_path), bench)
        print(f"  benchmark: {bench_path.name}")

    return dst


def generate_all_wireframes() -> int:
    """Generate wireframe images for all *_clean.* images in IMAGES_SRC."""
    clean_files = sorted(IMAGES_SRC.glob("*_clean.*"))
    count = 0
    for clean_path in clean_files:
        wireframe_path = generate_wireframe(clean_path)
        print(f"  wireframe: {wireframe_path.name}")
        count += 1
    return count



def main() -> None:
    # Generate wireframe images from all clean sources
    print("=== Generating wireframe images ===")
    wf_count = generate_all_wireframes()
    print(f"  Generated {wf_count} wireframes\n")

    # Symlink images into tagged_images/{structure}/{bloc}/
    linked = 0
    skipped = 0
    missing = 0

    for stem, blocs in sorted(IMAGE_CLASSIFICATION.items()):
        normal, cleaned = find_image_files(stem)
        if normal is None:
            print(f"  MISSING: {stem}")
            missing += 1
            continue

        for bloc_name in blocs:
            structure_slug = BLOC_TO_STRUCTURE.get(bloc_name)
            bloc_slug = BLOC_TO_SLUG.get(bloc_name)
            if not structure_slug or not bloc_slug:
                print(f"  UNKNOWN BLOC: {bloc_name} (from {stem})")
                skipped += 1
                continue

            dst_dir = IMAGES_DST / structure_slug / bloc_slug
            dst_dir.mkdir(parents=True, exist_ok=True)
            dst = dst_dir / normal.name

            if dst.exists() or dst.is_symlink():
                dst.unlink()

            rel_target = os.path.relpath(normal, dst_dir)
            dst.symlink_to(rel_target)
            if cleaned:
                rel_target = os.path.relpath(cleaned, dst_dir)
                clean_dst = dst_dir / cleaned.name
                if clean_dst.exists() or clean_dst.is_symlink():
                    clean_dst.unlink()
                clean_dst.symlink_to(rel_target)

                # Also symlink wireframe if it exists
                wf_stem = cleaned.stem.replace("_clean", "_wireframe")
                wf_name = f"{wf_stem}.png"
                wf_src = IMAGES_SRC / wf_name
                if wf_src.exists():
                    wf_dst = dst_dir / wf_name
                    if wf_dst.exists() or wf_dst.is_symlink():
                        wf_dst.unlink()
                    wf_dst.symlink_to(os.path.relpath(wf_src, dst_dir))
            linked += 1

    print(f"\nLinked {linked} files, skipped {skipped}, missing {missing}")

    # Summary per bloc
    print("\n=== Images per bloc ===")
    bloc_counts: dict[str, int] = {}
    for blocs in IMAGE_CLASSIFICATION.values():
        for b in blocs:
            bloc_counts[b] = bloc_counts.get(b, 0) + 1
    for bloc, count in sorted(bloc_counts.items(), key=lambda x: -x[1]):
        print(f"  {bloc:25s} {count:3d} images")


if __name__ == "__main__":
    main()
