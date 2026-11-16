#!/usr/bin/env python3
"""Fit the camera of a reference photo on the gym wireframe and draw the overlay.

Usage:
  photomatch.py WIREFRAME_JSON CALIBRATION_JSON PHOTOS_DIR OUTPUT_DIR [--grid]

CALIBRATION_JSON lists, per photo, picked pixels of recognisable gym points:
  {"photos": [{"file": "image_005.jpg", "points": [
      {"uv": [312, 540], "ref": "island:7:3"},          # structure id : ring level : vertex
      {"uv": [100, 200], "world": [1.0, 0.0, 2.0]}       # or explicit game coordinates
  ]}]}
Game (Unity) coordinates are left handed (x east, y up, z north); x is mirrored for OpenCV.
The focal length is searched so the reprojection error is smallest. With --grid, a labelled
pixel grid is drawn on a copy of each photo to help picking points.
Run with the repository .venv python (numpy, OpenCV, Pillow).
"""

import json
import sys
from pathlib import Path

import cv2
import numpy as np

FOCAL_RANGE = np.linspace(0.45, 2.2, 120)
COLORS = {"ring": (255, 220, 0), "corner": (0, 200, 255), "hall": (255, 0, 255), "mast": (0, 0, 255)}


def to_cv(point):
    return np.array([-point[0], point[1], point[2]], dtype=np.float64)


def world_point(spec, wireframe):
    if "world" in spec:
        return spec["world"]
    structure_id, level, vertex = spec["ref"].split(":")
    structure = next(s for s in wireframe["structures"] if s["id"] == structure_id)
    ring = structure["rings"][int(level)]
    return ring[int(vertex) % len(ring)]


def fit_camera(object_points, image_points, size):
    """Best pose over a sweep of focal lengths (in units of the image height)."""
    best = None
    for focal_ratio in FOCAL_RANGE:
        focal = focal_ratio * size[1]
        matrix = np.array([[focal, 0, size[0] / 2], [0, focal, size[1] / 2], [0, 0, 1]], dtype=np.float64)
        ok, rvec, tvec = cv2.solvePnP(object_points, image_points, matrix, None, flags=cv2.SOLVEPNP_ITERATIVE)
        if not ok:
            continue
        projected, _ = cv2.projectPoints(object_points, rvec, tvec, matrix, None)
        error = float(np.sqrt(np.mean(np.sum((projected.reshape(-1, 2) - image_points) ** 2, axis=1))))
        in_front = (cv2.Rodrigues(rvec)[0] @ object_points.T + tvec).T[:, 2].min() > 0
        if in_front and (best is None or error < best[0]):
            best = (error, matrix, rvec, tvec)
    return best


def camera_in_game(rvec, tvec, matrix, size):
    rotation = cv2.Rodrigues(rvec)[0]
    position = -rotation.T @ tvec.reshape(3)
    forward = rotation.T @ np.array([0.0, 0.0, 1.0])
    unmirror = np.array([-1.0, 1.0, 1.0])
    fov = float(np.degrees(2 * np.arctan(size[1] / 2 / matrix[1, 1])))
    return {"position": (position * unmirror).round(3).tolist(), "target": ((position + forward * 5) * unmirror).round(3).tolist(), "fov": round(fov, 1)}


def project(points, rvec, tvec, matrix):
    cv_points = np.array([to_cv(p) for p in points])
    rotation = cv2.Rodrigues(rvec)[0]
    depth = (rotation @ cv_points.T + tvec).T[:, 2]
    projected, _ = cv2.projectPoints(cv_points, rvec, tvec, matrix, None)
    return projected.reshape(-1, 2), depth


def draw_polyline(image, points, closed, color, pose):
    pixels, depth = project(points, *pose)
    count = len(points)
    for i in range(count if closed else count - 1):
        j = (i + 1) % count
        if depth[i] > 0.2 and depth[j] > 0.2:
            cv2.line(image, tuple(int(v) for v in pixels[i]), tuple(int(v) for v in pixels[j]), color, 2, cv2.LINE_AA)


def overlay(image, wireframe, pose):
    for structure in wireframe["structures"]:
        for ring in structure["rings"]:
            draw_polyline(image, ring, True, COLORS["ring"], pose)
        for column in structure["corners"]:
            draw_polyline(image, column, False, COLORS["corner"], pose)
    draw_polyline(image, wireframe["hallFloor"], True, COLORS["hall"], pose)
    draw_polyline(image, wireframe["hallCeiling"], True, COLORS["hall"], pose)
    if wireframe.get("mast"):
        draw_polyline(image, wireframe["mast"], False, COLORS["mast"], pose)


def grid(image, step=25):
    copy = image.copy()
    for x in range(0, copy.shape[1], step):
        cv2.line(copy, (x, 0), (x, copy.shape[0]), (255, 255, 255) if x % 100 == 0 else (128, 128, 128), 1)
        if x % 100 == 0:
            cv2.putText(copy, str(x), (x + 2, 12), cv2.FONT_HERSHEY_SIMPLEX, 0.35, (255, 255, 0), 1)
    for y in range(0, copy.shape[0], step):
        cv2.line(copy, (0, y), (copy.shape[1], y), (255, 255, 255) if y % 100 == 0 else (128, 128, 128), 1)
        if y % 100 == 0:
            cv2.putText(copy, str(y), (2, y - 2), cv2.FONT_HERSHEY_SIMPLEX, 0.35, (255, 255, 0), 1)
    return copy


def main():
    wireframe = json.loads(Path(sys.argv[1]).read_text())
    calibration = json.loads(Path(sys.argv[2]).read_text())
    photos, output = Path(sys.argv[3]), Path(sys.argv[4])
    output.mkdir(parents=True, exist_ok=True)
    results = {}
    for photo in calibration["photos"]:
        image = cv2.imread(str(photos / photo["file"]))
        size = (image.shape[1], image.shape[0])
        if "--grid" in sys.argv:
            cv2.imwrite(str(output / f"grid_{Path(photo['file']).stem}.png"), grid(image))
        if len(photo.get("points", [])) < 6:
            continue
        object_points = np.array([to_cv(world_point(p, wireframe)) for p in photo["points"]])
        image_points = np.array([p["uv"] for p in photo["points"]], dtype=np.float64)
        error, matrix, rvec, tvec = fit_camera(object_points, image_points, size)
        pose = (rvec, tvec, matrix)
        drawn = image.copy()
        overlay(drawn, wireframe, pose)
        reprojected, _ = cv2.projectPoints(object_points, rvec, tvec, matrix, None)
        for picked, back in zip(image_points, reprojected.reshape(-1, 2)):
            cv2.circle(drawn, tuple(int(v) for v in picked), 5, (0, 255, 0), 2)
            cv2.circle(drawn, tuple(int(v) for v in back), 3, (0, 0, 255), -1)
        cv2.putText(drawn, f"rms {error:.1f}px", (10, size[1] - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)
        cv2.imwrite(str(output / f"overlay_{Path(photo['file']).stem}.png"), drawn)
        results[photo["file"]] = {"rms_px": round(error, 2), **camera_in_game(rvec, tvec, matrix, size)}
    print(json.dumps(results, indent=1))


main()
