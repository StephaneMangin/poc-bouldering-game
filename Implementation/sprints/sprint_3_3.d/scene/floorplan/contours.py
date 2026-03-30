"""Contour extraction: threshold, morphology, approximation, coordinate transform."""
from __future__ import annotations

import logging

import cv2
import numpy as np

from scene.constants import BUILDING_DEPTH_M, BUILDING_WIDTH_M
from scene.models import Vertex

log = logging.getLogger(__name__)


class ContourProcessor:
    """Threshold, morphology, contour extraction, and coordinate transform."""

    MORPH_KERNEL_SIZE = 3
    MORPH_OPEN_ITERATIONS = 1
    MORPH_CLOSE_ITERATIONS = 0
    MIN_AREA_M2 = 0.3
    APPROX_EPSILON_RATIO = 0.002
    BORDER_CLEAR_PX = 4
    EDGE_MARGIN_PX = 20

    def __init__(self, img_shape: tuple[int, int]) -> None:
        self.img_h, self.img_w = img_shape
        self.scale_x = BUILDING_WIDTH_M / self.img_w
        self.scale_z = BUILDING_DEPTH_M / self.img_h

    def extract_contours(self, gray: np.ndarray) -> list[np.ndarray]:
        """Binary threshold, morphological cleanup, find external contours."""
        b = self.BORDER_CLEAR_PX
        gray[:b, :] = 255
        gray[-b:, :] = 255
        gray[:, :b] = 255
        gray[:, -b:] = 255

        _, binary = cv2.threshold(gray, 128, 255, cv2.THRESH_BINARY_INV)

        kernel = cv2.getStructuringElement(
            cv2.MORPH_RECT, (self.MORPH_KERNEL_SIZE, self.MORPH_KERNEL_SIZE),
        )
        opened = cv2.morphologyEx(binary, cv2.MORPH_OPEN, kernel, iterations=self.MORPH_OPEN_ITERATIONS)
        closed = cv2.morphologyEx(opened, cv2.MORPH_CLOSE, kernel, iterations=self.MORPH_CLOSE_ITERATIONS)

        contours, _ = cv2.findContours(closed, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        log.info("Found %d raw contours", len(contours))
        return list(contours)

    def approximate_and_filter(
        self, contours: list[np.ndarray],
    ) -> list[tuple[np.ndarray, float]]:
        """Simplify contours and filter by minimum area."""
        min_area_px = self.MIN_AREA_M2 / (self.scale_x * self.scale_z)

        results: list[tuple[np.ndarray, float]] = []
        for contour in contours:
            area_px = cv2.contourArea(contour)
            if area_px < min_area_px:
                continue
            perimeter = cv2.arcLength(contour, closed=True)
            epsilon = self.APPROX_EPSILON_RATIO * perimeter
            approx = cv2.approxPolyDP(contour, epsilon, closed=True)
            if len(approx) < 3:
                continue
            results.append((approx, area_px))

        results.sort(key=lambda item: item[1], reverse=True)
        log.info("Kept %d contours after filtering (min %.1f m²)", len(results), self.MIN_AREA_M2)
        return results

    def compute_origin(
        self, contours_with_area: list[tuple[np.ndarray, float]],
    ) -> tuple[float, float]:
        """Origin = centroid of the largest contour not touching image edges."""
        margin = self.EDGE_MARGIN_PX
        for approx, _ in contours_with_area:
            pts = approx.reshape(-1, 2)
            touches_edge = (
                np.any(pts[:, 0] <= margin)
                or np.any(pts[:, 0] >= self.img_w - margin)
                or np.any(pts[:, 1] <= margin)
                or np.any(pts[:, 1] >= self.img_h - margin)
            )
            if not touches_edge:
                cx = float(np.mean(pts[:, 0]))
                cy = float(np.mean(pts[:, 1]))
                log.info("Origin: centroid of interior structure at px (%.0f, %.0f)", cx, cy)
                return cx, cy

        cx, cy = self.img_w / 2.0, self.img_h / 2.0
        log.warning("No interior structure found — using image center as origin")
        return cx, cy

    def px_to_meters(
        self, px_x: float, px_y: float, origin: tuple[float, float],
    ) -> Vertex:
        """Convert pixel coords to meters (image Y↓ → Unity Z↑)."""
        x_m = (px_x - origin[0]) * self.scale_x
        z_m = (origin[1] - px_y) * self.scale_z
        return Vertex(x=round(x_m, 2), z=round(z_m, 2))
