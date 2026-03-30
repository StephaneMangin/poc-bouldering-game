"""Unified constants for the scene reconstruction pipeline.

Consolidates paths, building dimensions, drawing constants, and
edge detection parameters used across subpackages.
"""
from __future__ import annotations

from pathlib import Path

import cv2

# ── Paths ────────────────────────────────────────────────────────────

SCENE_DIR = Path(__file__).resolve().parent.parent
IMAGES_DIR = SCENE_DIR / "images"
ANALYSIS_DIR = SCENE_DIR / "analysis"
JSON_PATH = SCENE_DIR / "gym_blockout.json"

# Floor plan extraction paths
PNG_PATH = SCENE_DIR / "original_floor_plan.png"
RASTER_PATH = SCENE_DIR / "floor_plan_extracted.png"
OUT_JSON = JSON_PATH  # alias
PREVIEW_PATH = SCENE_DIR / "sectors_preview.png"
ANNOTATIONS_PATH = SCENE_DIR / "sector_annotations.json"

# ── Building dimensions ──────────────────────────────────────────────

BUILDING_WIDTH_M = 40.0
BUILDING_DEPTH_M = 35.0
CEILING_HEIGHT_M = 7.0
DEFAULT_WALL_HEIGHT_M = 5.0

# ── Drawing constants (architectural plans) ──────────────────────────

ARCH_SCALE = 80  # pixels per meter
ARCH_BG = (255, 255, 255)
ARCH_WALL = (40, 40, 40)
ARCH_FILL = (230, 240, 250)
ARCH_GRID = (220, 220, 220)
ARCH_DIM = (100, 100, 200)
ARCH_FONT = cv2.FONT_HERSHEY_SIMPLEX
CONTACT_THUMB = 320  # thumbnail size in contact sheet

# ── Edge detection parameters ────────────────────────────────────────

EDGE_CANNY_LOW = 50
EDGE_CANNY_HIGH = 150
HOUGH_THRESHOLD = 80
HOUGH_MIN_LEN = 60
HOUGH_MAX_GAP = 15
