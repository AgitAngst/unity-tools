using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        static void AuditGeneral(AuditContext ctx)
        {
            var b = ctx.budgets;
            foreach (var go in ctx.objects)
            {
                var t = go.transform;
                if (t.childCount == 0 && go.GetComponents<Component>().Length == 1)
                    ctx.Report("GEN_EMPTY_LEAF", Category.General, Severity.Info, go);

                var scale = t.localScale;
                if (scale.x < 0f || scale.y < 0f || scale.z < 0f)
                    ctx.Report("GEN_NEGATIVE_SCALE", Category.General, Severity.Info, go);

                // Only the first object past the limit, so a deep branch gives one row, not dozens.
                if (AuditContext.DepthOf(t) == VRBudgets.HierarchyDepthInfo + 1)
                    ctx.Report("GEN_DEEP_HIERARCHY", Category.General, Severity.Info, go, VRBudgets.HierarchyDepthInfo);

                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missing > 0)
                    ctx.Report("GEN_MISSING_SCRIPTS", Category.General, Severity.Warning, go, missing);

                if (!go.activeInHierarchy) AuditInactiveHeavy(ctx, go, b);
            }
        }

        // Inactive objects still load their meshes and textures with the scene.
        static void AuditInactiveHeavy(AuditContext ctx, GameObject go, VRBudgets b)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            long tris = AuditContext.Tris(AuditContext.MeshOf(renderer));
            if (tris > b.objectTrisWarning)
                ctx.Report("GEN_INACTIVE_HEAVY_MESH", Category.General, Severity.Info, go, tris);

            int largest = 0;
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null) continue;
                foreach (var pair in ctx.TexturesOf(material))
                    largest = Mathf.Max(largest, Mathf.Max(pair.Key.width, pair.Key.height));
            }
            if (largest > b.textureSizeWarning)
                ctx.Report("GEN_INACTIVE_HEAVY_TEXTURE", Category.General, Severity.Info, go, largest);
        }
    }
}
