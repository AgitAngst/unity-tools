using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Viewpoints and target GPUs: small scenes built for each case, numbers checked against what the estimate must give.
    public static partial class AuditTestViewpoints
    {
        const string ReportDir = @"E:\dev_personal\unity-tools-scratch\reports\audit-tests";
        const string SceneDir = AuditTestAssets.Root + "/Scenes";

        static List<string> failures;
        static StringBuilder log;

        public static void RunViewpoints()
        {
            Run("viewpoints", Viewpoints);
        }

        public static void RunTeleports()
        {
            Run("viewpoints-teleports", Teleports);
        }

        public static void RunGpu()
        {
            Run("gpu", Gpu);
        }

        static void Run(string name, Action body)
        {
            failures = new List<string>();
            log = new StringBuilder();
            int code;
            try
            {
                AuditTestAssets.Prepare();
                AuditTestSettings.ApplyBaseline();
                body();
                code = failures.Count == 0 ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[AuditTests] " + name + " crashed: " + e);
                log.AppendLine("CRASH\n" + e);
                code = 2;
            }
            var text = name + ": " + (code == 0 ? "OK" : code == 2 ? "CRASH" : failures.Count + " FAILED") + "\n" + log;
            Directory.CreateDirectory(ReportDir);
            File.WriteAllText(Path.Combine(ReportDir, name + ".txt"), text, new UTF8Encoding(false));
            Debug.Log("[AuditTests] " + name + ": " + (code == 0 ? "OK" : code == 2 ? "CRASH" : failures.Count + " FAILED"));
            AuditTestServer.Exit(code);
        }

        static void Check(bool ok, string what)
        {
            log.AppendLine((ok ? "PASS " : "FAIL ") + what);
            if (!ok) failures.Add(what);
        }

        // ---- viewpoints ----

        static void Viewpoints()
        {
            Pure();
            NavMeshScene();
            RaysScene();
            CountsScene();
            LodScene();
            TransparentScene();
            WallsScene();
            PrefabScene();
        }

        static void Pure()
        {
            Check(ViewpointEstimator.LodLevel(0.3f, new[] { 0.5f, 0.2f, 0.05f }, 1f, 0) == 1, "screen height 0.3 with transitions 0.5/0.2/0.05 draws LOD1");
            Check(ViewpointEstimator.LodLevel(0.6f, new[] { 0.5f, 0.2f, 0.05f }, 1f, 0) == 0, "screen height 0.6 draws LOD0");
            Check(ViewpointEstimator.LodLevel(0.01f, new[] { 0.5f, 0.2f, 0.05f }, 1f, 0) == -1, "below the last transition the group is culled");
            Check(ViewpointEstimator.LodLevel(0.6f, new[] { 0.5f, 0.2f, 0.05f }, 1f, 1) == 1, "maximum LOD level 1 skips LOD0");
            Check(ViewpointEstimator.LodLevel(0.3f, new[] { 0.5f, 0.2f, 0.05f }, 2f, 0) == 0, "LOD bias 2 doubles the screen height");
            float h = ViewpointEstimator.RelativeHeight(2f, 10f, 90f);
            Check(Mathf.Abs(h - 0.1f) < 0.001f, "2 m at 10 m with 90° field of view is 0.1 of the screen (" + h + ")");
            var b = new VRBudgets();
            Check(ViewpointEstimator.LevelOf(new ViewDirection { drawCalls = 1450 }, b) == Severity.Warning, "1 450 draw calls over the 1 000 threshold is a warning");
            Check(ViewpointEstimator.LevelOf(new ViewDirection { transparentPercent = 250f }, b) == Severity.Critical, "250 % transparent is critical");
            Check(ViewpointEstimator.LevelOf(new ViewDirection { tris = 100, drawCalls = 10 }, b) == null, "small numbers are within budget");
            var dirs = new List<ViewDirection> { new ViewDirection { drawCalls = 5, tris = 10 }, new ViewDirection { drawCalls = 9, tris = 1 }, new ViewDirection { drawCalls = 9, tris = 5 } };
            Check(ViewpointEstimator.WorstOf(dirs) == 2, "worst direction: most draw calls, then most triangles");
        }

        static void NavMeshScene()
        {
            var path = NewScene("Viewpoints-NavMesh");
            var floor = Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(20f, 0.2f, 20f), AuditTestAssets.Plain, false);
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            DeleteViewpointFile(path);

            var survey = ViewpointPlacer.Look();
            Check(survey.navMesh, "the baked NavMesh is found");
            var set = ViewpointSet.Load(new[] { path });
            Check(set.points.Count == 0, "no viewpoint file at first");
            var manual = set.AddManual(new Vector3(3f, 1.7f, 3f), 30f);

            float step;
            var settings = new PlacementSettings { navMesh = true, teleports = false, cameras = false, rays = false, step = 4f };
            var auto = ViewpointPlacer.Place(settings, out step);
            log.AppendLine("info: NavMesh viewpoints " + auto.Count + ", step " + step);
            Check(auto.Count >= 16 && auto.Count <= 36, "a 20 × 20 m floor with a 4 m step gives about 25 viewpoints (" + auto.Count + ")");
            Check(auto.All(p => p.kind == ViewpointKind.NavMesh && Mathf.Abs(p.position.y - 1.7f) < 0.3f), "NavMesh viewpoints stand 1.7 m above the floor");
            set.ReplaceAuto(auto, step);
            Check(set.Find(manual.id) != null && set.points.Count == auto.Count + 1, "placing again keeps the manual viewpoint");
            var moved = set.points.First(p => p.IsAuto);
            set.MakeManual(moved);
            Check(moved.kind == ViewpointKind.Manual && moved.number == 2, "a moved automatic viewpoint becomes manual (Point 2)");
            set.ReplaceAuto(ViewpointPlacer.Place(settings, out step), step);
            Check(set.Find(moved.id) != null && set.Find(manual.id) != null, "placing again keeps both manual viewpoints");

            bool dirtyBefore = EditorSceneManager.GetActiveScene().isDirty;
            Check(!dirtyBefore, "the saved scene is clean before the estimate");
            Check(!ViewpointEstimator.Estimate(set, set.points, false, (n, s) => true) && set.estimatedAt.Length == 0 && !set.points.Any(p => p.Estimated),
                "a cancelled estimate keeps the set as it was");
            Check(ViewpointEstimator.Estimate(set, set.points, false, null) && set.HasEstimate && set.points.All(p => p.Estimated), "every viewpoint is estimated");
            Check(set.points.Where(p => !p.hasYaw).All(p => p.directions.Count == 8) && set.Find(manual.id).directions.Count == 9,
                "8 directions, plus the own one of a manual viewpoint");
            set.Save();
            Check(!EditorSceneManager.GetActiveScene().isDirty, "placing, estimating and saving viewpoints do not change the scene");

            var again = ViewpointSet.Load(new[] { path });
            Check(again.points.Count == set.points.Count && again.points.Zip(set.points, (a, b) => a.id == b.id && a.kind == b.kind && (a.position - b.position).sqrMagnitude < 1e-6f).All(x => x)
                && again.HasEstimate && Math.Abs(again.placedStep - 4f) < 0.001f, "the viewpoint file is read back as written");
            Check(File.Exists(Path.Combine(ViewpointSet.Folder, AuditHistory.FileNameFor(new[] { path }))), "the file is SceneAuditPoints/<scene>-<hash>.json");
            UnityEngine.Object.DestroyImmediate(again);

            int count = set.points.Count;
            Undo.IncrementCurrentGroup();
            Undo.RecordObject(set, "Remove Viewpoints");
            set.points.RemoveAll(p => p.id == manual.id);
            Undo.IncrementCurrentGroup();
            Undo.PerformUndo();
            Check(set.points.Count == count && set.Find(manual.id) != null, "Undo brings a removed viewpoint back");

            var csv = ViewpointCsv.ToCsv(set, new VRBudgets());
            var lines = csv.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            Check(lines[0] == ViewpointCsv.Header && lines.Length == set.points.Count + 1, "CSV: header and one row per viewpoint");
            var csvPath = Path.Combine(ReportDir, "viewpoints.csv");
            ViewpointCsv.Save(set, new VRBudgets(), csvPath);
            var bytes = File.ReadAllBytes(csvPath);
            Check(bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "CSV is UTF-8 with BOM");

            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var withPoints = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, null, set));
            Check(withPoints.hasViewpoints && withPoints.viewpoints.points.Length == set.points.Count && withPoints.targetGpu == "RTX2070",
                "JSON: hasViewpoints, every viewpoint and targetGpu RTX2070");
            var without = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, null, null));
            Check(!without.hasViewpoints, "JSON without an estimate: hasViewpoints false");

            // History: the estimate goes into a new point once.
            var first = AuditHistory.PointOf(result, null, null, "", DateTime.Now);
            Check(first.hasViewpoints && first.vpCount == set.points.Count && first.vpDrawCalls > 0 && first.vpTime == set.estimatedAt, "a history point stores the worst viewpoint");
            var second = AuditHistory.PointOf(result, null, first, "", DateTime.Now);
            Check(!second.hasViewpoints, "the same estimate is not stored twice");
            UnityEngine.Object.DestroyImmediate(set);

            // Command line: -auditViewpoints places viewpoints when there is no file, estimates them, writes report and history.
            DeleteViewpointFile(path);
            var historyFile = AuditHistory.FileFor(new[] { path });
            if (File.Exists(historyFile)) File.Delete(historyFile);
            var report = Path.Combine(ReportDir, "cli-viewpoints.json");
            int code = ReportExporter.RunWithArgs(new[] { "Unity.exe", "-auditScene", path, "-auditReport", report, "-auditViewpoints", "-auditHistory" });
            var cli = JsonUtility.FromJson<ReportExporter.ReportDto>(File.ReadAllText(report));
            Check(code == 0 && cli.hasViewpoints && cli.viewpoints.points.Length >= 16, "-auditViewpoints: placed, estimated and written to the report (" + cli.viewpoints.points.Length + ")");
            var history = AuditHistory.Load(historyFile);
            Check(history.points.Count == 1 && history.points[0].hasViewpoints, "-auditViewpoints with -auditHistory: the point stores the viewpoints");
        }

        static void RaysScene()
        {
            var path = NewScene("Viewpoints-Rays");
            Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(20f, 0.2f, 20f), AuditTestAssets.Plain, true);
            Box("Slope", new Vector3(30f, 5f, 0f), new Vector3(0.2f, 10f, 10f), AuditTestAssets.Plain, true);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var survey = ViewpointPlacer.Look();
            Check(!survey.navMesh && !survey.HasTeleports, "a scene without NavMesh and teleports");
            float step;
            var points = ViewpointPlacer.Place(new PlacementSettings { navMesh = true, teleports = true, rays = true, step = 4f }, out step);
            Check(points.Count >= 16 && points.All(p => p.kind == ViewpointKind.Ray && Mathf.Abs(p.position.y - 1.7f) < 0.05f),
                "rays down put viewpoints on the floor, not on the wall (" + points.Count + ")");
            var none = ViewpointPlacer.Place(new PlacementSettings { navMesh = true, teleports = true, rays = false, step = 4f }, out step);
            Check(none.Count == 0, "without the rays source nothing is placed");

            Box("Big", new Vector3(0f, -0.1f, 200f), new Vector3(100f, 0.2f, 100f), AuditTestAssets.Plain, true);
            var many = ViewpointPlacer.Place(new PlacementSettings { rays = true, step = 1f }, out step);
            Check(many.Count <= VRBudgets.MaxViewpoints && step > 1f, "at most 500 viewpoints: the step grows (" + many.Count + " at " + step.ToString("F2") + " m)");
        }

        // A cube in front, a 12 000-triangle grid behind, one directional light with shadows.
        static void CountsScene()
        {
            NewScene("Viewpoints-Counts");
            var front = Box("Front", new Vector3(0f, 1.7f, 5f), Vector3.one, AuditTestAssets.Plain, false);
            front.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            var back = MeshObject("Back", AuditTestAssets.Grid12k, new Vector3(0f, 1.7f, -5f), AuditTestAssets.Plain);
            back.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            sun.shadows = LightShadows.Soft;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            var set = Estimated(new Vector3(0f, 1.7f, 0f));
            var p = set.points[0];
            int factor = XrInfo.Detect().DoublesDrawCalls ? 2 : 1;
            int cascades = Mathf.Max(1, QualitySettings.shadowCascades);
            log.AppendLine("info: directions " + string.Join(" | ", p.directions.Select(d => d.yaw + "°: " + d.tris + " tris, " + d.drawCalls + " dc").ToArray()));
            Check(p.directions[0].tris == 12 && p.directions[4].tris == 12000, "the grid behind is not seen looking forward, the cube in front is not seen looking back");
            Check(p.directions[0].drawCalls == (1 + cascades) * factor, "one material and a directional shadow with " + cascades + " cascades: " + (1 + cascades) * factor + " draw calls");
            Check(p.directions[4].drawCalls == (1 + cascades) * factor, "looking back: the grid plus the shadow of the cube");
            UnityEngine.Object.DestroyImmediate(set);
        }

        // A LOD group whose screen height at the viewpoint is 0.3: LOD1 is drawn.
        static void LodScene()
        {
            NewScene("Viewpoints-Lod");
            var root = new GameObject("Lod");
            var l0 = MeshObject("L0", AuditTestAssets.Grid30k, Vector3.zero, AuditTestAssets.Plain);
            var l1 = MeshObject("L1", AuditTestAssets.Grid12k, Vector3.zero, AuditTestAssets.Plain);
            var l2 = Box("L2", Vector3.zero, Vector3.one, AuditTestAssets.Plain, false);
            foreach (var go in new[] { l0, l1, l2 }) go.transform.SetParent(root.transform, false);
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(new[]
            {
                new LOD(0.5f, new Renderer[] { l0.GetComponent<Renderer>() }),
                new LOD(0.2f, new Renderer[] { l1.GetComponent<Renderer>() }),
                new LOD(0.05f, new Renderer[] { l2.GetComponent<Renderer>() })
            });
            group.size = 2f;
            float bias = QualitySnapshot.Capture().lodBias;
            float distance = group.size * bias / (0.3f * 2f * Mathf.Tan(VRBudgets.ViewFieldOfView * 0.5f * Mathf.Deg2Rad));
            root.transform.position = new Vector3(0f, 1.7f, distance) - group.localReferencePoint;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            var set = Estimated(new Vector3(0f, 1.7f, 0f));
            log.AppendLine("info: LOD bias " + bias + ", distance " + distance.ToString("F2") + " m, tris forward " + set.points[0].directions[0].tris);
            Check(set.points[0].directions[0].tris == 12000, "only the LOD1 renderer is counted at screen height 0.3");
            UnityEngine.Object.DestroyImmediate(set);
        }

        static void TransparentScene()
        {
            NewScene("Viewpoints-Transparent");
            Box("Glass", new Vector3(0f, 1.7f, 1.5f), new Vector3(2f, 2f, 0.1f), AuditTestAssets.Fade, false);
            Box("Wall", new Vector3(0f, 1.7f, -1.5f), new Vector3(2f, 2f, 0.1f), AuditTestAssets.Plain, false);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var set = Estimated(new Vector3(0f, 1.7f, 0f));
            var p = set.points[0];
            log.AppendLine("info: transparent forward " + p.directions[0].transparentPercent + " %, back " + p.directions[4].transparentPercent + " %");
            // 2 × 2 m at 1.45–1.55 m with 110° vertical and 0.9 aspect: about 0.47 of the height × 0.52 of the width.
            Check(p.directions[0].transparentPercent > 22f && p.directions[0].transparentPercent < 30f,
                "a transparent 2 × 2 m pane 1.5 m ahead covers about a quarter of the screen");
            Check(p.directions[4].transparentPercent == 0f, "an opaque pane adds no transparent share");
            UnityEngine.Object.DestroyImmediate(set);
        }

        static void WallsScene()
        {
            NewScene("Viewpoints-Walls");
            Box("Wall", new Vector3(0f, 1.7f, 3f), new Vector3(10f, 6f, 0.3f), AuditTestAssets.Plain, true);
            MeshObject("Hidden", AuditTestAssets.Grid12k, new Vector3(0f, 1.7f, 8f), AuditTestAssets.Plain);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var open = Estimated(new Vector3(0f, 1.7f, 0f));
            var walled = Estimated(new Vector3(0f, 1.7f, 0f), true);
            log.AppendLine("info: forward without walls " + open.points[0].directions[0].tris + ", with walls " + walled.points[0].directions[0].tris);
            Check(open.points[0].directions[0].tris == 12012, "without walls the grid behind the wall is counted");
            Check(walled.points[0].directions[0].tris == 12, "with walls the grid behind the wall collider is hidden");
            UnityEngine.Object.DestroyImmediate(open);
            UnityEngine.Object.DestroyImmediate(walled);
        }

        // 40 copies of one prefab without a LOD group in front of the viewpoint: one row of "What is heavy here".
        static void PrefabScene()
        {
            NewScene("Viewpoints-Prefab");
            var source = MeshObject("Heavy", AuditTestAssets.Grid12k, Vector3.zero, AuditTestAssets.Plain);
            var prefabPath = AuditTestAssets.Root + "/HeavyProp.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
            UnityEngine.Object.DestroyImmediate(source);
            for (int i = 0; i < 40; i++)
            {
                var copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                copy.transform.position = new Vector3(-4f + (i % 8) * 1.2f, 1.7f, 10f + (i / 8) * 2f);
                copy.transform.localScale = Vector3.one * 0.5f;
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var set = Estimated(new Vector3(0f, 1.7f, 0f));
            var heavy = ViewpointEstimator.Heavy(set.points[0], 0, new VRBudgets(), false, false);
            var entry = heavy.FirstOrDefault(e => e.key == prefabPath);
            log.AppendLine("info: heavy " + string.Join(" | ", heavy.Select(e => e.label + " ×" + e.copies + " " + e.tris + " [" + string.Join(",", e.marks.ToArray()) + "]").ToArray()));
            Check(entry != null && entry.copies == 40 && entry.tris == 40 * 12000L && entry.marks.Contains("nolod"),
                "40 copies of the prefab are one row with 40 copies and the mark \"no LOD group\"");
            Check(heavy.Count == 1, "nothing else is seen");
            UnityEngine.Object.DestroyImmediate(set);
        }

        // ---- teleports (SteamVR stub types) ----

        static void Teleports()
        {
            var pointType = FindType(ViewpointPlacer.TeleportPointType);
            var areaType = FindType(ViewpointPlacer.TeleportAreaType);
            Check(pointType != null && areaType != null, "SteamVR teleport stub types are compiled");
            if (pointType == null || areaType == null) return;
            NewScene("Viewpoints-Teleports");
            foreach (var x in new[] { -20f, 20f })
            {
                var go = new GameObject("TP_" + (x < 0 ? "West" : "East"));
                go.transform.position = new Vector3(x, 0f, 0f);
                go.AddComponent(pointType);
            }
            var area = Box("Area", new Vector3(0f, -0.05f, 0f), new Vector3(9f, 0.1f, 9f), AuditTestAssets.Plain, true);
            area.AddComponent(areaType);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var survey = ViewpointPlacer.Look();
            Check(survey.teleportPoints == 2 && survey.teleportAreas == 1, "2 teleport points and 1 teleport area are found");
            float step;
            var points = ViewpointPlacer.Place(new PlacementSettings { navMesh = false, teleports = true, rays = true, step = 4f }, out step);
            log.AppendLine("info: teleport viewpoints " + string.Join(" | ", points.Select(p => p.Name + " " + p.position).ToArray()));
            Check(points.Count(p => p.objectName == "TP_West" || p.objectName == "TP_East") == 2, "a viewpoint at each teleport point, named after it");
            Check(points.Count(p => p.objectName == "Area") == 9, "a 4 m grid over the 9 × 9 m teleport area gives 9 viewpoints");
            Check(points.All(p => p.kind == ViewpointKind.Teleport), "rays are not used when there are teleports");
        }

        // ---- target GPU ----

        static void Gpu()
        {
            var file = VRBudgets.FilePath;
            var saved = File.Exists(file) ? File.ReadAllText(file) : null;
            try
            {
                if (File.Exists(file)) File.Delete(file);
                var b = VRBudgets.Load();
                Check(b.targetGpu == "RTX2070" && b.sceneTrisWarning == 3000000 && b.sceneTrisCritical == 6000000 &&
                      b.textureMemoryWarningMB == 1536 && b.textureMemoryCriticalMB == 2048 && !b.IsCustom, "without a budget file: RTX 2070 and its thresholds");
                Check(b.transparentWarningPercent == 100 && b.transparentCriticalPercent == 200, "transparent in frame: 100 / 200 %");
                Check(b.drawCallsWarning == 1000 && b.frameTimeCriticalMs == 11.1f, "other thresholds keep their defaults");

                b.ApplyGpu("RTX2060");
                b.Save();
                var again = VRBudgets.Load();
                Check(again.targetGpu == "RTX2060" && again.textureMemoryWarningMB == 1152 && again.sceneTrisWarning == 2500000 && !again.IsCustom,
                    "RTX 2060 saved: texture memory 1152 MB, triangles 2.5 million");
                Check(File.ReadAllText(file).Contains("\"targetGpu\": \"RTX2060\""), "the budget file stores targetGpu");
                again.textureMemoryWarningMB = 1000;
                Check(again.IsCustom && again.GpuReportName == "Custom", "an edited threshold makes the set custom");
                Check(VRBudgets.DefaultsFor("RTX2060").textureMemoryWarningMB == 1152, "Defaults bring the values of the chosen GPU back");
                foreach (var gpu in VRBudgets.Gpus) log.AppendLine("info: " + gpu.id + " " + gpu.trisWarning + "/" + gpu.trisCritical + " tris, " + gpu.textureMemoryWarningMB + "/" + gpu.textureMemoryCriticalMB + " MB");
                Check(VRBudgets.Gpus.Select(g => g.id).SequenceEqual(new[] { "RTX2060", "RTX2070", "RTX2080", "RTX3060", "RTX4060" }), "GPUs to pick: RTX 2060, 2070, 2080, 3060, 4060");
                var rtx4060 = VRBudgets.DefaultsFor("RTX4060");
                Check(rtx4060.targetGpu == "RTX4060" && rtx4060.sceneTrisWarning == 3500000 && rtx4060.sceneTrisCritical == 7000000 &&
                      rtx4060.textureMemoryWarningMB == 1536 && rtx4060.textureMemoryCriticalMB == 2048 && !rtx4060.IsCustom,
                    "RTX 4060: 3.5 / 7 million triangles, 1536 / 2048 MB of textures");

                File.WriteAllText(file, "{ \"drawCallsWarning\": 1000, \"sceneTrisWarning\": 2000000, \"textureMemoryWarningMB\": 1536, \"textureMemoryCriticalMB\": 2048 }");
                var old = VRBudgets.Load();
                Check(old.textureMemoryWarningMB == 1536 && old.sceneTrisWarning == 2000000 && old.targetGpu == "" && old.IsCustom && old.transparentWarningPercent == 100,
                    "a budget file from an older version keeps its values, shows them as custom, transparent gets its default");
            }
            finally
            {
                if (saved != null) File.WriteAllText(file, saved);
                else if (File.Exists(file)) File.Delete(file);
            }
        }

        // ---- helpers ----

        static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }

        static string NewScene(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var path = SceneDir + "/" + name + ".unity";
            Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, path);
            DeleteViewpointFile(path);
            return path;
        }

        static void DeleteViewpointFile(string scenePath)
        {
            var file = ViewpointSet.FileFor(new[] { scenePath });
            if (File.Exists(file)) File.Delete(file);
        }

        static GameObject Box(string name, Vector3 position, Vector3 size, Material material, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject MeshObject(string name, Mesh mesh, Vector3 position, Material material)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // One manual viewpoint at the eye looking along +Z, estimated.
        static ViewpointSet Estimated(Vector3 eye, bool walls = false)
        {
            var set = ViewpointSet.Load(new[] { EditorSceneManager.GetActiveScene().path });
            set.points.Clear();
            var p = set.AddManual(eye, 0f);
            p.hasYaw = false;
            ViewpointEstimator.Estimate(set, set.points, walls, null);
            return set;
        }
    }
}
