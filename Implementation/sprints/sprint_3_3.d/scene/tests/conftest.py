"""Shared fixtures for scene tests."""
from __future__ import annotations

import json

import pytest

from scene.constants import JSON_PATH


@pytest.fixture(scope="module")
def scene_data() -> dict:
    assert JSON_PATH.exists(), f"{JSON_PATH} not found"
    return json.loads(JSON_PATH.read_text())
