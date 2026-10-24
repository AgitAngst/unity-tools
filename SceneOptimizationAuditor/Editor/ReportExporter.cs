using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    // Writes the audit result as JSON or CSV, from the window or from the command line.
    public static class ReportExporter
    {
        [Serializable]
        public sealed class XrDto
        {
            public string system;
            public string loader;
            public string stereoMode;
        }

        [Serializable]
        public sealed class SummaryDto
        {
            public int critical;
            public int warning;
            public int info;
            public int accepted;
        }

        [Serializable]
        public sealed class BudgetsDto
        {
            public bool isEstimate = true;
            public long totalTris;
            public int trisLimit;
            public long estimatedDrawCalls;
            public int drawCallLimit;
            public double textureMemoryMB;
            public int textureMemoryLimitMB;
        }

        [Serializable]
        public sealed class IssueDto
        {
            public string checkId;
            public string category;
            public string severity;
            public string scene;
            public string objectPath;
            public string assetPath;
            public string message;
            public string suggestion;
            public bool autoFixable;
            public bool accepted;
        }

        [Serializable]
        public sealed class ViewpointDto
        {
            public string name;
            public string kind;
            public float x;
            public float y;
            public float z;
            public float worstYaw;
            public long tris;
            public int drawCalls;
            public float transparentPercent;
            public bool overBudget;
        }

        [Serializable]
        public sealed class ViewpointsDto
        {
            public string estimatedAt = "";
            public string toolVersion = "";
            public bool wallsCounted;
            public int trisLimit;
            public int drawCallLimit;
            public int transparentLimitPercent;
            public ViewpointDto[] points = new ViewpointDto[0];
        }

        [Serializable]
        public sealed class ReportDto
        {
            public string[] scenes;
            public string timestamp;
            public string toolVersion = ToolVersion.Full;
            public string platform = "PC VR - HTC Vive Pro";
            public int targetFPS = VRBudgets.TargetFps;
            // Id of the target GPU, or "Custom" when the thresholds differ from its set.
            public string targetGpu;
            public XrDto xr;
            public string qualityLevel;
            public SummaryDto summary;
            public BudgetsDto sceneBudgets;
            public bool hasProfilerSnapshot;
            public ProfilerSnapshot profilerSnapshot;
            public bool hasViewpoints;
            public ViewpointsDto viewpoints;
            public IssueDto[] issues;
        }

        public static void Save(AuditResult result, string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var text = ext == ".csv" ? ToCsv(result) : ToJson(result, ProfilerCapture.LoadLast(result.activeScenePath));
            File.WriteAllText(path, text, new UTF8Encoding(ext == ".csv"));
            Debug.Log("[SceneAudit] Scene audit report saved: " + path);
        }

        // The last viewpoint estimate of the audited scenes goes into the report when there is one.
        public static string ToJson(AuditResult result, ProfilerSnapshot snapshot)
        {
            var set = ViewpointSet.Load(result.scenePaths);
            try
            {
                return ToJson(result, snapshot, set.HasEstimate ? set : null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(set);
            }
        }

        public static string ToJson(AuditResult result, ProfilerSnapshot snapshot, ViewpointSet viewpoints)
        {
            var report = new ReportDto
            {
                scenes = result.sceneNames,
                timestamp = result.time.ToString("o", CultureInfo.InvariantCulture),
                targetGpu = result.budgets.GpuReportName,
                xr = new XrDto
                {
                    system = result.xr.SystemName,
                    loader = result.xr.DeviceName,
                    stereoMode = result.xr.stereoMode.ToString()
                },
                qualityLevel = result.quality.name,
                summary = new SummaryDto
                {
                    critical = result.Count(Severity.Critical),
                    warning = result.Count(Severity.Warning),
                    info = result.Count(Severity.Info),
                    accepted = result.AcceptedCount
                },
                sceneBudgets = new BudgetsDto
                {
                    totalTris = result.sceneTris,
                    trisLimit = result.budgets.sceneTrisWarning,
                    estimatedDrawCalls = result.drawCallEstimate,
                    drawCallLimit = result.budgets.drawCallsWarning,
                    textureMemoryMB = Math.Round(result.textureMemoryMB, 1),
                    textureMemoryLimitMB = result.budgets.textureMemoryWarningMB
                },
                // JsonUtility cannot write null for a nested class, hence the flag.
                hasProfilerSnapshot = snapshot != null,
                profilerSnapshot = snapshot ?? new ProfilerSnapshot(),
                hasViewpoints = viewpoints != null,
                viewpoints = viewpoints != null ? ViewpointsOf(viewpoints, result.budgets) : new ViewpointsDto(),
                issues = new IssueDto[result.issues.Count]
            };
            for (int i = 0; i < result.issues.Count; i++)
            {
                var issue = result.issues[i];
                report.issues[i] = new IssueDto
                {
                    checkId = issue.checkId,
                    category = issue.category.ToString(),
                    severity = issue.severity.ToString(),
                    scene = issue.scene,
                    objectPath = issue.objectPath,
                    assetPath = !string.IsNullOrEmpty(issue.assetPath) ? issue.assetPath : issue.settingsPath,
                    message = issue.Message,
                    suggestion = issue.Suggestion,
                    autoFixable = issue.AutoFixable,
                    accepted = issue.accepted
                };
            }
            return JsonUtility.ToJson(report, true);
        }

        static ViewpointsDto ViewpointsOf(ViewpointSet set, VRBudgets b)
        {
            return new ViewpointsDto
            {
                estimatedAt = set.estimatedAt,
                toolVersion = set.estimateVersion,
                wallsCounted = set.estimateWalls,
                trisLimit = b.sceneTrisWarning,
                drawCallLimit = b.drawCallsWarning,
                transparentLimitPercent = b.transparentWarningPercent,
                points = set.points.Where(p => p.Estimated).Select(p => new ViewpointDto
                {
                    name = p.Name,
                    kind = p.kind.ToString(),
                    x = p.position.x,
                    y = p.position.y,
                    z = p.position.z,
                    worstYaw = p.Worst.yaw,
                    tris = p.Worst.tris,
                    drawCalls = p.Worst.drawCalls,
                    transparentPercent = p.Worst.transparentPercent,
                    overBudget = ViewpointEstimator.LevelOf(p.Worst, b) != null
                }).ToArray()
            };
        }

        public static string ToCsv(AuditResult result)
        {
            var sb = new StringBuilder();
            sb.Append("CheckId,Category,Severity,Scene,ObjectPath,AssetPath,Message,Suggestion,AutoFixable,Accepted\r\n");
            foreach (var issue in result.issues)
            {
                var assetPath = !string.IsNullOrEmpty(issue.assetPath) ? issue.assetPath : issue.settingsPath;
                sb.Append(Csv(issue.checkId)).Append(',')
                  .Append(Csv(issue.category.ToString())).Append(',')
                  .Append(Csv(issue.severity.ToString())).Append(',')
                  .Append(Csv(issue.scene)).Append(',')
                  .Append(Csv(issue.objectPath)).Append(',')
                  .Append(Csv(assetPath)).Append(',')
                  .Append(Csv(issue.Message)).Append(',')
                  .Append(Csv(issue.Suggestion)).Append(',')
                  .Append(issue.AutoFixable ? "true" : "false").Append(',')
                  .Append(issue.accepted ? "true" : "false").Append("\r\n");
            }
            return sb.ToString();
        }

        // RFC 4180: quote fields with commas, quotes or line breaks; double inner quotes.
        public static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            bool quote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            var escaped = value.Replace("\"", "\"\"");
            return quote ? "\"" + escaped + "\"" : escaped;
        }

        // Unity.exe -batchmode -quit -projectPath <project> -executeMethod
        //   UnityTools.SceneOptimization.ReportExporter.RunFromCommandLine
        //   -auditScene Assets/Scenes/Main.unity -auditReport E:/reports/main.json [-auditLang en|ru]
        //   [-auditHistory [-auditNote "text"]] [-auditViewpoints]
        public static void RunFromCommandLine()
        {
            EditorApplication.Exit(RunWithArgs(Environment.GetCommandLineArgs()));
        }

        // The export behind RunFromCommandLine for callers inside a running editor: the same arguments,
        // the exit code is returned instead of quitting the editor.
        public static int RunWithArgs(string[] args)
        {
            int code = 1;
            try
            {
                var scene = Argument(args, "-auditScene");
                var report = Argument(args, "-auditReport");
                var lang = Argument(args, "-auditLang");
                if (!string.IsNullOrEmpty(lang)) Strings.Current = lang.ToLowerInvariant() == "ru" ? Lang.Ru : Lang.En;
                if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(report))
                {
                    Debug.LogError("[SceneAudit] Usage: -auditScene <Assets/...unity> -auditReport <file.json|file.csv> [-auditLang en|ru] [-auditHistory [-auditNote <text>]] [-auditViewpoints]");
                }
                else
                {
                    EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                    var result = SceneAuditor.RunFullAudit(VRBudgets.Load(), false);
                    if (result == null) Debug.LogError("[SceneAudit] Audit returned no result.");
                    else
                    {
                        if (Array.IndexOf(args, "-auditViewpoints") >= 0) EstimateViewpoints(result.scenePaths);
                        Save(result, report);
                        if (Array.IndexOf(args, "-auditHistory") >= 0)
                        {
                            AuditHistory.Record(result, Argument(args, "-auditNote"));
                            Debug.Log("[SceneAudit] History point saved: " + AuditHistory.FileFor(result.scenePaths));
                        }
                        code = 0;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[SceneAudit] Command-line export failed: " + e);
            }
            return code;
        }

        // -auditViewpoints: estimates the saved viewpoints of the scenes; without a viewpoint file they are placed
        // from the NavMesh and teleports first (rays down when there are neither).
        static void EstimateViewpoints(string[] scenePaths)
        {
            var set = ViewpointSet.Load(scenePaths);
            try
            {
                if (set.points.Count == 0)
                {
                    float step;
                    var settings = new PlacementSettings { navMesh = true, teleports = true, rays = true };
                    set.placement = settings;
                    set.ReplaceAuto(ViewpointPlacer.Place(settings, out step), step);
                }
                if (set.points.Count == 0)
                {
                    Debug.LogWarning("[SceneAudit] No viewpoints: the scene has no NavMesh, teleports or surfaces to stand on.");
                    return;
                }
                ViewpointEstimator.Estimate(set, set.points, set.estimateWalls, null);
                set.Save();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(set);
            }
        }

        static string Argument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
