#!/usr/bin/env python3
"""Build the game layout of Block'Out Rennes.

Reads the floor plan extraction of sprint 3.3 (gym_blockout.json) and the per-sector
appearance read from photos (wall_appearance.json), repairs the polygon topology
(near-duplicate vertices, T-junctions), classifies every sector edge as exposed /
back / internal and writes blockout_rennes_layout.json for the Unity runtime.
The floor plan is assumed drawn to scale: coordinates are rescaled to one px/m.
A debug drawing of the classification is written next to this script.

Usage: build_layout.py  (run with the repository .venv python: numpy and Pillow)
"""

import json
import math
import unicodedata
import zlib
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
GYM_SCENES = ROOT / "unity/Assets/BoulderingGym/Data/Gameplay/GymScenes"
SOURCE = GYM_SCENES / "gym_blockout.json"
OUTPUT = GYM_SCENES / "blockout_rennes_layout.json"
APPEARANCE = HERE / "wall_appearance.json"
DEBUG_IMAGE = HERE / "layout_debug.png"

SNAP_TOLERANCE = 0.08
T_JUNCTION_TOLERANCE = 0.05
SIMPLIFY_DEVIATION = 0.15
OUTSIDE_PROBE = 0.3
CARDINAL = {"north": (0.0, 1.0), "south": (0.0, -1.0), "east": (1.0, 0.0), "west": (-1.0, 0.0)}
CARDINAL_MATCH = 0.7
MIN_EDGE = 0.02


def sector_key(label):
    """'Petit'Oie' -> 'petitoie', 'Bloc à Pic' -> 'bloc_a_pic'."""
    ascii_label = unicodedata.normalize("NFKD", label).encode("ascii", "ignore").decode()
    ascii_label = ascii_label.replace("'", "").lower()
    return "".join(c if c.isalnum() else "_" for c in ascii_label).strip("_")


def signed_area(points):
    return 0.5 * sum(a[0] * b[1] - b[0] * a[1] for a, b in zip(points, points[1:] + points[:1]))


def point_in_polygon(point, polygon):
    x, z = point
    inside = False
    for (x1, z1), (x2, z2) in zip(polygon, polygon[1:] + polygon[:1]):
        if (z1 > z) != (z2 > z) and x < (x2 - x1) * (z - z1) / (z2 - z1) + x1:
            inside = not inside
    return inside


def distance_to_segment(point, a, b):
    ax, az = a
    bx, bz = b
    px, pz = point
    dx, dz = bx - ax, bz - az
    length_sq = dx * dx + dz * dz
    t = 0.0 if length_sq == 0 else max(0.0, min(1.0, ((px - ax) * dx + (pz - az) * dz) / length_sq))
    return math.hypot(px - (ax + t * dx), pz - (az + t * dz)), t


class Topology:
    """Shared vertex pool for the sectors of one structure."""

    def __init__(self, blocs, tolerance):
        self.vertices = []
        self.loops = {}
        raw = {bloc["label"]: [(p["x"], p["z"]) for p in bloc["footprint_xz"]] for bloc in blocs}
        index_of = self._snap(raw, tolerance)
        for label, points in raw.items():
            loop = self._clean([index_of[(label, i)] for i in range(len(points))])
            if signed_area([self.vertices[i] for i in loop]) < 0:
                loop.reverse()
            self.loops[label] = loop
        self._split_t_junctions()
        self._simplify()

    def _snap(self, raw, tolerance):
        """Clusters vertices of different sectors closer than the tolerance (never two of one sector)."""
        index_of = {}
        clusters = []
        for label, points in raw.items():
            for i, point in enumerate(points):
                match = next((c for c in clusters if label not in c["labels"]
                              and math.dist(c["position"], point) < tolerance), None)
                if match is None:
                    match = {"position": point, "members": [], "labels": set()}
                    clusters.append(match)
                match["members"].append(point)
                match["labels"].add(label)
                index_of[(label, i)] = clusters.index(match)
        self.vertices = [tuple(sum(c) / len(c) for c in zip(*cluster["members"])) for cluster in clusters]
        return index_of

    def _clean(self, loop):
        cleaned = []
        for index in loop:
            if not cleaned or math.dist(self.vertices[cleaned[-1]], self.vertices[index]) > MIN_EDGE:
                cleaned.append(index)
        if len(cleaned) > 1 and math.dist(self.vertices[cleaned[0]], self.vertices[cleaned[-1]]) <= MIN_EDGE:
            cleaned.pop()
        return cleaned

    def _split_t_junctions(self):
        """Inserts vertices lying on another sector's edge into that edge."""
        changed = True
        while changed:
            changed = False
            for label, loop in self.loops.items():
                for i in range(len(loop)):
                    a, b = loop[i], loop[(i + 1) % len(loop)]
                    hit = self._vertex_on_edge(a, b)
                    if hit is not None:
                        loop.insert(i + 1, hit)
                        changed = True
                        break
                if changed:
                    break

    def _simplify(self):
        """Drops contour noise: vertices of one sector only, close to the line of their neighbours."""
        changed = True
        while changed:
            changed = False
            for loop in self.loops.values():
                index = self._removable_vertex(loop)
                if index is not None:
                    del loop[index]
                    changed = True

    def _removable_vertex(self, loop):
        if len(loop) <= 3:
            return None
        for i, vertex in enumerate(loop):
            if sum(vertex in other for other in self.loops.values()) > 1:
                continue
            previous, following = self.vertices[loop[i - 1]], self.vertices[loop[(i + 1) % len(loop)]]
            if distance_to_segment(self.vertices[vertex], previous, following)[0] < SIMPLIFY_DEVIATION:
                return i
        return None

    def _vertex_on_edge(self, a, b):
        for index, point in enumerate(self.vertices):
            if index in (a, b):
                continue
            distance, t = distance_to_segment(point, self.vertices[a], self.vertices[b])
            if distance < T_JUNCTION_TOLERANCE and 0.0 < t < 1.0 and any(index in loop for loop in self.loops.values()):
                return index
        return None


