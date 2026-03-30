"""Vanishing point detection via RANSAC on line intersections.

Extends detected lines into infinite representations, computes
pairwise intersections, then clusters them with RANSAC to find
dominant vanishing points (horizontal and vertical).
"""
from __future__ import annotations

import numpy as np

from .lines import line_to_homogeneous

# ── Parameters ───────────────────────────────────────────────────────

RANSAC_ITERATIONS = 500
RANSAC_INLIER_THRESHOLD_PX = 30.0
MIN_LINES_FOR_VP = 4


def _intersect(l1: np.ndarray, l2: np.ndarray) -> np.ndarray | None:
    """Compute intersection of two homogeneous lines.

    Returns (x, y) in pixel coordinates, or None if parallel.
    """
    pt = np.cross(l1, l2)
    if abs(pt[2]) < 1e-12:
        return None
    return pt[:2] / pt[2]


def _ransac_vanishing_point(
    lines: list[np.ndarray],
    img_shape: tuple[int, int],
    rng: np.random.Generator,
) -> tuple[np.ndarray | None, list[int]]:
    """RANSAC to find a single dominant vanishing point.

    Parameters
    ----------
    lines : list of [x1, y1, x2, y2] segments
    img_shape : (H, W)
    rng : numpy random generator

    Returns
    -------
    (vanishing_point, inlier_indices) or (None, [])
    """
    n = len(lines)
    if n < MIN_LINES_FOR_VP:
        return None, []

    h, w = img_shape
    # allow VP outside the image but not absurdly far
    limit = max(h, w) * 4

    homo_lines = [line_to_homogeneous(seg) for seg in lines]

    best_vp: np.ndarray | None = None
    best_inliers: list[int] = []

    for _ in range(RANSAC_ITERATIONS):
        i, j = rng.choice(n, size=2, replace=False)
        candidate = _intersect(homo_lines[i], homo_lines[j])
        if candidate is None:
            continue
        if abs(candidate[0]) > limit or abs(candidate[1]) > limit:
            continue

        # count inliers: lines whose extension passes close to candidate
        inliers: list[int] = []
        for k, hl in enumerate(homo_lines):
            dist = abs(hl[0] * candidate[0] + hl[1] * candidate[1] + hl[2])
            if dist < RANSAC_INLIER_THRESHOLD_PX:
                inliers.append(k)

        if len(inliers) > len(best_inliers):
            best_inliers = inliers
            best_vp = candidate

    return best_vp, best_inliers


def find_vanishing_points(
    horizontal_lines: list[np.ndarray],
    vertical_lines: list[np.ndarray],
    img_shape: tuple[int, int],
    *,
    seed: int = 42,
) -> tuple[np.ndarray | None, np.ndarray | None]:
    """Find horizontal and vertical vanishing points.

    Parameters
    ----------
    horizontal_lines : near-horizontal segments [x1, y1, x2, y2]
    vertical_lines : near-vertical segments
    img_shape : (H, W)
    seed : random seed for reproducibility

    Returns
    -------
    (vp_horizontal, vp_vertical) — each is (x, y) or None if not found.
    """
    rng = np.random.default_rng(seed)

    vp_h, _ = _ransac_vanishing_point(horizontal_lines, img_shape, rng)
    vp_v, _ = _ransac_vanishing_point(vertical_lines, img_shape, rng)

    return vp_h, vp_v
