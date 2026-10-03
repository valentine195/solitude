#!/usr/bin/env python3
"""Compose a flattened layered-map preview from a base image and prop placements."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

from PIL import Image


RESAMPLING_CHOICES = ("auto", "nearest", "lanczos")


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def resolve_path(value: str, roots: list[Path]) -> Path:
    path = Path(value)
    if path.is_absolute():
        return path
    for root in roots:
        candidate = root / path
        if candidate.exists():
            return candidate
    return roots[0] / path


def resolve_resampling(mode: str, pixel_art: bool) -> tuple[Image.Resampling, str]:
    if pixel_art:
        if mode == "lanczos":
            raise ValueError("--pixel-art is incompatible with --resampling lanczos; use nearest or auto.")
        return Image.Resampling.NEAREST, "nearest"
    if mode == "nearest":
        return Image.Resampling.NEAREST, "nearest"
    if mode in ("auto", "lanczos"):
        return Image.Resampling.LANCZOS, "lanczos"
    raise ValueError(f"Unsupported resampling mode: {mode}")


def validate_integer_uniform_scale(
    source_size: tuple[int, int], target_size: tuple[int, int], *, label: str
) -> str:
    sw, sh = source_size
    tw, th = target_size
    if min(sw, sh, tw, th) <= 0:
        raise ValueError(f"{label}: dimensions must be positive, got {source_size} -> {target_size}.")
    if source_size == target_size:
        return "1x"

    def axis_scale(src: int, dst: int) -> tuple[str, int]:
        if dst > src:
            if dst % src:
                raise ValueError(
                    f"{label}: non-integer upscale {source_size} -> {target_size}; "
                    "pixel-art preview requires integer scaling."
                )
            return ("up", dst // src)
        if src > dst:
            if src % dst:
                raise ValueError(
                    f"{label}: non-integer downscale {source_size} -> {target_size}; "
                    "pixel-art preview requires an integer divisor."
                )
            return ("down", src // dst)
        return ("same", 1)

    x_mode, x_factor = axis_scale(sw, tw)
    y_mode, y_factor = axis_scale(sh, th)
    if (x_mode, x_factor) != (y_mode, y_factor):
        raise ValueError(
            f"{label}: non-uniform pixel-art scale {source_size} -> {target_size}; "
            f"x={x_mode} {x_factor}x, y={y_mode} {y_factor}x."
        )
    return f"{x_mode}-{x_factor}x"


def load_props(data: Any) -> list[dict[str, Any]]:
    if isinstance(data, list):
        return data
    if isinstance(data, dict):
        for key in ("props", "foreground", "objects"):
            value = data.get(key)
            if isinstance(value, list):
                if key == "foreground":
                    return [{**item, "layer": "foreground"} for item in value]
                return value
    raise ValueError("Placement JSON must be a list or an object with a 'props' list.")


def placement_xy(prop: dict[str, Any], width: int, height: int) -> tuple[int, int]:
    anchor = str(prop.get("anchor", "center-bottom"))
    x = float(prop.get("x", 0))
    y = float(prop.get("y", 0))

    if anchor == "top-left":
        left = x
        top = y
    elif anchor == "center":
        left = x - width / 2
        top = y - height / 2
    elif anchor == "bottom-left":
        left = x
        top = y - height
    else:
        left = x - width / 2
        top = y - height
    return round(left), round(top)


def paste_prop(
    canvas: Image.Image,
    prop: dict[str, Any],
    roots: list[Path],
    *,
    resampler: Image.Resampling,
    require_integer_scale: bool,
) -> dict[str, Any]:
    image_key = prop.get("image") or prop.get("path")
    if not image_key:
        raise ValueError(f"Prop is missing image/path: {prop}")
    image_path = resolve_path(str(image_key), roots)
    if not image_path.exists():
        raise FileNotFoundError(f"Prop image not found: {image_path}")

    img = Image.open(image_path).convert("RGBA")
    source_size = img.size
    width = int(prop.get("w", prop.get("width", img.width)))
    height = int(prop.get("h", prop.get("height", img.height)))
    if width <= 0 or height <= 0:
        raise ValueError(f"Invalid prop size for {image_path}: {width}x{height}")

    scale_operation = "1x"
    if (width, height) != img.size:
        if require_integer_scale:
            scale_operation = validate_integer_uniform_scale(
                img.size, (width, height), label=str(prop.get("id", image_path.stem))
            )
        img = img.resize((width, height), resampler)

    opacity = float(prop.get("opacity", 1.0))
    if opacity < 1:
        alpha = img.getchannel("A").point(lambda value: int(value * max(0.0, min(1.0, opacity))))
        img.putalpha(alpha)

    left, top = placement_xy(prop, width, height)
    canvas.alpha_composite(img, (left, top))
    return {
        "id": prop.get("id", image_path.stem),
        "image": str(image_path),
        "source_size": list(source_size),
        "left": left,
        "top": top,
        "w": width,
        "h": height,
        "scale_operation": scale_operation,
        "sortY": prop.get("sortY", prop.get("y", top + height)),
        "layer": prop.get("layer", "props"),
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", required=True, type=Path)
    parser.add_argument("--placements", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--report", type=Path)
    parser.add_argument("--project-root", type=Path, default=Path.cwd())
    parser.add_argument(
        "--resampling",
        choices=RESAMPLING_CHOICES,
        default="auto",
        help="Resize filter. auto uses NEAREST with --pixel-art, otherwise LANCZOS.",
    )
    parser.add_argument(
        "--pixel-art",
        action="store_true",
        help="Force pixel-safe NEAREST resampling when preview props must be resized.",
    )
    parser.add_argument(
        "--require-integer-scale",
        action="store_true",
        help="Reject non-uniform or non-integer prop resize factors.",
    )
    return parser


def main() -> None:
    args = build_parser().parse_args()
    resampler, resampling_name = resolve_resampling(args.resampling, args.pixel_art)

    base = Image.open(args.base).convert("RGBA")
    data = read_json(args.placements)
    props = load_props(data)
    roots = [args.placements.parent, args.base.parent, args.project_root]

    props_layer = [prop for prop in props if str(prop.get("layer", "props")) != "foreground"]
    foreground_layer = [prop for prop in props if str(prop.get("layer", "props")) == "foreground"]
    props_layer.sort(key=lambda item: float(item.get("sortY", item.get("y", 0))))
    foreground_layer.sort(key=lambda item: float(item.get("sortY", item.get("y", 0))))

    pasted = []
    for prop in props_layer + foreground_layer:
        pasted.append(
            paste_prop(
                base,
                prop,
                roots,
                resampler=resampler,
                require_integer_scale=args.require_integer_scale,
            )
        )

    args.output.parent.mkdir(parents=True, exist_ok=True)
    base.save(args.output)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(
            json.dumps(
                {
                    "base": str(args.base),
                    "placements": str(args.placements),
                    "output": str(args.output),
                    "processing": {
                        "pixel_art": args.pixel_art,
                        "resampling": resampling_name,
                        "require_integer_scale": args.require_integer_scale,
                    },
                    "pasted": pasted,
                },
                indent=2,
            ),
            encoding="utf-8",
        )
    print(str(args.output.resolve()))


if __name__ == "__main__":
    main()
