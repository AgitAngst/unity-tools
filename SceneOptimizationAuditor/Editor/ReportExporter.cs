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
            // What fixing every problem that is not acceptable would free (rough sums).
            public double gainMB;
            public long gainDrawCalls;
            public long gainTris;
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
            public string scenePath;
            public string objectPath;
            public string assetPath;
            public string message;
            public string suggestion;
            public bool autoFixable;
            public bool accepted;
            public double gainMB;
            public long gainDrawCalls;
            public long gainTris;
            public bool gainApprox;
        }

        // One scene of a check of several scenes; budgets are -1 for scenes checked together with other open scenes.
        [Serializable]
        public sealed class SceneDto
        {
            public string path;
            public int critical;
            public int warning;
            public int info;
            public long tris;
            public long drawCalls;
            public double textureMB;
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

        // Worst direction of a viewpoint measured in Play Mode. Times in milliseconds; GPU and dropped frames -1 when unknown.
        [Serializable]
        public sealed class TourPointDto
        {
            public string name;
            public float worstYaw;
            public float frameMs;
            public float peakFrameMs;
            public float cpuMs;
            public float gpuMs;
            public int dropped;
            public int drawCalls;
            public int batches;
            public int setPassCalls;
            public long tris;
            public bool slow;
            public bool movedByGame;
        }

        [Serializable]
        public sealed class TourDto
        {
            public string time = "";
            public string toolVersion = "";
            public bool interrupted;
            public bool headset;
            public int frames;
            public int warmup;
            public int directions;
            public string mover = "";
            public float frameTimeLimitMs;
            public TourPointDto[] points = new TourPointDto[0];
        }

        // Lightmaps, the objects in them and the light probes of the audited scenes.
        [Serializable]
        public sealed class LightingDto
        {
            public int lightmaps;
            public double lightmapMB;
            public int lightmapLimitMB;
            public bool directional;
            public bool compressed;
            public int objectsInLightmaps;
            public int withoutLightmap;
            public int probes;
            public int probeGroups;
            public int outsideProbes;
            public int reflectionProbes;
            public double reflectionMB;
            public int outsideReflection;
        }

        // One of the sources with the most triangles (Heaviest tab).
        [Serializable]
        public sealed class HeavyDto
        {
            public string name;
            public string kind;
            public string assetPath;
            public int copies;
            public long tris;
            public int drawCalls;
            public double textureMB;
        }

        [Serializable]
        public sealed class ReportDto
        {
            public string[] scenes;
            public string timestamp;
            public string toolVersion = ToolVersion.Full;
            // Filled from the target headset by ToJson.
            public string platform = "PC VR - HTC Vive Pro";
            public int targetFPS = VRBudgets.TargetFps;
            // Id of the target headset, its refresh rate and the SteamVR render resolution in percent.
            public string headset;
            public int refreshRate;
            public int renderScale;
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
            public bool hasViewpointTour;
            public TourDto viewpointTour;
            public bool hasLighting;
            public LightingDto lighting;
            public HeavyDto[] heavy;
            // What was checked: loaded, selection, buildScenes, scenes or folder; the folder; every scene of a check of
            // several scenes (sceneBudgets then hold the worst scene).
            public string scope;
            public string folder;
            public SceneDto[] sceneDetails;
            public IssueDto[] issues;
        }

        public static void Save(AuditResult result, string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var text = ext == ".csv" ? ToCsv(result) : ToJson(result, ProfilerCapture.LoadLast(result.activeScenePath));
            File.WriteAllText(path, text, new UTF8Encoding(ext == ".csv"));
            Debug.Log("[SceneAudit] Scene audit report saved: " + path);
        }

        // The last viewpoint estimate and tour of the audited scenes go into the report when there are any.
        public static string ToJson(AuditResult result, ProfilerSnapshot snapshot)
        {
            var set = ViewpointSet.Load(result.scenePaths);
            try
            {
                return ToJson(result, snapshot, set);
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
                platform = "PC VR - " + result.budgets.Headset.name,
                targetFPS = result.budgets.refreshRate,
                headset = result.budgets.Headset.id,
                refreshRate = result.budgets.refreshRate,
                renderScale = result.budgets.renderScale,
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
                    accepted = result.AcceptedCount,
                    gainMB = Math.Round(FixGain.Sum(result.issues).mb, 1),
                    gainDrawCalls = FixGain.Sum(result.issues).drawCalls,
                    gainTris = FixGain.Sum(result.issues).tris
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
                hasViewpoints = viewpoints != null && viewpoints.HasEstimate,
                viewpoints = viewpoints != null && viewpoints.HasEstimate ? ViewpointsOf(viewpoints, result.budgets) : new ViewpointsDto(),
                hasViewpointTour = HasTour(viewpoints),
                viewpointTour = HasTour(viewpoints) ? TourOf(viewpoints, result.budgets) : new TourDto(),
                hasLighting = result.lighting != null,
                lighting = result.lighting != null ? LightingOf(result.lighting, result.budgets) : new LightingDto(),
                heavy = result.heavy != null ? result.heavy.Sorted(0).Take(HeavyInReport).Select(s => new HeavyDto
                {
                    name = s.label,
                    kind = s.kind.ToString(),
                    assetPath = s.assetPath,
                    copies = s.Copies,
                    tris = s.tris,
                    drawCalls = s.drawCalls,
                    textureMB = Math.Round(s.TextureMB, 1)
                }).ToArray() : new HeavyDto[0],
                scope = ScopeName(result.scope),
                folder = result.folder,
                sceneDetails = result.scenes.Select(s => new SceneDto
                {
                    path = s.path,
                    critical = s.critical,
                    warning = s.warning,
                    info = s.info,
                    tris = s.tris,
                    drawCalls = s.drawCalls,
                    textureMB = s.textureMB < 0 ? -1 : Math.Round(s.textureMB, 1)
                }).ToArray(),
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
                    scenePath = issue.scenePath,
                    objectPath = issue.objectPath,
                    assetPath = !string.IsNullOrEmpty(issue.assetPath) ? issue.assetPath : issue.settingsPath,
                    message = issue.Message,
                    suggestion = issue.Suggestion,
                    autoFixable = issue.AutoFixable,
                    accepted = issue.accepted,
                    gainMB = Math.Round(issue.gainMB, 1),
                    gainDrawCalls = issue.gainDrawCalls,
                    gainTris = issue.gainTris,
                    gainApprox = issue.gainApprox
                };
            }
            return JsonUtility.ToJson(report, true);
        }

        static string ScopeName(ScopeKind kind)
        {
            switch (kind)
            {
                case ScopeKind.Selection: return "selection";
                case ScopeKind.BuildScenes: return "buildScenes";
                case ScopeKind.Scenes: return "scenes";
                case ScopeKind.Folder: return "folder";
                default: return "loaded";
            }
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

        // Sources in the report: the ones with the most triangles.
        const int HeavyInReport = 20;

        static LightingDto LightingOf(LightReport r, VRBudgets b)
        {
            return new LightingDto
            {
                lightmaps = r.atlases.Count,
                lightmapMB = Math.Round(r.MB, 1),
                lightmapLimitMB = b.lightmapMemoryWarningMB,
                directional = r.directional,
                compressed = r.compressed,
                objectsInLightmaps = r.objects.Count,
                withoutLightmap = r.NoLightmap,
                probes = r.probes,
                probeGroups = r.groups.Count,
                outsideProbes = r.outside.Count,
                reflectionProbes = r.reflections.Count,
                reflectionMB = Math.Round(r.ReflectionMB, 1),
                outsideReflection = r.outsideReflection.Count
            };
        }

        // A tour whose viewpoints were all removed since is no tour.
        static bool HasTour(ViewpointSet set)
        {
            return set != null && set.HasTour && set.MeasuredPoints().Count > 0;
        }

        static TourDto TourOf(ViewpointSet set, VRBudgets b)
        {
            var tour = set.tour;
            return new TourDto
            {
                time = tour.time,
                toolVersion = tour.toolVersion,
                interrupted = tour.interrupted,
                headset = tour.headset,
                frames = tour.frames,
                warmup = tour.warmup,
                directions = tour.directions,
                mover = tour.mover.ToString(),
                frameTimeLimitMs = b.frameTimeCriticalMs,
                points = set.MeasuredPoints().Select(m => new TourPointDto
                {
                    name = m.name,
                    worstYaw = m.Worst.yaw,
                    frameMs = m.Worst.frameMs,
                    peakFrameMs = m.Worst.peakFrameMs,
                    cpuMs = m.Worst.cpuMs,
                    gpuMs = m.Worst.gpuMs,
                    dropped = m.Worst.dropped,
                    drawCalls = m.Worst.drawCalls,
                    batches = m.Worst.batches,
                    setPassCalls = m.Worst.setPassCalls,
                    tris = m.Worst.tris,
                    slow = TourMath.IsSlow(m.Worst, tour.headset, b),
                    movedByGame = m.displaced > TourMath.MovedMetres
                }).ToArray()
            };
        }

        public static string ToCsv(AuditResult result)
        {
            var sb = new StringBuilder();
            sb.Append("CheckId,Category,Severity,Scene,ObjectPath,AssetPath,Message,Suggestion,AutoFixable,Accepted,ScenePath,GainMB,GainDrawCalls,GainTris\r\n");
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
                  .Append(issue.accepted ? "true" : "false").Append(',')
                  .Append(Csv(issue.scenePath)).Append(',')
                  .Append(Math.Round(issue.gainMB, 1).ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(issue.gainDrawCalls.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(issue.gainTris.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
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
        //   -auditScene Assets/Scenes/Main.unity[,Assets/Scenes/Other.unity…] | -auditBuildScenes | -auditFolder Assets/Art
        //   -auditReport E:/reports/main.json [-auditLang en|ru] [-auditHistory [-auditNote "text"]] [-auditViewpoints]
        // -auditScene may repeat; several scenes are checked one by one into one report.
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
                var scenes = Arguments(args, "-auditScene").SelectMany(a => a.Split(',', ';')).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                bool build = Array.IndexOf(args, "-auditBuildScenes") >= 0;
                var folder = Argument(args, "-auditFolder");
                var report = Argument(args, "-auditReport");
                var lang = Argument(args, "-auditLang");
                if (!string.IsNullOrEmpty(lang)) Strings.Current = lang.ToLowerInvariant() == "ru" ? Lang.Ru : Lang.En;
                if (build) scenes = AuditScope.BuildScenePaths();
                if ((scenes.Count == 0 && string.IsNullOrEmpty(folder)) || string.IsNullOrEmpty(report))
                {
                    Debug.LogError("[SceneAudit] Usage: -auditScene <Assets/...unity>[,<more>] | -auditBuildScenes | -auditFolder <Assets/...> -auditReport <file.json|file.csv> [-auditLang en|ru] [-auditHistory [-auditNote <text>]] [-auditViewpoints]");
                }
                else
                {
                    bool viewpoints = Array.IndexOf(args, "-auditViewpoints") >= 0;
                    bool history = Array.IndexOf(args, "-auditHistory") >= 0;
                    var note = Argument(args, "-auditNote");
                    var budgets = VRBudgets.Load();
                    AuditResult result;
                    if (!string.IsNullOrEmpty(folder)) result = AuditScope.RunFolder(folder, budgets, false);
                    else
                    {
                        // Every scene on its own, while it is open: viewpoints and history points per scene.
                        Action<AuditResult> afterEach = part =>
                        {
                            if (viewpoints) EstimateViewpoints(part.scenePaths);
                            if (!history) return;
                            AuditHistory.Record(part, note);
                            Debug.Log("[SceneAudit] History point saved: " + AuditHistory.FileFor(part.scenePaths));
                        };
                        result = AuditScope.RunScenes(scenes, budgets, false, build ? ScopeKind.BuildScenes : ScopeKind.Scenes, afterEach);
                        // One scene stays a plain report of the open scene, as before.
                        if (result != null && result.parts != null && result.parts.Count == 1 && !build) result = result.parts[0];
                    }
                    if (result == null) Debug.LogError("[SceneAudit] Audit returned no result.");
                    else
                    {
                        Save(result, report);
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

        // Every value of an argument that may repeat.
        static System.Collections.Generic.List<string> Arguments(string[] args, string name)
        {
            var values = new System.Collections.Generic.List<string>();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) values.Add(args[i + 1]);
            return values;
        }

        static string Argument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
