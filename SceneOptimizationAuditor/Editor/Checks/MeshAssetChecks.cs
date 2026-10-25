using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        // Shaders whose vertex inputs are known: position, normal, UV0, UV1 (lightmap / detail), tangent only with a normal map.
        static bool IsStandardFamily(Shader shader)
        {
            if (shader == null) return false;
            var name = shader.name;
            return name == "Standard" || name == "Standard (Specular setup)" ||
                   name.StartsWith("Legacy Shaders/", StringComparison.Ordinal) || name.StartsWith("Mobile/", StringComparison.Ordinal);
        }

        static void AuditMeshAssets(AuditContext ctx)
        {
            var b = ctx.budgets;
            var renderersByMesh = new Dictionary<Mesh, List<Renderer>>();
            foreach (var renderer in ctx.All<Renderer>())
            {
                var mesh = AuditContext.MeshOf(renderer);
                if (mesh == null) continue;
                List<Renderer> list;
                if (!renderersByMesh.TryGetValue(mesh, out list))
                {
                    list = new List<Renderer>();
                    renderersByMesh[mesh] = list;
                }
                list.Add(renderer);
            }

            var hashes = new Dictionary<string, List<Mesh>>();
            foreach (var pair in renderersByMesh)
            {
                var mesh = pair.Key;
                var renderers = pair.Value;
                if (!EditorUtility.IsPersistent(mesh) || !AuditContext.IsProjectAssetPath(AssetDatabase.GetAssetPath(mesh))) continue;
                int users = renderers.Count;
                long tris = AuditContext.Tris(mesh);

                var unused = UnusedChannels(mesh, renderers);
                if (unused.Count > 0)
                    ctx.Report("MESH_UNUSED_CHANNELS", Category.Meshes, Severity.Info, mesh, mesh.name, string.Join(", ", unused.ToArray())).users = users;

                if (tris >= VRBudgets.SplitMeshMinTris && mesh.vertexCount > tris * VRBudgets.SplitVerticesPerTriangle)
                    ctx.Report("MESH_SPLIT_VERTICES", Category.Meshes, Severity.Info, mesh, mesh.name, mesh.vertexCount, tris).users = users;

                if (mesh.indexFormat == IndexFormat.UInt32 && mesh.vertexCount < 65535)
                {
                    var issue = ctx.Report("MESH_INDEX_32", Category.Meshes, Severity.Info, mesh, mesh.name, mesh.vertexCount);
                    issue.users = users;
                    var model = ModelOf(mesh);
                    if (model != null)
                        issue.SetFix(FixKind.Review, model, true, () =>
                        {
                            model.indexFormat = ModelImporterIndexFormat.Auto;
                            model.SaveAndReimport();
                        });
                }

                int degenerate = DegenerateTriangles(mesh);
                if (degenerate > 0 && degenerate >= tris * VRBudgets.DegenerateShareInfo)
                    ctx.Report("MESH_DEGENERATE", Category.Meshes, Severity.Info, mesh, mesh.name, degenerate).users = users;

                var bounds = mesh.bounds;
                float far = Mathf.Max(VRBudgets.PivotFarMinMeters, bounds.extents.magnitude * VRBudgets.PivotFarExtents);
                if (bounds.center.magnitude > far)
                    ctx.Report("MESH_PIVOT_FAR", Category.Meshes, Severity.Info, mesh, mesh.name, Math.Round(bounds.center.magnitude, 1)).users = users;

                var hash = MeshHash(mesh);
                List<Mesh> same;
                if (!hashes.TryGetValue(hash, out same))
                {
                    same = new List<Mesh>();
                    hashes[hash] = same;
                }
                same.Add(mesh);
            }

            foreach (var same in hashes.Values)
            {
                if (same.Count < 2) continue;
                var names = same.Select(m => AssetDatabase.GetAssetPath(m) + ":" + m.name).Distinct().ToList();
                if (names.Count < 2) continue;
                ctx.Report("MESH_DUPLICATE", Category.Meshes, Severity.Info, same[0], same.Count, string.Join(", ", same.Select(m => m.name).ToArray()))
                    .Gain(same.Skip(1).Sum(m => FixGain.MeshMB(m)), 0, 0, false)
                    .users = same.Sum(m => renderersByMesh[m].Count);
            }

            AuditModels(ctx, renderersByMesh);
            AuditMeshObjects(ctx);
            AuditLodGroups(ctx);
        }

        static ModelImporter ModelOf(Mesh mesh)
        {
            var path = AssetDatabase.GetAssetPath(mesh);
            return AuditContext.IsProjectAssetPath(path) ? AssetImporter.GetAtPath(path) as ModelImporter : null;
        }

        static List<string> UnusedChannels(Mesh mesh, List<Renderer> renderers)
        {
            var result = new List<string>();
            var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToList();
            if (materials.Count == 0 || !materials.All(m => IsStandardFamily(m.shader))) return result;
            if (renderers.Any(r => r is ParticleSystemRenderer)) return result;

            if (mesh.HasVertexAttribute(VertexAttribute.Color)) result.Add("Color");
            bool normalMaps = materials.Any(m => m.HasProperty("_BumpMap") && m.GetTexture("_BumpMap") != null);
            if (mesh.HasVertexAttribute(VertexAttribute.Tangent) && !normalMaps) result.Add("Tangent");
            for (int i = 2; i <= 7; i++)
                if (mesh.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0 + i))) result.Add("UV" + i);
            return result;
        }

        static int DegenerateTriangles(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var indices = new List<int>();
            int count = 0;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                if (mesh.GetTopology(sub) != MeshTopology.Triangles) continue;
                indices.Clear();
                mesh.GetIndices(indices, sub);
                for (int i = 0; i + 2 < indices.Count; i += 3)
                {
                    var a = vertices[indices[i]];
                    var c = Vector3.Cross(vertices[indices[i + 1]] - a, vertices[indices[i + 2]] - a);
                    if (c.sqrMagnitude < 1e-14f) count++;
                }
            }
            return count;
        }

        // Layout, positions, UV0 and indices per submesh: equal data gives equal hashes whatever the asset is called.
        static string MeshHash(Mesh mesh)
        {
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(mesh.subMeshCount));
            bytes.AddRange(BitConverter.GetBytes(mesh.bindposes.Length));
            for (int a = 0; a <= (int)VertexAttribute.BlendIndices; a++)
                bytes.Add(mesh.HasVertexAttribute((VertexAttribute)a) ? (byte)1 : (byte)0);
            foreach (var v in mesh.vertices)
            {
                bytes.AddRange(BitConverter.GetBytes(v.x));
                bytes.AddRange(BitConverter.GetBytes(v.y));
                bytes.AddRange(BitConverter.GetBytes(v.z));
            }
            foreach (var uv in mesh.uv)
            {
                bytes.AddRange(BitConverter.GetBytes(uv.x));
                bytes.AddRange(BitConverter.GetBytes(uv.y));
            }
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var indices = mesh.GetIndices(sub);
                bytes.AddRange(BitConverter.GetBytes(indices.Length));
                foreach (var index in indices) bytes.AddRange(BitConverter.GetBytes(index));
            }
            using (var md5 = MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(bytes.ToArray()));
        }

        // Import settings of every model file used in the scenes, one issue per file.
        static void AuditModels(AuditContext ctx, Dictionary<Mesh, List<Renderer>> renderersByMesh)
        {
            var byModel = new Dictionary<string, List<Renderer>>();
            foreach (var pair in renderersByMesh)
            {
                var path = AssetDatabase.GetAssetPath(pair.Key);
                if (!AuditContext.IsProjectAssetPath(path) || !(AssetImporter.GetAtPath(path) is ModelImporter)) continue;
                List<Renderer> list;
                if (!byModel.TryGetValue(path, out list))
                {
                    list = new List<Renderer>();
                    byModel[path] = list;
                }
                list.AddRange(pair.Value);
            }

            foreach (var pair in byModel)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(pair.Key);
                var asset = AssetDatabase.LoadMainAssetAtPath(pair.Key);
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(pair.Key);
                int users = pair.Value.Count;
                long modelTris = subAssets.OfType<Mesh>().Sum(m => AuditContext.Tris(m));

                if (importer.meshCompression == ModelImporterMeshCompression.Off && modelTris > ctx.budgets.objectTrisWarning)
                {
                    var model = importer;
                    var issue = ctx.Report("MESH_COMPRESSION_OFF", Category.Meshes, Severity.Info, asset, modelTris);
                    issue.users = users;
                    issue.SetFix(FixKind.Review, model, true, () =>
                    {
                        model.meshCompression = ModelImporterMeshCompression.Low;
                        model.SaveAndReimport();
                    });
                }

                if (Math.Abs(importer.globalScale - 1f) > 0.0001f)
                    ctx.Report("MESH_IMPORT_SCALE", Category.Meshes, Severity.Info, asset, importer.globalScale).users = users;

                bool skinned = subAssets.OfType<Mesh>().Any(m => m.bindposes.Length > 0) || pair.Value.Any(r => r is SkinnedMeshRenderer);
                bool clips = subAssets.OfType<AnimationClip>().Any(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                if (!skinned && !clips && (importer.animationType != ModelImporterAnimationType.None || importer.importAnimation))
                {
                    var model = importer;
                    var issue = ctx.Report("MESH_IMPORT_ANIMATION", Category.Meshes, Severity.Info, asset, importer.animationType.ToString());
                    issue.users = users;
                    issue.SetFix(FixKind.Review, model, true, () =>
                    {
                        model.animationType = ModelImporterAnimationType.None;
                        model.importAnimation = false;
                        model.SaveAndReimport();
                    });
                }

                var root = asset as GameObject;
                bool cameras = root != null && root.GetComponentsInChildren<Camera>(true).Length > 0;
                bool lights = root != null && root.GetComponentsInChildren<Light>(true).Length > 0;
                if (cameras || lights)
                {
                    var model = importer;
                    var what = cameras && lights ? "Camera, Light" : cameras ? "Camera" : "Light";
                    var issue = ctx.Report("MESH_IMPORT_CAMERAS_LIGHTS", Category.Meshes, Severity.Info, asset, what);
                    issue.users = users;
                    issue.SetFix(FixKind.Review, model, true, () =>
                    {
                        model.importCameras = false;
                        model.importLights = false;
                        model.SaveAndReimport();
                    });
                }

                if (importer.materialImportMode != ModelImporterMaterialImportMode.None &&
                    importer.materialLocation == ModelImporterMaterialLocation.InPrefab &&
                    subAssets.OfType<Material>().Any())
                    ctx.Report("MESH_EMBEDDED_MATERIALS", Category.Meshes, Severity.Info, asset, subAssets.OfType<Material>().Count()).users = users;
            }
        }

        static void AuditMeshObjects(AuditContext ctx)
        {
            foreach (var renderer in ctx.All<Renderer>())
            {
                if (!AuditContext.IsActive(renderer)) continue;
                var mesh = AuditContext.MeshOf(renderer);
                if (mesh == null) continue;

                var scale = renderer.transform.lossyScale;
                float max = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                float min = Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                if (max > VRBudgets.ExtremeScaleMax || (min > 0f && min < VRBudgets.ExtremeScaleMin))
                    ctx.Report("MESH_EXTREME_SCALE", Category.Meshes, Severity.Warning, renderer,
                        Math.Round(scale.x, 3) + " × " + Math.Round(scale.y, 3) + " × " + Math.Round(scale.z, 3));

                var size = renderer.bounds.size;
                float largest = Mathf.Max(size.x, size.y, size.z);
                long tris = AuditContext.Tris(mesh);
                if (largest > 0f && largest < VRBudgets.DenseSmallMaxMeters && tris > VRBudgets.DenseSmallMinTris)
                    ctx.Report("MESH_DENSE_SMALL", Category.Meshes, Severity.Info, renderer, tris, Math.Round(largest * 100f));
            }
        }

        static void AuditLodGroups(AuditContext ctx)
        {
            foreach (var group in ctx.All<LODGroup>())
            {
                if (!AuditContext.IsActive(group)) continue;
                var lods = group.GetLODs();
                if (lods.Length == 0) continue;

                if (lods.Length > 1)
                {
                    long lod0 = LodTris(lods[0]);
                    long lod1 = LodTris(lods[1]);
                    if (lod0 > 0 && lod1 > lod0 * VRBudgets.WeakLodShare)
                        ctx.Report("MESH_LOD_WEAK", Category.Meshes, Severity.Warning, group, Math.Round(100.0 * lod1 / lod0));

                    var first = LodMaterials(lods[0]);
                    for (int i = 1; i < lods.Length; i++)
                    {
                        var other = LodMaterials(lods[i]);
                        if (other.Count > 0 && first.Count > 0 && !other.SetEquals(first))
                        {
                            ctx.Report("MESH_LOD_MATERIALS", Category.Meshes, Severity.Info, group, i);
                            break;
                        }
                    }
                }
                if (lods[lods.Length - 1].screenRelativeTransitionHeight <= 0.0001f)
                    ctx.Report("MESH_LOD_NO_CULL", Category.Meshes, Severity.Info, group);
                if (lods[0].screenRelativeTransitionHeight < VRBudgets.LateLodTransition)
                    ctx.Report("MESH_LOD_LATE", Category.Meshes, Severity.Info, group, Math.Round(lods[0].screenRelativeTransitionHeight * 100f, 1));
            }
        }

        static long LodTris(LOD lod)
        {
            long tris = 0;
            foreach (var r in lod.renderers) if (r != null) tris += AuditContext.Tris(AuditContext.MeshOf(r));
            return tris;
        }

        static HashSet<Material> LodMaterials(LOD lod)
        {
            var set = new HashSet<Material>();
            foreach (var r in lod.renderers)
                if (r != null)
                    foreach (var m in r.sharedMaterials) if (m != null) set.Add(m);
            return set;
        }
    }
}
