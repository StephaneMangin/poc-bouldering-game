"""Unified CLI for the scene reconstruction pipeline.

Usage:
    python -m scene extract           # floor plan → gym_blockout.json
    python -m scene analyze           # bloc analysis (photos, edges, plans)
    python -m scene wireframe IMG ... # panel detection on wall photos
    python -m scene classify DIR      # image classification via OCR
    python -m scene calibrate         # camera calibration from photos
"""
from __future__ import annotations

import argparse
import json
import logging
import sys
from pathlib import Path

import cv2

logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")
log = logging.getLogger(__name__)


# ── Extract (floor plan) ────────────────────────────────────────────


def cmd_extract(_args: argparse.Namespace) -> None:
    """Run the floor plan extraction pipeline."""
    from scene.constants import ANNOTATIONS_PATH, OUT_JSON, PNG_PATH, PREVIEW_PATH, RASTER_PATH, SCENE_DIR
    from scene.floorplan.builder import StructureBuilder
    from scene.floorplan.contours import ContourProcessor
    from scene.floorplan.crop import export_bloc_images
    from scene.floorplan.exporter import SceneExporter
    from scene.floorplan.markers import RedMarkerExtractor

    log.info("=== Extract scene from floor plan ===")

    color_img = cv2.imread(str(PNG_PATH))
    if color_img is None:
        raise RuntimeError(f"Cannot read {PNG_PATH}")

    markers = RedMarkerExtractor()
    dots, _red_labels, red_mask = markers.extract(color_img)
    gray = markers.strip_to_gray(color_img, red_mask)
    cv2.imwrite(str(RASTER_PATH), gray)
    log.info("Image: %dx%d", gray.shape[1], gray.shape[0])

    proc = ContourProcessor(gray.shape[:2])
    contours = proc.extract_contours(gray)
    contours_with_area = proc.approximate_and_filter(contours)
    contours_with_area = markers.snap_to_markers(contours_with_area, dots)

    origin = proc.compute_origin(contours_with_area)
    label_overrides = markers.match_labels_to_contours(_red_labels, contours_with_area)

    builder = StructureBuilder(proc)
    blocs = builder.build_blocs(contours_with_area, origin, label_overrides)
    structures = builder.build_structures(contours_with_area, blocs)

    exporter = SceneExporter()
    structural_elements = exporter.merge_annotations(structures, ANNOTATIONS_PATH)

    for struct in structures:
        bloc_names = ", ".join(b.label for b in struct.blocs)
        log.info(
            "Structure %-30s  %5.1f m²  %d blocs: [%s]",
            struct.label, struct.area_m2, len(struct.blocs), bloc_names,
        )

    output = exporter.build_json(structures, structural_elements)
    OUT_JSON.write_text(
        json.dumps(output, indent=2, ensure_ascii=False), encoding="utf-8",
    )
    log.info(
        "JSON → %s (%d structures, %d blocs)",
        OUT_JSON.name, len(structures), sum(len(s.blocs) for s in structures),
    )

    exporter.draw_preview(gray, contours_with_area, structures, blocs, PREVIEW_PATH)
    export_bloc_images(color_img, contours_with_area, structures, blocs, SCENE_DIR / "images")
    log.info("Done.")


# ── Analyze (bloc analysis) ─────────────────────────────────────────


def cmd_analyze(_args: argparse.Namespace) -> None:
    """Run bloc analysis: edge detection, architectural plans, reports."""
    from scene.analyze.edges import analyze_edges
    from scene.analyze.loader import load_blocs
    from scene.analyze.report import write_bloc_report
    from scene.constants import ANALYSIS_DIR, JSON_PATH
    from scene.rendering.architectural import draw_front_view, draw_side_view, draw_top_view
    from scene.rendering.contact_sheet import make_contact_sheet

    blocs = load_blocs()
    print(f"Chargé {len(blocs)} blocs depuis {JSON_PATH.name}")

    ANALYSIS_DIR.mkdir(parents=True, exist_ok=True)
    total_sheets = 0
    total_edges = 0
    total_plans = 0

    for bloc in blocs:
        out_dir = ANALYSIS_DIR / bloc.structure_slug / bloc.slug
        out_dir.mkdir(parents=True, exist_ok=True)

        cs = make_contact_sheet(bloc, out_dir)
        if cs:
            total_sheets += 1

        edge_stats = analyze_edges(bloc, out_dir)
        if edge_stats:
            total_edges += 1

        wall_angle = edge_stats.dominant_angle_deg if edge_stats else 90.0
        draw_top_view(bloc, out_dir)
        draw_front_view(bloc, out_dir, wall_angle)
        draw_side_view(bloc, out_dir, wall_angle)
        total_plans += 3

        write_bloc_report(bloc, out_dir, edge_stats)

        status = (
            f"  {bloc.label:20s} | "
            f"{len(bloc.photos):2d} photos | "
            f"angle={wall_angle:5.1f} deg | "
            f"type={edge_stats.wall_type if edge_stats else 'N/A':15s} | "
            f"{bloc.area_m2:5.1f} m2"
        )
        print(status)

    print(f"\n{'=' * 60}")
    print(f"Contact sheets: {total_sheets}")
    print(f"Edge analyses:  {total_edges}")
    print(f"Plans architecte: {total_plans} ({total_plans // 3} blocs x 3 vues)")
    print(f"Sortie: {ANALYSIS_DIR}/")


# ── Wireframe (panel detection) ──────────────────────────────────────


