"""Render wireframe detection results onto images.

Four output modes:
- joints: blue dots at polygon vertices
- wired: blue wireframe lines on original image
- filled: semi-transparent color fill + wireframe overlay
- silhouette: blue polygon outlines on black canvas
"""
from __future__ import annotations

from pathlib import Path

import cv2
import numpy as np

from scene.models import WireframeResult

# ── Drawing style ────────────────────────────────────────────────────

WIRE_COLOR = (255, 0, 0)  # BGR blue
WIRE_THICKNESS = 2
JOINT_COLOR = (255, 0, 0)  # BGR blue
JOINT_RADIUS = 4
FILL_ALPHA = 0.45
JPGQ = [cv2.IMWRITE_JPEG_QUALITY, 92]

# Thin wireframe for 3D reconstruction
_SKEL_COLOR = (180, 0, 0)  # BGR dark blue — raw skeleton (all edges)
_HOUGH_COLOR = (255, 80, 0)  # BGR bright blue — rectified HoughLinesP segments
_THIN_COLOR = (255, 0, 0)  # kept for backward compat
_THIN_THICKNESS = 1
_JUNCTION_COLOR = (0, 200, 255)  # BGR yellow-orange
_JUNCTION_RADIUS = 2
_MIN_COMPONENT_SIZE = 10  # same as silhouette.py


def draw_joints(
    img: np.ndarray,
    result: WireframeResult,
) -> np.ndarray:
    """Draw blue dots at each polygon vertex (joint)."""
    out = img.copy()
    for x, y in result.joints:
        cv2.circle(out, (x, y), JOINT_RADIUS, JOINT_COLOR, -1, cv2.LINE_AA)
    return out


def draw_wired(
    img: np.ndarray,
    result: WireframeResult,
) -> np.ndarray:
    """Draw blue wireframe lines on the original image."""
    out = img.copy()
    for p1, p2 in result.segments:
        cv2.line(out, p1, p2, WIRE_COLOR, WIRE_THICKNESS, cv2.LINE_AA)
    return out


def draw_filled(
    img: np.ndarray,
    result: WireframeResult,
) -> np.ndarray:
    """Draw semi-transparent color fill per panel + wireframe overlay."""
    out = img.copy()
    overlay = img.copy()

    for panel in result.panels:
        fill_color = _saturate_color(panel.median_bgr)
        cv2.fillPoly(overlay, [panel.polygon], fill_color)

    cv2.addWeighted(overlay, FILL_ALPHA, out, 1 - FILL_ALPHA, 0, out)

    for p1, p2 in result.segments:
        cv2.line(out, p1, p2, WIRE_COLOR, WIRE_THICKNESS, cv2.LINE_AA)

    return out


def _saturate_color(bgr: tuple[int, int, int]) -> tuple[int, int, int]:
    """Boost saturation to make the fill color more vivid."""
    pixel = np.array([[list(bgr)]], dtype=np.uint8)
    hsv = cv2.cvtColor(pixel, cv2.COLOR_BGR2HSV)[0, 0]
    hsv[1] = min(255, int(hsv[1] * 1.8))
    hsv[2] = min(255, int(hsv[2] * 1.2))
    result = cv2.cvtColor(
        np.array([[hsv]], dtype=np.uint8), cv2.COLOR_HSV2BGR,
    )[0, 0]
    return (int(result[0]), int(result[1]), int(result[2]))


def draw_silhouette(
    result: WireframeResult,
) -> np.ndarray:
    """Draw blue wireframe on a black canvas.

    Uses line segments when available (edge-based detection),
    falls back to panel polygon outlines (region-based detection).
    """
    h, w = result.image_shape
    canvas = np.zeros((h, w, 3), dtype=np.uint8)
    if result.segments:
        for p1, p2 in result.segments:
            cv2.line(canvas, p1, p2, WIRE_COLOR, WIRE_THICKNESS, cv2.LINE_AA)
    else:
        for panel in result.panels:
            pts = panel.polygon.reshape(-1, 1, 2)
            cv2.polylines(
                canvas, [pts], True,
                WIRE_COLOR, WIRE_THICKNESS, cv2.LINE_AA,
            )
    return canvas


