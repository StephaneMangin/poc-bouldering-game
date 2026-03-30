"""Edge analysis: Canny + Hough lines on wall-masked photos."""
from __future__ import annotations

import math
from pathlib import Path

import cv2
import numpy as np
from scene.colors import build_wall_mask_hsv
from scene.constants import (
    ARCH_FONT,
    EDGE_CANNY_HIGH,
    EDGE_CANNY_LOW,
    HOUGH_MAX_GAP,
    HOUGH_MIN_LEN,
    HOUGH_THRESHOLD,
)
from scene.models import Bloc, EdgeStats


def classify_angle(angle_deg: float) -> str:
    """Classify an angle as vertical, horizontal, or angled."""
    abs_angle = abs(angle_deg)
    if abs_angle > 75:
        return "vertical"
    if abs_angle < 15:
        return "horizontal"
    return "angled"


def analyze_edges(bloc: Bloc, out_dir: Path) -> EdgeStats | None:
    """Run edge detection on wall-only pixels of bloc photos."""
    if not bloc.photos:
        return None

    all_angles: list[float] = []
    best_photo: np.ndarray | None = None
    best_mask: np.ndarray | None = None
    best_lines: np.ndarray | None = None
    best_wall_ratio = 0.0

    for p in bloc.photos:
        img = cv2.imread(str(p))
        if img is None:
            continue

        wall_mask = build_wall_mask_hsv(img)
        wall_ratio = cv2.countNonZero(wall_mask) / (img.shape[0] * img.shape[1])

        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        blurred = cv2.GaussianBlur(gray, (5, 5), 0)
        edges = cv2.Canny(blurred, EDGE_CANNY_LOW, EDGE_CANNY_HIGH)
        edges = cv2.bitwise_and(edges, wall_mask)

        lines = cv2.HoughLinesP(
            edges, rho=1, theta=np.pi / 180,
            threshold=HOUGH_THRESHOLD,
            minLineLength=HOUGH_MIN_LEN,
            maxLineGap=HOUGH_MAX_GAP,
        )

        if lines is not None:
            for line in lines:
                x1, y1, x2, y2 = line[0]
                angle = math.degrees(math.atan2(abs(y2 - y1), abs(x2 - x1)))
                all_angles.append(90 - angle)

            if wall_ratio > best_wall_ratio:
                best_wall_ratio = wall_ratio
                best_photo = img.copy()
                best_mask = wall_mask.copy()
                best_lines = lines

    if not all_angles or best_photo is None:
        return None

    stats = EdgeStats()
    for a in all_angles:
        cat = classify_angle(a)
        if cat == "vertical":
            stats.vertical_count += 1
        elif cat == "horizontal":
            stats.horizontal_count += 1
        else:
            stats.angled_count += 1

    wall_angles = [a for a in all_angles if abs(a) > 15]
    stats.dominant_angle_deg = float(np.median(wall_angles)) if wall_angles else 90.0

    if stats.dominant_angle_deg > 80:
        stats.wall_type = "vertical"
    elif stats.dominant_angle_deg > 60:
        stats.wall_type = "léger dévers"
    elif stats.dominant_angle_deg > 40:
        stats.wall_type = "dévers"
    elif stats.dominant_angle_deg > 20:
        stats.wall_type = "gros dévers"
    else:
        stats.wall_type = "dalle"

    _save_wall_mask_viz(best_photo, best_mask, out_dir)
    _save_edge_overlay(best_photo, best_lines, stats, out_dir)

    return stats


def _save_wall_mask_viz(
    photo: np.ndarray, mask: np.ndarray | None, out_dir: Path,
) -> None:
    if mask is None:
        return
    vis = photo.copy()
    mask_inv = cv2.bitwise_not(mask)
    mask_inv_3ch = cv2.merge([mask_inv, mask_inv, mask_inv])
    dimmed = cv2.subtract(vis, (mask_inv_3ch * 0.6).astype(np.uint8))
    wall_tint = np.zeros_like(vis)
    wall_tint[:, :, 1] = 40
    wall_highlight = cv2.add(
        dimmed,
        cv2.bitwise_and(wall_tint, cv2.merge([mask, mask, mask])),
    )
    cv2.putText(
        wall_highlight, "Zone mur detectee (gris+vert+rouge)",
        (10, 25), ARCH_FONT, 0.5, (0, 255, 0), 1, cv2.LINE_AA,
    )
    cv2.imwrite(
        str(out_dir / "wall_mask.jpg"), wall_highlight,
        [cv2.IMWRITE_JPEG_QUALITY, 85],
    )


def _save_edge_overlay(
    photo: np.ndarray,
    lines: np.ndarray | None,
    stats: EdgeStats,
    out_dir: Path,
) -> None:
    overlay = photo.copy()
    if lines is not None:
        for line in lines:
            x1, y1, x2, y2 = line[0]
            angle_fv = 90 - math.degrees(
                math.atan2(abs(y2 - y1), abs(x2 - x1)),
            )
            cat = classify_angle(angle_fv)
            color = {
                "vertical": (0, 255, 0),
                "horizontal": (255, 0, 0),
                "angled": (0, 165, 255),
            }[cat]
            cv2.line(overlay, (x1, y1), (x2, y2), color, 2, cv2.LINE_AA)

    legend_y = 30
    for label_text, color in [
        ("Vertical", (0, 255, 0)),
        ("Horizontal", (255, 0, 0)),
        (f"Angle ({stats.dominant_angle_deg:.0f} deg)", (0, 165, 255)),
    ]:
        cv2.putText(
            overlay, label_text, (10, legend_y),
            ARCH_FONT, 0.6, color, 2, cv2.LINE_AA,
        )
        legend_y += 25

    cv2.putText(
        overlay, f"Type: {stats.wall_type}",
        (10, legend_y + 5), ARCH_FONT, 0.5,
        (255, 255, 255), 1, cv2.LINE_AA,
    )
    cv2.imwrite(
        str(out_dir / "edge_analysis.jpg"), overlay,
        [cv2.IMWRITE_JPEG_QUALITY, 90],
    )