def cmd_wireframe(args: argparse.Namespace) -> None:
    """Detect panel polygons on climbing wall photos."""
    from scene.rendering.wireframe import save_outputs
    from scene.wireframe import detect_panels

    images_dir = Path(args.images_dir)
    out_dir = Path(args.output_dir) if args.output_dir else images_dir

    stems = args.images or ["image_001", "image_011", "image_042"]

    def find_image(stem: str) -> Path | None:
        for ext in (".jpg", ".jpeg", ".png", ".webp"):
            p = images_dir / f"{stem}{ext}"
            if p.exists():
                return p
        return None

    print(f"Processing {len(stems)} images\n")

    for stem in stems:
        img_path = find_image(stem)
        if not img_path:
            print(f"  NOT FOUND: {stem}")
            continue

        img = cv2.imread(str(img_path))
        if img is None:
            print(f"  SKIP (unreadable): {img_path.name}")
            continue

        result = detect_panels(img)
        paths = save_outputs(img, result, out_dir, img_path.stem)

        print(
            f"  {img_path.stem}: panels={len(result.panels)} "
            f"segments={len(result.segments)} "
            f"joints={len(result.joints)}",
        )
        for mode, path in paths.items():
            print(f"    → {path.name} ({mode})")

    print("\nDone.")


# ── Classify (image classification) ──────────────────────────────────


def cmd_classify(args: argparse.Namespace) -> None:
    """Classify images using OCR and visual cues."""
    from scene.classify_images import detect_island_visuals, find_matches, ocr_image

    target_dir = Path(args.directory)
    if not target_dir.is_dir():
        print(f"Not a directory: {target_dir}")
        sys.exit(1)

    images = sorted(
        f for f in target_dir.iterdir()
        if f.suffix.lower() in (".jpg", ".jpeg", ".png", ".webp")
    )
    print(f"Scanning {len(images)} images in {target_dir}\n")

    for img_path in images:
        text = ocr_image(img_path)
        matches = find_matches(text)
        is_island = detect_island_visuals(img_path)

        if matches or is_island:
            labels = ", ".join(matches) if matches else ""
            island = " [ISLAND]" if is_island else ""
            print(f"  {img_path.name}: {labels}{island}")


# ── Calibrate (camera calibration) ───────────────────────────────────


def cmd_calibrate(args: argparse.Namespace) -> None:
    """Estimate camera parameters from climbing gym photos."""
    from scene.calibration.lines import detect_lines
    from scene.calibration.pipeline import calibrate_batch, calibrate_image
    from scene.calibration.visualization import save_calibration_debug

    images_dir = Path(args.images_dir)
    out_dir = Path(args.output_dir) if args.output_dir else images_dir.parent / "calibration"
    out_dir.mkdir(parents=True, exist_ok=True)

    if args.images:
        # specific images
        extensions = (".jpg", ".jpeg", ".webp", ".png")
        files = []
        for stem in args.images:
            for ext in extensions:
                p = images_dir / f"{stem}{ext}"
                if p.exists():
                    files.append(p)
                    break
            else:
                print(f"  NOT FOUND: {stem}")
    else:
        files = sorted(
            f for f in images_dir.iterdir()
            if f.suffix.lower() in (".jpg", ".jpeg", ".webp", ".png")
            and not f.stem.endswith("_clean")
            and f.stem.startswith("image_")
        )

    print(f"Calibrating {len(files)} images\n")

    results = []
    for img_path in files:
        img = cv2.imread(str(img_path))
        if img is None:
            print(f"  SKIP (unreadable): {img_path.name}")
            continue

        result = calibrate_image(img, img_path.name)
        results.append(result)

        # debug visualization
        horizontal, vertical, angled = detect_lines(img)
        save_calibration_debug(img, result, horizontal, vertical, angled, out_dir)

        print(f"  {result.summary_line()}")

    # export JSON summary
    if results:
        summary = {"images": [r.to_dict() for r in results]}
        json_path = out_dir / "calibration_results.json"
        json_path.write_text(
            json.dumps(summary, indent=2, ensure_ascii=False), encoding="utf-8",
        )
        print(f"\nResults: {json_path}")
        print(f"Debug images: {out_dir}/")
        print(f"Total: {len(results)} images calibrated")


# ── Main entry point ─────────────────────────────────────────────────


def main() -> None:
    scene_dir = Path(__file__).resolve().parent.parent
    default_images_dir = str(scene_dir / "images")

    parser = argparse.ArgumentParser(
        prog="scene",
        description="Scene reconstruction pipeline for Block'Out Rennes.",
    )
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("extract", help="Floor plan → gym_blockout.json")
    sub.add_parser("analyze", help="Bloc analysis (photos, edges, plans)")

    wire = sub.add_parser("wireframe", help="Panel detection on wall photos")
    wire.add_argument("images", nargs="*", help="Image stems (e.g. image_001)")
    wire.add_argument("--images-dir", default=default_images_dir, help="Directory containing source images")
    wire.add_argument("--output-dir", default=None, help="Output directory (default: same as images-dir)")

    classify = sub.add_parser("classify", help="Image classification via OCR")
    classify.add_argument("directory", help="Directory to scan")

    calib = sub.add_parser("calibrate", help="Camera calibration from photos")
    calib.add_argument("images", nargs="*", help="Image stems (e.g. image_001)")
    calib.add_argument("--images-dir", default=default_images_dir, help="Directory containing source images")
    calib.add_argument("--output-dir", default=None, help="Output directory (default: ../calibration)")

    args = parser.parse_args()

    commands = {
        "extract": cmd_extract,
        "analyze": cmd_analyze,
        "wireframe": cmd_wireframe,
        "classify": cmd_classify,
        "calibrate": cmd_calibrate,
    }
    commands[args.command](args)


if __name__ == "__main__":
    main()
