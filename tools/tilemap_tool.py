#!/usr/bin/env python3
"""Parse and visualize the Tilemaps inside a Unity .unity scene file.

Usage:
  python3 tilemap_tool.py view <scene.unity> <out.png>
  python3 tilemap_tool.py info <scene.unity>
"""
import re
import sys
from collections import defaultdict

TILE_RE = re.compile(r"^\s*- first: \{x: (-?\d+), y: (-?\d+), z: (-?\d+)\}")
IDX_RE = re.compile(r"^\s*m_TileIndex: (\d+)")
NAME_RE = re.compile(r"^\s*m_Name: (.*)$")


def parse_scene(path):
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    # First pass: map GameObject fileID -> name
    go_name = {}
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        m = re.match(r"^--- !u!1 &(\d+)", line)
        if m:
            fid = m.group(1)
            # scan forward for m_Name within this GameObject block
            j = i + 1
            while j < n and not lines[j].startswith("--- "):
                nm = NAME_RE.match(lines[j])
                if nm:
                    go_name[fid] = nm.group(1).strip()
                    break
                j += 1
        i += 1

    # Second pass: find Tilemap blocks
    tilemaps = []  # list of dict: name, tiles {(x,y): tileindex}
    i = 0
    while i < n:
        if lines[i].rstrip() == "Tilemap:":
            # find m_GameObject
            go_id = None
            tiles = {}
            j = i + 1
            # read until m_Tiles:
            while j < n and lines[j].strip() != "m_Tiles:":
                gm = re.match(r"^\s*m_GameObject: \{fileID: (\d+)\}", lines[j])
                if gm:
                    go_id = gm.group(1)
                if lines[j].startswith("--- "):
                    break
                j += 1
            # now parse tiles until m_AnimatedTiles: or next block
            j += 1
            cur_pos = None
            while j < n:
                s = lines[j]
                if s.strip().startswith("m_AnimatedTiles:") or s.startswith("--- "):
                    break
                tm = TILE_RE.match(s)
                if tm:
                    cur_pos = (int(tm.group(1)), int(tm.group(2)))
                else:
                    im = IDX_RE.match(s)
                    if im and cur_pos is not None:
                        tiles[cur_pos] = int(im.group(1))
                        cur_pos = None
                j += 1
            name = go_name.get(go_id, f"GO{go_id}")
            tilemaps.append({"name": name, "go_id": go_id, "tiles": tiles})
            i = j
        else:
            i += 1
    return tilemaps


def bounds(tilemaps):
    xs = []
    ys = []
    for tm in tilemaps:
        for (x, y) in tm["tiles"].keys():
            xs.append(x)
            ys.append(y)
    return min(xs), max(xs), min(ys), max(ys)


PALETTE = [
    (0, 0, 0), (231, 76, 60), (46, 204, 113), (52, 152, 219),
    (241, 196, 15), (155, 89, 182), (26, 188, 156), (230, 126, 34),
    (149, 165, 166), (236, 240, 241), (192, 57, 43), (39, 174, 96),
    (41, 128, 185), (243, 156, 18), (142, 68, 173), (22, 160, 133),
    (211, 84, 0), (127, 140, 141), (189, 195, 199), (52, 73, 94),
    (44, 62, 80), (241, 148, 138), (130, 224, 170), (133, 193, 233),
    (247, 220, 111), (195, 155, 211), (115, 198, 182), (229, 152, 102),
]


def view(path, out):
    from PIL import Image, ImageDraw
    tms = parse_scene(path)
    minx, maxx, miny, maxy = bounds(tms)
    W = maxx - minx + 1
    H = maxy - miny + 1
    cell = 8
    img = Image.new("RGB", (W * cell, H * cell), (250, 250, 252))
    d = ImageDraw.Draw(img)

    def px(x, y):
        # world y up -> image y down
        return (x - minx) * cell, (maxy - y) * cell

    # draw each tilemap; solid/main tilemap first
    for ti, tm in enumerate(tms):
        for (x, y), idx in tm["tiles"].items():
            color = PALETTE[idx % len(PALETTE)]
            x0, y0 = px(x, y)
            d.rectangle([x0, y0, x0 + cell - 1, y0 + cell - 1], fill=color)

    # grid lines every 10 world units + axis labels
    for gx in range(minx, maxx + 2, 10):
        x0, _ = px(gx, maxy)
        d.line([(x0, 0), (x0, H * cell)], fill=(200, 200, 200))
        d.text((x0 + 1, 1), str(gx), fill=(0, 0, 0))
    for gy in range(miny, maxy + 2, 10):
        _, y0 = px(minx, gy)
        d.line([(0, y0), (W * cell, y0)], fill=(200, 200, 200))
        d.text((1, y0 + 1), str(gy), fill=(0, 0, 0))

    img.save(out)
    print(f"bounds x:[{minx},{maxx}] y:[{miny},{maxy}] size {W}x{H} -> {out}")


def info(path):
    tms = parse_scene(path)
    for tm in tms:
        tiles = tm["tiles"]
        if not tiles:
            print(f"[{tm['name']}] (go {tm['go_id']}): 0 tiles")
            continue
        xs = [p[0] for p in tiles]
        ys = [p[1] for p in tiles]
        idx_count = defaultdict(int)
        for v in tiles.values():
            idx_count[v] += 1
        print(f"[{tm['name']}] (go {tm['go_id']}): {len(tiles)} tiles "
              f"x:[{min(xs)},{max(xs)}] y:[{min(ys)},{max(ys)}]")
        top = sorted(idx_count.items(), key=lambda kv: -kv[1])
        print("   tileIndex counts:", ", ".join(f"{k}:{v}" for k, v in top))


def ascii_region(path, x0, x1, tmname=None):
    tms = parse_scene(path)
    # merge selected tilemaps
    solid = {}
    marks = {}
    for tm in tms:
        for (x, y), idx in tm["tiles"].items():
            if tm["name"] == "Ice_Ground":
                solid[(x, y)] = idx
            elif tm["name"] == "Gai":
                marks[(x, y)] = "^"
            else:
                marks.setdefault((x, y), "*")
    ys = [p[1] for p in solid]
    miny, maxy = min(ys), max(ys)
    print(f"columns x=[{x0},{x1}] y top={maxy} bottom={miny}  (#=Ice_Ground, ^=Gai, *=Snow, .=empty)")
    header = "     " + "".join(str(x % 10) for x in range(x0, x1 + 1))
    print(header)
    for y in range(maxy, miny - 1, -1):
        row = []
        for x in range(x0, x1 + 1):
            if (x, y) in marks and (x, y) not in solid:
                row.append(marks[(x, y)])
            elif (x, y) in solid:
                row.append("#")
            else:
                row.append(".")
        print(f"{y:4d} " + "".join(row))


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "view":
        view(sys.argv[2], sys.argv[3])
    elif cmd == "info":
        info(sys.argv[2])
    elif cmd == "ascii":
        ascii_region(sys.argv[2], int(sys.argv[3]), int(sys.argv[4]))
