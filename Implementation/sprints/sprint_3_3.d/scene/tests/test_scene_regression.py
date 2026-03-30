"""Regression tests for sprint 3.3 scene reconstruction pipeline."""
from __future__ import annotations

import numpy as np
import pytest

from scene.constants import ANALYSIS_DIR, IMAGES_DIR

EXPECTED_STRUCTURES = 4
EXPECTED_BLOCS = 32
MIN_FOOTPRINT_VERTICES = 3


class TestGymBlockoutJSON:
    def test_has_meta(self, scene_data: dict) -> None:
        assert "meta" in scene_data
        assert scene_data["meta"]["name"] == "Block'Out Rennes"

    def test_structure_count(self, scene_data: dict) -> None:
        structures = scene_data["structures"]
        assert len(structures) == EXPECTED_STRUCTURES

    def test_total_bloc_count(self, scene_data: dict) -> None:
        total = sum(len(s["blocs"]) for s in scene_data["structures"])
        assert total == EXPECTED_BLOCS

    def test_each_bloc_has_valid_footprint(self, scene_data: dict) -> None:
        for structure in scene_data["structures"]:
            for bloc in structure["blocs"]:
                fp = bloc["footprint_xz"]
                assert len(fp) >= MIN_FOOTPRINT_VERTICES, (
                    f"{bloc['label']} has only {len(fp)} vertices"
                )
                for v in fp:
                    assert "x" in v and "z" in v

    def test_each_bloc_has_height(self, scene_data: dict) -> None:
        for structure in scene_data["structures"]:
            for bloc in structure["blocs"]:
                assert bloc["height_m"] > 0, (
                    f"{bloc['label']} has invalid height"
                )

    def test_each_bloc_has_centroid(self, scene_data: dict) -> None:
        for structure in scene_data["structures"]:
            for bloc in structure["blocs"]:
                c = bloc["centroid_xz"]
                assert "x" in c and "z" in c

    def test_each_bloc_has_area(self, scene_data: dict) -> None:
        for structure in scene_data["structures"]:
            for bloc in structure["blocs"]:
                assert bloc["area_m2"] > 0

    def test_coordinate_system(self, scene_data: dict) -> None:
        cs = scene_data["meta"]["coordinate_system"]
        assert cs["origin"] == "center_island"
        assert cs["x"] == "east"
        assert cs["z"] == "north"
        assert cs["y"] == "up"

    def test_building_envelope(self, scene_data: dict) -> None:
        env = scene_data["meta"]["building_envelope"]
        assert env["width_m"] == pytest.approx(40.0)
        assert env["depth_m"] == pytest.approx(35.0)
        assert env["ceiling_height_m"] == pytest.approx(7.0)


class TestWallColors:
    def test_classify_pixel_grey(self) -> None:
        from scene.colors import GREY, classify_pixel_hsv

        assert classify_pixel_hsv(h=90, s=10, v=120) == GREY

    def test_classify_pixel_green(self) -> None:
        from scene.colors import GREEN, classify_pixel_hsv

        assert classify_pixel_hsv(h=60, s=100, v=100) == GREEN

    def test_classify_pixel_red(self) -> None:
        from scene.colors import RED, classify_pixel_hsv

        assert classify_pixel_hsv(h=5, s=150, v=150) == RED

    def test_classify_pixel_red_high_hue(self) -> None:
        from scene.colors import RED, classify_pixel_hsv

        assert classify_pixel_hsv(h=170, s=150, v=150) == RED

    def test_classify_pixel_dark(self) -> None:
        from scene.colors import UNKNOWN, classify_pixel_hsv

        assert classify_pixel_hsv(h=0, s=0, v=10) == UNKNOWN

    def test_kmeans_cluster_dark(self) -> None:
        import numpy as np
        from scene.colors import classify_kmeans_cluster

        small = np.zeros((10, 10, 3), dtype=np.uint8)
        labels = np.zeros((10, 10), dtype=np.uint8)
        assert classify_kmeans_cluster(small, labels, 0) == "dark"

    def test_kmeans_cluster_unknown_empty(self) -> None:
        import numpy as np
        from scene.colors import classify_kmeans_cluster

        small = np.zeros((10, 10, 3), dtype=np.uint8)
        labels = np.ones((10, 10), dtype=np.uint8)
        assert classify_kmeans_cluster(small, labels, 0) == "unknown"


