#!/usr/bin/env python3
"""Solve the cameras of labelled photos and the 3D position of the labelled corners.

Usage:
  triangulate.py LABELS_JSON PHOTOS_DIR WIREFRAME_JSON OUTPUT_DIR [--prefix ile] [--structure island] [--anchors ANCHORS_JSON]

LABELS_JSON comes from `correspondences.py merge`: one name marks one physical corner on every
photo, "floor" corners touch the floor. Cameras and corners are reconstructed incrementally
(essential matrix on the photo pair sharing most corners, then PnP for each further photo)
and refined by bundle adjustment. The floor corners give the vertical axis and the floor
height; the top view of the corners named with PREFIX is then fitted on the plan outline of
STRUCTURE (WIREFRAME_JSON from `GymPreview wire`), which gives scale, heading and position;
an elongated outline can match half a turn away, so ANCHORS_JSON ({"corner": [x, z]} in game
coordinates, 2 or more corners at known plan positions) replaces that fit when given.
Writes triangulated.json (game coordinates: x east, y up, z north), one overlay per photo
and a top view. Run with the repository .venv python (numpy, OpenCV).
"""

import json
import sys
from pathlib import Path

import cv2
import numpy as np

FOCAL_RATIOS = np.linspace(0.45, 1.6, 47)
PIXEL_SIGMA = 2.0
HUBER_PIXELS = 4.0
OUTLIER_PIXELS = 8.0
MIN_SHARED = 8
MIN_PNP = 6
MIN_TRIANGULATION_DEG = 2.0
LM_ITERATIONS = 60
JACOBIAN_STEP = 1e-6
ICP_ITERATIONS = 60


class Reconstruction:
    """Photos (cameras), corners (points) and observations; right handed world (x mirrored)."""

    def __init__(self, labels):
        counts = {}
        for photo in labels["photos"].values():
            for name in photo["points"]:
                counts[name] = counts.get(name, 0) + 1
        self.names = sorted(name for name, count in counts.items() if count >= 2)
        self.floor = np.array([labels["names"].get(name, {}).get("floor", False) for name in self.names])
        index = {name: i for i, name in enumerate(self.names)}
        self.photos = [name for name, photo in labels["photos"].items() if sum(n in index for n in photo["points"]) >= MIN_PNP]
        self.sizes = np.array([(labels["photos"][p]["width"], labels["photos"][p]["height"]) for p in self.photos], dtype=float)
        rows = [(i, index[n], uv) for i, p in enumerate(self.photos) for n, uv in labels["photos"][p]["points"].items() if n in index]
        self.obs_photo = np.array([r[0] for r in rows])
        self.obs_point = np.array([r[1] for r in rows])
        self.obs_uv = np.array([r[2] for r in rows], dtype=float)
        self.rvec = np.full((len(self.photos), 3), np.nan)
        self.tvec = np.full((len(self.photos), 3), np.nan)
        self.focal = np.full(len(self.photos), np.nan)
        self.points = np.full((len(self.names), 3), np.nan)

    def registered(self):
        return ~np.isnan(self.focal)

    def triangulated(self):
        return ~np.isnan(self.points[:, 0])

    def usable(self):
        return self.registered()[self.obs_photo] & self.triangulated()[self.obs_point]

    def project(self, rvec, tvec, focal, points, photo_index):
        """Pixels and depths of points seen from the photos photo_index (per photo camera arrays)."""
        rotations = np.array([np.eye(3) if np.isnan(r[0]) else cv2.Rodrigues(r)[0] for r in rvec])
        camera = np.einsum("nij,nj->ni", rotations[photo_index], points) + tvec[photo_index]
        centre = self.sizes[photo_index] / 2
        return focal[photo_index][:, None] * camera[:, :2] / camera[:, 2:3] + centre, camera[:, 2]


def residuals(model, selection, rvec, tvec, focal, points):
    photo = model.obs_photo[selection]
    projected, _ = model.project(rvec, tvec, focal, points[model.obs_point[selection]], photo)
    return ((projected - model.obs_uv[selection]) / PIXEL_SIGMA).ravel()


def reprojection_errors(model):
    selection = np.nonzero(model.usable())[0]
    error = residuals(model, selection, model.rvec, model.tvec, model.focal, model.points).reshape(-1, 2)
    return selection, np.linalg.norm(error, axis=1) * PIXEL_SIGMA


def focal_matrix(model, photo, focal):
    width, height = model.sizes[photo]
    return np.array([[focal, 0, width / 2], [0, focal, height / 2], [0, 0, 1]], dtype=float)


