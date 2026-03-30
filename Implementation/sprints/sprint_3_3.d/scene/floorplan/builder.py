"""Structure building: contours → Blocs → Structures (adjacency grouping)."""
from __future__ import annotations

import logging
from collections import Counter, defaultdict

import numpy as np

from scene.constants import DEFAULT_WALL_HEIGHT_M
from scene.models import Bloc, Structure, Vertex

from .contours import ContourProcessor
from .markers import RedMarkerExtractor

log = logging.getLogger(__name__)


class StructureBuilder:
    """Groups contours into Blocs, then Blocs into Structures by adjacency."""

    ISLAND_BLOCS = {
        "Quinquin", "Cyrano", "Entre Deux", "Oublié", "Bloc à Pic",
        "Tortue", "Point and Clic", "Dodécaèdre",
    }
    SPINNAKER_BLOCS = {"Spinaker Nord", "Spinaker Sud"}

    ADJACENCY_TOLERANCE_PX = RedMarkerExtractor.SNAP_RADIUS_PX

    def __init__(self, processor: ContourProcessor) -> None:
        self._proc = processor

    def build_blocs(
        self,
        contours_with_area: list[tuple[np.ndarray, float]],
        origin: tuple[float, float],
        label_overrides: dict[int, list[str]] | None = None,
    ) -> list[Bloc]:
        """Convert each contour to a Bloc with meter coordinates."""
        blocs: list[Bloc] = []
        for idx, (approx, _area_px) in enumerate(contours_with_area):
            pts = approx.reshape(-1, 2)

            vertices = [
                self._proc.px_to_meters(float(pt[0]), float(pt[1]), origin)
                for pt in pts
            ]
            cx = sum(v.x for v in vertices) / len(vertices)
            cz = sum(v.z for v in vertices) / len(vertices)
            centroid = Vertex(x=round(cx, 2), z=round(cz, 2))

            label = f"bloc_{idx:02d}"
            if label_overrides and idx in label_overrides:
                label = " / ".join(label_overrides[idx])

            blocs.append(Bloc(
                id=f"bloc_{idx:02d}",
                label=label,
                height_m=DEFAULT_WALL_HEIGHT_M,
                footprint_xz=vertices,
                centroid=centroid,
            ))
        return blocs

    def build_structures(
        self,
        contours_with_area: list[tuple[np.ndarray, float]],
        blocs: list[Bloc],
    ) -> list[Structure]:
        """Group blocs by adjacency and build Structure objects."""
        groups = self._group_by_adjacency(contours_with_area, blocs)

        structures: list[Structure] = []
        for group_indices in groups:
            group_blocs = [blocs[i] for i in group_indices]
            struct_id, struct_label = self._name_structure(group_blocs)
            structures.append(Structure(id=struct_id, label=struct_label, blocs=group_blocs))

        self._deduplicate_ids(structures)
        return structures

    # ── Private helpers ──────────────────────────────────────────────

    def _group_by_adjacency(
        self,
        contours_with_area: list[tuple[np.ndarray, float]],
        blocs: list[Bloc],
    ) -> list[list[int]]:
        """Union-Find grouping: blocs whose contours touch form one structure."""
        n = len(blocs)
        parent = list(range(n))

        def find(x: int) -> int:
            while parent[x] != x:
                parent[x] = parent[parent[x]]
                x = parent[x]
            return x

        def union(a: int, b: int) -> None:
            ra, rb = find(a), find(b)
            if ra != rb:
                parent[ra] = rb

        tol = self.ADJACENCY_TOLERANCE_PX
        for i in range(n):
            for j in range(i + 1, n):
                if self._contours_touch(contours_with_area[i][0], contours_with_area[j][0], tol):
                    union(i, j)
                    log.debug("Blocs touch: %s ↔ %s", blocs[i].label, blocs[j].label)

        groups: dict[int, list[int]] = defaultdict(list)
        for i in range(n):
            groups[find(i)].append(i)

        result = sorted(groups.values(), key=lambda g: sum(blocs[i].area_m2 for i in g), reverse=True)
        log.info("Adjacency grouping: %d blocs → %d structures", n, len(result))
        return result

    @staticmethod
    def _contours_touch(
        contour_a: np.ndarray, contour_b: np.ndarray, tolerance_px: float,
    ) -> bool:
        """True if any vertex of a is within tolerance of any vertex of b."""
        pts_a = contour_a.reshape(-1, 2).astype(np.float32)
        pts_b = contour_b.reshape(-1, 2).astype(np.float32)
        for pa in pts_a:
            if np.min(np.linalg.norm(pts_b - pa, axis=1)) < tolerance_px:
                return True
        return False

    def _name_structure(self, group_blocs: list[Bloc]) -> tuple[str, str]:
        """Determine structure id/label from its blocs' names or positions."""
        labels = {b.label for b in group_blocs}

        if labels & self.ISLAND_BLOCS:
            return "island", "Island"
        if labels & self.SPINNAKER_BLOCS:
            return "spinnaker", "Spinnaker"

        avg_z = sum(b.centroid.z for b in group_blocs) / len(group_blocs)
        if avg_z >= 0:
            return "mur_peripherique_nord", "Mur périphérique Nord"
        return "mur_peripherique_sud", "Mur périphérique Sud"

    @staticmethod
    def _deduplicate_ids(structures: list[Structure]) -> None:
        """Append _a, _b suffixes when multiple structures share an ID."""
        id_counts = Counter(s.id for s in structures)
        id_seq: dict[str, int] = {}
        for struct in structures:
            if id_counts[struct.id] > 1:
                seq = id_seq.get(struct.id, 0)
                id_seq[struct.id] = seq + 1
                struct.id = f"{struct.id}_{chr(ord('a') + seq)}"
