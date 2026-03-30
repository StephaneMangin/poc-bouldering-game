"""Panel edge detection via multi-layer gradient fusion.

Detects climbing wall panel boundaries by fusing four edge layers,
then applies a Sobel color-gradient confirm+boost post-processing step:

1. **GSV Canny**: guided-filter-smoothed Grayscale + Saturation + Value channels
   with Canny edge detection — captures strong inter-color boundaries (P≈96%)
2. **Adaptive threshold**: Gaussian adaptive threshold on smoothed grayscale —
   captures subtle same-color panel joints
3. **LSD on S-channel**: Line Segment Detector on bilateral-smoothed Saturation —
   detects structural color transitions as clean line segments (P≈93%)
4. **Weak-zone boost**: lower-threshold GSV for the top of the image where
   distant panels have low contrast

**Post-processing — Sobel color confirm+boost**:
- *Confirm*: remove pipeline edges that are **not** corroborated by a
  Sobel color-gradient edge (Gaussian k=7, Canny 40/120, dilated 7px).
  This eliminates false-positive textures/noise.
- *Boost*: add high-confidence Sobel color edges (Canny 65/195) that
  are **not** already covered by the pipeline.  This adds true-positive
  panel edges the base layers may have missed.

All layers are masked to the wall region (HSV-based wall mask) and
fused into line segments via HoughLinesP.
"""
from __future__ import annotations

import cv2
import numpy as np

from scene.colors import build_wall_mask_hsv
from scene.models import Segment, WireframeResult

# ── Parameters ───────────────────────────────────────────────────────

# Guided filter: (radius, eps_factor) — eps = factor * 255²
_GUIDED_RADIUS = 8
_GUIDED_EPS_FACTOR = 0.02

# Bilateral filter (still used for layers 2-4)
_BILAT_STRONG = (11, 75, 75)
_BILAT_LIGHT = (9, 60, 60)
_BILAT_WEAK = (7, 50, 50)

# Canny thresholds
_CANNY_STRONG = (40, 120)
_CANNY_WEAK = (25, 75)

# Adaptive threshold
_ADAPT_BLOCK = 21
_ADAPT_C = 5

# LSD minimum segment length (pixels)
_LSD_MIN_LENGTH = 20

# Weak-zone: top fraction of image where lower thresholds apply
_WEAK_ZONE_TOP = 0.15

# HoughLinesP parameters
_HOUGH_THRESHOLD = 10
_HOUGH_MIN_LENGTH = 8
_HOUGH_MAX_GAP = 15

# Small connected components to filter out (pixels)
_MIN_COMPONENT_SIZE = 10

# Sobel color confirm+boost parameters
_SOBEL_GAUSS_K = 7
_SOBEL_CONF_CANNY = (40, 120)  # confirmation: moderate threshold
_SOBEL_CONF_DILATE = 7  # dilation radius for confirmation match
_SOBEL_BOOST_CANNY = (65, 195)  # boost: high-confidence only
_SOBEL_BOOST_EXCL = 7  # exclusion radius around existing edges


def _gsv_canny_guided(
    gray: np.ndarray,
    s_ch: np.ndarray,
    v_ch: np.ndarray,
    canny_params: tuple[int, int],
    *,
    radius: int = _GUIDED_RADIUS,
    eps_factor: float = _GUIDED_EPS_FACTOR,
) -> np.ndarray:
    """Fuse Canny edges from guided-filtered Gray, Saturation, Value."""
    lo, hi = canny_params
    eps = int(eps_factor * 255 * 255)
    sg = cv2.ximgproc.guidedFilter(gray, gray, radius, eps)
    ss_ = cv2.ximgproc.guidedFilter(s_ch, s_ch, radius, eps)
    sv = cv2.ximgproc.guidedFilter(v_ch, v_ch, radius, eps)
    return cv2.bitwise_or(
        cv2.bitwise_or(cv2.Canny(sg, lo, hi), cv2.Canny(ss_, lo, hi)),
        cv2.Canny(sv, lo, hi),
    )


