"""Unified domain models for the scene reconstruction pipeline.

Contains all data structures shared across subpackages:
- Floor plan extraction (Vertex, Bloc, Structure)
- Bloc analysis (EdgeStats)
- Wireframe panel detection (Panel, WireframeResult, Segment)
"""
from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import numpy as np

# ── Type aliases ─────────────────────────────────────────────────────

Segment = tuple[tuple[int, int], tuple[int, int]]


# ── Floor plan models ────────────────────────────────────────────────


@dataclass
class Vertex:
    x: float
    z: float

    def to_dict(self) -> dict[str, float]:
        return {"x": round(self.x, 2), "z": round(self.z, 2)}


@dataclass
class Bloc:
    """A single climbing wall bloc (panel section).

    Used by both floor plan extraction (geometry) and analysis (photos).
    """

    id: str
    label: str
    height_m: float
    footprint_xz: list[Vertex]
    centroid: Vertex
    faces: list[dict[str, Any]] = field(default_factory=list)
    features: list[dict[str, Any]] = field(default_factory=list)
    # Enriched fields (populated during analysis)
    slug: str = ""
    structure_label: str = ""
    structure_slug: str = ""
    photos: list[Path] = field(default_factory=list)

    @property
    def area_m2(self) -> float:
        """Signed area via shoelace formula (absolute value)."""
        pts = self.footprint_xz
        n = len(pts)
        if n < 3:
            return 0.0
        area = sum(
            pts[i].x * pts[(i + 1) % n].z - pts[(i + 1) % n].x * pts[i].z
            for i in range(n)
        )
        return abs(area) / 2.0

    @property
    def centroid_xz(self) -> tuple[float, float]:
        """Centroid as a plain tuple (x, z)."""
        return (self.centroid.x, self.centroid.z)

    @property
    def footprint_tuples(self) -> list[tuple[float, float]]:
        """Footprint as a list of (x, z) tuples."""
        return [(v.x, v.z) for v in self.footprint_xz]

    def to_dict(self) -> dict[str, Any]:
        result: dict[str, Any] = {
            "id": self.id,
            "label": self.label,
            "height_m": self.height_m,
            "area_m2": round(self.area_m2, 1),
            "centroid_xz": self.centroid.to_dict(),
            "footprint_xz": [v.to_dict() for v in self.footprint_xz],
        }
        if self.faces:
            result["faces"] = self.faces
        if self.features:
            result["features"] = self.features
        return result


@dataclass
class Structure:
    id: str
    label: str
    blocs: list[Bloc]

    @property
    def area_m2(self) -> float:
        return sum(b.area_m2 for b in self.blocs)

    @property
    def centroid(self) -> Vertex:
        if not self.blocs:
            return Vertex(0.0, 0.0)
        cx = sum(b.centroid.x for b in self.blocs) / len(self.blocs)
        cz = sum(b.centroid.z for b in self.blocs) / len(self.blocs)
        return Vertex(x=round(cx, 2), z=round(cz, 2))

    def to_dict(self) -> dict[str, Any]:
        return {
            "id": self.id,
            "label": self.label,
            "area_m2": round(self.area_m2, 1),
            "centroid_xz": self.centroid.to_dict(),
            "blocs": [b.to_dict() for b in self.blocs],
        }


# ── Analysis models ──────────────────────────────────────────────────


@dataclass
class EdgeStats:
    """Statistics from Hough line analysis."""

    vertical_count: int = 0
    horizontal_count: int = 0
    angled_count: int = 0
    dominant_angle_deg: float = 90.0
    wall_type: str = "vertical"


# ── Wireframe models ─────────────────────────────────────────────────


@dataclass
class Panel:
    """A single colored panel polygon detected on the wall."""

    polygon: np.ndarray
    color_label: int
    color_name: str
    median_bgr: tuple[int, int, int]
    area_px: float
    cluster_id: int


@dataclass
class WireframeResult:
    """Complete wireframe detection result for one image."""

    panels: list[Panel] = field(default_factory=list)
    segments: list[Segment] = field(default_factory=list)
    joints: list[tuple[int, int]] = field(default_factory=list)
    image_shape: tuple[int, int] = (0, 0)


# ── Keep unused imports visible for type checkers ────────────────────
__all__ = [
    "Bloc",
    "EdgeStats",
    "Panel",
    "Segment",
    "Structure",
    "Vertex",
    "WireframeResult",
]
