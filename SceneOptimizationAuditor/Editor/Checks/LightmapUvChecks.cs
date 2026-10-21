using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        // UV1 of meshes baked into lightmaps: present, inside 0..1 and without overlaps. One issue per mesh.
        static void AuditLightmapUv(AuditContext ctx)
        {
            var users = new Dictionary<Mesh, List<MeshRenderer>>();
            foreach (var renderer in ctx.All<MeshRenderer>())
            {
                if (!AuditContext.IsActive(renderer)) continue;
                var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                if ((flags & StaticEditorFlags.ContributeGI) == 0 || renderer.receiveGI != ReceiveGI.Lightmaps) continue;
                var mesh = AuditContext.MeshOf(renderer);
                if (mesh == null) continue;
                List<MeshRenderer> list;
                if (!users.TryGetValue(mesh, out list))
                {
                    list = new List<MeshRenderer>();
                    users[mesh] = list;
                }
                list.Add(renderer);
            }

            foreach (var pair in users)
            {
                var mesh = pair.Key;
                // Scene-only meshes have no asset: point at the first object that uses them.
                UnityEngine.Object target = EditorUtility.IsPersistent(mesh) ? (UnityEngine.Object)mesh : pair.Value[0];
                int count = pair.Value.Count;

                if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord1))
                {
                    var issue = ctx.Report("LIGHT_NO_UV1", Category.Lighting, Severity.Warning, target, mesh.name);
                    issue.users = count;
                    var path = AssetDatabase.GetAssetPath(mesh);
                    var model = AuditContext.IsProjectAssetPath(path) ? AssetImporter.GetAtPath(path) as ModelImporter : null;
                    if (model != null && !model.generateSecondaryUV)
                    {
                        var importer = model;
                        issue.SetFix(FixKind.Review, importer, true, () =>
                        {
                            importer.generateSecondaryUV = true;
                            importer.SaveAndReimport();
                        });
                    }
                    continue;
                }

                var uv = new List<Vector2>();
                mesh.GetUVs(1, uv);
                float outside = OutsideShare(uv);
                if (outside > 0f)
                    ctx.Report("LIGHT_UV1_OUT_OF_RANGE", Category.Lighting, Severity.Warning, target, mesh.name, Percent(outside)).users = count;

                float overlap = OverlapShare(mesh, uv);
                if (overlap > VRBudgets.LightmapUvOverlapMinShare)
                    ctx.Report("LIGHT_UV1_OVERLAP", Category.Lighting, Severity.Warning, target, mesh.name, Percent(overlap)).users = count;
            }
        }

        static object Percent(float share)
        {
            return Math.Round(share * 100.0, share < 0.1f ? 1 : 0);
        }

        // Share of UV1 vertices outside 0..1 (with a small tolerance).
        static float OutsideShare(List<Vector2> uv)
        {
            if (uv.Count == 0) return 0f;
            const float t = VRBudgets.LightmapUvTolerance;
            int outside = 0;
            foreach (var p in uv)
                if (p.x < -t || p.y < -t || p.x > 1f + t || p.y > 1f + t) outside++;
            return (float)outside / uv.Count;
        }

        // Share of covered lightmap texels that more than one UV1 triangle covers. Texel centres exactly on an
        // edge count for no triangle, so neighbours that only share an edge do not overlap.
        static float OverlapShare(Mesh mesh, List<Vector2> uv)
        {
            const int grid = VRBudgets.LightmapUvOverlapGrid;
            var coverage = new byte[grid * grid];
            var indices = new List<int>();
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                if (mesh.GetTopology(sub) != MeshTopology.Triangles) continue;
                indices.Clear();
                mesh.GetIndices(indices, sub);
                for (int i = 0; i + 2 < indices.Count; i += 3)
                    Rasterize(uv[indices[i]] * grid, uv[indices[i + 1]] * grid, uv[indices[i + 2]] * grid, coverage, grid);
            }
            int covered = 0, overlapped = 0;
            foreach (var c in coverage)
            {
                if (c == 0) continue;
                covered++;
                if (c > 1) overlapped++;
            }
            return covered == 0 ? 0f : (float)overlapped / covered;
        }

        static void Rasterize(Vector2 a, Vector2 b, Vector2 c, byte[] coverage, int grid)
        {
            float area = Cross(b - a, c - a);
            if (Mathf.Abs(area) < 1e-8f) return;
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int maxX = Mathf.Min(grid - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int maxY = Mathf.Min(grid - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
            float sign = Mathf.Sign(area);
            const float eps = 1e-4f;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    if (Cross(b - a, p - a) * sign <= eps || Cross(c - b, p - b) * sign <= eps || Cross(a - c, p - c) * sign <= eps) continue;
                    int k = y * grid + x;
                    if (coverage[k] < 255) coverage[k]++;
                }
            }
        }

        static float Cross(Vector2 u, Vector2 v)
        {
            return u.x * v.y - u.y * v.x;
        }
    }
}