def classify_edges(structure_id, topology, envelope, all_polygons):
    """exposed: faces the room; back: against the building or another structure; internal: shared."""
    owners = {}
    for label, loop in topology.loops.items():
        for i in range(len(loop)):
            owners.setdefault(frozenset((loop[i], loop[(i + 1) % len(loop)])), []).append(label)
    kinds = {}
    for label, loop in topology.loops.items():
        kinds[label] = []
        for i in range(len(loop)):
            a, b = loop[i], loop[(i + 1) % len(loop)]
            if len(owners[frozenset((a, b))]) > 1:
                kinds[label].append("internal")
                continue
            probe = outward_probe(topology.vertices[a], topology.vertices[b])
            outside_building = not (envelope[0] < probe[0] < envelope[1] and envelope[2] < probe[1] < envelope[3])
            in_other = any(point_in_polygon(probe, polygon) for sid, polygon in all_polygons if sid != (structure_id, label))
            kinds[label].append("back" if outside_building or in_other else "exposed")
    return kinds


def outward_probe(a, b):
    dx, dz = b[0] - a[0], b[1] - a[1]
    length = math.hypot(dx, dz)
    mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
    return mid[0] + dz / length * OUTSIDE_PROBE, mid[1] - dx / length * OUTSIDE_PROBE


def boundary_loops(topology, kinds):
    """Chains non-internal edges; a valid structure has exactly one closed loop."""
    by_start = {}
    for label, loop in topology.loops.items():
        for i, kind in enumerate(kinds[label]):
            if kind != "internal":
                a, b = loop[i], loop[(i + 1) % len(loop)]
                if a in by_start:
                    raise ValueError(f"two boundary edges start at vertex {a} ({label})")
                by_start[a] = b
    loops = []
    remaining = dict(by_start)
    while remaining:
        start, current = next(iter(remaining.items()))
        chain = [start]
        del remaining[start]
        while current != start:
            chain.append(current)
            if current not in remaining:
                raise ValueError(f"boundary broken at vertex {current}")
            current = remaining.pop(current)
        loops.append(chain)
    return loops


def apply_blank_sides(kinds, loop, vertices, blank_sides):
    """Exposed edges whose outward direction points to a listed side become plain (back) walls."""
    result = list(kinds)
    for i, kind in enumerate(kinds):
        a, b = vertices[loop[i]], vertices[loop[(i + 1) % len(loop)]]
        probe = outward_probe(a, b)
        mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
        direction = ((probe[0] - mid[0]) / OUTSIDE_PROBE, (probe[1] - mid[1]) / OUTSIDE_PROBE)
        for side in blank_sides:
            if kind == "exposed" and direction[0] * CARDINAL[side][0] + direction[1] * CARDINAL[side][1] > CARDINAL_MATCH:
                result[i] = "back"
    return result


def sector_definition(label, loop, kinds, vertices, appearance):
    key = sector_key(label)
    look = appearance["sectors"][key]
    return {
        "id": key,
        "name": label,
        "loop": loop,
        "edges": apply_blank_sides(kinds, loop, vertices, look.get("blankSides", [])),
        "profile": [{"height": h, "angle": a} for h, a in look["profile"]],
        "panelColors": look["panelColors"],
        "pattern": look["pattern"],
        "volumes": [{"shape": s, "color": c, "count": n} for s, c, n in look["volumes"]],
        "seed": zlib.crc32(key.encode()) & 0x7FFFFFFF,
    }


