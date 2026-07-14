#!/usr/bin/env python3
"""Expand Map2 to the right: extend Ice_Ground terrain, add Gai spikes,
open a doorway in the right wall and place a LevelDoor to Map3.

Idempotent-ish: run once on a clean scene. Creates a .bak backup.
"""
import re
import sys
import shutil

SCENE = "Assets/Scenes/Game/Map2.unity"
ICE_ANCHOR = "1978652700"      # Ice_Ground Tilemap component
GAI_ANCHOR = "700501112"       # Gai Tilemap component
CAM_ANCHOR = "725555900"       # CameraBound BoxCollider2D
DOOR_SCRIPT_GUID = "30d4696947c64b5595e352bcbf4ceac5"
DOOR_SPRITE = "{fileID: 21300000, guid: 5260a3a99fe7a6841bac3c5787280897, type: 3}"
SPRITE_MAT = "{fileID: 2100000, guid: a97c105638bdf8b4a8650670310a4cd3, type: 2}"
FLAGS = 1073741825

# ---- design of the new right-hand section --------------------------------
BOTTOM = -33

def build_design():
    # column -> surface top (topmost solid y). Fill from top down to BOTTOM.
    surface = {}
    for x in range(114, 121):   # 114..120 flat entry corridor
        surface[x] = -8
    # 121,122 = spike pit (deep), leave a gap the player jumps over
    surface[121] = -13
    surface[122] = -13
    for x in range(123, 127):   # 123..126 landing
        surface[x] = -8
    surface[127] = -7           # steps up
    surface[128] = -7
    for x in range(129, 150):   # 129..149 raised plateau toward the door
        surface[x] = -6
    surface[150] = -3           # rising mountain cap on the far right
    surface[151] = 0
    surface[152] = 3

    ice_add = []
    for x, top in surface.items():
        for y in range(top, BOTTOM - 1, -1):
            ice_add.append((x, y))

    ice_remove = {(113, -7), (113, -6), (113, -5)}   # doorway in the wall
    gai_add = [(121, -12), (122, -12)]               # spikes at pit floor
    return ice_add, ice_remove, gai_add


# ---- generic scene helpers -----------------------------------------------
def split_docs(text):
    """Split a Unity YAML scene into (header, [doc_texts])."""
    lines = text.split("\n")
    # find first '--- '
    start = next(i for i, l in enumerate(lines) if l.startswith("--- "))
    header = "\n".join(lines[:start])
    docs = []
    cur = [lines[start]]
    for l in lines[start + 1:]:
        if l.startswith("--- "):
            docs.append("\n".join(cur))
            cur = [l]
        else:
            cur.append(l)
    docs.append("\n".join(cur))
    return header, docs


def tile_entry(x, y, ti=0, si=0, mi=0, ci=0):
    return (
        f"  - first: {{x: {x}, y: {y}, z: 0}}\n"
        f"    second:\n"
        f"      serializedVersion: 2\n"
        f"      m_TileIndex: {ti}\n"
        f"      m_TileSpriteIndex: {si}\n"
        f"      m_TileMatrixIndex: {mi}\n"
        f"      m_TileColorIndex: {ci}\n"
        f"      m_TileObjectToInstantiateIndex: 65535\n"
        f"      dummyAlignment: 0\n"
        f"      m_AllTileFlags: {FLAGS}"
    )


ENTRY_RE = re.compile(
    r"  - first: \{x: (-?\d+), y: (-?\d+), z: -?\d+\}\n"
    r"    second:\n"
    r"      serializedVersion: 2\n"
    r"      m_TileIndex: (\d+)\n"
    r"      m_TileSpriteIndex: (\d+)\n"
    r"      m_TileMatrixIndex: (\d+)\n"
    r"      m_TileColorIndex: (\d+)\n"
    r"      m_TileObjectToInstantiateIndex: \d+\n"
    r"      dummyAlignment: \d+\n"
    r"      m_AllTileFlags: -?\d+"
)


