"""Red marker extraction: dots, OCR text labels, and vertex snapping."""
from __future__ import annotations

import logging
from collections import defaultdict

import cv2
import numpy as np

log = logging.getLogger(__name__)


class RedMarkerExtractor:
    """Extracts red intersection dots and OCR text labels from the floor plan."""

    MERGE_THRESHOLD_PX = 5
    SNAP_RADIUS_PX = 30

    OCR_CORRECTIONS: dict[str, str] = {
        "'ague": "Vagues",
        "\u2018ague": "Vagues",
        "Grille": "La Grille",
        "e Last": "The Last",
    }

    def extract(
        self, color_img: np.ndarray,
    ) -> tuple[list[tuple[int, int]], list[tuple[float, float, str]], np.ndarray]:
        """Extract red dots and text labels from the color image.

        Returns:
            dots: (x, y) pixel coords of intersection markers
            text_labels: (cx, cy, text) for OCR'd red text labels
            red_mask: binary mask of all red pixels
        """
        b, g, r = cv2.split(color_img)
        red_mask = (
            (r.astype(np.int16) > 150)
            & (g.astype(np.int16) < 100)
            & (b.astype(np.int16) < 100)
        ).astype(np.uint8) * 255

        if cv2.countNonZero(red_mask) == 0:
            log.info("No red markers detected")
            return [], [], red_mask

        n_labels, _labels, stats, centroids = cv2.connectedComponentsWithStats(red_mask)
        h, w = color_img.shape[:2]

        dot_positions_raw: list[tuple[int, int]] = []
        text_components: list[tuple[int, float, float, int, int, int]] = []

        for i in range(1, n_labels):
            cx, cy = centroids[i]
            area = stats[i, cv2.CC_STAT_AREA]
            if area <= 2:
                if 5 < cx < w - 5 and 5 < cy < h - 5:
                    dot_positions_raw.append((int(round(cx)), int(round(cy))))
            else:
                text_components.append((
                    i, cx, cy, area,
                    stats[i, cv2.CC_STAT_WIDTH], stats[i, cv2.CC_STAT_HEIGHT],
                ))

        dots = self._merge_nearby_dots(dot_positions_raw)
        text_labels = self._ocr_text_regions(red_mask, text_components, (h, w))

        log.info(
            "Red markers: %d dots (from %d raw), %d text labels",
            len(dots), len(dot_positions_raw), len(text_labels),
        )
        return dots, text_labels, red_mask

    def strip_to_gray(self, color_img: np.ndarray, red_mask: np.ndarray) -> np.ndarray:
        """Replace red pixels with white, then convert to grayscale."""
        clean = color_img.copy()
        clean[red_mask > 0] = [255, 255, 255]
        return cv2.cvtColor(clean, cv2.COLOR_BGR2GRAY)

    def match_labels_to_contours(
        self,
        text_labels: list[tuple[float, float, str]],
        contours_with_area: list[tuple[np.ndarray, float]],
    ) -> dict[int, list[str]]:
        """Match OCR text labels to contours via point-in-polygon.

        Deduplicates near-identical reads and applies OCR corrections.
        """
        log.info("Matching %d red text labels to contours", len(text_labels))
        contour_labels: dict[int, list[str]] = {}
        for lx, ly, raw_name in text_labels:
            name = self.OCR_CORRECTIONS.get(raw_name, raw_name)
            for idx, (approx, _) in enumerate(contours_with_area):
                dist = cv2.pointPolygonTest(approx, (lx, ly), False)
                if dist >= 0:
                    log.info("Label '%s' at (%.0f, %.0f) → contour %d", name, lx, ly, idx)
                    existing = contour_labels.get(idx, [])
                    if not any(name.lower() in e.lower() or e.lower() in name.lower() for e in existing):
                        contour_labels.setdefault(idx, []).append(name)
                    break
            else:
                log.debug("Label '%s' at (%.0f, %.0f) matches no contour", name, lx, ly)
        return contour_labels

    def snap_to_markers(
        self,
        contours_with_area: list[tuple[np.ndarray, float]],
        dots: list[tuple[int, int]],
    ) -> list[tuple[np.ndarray, float]]:
        """Snap vertices to red dots, then insert unmatched dots on edges.

        Pass 1: snap existing vertices to nearest dot within radius.
        Pass 2: insert unmatched dots that lie on polygon edges.
        """
        if not dots:
            return contours_with_area

        radius = self.SNAP_RADIUS_PX
        dot_array = np.array(dots, dtype=np.float32)
        snapped = 0
        consumed_dots: set[int] = set()
        result: list[tuple[np.ndarray, float]] = []

        for approx, area in contours_with_area:
            new_approx = approx.copy()
            pts = new_approx.reshape(-1, 2)
            for i, pt in enumerate(pts):
                dists = np.linalg.norm(dot_array - pt.astype(np.float32), axis=1)
                best = int(np.argmin(dists))
                if dists[best] < radius:
                    pts[i] = dot_array[best].astype(int)
                    consumed_dots.add(best)
                    snapped += 1
            result.append((new_approx, area))

        inserted = 0
        final: list[tuple[np.ndarray, float]] = []
        for approx, area in result:
            pts = list(approx.reshape(-1, 2))
            n = len(pts)
            insertions: list[tuple[int, np.ndarray]] = []

            for dot_idx in range(len(dots)):
                if dot_idx in consumed_dots:
                    continue
                dp = dot_array[dot_idx]
                for edge_i in range(n):
                    a = pts[edge_i].astype(np.float32)
                    b = pts[(edge_i + 1) % n].astype(np.float32)
                    if self._point_to_segment_dist(dp, a, b) < radius:
                        insertions.append((edge_i, dp.astype(int)))
                        consumed_dots.add(dot_idx)
                        break

            if insertions:
                insertions.sort(key=lambda x: x[0], reverse=True)
                for edge_i, new_pt in insertions:
                    pts.insert(edge_i + 1, new_pt)
                    inserted += 1
                approx = np.array(pts).reshape(-1, 1, 2)
                area = float(cv2.contourArea(approx))

            final.append((approx, area))

        log.info(
            "Snapped %d vertices, inserted %d new vertices from red markers (radius=%dpx)",
            snapped, inserted, radius,
        )
        return final

    # ── Private helpers ──────────────────────────────────────────────

    def _merge_nearby_dots(
        self, positions: list[tuple[int, int]],
    ) -> list[tuple[int, int]]:
        """Merge dots within threshold pixels into midpoints."""
        if not positions:
            return []

        threshold = self.MERGE_THRESHOLD_PX
        used: set[int] = set()
        merged: list[tuple[int, int]] = []

        for i, (x1, y1) in enumerate(positions):
            if i in used:
                continue
            group_x, group_y = [x1], [y1]
            used.add(i)
            for j in range(i + 1, len(positions)):
                if j in used:
                    continue
                x2, y2 = positions[j]
                if abs(x2 - x1) <= threshold and abs(y2 - y1) <= threshold:
                    group_x.append(x2)
                    group_y.append(y2)
                    used.add(j)
            merged.append((
                int(round(sum(group_x) / len(group_x))),
                int(round(sum(group_y) / len(group_y))),
            ))
        return merged

    def _ocr_text_regions(
        self,
        red_mask: np.ndarray,
        text_components: list[tuple[int, float, float, int, int, int]],
        img_shape: tuple[int, int],
    ) -> list[tuple[float, float, str]]:
        """OCR red text regions by clustering text components into lines."""
        try:
            import pytesseract
        except ImportError:
            log.warning("pytesseract not installed — skipping red text OCR")
            return []

        if not text_components:
            return []

        y_groups: dict[int, list[tuple[int, float, float, int, int, int]]] = defaultdict(list)
        for comp in text_components:
            y_key = int(comp[2] // 30) * 30
            y_groups[y_key].append(comp)

        text_regions: list[tuple[int, int, int, int]] = []
        for _y_key, comps in sorted(y_groups.items()):
            comps.sort(key=lambda c: c[1])
            clusters: list[list[tuple[int, float, float, int, int, int]]] = [[comps[0]]]
            for j in range(1, len(comps)):
                if comps[j][1] - comps[j - 1][1] < 50:
                    clusters[-1].append(comps[j])
                else:
                    clusters.append([comps[j]])

            for cluster in clusters:
                if len(cluster) < 3:
                    continue
                xs = [c[1] for c in cluster]
                ys = [c[2] for c in cluster]
                text_regions.append((
                    int(min(xs)) - 10, int(min(ys)) - 15,
                    int(max(xs)) + 30, int(max(ys)) + 15,
                ))

        h, w = img_shape
        labels: list[tuple[float, float, str]] = []
        for x1, y1, x2, y2 in text_regions:
            roi = red_mask[max(0, y1):min(h, y2), max(0, x1):min(w, x2)]
            if roi.size == 0:
                continue
            roi_inv = 255 - roi
            padded = cv2.copyMakeBorder(
                roi_inv, 10, 10, 10, 10, cv2.BORDER_CONSTANT, value=255,
            )
            text = pytesseract.image_to_string(
                padded, lang="fra", config="--psm 7",
            ).strip()
            if text:
                labels.append(((x1 + x2) / 2, (y1 + y2) / 2, text))

        return labels

    @staticmethod
    def _point_to_segment_dist(
        p: np.ndarray, a: np.ndarray, b: np.ndarray,
    ) -> float:
        """Perpendicular distance from point *p* to segment *a*–*b*."""
        ab = b - a
        ab_sq = float(np.dot(ab, ab))
        if ab_sq < 1e-9:
            return float(np.linalg.norm(p - a))
        t = float(np.dot(p - a, ab)) / ab_sq
        t = max(0.0, min(1.0, t))
        proj = a + t * ab
        return float(np.linalg.norm(p - proj))