def camera_centre(model, photo):
    rotation = cv2.Rodrigues(model.rvec[photo])[0]
    return -rotation.T @ model.tvec[photo]


def triangulate(model, point):
    """Linear triangulation over every registered view, refused when the views are too close."""
    rows = np.nonzero((model.obs_point == point) & model.registered()[model.obs_photo])[0]
    if len(rows) < 2:
        return np.full(3, np.nan)
    system = []
    for row in rows:
        photo = model.obs_photo[row]
        rotation = cv2.Rodrigues(model.rvec[photo])[0]
        projection = focal_matrix(model, photo, model.focal[photo]) @ np.hstack([rotation, model.tvec[photo][:, None]])
        u, v = model.obs_uv[row]
        system += [u * projection[2] - projection[0], v * projection[2] - projection[1]]
    solution = np.linalg.svd(np.array(system))[2][-1]
    position = solution[:3] / solution[3]
    rays = [position - camera_centre(model, model.obs_photo[row]) for row in rows]
    rays = [ray / np.linalg.norm(ray) for ray in rays]
    widest = max(np.degrees(np.arccos(np.clip(a @ b, -1, 1))) for i, a in enumerate(rays) for b in rays[i + 1:])
    return position if widest >= MIN_TRIANGULATION_DEG else np.full(3, np.nan)


def triangulate_all(model):
    for point in range(len(model.names)):
        model.points[point] = triangulate(model, point)
    _, depth = model.project(model.rvec, model.tvec, model.focal, np.nan_to_num(model.points[model.obs_point]), model.obs_photo)
    behind = model.usable() & (depth <= 0)
    model.points[np.unique(model.obs_point[behind])] = np.nan


def shared(model, a, b):
    in_a = set(model.obs_point[model.obs_photo == a])
    return sorted(p for p in model.obs_point[model.obs_photo == b] if p in in_a)


def best_pair(model):
    pairs = [(len(shared(model, a, b)), a, b) for a in range(len(model.photos)) for b in range(a + 1, len(model.photos))]
    count, a, b = max(pairs)
    if count < MIN_SHARED:
        raise ValueError(f"no photo pair shares {MIN_SHARED} corners (best: {count})")
    return a, b


def observed_uv(model, photo, points):
    rows = {p: i for i, p in enumerate(model.obs_point) if model.obs_photo[i] == photo}
    return np.array([model.obs_uv[rows[p]] for p in points])


def initialise_pair(model, a, b):
    """Relative pose of the first pair, focal ratio picked by the reprojection error."""
    points = shared(model, a, b)
    best = None
    for ratio in FOCAL_RATIOS:
        trial = pair_trial(model, a, b, points, ratio)
        if trial is not None and (best is None or trial[0] < best[0]):
            best = trial
    if best is None:
        raise ValueError("the first photo pair gives no valid relative pose")
    _, rvec, tvec, focal_a, focal_b = best
    model.rvec[a], model.tvec[a], model.focal[a] = np.zeros(3), np.zeros(3), focal_a
    model.rvec[b], model.tvec[b], model.focal[b] = rvec, tvec, focal_b
    triangulate_all(model)


def pair_trial(model, a, b, points, ratio):
    focal_a, focal_b = ratio * model.sizes[a].max(), ratio * model.sizes[b].max()
    normalised_a = (observed_uv(model, a, points) - model.sizes[a] / 2) / focal_a
    normalised_b = (observed_uv(model, b, points) - model.sizes[b] / 2) / focal_b
    essential, mask = cv2.findEssentialMat(normalised_a, normalised_b, np.eye(3), cv2.RANSAC, 0.999, 3.0 / focal_a)
    if essential is None or essential.shape != (3, 3):
        return None
    _, rotation, translation, _ = cv2.recoverPose(essential, normalised_a, normalised_b, np.eye(3), mask=mask)
    rvec_b, tvec_b = cv2.Rodrigues(rotation)[0].ravel(), translation.ravel()
    model.rvec[a], model.tvec[a], model.focal[a] = np.zeros(3), np.zeros(3), focal_a
    model.rvec[b], model.tvec[b], model.focal[b] = rvec_b, tvec_b, focal_b
    triangulate_all(model)
    selection, errors = reprojection_errors(model)
    score = np.sqrt(np.mean(errors ** 2)) if len(selection) >= MIN_SHARED else None
    model.rvec[:], model.tvec[:], model.focal[:], model.points[:] = np.nan, np.nan, np.nan, np.nan
    return None if score is None else (score, rvec_b, tvec_b, focal_a, focal_b)


