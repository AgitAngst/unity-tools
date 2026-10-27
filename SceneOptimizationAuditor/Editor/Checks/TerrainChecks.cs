using System;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        // Terrain and its grass: what is drawn around the player, billboard grass, the settings of the terrain.
        static void AuditTerrain(AuditContext ctx)
        {
            foreach (var terrain in ctx.All<Terrain>())
            {
                if (!AuditContext.IsActive(terrain) || terrain.terrainData == null) continue;
                var data = terrain.terrainData;
                var target = terrain;
                var prototypes = data.detailPrototypes;

                // Details are drawn only with "Draw" on and some density: otherwise the grass costs nothing.
                if (prototypes.Length > 0 && data.detailResolution > 0 && terrain.drawTreesAndFoliage && terrain.detailObjectDensity > 0f)
                {
                    long tris;
                    long instances = DetailsAround(terrain, prototypes, out tris);
                    if (instances > VRBudgets.TerrainDetailsCritical)
                        ctx.Report("TERRAIN_DETAIL_INSTANCES", Category.Terrain, Severity.Critical, terrain, instances, tris, Math.Round(terrain.detailObjectDistance));
                    else if (instances > VRBudgets.TerrainDetailsWarning)
                        ctx.Report("TERRAIN_DETAIL_INSTANCES", Category.Terrain, Severity.Warning, terrain, instances, tris, Math.Round(terrain.detailObjectDistance));

                    if (terrain.detailObjectDistance > VRBudgets.TerrainDetailDistanceInfo)
                        ctx.Report("TERRAIN_DETAIL_DISTANCE", Category.Terrain, Severity.Info, terrain, Math.Round(terrain.detailObjectDistance), VRBudgets.TerrainDetailDistanceInfo)
                            .SetFix(FixKind.Review, target, false, () => target.detailObjectDistance = VRBudgets.TerrainDetailDistanceInfo);

                    // Billboards turn with the head: each eye sees them turned its own way. The fix changes the terrain data
                    // asset; terrain data kept in the scene only is left to the user (Undo would not cover it).
                    bool asset = EditorUtility.IsPersistent(data);
                    for (int i = 0; i < prototypes.Length; i++)
                    {
                        if (prototypes[i].renderMode != DetailRenderMode.GrassBillboard) continue;
                        int layer = i;
                        var issue = ctx.Report("TERRAIN_GRASS_BILLBOARD", Category.Terrain, Severity.Warning, terrain, DetailName(prototypes[i], i));
                        if (asset)
                            issue.SetFix(FixKind.Review, data, true, () =>
                            {
                                var all = data.detailPrototypes;
                                all[layer].renderMode = DetailRenderMode.Grass;
                                data.detailPrototypes = all;
                            });
                    }
                }

                if (!terrain.drawInstanced)
                    ctx.Report("TERRAIN_DRAW_INSTANCED", Category.Terrain, Severity.Info, terrain)
                        .SetFix(FixKind.Review, target, false, () => target.drawInstanced = true);
                if (terrain.heightmapPixelError < VRBudgets.TerrainPixelErrorInfo)
                    ctx.Report("TERRAIN_PIXEL_ERROR", Category.Terrain, Severity.Info, terrain, Math.Round(terrain.heightmapPixelError, 1), VRBudgets.TerrainPixelErrorInfo);
            }
        }

        // Grass and detail instances inside the circle of Detail Distance, at the average density of the terrain, and their
        // triangles: a grass quad is 2, a detail mesh its own. The map is read in strips: a whole 4096² layer is 64 MB.
        static long DetailsAround(Terrain terrain, DetailPrototype[] prototypes, out long tris)
        {
            var data = terrain.terrainData;
            int resolution = data.detailResolution;
            double area = Math.Max(1.0, (double)data.size.x * data.size.z);
            double distance = terrain.detailObjectDistance;
            double share = Math.Min(1.0, Math.PI * distance * distance / area);
            double instances = 0;
            double triangles = 0;
            for (int layer = 0; layer < prototypes.Length; layer++)
            {
                long sum = 0;
                for (int y = 0; y < resolution; y += DetailStrip)
                {
                    var map = data.GetDetailLayer(0, y, resolution, Math.Min(DetailStrip, resolution - y), layer);
                    foreach (var cell in map) sum += cell;
                }
                double around = sum * terrain.detailObjectDensity * share;
                instances += around;
                triangles += around * DetailTris(prototypes[layer]);
            }
            tris = (long)Math.Round(triangles);
            return (long)Math.Round(instances);
        }

        const int DetailStrip = 256;

        static long DetailTris(DetailPrototype prototype)
        {
            if (!prototype.usePrototypeMesh || prototype.prototype == null) return 2;
            var filter = prototype.prototype.GetComponentInChildren<MeshFilter>();
            return filter != null && filter.sharedMesh != null ? AuditContext.Tris(filter.sharedMesh) : 2;
        }

        static string DetailName(DetailPrototype prototype, int index)
        {
            if (prototype.usePrototypeMesh && prototype.prototype != null) return prototype.prototype.name;
            if (prototype.prototypeTexture != null) return prototype.prototypeTexture.name;
            return "#" + index;
        }
    }
}
