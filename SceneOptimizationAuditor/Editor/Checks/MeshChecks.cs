using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        static void AuditMeshes(AuditContext ctx)
        {
            var b = ctx.budgets;

            long total = 0;
            bool anySkinned = false;
            foreach (var renderer in ctx.All<Renderer>())
            {
                if (!AuditContext.IsActive(renderer)) continue;
                var mesh = AuditContext.MeshOf(renderer);
                if (mesh == null) continue;
                if (renderer is SkinnedMeshRenderer) anySkinned = true;
                long tris = AuditContext.Tris(mesh);
                if (!ctx.lowerLodRenderers.Contains(renderer)) total += tris;

                if (ctx.lodRenderers.Contains(renderer)) continue;
                if (tris > b.objectTrisCritical)
                    ctx.Report("MESH_NO_LOD", Category.Meshes, Severity.Critical, renderer, tris, b.objectTrisCritical);
                else if (tris > b.objectTrisWarning)
                    ctx.Report("MESH_NO_LOD", Category.Meshes, Severity.Warning, renderer, tris, b.objectTrisWarning);
            }
            ctx.sceneTris = total;
            Budget(ctx, "MESH_SCENE_TRIS", Category.Meshes, total, b.sceneTrisWarning, b.sceneTrisCritical);

            AuditModelImporters(ctx);
            AuditMeshAssets(ctx);

            var checkedSkins = new HashSet<Mesh>();
            foreach (var skinned in ctx.All<SkinnedMeshRenderer>())
            {
                var mesh = skinned.sharedMesh;
                if (mesh == null || !checkedSkins.Add(mesh)) continue;
                var bones = mesh.GetBonesPerVertex();
                int max = 0;
                for (int i = 0; i < bones.Length; i++) if (bones[i] > max) max = bones[i];
                if (max > VRBudgets.MaxBonesPerVertex)
                    ctx.Report("MESH_BONES_PER_VERTEX", Category.Meshes, Severity.Warning, mesh, max).users = ctx.meshes.ContainsKey(mesh) ? ctx.meshes[mesh] : 0;
            }

            if (anySkinned)
            {
                if ((int)ctx.quality.skinWeights > (int)SkinWeights.FourBones)
                    ctx.ReportSetting("MESH_QUALITY_SKIN_WEIGHTS", Category.Meshes, Severity.Info, "Project/Quality", ctx.quality.skinWeights.ToString());
                if (!PlayerSettings.gpuSkinning)
                    ctx.ReportSetting("MESH_GPU_SKINNING_OFF", Category.Meshes, Severity.Warning, "Project/Player");
            }
        }

        // One issue per model file, even when the model holds several meshes.
        static void AuditModelImporters(AuditContext ctx)
        {
            var users = new Dictionary<string, int>();
            var neededReadable = new Dictionary<string, bool>();
            foreach (var pair in ctx.meshes)
            {
                var path = AssetDatabase.GetAssetPath(pair.Key);
                if (!AuditContext.IsProjectAssetPath(path)) continue;
                int count;
                users.TryGetValue(path, out count);
                users[path] = count + pair.Value;
                bool needed;
                neededReadable.TryGetValue(path, out needed);
                neededReadable[path] = needed || ctx.colliderMeshes.Contains(pair.Key) || ctx.particleMeshes.Contains(pair.Key);
            }

            foreach (var pair in users)
            {
                var importer = AssetImporter.GetAtPath(pair.Key) as ModelImporter;
                if (importer == null) continue;
                var asset = AssetDatabase.LoadMainAssetAtPath(pair.Key);

                if (importer.isReadable)
                {
                    bool needed = neededReadable[pair.Key];
                    var issue = ctx.Report(needed ? "MESH_READ_WRITE_NEEDED" : "MESH_READ_WRITE", Category.Meshes,
                        needed ? Severity.Info : Severity.Warning, asset);
                    issue.users = pair.Value;
                    if (!needed)
                    {
                        var model = importer;
                        issue.SetFix(FixKind.Review, model, true, () =>
                        {
                            model.isReadable = false;
                            model.SaveAndReimport();
                        });
                    }
                }

                if (importer.meshOptimizationFlags != MeshOptimizationFlags.Everything)
                    ctx.Report("MESH_NOT_OPTIMIZED", Category.Meshes, Severity.Info, asset, importer.meshOptimizationFlags.ToString()).users = pair.Value;
            }
        }
    }
}
