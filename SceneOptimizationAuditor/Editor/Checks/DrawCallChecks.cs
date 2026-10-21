using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        static void AuditDrawCalls(AuditContext ctx)
        {
            var b = ctx.budgets;

            // Upper estimate: every material slot is one call, plus shadow passes, no batching or culling.
            long baseCalls = 0;
            long casterSlots = 0;
            int casters = 0;
            foreach (var renderer in ctx.All<Renderer>())
            {
                if (!AuditContext.IsActive(renderer) || ctx.lowerLodRenderers.Contains(renderer)) continue;
                int slots = SlotCount(renderer);
                if (slots == 0) continue;
                baseCalls += slots;
                if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                {
                    casters++;
                    casterSlots += slots;
                }
            }
            int shadowPasses = ctx.ShadowPassesPerCaster();
            long estimate = baseCalls + casterSlots * shadowPasses;
            if (ctx.xr.DoublesDrawCalls) estimate *= 2;
            ctx.drawCallEstimate = estimate;
            Budget(ctx, "DC_BUDGET", Category.DrawCalls, estimate, b.drawCallsWarning, b.drawCallsCritical);
            if (casters > 0) ctx.Report("DC_SHADOW_CASTERS", Category.DrawCalls, Severity.Info, null, casters, shadowPasses);

            var instancingGroups = new Dictionary<Material, Dictionary<Mesh, int>>();
            foreach (var renderer in ctx.All<MeshRenderer>())
            {
                if (!AuditContext.IsActive(renderer)) continue;
                var mesh = AuditContext.MeshOf(renderer);
                if (mesh == null) continue;
                var go = renderer.gameObject;
                var materials = renderer.sharedMaterials;

                if (materials.Length > VRBudgets.MultiMaterialWarning)
                    ctx.Report("DC_MANY_MATERIALS", Category.DrawCalls, Severity.Warning, renderer, materials.Length);
                else if (materials.Length > 1)
                    ctx.Report("DC_MULTI_MATERIAL", Category.DrawCalls, Severity.Info, renderer, materials.Length);
                if (materials.Length > mesh.subMeshCount)
                    ctx.Report("DC_EXTRA_MATERIALS", Category.DrawCalls, Severity.Warning, renderer, materials.Length, mesh.subMeshCount);

                bool isStatic = (GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.BatchingStatic) != 0;
                if (!isStatic && !HasMovers(go.transform))
                {
                    var issue = ctx.Report("DC_STATIC_CANDIDATE", Category.DrawCalls, Severity.Info, go);
                    issue.SetFix(FixKind.Review, go, false, () =>
                        GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(go) | StaticEditorFlags.BatchingStatic));
                }

                // Static batching wins over instancing, so only non-static renderers can instance.
                if (isStatic) continue;
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    Dictionary<Mesh, int> perMesh;
                    if (!instancingGroups.TryGetValue(material, out perMesh))
                    {
                        perMesh = new Dictionary<Mesh, int>();
                        instancingGroups[material] = perMesh;
                    }
                    int count;
                    perMesh.TryGetValue(mesh, out count);
                    perMesh[mesh] = count + 1;
                }
            }

            foreach (var group in instancingGroups)
            {
                var material = group.Key;
                if (material.enableInstancing) continue;
                int instances = 0;
                foreach (var count in group.Value.Values) instances = Mathf.Max(instances, count);
                if (instances <= VRBudgets.InstancingMinInstances) continue;

                var supported = ShaderSupportsInstancing(material.shader);
                if (supported == false) continue;
                if (supported == null)
                {
                    ctx.Report("DC_INSTANCING_UNKNOWN", Category.DrawCalls, Severity.Info, material, instances).users = instances;
                    continue;
                }
                var issue = ctx.Report("DC_INSTANCING_OFF", Category.DrawCalls, Severity.Warning, material, instances);
                issue.users = instances;
                if (AuditContext.IsEditableAsset(material))
                    issue.SetFix(FixKind.Safe, material, true, () => material.enableInstancing = true);
            }
        }

        static int SlotCount(Renderer renderer)
        {
            if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            {
                if (AuditContext.MeshOf(renderer) == null) return 0;
                int slots = 0;
                foreach (var material in renderer.sharedMaterials) if (material != null) slots++;
                return slots;
            }
            foreach (var material in renderer.sharedMaterials) if (material != null) return 1;
            return 0;
        }

        // Anything on the object or its parents that may move it at runtime.
        static bool HasMovers(Transform t)
        {
            for (var current = t; current != null; current = current.parent)
            {
                if (current.GetComponent<Animator>() != null || current.GetComponent<Animation>() != null) return true;
                if (current.GetComponent<Rigidbody>() != null || current.GetComponent<NavMeshAgent>() != null) return true;
                if (current.GetComponents<MonoBehaviour>().Length > 0) return true;
            }
            return false;
        }

        static MethodInfo hasInstancingMethod;
        static bool hasInstancingLookedUp;

        // null when the editor has no way to tell (ShaderUtil.HasInstancing is not public).
        static bool? ShaderSupportsInstancing(Shader shader)
        {
            if (shader == null) return false;
            if (!hasInstancingLookedUp)
            {
                hasInstancingLookedUp = true;
                hasInstancingMethod = ReflectionUtil.FindStaticMethod(typeof(ShaderUtil), "HasInstancing", new[] { typeof(Shader) });
            }
            if (hasInstancingMethod == null) return null;
            try
            {
                return (bool)hasInstancingMethod.Invoke(null, new object[] { shader });
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
