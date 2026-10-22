using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace UnityTools.SceneOptimization
{
    public sealed class AuditResult
    {
        public List<AuditIssue> issues = new List<AuditIssue>();
        public XrInfo xr;
        public QualitySnapshot quality;
        public VRBudgets budgets;
        public string[] sceneNames = new string[0];
        public string activeScenePath = "";
        public DateTime time;
        public long sceneTris;
        public long drawCallEstimate;
        public double textureMemoryMB;

        public int Count(Severity severity)
        {
            int n = 0;
            foreach (var issue in issues) if (issue.severity == severity) n++;
            return n;
        }

        public int Count(Category category, Severity severity)
        {
            int n = 0;
            foreach (var issue in issues) if (issue.category == category && issue.severity == severity) n++;
            return n;
        }
    }

    // Entry point of the audit. Each category lives in its own partial file under Checks/.
    public static partial class SceneAuditor
    {
        // Returns null when the user cancels the progress bar.
        public static AuditResult RunFullAudit(VRBudgets budgets, bool showProgress)
        {
            var steps = new KeyValuePair<Category, Action<AuditContext>>[]
            {
                new KeyValuePair<Category, Action<AuditContext>>(Category.DrawCalls, AuditDrawCalls),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Meshes, AuditMeshes),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Textures, AuditTextures),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Lighting, AuditLighting),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Physics, AuditPhysics),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Animators, AuditAnimators),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Audio, AuditAudio),
                new KeyValuePair<Category, Action<AuditContext>>(Category.VRSpecific, AuditVRSpecific),
                new KeyValuePair<Category, Action<AuditContext>>(Category.General, AuditGeneral),
            };

            try
            {
                if (Progress(showProgress, Strings.Get("ui.progress.collect"), 0f)) return null;
                var ctx = new AuditContext(budgets);
                ctx.Collect();
                for (int i = 0; i < steps.Length; i++)
                {
                    var info = Strings.Format("ui.progress.category", new object[] { Strings.CategoryName(steps[i].Key) });
                    if (Progress(showProgress, info, (i + 1f) / (steps.Length + 1f))) return null;
                    steps[i].Value(ctx);
                }
                var result = ToResult(ctx);
                UnityEngine.Debug.Log("[SceneAudit] Audit " + ToolVersion.Full + ": " + result.issues.Count + " issues in " + result.sceneNames.Length + " scene(s).");
                return result;
            }
            finally
            {
                if (showProgress) EditorUtility.ClearProgressBar();
            }
        }

        static bool Progress(bool show, string info, float value)
        {
            return show && EditorUtility.DisplayCancelableProgressBar(Strings.Get("ui.title"), info, value);
        }

        static AuditResult ToResult(AuditContext ctx)
        {
            return new AuditResult
            {
                issues = ctx.issues.OrderBy(i => (int)i.category).ThenBy(i => (int)i.severity).ToList(),
                xr = ctx.xr,
                quality = ctx.quality,
                budgets = ctx.budgets,
                sceneNames = ctx.scenes.Select(s => s.name).ToArray(),
                activeScenePath = SceneManager.GetActiveScene().path,
                time = DateTime.Now,
                sceneTris = ctx.sceneTris,
                drawCallEstimate = ctx.drawCallEstimate,
                textureMemoryMB = ctx.textureMemoryMB
            };
        }

        // Shared helper: Warning or Critical issue for a value over a budget.
        static void Budget(AuditContext ctx, string checkId, Category category, double value, double warning, double critical)
        {
            if (value > critical) ctx.Report(checkId, category, Severity.Critical, null, Round(value), critical);
            else if (value > warning) ctx.Report(checkId, category, Severity.Warning, null, Round(value), warning);
        }

        static object Round(double value)
        {
            return Math.Abs(value - Math.Round(value)) < 0.0001 ? (object)(long)Math.Round(value) : Math.Round(value, 1);
        }
    }
}