def register(model, photo):
    """Camera of a further photo from its corners already in 3D (PnP over a focal sweep)."""
    rows = np.nonzero((model.obs_photo == photo) & model.triangulated()[model.obs_point])[0]
    if len(rows) < MIN_PNP:
        return False
    world = model.points[model.obs_point[rows]]
    image = model.obs_uv[rows]
    best = None
    for ratio in FOCAL_RATIOS:
        focal = ratio * model.sizes[photo].max()
        ok, rvec, tvec = cv2.solvePnP(world, image, focal_matrix(model, photo, focal), None, flags=cv2.SOLVEPNP_ITERATIVE)
        if not ok:
            continue
        projected, _ = cv2.projectPoints(world, rvec, tvec, focal_matrix(model, photo, focal), None)
        error = np.sqrt(np.mean(np.sum((projected.reshape(-1, 2) - image) ** 2, axis=1)))
        in_front = ((cv2.Rodrigues(rvec)[0] @ world.T).T + tvec.ravel())[:, 2].min() > 0
        if in_front and (best is None or error < best[0]):
            best = (error, rvec.ravel(), tvec.ravel(), focal)
    if best is None:
        return False
    model.rvec[photo], model.tvec[photo], model.focal[photo] = best[1], best[2], best[3]
    return True


def pack(model, fixed):
    cameras = list(np.nonzero(model.registered())[0])
    points = np.nonzero(model.triangulated())[0]
    blocks = []
    for photo in cameras:
        pose = [] if photo == fixed else [model.rvec[photo], model.tvec[photo]]
        blocks += pose + [np.log([model.focal[photo]])]
    blocks.append(model.points[points].ravel())
    return np.concatenate(blocks), cameras, points


def unpack(model, vector, cameras, points, fixed):
    rvec, tvec, focal, cursor = model.rvec.copy(), model.tvec.copy(), model.focal.copy(), 0
    for photo in cameras:
        if photo != fixed:
            rvec[photo], tvec[photo] = vector[cursor:cursor + 3], vector[cursor + 3:cursor + 6]
            cursor += 6
        focal[photo] = np.exp(vector[cursor])
        cursor += 1
    positions = model.points.copy()
    positions[points] = vector[cursor:].reshape(-1, 3)
    return rvec, tvec, focal, positions


def bundle_adjust(model, fixed):
    """Levenberg-Marquardt on pixel errors (Huber weights), the fixed photo anchoring the pose."""
    vector, cameras, points = pack(model, fixed)
    selection = np.nonzero(model.usable())[0]

    def evaluate(candidate):
        rvec, tvec, focal, positions = unpack(model, candidate, cameras, points, fixed)
        return residuals(model, selection, rvec, tvec, focal, positions)

    damping = 1e-3
    current = evaluate(vector)
    for _ in range(LM_ITERATIONS):
        weights = huber_weights(current)
        jacobian = numeric_jacobian(evaluate, vector, current)
        weighted = jacobian * weights[:, None]
        normal = weighted.T @ jacobian
        gradient = weighted.T @ current
        improved = False
        while damping < 1e8:
            step = np.linalg.solve(normal + damping * np.diag(np.diag(normal) + 1e-9), -gradient)
            candidate = vector + step
            trial = evaluate(candidate)
            if np.sum(huber_weights(trial) * trial ** 2) < np.sum(weights * current ** 2):
                vector, current, damping, improved = candidate, trial, max(damping / 3, 1e-7), True
                break
            damping *= 4
        if not improved or np.linalg.norm(step) < 1e-9:
            break
    model.rvec, model.tvec, model.focal, model.points = unpack(model, vector, cameras, points, fixed)


def huber_weights(values):
    pixel = np.abs(values) * PIXEL_SIGMA
    return np.where(pixel <= HUBER_PIXELS, 1.0, HUBER_PIXELS / np.maximum(pixel, 1e-9))


def numeric_jacobian(evaluate, vector, current):
    jacobian = np.empty((len(current), len(vector)))
    for column in range(len(vector)):
        shifted = vector.copy()
        step = JACOBIAN_STEP * max(1.0, abs(vector[column]))
        shifted[column] += step
        jacobian[:, column] = (evaluate(shifted) - current) / step
    return jacobian


