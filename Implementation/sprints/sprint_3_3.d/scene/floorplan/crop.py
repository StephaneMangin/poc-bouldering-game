"""Crop bloc images from the floor plan and organize into folder tree."""
from __future__ import annotations

import logging
import shutil
from pathlib import Path

import cv2
import numpy as np

from scene.models import Bloc, Structure
from scene.utils import slugify

log = logging.getLogger(__name__)

PADDING_PX = 40


def crop_bloc_image(
    color_img: np.ndarray,
    contour: np.ndarray,
) -> np.ndarray:
    """Crop the bounding box of a contour (with padding) from the image."""
    h, w = color_img.shape[:2]
    x, y, bw, bh = cv2.boundingRect(contour)

    x1 = max(0, x - PADDING_PX)
    y1 = max(0, y - PADDING_PX)
    x2 = min(w, x + bw + PADDING_PX)
    y2 = min(h, y + bh + PADDING_PX)

    return color_img[y1:y2, x1:x2].copy()


def crop_structure_image(
    color_img: np.ndarray,
    contours: list[np.ndarray],
) -> np.ndarray:
    """Crop the combined bounding box of all contours in a structure."""
    h, w = color_img.shape[:2]
    all_pts = np.vstack(contours)
    x, y, bw, bh = cv2.boundingRect(all_pts)

    x1 = max(0, x - PADDING_PX)
    y1 = max(0, y - PADDING_PX)
    x2 = min(w, x + bw + PADDING_PX)
    y2 = min(h, y + bh + PADDING_PX)

    return color_img[y1:y2, x1:x2].copy()


def export_bloc_images(
    color_img: np.ndarray,
    contours_with_area: list[tuple[np.ndarray, float]],
    structures: list[Structure],
    blocs: list[Bloc],
    output_dir: Path,
) -> None:
    """Export cropped images into images/{structure}/{bloc}/ tree.

    - Each bloc gets its own crop from the floor plan.
    - Each structure gets a full crop placed in every child bloc folder.
    """
    if output_dir.exists():
        shutil.rmtree(output_dir)

    # Map bloc id → contour index
    bloc_contour_idx: dict[str, int] = {
        blocs[i].id: i for i in range(len(blocs))
    }

    for struct in structures:
        struct_slug = slugify(struct.label)
        struct_dir = output_dir / struct_slug
        struct_dir.mkdir(parents=True, exist_ok=True)

        # Collect contour indices for this structure
        struct_contour_indices = [
            bloc_contour_idx[b.id] for b in struct.blocs
        ]
        struct_contours = [
            contours_with_area[i][0] for i in struct_contour_indices
        ]

        # Structure-level crop (one file at structure root)
        struct_img = crop_structure_image(color_img, struct_contours)
        cv2.imwrite(str(struct_dir / f"{struct_slug}.png"), struct_img)

        for bloc in struct.blocs:
            bloc_slug = slugify(bloc.label)
            bloc_dir = struct_dir / bloc_slug
            bloc_dir.mkdir(parents=True, exist_ok=True)

            # Individual bloc crop
            idx = bloc_contour_idx[bloc.id]
            contour = contours_with_area[idx][0]
            bloc_img = crop_bloc_image(color_img, contour)
            cv2.imwrite(str(bloc_dir / f"{bloc_slug}.png"), bloc_img)

    total = sum(len(s.blocs) for s in structures)
    log.info(
        "Exported images → %s/ (%d structures, %d bloc folders)",
        output_dir.name, len(structures), total,
    )
