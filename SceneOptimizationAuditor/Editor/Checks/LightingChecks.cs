using System;
using System.Collections.Generic;
using System.Linq;
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

            AuditLightmapUv(ctx);

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

            ctx.lighting = LightReport.Build(ctx.roots, q.pixelLightCount, q.shadowmaskMode);
            // A realtime point or spot costs a pass on every object in its range; baked, it costs none.
            foreach (var issue in ctx.issues)
                if (issue.checkId == "LIGHT_REALTIME") issue.Gain(0, InRangeOf(ctx.lighting, issue.target as Light), 0, true);
            AuditBakedLighting(ctx, ctx.lighting);
            AuditReflections(ctx, ctx.lighting);
        }

        static long InRangeOf(LightReport report, Light light)
        {
            foreach (var s in report.sources)
                if (s.light == light) return s.inRange;
            return 0;
        }

        // Settings path that opens the Lighting window instead of a Project Settings page.
        public const string LightingWindow = "Lighting";

        // Lightmaps, the objects in them, the lights and the probes — the numbers of the Lighting tab.
        static void AuditBakedLighting(AuditContext ctx, LightReport report)
        {
            var b = ctx.budgets;
            var q = ctx.quality;
            double mb = Math.Round(report.MB, 1);
            // Compared as shown: rounded to 0.1 MB.
            if (mb > b.lightmapMemoryCriticalMB)
                ctx.Report("LIGHT_LIGHTMAP_MEMORY", Category.Lighting, Severity.Critical, null, mb, b.lightmapMemoryCriticalMB);
            else if (mb > b.lightmapMemoryWarningMB)
                ctx.Report("LIGHT_LIGHTMAP_MEMORY", Category.Lighting, Severity.Warning, null, mb, b.lightmapMemoryWarningMB);
            if (report.atlases.Count > 0 && !report.compressed)
                ctx.ReportSetting("LIGHT_LIGHTMAP_UNCOMPRESSED", Category.Lighting, Severity.Warning, LightingWindow, mb)
                    .Gain(report.MB * (1 - VRBudgets.CompressedLightmapShare), 0, 0, true);
            if (report.HasDirectionTextures)
                ctx.ReportSetting("LIGHT_LIGHTMAP_DIRECTIONAL", Category.Lighting, Severity.Info, LightingWindow, Math.Round(report.DirectionMB, 1))
                    .Gain(report.DirectionMB, 0, 0, false);

            if (report.NoLightmap > 0 || report.NotBakedLights > 0)
                ctx.ReportSetting("LIGHT_NOT_BAKED", Category.Lighting, Severity.Warning, LightingWindow, report.NoLightmap, report.NotBakedLights);

            foreach (var o in report.objects)
            {
                if (o.small)
                {
                    var issue = ctx.Report("LIGHT_SMALL_IN_LIGHTMAP", Category.Lighting, Severity.Info, o.renderer.gameObject, Math.Round(o.size, 2));
                    // Its part of the atlas, once the lighting is baked again without it.
                    if (o.atlas >= 0) issue.Gain(o.share * report.atlases[o.atlas].bytes / (1024.0 * 1024.0), 0, 0, true);
                    var target = o.renderer;
                    issue.SetFix(FixKind.Review, target, false, () => target.receiveGI = ReceiveGI.LightProbes);
                }
                if (o.much)
                    ctx.Report("LIGHT_LIGHTMAP_SHARE", Category.Lighting, Severity.Warning, o.renderer.gameObject,
                        Math.Round(o.share * 100.0), o.atlas, Math.Round(o.scale, 2));
            }

            if (report.groups.Count == 0 && report.probeObjects > 0 && report.UsesBakedLighting)
                ctx.Report("LIGHT_NO_PROBES", Category.Lighting, Severity.Warning, null, report.probeObjects);
            foreach (var o in report.outside)
                ctx.Report("LIGHT_OUTSIDE_PROBES", Category.Lighting, Severity.Info, o.renderer.gameObject, Math.Round(o.distance, 1));

            if (q.shadowmaskMode == ShadowmaskMode.DistanceShadowmask && report.mixedMode == MixedLightingMode.Shadowmask && report.HasMixedLights)
                ctx.ReportSetting("LIGHT_DISTANCE_SHADOWMASK", Category.Lighting, Severity.Warning, "Project/Quality");
            bool realtimeSun = report.sources.Any(s => s.light.type == LightType.Directional && s.light.shadows != LightShadows.None &&
                                                       s.light.lightmapBakeType != LightmapBakeType.Baked);
            if (q.shadows != ShadowQuality.Disable && q.shadowCascades == 4 && realtimeSun)
                ctx.ReportSetting("LIGHT_SHADOW_CASCADES", Category.Lighting, Severity.Warning, "Project/Quality");

            foreach (var s in report.sources)
            {
                if (s.wide)
                    ctx.Report("LIGHT_WIDE_REALTIME", Category.Lighting, Severity.Warning, s.light, s.light.type.ToString(), s.inRange, VRBudgets.WideLightRenderers)
                        .Gain(0, s.inRange, 0, true);
                if (s.important)
                    ctx.Report("LIGHT_IMPORTANT", Category.Lighting, Severity.Warning, s.light, s.light.type.ToString()).Gain(0, s.inRange, 0, true);
            }
        }
    }
}