def reconstruct(model):
    first, second = best_pair(model)
    initialise_pair(model, first, second)
    bundle_adjust(model, fixed=first)
    while True:
        waiting = [p for p in range(len(model.photos)) if not model.registered()[p]]
        ranked = sorted(waiting, key=lambda p: -np.sum((model.obs_photo == p) & model.triangulated()[model.obs_point]))
        if not ranked or not register(model, ranked[0]):
            break
        triangulate_all(model)
        bundle_adjust(model, fixed=first)
    return first


def apply_similarity(model, rotation, translation, scale=1.0):
    """World change X' = scale * rotation @ X + translation, cameras kept consistent."""
    model.points = scale * (model.points @ rotation.T) + translation
    for photo in np.nonzero(model.registered())[0]:
        camera_rotation = cv2.Rodrigues(model.rvec[photo])[0] @ rotation.T
        centre = scale * rotation @ camera_centre(model, photo) + translation
        model.rvec[photo] = cv2.Rodrigues(camera_rotation)[0].ravel()
        model.tvec[photo] = -camera_rotation @ centre


def level_on_floor(model):
    """Rotate and shift so the floor corners lie on y = 0 with the cameras above."""
    floor = model.points[model.floor & model.triangulated()]
    if len(floor) < 3:
        raise ValueError(f"{len(floor)} floor corners in 3D, 3 needed to find the floor")
    centroid = floor.mean(axis=0)
    normal = np.linalg.svd(floor - centroid)[2][-1]
    centres = np.array([camera_centre(model, p) for p in np.nonzero(model.registered())[0]])
    if np.mean((centres - centroid) @ normal) < 0:
        normal = -normal
    axis = np.cross(normal, [0.0, 1.0, 0.0])
    angle = np.arccos(np.clip(normal[1], -1, 1))
    rotation = cv2.Rodrigues(axis / max(np.linalg.norm(axis), 1e-12) * angle)[0]
    apply_similarity(model, rotation, -rotation @ centroid)


def plan_outline(wireframe, structure_id):
    """Top view outline of a structure: its widest ring, mirrored into the right handed world."""
    structure = next(s for s in wireframe["structures"] if s["id"] == structure_id)
    rings = [np.array([[-p[0], p[2]] for p in ring]) for ring in structure["rings"]]
    widest = max(rings, key=lambda ring: polygon_area(ring))
    keep = np.linalg.norm(widest - np.roll(widest, -1, axis=0), axis=1) > 1e-6
    return widest[keep]


def polygon_area(ring):
    x, z = ring[:, 0], ring[:, 1]
    return abs(np.dot(x, np.roll(z, 1)) - np.dot(z, np.roll(x, 1))) / 2


def boundary_samples(polygon, count):
    """Points spread evenly along a closed polygon."""
    edges = np.roll(polygon, -1, axis=0) - polygon
    lengths = np.linalg.norm(edges, axis=1)
    positions = np.linspace(0, lengths.sum(), count, endpoint=False)
    starts = np.concatenate([[0], np.cumsum(lengths)[:-1]])
    edge = np.searchsorted(starts, positions, side="right") - 1
    return polygon[edge] + edges[edge] * ((positions - starts[edge]) / lengths[edge])[:, None]


def nearest(points, candidates):
    distances = np.linalg.norm(points[:, None, :] - candidates[None], axis=2)
    return candidates[distances.argmin(axis=1)]


def fit_on_plan(model, prefix, outline, anchors):
    """Scale, heading and position on the plan: from anchor corners when given, else from top views."""
    if len(anchors) >= 2:
        names = [name for name in anchors if name in model.names and model.triangulated()[model.names.index(name)]]
        if len(names) < 2:
            raise ValueError(f"{len(names)} anchor corners in 3D, 2 needed to place the corners on the plan")
        source = np.array([model.points[model.names.index(name)][[0, 2]] for name in names])
        target = np.array([[-anchors[name][0], anchors[name][1]] for name in names])
        scale, rotation2, translation2 = umeyama(source, target)
        error = np.sqrt(np.mean(np.sum((scale * source @ rotation2.T + translation2 - target) ** 2, axis=1)))
    else:
        members = np.array([name.startswith(prefix) for name in model.names]) & model.triangulated()
        if members.sum() < 4:
            raise ValueError(f"{members.sum()} corners named {prefix}* in 3D, 4 needed to place them on the plan")
        fits = [icp_similarity(model.points[members][:, [0, 2]], outline, heading) for heading in np.radians(np.arange(0, 360, 15))]
        error, scale, rotation2, translation2 = min(fits, key=lambda fit: fit[0])
    rotation = np.array([[rotation2[0, 0], 0, rotation2[0, 1]], [0, 1, 0], [rotation2[1, 0], 0, rotation2[1, 1]]])
    apply_similarity(model, rotation, np.array([translation2[0], 0, translation2[1]]), scale)
    return error


