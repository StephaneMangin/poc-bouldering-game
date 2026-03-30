"""Shared utility functions for the scene pipeline."""
from __future__ import annotations

import math
import re
import unicodedata


def slugify(text: str) -> str:
    """Convert label to filesystem-safe slug (ASCII, lowercase, underscores)."""
    text = unicodedata.normalize("NFKD", text).encode("ascii", "ignore").decode()
    text = re.sub(r"[^\w\s-]", "", text).strip().lower()
    return re.sub(r"[\s-]+", "_", text)


def polygon_edges(
    footprint: list[tuple[float, float]],
) -> list[tuple[tuple[float, float], tuple[float, float], float]]:
    """Return edges as (p1, p2, length_m) sorted by length descending."""
    edges = []
    n = len(footprint)
    for i in range(n):
        p1 = footprint[i]
        p2 = footprint[(i + 1) % n]
        length = math.dist(p1, p2)
        edges.append((p1, p2, length))
    edges.sort(key=lambda e: e[2], reverse=True)
    return edges
