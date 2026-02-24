using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Programmatically generates the "Sunken Spires" level layout.
/// Attach to a GameObject in the scene, assign tilemaps and tiles in the Inspector,
/// then use the custom Inspector button "Generate Level" to paint the map.
/// 
/// Level flow: [A] Start → [B] Archivist → [C] Evolution Pedestal
///             → [D] Coolant Pools → [E] Vent Puzzles → [F] Survivor's Hub → [G] Boss
/// </summary>
[ExecuteInEditMode]
public class SunkenSpiresGenerator : MonoBehaviour
{
    // =========================================================
    // Tilemaps
    // =========================================================
    [Header("Tilemaps — assign from your Grid child objects")]
    [Tooltip("Solid ground, walls, and ceilings (has Tilemap Collider 2D)")]
    public Tilemap groundTilemap;

    [Tooltip("One-way platforms (has Platform Effector 2D)")]
    public Tilemap platformTilemap;

    [Tooltip("Water volume tiles (trigger collider, tag = Water)")]
    public Tilemap waterTilemap;

    [Tooltip("Background / decorative layer (no collider)")]
    public Tilemap backgroundTilemap;

    // =========================================================
    // Tiles — assign your actual TileBase assets
    // =========================================================
    [Header("Tiles — drag your tile assets here")]
    public TileBase solidTile;       // main ground / wall tile
    public TileBase platformTile;    // thin one-way platform tile
    public TileBase waterFillTile;   // water volume tile
    public TileBase bgTile;          // background fill tile

    // =========================================================
    // Spawn prefabs (optional — placed as GameObjects)
    // =========================================================
    [Header("Spawn Prefabs (optional)")]
    public GameObject playerSpawnMarker;
    public GameObject npc1Prefab;               // [B] The Archivist
    public GameObject npc2Prefab;               // [F] Survivor's Hub
    public GameObject evolutionPedestalPrefab;  // [C]
    public GameObject bossPrefab;               // [G] The Sentinel / Mech Boss
    public GameObject levelEntrancePrefab;      // connects rooms

    // =========================================================
    // Spawn point records (read by other scripts at runtime)
    // =========================================================
    [HideInInspector] public Vector3 playerSpawnWorld;
    [HideInInspector] public Vector3 bossSpawnWorld;

    // =========================================================
    // Internal: generated object tracking for clean re-generation
    // =========================================================
    private const string GeneratedTag = "GeneratedLevelObject";
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    // =========================================================
    // Public entry point — called by the custom Editor button
    // =========================================================
    public void GenerateLevel()
    {
        ClearAll();

        if (groundTilemap == null || solidTile == null)
        {
            Debug.LogError("[SunkenSpiresGenerator] Assign at least groundTilemap and solidTile before generating.");
            return;
        }

        BuildBackground();

        BuildSectionA_Start();
        BuildSectionB_Archivist();
        BuildSectionC_EvolutionPedestal();
        BuildSectionD_CoolantPools();
        BuildSectionE_VentPuzzles();
        BuildSectionF_SurvivorsHub();
        BuildSectionG_BossRoom();

        BuildTransitionPassages();
        SpawnObjects();

        Debug.Log("[SunkenSpiresGenerator] Level generated successfully!");
    }

    // =========================================================
    // Clear everything generated previously
    // =========================================================
    public void ClearAll()
    {
        if (groundTilemap)     groundTilemap.ClearAllTiles();
        if (platformTilemap)   platformTilemap.ClearAllTiles();
        if (waterTilemap)      waterTilemap.ClearAllTiles();
        if (backgroundTilemap) backgroundTilemap.ClearAllTiles();

        // Remove previously spawned objects stored in our list
        foreach (var go in spawnedObjects)
            if (go != null) DestroyImmediate(go);
        spawnedObjects.Clear();

        Debug.Log("[SunkenSpiresGenerator] Level cleared.");
    }