def icp_similarity(points, outline, heading):
    """2D similarity between the convex top views of the corners and of the plan outline.

    Matches run both ways so the corners cannot shrink onto one stretch of the outline."""
    source = boundary_samples(cv2.convexHull(points.astype(np.float32)).reshape(-1, 2).astype(float), 200)
    target = boundary_samples(cv2.convexHull(outline.astype(np.float32)).reshape(-1, 2).astype(float), 200)
    rotation = np.array([[np.cos(heading), -np.sin(heading)], [np.sin(heading), np.cos(heading)]])
    scale = np.sqrt(polygon_area(target) / max(polygon_area(source), 1e-9))
    translation = target.mean(axis=0) - scale * rotation @ source.mean(axis=0)
    for _ in range(ICP_ITERATIONS):
        moved = scale * source @ rotation.T + translation
        pairs_source = np.vstack([source, (nearest(target, moved) - translation) @ rotation / scale])
        pairs_target = np.vstack([nearest(moved, target), target])
        scale, rotation, translation = umeyama(pairs_source, pairs_target)
    moved = scale * source @ rotation.T + translation
    forward = np.linalg.norm(nearest(moved, target) - moved, axis=1)
    backward = np.linalg.norm(nearest(target, moved) - target, axis=1)
    return np.sqrt(np.mean(np.concatenate([forward, backward]) ** 2)), scale, rotation, translation


def umeyama(source, target):
    mean_source, mean_target = source.mean(axis=0), target.mean(axis=0)
    centred_source, centred_target = source - mean_source, target - mean_target
    u, singular, vt = np.linalg.svd(centred_target.T @ centred_source / len(source))
    sign = np.diag([1.0, np.sign(np.linalg.det(u @ vt))])
    rotation = u @ sign @ vt
    scale = np.trace(np.diag(singular) @ sign) / np.mean(np.sum(centred_source ** 2, axis=1))
    return scale, rotation, mean_target - scale * rotation @ mean_source


def to_game(vector):
    return [round(-float(vector[0]), 3), round(float(vector[1]), 3), round(float(vector[2]), 3)]


def camera_report(model, photo, errors):
    rotation = cv2.Rodrigues(model.rvec[photo])[0]
    centre = camera_centre(model, photo)
    forward, up = rotation.T @ np.array([0.0, 0.0, 1.0]), rotation.T @ np.array([0.0, -1.0, 0.0])
    fov = np.degrees(2 * np.arctan(model.sizes[photo][1] / 2 / model.focal[photo]))
    return {"position": to_game(centre), "target": to_game(centre + forward * 5), "up": to_game(up),
            "fov": round(float(fov), 1), "rms_px": round(float(np.sqrt(np.mean(errors ** 2))), 2) if len(errors) else None}


def draw_overlay(model, photo, photos_dir, wireframe, output):
    image = cv2.imread(str(Path(photos_dir) / model.photos[photo]))
    matrix = focal_matrix(model, photo, model.focal[photo])
    for structure in wireframe["structures"]:
        for line in structure["rings"] + structure["corners"]:
            draw_polyline(image, model, photo, matrix, line, line in structure["rings"])
    rows = np.nonzero((model.obs_photo == photo) & model.triangulated()[model.obs_point])[0]
    projected, _ = cv2.projectPoints(model.points[model.obs_point[rows]], model.rvec[photo], model.tvec[photo], matrix, None)
    for row, back in zip(rows, projected.reshape(-1, 2)):
        label = tuple(int(v) for v in model.obs_uv[row])
        cv2.circle(image, label, 6, (0, 255, 0), 2)
        cv2.line(image, label, tuple(int(v) for v in back), (0, 0, 255), 2)
        cv2.putText(image, model.names[model.obs_point[row]], (label[0] + 7, label[1] - 7), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 2)
    cv2.imwrite(str(output / f"labels_{Path(model.photos[photo]).stem}.png"), image)