def edit_tilemap_doc(doc, add, remove):
    lines = doc.split("\n")
    ti = next(i for i, l in enumerate(lines) if l == "  m_Tiles:")
    te = next(i for i in range(ti, len(lines)) if lines[i] == "  m_AnimatedTiles: {}")
    tiles_block = "\n".join(lines[ti + 1:te])

    existing = {}
    order = []
    for m in ENTRY_RE.finditer(tiles_block):
        x, y = int(m.group(1)), int(m.group(2))
        rec = (int(m.group(3)), int(m.group(4)), int(m.group(5)), int(m.group(6)))
        if (x, y) not in existing:
            order.append((x, y))
        existing[(x, y)] = rec

    for pos in remove:
        if pos in existing:
            del existing[pos]

    new_order = [p for p in order if p in existing]
    for (x, y) in add:
        if (x, y) not in existing:
            existing[(x, y)] = (0, 0, 0, 0)
            new_order.append((x, y))

    # rebuild entries
    entry_texts = []
    for (x, y) in new_order:
        ti_, si_, mi_, ci_ = existing[(x, y)]
        entry_texts.append(tile_entry(x, y, ti_, si_, mi_, ci_))
    new_tiles_block = "\n".join(entry_texts)

    lines = lines[:ti + 1] + new_tiles_block.split("\n") + lines[te:]
    doc = "\n".join(lines)

    # recompute ref counts
    counts = {"asset": {}, "sprite": {}, "matrix": {}, "color": {}}
    for (ti_, si_, mi_, ci_) in existing.values():
        counts["asset"][ti_] = counts["asset"].get(ti_, 0) + 1
        counts["sprite"][si_] = counts["sprite"].get(si_, 0) + 1
        counts["matrix"][mi_] = counts["matrix"].get(mi_, 0) + 1
        counts["color"][ci_] = counts["color"].get(ci_, 0) + 1

    doc = rewrite_refcounts(doc, "m_TileAssetArray", "m_TileSpriteArray", counts["asset"])
    doc = rewrite_refcounts(doc, "m_TileSpriteArray", "m_TileMatrixArray", counts["sprite"])
    doc = rewrite_refcounts(doc, "m_TileMatrixArray", "m_TileColorArray", counts["matrix"])
    doc = rewrite_refcounts(doc, "m_TileColorArray", "m_TileObjectToInstantiateArray", counts["color"])

    # update origin/size to contain everything
    xs = [p[0] for p in existing]
    ys = [p[1] for p in existing]
    doc = update_bounds(doc, min(xs), max(xs), min(ys), max(ys))
    return doc


def rewrite_refcounts(doc, start_key, end_key, counts):
    lines = doc.split("\n")
    s = next(i for i, l in enumerate(lines) if l == f"  {start_key}:")
    e = next(i for i in range(s + 1, len(lines)) if lines[i].startswith(f"  {end_key}:"))
    idx = -1
    for i in range(s + 1, e):
        if lines[i] == "  - serializedVersion: 2":
            idx += 1
            # next line is m_RefCount
            rc_line = i + 1
            assert lines[rc_line].startswith("    m_RefCount:"), lines[rc_line]
            lines[rc_line] = f"    m_RefCount: {counts.get(idx, 0)}"
    return "\n".join(lines)


def update_bounds(doc, minx, maxx, miny, maxy):
    lines = doc.split("\n")
    for i, l in enumerate(lines):
        mo = re.match(r"  m_Origin: \{x: (-?\d+), y: (-?\d+), z: (-?\d+)\}", l)
        if mo:
            ox = min(int(mo.group(1)), minx)
            oy = min(int(mo.group(2)), miny)
            lines[i] = f"  m_Origin: {{x: {ox}, y: {oy}, z: {mo.group(3)}}}"
            origin = (ox, oy)
        ms = re.match(r"  m_Size: \{x: (\d+), y: (\d+), z: (\d+)\}", l)
        if ms:
            size_line = i
    # recompute size using the (possibly updated) origin
    for i, l in enumerate(lines):
        mo = re.match(r"  m_Origin: \{x: (-?\d+), y: (-?\d+), z: (-?\d+)\}", l)
        if mo:
            ox, oy = int(mo.group(1)), int(mo.group(2))
    ms = re.match(r"  m_Size: \{x: (\d+), y: (\d+), z: (\d+)\}", lines[size_line])
    sx = max(int(ms.group(1)), maxx - ox + 1)
    sy = max(int(ms.group(2)), maxy - oy + 1)
    lines[size_line] = f"  m_Size: {{x: {sx}, y: {sy}, z: {ms.group(3)}}}"
    return "\n".join(lines)