def to_uniform_scale(source):
    """The extraction used different px/m in x and z; rescale to one px/m keeping the floor area."""
    scale = source["meta"]["scale_px_per_m"]
    uniform = math.sqrt(scale["x"] * scale["z"])
    for structure in source["structures"]:
        for bloc in structure["blocs"]:
            for point in bloc["footprint_xz"]:
                point["x"] = round(point["x"] * scale["x"] / uniform, 3)
                point["z"] = round(point["z"] * scale["z"] / uniform, 3)


def build():
    source = json.loads(SOURCE.read_text(encoding="utf-8-sig"))
    to_uniform_scale(source)
    appearance = json.loads(APPEARANCE.read_text(encoding="utf-8"))
    all_points = [(p["x"], p["z"]) for s in source["structures"] for b in s["blocs"] for p in b["footprint_xz"]]
    envelope = (min(p[0] for p in all_points), max(p[0] for p in all_points),
                min(p[1] for p in all_points), max(p[1] for p in all_points))
    topologies = {s["id"]: Topology(s["blocs"], SNAP_TOLERANCE) for s in source["structures"]}
    polygons = [((sid, label), [topo.vertices[i] for i in loop])
                for sid, topo in topologies.items() for label, loop in topo.loops.items()]

    structures, report = [], []
    for structure_id, topology in topologies.items():
        kinds = classify_edges(structure_id, topology, envelope, polygons)
        loops = boundary_loops(topology, kinds)
        report.append(f"{structure_id}: {len(topology.vertices)} vertices, {len(topology.loops)} sectors, "
                      f"{len(loops)} boundary loop(s), "
                      f"{sum(k == 'exposed' for ks in kinds.values() for k in ks)} exposed edges")
        if len(loops) != 1:
            raise ValueError(f"{structure_id}: expected one boundary loop, found {len(loops)}")
        look = appearance["structures"][structure_id]
        structures.append({
            "id": structure_id,
            "name": look["name"],
            "height": look["height"],
            "vertices": [{"x": round(x, 3), "z": round(z, 3)} for x, z in topology.vertices],
            "sectors": [sector_definition(label, loop, kinds[label], topology.vertices, appearance)
                        for label, loop in topology.loops.items()],
        })

    spawn = appearance["spawn"]
    layout = {
        "name": "Block'Out Rennes",
        "version": "3.0",
        "envelope": {"minX": envelope[0], "maxX": envelope[1], "minZ": envelope[2], "maxZ": envelope[3],
                     "height": source["meta"]["building_envelope"]["ceiling_height_m"]},
        "spawn": spawn,
        "palette": [{"id": key, "r": rgb[0], "g": rgb[1], "b": rgb[2]} for key, rgb in appearance["palette"].items()],
        "structures": structures,
    }
    OUTPUT.write_text(json.dumps(layout, ensure_ascii=False, indent=1), encoding="utf-8")
    draw_debug(topologies, structures, envelope, spawn)
    print("\n".join(report))
    print(f"spawn {spawn}, envelope {envelope}")
    print(f"written {OUTPUT}")


def draw_debug(topologies, structures, envelope, spawn):
    scale, margin = 30, 20
    width = int((envelope[1] - envelope[0]) * scale) + 2 * margin
    height = int((envelope[3] - envelope[2]) * scale) + 2 * margin
    image = Image.new("RGB", (width, height), "white")
    draw = ImageDraw.Draw(image)

    def to_pixel(point):
        return (margin + (point[0] - envelope[0]) * scale, margin + (envelope[3] - point[1]) * scale)

    colors = {"exposed": (0, 160, 0), "back": (200, 0, 0), "internal": (170, 170, 170)}
    for structure in structures:
        topology = topologies[structure["id"]]
        for sector in structure["sectors"]:
            points = [topology.vertices[i] for i in sector["loop"]]
            draw.polygon([to_pixel(p) for p in points], fill=(235, 235, 235))
            for i, kind in enumerate(sector["edges"]):
                a, b = points[i], points[(i + 1) % len(points)]
                draw.line([to_pixel(a), to_pixel(b)], fill=colors[kind], width=3)
                if kind == "exposed":
                    probe = outward_probe(a, b)
                    mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
                    draw.line([to_pixel(mid), to_pixel(probe)], fill=colors[kind], width=2)
            cx = sum(p[0] for p in points) / len(points)
            cz = sum(p[1] for p in points) / len(points)
            draw.text(to_pixel((cx, cz)), sector["name"], fill=(0, 0, 0))
    sx, sy = to_pixel((spawn["x"], spawn["z"]))
    draw.ellipse([sx - 6, sy - 6, sx + 6, sy + 6], fill=(0, 0, 255))
    image.save(DEBUG_IMAGE)


if __name__ == "__main__":
    build()
