"""Extract panel polygons from K-means segmented wall clusters.

For each wall cluster: extract contours → approximate as polygons →
create Panel objects with median color from the original image.
"""
from __future__ import annotations

import cv2
import numpy as np

from scene.colors import GREY, GREEN, RED, classify_kmeans_cluster
from scene.models import Panel, Segment

# ── Parameters ───────────────────────────────────────────────────────

MIN_CONTOUR_AREA = 200  # at small scale (px²) — filters noise
EPSILON_RATIO = 0.012  # polygon approximation tolerance
MIN_SEG_LENGTH = 35  # at original scale (px)
DEDUP_DIST = 25  # merge near-duplicate segments within this dist

# ── Cluster → color label mapping ────────────────────────────────────

_CLUSTER_TO_LABEL = {
    "wall_grey": GREY,
    "wall_green": GREEN,
    "wall_red": RED,
    "wall_yellow": RED,  # yellow accent → treat as red variant
}

_LABEL_TO_NAME = {
    GREY: "grey",
    GREEN: "green",
    RED: "red",
}


def _cluster_color_label(
    small_bgr: np.ndarray, labels: np.ndarray, cluster_id: int,
) -> tuple[int, str]:
    """Classify a K-means cluster and return (color_label, color_name)."""
    cls = classify_kmeans_cluster(small_bgr, labels, cluster_id)
    label = _CLUSTER_TO_LABEL.get(cls, 0)
    name = _LABEL_TO_NAME.get(label, "unknown")
    return label, name


# ── Contour → Polygon extraction ────────────────────────────────────


def extract_panels(
    labels: np.ndarray,
    small_bgr: np.ndarray,
    img: np.ndarray,
    wall_clusters: set[int],
    scale: float,
    wall_mask: np.ndarray | None = None,
) -> list[Panel]:
    """Extract Panel objects from wall-cluster contours.

    Each contour is approximated as a polygon, scaled back to original
    resolution, and assigned its median BGR color from the source image.

    Parameters
    ----------
    wall_mask : optional binary mask (same size as labels) restricting
        detection to wall-only pixels. Non-wall contours are discarded.
    """
    orig_h, orig_w = img.shape[:2]
    panels: list[Panel] = []

    for cluster_id in sorted(wall_clusters):
        color_label, color_name = _cluster_color_label(
            small_bgr, labels, cluster_id,
        )

        mask = (labels == cluster_id).astype(np.uint8) * 255
        if wall_mask is not None:
            mask = cv2.bitwise_and(mask, wall_mask)

        mask = (labels == cluster_id).astype(np.uint8) * 255
        contours, _ = cv2.findContours(
            mask, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE,
        )

        for contour in contours:
            if cv2.contourArea(contour) < MIN_CONTOUR_AREA:
                continue
            perimeter = cv2.arcLength(contour, True)
            if perimeter < 20:
                continue

            epsilon = EPSILON_RATIO * perimeter
            approx = cv2.approxPolyDP(contour, epsilon, True)

            # Scale polygon to original resolution
            pts = approx.reshape(-1, 2).astype(np.float64)
            pts[:, 0] /= scale
            pts[:, 1] /= scale
            pts = np.clip(pts, 0, [orig_w - 1, orig_h - 1]).astype(np.int32)

            # Compute median color from original image
            poly_mask = np.zeros((orig_h, orig_w), dtype=np.uint8)
            cv2.fillPoly(poly_mask, [pts], 255)
            pixels = img[poly_mask > 0]
            if len(pixels) == 0:
                continue
            median_bgr = tuple(int(c) for c in np.median(pixels, axis=0))

            area_px = float(cv2.contourArea(pts.reshape(-1, 1, 2)))

            panels.append(Panel(
                polygon=pts,
                color_label=color_label,
                color_name=color_name,
                median_bgr=median_bgr,  # type: ignore[arg-type]
                area_px=area_px,
                cluster_id=cluster_id,
            ))

    return panels


# ── Segment extraction + deduplication ───────────────────────────────


def panels_to_segments(
    panels: list[Panel],
) -> list[Segment]:
    """Extract edge segments from all panel polygons."""
    all_segments: list[Segment] = []
    for panel in panels:
        pts = panel.polygon
        n = len(pts)
        for i in range(n):
            p1 = (int(pts[i][0]), int(pts[i][1]))
            p2 = (int(pts[(i + 1) % n][0]), int(pts[(i + 1) % n][1]))
            seg_len = np.hypot(p2[0] - p1[0], p2[1] - p1[1])
            if seg_len >= MIN_SEG_LENGTH:
                all_segments.append((p1, p2))
    return all_segments


def deduplicate_segments(segments: list[Segment]) -> list[Segment]:
    """Remove near-duplicate segments from overlapping cluster contours."""
    if len(segments) <= 1:
        return list(segments)

    n = len(segments)
    used = [False] * n
    kept: list[Segment] = []

    for i in range(n):
        if used[i]:
            continue
        used[i] = True
        p1, p2 = segments[i]

        for j in range(i + 1, n):
            if used[j]:
                continue
            q1, q2 = segments[j]

            d_same = max(
                np.hypot(p1[0] - q1[0], p1[1] - q1[1]),
                np.hypot(p2[0] - q2[0], p2[1] - q2[1]),
            )
            d_rev = max(
                np.hypot(p1[0] - q2[0], p1[1] - q2[1]),
                np.hypot(p2[0] - q1[0], p2[1] - q1[1]),
            )

            if min(d_same, d_rev) < DEDUP_DIST:
                used[j] = True

        kept.append((p1, p2))

    return kept


def extract_joints(panels: list[Panel]) -> list[tuple[int, int]]:
    """Collect unique polygon vertices (joints) across all panels."""
    seen: set[tuple[int, int]] = set()
    joints: list[tuple[int, int]] = []
    for panel in panels:
        for pt in panel.polygon:
            key = (int(pt[0]), int(pt[1]))
            if key not in seen:
                seen.add(key)
                joints.append(key)
    return joints