def draw_wireframe_thin(
    edge_map: np.ndarray,
    wall: np.ndarray,
    image_shape: tuple[int, int],
) -> np.ndarray:
    """Render a hybrid wireframe suitable for 3D reconstruction.

    Layer 1 (dark blue): raw thinned skeleton — preserves ALL detected edges.
    Layer 2 (bright blue): HoughLinesP segments — rectified straight lines.
    Layer 3 (yellow-orange dots): geometric intersections between segments.

    *wall* is accepted for API compat but no longer used for masking;
    the edge_map already encodes the pipeline's full output.
    """
    h, w = image_shape

    # Filter small components on thick edges FIRST (same as benchmark),
    # then thin — preserves more content than thin-then-filter.
    n_labels, labels = cv2.connectedComponents(edge_map)
    filtered_thick = np.zeros_like(edge_map)
    for i in range(1, n_labels):
        if np.sum(labels == i) >= _MIN_COMPONENT_SIZE:
            filtered_thick[labels == i] = 255
    skel = cv2.ximgproc.thinning(filtered_thick)

    canvas = np.zeros((h, w, 3), dtype=np.uint8)

    # Layer 1: raw skeleton — all pixels, no loss
    canvas[skel > 0] = _SKEL_COLOR

    # Layer 2: rectified segments via HoughLinesP (overlay)
    lines = cv2.HoughLinesP(
        skel, 1, np.pi / 180,
        threshold=10, minLineLength=8, maxLineGap=15,
    )

    if lines is not None:
        for l in lines:
            p1 = (int(l[0][0]), int(l[0][1]))
            p2 = (int(l[0][2]), int(l[0][3]))
            cv2.line(canvas, p1, p2, _HOUGH_COLOR, _THIN_THICKNESS, cv2.LINE_AA)

    # Layer 3: junctions where ≥3 segment endpoints converge
    if lines is not None:
        long_segs = [
            ((int(l[0][0]), int(l[0][1])), (int(l[0][2]), int(l[0][3])))
            for l in lines
            if np.hypot(l[0][2] - l[0][0], l[0][3] - l[0][1]) >= 40
        ]
        junctions = _find_endpoint_junctions(long_segs, radius=15, min_count=4)
    else:
        junctions = []
    for jx, jy in junctions:
        cv2.circle(
            canvas, (jx, jy), _JUNCTION_RADIUS,
            _JUNCTION_COLOR, -1, cv2.LINE_AA,
        )

    return canvas


def _find_endpoint_junctions(
    segments: list[tuple[tuple[int, int], tuple[int, int]]],
    radius: int = 12,
    min_count: int = 3,
    min_angle_diversity: float = 25.0,
) -> list[tuple[int, int]]:
    """Find junctions where endpoints of segments with distinct angles converge.

    Collects all endpoints, clusters them, then checks that the cluster
    contains endpoints from segments whose angles differ by at least
    *min_angle_diversity*° — filtering false clusters from parallel
    overlapping segments.
    """
    if not segments:
        return []

    # Precompute angle per segment (0-180°)
    seg_angles = []
    for (x1, y1), (x2, y2) in segments:
        seg_angles.append(np.degrees(np.arctan2(y2 - y1, x2 - x1)) % 180)

    # Collect endpoints with their segment index
    pts: list[tuple[int, int]] = []
    seg_idx: list[int] = []
    for i, ((x1, y1), (x2, y2)) in enumerate(segments):
        pts.append((x1, y1))
        seg_idx.append(i)
        pts.append((x2, y2))
        seg_idx.append(i)

    arr = np.array(pts, dtype=np.float32)
    used = np.zeros(len(arr), dtype=bool)
    junctions: list[tuple[int, int]] = []

    for i in range(len(arr)):
        if used[i]:
            continue
        dists = np.sqrt(np.sum((arr - arr[i]) ** 2, axis=1))
        cluster = np.where((dists <= radius) & (~used))[0]
        if len(cluster) < min_count:
            used[i] = True
            continue

        # Check angle diversity: at least 2 distinct directions
        cluster_angles = {seg_idx[c]: seg_angles[seg_idx[c]] for c in cluster}
        unique_angles = list(cluster_angles.values())
        has_diversity = False
        for ai in range(len(unique_angles)):
            for aj in range(ai + 1, len(unique_angles)):
                diff = abs(unique_angles[ai] - unique_angles[aj])
                diff = min(diff, 180 - diff)
                if diff >= min_angle_diversity:
                    has_diversity = True
                    break
            if has_diversity:
                break

        if has_diversity:
            centroid = arr[cluster].mean(axis=0)
            junctions.append((int(centroid[0]), int(centroid[1])))
        used[cluster] = True

    return junctions


