"""Text report generation for bloc analysis."""
from __future__ import annotations

import math
from pathlib import Path

from scene.models import Bloc, EdgeStats
from scene.utils import polygon_edges


def write_bloc_report(
    bloc: Bloc,
    out_dir: Path,
    edge_stats: EdgeStats | None,
) -> None:
    """Write a text summary for the bloc."""
    lines: list[str] = [
        f"# {bloc.label}",
        f"Structure: {bloc.structure_label}",
        f"Aire: {bloc.area_m2:.1f} m\u00b2",
        f"Hauteur: {bloc.height_m:.1f} m",
        f"Centroïde: ({bloc.centroid_xz[0]:.1f}, {bloc.centroid_xz[1]:.1f})",
        f"Photos: {len(bloc.photos)}",
        "",
        "## Géométrie du plan (footprint)",
        f"Sommets: {len(bloc.footprint_xz)}",
    ]

    edges = polygon_edges(bloc.footprint_tuples)
    lines.append("Arêtes (par longueur décroissante):")
    for i, ((x1, z1), (x2, z2), length) in enumerate(edges):
        angle = math.degrees(math.atan2(z2 - z1, x2 - x1))
        lines.append(
            f"  {i + 1}. {length:.2f} m  "
            f"({x1:.1f},{z1:.1f})\u2192({x2:.1f},{z2:.1f})  "
            f"azimut={angle:.0f}\u00b0",
        )

    if edge_stats:
        lines.extend([
            "",
            "## Analyse des arêtes (photos)",
            f"Lignes verticales: {edge_stats.vertical_count}",
            f"Lignes horizontales: {edge_stats.horizontal_count}",
            f"Lignes angulées: {edge_stats.angled_count}",
            f"Angle dominant: {edge_stats.dominant_angle_deg:.1f}\u00b0",
            f"Type de mur: {edge_stats.wall_type}",
        ])

    (out_dir / "rapport.txt").write_text("\n".join(lines), encoding="utf-8")