    // =========================================================
    // SECTION A — Start (Slime Form)
    // Bottom-left entry room. Small, enclosed, safe.
    //   x: 0-22   y: 0-9
    // =========================================================
    void BuildSectionA_Start()
    {
        // Floor & ceiling
        FillRow(groundTilemap, solidTile, 0, 22, 0);
        FillRow(groundTilemap, solidTile, 0, 22, 9);
        // Left wall (world boundary)
        FillCol(groundTilemap, solidTile, 0, 0, 9);
        // Right wall is open (connects to B via passage at y=1-5)

        // Inner step platform to jump onto
        FillRow(platformTilemap, platformTile, 6, 14, 4);

        playerSpawnWorld = TileToWorld(3, 1);
        Debug.Log("[A] Start section built.");
    }

    // =========================================================
    // SECTION B — The Archivist (NPC 1)
    // Mid ground floor, right of A. NPC teaches lore.
    //   x: 22-50   y: 0-9
    // =========================================================
    void BuildSectionB_Archivist()
    {
        FillRow(groundTilemap, solidTile, 22, 50, 0);
        FillRow(groundTilemap, solidTile, 22, 50, 9);
        // Ceiling gap on right side opens upward to C

        // Platforms across room
        FillRow(platformTilemap, platformTile, 27, 35, 4);
        FillRow(platformTilemap, platformTile, 38, 46, 6);

        // NPC 1 stands at mid-floor
        // (Object placed in SpawnObjects)
        Debug.Log("[B] Archivist section built.");
    }

    // =========================================================
    // SECTION C — Evolution Pedestal (Slime → Human)
    // Vertical climb left of center. Unlocks human form.
    //   x: 5-28   y: 9-38
    // =========================================================
    void BuildSectionC_EvolutionPedestal()
    {
        // Left wall
        FillCol(groundTilemap, solidTile, 5, 9, 38);
        // Right wall
        FillCol(groundTilemap, solidTile, 28, 9, 38);
        // Ceiling
        FillRow(groundTilemap, solidTile, 5, 28, 38);
        // Floor (connects to B ceiling gap at y=9)

        // Climbing platforms — zigzag up the shaft
        FillRow(platformTilemap, platformTile, 7,  18, 13);
        FillRow(platformTilemap, platformTile, 15, 26, 18);
        FillRow(platformTilemap, platformTile, 7,  18, 23);
        FillRow(platformTilemap, platformTile, 15, 26, 28);

        // Pedestal platform at top
        FillRow(groundTilemap, solidTile, 10, 24, 33);
        FillRow(groundTilemap, solidTile, 10, 24, 34); // thick pedestal base

        Debug.Log("[C] Evolution Pedestal section built.");
    }

    // =========================================================
    // SECTION D — Coolant Pools (Water & Buoyancy)
    // Center of map. Water fills lower half. Slime floats, human sinks.
    //   x: 28-65   y: 0-18
    // =========================================================
    void BuildSectionD_CoolantPools()
    {
        // Floor
        FillRow(groundTilemap, solidTile, 28, 65, 0);
        // Side walls
        FillCol(groundTilemap, solidTile, 28, 0, 18);
        FillCol(groundTilemap, solidTile, 65, 0, 18);
        // Ceiling
        FillRow(groundTilemap, solidTile, 28, 65, 18);

        // Water fill — lower 8 rows
        if (waterTilemap != null && waterFillTile != null)
            FillRect(waterTilemap, waterFillTile, 29, 1, 65, 8);

        // Platforms above waterline for human form
        FillRow(platformTilemap, platformTile, 32, 44, 11);
        FillRow(platformTilemap, platformTile, 48, 60, 13);
        FillRow(platformTilemap, platformTile, 36, 56, 16);

        // Rock pillars in water (partial ground blocks)
        FillCol(groundTilemap, solidTile, 38, 1, 6);
        FillCol(groundTilemap, solidTile, 52, 1, 6);

        Debug.Log("[D] Coolant Pools section built.");
    }

