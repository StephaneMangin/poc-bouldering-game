"""Scene export: JSON output, annotation merging, visual preview."""
from __future__ import annotations

import json
import logging
from pathlib import Path
from typing import Any

import cv2
import numpy as np

from scene.constants import BUILDING_DEPTH_M, BUILDING_WIDTH_M, CEILING_HEIGHT_M
from scene.models import Bloc, Structure

log = logging.getLogger(__name__)


class SceneExporter:
    """Produces JSON output and visual preview PNG."""

    COLORS = [
        (0, 200, 200),   (0, 120, 255),   (100, 100, 255),
        (200, 200, 0),   (0, 200, 0),     (200, 0, 200),
        (255, 200, 0),   (0, 255, 255),   (150, 100, 50),
        (100, 200, 255), (200, 150, 100), (50, 200, 150),
        (255, 100, 100),
    ]

    @staticmethod
    def merge_annotations(
        structures: list[Structure], annotations_path: Path,
    ) -> list[dict[str, Any]]:
        """Load sector_annotations.json and merge into bloc data."""
        if not annotations_path.exists():
            log.warning("No annotations file at %s — skipping", annotations_path)
            return []

        raw = json.loads(annotations_path.read_text(encoding="utf-8"))
        overrides = raw.get("structure_overrides", {})

        for struct in structures:
            for bloc in struct.blocs:
                if bloc.id not in overrides:
                    continue
                ov = overrides[bloc.id]
                if "label" in ov:
                    bloc.label = ov["label"]
                if "height_m" in ov:
                    bloc.height_m = ov["height_m"]
                if "faces" in ov:
                    bloc.faces = ov["faces"]
                if "features" in ov:
                    bloc.features = ov["features"]

        elements: list[dict[str, Any]] = raw.get("structural_elements", [])
        log.info("Merged annotations: %d structural elements", len(elements))
        return elements

    @staticmethod
    def build_json(
        structures: list[Structure],
        structural_elements: list[dict[str, Any]] | None = None,
    ) -> dict[str, Any]:
        """Assemble the full JSON output."""
        all_blocs = [b for s in structures for b in s.blocs]
        return {
            "meta": {
                "name": "Block'Out Rennes",
                "version": "2.0",
                "source": "original_floor_plan.png",
                "coordinate_system": {
                    "origin": "center_island",
                    "x": "east", "z": "north", "y": "up",
                },
                "building_envelope": {
                    "width_m": BUILDING_WIDTH_M,
                    "depth_m": BUILDING_DEPTH_M,
                    "ceiling_height_m": CEILING_HEIGHT_M,
                },
                "scale_px_per_m": {"x": 175.0, "z": 152.6},
            },
            "structures": [s.to_dict() for s in structures],
            "structural_elements": structural_elements or [],
            "sectors": [
                {
                    "id": b.id,
                    "height_m": b.height_m,
                    "vertices": [v.to_dict() for v in b.footprint_xz],
                }
                for b in all_blocs
            ],
        }

    def draw_preview(
        self,
        gray: np.ndarray,
        contours_with_area: list[tuple[np.ndarray, float]],
        structures: list[Structure],
        blocs: list[Bloc],
        out_path: Path,
    ) -> None:
        """Draw polygons colored by parent structure over the raster."""
        canvas = cv2.cvtColor(gray, cv2.COLOR_GRAY2BGR)

        bloc_color: dict[str, int] = {}
        for struct_idx, struct in enumerate(structures):
            for bloc in struct.blocs:
                bloc_color[bloc.id] = struct_idx

        for i, (approx, _) in enumerate(contours_with_area):
            bloc = blocs[i]
            color_idx = bloc_color.get(bloc.id, i)
            color = self.COLORS[color_idx % len(self.COLORS)]
            cv2.drawContours(canvas, [approx], -1, color, 3)

            pts = approx.reshape(-1, 2)
            for pt in pts:
                cv2.circle(canvas, (int(pt[0]), int(pt[1])), 8, color, -1)

            cx = int(np.mean(pts[:, 0]))
            cy = int(np.mean(pts[:, 1]))
            cv2.putText(
                canvas,
                f"{bloc.label} ({bloc.area_m2:.0f}m2)",
                (cx - 100, cy),
                cv2.FONT_HERSHEY_SIMPLEX, 0.8, color, 2,
            )

        cv2.imwrite(str(out_path), canvas)
        log.info("Preview → %s", out_path.name)
