"""Read-only checks for the imported SOLITUDE starting room. Run with Python 3."""
from __future__ import annotations

import hashlib
import json
import re
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parent
ATLAS = ROOT / "Art/solitude-atlas-32.png"
DOOR = ROOT / "Art/door-opening-spritesheet.png"
SCENE = ROOT / "Scenes/SOLITUDE_Start.unity"


def png_size(path: Path) -> tuple[int, int]:
    data = path.read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", f"Not PNG: {path}"
    return struct.unpack(">II", data[16:24])


def assert_import(path: Path, size: tuple[int, int], prefix: str, count: int) -> None:
    assert png_size(path) == size, f"Wrong dimensions: {path}"
    meta = path.with_suffix(path.suffix + ".meta").read_text()
    assert "spriteMode: 2" in meta and "filterMode: 0" in meta
    assert "spritePixelsToUnits: 32" in meta
    assert len(re.findall(rf"^\s+name: {prefix}_\d\d$", meta, re.M)) == count
    for x, y, w, h in re.findall(r"rect:\s*\{x: (\d+), y: (\d+), width: (\d+), height: (\d+)\}", meta):
        if int(w) == (64 if prefix == "door" else 32):
            assert int(h) == int(w)
            assert int(x) % int(w) == int(y) % int(h) == 0


def scene_sections() -> dict[str, str]:
    text = SCENE.read_text()
    blocks = re.split(r"(?=^--- !u!)", text, flags=re.M)
    objects = {}
    for block in blocks:
        match = re.search(r"^--- !u!\d+ &(\d+)\n", block)
        if match:
            objects[match.group(1)] = block
    return objects


def tile_positions(objects: dict[str, str], name: str) -> set[tuple[int, int]]:
    ids = [id for id, block in objects.items() if re.search(rf"^  m_Name: {re.escape(name)}$", block, re.M)]
    assert len(ids) == 1, f"Expected one {name} GameObject"
    maps = [block for block in objects.values() if block.startswith("--- !u!1839735485")
            and f"m_GameObject: {{fileID: {ids[0]}}}" in block]
    assert len(maps) == 1, f"Expected one {name} Tilemap"
    return {(int(x), int(y)) for x, y in re.findall(r"- first: \{x: (-?\d+), y: (-?\d+), z: 0\}", maps[0])}


def main() -> None:
    assert_import(ATLAS, (256, 128), "atlas", 32)
    assert_import(DOOR, (576, 64), "door", 9)
    manifest = json.loads((ROOT / "Art/tile-manifest.json").read_text())
    assert manifest["tile_size"] == 32
    assert len(manifest["tiles"]) == 29
    objects = scene_sections()
    floor = tile_positions(objects, "Floor")
    north = tile_positions(objects, "North Wall")
    boundary = tile_positions(objects, "Rule Boundaries")
    assert floor == {(x, y) for y in range(1, 7) for x in range(1, 9)}
    assert north == {(x, y) for y in (7, 8) for x in range(1, 9) if x not in (4, 5)}
    assert boundary == ({(x, 0) for x in range(10)} |
                        {(0, y) for y in range(1, 9)} |
                        {(9, y) for y in range(1, 9)})
    assert len([1 for block in objects.values() if re.search(r"^  m_Name: SOLITUDE Sliding Door$", block, re.M)]) == 1
    for path in (ATLAS, DOOR):
        print(f"{path.name}: {png_size(path)}, sha256={hashlib.sha256(path.read_bytes()).hexdigest()}")
    print("PASS: 48 floor cells in six rows, two-high north wall, vertical sides, 2x2 door opening")


if __name__ == "__main__":
    main()
