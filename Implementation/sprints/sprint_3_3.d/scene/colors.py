"""Wall-color classification for climbing gym panels.

Climbing wall panels come in exactly 3 colors:
- Grey concrete
- Green/teal painted
- Red/orange painted
- (Yellow accent, rare)

Centralizes HSV-based classification used by wireframe/ and analyze/.
"""

from __future__ import annotations

import cv2
import numpy as np

# ── Panel color IDs ──────────────────────────────────────────────────────

UNKNOWN = 0
GREY = 1
GREEN = 2
RED = 3

# ── HSV thresholds ───────────────────────────────────────────────────────
# All values in OpenCV HSV: H ∈ [0,180], S ∈ [0,255], V ∈ [0,255]

# Minimum brightness to be considered (below = shadow/dark)
V_MIN_DARK = 30

# Red/orange panel: high sat, wraps around H=0
RED_S_MIN = 35
RED_V_MIN = 40
RED_H_LO_MAX = 15
RED_H_HI_MIN = 155

# Green/teal panel
GREEN_S_MIN = 30
GREEN_V_MIN = 25
GREEN_H_MIN = 35
GREEN_H_MAX = 100

# Grey panel: low saturation, moderate brightness
GREY_S_MAX = 50
GREY_V_MIN = 40
GREY_V_MAX = 215


def classify_pixel_hsv(h: int, s: int, v: int) -> int:
    """Classify a single HSV pixel as panel color.

    Returns: UNKNOWN (0), GREY (1), GREEN (2), or RED (3).
    """
    if v < V_MIN_DARK:
        return UNKNOWN

    if s > RED_S_MIN and v > RED_V_MIN and (h <= RED_H_LO_MAX or h >= RED_H_HI_MIN):
        return RED

    if s > GREEN_S_MIN and v > GREEN_V_MIN and GREEN_H_MIN <= h <= GREEN_H_MAX:
        return GREEN

    if s < GREY_S_MAX and GREY_V_MIN <= v <= GREY_V_MAX:
        return GREY

    return UNKNOWN


def build_wall_mask_hsv(
    img: np.ndarray,
    *,
    exclude_ceiling_ratio: float = 0.12,
    exclude_floor_ratio: float = 0.15,
    grey_v_range: tuple[int, int] = (70, 200),
    green_s_min: int = 50,
    red_s_min: int = 60,
) -> np.ndarray:
    """Build a binary mask isolating climbing wall surfaces (grey, green, red).

    Excludes floor mats, ceiling, holds (via morphology).
    """
    hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)
    h_img = img.shape[0]

    grey_mask = cv2.inRange(hsv, (0, 0, grey_v_range[0]), (180, GREY_S_MAX, grey_v_range[1]))
    green_mask = cv2.inRange(hsv, (GREEN_H_MIN + 5, green_s_min, 50), (GREEN_H_MAX - 5, 255, 220))

    red_lo = cv2.inRange(hsv, (0, red_s_min, 50), (12, 255, 230))
    red_hi = cv2.inRange(hsv, (RED_H_HI_MIN, red_s_min, 50), (180, 255, 230))
    red_mask = cv2.bitwise_or(red_lo, red_hi)

    mask = cv2.bitwise_or(grey_mask, green_mask)
    mask = cv2.bitwise_or(mask, red_mask)

    ceiling_cut = int(h_img * exclude_ceiling_ratio)
    floor_cut = int(h_img * (1.0 - exclude_floor_ratio))
    mask[:ceiling_cut, :] = 0
    mask[floor_cut:, :] = 0

    kernel_close = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (21, 21))
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel_close)
    kernel_open = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (11, 11))
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, kernel_open)

    return mask


def build_label_map(
    img: np.ndarray,
    downscale: int = 12,
) -> np.ndarray:
    """Build a coarse label map by classifying a heavily downscaled image.

    Downscaling averages out climbing holds (small relative to panels),
    so the label map only reflects the panel colors. Upscaling back with
    nearest-neighbor preserves sharp boundaries.

    Returns an array of shape (H, W) with values UNKNOWN/GREY/GREEN/RED.
    """
    h, w = img.shape[:2]
    small_h = max(1, h // downscale)
    small_w = max(1, w // downscale)
    small = cv2.resize(img, (small_w, small_h), interpolation=cv2.INTER_AREA)
    small = cv2.GaussianBlur(small, (7, 7), 0)
    hsv = cv2.cvtColor(small, cv2.COLOR_BGR2HSV)

    labels_small = np.zeros((small_h, small_w), dtype=np.uint8)

    red_lo = cv2.inRange(hsv, (0, RED_S_MIN, RED_V_MIN), (RED_H_LO_MAX, 255, 245))
    red_hi = cv2.inRange(hsv, (RED_H_HI_MIN, RED_S_MIN, RED_V_MIN), (180, 255, 245))
    red_mask = cv2.bitwise_or(red_lo, red_hi)

    green_mask = cv2.inRange(
        hsv, (GREEN_H_MIN, GREEN_S_MIN, GREEN_V_MIN), (GREEN_H_MAX, 255, 225),
    )
    grey_mask = cv2.inRange(hsv, (0, 0, GREY_V_MIN), (180, GREY_S_MAX, GREY_V_MAX))

    labels_small[grey_mask > 0] = GREY
    labels_small[green_mask > 0] = GREEN
    labels_small[red_mask > 0] = RED

    morph_k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    for lv in (GREY, GREEN, RED):
        m = (labels_small == lv).astype(np.uint8) * 255
        m = cv2.morphologyEx(m, cv2.MORPH_OPEN, morph_k)
        m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, morph_k)
        labels_small[m == 0] = np.where(
            labels_small[m == 0] == lv, UNKNOWN, labels_small[m == 0],
        )
        labels_small[m > 0] = lv

    return cv2.resize(labels_small, (w, h), interpolation=cv2.INTER_NEAREST)


def classify_kmeans_cluster(
    small_bgr: np.ndarray,
    labels: np.ndarray,
    cluster_id: int,
) -> str:
    """Classify a K-means cluster as wall or non-wall by its mean HSV color.

    Returns a string label: 'wall_red', 'wall_green', 'wall_yellow',
    'wall_grey', 'dark', 'bright', 'other', 'unknown'.
    """
    mask = labels == cluster_id
    if not np.any(mask):
        return "unknown"

    cluster_px = small_bgr[mask]
    mean_bgr = np.mean(cluster_px, axis=0).astype(np.uint8).reshape(1, 1, 3)
    mean_hsv = cv2.cvtColor(mean_bgr, cv2.COLOR_BGR2HSV)[0, 0]
    h, s, v = int(mean_hsv[0]), int(mean_hsv[1]), int(mean_hsv[2])

    if v < V_MIN_DARK:
        return "dark"

    if v > 240 and s < 25:
        return "bright"

    if s > 30 and v > 35 and (h <= 18 or h >= RED_H_HI_MIN):
        return "wall_red"

    if s > 25 and v > 20 and GREEN_H_MIN <= h <= GREEN_H_MAX:
        return "wall_green"

    if s > 35 and 18 < h < 35 and v > 60:
        return "wall_yellow"

    if s < 60 and V_MIN_DARK <= v <= 225:
        return "wall_grey"

    return "other"
