"""Line detection for camera calibration.

Extracts lines from panel joints and t-nut grids using
Canny edge detection + probabilistic Hough transform,
then classifies them as horizontal, vertical, or angled.
"""
from __future__ import annotations

import math

import cv2
import numpy as np

from scene.colors import build_wall_mask_hsv

# ── Parameters ───────────────────────────────────────────────────────

CANNY_LOW = 40
CANNY_HIGH = 130
HOUGH_RHO = 1
HOUGH_THETA = np.pi / 180
HOUGH_THRESHOLD = 60
HOUGH_MIN_LEN = 50
HOUGH_MAX_GAP = 12

# Angle classification thresholds (degrees from horizontal)
HORIZONTAL_MAX_DEG = 20.0
VERTICAL_MIN_DEG = 70.0


def detect_lines(
    img: np.ndarray,
    *,
    wall_only: bool = True,
) -> tuple[list[np.ndarray], list[np.ndarray], list[np.ndarray]]:
    """Detect and classify lines as horizontal, vertical, or angled.

    Parameters
    ----------
    img : ndarray (H, W, 3) BGR
    wall_only : bool
        If True, mask non-wall pixels before edge detection.

    Returns
    -------
    (horizontal_lines, vertical_lines, angled_lines)
        Each is a list of segments as [x1, y1, x2, y2] arrays.
    """
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    blurred = cv2.GaussianBlur(gray, (5, 5), 0)
    edges = cv2.Canny(blurred, CANNY_LOW, CANNY_HIGH)

    if wall_only:
        wall_mask = build_wall_mask_hsv(img)
        edges = cv2.bitwise_and(edges, wall_mask)

    raw = cv2.HoughLinesP(
        edges,
        rho=HOUGH_RHO,
        theta=HOUGH_THETA,
        threshold=HOUGH_THRESHOLD,
        minLineLength=HOUGH_MIN_LEN,
        maxLineGap=HOUGH_MAX_GAP,
    )

    horizontal: list[np.ndarray] = []
    vertical: list[np.ndarray] = []
    angled: list[np.ndarray] = []

    if raw is None:
        return horizontal, vertical, angled

    for seg in raw:
        line = seg[0]
        x1, y1, x2, y2 = line
        dx = x2 - x1
        dy = y2 - y1
        angle_deg = abs(math.degrees(math.atan2(dy, dx)))

        if angle_deg < HORIZONTAL_MAX_DEG or angle_deg > (180 - HORIZONTAL_MAX_DEG):
            horizontal.append(line)
        elif VERTICAL_MIN_DEG < angle_deg < (180 - VERTICAL_MIN_DEG):
            vertical.append(line)
        else:
            angled.append(line)

    return horizontal, vertical, angled


def line_to_homogeneous(seg: np.ndarray) -> np.ndarray:
    """Convert segment [x1, y1, x2, y2] to homogeneous line (a, b, c).

    Line equation: ax + by + c = 0.
    """
    x1, y1, x2, y2 = seg.astype(np.float64)
    p1 = np.array([x1, y1, 1.0])
    p2 = np.array([x2, y2, 1.0])
    line = np.cross(p1, p2)
    norm = np.linalg.norm(line[:2])
    if norm > 1e-12:
        line /= norm
    return line
