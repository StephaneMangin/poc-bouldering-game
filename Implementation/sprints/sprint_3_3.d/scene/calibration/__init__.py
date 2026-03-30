"""Camera calibration from single climbing gym photos.

Pipeline: image → line detection → vanishing points → horizon →
camera height, FOV, pitch estimation.

Uses structural cues (panel joints, t-nut grids) and known
panel dimensions (1.20 m × 1.20 m, 20 cm hole spacing).
"""
from __future__ import annotations

from .pipeline import calibrate_image

__all__ = ["calibrate_image"]
