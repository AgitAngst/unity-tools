using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        static void AuditLighting(AuditContext ctx)
        {
            var b = ctx.budgets;
            var q = ctx.quality;

            var shadowed = new List<string>();
            foreach (var light in ctx.All<Light>())
            {
                if (!AuditContext.IsActive(light)) continue;
                bool pointOrSpot = light.type == LightType.Point || light.type == LightType.Spot;
                if (pointOrSpot && light.lightmapBakeType == LightmapBakeType.Realtime)
                    ctx.Report("LIGHT_REALTIME", Category.Lighting, Severity.Warning, light, light.type.ToString());
                if (light.shadows != LightShadows.None && light.lightmapBakeType != LightmapBakeType.Baked)
                    shadowed.Add(light.name);
            }
            if (shadowed.Count > b.shadowLightsCritical)
                ctx.Report("LIGHT_SHADOWED", Category.Lighting, Severity.Critical, null, shadowed.Count, b.shadowLightsCritical, string.Join(", ", shadowed.ToArray()));
            else if (shadowed.Count > b.shadowLightsWarning)
                ctx.Report("LIGHT_SHADOWED", Category.Lighting, Severity.Warning, null, shadowed.Count, b.shadowLightsWarning, string.Join(", ", shadowed.ToArray()));

            foreach (var renderer in ctx.All<MeshRenderer>())
            {
                if (!AuditContext.IsActive(renderer)) continue;
                var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                if ((flags & StaticEditorFlags.ContributeGI) == 0 || renderer.receiveGI != ReceiveGI.Lightmaps) continue;
                var mesh = AuditContext.MeshOf(renderer);
                if (mesh != null && !mesh.HasVertexAttribute(VertexAttribute.TexCoord1))
                    ctx.Report("LIGHT_NO_UV1", Category.Lighting, Severity.Warning, renderer, mesh.name);
            }

            if (q.shadows != ShadowQuality.Disable)
            {
                if (q.shadowResolution == ShadowResolution.VeryHigh)
                    ctx.ReportSetting("LIGHT_SHADOW_RESOLUTION", Category.Lighting, Severity.Warning, "Project/Quality", q.shadowResolution.ToString());
                else if (q.shadowResolution == ShadowResolution.High)
                    ctx.ReportSetting("LIGHT_SHADOW_RESOLUTION", Category.Lighting, Severity.Info, "Project/Quality", q.shadowResolution.ToString());
                if (q.shadowDistance > VRBudgets.ShadowDistanceInfo)
                    ctx.ReportSetting("LIGHT_SHADOW_DISTANCE", Category.Lighting, Severity.Info, "Project/Quality", q.shadowDistance, VRBudgets.ShadowDistanceInfo);
            }

            if (q.pixelLightCount > 1)
                ctx.ReportSetting("LIGHT_PIXEL_LIGHTS", Category.Lighting, Severity.Warning, "Project/Quality", q.pixelLightCount);

#pragma warning disable 0618
            if (Lightmapping.realtimeGI)
                ctx.Report("LIGHT_REALTIME_GI", Category.Lighting, Severity.Info, null);
#pragma warning restore 0618
        }
    }
}
