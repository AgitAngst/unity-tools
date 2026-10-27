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
        public string[] scenePaths = new string[0];
        public string activeScenePath = "";
        public DateTime time;
        public long sceneTris;
        public long drawCallEstimate;
        public double textureMemoryMB;
        // Lightmaps, the objects in them, the lights and the probes, as the lighting checks saw them.
        public LightReport lighting;
        // Sources of the active renderers (Heaviest tab), for the report.
        public HeavyReport heavy;

        // What was checked (AuditScope): the open scenes, a selection, scenes one by one or a folder.
        public ScopeKind scope = ScopeKind.Loaded;
        public string folder = "";
        // Scenes checked one by one: a row each, and the runs themselves (history points, refresh after fixes).
        public List<SceneSummary> scenes = new List<SceneSummary>();
        public List<AuditResult> parts;
        // This part is the run of the scenes open in the editor.
        public bool open;
        // Selection: the selected objects (checked again after fixes) and all of them with their children.
        public UnityEngine.GameObject[] selection;
        public int selected;
        public int selectedWithChildren;
        public FolderCounts folderCounts;
        // Scenes one by one, cancelled: how many were to be checked.
        public bool incomplete;
        public int planned;
        // Every issue of the run before the selection filter, so that copies of a prefab count all copies.
        public List<AuditIssue> allIssues;
        // How long the run took (0: not measured), whether it only brought the list back after a script reload, and the
        // last quick re-check: how many objects, assets and settings, and when (2.7.0).
        public double seconds;
        public bool afterReload;
        public int rechecked;
        public bool recheckedObjects;
        public DateTime recheckTime;

        // Counts leave accepted problems out.
        public int Count(Severity severity)
        {
            int n = 0;
            foreach (var issue in issues) if (!issue.accepted && issue.severity == severity) n++;
            return n;
        }

        public int Count(Category category, Severity severity)
        {
            int n = 0;
            foreach (var issue in issues) if (!issue.accepted && issue.category == category && issue.severity == severity) n++;
            return n;
        }

        public int ActiveCount { get { return issues.Count(i => !i.accepted); } }

        public int AcceptedCount { get { return issues.Count(i => i.accepted); } }
    }

    // Entry point of the audit. Each category lives in its own partial file under Checks/.
    public static partial class SceneAuditor
    {
        // Returns null when the user cancels the progress bar.
        public static AuditResult RunFullAudit(VRBudgets budgets, bool showProgress)
        {
            return RunFullAudit(budgets, showProgress, null, null);
        }

        // The steps of a run, a category each, in the order of the progress bar; a quick re-check runs them too.
        internal static KeyValuePair<Category, Action<AuditContext>>[] Steps()
        {
            return new KeyValuePair<Category, Action<AuditContext>>[]
            {
                new KeyValuePair<Category, Action<AuditContext>>(Category.DrawCalls, AuditDrawCalls),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Meshes, AuditMeshes),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Textures, AuditTextures),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Materials, AuditMaterials),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Shaders, AuditShaders),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Lighting, AuditLighting),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Particles, AuditParticles),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Physics, AuditPhysics),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Animators, AuditAnimators),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Audio, AuditAudio),
                new KeyValuePair<Category, Action<AuditContext>>(Category.VRSpecific, AuditVRSpecific),
                new KeyValuePair<Category, Action<AuditContext>>(Category.General, AuditGeneral),
                new KeyValuePair<Category, Action<AuditContext>>(Category.Terrain, AuditTerrain),
            };
        }

        // only: the scenes to check (null: every loaded scene); extraAssets: loose assets to check too.
        public static AuditResult RunFullAudit(VRBudgets budgets, bool showProgress, IList<Scene> only, IList<UnityEngine.Object> extraAssets)
        {
            var steps = Steps();

            try
            {
                if (Progress(showProgress, Strings.Get("ui.progress.collect"), 0f)) return null;
                // Slow steps are named in the log line, so a heavy scene shows where the time goes.
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var slow = new List<string>();
                var ctx = new AuditContext(budgets, only, extraAssets);
                ctx.Collect();
                if (watch.Elapsed.TotalSeconds > 0.5) slow.Add("collect " + watch.Elapsed.TotalSeconds.ToString("F1") + " s");
                for (int i = 0; i < steps.Length; i++)
                {
                    var info = Strings.Format("ui.progress.category", new object[] { Strings.CategoryName(steps[i].Key) });
                    if (Progress(showProgress, info, (i + 1f) / (steps.Length + 1f))) return null;
                    var stepStart = watch.Elapsed.TotalSeconds;
                    steps[i].Value(ctx);
                    var took = watch.Elapsed.TotalSeconds - stepStart;
                    if (took > 0.5) slow.Add(steps[i].Key + " " + took.ToString("F1") + " s");
                }
                var result = ToResult(ctx);
                UnityEngine.Debug.Log("[SceneAudit] Audit " + ToolVersion.Full + ": " + result.issues.Count + " issues in " +
                    result.sceneNames.Length + " scene(s), " + watch.Elapsed.TotalSeconds.ToString("F1") + " s" +
                    (slow.Count > 0 ? " (slow: " + string.Join(", ", slow.ToArray()) + ")" : "") + ".");
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
            var result = new AuditResult
            {
                issues = ctx.issues.OrderBy(i => (int)i.category).ThenBy(i => (int)i.severity).ToList(),
                xr = ctx.xr,
                quality = ctx.quality,
                budgets = ctx.budgets,
                sceneNames = ctx.scenes.Select(s => s.name).ToArray(),
                scenePaths = ctx.scenes.Select(s => s.path).ToArray(),
                activeScenePath = SceneManager.GetActiveScene().path,
                time = DateTime.Now,
                sceneTris = ctx.sceneTris,
                drawCallEstimate = ctx.drawCallEstimate,
                textureMemoryMB = ctx.textureMemoryMB,
                lighting = ctx.lighting,
                heavy = HeavyReport.Build(ctx.roots, ctx.budgets.objectTrisWarning)
            };
            if (ctx.scenes.Count == 1) AcceptedList.KeepScene(result.issues, ctx.scenes[0].path);
            AcceptedList.Load().Apply(result.issues);
            return result;
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
