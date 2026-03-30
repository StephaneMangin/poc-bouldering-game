"""Architectural plan generation: top, front, and side views."""
from __future__ import annotations

import math
from pathlib import Path

import cv2
import numpy as np

from scene.constants import ARCH_DIM, ARCH_FILL, ARCH_FONT, ARCH_GRID, ARCH_SCALE, ARCH_WALL
from scene.models import Bloc
from scene.utils import polygon_edges


def _draw_grid(
    canvas: np.ndarray,
    origin_px: tuple[int, int],
    width_m: float,
    height_m: float,
) -> None:
    ox, oy = origin_px
    for i in range(int(width_m) + 2):
        x = ox + i * ARCH_SCALE
        if 0 <= x < canvas.shape[1]:
            cv2.line(canvas, (x, 0), (x, canvas.shape[0]), ARCH_GRID, 1)
    for i in range(int(height_m) + 2):
        y = oy - i * ARCH_SCALE
        if 0 <= y < canvas.shape[0]:
            cv2.line(canvas, (0, y), (canvas.shape[1], y), ARCH_GRID, 1)


def _draw_dimension(
    canvas: np.ndarray,
    p1: tuple[int, int],
    p2: tuple[int, int],
    text: str,
    offset: int = 20,
) -> None:
    mx = (p1[0] + p2[0]) // 2
    my = (p1[1] + p2[1]) // 2
    cv2.line(canvas, p1, p2, ARCH_DIM, 1, cv2.LINE_AA)
    cv2.line(canvas, (p1[0] - 3, p1[1] - 3), (p1[0] + 3, p1[1] + 3), ARCH_DIM, 2)
    cv2.line(canvas, (p2[0] - 3, p2[1] - 3), (p2[0] + 3, p2[1] + 3), ARCH_DIM, 2)
    cv2.putText(canvas, text, (mx - 15, my - offset), ARCH_FONT, 0.4, ARCH_DIM, 1, cv2.LINE_AA)


def draw_top_view(bloc: Bloc, out_dir: Path) -> Path:
    """Draw the floor plan polygon (vue de dessus)."""
    pts = bloc.footprint_tuples
    xs = [p[0] for p in pts]
    zs = [p[1] for p in pts]
    min_x, max_x = min(xs), max(xs)
    min_z, max_z = min(zs), max(zs)
    w_m = max_x - min_x
    h_m = max_z - min_z

    margin = 80
    canvas_w = int(w_m * ARCH_SCALE) + 2 * margin
    canvas_h = int(h_m * ARCH_SCALE) + 2 * margin
    canvas = np.full((canvas_h, canvas_w, 3), 255, dtype=np.uint8)

    _draw_grid(canvas, (margin, canvas_h - margin), w_m, h_m)

    poly_px = []
    for x, z in pts:
        px = int((x - min_x) * ARCH_SCALE) + margin
        py = int((max_z - z) * ARCH_SCALE) + margin
        poly_px.append((px, py))

    poly_np = np.array(poly_px, dtype=np.int32)
    cv2.fillPoly(canvas, [poly_np], ARCH_FILL)
    cv2.polylines(canvas, [poly_np], True, ARCH_WALL, 2, cv2.LINE_AA)

    edges = polygon_edges(pts)
    for (x1, z1), (x2, z2), length in edges[:6]:
        px1 = int((x1 - min_x) * ARCH_SCALE) + margin
        py1 = int((max_z - z1) * ARCH_SCALE) + margin
        px2 = int((x2 - min_x) * ARCH_SCALE) + margin
        py2 = int((max_z - z2) * ARCH_SCALE) + margin
        mx = (px1 + px2) // 2
        my = (py1 + py2) // 2
        cv2.putText(canvas, f"{length:.1f}m", (mx - 15, my - 8), ARCH_FONT, 0.35, ARCH_DIM, 1, cv2.LINE_AA)

    cv2.putText(canvas, f"VUE DE DESSUS - {bloc.label}", (10, 20), ARCH_FONT, 0.5, ARCH_WALL, 1, cv2.LINE_AA)
    cv2.putText(canvas, f"Aire: {bloc.area_m2:.1f} m2", (10, canvas_h - 10), ARCH_FONT, 0.4, ARCH_DIM, 1, cv2.LINE_AA)

    arrow_x = canvas_w - 40
    cv2.arrowedLine(canvas, (arrow_x, 50), (arrow_x, 20), ARCH_WALL, 2, tipLength=0.4)
    cv2.putText(canvas, "N", (arrow_x - 5, 65), ARCH_FONT, 0.4, ARCH_WALL, 1)

    path = out_dir / "plan_dessus.png"
    cv2.imwrite(str(path), canvas)
    return path


