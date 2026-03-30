"""CLI entry point: python -m scene.floorplan."""
from __future__ import annotations

import json
import logging

import cv2

from scene.constants import ANNOTATIONS_PATH, OUT_JSON, PNG_PATH, PREVIEW_PATH, RASTER_PATH, SCENE_DIR

from .builder import StructureBuilder
from .contours import ContourProcessor
from .crop import export_bloc_images
from .exporter import SceneExporter
from .markers import RedMarkerExtractor

logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")
log = logging.getLogger(__name__)


def main() -> None:
    log.info("=== Extract scene from floor plan ===")

    color_img = cv2.imread(str(PNG_PATH))
    if color_img is None:
        raise RuntimeError(f"Cannot read {PNG_PATH}")

    # 1. Red markers
    markers = RedMarkerExtractor()
    dots, red_labels, red_mask = markers.extract(color_img)

    # 2. Clean grayscale
    gray = markers.strip_to_gray(color_img, red_mask)
    cv2.imwrite(str(RASTER_PATH), gray)
    log.info("Image: %dx%d", gray.shape[1], gray.shape[0])

    # 3. Contour extraction
    proc = ContourProcessor(gray.shape[:2])
    contours = proc.extract_contours(gray)
    contours_with_area = proc.approximate_and_filter(contours)
    contours_with_area = markers.snap_to_markers(contours_with_area, dots)

    # 4. Coordinate system
    origin = proc.compute_origin(contours_with_area)
    label_overrides = markers.match_labels_to_contours(red_labels, contours_with_area)

    # 5. Build blocs → structures
    builder = StructureBuilder(proc)
    blocs = builder.build_blocs(contours_with_area, origin, label_overrides)
    structures = builder.build_structures(contours_with_area, blocs)

    # 6. Export
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

    # 7. Crop bloc images
    export_bloc_images(color_img, contours_with_area, structures, blocs, SCENE_DIR / "images")

    log.info("Done.")


if __name__ == "__main__":
    main()
