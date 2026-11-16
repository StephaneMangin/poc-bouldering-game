"""Render the exported gym (GymPreview output) from the viewpoints file with Blender.

Usage:
  blender -b -P render.py -- EXPORT_DIR VIEWPOINTS_JSON OUTPUT_DIR [--engine eevee|cycles]
                             [--samples N] [--views name,name] [--suffix TEXT] [--no-climber]

Coordinates in the viewpoints file are game (Unity) coordinates; the OBJ export mirrored x, so
game (x, y, z) is Blender (-x, -z, y).
"""

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOF_OBJECTS = ("ceiling", "steel", "ducts", "skylights", "lamps", "cladding", "strips", "flag")


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("export_dir")
    parser.add_argument("viewpoints")
    parser.add_argument("output_dir")
    parser.add_argument("--engine", default="eevee", choices=["eevee", "cycles"])
    parser.add_argument("--samples", type=int, default=64)
    parser.add_argument("--views", default="")
    parser.add_argument("--suffix", default="")
    parser.add_argument("--no-climber", action="store_true")
    return parser.parse_args(argv)


def to_blender(point):
    return Vector((-point[0], -point[2], point[1]))


def setup_scene(export_dir, engine, samples):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.obj_import(filepath=str(Path(export_dir) / "gym.obj"), forward_axis="NEGATIVE_Z", up_axis="Y")
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT" if engine == "eevee" else "CYCLES"
    if engine == "cycles":
        scene.cycles.device = "CPU"
        scene.cycles.samples = samples
        scene.cycles.use_denoising = True
    else:
        scene.eevee.taa_render_samples = samples
        scene.eevee.use_shadows = True
        scene.eevee.use_raytracing = True
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"
    tune_materials()
    add_lighting(scene)


def tune_materials():
    for material in bpy.data.materials:
        node = material.node_tree.nodes.get("Principled BSDF") if material.node_tree else None
        if node is None:
            continue
        name = material.name
        if name.startswith("hold_"):
            node.inputs["Roughness"].default_value = 0.45
        elif name in ("skylight", "lamp", "strip"):
            node.inputs["Emission Strength"].default_value = {"skylight": 12.0, "lamp": 6.0, "strip": 8.0}[name]
        else:
            node.inputs["Roughness"].default_value = 0.85
        node.inputs["Specular IOR Level"].default_value = 0.35


def add_lighting(scene):
    world = bpy.data.worlds.new("Hall")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.62, 0.66, 0.72, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.45
    scene.world = world
    sun = bpy.data.lights.new("Sun", "SUN")
    sun.energy = 2.2
    sun.angle = math.radians(6)
    sun_object = bpy.data.objects.new("Sun", sun)
    sun_object.rotation_euler = (math.radians(50), math.radians(10), math.radians(35))
    scene.collection.objects.link(sun_object)
    for x in (-18.0, -8.0, 2.0, 12.0):
        for z in (-8.0, 2.0, 12.0):
            area = bpy.data.lights.new(f"Area {x} {z}", "AREA")
            area.energy = 650.0
            area.size = 5.0
            area.color = (1.0, 0.97, 0.92)
            area_object = bpy.data.objects.new(area.name, area)
            area_object.location = to_blender((x, 8.2, z))
            scene.collection.objects.link(area_object)


def add_climber(config, base_dir):
    path = (base_dir / config["fbx"]).resolve()
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in imported if o.parent is None]
    facing = config["facing"]
    yaw = math.atan2(-facing[0], facing[2])
    for root in roots:
        root.location = to_blender(config["position"])
        root.rotation_euler = (math.radians(90), 0.0, -yaw)
    bpy.context.scene.frame_set(1)


def look_at(camera_object, target):
    direction = target - camera_object.location
    camera_object.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_view(view, output_dir, suffix):
    scene = bpy.context.scene
    camera = bpy.data.cameras.new(view["name"])
    camera.sensor_fit = "VERTICAL"
    camera.angle_y = math.radians(view["fov"])
    camera.clip_start = 0.05
    camera.clip_end = 200.0
    camera_object = bpy.data.objects.new(view["name"], camera)
    scene.collection.objects.link(camera_object)
    camera_object.location = to_blender(view["position"])
    look_at(camera_object, to_blender(view["target"]))
    scene.camera = camera_object
    scene.render.resolution_x = view["width"]
    scene.render.resolution_y = view["height"]
    for name in ROOF_OBJECTS:
        for obj in bpy.data.objects:
            if obj.name.startswith(name):
                obj.hide_render = bool(view.get("hideRoof"))
    scene.render.filepath = str(Path(output_dir) / f"{view['name']}{suffix}.png")
    bpy.ops.render.render(write_still=True)


def main():
    args = parse_args()
    viewpoints_path = Path(args.viewpoints).resolve()
    config = json.loads(viewpoints_path.read_text(encoding="utf-8"))
    setup_scene(args.export_dir, args.engine, args.samples)
    if not args.no_climber and "climber" in config:
        add_climber(config["climber"], viewpoints_path.parent)
    wanted = set(filter(None, args.views.split(",")))
    Path(args.output_dir).mkdir(parents=True, exist_ok=True)
    for view in config["views"]:
        if not wanted or view["name"] in wanted:
            render_view(view, args.output_dir, args.suffix)


main()