def draw_front_view(
    bloc: Bloc,
    out_dir: Path,
    wall_angle_deg: float = 90.0,
) -> Path:
    """Draw front elevation (vue de face) — longest edge as the facing wall."""
    edges = polygon_edges(bloc.footprint_tuples)
    front_edge = edges[0]
    wall_width_m = front_edge[2]
    wall_height_m = bloc.height_m

    margin = 80
    canvas_w = int(wall_width_m * ARCH_SCALE) + 2 * margin
    canvas_h = int(wall_height_m * ARCH_SCALE) + 2 * margin
    canvas = np.full((canvas_h, canvas_w, 3), 255, dtype=np.uint8)

    _draw_grid(canvas, (margin, canvas_h - margin), wall_width_m, wall_height_m)

    base_y = canvas_h - margin
    top_y = base_y - int(wall_height_m * ARCH_SCALE)

    if abs(wall_angle_deg - 90.0) < 5:
        pts_wall = np.array([
            [margin, top_y],
            [margin + int(wall_width_m * ARCH_SCALE), top_y],
            [margin + int(wall_width_m * ARCH_SCALE), base_y],
            [margin, base_y],
        ], dtype=np.int32)
    else:
        angle_rad = math.radians(wall_angle_deg)
        top_offset = int(wall_height_m / math.tan(angle_rad) * ARCH_SCALE)
        pts_wall = np.array([
            [margin + top_offset, top_y],
            [margin + int(wall_width_m * ARCH_SCALE) + top_offset, top_y],
            [margin + int(wall_width_m * ARCH_SCALE), base_y],
            [margin, base_y],
        ], dtype=np.int32)

    cv2.fillPoly(canvas, [pts_wall], ARCH_FILL)
    cv2.polylines(canvas, [pts_wall], True, ARCH_WALL, 2, cv2.LINE_AA)

    _draw_dimension(
        canvas,
        (margin, base_y + 30),
        (margin + int(wall_width_m * ARCH_SCALE), base_y + 30),
        f"{wall_width_m:.1f} m", offset=5,
    )
    _draw_dimension(
        canvas, (margin - 30, base_y), (margin - 30, top_y),
        f"{wall_height_m:.1f} m", offset=-15,
    )

    if abs(wall_angle_deg - 90.0) >= 5:
        cv2.putText(
            canvas, f"Inclinaison: {wall_angle_deg:.0f} deg",
            (margin + 10, top_y + 20), ARCH_FONT, 0.4, (0, 0, 200), 1, cv2.LINE_AA,
        )

    cv2.putText(canvas, f"VUE DE FACE - {bloc.label}", (10, 20), ARCH_FONT, 0.5, ARCH_WALL, 1, cv2.LINE_AA)
    cv2.line(canvas, (0, base_y), (canvas_w, base_y), (0, 150, 0), 2, cv2.LINE_AA)

    path = out_dir / "plan_face.png"
    cv2.imwrite(str(path), canvas)
    return path


def draw_side_view(
    bloc: Bloc,
    out_dir: Path,
    wall_angle_deg: float = 90.0,
) -> Path:
    """Draw side elevation — depth from perpendicular to longest edge."""
    edges = polygon_edges(bloc.footprint_tuples)
    front_p1, front_p2, _ = edges[0]

    dx = front_p2[0] - front_p1[0]
    dz = front_p2[1] - front_p1[1]
    edge_len = math.sqrt(dx * dx + dz * dz)
    nx = -dz / edge_len
    nz = dx / edge_len

    projections = [
        (x - front_p1[0]) * nx + (z - front_p1[1]) * nz
        for x, z in bloc.footprint_tuples
    ]

    depth_m = max(max(projections) - min(projections), 0.5)
    wall_height_m = bloc.height_m

    margin = 80
    canvas_w = int(depth_m * ARCH_SCALE) + 2 * margin
    canvas_h = int(wall_height_m * ARCH_SCALE) + 2 * margin
    canvas = np.full((canvas_h, canvas_w, 3), 255, dtype=np.uint8)

    _draw_grid(canvas, (margin, canvas_h - margin), depth_m, wall_height_m)

    base_y = canvas_h - margin
    top_y = base_y - int(wall_height_m * ARCH_SCALE)

    if abs(wall_angle_deg - 90.0) < 5:
        pts_wall = np.array([
            [margin, top_y],
            [margin + int(depth_m * ARCH_SCALE), top_y],
            [margin + int(depth_m * ARCH_SCALE), base_y],
            [margin, base_y],
        ], dtype=np.int32)
    else:
        angle_rad = math.radians(wall_angle_deg)
        top_offset = int(wall_height_m / math.tan(angle_rad) * ARCH_SCALE)
        pts_wall = np.array([
            [margin + top_offset, top_y],
            [margin + int(depth_m * ARCH_SCALE), top_y],
            [margin + int(depth_m * ARCH_SCALE), base_y],
            [margin, base_y],
        ], dtype=np.int32)

    cv2.fillPoly(canvas, [pts_wall], ARCH_FILL)
    cv2.polylines(canvas, [pts_wall], True, ARCH_WALL, 2, cv2.LINE_AA)

    _draw_dimension(
        canvas,
        (margin, base_y + 30),
        (margin + int(depth_m * ARCH_SCALE), base_y + 30),
        f"{depth_m:.1f} m", offset=5,
    )
    _draw_dimension(
        canvas, (margin - 30, base_y), (margin - 30, top_y),
        f"{wall_height_m:.1f} m", offset=-15,
    )

    cv2.putText(canvas, f"VUE DE COTE - {bloc.label}", (10, 20), ARCH_FONT, 0.5, ARCH_WALL, 1, cv2.LINE_AA)
    cv2.line(canvas, (0, base_y), (canvas_w, base_y), (0, 150, 0), 2, cv2.LINE_AA)

    path = out_dir / "plan_cote.png"
    cv2.imwrite(str(path), canvas)
    return path