def draw_polyline(image, model, photo, matrix, line, closed):
    world = np.array([[-p[0], p[1], p[2]] for p in line], dtype=float)
    camera = (cv2.Rodrigues(model.rvec[photo])[0] @ world.T).T + model.tvec[photo]
    pixels, _ = cv2.projectPoints(world, model.rvec[photo], model.tvec[photo], matrix, None)
    pixels = pixels.reshape(-1, 2)
    count = len(world)
    for i in range(count if closed else count - 1):
        j = (i + 1) % count
        if camera[i, 2] > 0.2 and camera[j, 2] > 0.2:
            cv2.line(image, tuple(int(v) for v in pixels[i]), tuple(int(v) for v in pixels[j]), (0, 220, 255), 1, cv2.LINE_AA)


def draw_top_view(model, outline, output):
    scale, margin = 40, 60
    points = model.points[model.triangulated()][:, [0, 2]]
    centres = np.array([camera_centre(model, p)[[0, 2]] for p in np.nonzero(model.registered())[0]])
    everything = np.vstack([points, centres, outline])
    low, high = everything.min(axis=0), everything.max(axis=0)
    size = ((high - low) * scale + 2 * margin).astype(int)
    image = np.full((size[1], size[0], 3), 255, np.uint8)

    def pixel(p):
        # Mirrored x back to east = right, north = up
        return int((high[0] - p[0]) * scale + margin), int((high[1] - p[1]) * scale + margin)

    cv2.polylines(image, [np.array([pixel(p) for p in outline])], True, (120, 120, 120), 2)
    for name, point, floor in zip(np.array(model.names)[model.triangulated()], points, model.floor[model.triangulated()]):
        cv2.circle(image, pixel(point), 4, (0, 160, 0) if floor else (200, 0, 200), -1)
        cv2.putText(image, name, (pixel(point)[0] + 5, pixel(point)[1] - 5), cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 0, 0), 1)
    for photo, centre in zip(np.nonzero(model.registered())[0], centres):
        cv2.circle(image, pixel(centre), 6, (255, 0, 0), 2)
        cv2.putText(image, Path(model.photos[photo]).stem, (pixel(centre)[0] + 7, pixel(centre)[1]), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (255, 0, 0), 1)
    cv2.imwrite(str(output / "topview.png"), image)


def option(flag, default):
    return sys.argv[sys.argv.index(flag) + 1] if flag in sys.argv else default


def main():
    labels = json.loads(Path(sys.argv[1]).read_text())
    photos_dir, wireframe = sys.argv[2], json.loads(Path(sys.argv[3]).read_text())
    output = Path(sys.argv[4])
    output.mkdir(parents=True, exist_ok=True)
    model = Reconstruction(labels)
    print(f"{len(model.names)} corners seen twice or more, {len(model.photos)} photos with {MIN_PNP}+ of them")
    first = reconstruct(model)
    level_on_floor(model)
    outline = plan_outline(wireframe, option("--structure", "island"))
    anchors = json.loads(Path(option("--anchors", "")).read_text()) if "--anchors" in sys.argv else {}
    plan_error = fit_on_plan(model, option("--prefix", "ile"), outline, anchors)
    report(model, first, plan_error, photos_dir, wireframe, outline, output)


def report(model, first, plan_error, photos_dir, wireframe, outline, output):
    selection, errors = reprojection_errors(model)
    cameras = {}
    for photo in np.nonzero(model.registered())[0]:
        cameras[model.photos[photo]] = camera_report(model, photo, errors[model.obs_photo[selection] == photo])
        draw_overlay(model, photo, photos_dir, wireframe, output)
    outliers = [{"photo": model.photos[model.obs_photo[row]], "corner": model.names[model.obs_point[row]], "error_px": round(float(e), 1)}
                for row, e in zip(selection, errors) if e > OUTLIER_PIXELS]
    result = {"first_photo": model.photos[first], "plan_fit_rms_m": round(float(plan_error), 3),
              "points": {n: to_game(p) for n, p in zip(model.names, model.points) if not np.isnan(p[0])},
              "floor": [n for n, f in zip(model.names, model.floor) if f],
              "cameras": cameras, "outliers": sorted(outliers, key=lambda o: -o["error_px"]),
              "unregistered": [p for i, p in enumerate(model.photos) if not model.registered()[i]],
              "untriangulated": [n for n, p in zip(model.names, model.points) if np.isnan(p[0])]}
    (output / "triangulated.json").write_text(json.dumps(result, indent=1))
    draw_top_view(model, outline, output)
    print(json.dumps({k: result[k] for k in ("plan_fit_rms_m", "cameras", "outliers", "unregistered", "untriangulated")}, indent=1))


if __name__ == "__main__":
    main()
