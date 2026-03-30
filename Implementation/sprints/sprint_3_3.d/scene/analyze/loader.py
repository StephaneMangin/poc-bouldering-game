"""Load bloc data from gym_blockout.json."""
from __future__ import annotations

import json
from pathlib import Path

from scene.constants import IMAGES_DIR, JSON_PATH
from scene.models import Bloc, Vertex
from scene.utils import slugify


def load_blocs() -> list[Bloc]:
    """Load blocs from scene JSON and discover photos."""
    data = json.loads(JSON_PATH.read_text())
    blocs: list[Bloc] = []

    for structure in data["structures"]:
        struct_label = structure["label"]
        struct_slug = slugify(struct_label)

        for bloc_json in structure["blocs"]:
            label = bloc_json["label"]
            slug = slugify(label)
            footprint = [
                Vertex(x=v["x"], z=v["z"]) for v in bloc_json["footprint_xz"]
            ]
            centroid = Vertex(
                x=bloc_json["centroid_xz"]["x"],
                z=bloc_json["centroid_xz"]["z"],
            )

            photo_dir = IMAGES_DIR / struct_slug / slug
            photos: list[Path] = []
            if photo_dir.is_dir():
                photos = sorted(
                    f
                    for f in photo_dir.iterdir()
                    if f.suffix.lower() in (".jpg", ".jpeg", ".webp")
                )

            blocs.append(
                Bloc(
                    id=bloc_json["id"],
                    label=label,
                    height_m=bloc_json["height_m"],
                    footprint_xz=footprint,
                    centroid=centroid,
                    slug=slug,
                    structure_label=struct_label,
                    structure_slug=struct_slug,
                    photos=photos,
                ),
            )

    return blocs