    // =========================================================
    // SECTION E — Vent Puzzles (Needle Thread Dash)
    // Tall narrow vertical shaft. Requires dash to get through gaps.
    //   x: 60-78   y: 0-55
    // =========================================================
    void BuildSectionE_VentPuzzles()
    {
        // Shaft walls
        FillCol(groundTilemap, solidTile, 60, 0, 55);
        FillCol(groundTilemap, solidTile, 78, 0, 55);

        // Vent passage — series of narrow platforms with gaps that require dashing

        // Vent gate 1 (gap y=20-24, right side open gap at x=68-78)
        FillRow(groundTilemap, solidTile, 60, 68, 20);
        FillRow(groundTilemap, solidTile, 60, 68, 24);

        // Vent gate 2 (gap y=32-36, left side open gap at x=60-70)
        FillRow(groundTilemap, solidTile, 70, 78, 32);
        FillRow(groundTilemap, solidTile, 70, 78, 36);

        // Vent gate 3 (gap y=44-48, right open)
        FillRow(groundTilemap, solidTile, 60, 68, 44);
        FillRow(groundTilemap, solidTile, 60, 68, 48);

        // Footholds inside shaft
        FillRow(platformTilemap, platformTile, 62, 70, 15);
        FillRow(platformTilemap, platformTile, 68, 76, 28);
        FillRow(platformTilemap, platformTile, 62, 70, 40);
        FillRow(platformTilemap, platformTile, 68, 76, 52);

        Debug.Log("[E] Vent Puzzles section built.");
    }

    // =========================================================
    // SECTION F — Survivor's Hub (NPC 2)
    // Upper right safe area. Rest point before the boss.
    //   x: 65-100   y: 50-62
    // =========================================================
    void BuildSectionF_SurvivorsHub()
    {
        FillRow(groundTilemap, solidTile, 65, 100, 50);
        FillRow(groundTilemap, solidTile, 65, 100, 62);
        FillCol(groundTilemap, solidTile, 65, 50, 62);
        // Right side open — connects up to boss room

        // Cozy platforms
        FillRow(platformTilemap, platformTile, 70, 82, 54);
        FillRow(platformTilemap, platformTile, 85, 97, 57);

        // NPC 2 position
        Debug.Log("[F] Survivor's Hub section built.");
    }

    // =========================================================
    // SECTION G — Boss Room: The Sentinel
    // Large arena, top right. Circular ceiling detail.
    //   x: 80-120   y: 62-85
    // =========================================================
    void BuildSectionG_BossRoom()
    {
        // Floor
        FillRow(groundTilemap, solidTile, 80, 120, 62);
        // Ceiling (flat)
        FillRow(groundTilemap, solidTile, 80, 120, 85);
        // Walls
        FillCol(groundTilemap, solidTile, 80,  62, 85);
        FillCol(groundTilemap, solidTile, 120, 62, 85);

        // Thick floor (boss arena feel — 2 rows)
        FillRow(groundTilemap, solidTile, 80, 120, 63);

        // Arena platforms — wide ledges on sides
        FillRow(platformTilemap, platformTile, 82,  96, 70);
        FillRow(platformTilemap, platformTile, 104, 118, 70);
        FillRow(platformTilemap, platformTile, 88,  112, 77);

        // Boss spawn center
        bossSpawnWorld = TileToWorld(100, 65);

        Debug.Log("[G] Boss Room built.");
    }