def _gsv_canny(
    gray: np.ndarray,
    s_ch: np.ndarray,
    v_ch: np.ndarray,
    bilat_params: tuple[int, int, int],
    canny_params: tuple[int, int],
) -> np.ndarray:
    """Fuse Canny edges from bilateral-filtered Gray, Saturation, Value."""
    d, sc, ss = bilat_params
    lo, hi = canny_params
    sg = cv2.bilateralFilter(gray, d, sc, ss)
    ss_ = cv2.bilateralFilter(s_ch, d, sc, ss)
    sv = cv2.bilateralFilter(v_ch, d, sc, ss)
    return cv2.bitwise_or(
        cv2.bitwise_or(cv2.Canny(sg, lo, hi), cv2.Canny(ss_, lo, hi)),
        cv2.Canny(sv, lo, hi),
    )


def _sobel_color_magnitude(
    img: np.ndarray,
    gauss_k: int = _SOBEL_GAUSS_K,
) -> np.ndarray:
    """Compute per-channel Sobel gradient magnitude (max across BGR).

    Returns a normalized uint8 magnitude image (0–255).
    """
    blurred = cv2.GaussianBlur(img, (gauss_k, gauss_k), 0)
    h, w = blurred.shape[:2]
    mag = np.zeros((h, w), dtype=np.float32)
    for c in range(3):
        sx = cv2.Sobel(blurred[:, :, c], cv2.CV_32F, 1, 0, ksize=3)
        sy = cv2.Sobel(blurred[:, :, c], cv2.CV_32F, 0, 1, ksize=3)
        mag = np.maximum(mag, np.sqrt(sx * sx + sy * sy))
    return np.clip(mag * 255.0 / mag.max(), 0, 255).astype(np.uint8)


def _sobel_confirm_boost(
    edge_map: np.ndarray,
    sobel_mag: np.ndarray,
) -> np.ndarray:
    """Confirm pipeline edges with Sobel color gradient and boost recall.

    1. **Confirm**: keep only pipeline edges corroborated by a moderate-
       threshold Sobel edge within ``_SOBEL_CONF_DILATE`` pixels.
    2. **Boost**: add high-confidence Sobel edges that are **not**
       already covered by the confirmed pipeline edges.
    """
    # Confirmation: Canny on Sobel magnitude at moderate threshold
    lo_c, hi_c = _SOBEL_CONF_CANNY
    sob_conf = cv2.Canny(sobel_mag, lo_c, hi_c)
    dk = np.ones((_SOBEL_CONF_DILATE, _SOBEL_CONF_DILATE), dtype=np.uint8)
    confirmed = cv2.bitwise_and(edge_map, cv2.dilate(sob_conf, dk))

    # Boost: high-confidence Sobel edges not already in confirmed map
    lo_b, hi_b = _SOBEL_BOOST_CANNY
    sob_boost = cv2.Canny(sobel_mag, lo_b, hi_b)
    ek = np.ones((_SOBEL_BOOST_EXCL, _SOBEL_BOOST_EXCL), dtype=np.uint8)
    already_covered = cv2.dilate(confirmed, ek)
    new_edges = cv2.bitwise_and(sob_boost, cv2.bitwise_not(already_covered))

    return cv2.bitwise_or(confirmed, new_edges)


