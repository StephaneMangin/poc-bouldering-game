"""Wireframe detection pipeline.

Full pipeline: image → K-means segmentation → wall-cluster filter →
contour extraction → polygon approximation → WireframeResult.

Usage::

    from scene.wireframe import detect_panels

    result = detect_panels(img)
"""
from __future__ import annotations

from typing import TYPE_CHECKING

import cv2

if TYPE_CHECKING:
    import numpy as np

from scene.colors import build_wall_mask_hsv
from scene.models import WireframeResult

from .panels import deduplicate_segments, extract_joints, extract_panels, panels_to_segments
from .segmentation import build_kmeans_labels, find_wall_clusters

# Minimum fraction of panel polygon area that must overlap with wall mask
_WALL_OVERLAP_MIN = 0.40


def _filter_wall_panels(
    panels: "list",
    wall_mask: "np.ndarray",
) -> "list":
    """Keep panels whose polygon significantly overlaps the wall mask."""
    import numpy as np

    h, w = wall_mask.shape[:2]
    kept = []
    for panel in panels:
        poly_mask = np.zeros((h, w), dtype=np.uint8)
        cv2.fillPoly(poly_mask, [panel.polygon], 255)
        poly_area = cv2.countNonZero(poly_mask)
        if poly_area == 0:
            continue
        overlap = cv2.countNonZero(cv2.bitwise_and(poly_mask, wall_mask))
        if overlap / poly_area >= _WALL_OVERLAP_MIN:
            kept.append(panel)
    return kept


def detect_panels(img: "np.ndarray") -> WireframeResult:
    """Run the full panel detection pipeline on a BGR image.

    Parameters
    ----------
    img : ndarray (H, W, 3) BGR
        Source image to analyze.

    Returns
    -------
    WireframeResult with panels, segments (deduplicated), and joints.
    """
    h, w = img.shape[:2]

    # 1. K-means segmentation
    labels, small_bgr, scale, _sh, _sw = build_kmeans_labels(img)

    # 2. Identify wall clusters
    wall_clusters = find_wall_clusters(small_bgr, labels)

    # 3. Build wall mask to identify the wall region
    wall_mask_full = build_wall_mask_hsv(img)

    # 4. Extract panel polygons
    panels = extract_panels(
        labels, small_bgr, img, wall_clusters, scale,
    )

    # 5. Filter: keep panels whose centroid lies on the wall
    panels = _filter_wall_panels(panels, wall_mask_full)

    # 6. Build segments + deduplicate
    raw_segments = panels_to_segments(panels)
    segments = deduplicate_segments(raw_segments)

    # 7. Collect unique joints
    joints = extract_joints(panels)

    return WireframeResult(
        panels=panels,
        segments=segments,
        joints=joints,
        image_shape=(h, w),
    )
