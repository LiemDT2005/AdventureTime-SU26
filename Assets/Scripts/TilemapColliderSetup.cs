using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Merges adjacent tile colliders into one composite surface so characters
/// do not snag on seams between tiles.
/// </summary>
public static class TilemapColliderSetup
{
    private const float MinExtrusionFactor = 0.00001f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureSceneTilemaps()
    {
        ConfigureAllTilemaps();
    }

    public static void ConfigureAllTilemaps()
    {
        TilemapCollider2D[] tileColliders = Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None);
        foreach (TilemapCollider2D tileCollider in tileColliders)
        {
            Configure(tileCollider);
        }
    }

    public static void Configure(TilemapCollider2D tileCollider)
    {
        if (tileCollider == null || tileCollider.isTrigger)
        {
            return;
        }

        CompositeCollider2D composite = tileCollider.GetComponent<CompositeCollider2D>();
        if (composite == null)
        {
            return;
        }

        bool changed = false;

        if (tileCollider.compositeOperation != Collider2D.CompositeOperation.Merge)
        {
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            changed = true;
        }

        if (tileCollider.extrusionFactor < MinExtrusionFactor)
        {
            tileCollider.extrusionFactor = MinExtrusionFactor;
            changed = true;
        }

        if (composite.geometryType != CompositeCollider2D.GeometryType.Polygons)
        {
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            changed = true;
        }

        if (changed)
        {
            composite.GenerateGeometry();
        }
    }
}
