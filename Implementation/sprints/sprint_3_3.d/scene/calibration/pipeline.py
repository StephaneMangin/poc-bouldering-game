"""Full camera calibration pipeline.

Chains: line detection → vanishing point estimation → camera parameter
estimation. Produces a CalibrationResult for each image.
"""
from __future__ import annotations

import logging
from dataclasses import dataclass
from pathlib import Path

import cv2
import numpy as np

from .estimators import (
    estimate_camera_height,
    estimate_fov,
    estimate_horizon_y,
    estimate_pitch,
    estimate_scale,
)
from .lines import detect_lines
from .vanishing import find_vanishing_points

log = logging.getLogger(__name__)


@dataclass
class CalibrationResult:
    """Camera calibration estimated from a single photo."""

    filename: str
    image_shape: tuple[int, int]  # (H, W)
    # detected geometry
    n_horizontal: int
    n_vertical: int
    n_angled: int
    vp_horizontal: tuple[float, float] | None
    vp_vertical: tuple[float, float] | None
    horizon_y_px: float
    # estimated camera parameters
    camera_height_m: float
    fov_deg: float
    pitch_deg: float
    px_per_meter: float | None

    def to_dict(self) -> dict:
        return {
            "filename": self.filename,
            "image_size": list(self.image_shape),
            "lines": {
                "horizontal": self.n_horizontal,
                "vertical": self.n_vertical,
                "angled": self.n_angled,
            },
            "vp_horizontal": list(self.vp_horizontal) if self.vp_horizontal else None,
            "vp_vertical": list(self.vp_vertical) if self.vp_vertical else None,
            "horizon_y_px": round(self.horizon_y_px, 1),
            "camera_height_m": round(self.camera_height_m, 2),
            "fov_deg": round(self.fov_deg, 0),
            "pitch_deg": round(self.pitch_deg, 0),
            "px_per_meter": round(self.px_per_meter, 1) if self.px_per_meter else None,
        }

    def summary_line(self) -> str:
        vp_h = f"({self.vp_horizontal[0]:.0f}, {self.vp_horizontal[1]:.0f})" if self.vp_horizontal else "N/A"
        vp_v = f"({self.vp_vertical[0]:.0f}, {self.vp_vertical[1]:.0f})" if self.vp_vertical else "N/A"
        return (
            f"{self.filename:25s} | "
            f"lines H={self.n_horizontal:3d} V={self.n_vertical:3d} A={self.n_angled:3d} | "
            f"VP_h={vp_h:>15s} VP_v={vp_v:>15s} | "
            f"horizon={self.horizon_y_px:6.0f}px | "
            f"height={self.camera_height_m:5.2f}m | "
            f"fov={self.fov_deg:5.0f}° | "
            f"pitch={self.pitch_deg:+5.0f}°"
        )


def calibrate_image(
    img: np.ndarray,
    filename: str = "unknown",
) -> CalibrationResult:
    """Run the full calibration pipeline on a single image.

    Parameters
    ----------
    img : ndarray (H, W, 3) BGR
    filename : str — for identification in output

    Returns
    -------
    CalibrationResult with all estimated parameters.
    """
    h, w = img.shape[:2]

    # 1. detect and classify lines
    horizontal, vertical, angled = detect_lines(img)
    log.info(
        "%s: %d horizontal, %d vertical, %d angled lines",
        filename, len(horizontal), len(vertical), len(angled),
    )

    # 2. find vanishing points
    vp_h, vp_v = find_vanishing_points(horizontal, vertical, (h, w))

    if vp_h is not None:
        log.info("%s: horizontal VP at (%.0f, %.0f)", filename, vp_h[0], vp_h[1])
    if vp_v is not None:
        log.info("%s: vertical VP at (%.0f, %.0f)", filename, vp_v[0], vp_v[1])

    # 3. estimate horizon
    horizon_y = estimate_horizon_y(vp_h, vp_v, (h, w))

    # 4. camera parameters
    camera_y = estimate_camera_height(horizon_y, h)
    fov = estimate_fov(vp_h, (h, w))
    pitch = estimate_pitch(vp_v, (h, w))

    # 5. scale calibration
    all_segments = horizontal + vertical + angled
    px_per_m = estimate_scale(all_segments)

    return CalibrationResult(
        filename=filename,
        image_shape=(h, w),
        n_horizontal=len(horizontal),
        n_vertical=len(vertical),
        n_angled=len(angled),
        vp_horizontal=tuple(vp_h) if vp_h is not None else None,
        vp_vertical=tuple(vp_v) if vp_v is not None else None,
        horizon_y_px=horizon_y,
        camera_height_m=camera_y,
        fov_deg=fov,
        pitch_deg=pitch,
        px_per_meter=px_per_m,
    )


def calibrate_batch(
    images_dir: Path,
    pattern: str = "image_*.jpg",
) -> list[CalibrationResult]:
    """Calibrate all matching images in a directory.

    Also picks up .webp and .jpeg files.
    """
    results: list[CalibrationResult] = []
    extensions = (".jpg", ".jpeg", ".webp", ".png")

    files = sorted(
        f for f in images_dir.iterdir()
        if f.suffix.lower() in extensions
        and not f.stem.endswith("_clean")
        and f.stem.startswith("image_")
    )

    for img_path in files:
        img = cv2.imread(str(img_path))
        if img is None:
            log.warning("Cannot read %s, skipping", img_path.name)
            continue

        result = calibrate_image(img, img_path.name)
        results.append(result)

    return results
