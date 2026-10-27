using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Terrain and grass (2.4.0): grass around the player on a known detail map, billboard grass, the terrain settings, fixes.
    public static partial class AuditTestViewpoints
    {
        const string TerrainDataPath = AuditTestAssets.Root + "/Terrain/AuditTerrain.asset";

        public static void RunTerrain()
        {
            Run("terrain", TerrainChecks);
        }

        static Terrain MakeTerrain(float distance, float density, bool instanced, float pixelError, bool billboard)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TerrainDataPath));
            AssetDatabase.DeleteAsset(TerrainDataPath);
            var data = new TerrainData { heightmapResolution = 33 };
            data.size = new Vector3(100f, 10f, 100f);
            data.SetDetailResolution(128, 16);
            var grass = AuditTestAssets.Plain.mainTexture as Texture2D;
            data.detailPrototypes = new[]
            {
                new DetailPrototype { prototypeTexture = grass, renderMode = billboard ? DetailRenderMode.GrassBillboard : DetailRenderMode.Grass },
                new DetailPrototype { prototypeTexture = grass, renderMode = DetailRenderMode.Grass }
            };
            var map = new int[128, 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++) map[y, x] = 8;
            data.SetDetailLayer(0, 0, 0, map);
            data.SetDetailLayer(0, 0, 1, map);
            AssetDatabase.CreateAsset(data, TerrainDataPath);
            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Ground";
            var terrain = go.GetComponent<Terrain>();
            terrain.detailObjectDistance = distance;
            terrain.detailObjectDensity = density;
            terrain.drawInstanced = instanced;
            terrain.heightmapPixelError = pixelError;
            return terrain;
        }

        static void TerrainChecks()
        {
            NewScene("Terrain");
            var terrain = MakeTerrain(80f, 1f, false, 3f, true);
            var r = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var rows = r.issues.Where(i => i.category == Category.Terrain).ToList();
            foreach (var row in rows) log.AppendLine("info: " + row.checkId + " " + row.severity + " " + row.Message);
            var many = rows.FirstOrDefault(i => i.checkId == "TERRAIN_DETAIL_INSTANCES");
            Check(many != null && many.severity == Severity.Warning && (long)many.args[0] == 262144 && (long)many.args[1] == 524288,
                "two layers of 8 a cell on 128 × 128, density 1, 80 m over a 100 m terrain: 262 144 instances, 524 288 triangles, Warning");
            var distance = rows.FirstOrDefault(i => i.checkId == "TERRAIN_DETAIL_DISTANCE");
            var billboard = rows.FirstOrDefault(i => i.checkId == "TERRAIN_GRASS_BILLBOARD");
            var instanced = rows.FirstOrDefault(i => i.checkId == "TERRAIN_DRAW_INSTANCED");
            var pixel = rows.FirstOrDefault(i => i.checkId == "TERRAIN_PIXEL_ERROR");
            Check(distance != null && distance.severity == Severity.Info && distance.AutoFixable && !distance.fixTouchesAsset, "Detail Distance 80 m: Info with a scene fix");
            Check(billboard != null && billboard.severity == Severity.Warning && billboard.AutoFixable && billboard.fixTouchesAsset &&
                  rows.Count(i => i.checkId == "TERRAIN_GRASS_BILLBOARD") == 1, "one Grass Billboard layer: Warning with a fix in the terrain data");
            Check(instanced != null && instanced.AutoFixable && pixel != null && pixel.severity == Severity.Info, "Draw Instanced off and Pixel Error 3: Info");
            Check(rows.All(i => i.objectPath == "Ground" && i.category == Category.Terrain) && Strings.Has("cat.Terrain"), "rows on the terrain, in the category Terrain");

            AutoFixer.Apply(new[] { distance, instanced });
            Check(Mathf.Approximately(terrain.detailObjectDistance, 60f) && terrain.drawInstanced, "the scene fixes: 60 m and Draw Instanced");
            Undo.PerformUndo();
            Check(Mathf.Approximately(terrain.detailObjectDistance, 80f) && !terrain.drawInstanced, "Undo brings them back");
            var before = File.ReadAllBytes(TerrainDataPath);
            AutoFixer.Apply(new[] { billboard });
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            Check(data.detailPrototypes[0].renderMode == DetailRenderMode.Grass, "the billboard layer is Grass in the terrain data");
            // Terrain data is written binary even with Force Text: the file must differ from the one before the fix.
            Check(!File.ReadAllBytes(TerrainDataPath).SequenceEqual(before), "the terrain data is saved to disk after the fix");

            var full = new int[128, 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++) full[y, x] = 16;
            data.SetDetailLayer(0, 0, 0, full);
            data.SetDetailLayer(0, 0, 1, full);
            var dense = SceneAuditor.RunFullAudit(new VRBudgets(), false).issues.FirstOrDefault(i => i.checkId == "TERRAIN_DETAIL_INSTANCES");
            Check(dense != null && dense.severity == Severity.Critical && (long)dense.args[0] == 524288, "16 a cell: 524 288 instances, Critical");
            terrain.drawTreesAndFoliage = false;
            var off = SceneAuditor.RunFullAudit(new VRBudgets(), false).issues.Where(i => i.category == Category.Terrain).Select(i => i.checkId).ToList();
            Check(!off.Any(id => id.StartsWith("TERRAIN_DETAIL") || id == "TERRAIN_GRASS_BILLBOARD"), "Draw off: no grass rows (" + string.Join(", ", off.ToArray()) + ")");

            NewScene("TerrainQuiet");
            MakeTerrain(50f, 0.1f, true, 5f, false);
            var quiet = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            Check(!quiet.issues.Any(i => i.category == Category.Terrain), "density 0.1, 50 m, instanced, Pixel Error 5, Grass: nothing on the terrain");
            AssetDatabase.DeleteAsset(TerrainDataPath);
        }
    }
}
