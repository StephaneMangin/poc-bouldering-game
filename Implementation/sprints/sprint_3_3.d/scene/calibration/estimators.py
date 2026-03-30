"""Camera parameter estimation from vanishing points and known geometry.

Estimates:
- Camera height (y) from horizon line position
- Field of view from vanishing point distance
- Pitch from vertical vanishing point convergence
- Scale calibration from known panel size (1.20 m, holes every 0.20 m)
"""
from __future__ import annotations

import math

import numpy as np

# ── Known gym dimensions ─────────────────────────────────────────────

PANEL_SIZE_M = 1.20      # standard plywood panel: 1.20 m × 1.20 m
HOLE_SPACING_M = 0.20    # t-nut grid spacing: 20 cm
DOOR_HEIGHT_M = 2.00     # reference door height


def estimate_horizon_y(
    vp_horizontal: np.ndarray | None,
    vp_vertical: np.ndarray | None,
    img_shape: tuple[int, int],
) -> float:
    """Estimate the horizon line y-coordinate (in pixels).

    The horizon line passes through horizontal vanishing points.
    If only one VP is found, use its y-coordinate directly.
    If none found, assume center of image.
    """
    if vp_horizontal is not None:
        return float(vp_horizontal[1])
    if vp_vertical is not None:
        # vertical VP doesn't directly give horizon, fall back to center
        return img_shape[0] / 2.0
    return img_shape[0] / 2.0


def estimate_camera_height(
    horizon_y_px: float,
    img_height_px: int,
    reference_height_m: float = DOOR_HEIGHT_M,
    reference_top_y_px: float | None = None,
    reference_bottom_y_px: float | None = None,
) -> float:
    """Estimate camera height from horizon position.

    If a reference object (e.g. door of known height) is visible with its
    top and bottom pixel positions known, compute exact height.
    Otherwise, use a heuristic based on horizon position in frame.

    The horizon line is at the camera's optical center height.
    """
    if reference_top_y_px is not None and reference_bottom_y_px is not None:
        ref_span_px = abs(reference_bottom_y_px - reference_top_y_px)
        if ref_span_px < 1.0:
            return 1.60  # fallback to average eye level

        px_per_meter = ref_span_px / reference_height_m
        ref_bottom = max(reference_bottom_y_px, reference_top_y_px)
        # distance from horizon to reference bottom = camera-height portion
        camera_height_px = ref_bottom - horizon_y_px
        return camera_height_px / px_per_meter

    # heuristic: horizon position normalized (0=top, 1=bottom)
    horizon_ratio = horizon_y_px / img_height_px
    # typical range: 0.3 (high camera ~3m) to 0.8 (low camera ~0.5m)
    # linear mapping: ratio 0.5 → ~1.60m (eye level)
    camera_y = 1.60 + (0.5 - horizon_ratio) * 5.0
    return max(0.10, min(5.00, camera_y))


def estimate_fov(
    vp_horizontal: np.ndarray | None,
    img_shape: tuple[int, int],
    sensor_width_mm: float = 6.17,  # typical smartphone (1/2.55")
) -> float:
    """Estimate vertical FOV in degrees.

    Uses the distance from the image center to the horizontal VP
    to estimate focal length, then derives vertical FOV.

    FOV = 2 * arctan(d / (2*f))

    If VP is not available, estimate from image aspect ratio
    and typical smartphone FOV (75-80 deg horizontal).
    """
    h, w = img_shape

    if vp_horizontal is not None:
        cx = w / 2.0
        cy = h / 2.0
        # distance from VP to image center
        dx = vp_horizontal[0] - cx
        dy = vp_horizontal[1] - cy
        vp_dist = math.sqrt(dx * dx + dy * dy)

        if vp_dist > 10:
            # focal length in pixels ≈ distance to VP for ~90° viewing angle
            focal_px = vp_dist
            # vertical FOV
            fov = 2.0 * math.degrees(math.atan2(h / 2.0, focal_px))
            return max(30.0, min(120.0, fov))

    # fallback: common smartphone wide-angle ~ 77° horizontal
    horizontal_fov = 77.0
    focal_est = (w / 2.0) / math.tan(math.radians(horizontal_fov / 2.0))
    fov = 2.0 * math.degrees(math.atan2(h / 2.0, focal_est))
    return round(fov, 0)


def estimate_pitch(
    vp_vertical: np.ndarray | None,
    img_shape: tuple[int, int],
) -> float:
    """Estimate camera pitch (tilt) in degrees.

    Positive = camera looking up, negative = looking down.

    If vertical lines converge toward the top of the image,
    the camera is tilted upward. If they converge below,
    it's tilted downward.
    """
    if vp_vertical is None:
        return 0.0

    h, w = img_shape
    cy = h / 2.0
    cx = w / 2.0

    # VP y-position relative to center
    # VP above center means camera looks up (positive pitch)
    vp_y = vp_vertical[1]
    vp_x = vp_vertical[0]

    # only use VP if it's roughly centered horizontally
    # (to avoid misidentified diagonal lines)
    if abs(vp_x - cx) > w:
        return 0.0

    # estimate focal length from VP distance to center
    dy = cy - vp_y
    focal_estimate = max(h, w) * 0.8  # rough estimate

    pitch_rad = math.atan2(dy, focal_estimate)
    pitch_deg = math.degrees(pitch_rad)

    return max(-45.0, min(45.0, round(pitch_deg, 0)))


def estimate_scale(
    line_segments: list[np.ndarray],
    known_spacing_m: float = HOLE_SPACING_M,
    known_panel_m: float = PANEL_SIZE_M,
) -> float | None:
    """Estimate pixels-per-meter from regularly spaced features.

    Looks for clusters of similar-length line segments that match
    the expected t-nut hole spacing (20 cm) or panel size (1.20 m).

    Returns pixels-per-meter or None if no reliable scale found.
    """
    if not line_segments:
        return None

    lengths = []
    for seg in line_segments:
        x1, y1, x2, y2 = seg
        length = math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2)
        if length > 5:
            lengths.append(length)

    if not lengths:
        return None

    lengths_arr = np.array(lengths)

    # find the most common length via histogram
    n_bins = max(10, len(lengths) // 3)
    hist, bin_edges = np.histogram(lengths_arr, bins=n_bins)
    peak_idx = np.argmax(hist)
    peak_length = (bin_edges[peak_idx] + bin_edges[peak_idx + 1]) / 2.0

    if peak_length < 5:
        return None

    # decide if this length corresponds to hole spacing or panel size
    # ratio: panel / holes = 6.0
    # if peak ~ 6x some smaller cluster → it's panel-scale
    panel_ratio = known_panel_m / known_spacing_m  # 6.0
    px_per_m_hole = peak_length / known_spacing_m
    px_per_m_panel = peak_length / known_panel_m

    # prefer hole spacing if many small segments, panel if fewer large ones
    median_len = float(np.median(lengths_arr))
    if median_len < peak_length * panel_ratio / 2:
        return px_per_m_hole
    return px_per_m_panel
