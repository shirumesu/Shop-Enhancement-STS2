"""Build the ShopEnhancement mod folder without a Godot editor.

Writes <out>/ShopEnhancement.dll (+ .pdb), <out>/ShopEnhancement.pck and <out>/mod_manifest.json.
The game loads the DLL with LoadFromAssemblyPath, so the .pdb next to it gives file:line in logged stack traces.

The .pck is a Godot 4.5 resource pack (format v3) containing:
  res://mod_manifest.json
  res://ShopEnhancement/localization/**          (loc tables + BaseLib settings_ui)
  res://ShopEnhancement/mod_image.png(.import)   (+ the imported .ctex it remaps to)

Only the Python standard library is needed.
"""

import argparse
import hashlib
import shutil
import struct
from pathlib import Path

MOD_ID = "ShopEnhancement"
GODOT_VERSION = (4, 5, 1)
PCK_FORMAT_VERSION = 3
PCK_FLAG_REL_FILEBASE = 1 << 1
PCK_ALIGN = 16

# uid used by the original Godot import of mod_image.png; keep it stable.
MOD_IMAGE_UID = "uid://pqornh7o4i0l"

# Godot Image::Format values
IMAGE_FORMAT_RGB8 = 4
IMAGE_FORMAT_RGBA8 = 5
# CompressedTexture2D data formats
CTEX_DATA_FORMAT_PNG = 1


def align(n: int) -> int:
    return (n + PCK_ALIGN - 1) // PCK_ALIGN * PCK_ALIGN


def png_info(data: bytes) -> tuple[int, int, int]:
    if data[:8] != b"\x89PNG\r\n\x1a\n" or data[12:16] != b"IHDR":
        raise ValueError("mod_image.png is not a valid PNG")
    width, height, bit_depth, color_type = struct.unpack(">IIBB", data[16:26])
    if bit_depth != 8:
        raise ValueError(f"mod_image.png must be 8-bit, got {bit_depth}-bit")
    if color_type == 2:
        return width, height, IMAGE_FORMAT_RGB8
    if color_type == 6:
        return width, height, IMAGE_FORMAT_RGBA8
    raise ValueError(f"mod_image.png must be RGB or RGBA (PNG color type {color_type})")


def make_ctex(png: bytes) -> bytes:
    """CompressedTexture2D ("GST2") holding a lossless PNG, no mipmaps."""
    width, height, image_format = png_info(png)
    header = struct.pack(
        "<4sIIIIi3I",
        b"GST2",
        1,  # ctex version
        width,
        height,
        0x0D000000,  # texture flags as written by the original import
        -1,  # mipmap limit
        0, 0, 0,
    )
    image = struct.pack("<IHHII", CTEX_DATA_FORMAT_PNG, width, height, 0, image_format)
    image += struct.pack("<I", len(png)) + png
    return header + image


def import_file(res_path: str, ctex_res_path: str) -> bytes:
    return (
        "[remap]\n\n"
        'importer="texture"\n'
        'type="CompressedTexture2D"\n'
        f'uid="{MOD_IMAGE_UID}"\n'
        f'path="{ctex_res_path}"\n'
        "metadata={\n"
        '"vram_texture": false\n'
        "}\n"
    ).encode("utf-8")


def collect_files(project: Path) -> dict[str, bytes]:
    files: dict[str, bytes] = {}

    files["mod_manifest.json"] = (project / "mod_manifest.json").read_bytes()

    loc_dir = project / MOD_ID / "localization"
    loc_files = sorted(p for p in loc_dir.rglob("*") if p.is_file())
    if not loc_files:
        raise FileNotFoundError(f"no localization files under {loc_dir}")
    for p in loc_files:
        files[p.relative_to(project).as_posix()] = p.read_bytes()

    image_rel = f"{MOD_ID}/mod_image.png"
    png = (project / image_rel).read_bytes()
    image_res = f"res://{image_rel}"
    path_md5 = hashlib.md5(image_res.encode("utf-8")).hexdigest()
    ctex_rel = f".godot/imported/mod_image.png-{path_md5}.ctex"
    files[ctex_rel] = make_ctex(png)
    files[image_rel + ".import"] = import_file(image_res, f"res://{ctex_rel}")

    return files


def write_pck(files: dict[str, bytes], out: Path) -> None:
    header_size = 4 + 4 * 5 + 8 + 8 + 16 * 4
    file_base = align(header_size)

    blob = bytearray()
    entries = []
    for path, data in files.items():
        entries.append((path, len(blob), data))
        blob += data
        blob += b"\0" * (align(len(blob)) - len(blob))

    directory = bytearray(struct.pack("<I", len(entries)))
    for path, offset, data in entries:
        raw = path.encode("utf-8")
        padded_len = (len(raw) + 4) // 4 * 4  # always at least one NUL
        directory += struct.pack("<I", padded_len) + raw + b"\0" * (padded_len - len(raw))
        directory += struct.pack("<QQ", offset, len(data))
        directory += hashlib.md5(data).digest()
        directory += struct.pack("<I", 0)

    dir_offset = file_base + len(blob)
    header = struct.pack(
        "<4sIIIIIQQ",
        b"GDPC",
        PCK_FORMAT_VERSION,
        *GODOT_VERSION,
        PCK_FLAG_REL_FILEBASE,
        file_base,
        dir_offset,
    ) + b"\0" * (16 * 4)
    header += b"\0" * (file_base - len(header))

    out.write_bytes(header + blob + directory)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--project", required=True, type=Path)
    ap.add_argument("--dll", required=True, type=Path)
    ap.add_argument("--out", required=True, type=Path)
    args = ap.parse_args()

    out: Path = args.out
    out.mkdir(parents=True, exist_ok=True)

    files = collect_files(args.project)
    write_pck(files, out / f"{MOD_ID}.pck")
    shutil.copy2(args.dll, out / f"{MOD_ID}.dll")
    shutil.copy2(args.dll.with_suffix(".pdb"), out / f"{MOD_ID}.pdb")
    shutil.copy2(args.project / "mod_manifest.json", out / "mod_manifest.json")

    print(f"[pack_mod] {len(files)} files -> {out / (MOD_ID + '.pck')}")
    for path in files:
        print(f"[pack_mod]   res://{path}")


if __name__ == "__main__":
    main()
