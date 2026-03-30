"""Debug visualization for camera calibration results.

Draws detected lines, vanishing points, and horizon on the source image.
"""
from __future__ import annotations

from pathlib import Path

import cv2
import numpy as np

from .pipeline import CalibrationResult

# ── Colors (BGR) ─────────────────────────────────────────────────────

COLOR_HORIZONTAL = (255, 100, 0)    # blue
COLOR_VERTICAL = (0, 200, 0)        # green
COLOR_ANGLED = (0, 180, 255)        # orange
COLOR_VP = (0, 0, 255)              # red
COLOR_HORIZON = (255, 0, 255)       # magenta
COLOR_TEXT = (255, 255, 255)        # white
COLOR_TEXT_BG = (0, 0, 0)           # black


def draw_calibration_overlay(
    img: np.ndarray,
    result: CalibrationResult,
    horizontal_lines: list[np.ndarray],
    vertical_lines: list[np.ndarray],
    angled_lines: list[np.ndarray],
) -> np.ndarray:
    """Draw full calibration overlay on a copy of the image."""
    vis = img.copy()
    h, w = vis.shape[:2]

    # draw lines
    for seg in horizontal_lines:
        x1, y1, x2, y2 = seg
        cv2.line(vis, (x1, y1), (x2, y2), COLOR_HORIZONTAL, 1, cv2.LINE_AA)
    for seg in vertical_lines:
        x1, y1, x2, y2 = seg
        cv2.line(vis, (x1, y1), (x2, y2), COLOR_VERTICAL, 1, cv2.LINE_AA)
    for seg in angled_lines:
        x1, y1, x2, y2 = seg
        cv2.line(vis, (x1, y1), (x2, y2), COLOR_ANGLED, 1, cv2.LINE_AA)

    # draw horizon line
    hy = int(result.horizon_y_px)
    cv2.line(vis, (0, hy), (w, hy), COLOR_HORIZON, 2, cv2.LINE_AA)

    # draw vanishing points
    if result.vp_horizontal is not None:
        vx, vy = int(result.vp_horizontal[0]), int(result.vp_horizontal[1])
        if 0 <= vx < w and 0 <= vy < h:
            cv2.drawMarker(vis, (vx, vy), COLOR_VP, cv2.MARKER_CROSS, 30, 2)
            cv2.putText(vis, "VP_h", (vx + 15, vy - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, COLOR_VP, 1)

    if result.vp_vertical is not None:
        vx, vy = int(result.vp_vertical[0]), int(result.vp_vertical[1])
        if 0 <= vx < w and 0 <= vy < h:
            cv2.drawMarker(vis, (vx, vy), COLOR_VP, cv2.MARKER_DIAMOND, 30, 2)
            cv2.putText(vis, "VP_v", (vx + 15, vy - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, COLOR_VP, 1)

    # info panel
    info_lines = [
        f"height={result.camera_height_m:.2f}m",
        f"fov={result.fov_deg:.0f} deg",
        f"pitch={result.pitch_deg:+.0f} deg",
        f"H={result.n_horizontal} V={result.n_vertical} A={result.n_angled}",
    ]
    y0 = 30
    for i, text in enumerate(info_lines):
        ty = y0 + i * 25
        cv2.putText(vis, text, (12, ty + 1), cv2.FONT_HERSHEY_SIMPLEX, 0.6, COLOR_TEXT_BG, 3, cv2.LINE_AA)
        cv2.putText(vis, text, (12, ty), cv2.FONT_HERSHEY_SIMPLEX, 0.6, COLOR_TEXT, 1, cv2.LINE_AA)

    # legend
    legend_y = h - 80
    legend_items = [
        ("horizontal", COLOR_HORIZONTAL),
        ("vertical", COLOR_VERTICAL),
        ("angled", COLOR_ANGLED),
        ("horizon", COLOR_HORIZON),
    ]
    for i, (label, color) in enumerate(legend_items):
        ly = legend_y + i * 20
        cv2.line(vis, (15, ly), (45, ly), color, 2)
        cv2.putText(vis, label, (50, ly + 5), cv2.FONT_HERSHEY_SIMPLEX, 0.4, COLOR_TEXT, 1, cv2.LINE_AA)

    return vis


def save_calibration_debug(
    img: np.ndarray,
    result: CalibrationResult,
    horizontal_lines: list[np.ndarray],
    vertical_lines: list[np.ndarray],
    angled_lines: list[np.ndarray],
    out_dir: Path,
) -> Path:
    """Save debug visualization to disk."""
    vis = draw_calibration_overlay(img, result, horizontal_lines, vertical_lines, angled_lines)
    stem = Path(result.filename).stem
    out_path = out_dir / f"{stem}_calibration.jpg"
    cv2.imwrite(str(out_path), vis, [cv2.IMWRITE_JPEG_QUALITY, 90])
    return out_path