_BENCHMARK_TOLERANCE = 3  # pixels for TP/FP/FN matching
_TP_COLOR = (0, 255, 0)    # green — correct detection
_FP_COLOR = (0, 0, 255)    # red   — false positive
_FN_COLOR = (255, 100, 0)  # blue  — missed reference edge
_BG_DIM = 0.3


def draw_benchmark(
    img: np.ndarray,
    edge_map: np.ndarray,
    ref_path: Path | str,
) -> np.ndarray | None:
    """Draw a TP/FP/FN comparison between detected edges and a reference.

    Parameters
    ----------
    img : BGR source image (used as dimmed background).
    edge_map : binary edge map from the pipeline (uint8, 0/255).
    ref_path : path to the manual reference wireframe PNG
               (blue lines = BGR(254,9,0) on black canvas).

    Returns None if *ref_path* does not exist.
    """
    ref_path = Path(ref_path)
    if not ref_path.is_file():
        return None

    ref = cv2.imread(str(ref_path))
    if ref is None:
        return None

    h, w = img.shape[:2]

    # Extract reference skeleton (blue channel > 100 → thin)
    ref_bin = (ref[:, :, 0] > 100).astype(np.uint8) * 255
    skel_ref = cv2.ximgproc.thinning(ref_bin)

    # Thin + filter detected edges
    n_labels, labels = cv2.connectedComponents(edge_map)
    det = np.zeros_like(edge_map)
    for i in range(1, n_labels):
        if np.sum(labels == i) >= 10:
            det[labels == i] = 255
    det = cv2.ximgproc.thinning(det)

    # Dilate for tolerance matching
    k = 2 * _BENCHMARK_TOLERANCE + 1
    kern = np.ones((k, k), np.uint8)
    det_dil = cv2.dilate(det, kern)
    ref_dil = cv2.dilate(skel_ref, kern)

    tp_mask = (det > 0) & (ref_dil > 0)
    fp_mask = (det > 0) & (ref_dil == 0)
    fn_mask = (skel_ref > 0) & (det_dil == 0)

    # Scores
    skel_pts = np.where(skel_ref > 0)
    total_ref = len(skel_pts[0])
    det_pts = np.where(det > 0)
    total_det = len(det_pts[0])
    recall = np.sum(det_dil[skel_pts] > 0) / total_ref * 100 if total_ref else 0
    precision = np.sum(ref_dil[det_pts] > 0) / total_det * 100 if total_det else 0
    f1 = 2 * recall * precision / (recall + precision) if (recall + precision) else 0

    # Compose
    comp = (img.astype(np.float32) * _BG_DIM).astype(np.uint8)
    comp[tp_mask] = _TP_COLOR
    comp[fp_mask] = _FP_COLOR
    comp[fn_mask] = _FN_COLOR

    font = cv2.FONT_HERSHEY_SIMPLEX
    cv2.putText(comp, "GREEN = correct (TP)", (10, 25), font, 0.6, _TP_COLOR, 2)
    cv2.putText(comp, "RED = false positive (FP)", (10, 50), font, 0.6, _FP_COLOR, 2)
    cv2.putText(comp, "BLUE = missed ref (FN)", (10, 75), font, 0.6, _FN_COLOR, 2)
    cv2.putText(
        comp,
        f"R={recall:.0f}% P={precision:.0f}% F1={f1:.0f} (+/-{_BENCHMARK_TOLERANCE}px)",
        (10, h - 15), font, 0.6, (255, 255, 255), 2,
    )

    return comp


def save_outputs(
    img: np.ndarray,
    result: WireframeResult,
    out_dir: Path,
    stem: str,
) -> dict[str, Path]:
    """Render and save all three output images. Returns {mode: path}."""
    out_dir.mkdir(parents=True, exist_ok=True)
    paths: dict[str, Path] = {}

    for mode, draw_fn in [
        ("joints", draw_joints),
        ("wired", draw_wired),
        ("filled", draw_filled),
    ]:
        rendered = draw_fn(img, result)
        path = out_dir / f"{stem}_{mode}.jpg"
        cv2.imwrite(str(path), rendered, JPGQ)
        paths[mode] = path

    return paths
