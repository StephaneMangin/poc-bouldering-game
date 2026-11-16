#!/usr/bin/env python3
"""Corners and edges of facet drawings traced over photos (flat colours, blue outlines).

Usage:
  traced_corners.py OUTPUT_JSON OUTPUT_DIR PHOTO=DRAWING [PHOTO=DRAWING ...]

Each DRAWING (e.g. image_005_clean.jpeg) is traced over PHOTO with the same framing. Facets
are the coloured regions between blue lines; their outline corners, merged where facets meet,
give corners t1, t2… in PHOTO pixels and the facet sides give segments. Writes the
photo_corners.json format of labeler.html and one numbered picture per drawing.
"""

import json
import sys
from pathlib import Path

import cv2
import numpy as np

MIN_FACET_AREA = 300
OUTLINE_TOLERANCE = 4.0
MERGE_DISTANCE = 14.0


def read(path):
    image = cv2.imread(str(path))
    if image is None:
        raise FileNotFoundError(path)
    return image


def facet_outlines(drawing):
    """Polygon of every coloured region enclosed by blue lines (white background excluded)."""
    blue, green, red = (drawing[..., i].astype(int) for i in range(3))
    lines = (blue > 150) & (red < 110) & (green < 130) & (blue - green > 70)
    background = (blue > 225) & (green > 225) & (red > 225)
    regions = cv2.erode((~lines & ~background).astype(np.uint8), np.ones((3, 3), np.uint8))
    count, labels, stats, _ = cv2.connectedComponentsWithStats(regions, connectivity=4)
    outlines = []
    for label in range(1, count):
        if stats[label, cv2.CC_STAT_AREA] < MIN_FACET_AREA:
            continue
        contours, _ = cv2.findContours((labels == label).astype(np.uint8), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
        outlines.append(cv2.approxPolyDP(max(contours, key=cv2.contourArea), OUTLINE_TOLERANCE, True).reshape(-1, 2).astype(float))
    return outlines


def merge_corners(outlines):
    """Facet corners closer than MERGE_DISTANCE become one corner placed at their mean."""
    points = np.vstack(outlines)
    centres, members, index_of = [], [], {}
    for i, point in enumerate(points):
        distances = [np.linalg.norm(point - centre) for centre in centres]
        if distances and min(distances) < MERGE_DISTANCE:
            cluster = int(np.argmin(distances))
            members[cluster].append(i)
            centres[cluster] = points[members[cluster]].mean(axis=0)
        else:
            cluster = len(centres)
            centres.append(point.copy())
            members.append([i])
        index_of[i] = cluster
    return dict(enumerate(centres)), index_of


def traced(photo_path, drawing_path):
    photo, drawing = read(photo_path), read(drawing_path)
    scale = np.array([photo.shape[1] / drawing.shape[1], photo.shape[0] / drawing.shape[0]])
    outlines = facet_outlines(drawing)
    corners, index_of = merge_corners(outlines)
    names = {root: f"t{i + 1}" for i, root in enumerate(sorted(corners, key=lambda r: (corners[r][1], corners[r][0])))}
    segments, offset = set(), 0
    for outline in outlines:
        ids = [names[index_of[offset + k]] for k in range(len(outline))]
        segments |= {tuple(sorted((a, b))) for a, b in zip(ids, ids[1:] + ids[:1]) if a != b}
        offset += len(outline)
    points = {names[root]: (corners[root] * scale).round(1).tolist() for root in corners}
    return {"width": photo.shape[1], "height": photo.shape[0], "points": points, "floor": [], "segments": sorted(map(list, segments))}


def draw(photo_path, entry, output):
    image = read(photo_path)
    for a, b in entry["segments"]:
        cv2.line(image, tuple(int(v) for v in entry["points"][a]), tuple(int(v) for v in entry["points"][b]), (255, 200, 0), 2)
    for name, uv in entry["points"].items():
        position = tuple(int(v) for v in uv)
        cv2.circle(image, position, 6, (0, 0, 255), -1)
        cv2.putText(image, name, (position[0] + 6, position[1] - 6), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 0), 4)
        cv2.putText(image, name, (position[0] + 6, position[1] - 6), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)
    cv2.imwrite(str(output / f"traced_{Path(photo_path).stem}.png"), image)


def main():
    output_json, output_dir = Path(sys.argv[1]), Path(sys.argv[2])
    output_dir.mkdir(parents=True, exist_ok=True)
    photos = {}
    for pair in sys.argv[3:]:
        photo_path, drawing_path = (Path(part) for part in pair.split("="))
        photos[photo_path.name] = traced(photo_path, drawing_path)
        draw(photo_path, photos[photo_path.name], output_dir)
        print(f"{photo_path.name}: {len(photos[photo_path.name]['points'])} corners, {len(photos[photo_path.name]['segments'])} segments")
    output_json.write_text(json.dumps({"version": 2, "photos": photos}, indent=1))


if __name__ == "__main__":
    main()