def _build_edge_map(img: np.ndarray, wall: np.ndarray) -> np.ndarray:
    """Build multi-layer edge map masked to the wall region.

    Returns a binary edge image (uint8, 0 or 255).
    """
    h, w = img.shape[:2]
    hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    s_ch = hsv[:, :, 1]
    v_ch = hsv[:, :, 2]

    # Layer 1: GSV Canny (guided filter + strong Canny)
    layer1 = cv2.bitwise_and(
        _gsv_canny_guided(gray, s_ch, v_ch, _CANNY_STRONG), wall,
    )

    # Layer 2: Adaptive threshold (captures subtle same-color joints)
    gray_bilat = cv2.bilateralFilter(gray, *_BILAT_LIGHT)
    adapt = cv2.adaptiveThreshold(
        gray_bilat, 255,
        cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY_INV,
        _ADAPT_BLOCK, _ADAPT_C,
    )
    layer2 = cv2.bitwise_and(adapt, wall)

    # Layer 3: LSD on S-channel (structural color transitions)
    s_bilat = cv2.bilateralFilter(s_ch, *_BILAT_STRONG)
    lsd = cv2.createLineSegmentDetector(cv2.LSD_REFINE_STD)
    lines_s, _, _, _ = lsd.detect(s_bilat)
    layer3 = np.zeros((h, w), dtype=np.uint8)
    if lines_s is not None:
        for line in lines_s:
            x1, y1, x2, y2 = line[0]
            if np.hypot(x2 - x1, y2 - y1) < _LSD_MIN_LENGTH:
                continue
            mx = int(np.clip((x1 + x2) / 2, 0, w - 1))
            my = int(np.clip((y1 + y2) / 2, 0, h - 1))
            if wall[my, mx] == 0:
                continue
            cv2.line(layer3, (int(x1), int(y1)), (int(x2), int(y2)), 255, 2)

    # Layer 4: Weak-zone boost (top of image, lower thresholds)
    weak = _gsv_canny(gray, s_ch, v_ch, _BILAT_WEAK, _CANNY_WEAK)
    top_zone = np.zeros((h, w), dtype=np.uint8)
    top_zone[: int(h * _WEAK_ZONE_TOP)] = 255
    layer4 = cv2.bitwise_and(cv2.bitwise_and(weak, wall), top_zone)

    fused = cv2.bitwise_or(
        cv2.bitwise_or(layer1, layer2),
        cv2.bitwise_or(layer3, layer4),
    )

    # Post-processing: Sobel color confirm + boost
    sobel_mag = _sobel_color_magnitude(img)
    return _sobel_confirm_boost(fused, sobel_mag)


def _filter_small_components(
    thin: np.ndarray,
    min_size: int = _MIN_COMPONENT_SIZE,
) -> np.ndarray:
    """Remove connected components smaller than *min_size* pixels."""
    n_labels, labels = cv2.connectedComponents(thin)
    filtered = np.zeros_like(thin)
    for i in range(1, n_labels):
        if np.sum(labels == i) >= min_size:
            filtered[labels == i] = 255
    return filtered


def _extract_segments(edge_map: np.ndarray) -> list[Segment]:
    """Extract straight line segments from a binary edge map."""
    thin = cv2.ximgproc.thinning(edge_map)
    thin = _filter_small_components(thin)

    lines = cv2.HoughLinesP(
        thin, 1, np.pi / 180,
        threshold=_HOUGH_THRESHOLD,
        minLineLength=_HOUGH_MIN_LENGTH,
        maxLineGap=_HOUGH_MAX_GAP,
    )
    if lines is None:
        return []

    return [
        ((int(l[0][0]), int(l[0][1])), (int(l[0][2]), int(l[0][3])))
        for l in lines
    ]


def detect_silhouette(
    img: np.ndarray,
    **_kwargs: object,
) -> WireframeResult:
    """Detect panel boundary line segments via multi-layer edge fusion.

    Parameters
    ----------
    img : ndarray (H, W, 3) BGR
        Source climbing wall image.

    Returns
    -------
    WireframeResult with segments and joints suitable for
    silhouette-style rendering (blue lines on black canvas).
    """
    h, w = img.shape[:2]
    wall = build_wall_mask_hsv(img)

    edge_map = _build_edge_map(img, wall)
    segments = _extract_segments(edge_map)

    # Collect unique endpoints as joints
    joint_set: set[tuple[int, int]] = set()
    for p1, p2 in segments:
        joint_set.add(p1)
        joint_set.add(p2)

    return WireframeResult(
        panels=[],
        segments=segments,
        joints=sorted(joint_set),
        image_shape=(h, w),
    )
