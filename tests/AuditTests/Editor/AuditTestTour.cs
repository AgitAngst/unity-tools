using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // The viewpoint tour on made-up frames: the sampler, the steps of a run, the worst direction with and without the
    // headset, moving a rig, freezing physics, the points file, the report and the history. A short tour in Play Mode
    // is started by TourStart and checked by TourCheck once Play Mode is over.
    public static partial class AuditTestViewpoints
    {
        const string TourScene = "Viewpoints-Tour";
        const string TourPlayScene = "Viewpoints-TourPlay";

        public static void RunTour()
        {
            Run("viewpoints-tour", Tour);
        }

        // Starts a tour of 3 viewpoints × 4 directions in Play Mode, moving a rig the scene builds on its 5th frame, and
        // answers at once: the server waits for Play Mode to end before the next command (TourCheck).
        public static void RunTourStart()
        {
            Run("viewpoints-tour-start", TourStart);
        }

        public static void RunTourCheck()
        {
            Run("viewpoints-tour-check", TourCheck);
        }

        static void Tour()
        {
            Sampler();
            Steps();
            Headroom();
            PlaceRig();
            Freeze();
            TourFiles();
        }

        static FrameSample Frame(float ms, float cpu, float gpu, int dropped = -1, int drawCalls = 100, bool headset = false)
        {
            return new FrameSample { dt = ms / 1000f, cpuMs = cpu, gpuMs = gpu, dropped = dropped, drawCalls = drawCalls, setPassCalls = drawCalls / 4, batches = drawCalls / 2, tris = drawCalls * 1000, headset = headset };
        }

        static bool Near(float a, float b, float eps = 0.01f)
        {
            return Mathf.Abs(a - b) <= eps;
        }

        static void Sampler()
        {
            var s = new FrameSampler();
            s.Add(Frame(10f, 4f, -1f, -1, 100));
            s.Add(Frame(12f, 5f, 8f, -1, 200));
            s.Add(Frame(14f, 6f, 10f, -1, 300));
            Check(s.frames == 3 && Near(s.AvgFps, 83.33f) && Near(s.MinFps, 71.43f) && Near(s.MaxFps, 100f), "sampler: 3 frames, FPS average 83.3, min 71.4, max 100");
            Check(Near(s.AvgFrameMs, 12f) && Near(s.PeakFrameMs, 14f) && Near(s.AvgCpuMs, 5f) && Near(s.PeakCpuMs, 6f), "sampler: frame 12 ms average, 14 ms peak, CPU 5 / 6 ms");
            Check(Near(s.AvgGpuMs, 9f) && Near(s.PeakGpuMs, 10f), "sampler: GPU averages only the frames that have it (9 ms)");
            Check(s.dropped == -1 && s.DrawCalls == 200 && s.SetPassCalls == 50 && s.Batches == 100 && s.Tris == 200000, "sampler: no dropped frames known, counters averaged");
            var snapshot = ProfilerCapture.SnapshotOf(s, "Assets/Demo.unity", new DateTime(2026, 10, 24, 15, 20, 0));
            Check(snapshot.frames == 3 && Near(snapshot.avgFPS, 83.33f) && Near(snapshot.minFPS, 71.43f) && Near(snapshot.avgCPUms, 5f) && Near(snapshot.peakGPUms, 10f)
                && snapshot.drawCalls == 200 && snapshot.timestamp == "2026-10-24 15:20:00", "profiler snapshot from the sampler keeps the numbers");
            var none = new FrameSampler();
            none.Add(Frame(11f, 4f, -1f));
            Check(none.AvgGpuMs < 0f && none.PeakGpuMs < 0f, "sampler: GPU -1 when no frame has it");
            var drops = new FrameSampler();
            drops.Add(Frame(11.1f, 4f, 9f, 0));
            drops.Add(Frame(22.2f, 4f, 12f, 2));
            drops.Add(Frame(11.1f, 4f, 9f, 1));
            Check(drops.dropped == 3, "sampler: dropped frames add up (3)");
        }

        static TourPlan PlanOfTen(int frames, int warmup)
        {
            var plan = new TourPlan { frames = frames, warmup = warmup, settle = 3, directions = 4 };
            for (int i = 0; i < 10; i++)
                plan.stops.Add(new TourStop { id = "p" + i, name = "Point " + i, position = new Vector3(i * 2f, 1.7f, 0f), yaws = new List<float> { 0f, 90f, 180f, 270f } });
            return plan;
        }

        // Frame time grows with the direction, so the last one (270°) is the worst without the headset.
        static TourStepKind Feed(TourRun run)
        {
            return run.Next(Frame(9f + run.yaw, 4f, 6f, -1, 100 * (run.yaw + 1)));
        }

        static void Steps()
        {
            var plan = PlanOfTen(5, 2);
            Check(plan.Steps == 40 && Near(plan.Seconds, (3 + 40 * 7) / 90f, 0.001f), "plan: 10 viewpoints × 4 directions, (settle + 40 × (2 + 5)) / 90 s");
            var run = new TourRun(plan);
            var moves = new List<string>();
            int frames = 0;
            TourStepKind step;
            do
            {
                step = Feed(run);
                if (step == TourStepKind.Move) moves.Add(run.CurrentStop.id + "@" + run.CurrentYaw);
                frames++;
            } while (step != TourStepKind.Done && frames < 10000);
            var expected = plan.stops.SelectMany(s => s.yaws.Select(y => s.id + "@" + y)).ToList();
            Check(step == TourStepKind.Done && moves.SequenceEqual(expected), "the run moves to every viewpoint and direction in order (" + moves.Count + " moves)");
            Check(frames == 3 + 40 * 8, "frames: settle 3, then per direction move + warm-up 2 + 5 measured (" + frames + ")");
            var result = run.Finish(false);
            Check(!result.interrupted && result.points.Count == 10 && result.points.All(p => p.directions.Count == 4 && p.directions.All(d => d.frames == 5)),
                "result: 10 viewpoints with 4 directions of 5 frames");
            Check(result.points.All(p => p.worst == 3 && Near(p.Worst.frameMs, 12f) && p.Worst.drawCalls == 400), "without the headset the worst direction is the longest frame (270°, 12 ms)");
            Check(!result.headset && result.frames == 5 && result.warmup == 2 && result.directions == 4 && result.time.Length > 0, "result keeps the parameters, time and \"without headset\"");

            // Interrupted after the 4th viewpoint, a little into the 5th: 4 measured viewpoints stay.
            var cut = new TourRun(PlanOfTen(5, 2));
            for (int i = 0; i < 10000 && cut.StepsDone < 16; i++) Feed(cut);
            for (int i = 0; i < 3; i++) Feed(cut);
            Check(cut.StepsDone == 16 && Near(cut.SecondsLeft, 24 * 7 / 90f, 0.001f), "after 4 viewpoints 24 directions are left (" + cut.SecondsLeft.ToString("F2") + " s)");
            var partial = cut.Finish(true);
            Check(partial.interrupted && partial.points.Count == 4 && partial.points.All(p => p.directions.Count == 4), "an interrupted tour keeps the 4 measured viewpoints, marked interrupted");

            // The run lives in SessionState as JSON over the domain reload of entering Play Mode.
            var fresh = new TourRun(PlanOfTen(5, 2));
            for (int i = 0; i < 50; i++) Feed(fresh);
            var loaded = new TourRun(new TourPlan());
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fresh), loaded);
            Check(loaded.plan.stops.Count == 10 && loaded.StepsDone == fresh.StepsDone && loaded.result.points.Count == fresh.result.points.Count, "the run reads back from JSON where it was");
            int left = 0;
            for (; left < 10000 && Feed(loaded) != TourStepKind.Done; left++) { }
            Check(loaded.IsDone && loaded.result.points.Count == 10, "the run read back goes on to the end");
        }

        static void Headroom()
        {
            var calm = new DirectionMeasure { yaw = 0f, frameMs = 11.1f, cpuMs = 4f, gpuMs = 6f, dropped = 0 };
            var busy = new DirectionMeasure { yaw = 90f, frameMs = 11.1f, cpuMs = 5f, gpuMs = 10.5f, dropped = 0 };
            Check(TourMath.WorstOf(new List<DirectionMeasure> { calm, busy }, true) == 1, "with the headset, both at 11.1 ms: GPU 10.5 ms is worse than GPU 6 ms");
            var dropping = new DirectionMeasure { yaw = 180f, frameMs = 11.1f, cpuMs = 3f, gpuMs = 6f, dropped = 2 };
            Check(TourMath.WorstOf(new List<DirectionMeasure> { calm, busy, dropping }, true) == 2, "with the headset dropped frames come first");
            var b = new VRBudgets();
            Check(!TourMath.IsSlow(busy, true, b) && TourMath.LevelOf(busy, true, b) == Severity.Warning, "GPU 10.5 ms is within 11.1 ms but over the 10 ms warning");
            Check(TourMath.IsSlow(dropping, true, b) && TourMath.LevelOf(dropping, true, b) == Severity.Critical, "dropping frames is slow");
            var slow = new DirectionMeasure { frameMs = 12.4f, cpuMs = 5f, gpuMs = -1f, dropped = -1 };
            Check(TourMath.IsSlow(slow, false, b) && TourMath.LevelOf(slow, false, b) == Severity.Critical, "without the headset 12.4 ms is slower than 11.1 ms");
            Check(TourMath.LevelOf(calm, true, b) == null && TourMath.LevelOf(null, true, b) == null, "GPU 6 ms with the headset is fine");
            var a = new DirectionMeasure { frameMs = 9f, cpuMs = 8f };
            var c = new DirectionMeasure { frameMs = 13f, cpuMs = 2f };
            Check(TourMath.WorstOf(new List<DirectionMeasure> { a, c }, false) == 1, "without the headset the frame time decides");
        }

        static void PlaceRig()
        {
            var root = new GameObject("Rig").transform;
            root.SetPositionAndRotation(new Vector3(5f, 0f, 5f), Quaternion.Euler(0f, 30f, 0f));
            var head = new GameObject("Head").transform;
            head.SetParent(root, false);
            head.localPosition = new Vector3(0.3f, 1.6f, 0f);
            head.localRotation = Quaternion.Euler(10f, 20f, 0f);
            var target = new Vector3(10f, 1.7f, -2f);
            TourMath.Place(root, head, target, 180f);
            Check((head.position - target).magnitude < 0.001f, "rig: the head 0.3 m off the root stands at the viewpoint (" + (head.position - target).magnitude.ToString("F4") + " m)");
            Check(Mathf.Abs(Mathf.DeltaAngle(head.eulerAngles.y, 180f)) < 0.05f, "rig: the head turned 20° by tracking faces 180° (" + head.eulerAngles.y.ToString("F2") + ")");
            Check(Mathf.Abs(Mathf.DeltaAngle(head.localEulerAngles.x, 10f)) < 0.05f && Mathf.Abs(root.eulerAngles.x) < 0.01f && Mathf.Abs(root.eulerAngles.z) < 0.01f,
                "rig: the root turns around the vertical only, the head keeps its pitch");
            TourMath.Place(root, head, new Vector3(-3f, 1.7f, 4f), 90f);
            Check((head.position - new Vector3(-3f, 1.7f, 4f)).magnitude < 0.001f && Mathf.Abs(Mathf.DeltaAngle(head.eulerAngles.y, 90f)) < 0.05f, "rig: the next viewpoint from there");
            UnityEngine.Object.DestroyImmediate(root.gameObject);

            var camera = new GameObject("Camera").transform;
            camera.rotation = Quaternion.Euler(25f, 70f, 5f);
            TourMath.Place(camera, camera, target, 270f);
            Check((camera.position - target).magnitude < 0.001f && Quaternion.Angle(camera.rotation, Quaternion.Euler(0f, 270f, 0f)) < 0.05f, "camera: at the viewpoint, level, facing 270°");
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }

        static void Freeze()
        {
            var player = new GameObject("Player");
            var body = player.AddComponent<Rigidbody>();
            body.isKinematic = false;
            var hand = new GameObject("Hand");
            hand.transform.SetParent(player.transform, false);
            var handBody = hand.AddComponent<Rigidbody>();
            handBody.isKinematic = true;
            var feet = new GameObject("Feet");
            feet.transform.SetParent(player.transform, false);
            var controller = feet.AddComponent<CharacterController>();
            controller.enabled = true;
            var frozen = TourMath.Freeze(player.transform);
            Check(body.isKinematic && handBody.isKinematic && !controller.enabled, "freeze: bodies kinematic, character controller off");
            TourMath.Restore(frozen);
            Check(!body.isKinematic && handBody.isKinematic && controller.enabled, "restore: every value as it was");
            TourMath.Restore(null);
            UnityEngine.Object.DestroyImmediate(player);
        }

        static TourMeasure MadeUpTour(ViewpointSet set, float slowMs, int slowCount, bool headset)
        {
            var plan = TourMath.PlanOf(set, set.points, 5, 2, 4, TourMover.Rig, "");
            var run = new TourRun(plan);
            for (int i = 0; i < 10000; i++)
            {
                int stop = run.stop;
                float ms = stop < slowCount ? slowMs : 8f + run.yaw * 0.2f;
                if (run.InWarmup && stop == 1) run.NoteDisplacement(0.8f);
                if (run.Next(Frame(ms, 4f, ms - 1f, headset ? 0 : -1, 120, headset)) == TourStepKind.Done) break;
            }
            return run.Finish(false);
        }

        static void TourFiles()
        {
            var path = NewScene(TourScene);
            Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(20f, 0.2f, 20f), AuditTestAssets.Plain, false);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var historyFile = AuditHistory.FileFor(new[] { path });
            if (File.Exists(historyFile)) File.Delete(historyFile);
            var set = ViewpointSet.Load(new[] { path });
            for (int i = 0; i < 6; i++) set.AddManual(new Vector3(i * 3f - 8f, 1.7f, 0f), 0f).hasYaw = false;
            var plan = TourMath.PlanOf(set, set.points, 30, 15, 4, TourMover.Object, "Player/Rig");
            Check(plan.stops.Count == 6 && plan.stops.All(s => s.yaws.SequenceEqual(new[] { 0f, 90f, 180f, 270f })) && plan.objectPath == "Player/Rig" && plan.mover == TourMover.Object,
                "plan of 4 directions: 0°, 90°, 180°, 270° and the picked object");
            var eight = TourMath.PlanOf(set, set.points.Take(1).ToList(), 30, 15, 8, TourMover.Rig, null);
            Check(eight.stops[0].yaws.Count == 8 && eight.objectPath == "", "plan of 8 directions");

            set.tour = MadeUpTour(set, 12.4f, 2, false);
            set.Save();
            var again = ViewpointSet.Load(new[] { path });
            Check(again.HasTour && again.tour.points.Count == 6 && !again.tour.headset && again.tour.points[1].displaced > 0.5f
                && again.tour.points.All(p => p.directions.Count == 4 && p.Worst != null), "the tour is kept in the points file and read back");
            UnityEngine.Object.DestroyImmediate(again);

            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var report = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, null));
            var t = report.viewpointTour;
            Check(report.hasViewpointTour && !report.hasViewpoints && t.points.Length == 6 && !t.headset && !t.interrupted && t.frames == 5 && t.directions == 4,
                "JSON: hasViewpointTour with 6 viewpoints, no estimate, without headset");
            Check(t.points.Count(p => p.slow) == 2 && Near(t.points[0].frameMs, 12.4f) && t.points[0].gpuMs > 0f && t.points[0].dropped == -1 && t.points[1].movedByGame
                && Near(t.frameTimeLimitMs, 11.1f), "JSON: 2 slow viewpoints at 12.4 ms, moved by the game, the 11.1 ms limit");
            var none = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, null, null));
            Check(!none.hasViewpointTour && !none.hasViewpoints, "JSON without viewpoints: hasViewpointTour false");
            var emptied = ViewpointSet.Load(new[] { path });
            emptied.points.Clear();
            var gone = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, null, emptied));
            Check(!gone.hasViewpointTour, "JSON: a tour whose viewpoints were all removed is no tour");
            UnityEngine.Object.DestroyImmediate(emptied);

            // A removed viewpoint leaves the report and the history.
            var removed = set.points[5].id;
            set.points.RemoveAt(5);
            Check(set.MeasuredPoints().Count == 5 && set.MeasuredPoints().All(m => m.id != removed), "measured points follow the set");
            set.Save();

            // History: faster after fixes.
            var first = AuditHistory.PointOf(result, null, null, "", DateTime.Now);
            Check(first.hasTour && first.tourCount == 5 && first.tourSlow == 2 && Near(first.tourWorstMs, 12.4f) && !first.tourHeadset, "history: the tour of 5 viewpoints, 2 slow, 12.4 ms worst");
            var same = AuditHistory.PointOf(result, null, first, "", DateTime.Now);
            Check(!same.hasTour, "history: the same tour is not stored twice");
            System.Threading.Thread.Sleep(20);
            set.tour = MadeUpTour(set, 10.6f, 1, false);
            set.Save();
            var faster = AuditHistory.PointOf(result, null, first, "LOD on the market", DateTime.Now);
            Check(faster.hasTour && faster.tourSlow == 0 && Near(faster.tourWorstMs, 10.6f) && faster.tourWorstMs < first.tourWorstMs, "history: faster after fixes (12.4 → 10.6 ms, 2 → 0 slow)");

            // Placing again replaces the automatic viewpoints and drops the tour.
            set.ReplaceAuto(new List<Viewpoint>(), 4f);
            Check(!set.HasTour, "placing viewpoints again drops the old tour");
            UnityEngine.Object.DestroyImmediate(set);
        }

        // ---- Play Mode ----

        static void TourStart()
        {
            var path = NewScene(TourPlayScene);
            Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 30f), AuditTestAssets.Plain, true);
            for (int i = 0; i < 12; i++) Box("Crate " + i, new Vector3(i * 2f - 11f, 0.5f, 6f), Vector3.one, AuditTestAssets.Plain, false);
            new GameObject("Light").AddComponent<Light>().type = LightType.Directional;
            // No camera at the start: the rig appears only on the 5th frame of Play Mode.
            new GameObject("Spawner").AddComponent<RigLater>();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var set = ViewpointSet.Load(new[] { path });
            for (int i = 0; i < 3; i++) set.AddManual(new Vector3(i * 4f - 4f, 1.7f, 0f), 0f).hasYaw = false;
            set.Save();
            var plan = TourMath.PlanOf(set, set.points, 5, 2, 4, TourMover.Rig, "");
            UnityEngine.Object.DestroyImmediate(set);
            Check(!ViewpointTour.IsRunning && !EditorApplication.isPlaying, "no tour and no Play Mode before the start");
            ViewpointTour.Start(plan);
            Check(ViewpointTour.IsRunning && ViewpointTour.Entering, "the tour waits for Play Mode");
        }

        static void TourCheck()
        {
            var path = SceneDir + "/" + TourPlayScene + ".unity";
            Check(!ViewpointTour.IsRunning && !EditorApplication.isPlaying, "the tour is over and the editor is back in Edit Mode");
            var set = ViewpointSet.Load(new[] { path });
            var tour = set.tour;
            Check(set.HasTour && !tour.interrupted && tour.points.Count == 3, "the tour measured 3 viewpoints (" + tour.points.Count + ", interrupted " + tour.interrupted + ")");
            Check(tour.points.All(p => p.directions.Count == 4 && p.directions.All(d => d.frames == 5 && d.frameMs > 0f && d.drawCalls >= 0)),
                "every viewpoint has 4 directions of 5 frames with a frame time");
            Check(!tour.headset && tour.mover == TourMover.Rig, "measured without a headset, the VR rig moved");
            Check(tour.points.All(p => p.displaced < 0.01f), "the rig built on the 5th frame was found after the settle frames: the head stood at every viewpoint ("
                + string.Join(", ", tour.points.Select(p => p.displaced.ToString("F3")).ToArray()) + " m)");
            Check(GameObject.Find("Spawned Rig") == null, "the rig of Play Mode is gone in Edit Mode");
            log.AppendLine("info: worst " + string.Join(", ", tour.points.Select(p => p.name + " " + p.Worst.frameMs.ToString("F2") + " ms, " + p.Worst.drawCalls + " dc").ToArray()));
            UnityEngine.Object.DestroyImmediate(set);
        }
    }
}