    // =========================================================
    // Passages between sections
    // =========================================================
    void BuildTransitionPassages()
    {
        // A → B: open right wall of A (no tile at x=22, y=1-8 — already open)

        // B ceiling → C floor: opening at x=10-26, y=9
        ClearRow(groundTilemap, 10, 26, 9);

        // B/D connection: floor continues (already at y=0)

        // C → E: passage at top of C (x=26-60, y=38 is open, player jumps right)

        // D ceiling → E base: opening at x=61-77, y=18
        ClearRow(groundTilemap, 61, 77, 18);

        // E top → F: opening at x=61-77, y=55
        FillRow(groundTilemap, solidTile, 61, 79, 55); // platform connecting E top to F floor
        ClearCol(groundTilemap, 78, 50, 55);           // keep shaft open

        // F → G: opening at x=100-119, y=62
        ClearRow(groundTilemap, 100, 119, 62);
    }

    // =========================================================
    // Spawn scene objects
    // =========================================================
    void SpawnObjects()
    {
        // [A] Player spawn marker
        SpawnPrefab(playerSpawnMarker,       TileToWorld(3, 1),   "PlayerSpawn");

        // [B] NPC 1 — The Archivist
        SpawnPrefab(npc1Prefab,              TileToWorld(36, 1),  "NPC");

        // [C] Evolution Pedestal
        SpawnPrefab(evolutionPedestalPrefab, TileToWorld(15, 35), "Pedestal");

        // [F] NPC 2 — Survivor
        SpawnPrefab(npc2Prefab,              TileToWorld(76, 51), "NPC");

        // [G] Boss spawn
        SpawnPrefab(bossPrefab,              bossSpawnWorld,      "Boss");
    }

    private void SpawnPrefab(GameObject prefab, Vector3 worldPos, string tag)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
        go.name = $"[Generated] {prefab.name}";
        spawnedObjects.Add(go);
    }

    // =========================================================
    // Background fill (purely decorative)
    // =========================================================
    void BuildBackground()
    {
        if (backgroundTilemap == null || bgTile == null) return;
        // Fill a large area behind the level
        FillRect(backgroundTilemap, bgTile, -2, -2, 125, 90);
    }

    // =========================================================
    // Tilemap helpers
    // =========================================================

    /// <summary>Fills a horizontal row of tiles from xStart to xEnd (exclusive) at y.</summary>
    void FillRow(Tilemap tilemap, TileBase tile, int xStart, int xEnd, int y)
    {
        if (tilemap == null || tile == null) return;
        for (int x = xStart; x < xEnd; x++)
            tilemap.SetTile(new Vector3Int(x, y, 0), tile);
    }

    /// <summary>Fills a vertical column of tiles from yStart to yEnd (exclusive) at x.</summary>
    void FillCol(Tilemap tilemap, TileBase tile, int x, int yStart, int yEnd)
    {
        if (tilemap == null || tile == null) return;
        for (int y = yStart; y < yEnd; y++)
            tilemap.SetTile(new Vector3Int(x, y, 0), tile);
    }

    /// <summary>Fills a rectangle of tiles.</summary>
    void FillRect(Tilemap tilemap, TileBase tile, int xStart, int yStart, int xEnd, int yEnd)
    {
        if (tilemap == null || tile == null) return;
        for (int x = xStart; x < xEnd; x++)
            for (int y = yStart; y < yEnd; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
    }

    /// <summary>Removes a horizontal row of tiles (creates a passage).</summary>
    void ClearRow(Tilemap tilemap, int xStart, int xEnd, int y)
    {
        if (tilemap == null) return;
        for (int x = xStart; x < xEnd; x++)
            tilemap.SetTile(new Vector3Int(x, y, 0), null);
    }

    /// <summary>Removes a vertical column of tiles (creates a passage).</summary>
    void ClearCol(Tilemap tilemap, int x, int yStart, int yEnd)
    {
        if (tilemap == null) return;
        for (int y = yStart; y < yEnd; y++)
            tilemap.SetTile(new Vector3Int(x, y, 0), null);
    }

    /// <summary>Converts tile grid position to world position (center of tile).</summary>
    public Vector3 TileToWorld(int x, int y)
    {
        if (groundTilemap != null)
            return groundTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
        return new Vector3(x, y, 0);
    }
}