def edit_camera_bound(doc):
    # widen box so the camera can follow into the new area
    lines = doc.split("\n")
    for i, l in enumerate(lines):
        if l.startswith("  m_Offset:"):
            lines[i] = "  m_Offset: {x: 81.11341, y: -6.6956196}"
        if l.startswith("  m_Size:"):
            lines[i] = "  m_Size: {x: 170.08899, y: 52.5948}"
    return "\n".join(lines)


DOOR_DOCS = """--- !u!1 &1751500001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 1751500002}
  - component: {fileID: 1751500003}
  - component: {fileID: 1751500004}
  - component: {fileID: 1751500005}
  m_Layer: 0
  m_Name: DoorToMap3
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &1751500002
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1751500001}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 147.5, y: -3.5, z: 0}
  m_LocalScale: {x: 3, y: 3, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!212 &1751500003
SpriteRenderer:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1751500001}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - __MAT__
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 10
  m_MaskInteraction: 0
  m_Sprite: __SPRITE__
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {x: 1, y: 1}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_SpriteSortPoint: 0
--- !u!61 &1751500004
BoxCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1751500001}
  m_Enabled: 1
  serializedVersion: 3
  m_Density: 1
  m_Material: {fileID: 0}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_ForceSendLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ForceReceiveLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ContactCaptureLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_CallbackLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_IsTrigger: 1
  m_UsedByEffector: 0
  m_CompositeOperation: 0
  m_CompositeOrder: 0
  m_Offset: {x: 0, y: 0}
  m_SpriteTilingProperty:
    border: {x: 0, y: 0, z: 0, w: 0}
    pivot: {x: 0, y: 0}
    oldSize: {x: 0, y: 0}
    newSize: {x: 0, y: 0}
    adaptiveTilingThreshold: 0
    drawMode: 0
    adaptiveTiling: 0
  m_AutoTiling: 0
  m_Size: {x: 0.8, y: 1.4}
  m_EdgeRadius: 0
--- !u!114 &1751500005
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1751500001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: __GUID__, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  targetScene: Map3
  playerTag: Player
"""


def main():
    with open(SCENE, "r", encoding="utf-8") as f:
        text = f.read()

    for fid in ("1751500001", "1751500002", "1751500003", "1751500004", "1751500005"):
        if f"&{fid}" in text:
            print(f"ERROR: fileID {fid} already present; aborting.")
            sys.exit(1)

    shutil.copyfile(SCENE, SCENE + ".bak")

    ice_add, ice_remove, gai_add = build_design()

    header, docs = split_docs(text)
    out = []
    for doc in docs:
        first = doc.split("\n", 1)[0]
        if first.endswith(f"&{ICE_ANCHOR}"):
            doc = edit_tilemap_doc(doc, ice_add, ice_remove)
        elif first.endswith(f"&{GAI_ANCHOR}"):
            doc = edit_tilemap_doc(doc, gai_add, set())
        elif first.endswith(f"&{CAM_ANCHOR}"):
            doc = edit_camera_bound(doc)
        out.append(doc)

    door = (DOOR_DOCS.replace("__MAT__", SPRITE_MAT)
                     .replace("__SPRITE__", DOOR_SPRITE)
                     .replace("__GUID__", DOOR_SCRIPT_GUID)).rstrip("\n")
    out.append(door)

    new_text = header + "\n" + "\n".join(out) + "\n"
    with open(SCENE, "w", encoding="utf-8") as f:
        f.write(new_text)
    print(f"Added {len(ice_add)} ground tiles, removed {len(ice_remove)}, "
          f"{len(gai_add)} spikes. Door + camera bound updated.")


if __name__ == "__main__":
    main()
