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

            // The whole scene: every material slot is one call, plus shadow passes, no batching or culling. It is shown for
            // reference; the budget per frame is checked at the viewpoints (FrameBudget).
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
            // Without shadow passes (shadows off, no shadowed realtime light) the casters cost nothing.
            if (casters > 0 && shadowPasses > 0) ctx.Report("DC_SHADOW_CASTERS", Category.DrawCalls, Severity.Info, null, casters, shadowPasses);

            var instancingGroups = new Dictionary<Material, Dictionary<Mesh, int>>();
            foreach (var renderer in ctx.All<MeshRenderer>())
            {
                if (!AuditContext.IsActive(renderer)) continue;
                var mesh = AuditContext.MeshOf(renderer);
                if (mesh == null) continue;
                var go = renderer.gameObject;
                var materials = renderer.sharedMaterials;

                // One material in an atlas instead of several: one call instead of one per material.
                if (materials.Length > VRBudgets.MultiMaterialWarning)
                    ctx.Report("DC_MANY_MATERIALS", Category.DrawCalls, Severity.Warning, renderer, materials.Length).Gain(0, materials.Length - 1, 0, true);
                else if (materials.Length > 1)
                    ctx.Report("DC_MULTI_MATERIAL", Category.DrawCalls, Severity.Info, renderer, materials.Length).Gain(0, materials.Length - 1, 0, true);
                if (materials.Length > mesh.subMeshCount)
                    ctx.Report("DC_EXTRA_MATERIALS", Category.DrawCalls, Severity.Warning, renderer, materials.Length, mesh.subMeshCount)
                        .Gain(0, materials.Length - mesh.subMeshCount, 0, false);

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
                issue.Gain(0, instances - 1, 0, false);
                if (AuditContext.IsEditableAsset(material))
                    issue.SetFix(FixKind.Safe, material, true, () => material.enableInstancing = true);
            }
        }

        // Draw calls of a renderer before batching: its material slots, one for particles, lines and sprites.
        internal static int SlotCount(Renderer renderer)
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
