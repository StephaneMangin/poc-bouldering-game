#!/usr/bin/env python3
"""Numbered corner sheets and merge of matched corners into the triangulation input.

Usage:
  correspondences.py sheets PHOTOS_DIR OUTPUT_DIR CORNERS_JSON [CORNERS_JSON ...]
  correspondences.py merge MATCHES_JSON OUTPUT_LABELS_JSON CORNERS_JSON [CORNERS_JSON ...]

CORNERS_JSON: photo_corners.json from labeler.html or traced_corners.py (unnamed corners
p1…, t1… per photo). MATCHES_JSON names the physical corners seen on several photos:
  {"corners": {"ile_tip_floor": ["image_005.jpg:t42", "image_042.jpg:p7"]}, "floor": ["ile_tip_floor"]}
"merge" writes the labels triangulate.py reads; "sheets" draws every photo with its corner
ids so matches can be read off the pictures.
"""

import json
import sys
from pathlib import Path

import cv2

LABEL_SCALE = 0.6


def load_corners(paths):
    photos = {}
    for path in paths:
        for photo, entry in json.loads(Path(path).read_text())["photos"].items():
            merged = photos.setdefault(photo, {"width": entry["width"], "height": entry["height"], "points": {}, "floor": [], "segments": []})
            merged["points"].update(entry["points"])
            merged["floor"] += entry.get("floor", [])
            merged["segments"] += entry.get("segments", [])
    return photos


def sheets(photos_dir, output, corner_paths):
    output.mkdir(parents=True, exist_ok=True)
    for photo, entry in load_corners(corner_paths).items():
        image = cv2.imread(str(photos_dir / photo))
        if image is None:
            raise FileNotFoundError(photos_dir / photo)
        for a, b in entry["segments"]:
            cv2.line(image, pixel(entry["points"][a]), pixel(entry["points"][b]), (255, 200, 0), 1, cv2.LINE_AA)
        for name, uv in entry["points"].items():
            colour = (0, 220, 0) if name in entry["floor"] else (0, 0, 255)
            cv2.circle(image, pixel(uv), 4, colour, -1)
            for thickness, text_colour in ((3, (0, 0, 0)), (1, (0, 255, 255))):
                cv2.putText(image, name, (pixel(uv)[0] + 5, pixel(uv)[1] - 5), cv2.FONT_HERSHEY_SIMPLEX, LABEL_SCALE, text_colour, thickness)
        cv2.imwrite(str(output / f"ids_{Path(photo).stem}.png"), image)


def pixel(uv):
    return int(round(uv[0])), int(round(uv[1]))


def merge(matches_path, output_path, corner_paths):
    photos = load_corners(corner_paths)
    matches = json.loads(Path(matches_path).read_text())
    labels = {"photos": {}, "names": {}}
    for corner, references in matches["corners"].items():
        floor = corner in matches.get("floor", [])
        for reference in references:
            photo, point = reference.split(":")
            entry = photos[photo]
            if point not in entry["points"]:
                raise KeyError(f"{reference}: no such corner")
            target = labels["photos"].setdefault(photo, {"width": entry["width"], "height": entry["height"], "points": {}})
            if corner in target["points"]:
                raise ValueError(f"{corner} matched twice on {photo}")
            target["points"][corner] = entry["points"][point]
            floor |= point in entry["floor"]
        labels["names"][corner] = {"floor": floor}
    Path(output_path).write_text(json.dumps(labels, indent=1))
    print(f"{len(labels['names'])} corners on {len(labels['photos'])} photos")


def main():
    command = sys.argv[1]
    if command == "sheets":
        sheets(Path(sys.argv[2]), Path(sys.argv[3]), sys.argv[4:])
    elif command == "merge":
        merge(sys.argv[2], sys.argv[3], sys.argv[4:])
    else:
        raise SystemExit(__doc__)


if __name__ == "__main__":
    main()
