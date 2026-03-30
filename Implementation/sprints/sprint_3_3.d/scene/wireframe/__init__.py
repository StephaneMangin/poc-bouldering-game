"""Wireframe panel detection for climbing wall photos.

Two detection strategies:

- **detect_panels**: K-means segmentation → coarse panel blobs.
  Good for color-fill overlays on the original image.

- **detect_silhouette**: HSV label map + Canny edge splits → fine
  polygon outlines. Good for silhouette wireframes (blue on black).

Usage::

    from scene.wireframe import detect_panels, detect_silhouette

    # Coarse overlay
    result = detect_panels(img_bgr)

    # Fine silhouette wireframe
    result = detect_silhouette(img_bgr)
"""
from scene.models import Panel, Segment, WireframeResult

from .pipeline import detect_panels
from .silhouette import detect_silhouette

__all__ = [
    "Panel",
    "Segment",
    "WireframeResult",
    "detect_panels",
    "detect_silhouette",
]
