#!/usr/bin/env python3
"""Focal length and orientation of a photo from the hall's three orthogonal line directions.

Usage:
  vanishing.py PHOTO OUTPUT_PNG [--ignore X0,Y0,X1,Y1 ...]

Line segments (LSD) vote for vanishing points. The vertical one is found in the image first;
then, for each focal length, the two horizontal directions orthogonal to it and to each other
are swept in heading, and the focal and heading gathering the most segment length win.
The direction closest to the image's up becomes the vertical (the sweep may elect a
horizontal one). Prints the focal (pixels), the vertical field of view and the three
directions in the camera frame (OpenCV: x right, y down, z forward): d1 and d2 horizontal,
up. Draws the segments of each direction in red, green and blue. --ignore drops segments
inside a rectangle (text printed over the photo).
"""

import json
import sys

import cv2
import numpy as np

MIN_SEGMENT = 25
ANGLE_TOLERANCE_DEG = 1.5
NEAR_VERTICAL_DEG = 30
VERTICAL_SAMPLES = 4000
FOCAL_RATIOS = np.linspace(0.4, 1.8, 141)
HEADINGS_DEG = np.arange(0, 90, 0.25)
COLOURS = [(0, 0, 255), (0, 200, 0), (255, 0, 0)]


def segments(image, ignored):
    grey = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    found = cv2.createLineSegmentDetector().detect(grey)[0].reshape(-1, 4)
    lengths = np.hypot(found[:, 2] - found[:, 0], found[:, 3] - found[:, 1])
    keep = lengths >= MIN_SEGMENT
    for x0, y0, x1, y1 in ignored:
        inside = (np.minimum(found[:, 0], found[:, 2]) >= x0) & (np.maximum(found[:, 0], found[:, 2]) <= x1)
        keep &= ~(inside & (np.minimum(found[:, 1], found[:, 3]) >= y0) & (np.maximum(found[:, 1], found[:, 3]) <= y1))
    return found[keep], lengths[keep]


def support(lines, lengths, vanishing):
    """Segment length pointing at a homogeneous vanishing point, and which segments do."""
    midpoints = (lines[:, :2] + lines[:, 2:]) / 2
    towards = vanishing[:2] - midpoints * vanishing[2]
    along = lines[:, 2:] - lines[:, :2]
    cosine = np.abs(np.sum(towards * along, axis=1)) / (np.linalg.norm(towards, axis=1) * np.linalg.norm(along, axis=1) + 1e-12)
    inliers = cosine > np.cos(np.radians(ANGLE_TOLERANCE_DEG))
    return lengths[inliers].sum(), inliers


def vertical_vanishing_point(lines, lengths, size):
    """Intersection of near vertical segments that most of them agree with.

    A camera tilted less than 35 degrees sees it above or below the picture, never inside: this
    rejects the point where lines running away from the camera meet."""
    along = lines[:, 2:] - lines[:, :2]
    steep = np.abs(along[:, 0]) < np.tan(np.radians(NEAR_VERTICAL_DEG)) * np.abs(along[:, 1])
    homogeneous = np.cross(np.c_[lines[:, :2], np.ones(len(lines))], np.c_[lines[:, 2:], np.ones(len(lines))])
    rng = np.random.default_rng(0)
    best = (0, None, None)
    for a, b in rng.choice(np.nonzero(steep)[0], size=(VERTICAL_SAMPLES, 2)):
        candidate = np.cross(homogeneous[a], homogeneous[b])
        if a == b or np.linalg.norm(candidate) < 1e-12:
            continue
        candidate /= np.linalg.norm(candidate)
        if abs(candidate[2]) > 1e-9 and abs(candidate[1] / candidate[2] - size[1] / 2) < size[1]:
            continue
        score, inliers = support(lines[steep], lengths[steep], candidate)
        if score > best[0]:
            best = (score, candidate, lines[steep][inliers])
    return best[1], best[2]


def vertical_segments(path, ignored=()):
    """Segments of a photo pointing at its vertical vanishing point (upright edges of the hall)."""
    image = cv2.imread(str(path))
    lines, lengths = segments(image, ignored)
    _, inliers = vertical_vanishing_point(lines, lengths, (image.shape[1], image.shape[0]))
    return inliers


def camera_matrix(focal, size):
    return np.array([[focal, 0, size[0] / 2], [0, focal, size[1] / 2], [0, 0, 1]])


def manhattan_frame(lines, lengths, size, vertical):
    """Focal and the two horizontal directions orthogonal to the vertical vanishing point."""
    best = (0, None, None)
    for ratio in FOCAL_RATIOS:
        focal = ratio * max(size)
        matrix = camera_matrix(focal, size)
        up = np.linalg.solve(matrix, vertical)
        up = up / np.linalg.norm(up) * (-1 if up[1] > 0 else 1)
        first = np.cross(up, [0.0, 0.0, 1.0])
        first /= np.linalg.norm(first)
        second = np.cross(up, first)
        for heading in np.radians(HEADINGS_DEG):
            d1 = np.cos(heading) * first + np.sin(heading) * second
            d2 = np.cross(up, d1)
            score = support(lines, lengths, matrix @ d1)[0] + support(lines, lengths, matrix @ d2)[0]
            if score > best[0]:
                best = (score, focal, np.array([d1, d2, up]))
    return best[1], upright(best[2])


def upright(frame):
    """Reorders a Manhattan frame so its last direction is the one closest to the image's up."""
    vertical = int(np.argmax(np.abs(frame[:, 1])))
    up = frame[vertical] * (-1 if frame[vertical, 1] > 0 else 1)
    first = next(frame[i] for i in range(3) if i != vertical)
    return np.array([first, np.cross(up, first), up])


def main():
    image = cv2.imread(sys.argv[1])
    size = (image.shape[1], image.shape[0])
    ignored = [[float(v) for v in sys.argv[i + 1].split(",")] for i, arg in enumerate(sys.argv) if arg == "--ignore"]
    lines, lengths = segments(image, ignored)
    vertical, _ = vertical_vanishing_point(lines, lengths, size)
    focal, frame = manhattan_frame(lines, lengths, size, vertical)
    matrix = camera_matrix(focal, size)
    drawn = image.copy()
    for direction, colour in zip(frame, COLOURS):
        _, inliers = support(lines, lengths, matrix @ direction)
        for x1, y1, x2, y2 in lines[inliers]:
            cv2.line(drawn, (int(x1), int(y1)), (int(x2), int(y2)), colour, 2)
    cv2.imwrite(sys.argv[2], drawn)
    fov = np.degrees(2 * np.arctan(size[1] / 2 / focal))
    print(json.dumps({"focal_px": round(float(focal), 1), "vertical_fov": round(float(fov), 1),
                      "frame_in_camera": np.round(frame, 4).tolist()}))


if __name__ == "__main__":
    main()