class TestAnalysisOutputs:
    """Verify that analysis outputs exist for all blocs."""

    def test_all_blocs_have_reports(self, scene_data: dict) -> None:
        from scene.utils import slugify

        analysis_dir = ANALYSIS_DIR
        missing = []
        for structure in scene_data["structures"]:
            struct_slug = slugify(structure["label"])
            for bloc in structure["blocs"]:
                bloc_slug = slugify(bloc["label"])
                rapport = analysis_dir / struct_slug / bloc_slug / "rapport.txt"
                if not rapport.exists():
                    missing.append(f"{struct_slug}/{bloc_slug}")

        assert not missing, f"Missing reports: {missing}"

    def test_all_blocs_have_plans(self, scene_data: dict) -> None:
        from scene.utils import slugify

        analysis_dir = ANALYSIS_DIR
        missing = []
        for structure in scene_data["structures"]:
            struct_slug = slugify(structure["label"])
            for bloc in structure["blocs"]:
                bloc_slug = slugify(bloc["label"])
                bloc_dir = analysis_dir / struct_slug / bloc_slug
                for plan in ("plan_dessus.png", "plan_face.png", "plan_cote.png"):
                    if not (bloc_dir / plan).exists():
                        missing.append(f"{struct_slug}/{bloc_slug}/{plan}")

        assert not missing, f"Missing plans: {missing}"


class TestWireframeModels:
    """Unit tests for wireframe data models."""

    def test_panel_creation(self) -> None:
        from scene.models import Panel

        poly = np.array([[0, 0], [100, 0], [50, 80]], dtype=np.int32)
        panel = Panel(
            polygon=poly,
            color_label=2,
            color_name="green",
            median_bgr=(0, 180, 0),
            area_px=4000.0,
            cluster_id=1,
        )
        assert panel.color_name == "green"
        assert len(panel.polygon) == 3

    def test_wireframe_result_defaults(self) -> None:
        from scene.models import WireframeResult

        result = WireframeResult()
        assert result.panels == []
        assert result.segments == []
        assert result.joints == []

    def test_segment_type(self) -> None:
        from scene.models import Segment

        seg: Segment = ((0, 0), (10, 10))
        assert seg[0] == (0, 0)


class TestWireframeSegmentation:
    """Unit tests for K-means segmentation."""

    def test_build_kmeans_labels_shape(self) -> None:
        from scene.wireframe.segmentation import TARGET_SMALL_H, build_kmeans_labels

        img = np.random.randint(0, 255, (200, 300, 3), dtype=np.uint8)
        labels, small_bgr, scale, sh, sw = build_kmeans_labels(img)
        assert sh == TARGET_SMALL_H
        assert labels.shape == (sh, sw)
        assert small_bgr.shape == (sh, sw, 3)

    def test_find_wall_clusters_returns_set(self) -> None:
        from scene.wireframe.segmentation import build_kmeans_labels, find_wall_clusters

        img = np.random.randint(0, 255, (200, 300, 3), dtype=np.uint8)
        labels, small_bgr, *_ = build_kmeans_labels(img)
        result = find_wall_clusters(small_bgr, labels)
        assert isinstance(result, set)


class TestWireframePanels:
    """Unit tests for panel extraction utilities."""

    def test_extract_joints_deduplicates(self) -> None:
        from scene.models import Panel
        from scene.wireframe.panels import extract_joints

        poly = np.array([[0, 0], [10, 0], [10, 10]], dtype=np.int32)
        p1 = Panel(poly, 1, "grey", (128, 128, 128), 50.0, 0)
        p2 = Panel(poly, 2, "green", (0, 180, 0), 50.0, 1)
        joints = extract_joints([p1, p2])
        assert len(joints) == 3  # same 3 vertices, deduplicated

    def test_deduplicate_segments_removes_near_dupes(self) -> None:
        from scene.wireframe.panels import deduplicate_segments

        seg1 = ((0, 0), (100, 0))
        seg2 = ((2, 1), (101, 1))  # near-duplicate
        seg3 = ((0, 200), (100, 200))  # distinct
        result = deduplicate_segments([seg1, seg2, seg3])
        assert len(result) == 2

    def test_panels_to_segments_filters_short(self) -> None:
        from scene.models import Panel
        from scene.wireframe.panels import MIN_SEG_LENGTH, panels_to_segments

        # Triangle with one very short edge
        poly = np.array([[0, 0], [100, 0], [100, 5]], dtype=np.int32)
        panel = Panel(poly, 1, "grey", (128, 128, 128), 250.0, 0)
        segments = panels_to_segments([panel])
        for p1, p2 in segments:
            length = np.hypot(p2[0] - p1[0], p2[1] - p1[1])
            assert length >= MIN_SEG_LENGTH


class TestWireframePipeline:
    """Integration test for the full pipeline."""

    @pytest.fixture(scope="class")
    def sample_image(self) -> np.ndarray | None:
        import cv2

        img_path = IMAGES_DIR / "image_001.jpg"
        if not img_path.exists():
            pytest.skip("image_001.jpg not available")
        return cv2.imread(str(img_path))

    def test_detect_panels_returns_result(
        self, sample_image: np.ndarray,
    ) -> None:
        from scene.wireframe import detect_panels

        result = detect_panels(sample_image)
        assert result.panels
        assert result.segments
        assert result.joints
        assert result.image_shape == sample_image.shape[:2]

    def test_detect_panels_has_reasonable_count(
        self, sample_image: np.ndarray,
    ) -> None:
        from scene.wireframe import detect_panels

        result = detect_panels(sample_image)
        assert len(result.panels) >= 3
        assert len(result.joints) >= 10

