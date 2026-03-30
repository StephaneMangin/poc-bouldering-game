"""Contact sheet generation: grid of all photos for a bloc."""
from __future__ import annotations

import math
from pathlib import Path

import cv2
import numpy as np

from scene.constants import ARCH_FONT, CONTACT_THUMB
from scene.models import Bloc


def make_contact_sheet(bloc: Bloc, out_dir: Path) -> Path | None:
    """Create a grid of all photos for this bloc."""
    if not bloc.photos:
        return None

    thumbs: list[np.ndarray] = []
    for p in bloc.photos:
        img = cv2.imread(str(p))
        if img is None:
            continue
        h, w = img.shape[:2]
        scale = CONTACT_THUMB / max(h, w)
        thumb = cv2.resize(img, (int(w * scale), int(h * scale)))
        canvas = np.full(
            (CONTACT_THUMB, CONTACT_THUMB, 3), 240, dtype=np.uint8,
        )
        y0 = (CONTACT_THUMB - thumb.shape[0]) // 2
        x0 = (CONTACT_THUMB - thumb.shape[1]) // 2
        canvas[y0 : y0 + thumb.shape[0], x0 : x0 + thumb.shape[1]] = thumb
        cv2.putText(
            canvas, p.stem, (5, CONTACT_THUMB - 10),
            ARCH_FONT, 0.45, (0, 0, 0), 1, cv2.LINE_AA,
        )
        thumbs.append(canvas)

    if not thumbs:
        return None

    cols = min(4, len(thumbs))
    rows = math.ceil(len(thumbs) / cols)
    while len(thumbs) < rows * cols:
        thumbs.append(
            np.full((CONTACT_THUMB, CONTACT_THUMB, 3), 240, dtype=np.uint8),
        )

    grid_rows = []
    for r in range(rows):
        row_imgs = thumbs[r * cols : (r + 1) * cols]
        grid_rows.append(np.hstack(row_imgs))
    grid = np.vstack(grid_rows)

    title_h = 40
    title_bar = np.full((title_h, grid.shape[1], 3), 50, dtype=np.uint8)
    text = f"{bloc.label} - {len(bloc.photos)} photos"
    cv2.putText(
        title_bar, text, (10, 28), ARCH_FONT, 0.7,
        (255, 255, 255), 1, cv2.LINE_AA,
    )
    result = np.vstack([title_bar, grid])

    path = out_dir / "contact_sheet.jpg"
    cv2.imwrite(str(path), result, [cv2.IMWRITE_JPEG_QUALITY, 90])
    return path
