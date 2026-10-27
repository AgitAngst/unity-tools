using System;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        // Reflection probes as the Lighting tab shows them: resolution, memory, bake, box projection, duplicates.
        static void AuditReflections(AuditContext ctx, LightReport report)
        {
            foreach (var row in report.reflections)
            {
                var probe = row.probe;
                // Down to the Warning resolution: the cubemap shrinks with the area of its faces.
                double k = (double)VRBudgets.ReflectionResolutionWarning / Math.Max(1, row.resolution);
                double smaller = row.MB * (1 - k * k);
                if (row.resolution >= VRBudgets.ReflectionResolutionCritical)
                    ctx.Report("REFL_RESOLUTION", Category.Lighting, Severity.Critical, probe, row.resolution, VRBudgets.ReflectionResolutionWarning, Math.Round(row.MB, 1))
                        .Gain(smaller, 0, 0, row.estimated);
                else if (row.Large)
                    ctx.Report("REFL_RESOLUTION", Category.Lighting, Severity.Warning, probe, row.resolution, VRBudgets.ReflectionResolutionWarning, Math.Round(row.MB, 1))
                        .Gain(smaller, 0, 0, row.estimated);
                if (row.notBaked)
                    ctx.Report("REFL_NOT_BAKED", Category.Lighting, Severity.Warning, probe, row.mode.ToString());
                if (row.noBox)
                {
                    var size = row.bounds.size;
                    var issue = ctx.Report("REFL_NO_BOX", Category.Lighting, Severity.Info, probe, Math.Round(Math.Max(size.x, Math.Max(size.y, size.z)), 1));
                    var target = probe;
                    issue.SetFix(FixKind.Review, target, false, () => target.boxProjection = true);
                }
                if (row.Duplicate)
                    ctx.Report("REFL_DUPLICATE", Category.Lighting, Severity.Warning, probe, row.duplicateOf.name, (int)Math.Round(row.overlap * 100f))
                        .Gain(row.MB, 0, 0, row.estimated);
            }
            // Compared as shown: rounded to 0.1 MB.
            double mb = Math.Round(report.ReflectionMB, 1);
            if (mb > VRBudgets.ReflectionMemoryWarningMB)
                ctx.Report("REFL_MEMORY", Category.Lighting, Severity.Warning, null, mb, VRBudgets.ReflectionMemoryWarningMB, report.reflections.Count);
            if (report.outsideReflection.Count > 0)
                ctx.Report("REFL_OUTSIDE", Category.Lighting, Severity.Info, null, report.outsideReflection.Count, report.reflections.Count);
            if (ctx.quality.realtimeReflectionProbes)
                ctx.ReportSetting("REFL_QUALITY_REALTIME", Category.Lighting, Severity.Info, "Project/Quality");
        }
    }
}
