# Sprint 3.3 — Scene Reconstruction (Block'Out Rennes)

## Sprint Objective
Reconstruct the full 3D gym layout from floor plan + photo references.
Produce a structured JSON blockout and per-bloc analysis reports ready for
Unity import.

## Definition of Done
1. Floor plan extracted into `gym_blockout.json` with 4 structures, 32 blocs.
2. Each bloc has meter-scale footprint polygon, height, centroid.
3. All 77 reference photos classified and assigned to blocs.
4. Per-bloc analysis generated: contact sheet, edge analysis, 3 architectural
   plans (top/front/side), text report.
5. Wireframe auto-generation pipeline functional on all source images.

## Deliverables

### 1. Floor plan extraction (`scene/extract_scene/`)
- **Input**: `original_floor_plan.png` (annotated with red labels)
- **Pipeline**: red marker extraction → contour detection → polygon
  approximation → bloc/structure grouping by adjacency → JSON export
- **Output**: `gym_blockout.json` — 4 structures × 32 blocs with footprint
  coordinates in meters (origin = island center, X=east, Z=north)
- **Modules**: `constants.py`, `models.py`, `markers.py`, `contours.py`,
  `builder.py`, `exporter.py`, `crop.py`, `__main__.py`

### 2. Photo classification & organization
- **`classify_images.py`**: OCR scan + color heuristics to auto-detect
  sector/bloc names
- **`copy_images_to_blocs.py`**: Manual classification mapping
  `IMAGE_CLASSIFICATION` → organized into `scene/images/{structure}/{bloc}/`
  via relative symlinks

### 3. Auto wireframe generation (`generate_auto_wired.py`)
- K-means color segmentation (5 clusters, LAB space)
- Wall-color classification via shared `wall_colors.py`
- Contour extraction on wall clusters only → polygon approximation
- Segment deduplication → blue wireframe overlay

### 4. Bloc analysis (`scene/analyze/`)
- **`contact_sheet.py`**: Photo grid per bloc
- **`edge_analysis.py`**: Canny + HoughLinesP on wall-masked regions,
  classifies wall type (vertical / dévers / dalle)
- **`architectural.py`**: Top/front/side SVG-like PNG plans with grid,
  dimension lines, angle annotations
- **`report.py`**: Text summary with geometry, edge stats, wall type

### 5. Edge detection pipeline (`scene/edge_detection_pipeline.py`)
- Multi-channel: LSD (luminance) + Canny-Hue + Canny-Saturation
- Color boundary validation against label map
- Collinear segment merging

### 6. Shared color module (`scene/wall_colors.py`)
- Centralized HSV classification for grey/green/red/yellow panels
- `classify_pixel_hsv()`, `build_wall_mask_hsv()`, `build_label_map()`,
  `classify_kmeans_cluster()`

## Architecture

```
sprint_3_3.d/
├── generate_auto_wired.py      # Wireframe pipeline
├── copy_images_to_blocs.py     # Photo → bloc symlinks
├── images/                     # 77 source photos + auto_wired outputs
├── scene/
│   ├── wall_colors.py          # Shared HSV color classification
│   ├── edge_detection_pipeline.py  # Multi-channel boundary detection
│   ├── classify_images.py      # OCR-based auto-classification
│   ├── gym_blockout.json       # Extracted scene data
│   ├── original_floor_plan.png
│   ├── extract_scene/          # Plan → JSON pipeline (8 modules)
│   ├── analyze/                # Per-bloc analysis (6 modules)
│   │   ├── models.py
│   │   ├── loader.py
│   │   ├── contact_sheet.py
│   │   ├── edge_analysis.py
│   │   ├── architectural.py
│   │   └── report.py
│   ├── images/                 # Symlinks organized by structure/bloc
│   └── analysis/               # Generated outputs (32 bloc reports)
└── sprint_3_3.md               # This file
```

## Key Numbers
| Metric | Value |
|---|---|
| Structures | 4 (island, mur nord, mur sud, spinnaker) |
| Blocs | 32 |
| Source photos | 77 |
| Classified photos | 65 (12 skipped: recaps, plans, duplicates) |
| Reports generated | 32 (1 per bloc) |
| Building envelope | 40 × 35 m, ceiling 7 m |
| Python LoC (pipeline) | ~3100 |

## Status: COMPLETE
All deliverables produced. Ready for Unity blockout import (Sprint 4+).
