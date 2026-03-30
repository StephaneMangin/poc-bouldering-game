"""K-means color segmentation for climbing wall photos.

Heavily downscales + blurs the image to eliminate holds and textures,
then clusters in LAB color space. Each cluster is classified as a wall
color (grey/green/red) or non-wall (floor, people, ceiling).
"""
from __future__ import annotations

import cv2
import numpy as np

from scene.colors import classify_kmeans_cluster

# ── Parameters ───────────────────────────────────────────────────────

N_CLUSTERS = 5
TARGET_SMALL_H = 80  # very aggressive downscale
BLUR_KERNEL = 15  # heavy blur to kill hold textures


def build_kmeans_labels(
    img: np.ndarray,
) -> tuple[np.ndarray, np.ndarray, float, int, int]:
    """K-means on heavily downscaled + blurred image in LAB space.

    Returns
    -------
    labels : ndarray (sh, sw) uint8 — cluster id per pixel
    small_bgr : ndarray (sh, sw, 3) — the small BGR image used
    scale : float — downscale factor (small / original)
    sh, sw : int — small image dimensions
    """
    h, w = img.shape[:2]
    scale = TARGET_SMALL_H / h
    sh = TARGET_SMALL_H
    sw = max(1, int(w * scale))

    small = cv2.resize(img, (sw, sh), interpolation=cv2.INTER_AREA)
    small = cv2.GaussianBlur(small, (BLUR_KERNEL, BLUR_KERNEL), 0)
    small = cv2.bilateralFilter(small, 9, 75, 75)

    lab = cv2.cvtColor(small, cv2.COLOR_BGR2LAB)
    samples = lab.reshape((-1, 3)).astype(np.float32)
    criteria = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 300, 0.2)
    _, labels_flat, _ = cv2.kmeans(
        samples, N_CLUSTERS, None, criteria, 15, cv2.KMEANS_PP_CENTERS,
    )
    labels = labels_flat.reshape((sh, sw)).astype(np.uint8)
    return labels, small, scale, sh, sw


def find_wall_clusters(
    small_bgr: np.ndarray,
    labels: np.ndarray,
) -> set[int]:
    """Identify which K-means clusters correspond to wall panels."""
    wall_ids: set[int] = set()
    for cluster_id in range(N_CLUSTERS):
        cls = classify_kmeans_cluster(small_bgr, labels, cluster_id)
        if cls.startswith("wall_"):
            wall_ids.add(cluster_id)
    return wall_ids
