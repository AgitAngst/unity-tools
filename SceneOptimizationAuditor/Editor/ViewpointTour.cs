using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public enum TourMover { Rig = 0, Object = 1, Camera = 2 }

    // One measured direction of a viewpoint. Times in milliseconds; GPU and dropped frames -1 when unknown.
    [Serializable]
    public sealed class DirectionMeasure
    {
        public float yaw;
        public int frames;
        public float frameMs;
        public float peakFrameMs;
        public float cpuMs;
        public float gpuMs = -1f;
        public int dropped = -1;
        public int drawCalls;
        public int batches;
        public int setPassCalls;
        public long tris;
    }

    [Serializable]
    public sealed class PointMeasure
    {
        public string id = "";
        public string name = "";
        public List<DirectionMeasure> directions = new List<DirectionMeasure>();
        public int worst = -1;
        // How far the game pulled the head away from the viewpoint during the warm-up, metres (largest of the directions).
        public float displaced;

        public DirectionMeasure Worst { get { return worst >= 0 && worst < directions.Count ? directions[worst] : null; } }
    }

    // The last tour of a viewpoint set, kept in its file.
    [Serializable]
    public sealed class TourMeasure
    {
        public string time = "";
        public string toolVersion = "";
        public bool interrupted;
        public bool headset;
        public int frames;
        public int warmup;
        public int directions;
        public TourMover mover;
        public string objectPath = "";
        public float seconds;
        public List<PointMeasure> points = new List<PointMeasure>();

        public bool Exists { get { return time.Length > 0 && points.Count > 0; } }

        public PointMeasure Find(string id) { return points.FirstOrDefault(p => p.id == id); }
    }

    [Serializable]
    public sealed class TourStop
    {
        public string id = "";
        public string name = "";
        public Vector3 position;
        public List<float> yaws = new List<float>();
    }

    [Serializable]
    public sealed class TourPlan
    {
        public string[] scenes = new string[0];
        public List<TourStop> stops = new List<TourStop>();
        public int frames = 30;
        public int warmup = 15;
        public int settle = 30;
        public int directions = 8;
        public TourMover mover;
        public string objectPath = "";
        public bool startedInPlay;
        // Frames a second the tour is expected to run at: the refresh rate of the headset.
        public int fps = VRBudgets.TargetFps;

        public int Steps { get { return stops.Sum(s => s.yaws.Count); } }

        // Seconds the tour takes at the refresh rate of the headset.
        public float Seconds { get { return (settle + Steps * (warmup + frames)) / (float)fps; } }
    }

    public enum TourStepKind { Wait, Move, Done }

    // The tour without the editor: which viewpoint and direction comes next, frame by frame, and what was measured.
    // The editor driver (ViewpointTour) feeds frames in and moves the viewer when asked to.
    [Serializable]
    public sealed class TourRun
    {
        enum Phase { Settle = 0, Move = 1, Warmup = 2, Collect = 3, Done = 4 }

        public TourPlan plan = new TourPlan();
        public TourMeasure result = new TourMeasure();
        [SerializeField] Phase phase;
        [SerializeField] int counter;
        public int stop;
        public int yaw;
        [SerializeField] FrameSampler sampler = new FrameSampler();
        [SerializeField] float displaced;
        [SerializeField] bool headset;
        public string startedAt = "";

        public TourRun(TourPlan plan)
        {
            this.plan = plan;
            result.frames = plan.frames;
            result.warmup = plan.warmup;
            result.directions = plan.directions;
            result.mover = plan.mover;
            result.objectPath = plan.objectPath;
            startedAt = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);
            phase = plan.stops.Count > 0 ? Phase.Settle : Phase.Done;
        }

        public bool IsDone { get { return phase == Phase.Done; } }

        public TourStop CurrentStop { get { return stop < plan.stops.Count ? plan.stops[stop] : null; } }

        public float CurrentYaw { get { var s = CurrentStop; return s != null && yaw < s.yaws.Count ? s.yaws[yaw] : 0f; } }

        public bool InWarmup { get { return phase == Phase.Warmup; } }

        // The first frames of Play Mode: the game may still be building its rig and player.
        public bool Settling { get { return phase == Phase.Settle; } }

        // The next frame is the last measured one of the direction.
        public bool LastFrameNext { get { return phase == Phase.Collect && counter == plan.frames - 1; } }

        // Steps measured so far, out of plan.Steps.
        public int StepsDone { get { return plan.stops.Take(stop).Sum(s => s.yaws.Count) + yaw; } }

        public float SecondsLeft
        {
            get
            {
                int left = plan.Steps - StepsDone;
                return left * (plan.warmup + plan.frames) / (float)plan.fps;
            }
        }

        // Advances one frame. Move: put the viewer at CurrentStop looking along CurrentYaw before the next frame.
        public TourStepKind Next(FrameSample sample)
        {
            switch (phase)
            {
                case Phase.Settle:
                    if (++counter >= plan.settle) phase = Phase.Move;
                    return TourStepKind.Wait;
                case Phase.Move:
                    phase = plan.warmup > 0 ? Phase.Warmup : Phase.Collect;
                    counter = 0;
                    sampler = new FrameSampler();
                    displaced = 0f;
                    return TourStepKind.Move;
                case Phase.Warmup:
                    if (++counter >= plan.warmup)
                    {
                        phase = Phase.Collect;
                        counter = 0;
                    }
                    return TourStepKind.Wait;
                case Phase.Collect:
                    sampler.Add(sample);
                    headset |= sample.headset;
                    if (++counter < plan.frames) return TourStepKind.Wait;
                    Record();
                    return phase == Phase.Done ? TourStepKind.Done : TourStepKind.Wait;
                default:
                    return TourStepKind.Done;
            }
        }

        // The head was found this far from the viewpoint at the end of the warm-up: the game moved it.
        public void NoteDisplacement(float metres)
        {
            displaced = Mathf.Max(displaced, metres);
        }

        void Record()
        {
            var s = CurrentStop;
            var point = result.Find(s.id);
            if (point == null)
            {
                point = new PointMeasure { id = s.id, name = s.name };
                result.points.Add(point);
            }
            point.directions.Add(new DirectionMeasure
            {
                yaw = CurrentYaw,
                frames = sampler.frames,
                frameMs = sampler.AvgFrameMs,
                peakFrameMs = sampler.PeakFrameMs,
                cpuMs = sampler.AvgCpuMs,
                gpuMs = sampler.AvgGpuMs,
                dropped = sampler.dropped,
                drawCalls = sampler.DrawCalls,
                batches = sampler.Batches,
                setPassCalls = sampler.SetPassCalls,
                tris = sampler.Tris
            });
            point.displaced = Mathf.Max(point.displaced, displaced);
            point.worst = TourMath.WorstOf(point.directions, headset);
            result.headset = headset;
            if (++yaw >= s.yaws.Count)
            {
                yaw = 0;
                stop++;
            }
            phase = stop >= plan.stops.Count ? Phase.Done : Phase.Move;
        }

        // Ends the tour: the measured viewpoints stay, the result is stamped.
        public TourMeasure Finish(bool interrupted)
        {
            result.interrupted = interrupted || phase != Phase.Done;
            result.time = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);
            result.toolVersion = ToolVersion.Full;
            DateTime started;
            if (DateTime.TryParse(startedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out started))
                result.seconds = (float)Math.Round((DateTime.Now - started).TotalSeconds, 1);
            phase = Phase.Done;
            return result;
        }
    }

    // Arithmetic of the tour that does not need Play Mode.
    public static class TourMath
    {
        // A head farther than this from the viewpoint at the end of the warm-up: the game moved the player.
        public const float MovedMetres = 0.5f;

        // Worst direction. With the headset the frame is paced to its refresh rate, so frame time shows no headroom: dropped frames
        // first, then the larger of CPU and GPU time. Without it — frame time.
        public static int WorstOf(List<DirectionMeasure> directions, bool headset)
        {
            int worst = -1;
            for (int i = 0; i < directions.Count; i++)
            {
                if (worst < 0 || Worse(directions[i], directions[worst], headset)) worst = i;
            }
            return worst;
        }

        static bool Worse(DirectionMeasure a, DirectionMeasure b, bool headset)
        {
            if (!headset) return a.frameMs > b.frameMs;
            if (a.dropped != b.dropped) return a.dropped > b.dropped;
            return Cost(a, true) > Cost(b, true);
        }

        // Time the direction costs: with the headset the larger of CPU and GPU, else frame time.
        public static float Cost(DirectionMeasure d, bool headset)
        {
            return headset ? Mathf.Max(d.cpuMs, d.gpuMs) : d.frameMs;
        }

        // Slower than the critical frame time, or dropping frames.
        public static bool IsSlow(DirectionMeasure d, bool headset, VRBudgets b)
        {
            return d != null && (d.dropped > 0 || Cost(d, headset) > b.frameTimeCriticalMs);
        }

        // Critical when slow, a warning over the warning frame time, else none.
        public static Severity? LevelOf(DirectionMeasure d, bool headset, VRBudgets b)
        {
            if (d == null) return null;
            if (IsSlow(d, headset, b)) return Severity.Critical;
            return Cost(d, headset) > b.frameTimeWarningMs ? Severity.Warning : (Severity?)null;
        }

        // Moves and turns the root around the head so that the head stands at the target and looks along the yaw.
        // A root that is its own head (a plain camera) also gets zero pitch and roll.
        public static void Place(Transform root, Transform head, Vector3 target, float yaw)
        {
            if (root == head)
            {
                root.SetPositionAndRotation(target, Quaternion.Euler(0f, yaw, 0f));
                return;
            }
            root.RotateAround(head.position, Vector3.up, Mathf.DeltaAngle(head.eulerAngles.y, yaw));
            root.position += target - head.position;
        }

        public sealed class Frozen
        {
            public readonly List<KeyValuePair<Rigidbody, bool>> bodies = new List<KeyValuePair<Rigidbody, bool>>();
            public readonly List<KeyValuePair<CharacterController, bool>> controllers = new List<KeyValuePair<CharacterController, bool>>();
        }

        // Physics of the moved object would pull it back: kinematic bodies and switched-off character controllers
        // while measuring.
        public static Frozen Freeze(Transform root)
        {
            var frozen = new Frozen();
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true))
            {
                frozen.bodies.Add(new KeyValuePair<Rigidbody, bool>(body, body.isKinematic));
                body.isKinematic = true;
            }
            foreach (var controller in root.GetComponentsInChildren<CharacterController>(true))
            {
                frozen.controllers.Add(new KeyValuePair<CharacterController, bool>(controller, controller.enabled));
                controller.enabled = false;
            }
            return frozen;
        }

        public static void Restore(Frozen frozen)
        {
            if (frozen == null) return;
            foreach (var pair in frozen.bodies) if (pair.Key != null) pair.Key.isKinematic = pair.Value;
            foreach (var pair in frozen.controllers) if (pair.Key != null) pair.Key.enabled = pair.Value;
        }

        // Plan of a tour over the viewpoints: worst by estimate first, their directions worst first.
        public static TourPlan PlanOf(ViewpointSet set, IList<Viewpoint> points, int frames, int warmup, int directions, TourMover mover, string objectPath,
            int fps = VRBudgets.TargetFps)
        {
            var plan = new TourPlan
            {
                fps = Mathf.Max(1, fps),
                scenes = set.scenes,
                frames = Mathf.Max(1, frames),
                warmup = Mathf.Max(0, warmup),
                directions = directions == 4 ? 4 : 8,
                mover = mover,
                objectPath = objectPath ?? ""
            };
            foreach (var p in points)
            {
                var yaws = directions == 4 ? new List<float> { 0f, 90f, 180f, 270f } : ViewpointEstimator.Yaws(p);
                if (p.Estimated)
                    yaws = yaws.OrderByDescending(y =>
                    {
                        var d = p.directions.FirstOrDefault(x => Mathf.Abs(Mathf.DeltaAngle(x.yaw, y)) < 0.5f);
                        return d != null ? d.drawCalls : 0;
                    }).ToList();
                plan.stops.Add(new TourStop { id = p.id, name = p.Name, position = p.position, yaws = yaws });
            }
            return plan;
        }
    }

    // Drives a tour in Play Mode. Entering Play Mode reloads the domain, so the run lives in SessionState and this class
    // re-attaches itself on load; measured viewpoints are saved there after each direction, so an interrupted tour
    // keeps them.
    [InitializeOnLoad]
    public static class ViewpointTour
    {
        const string Prefix = "UnityTools.SceneAudit.Tour.";
        const string StateKey = Prefix + "State";
        const string RunKey = Prefix + "Run";
        const int Idle = 0, EnteringPlay = 1, Running = 2;

        public static event Action Changed;

        static TourRun run;
        static bool hooked;
        static int lastFrame = -1;
        static Transform root;
        static Transform head;
        static bool found;
        static TourMath.Frozen frozen;

        static ViewpointTour()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            // A script reload in the middle of a tour: the frames are gone, the measured viewpoints are kept.
            if (State == Running) Stop(true, "The viewpoint tour was interrupted by a script reload.");
        }

        static int State
        {
            get { return SessionState.GetInt(StateKey, Idle); }
            set { SessionState.SetInt(StateKey, value); }
        }

        public static bool IsRunning { get { return State != Idle; } }

        // Started but Play Mode is not there yet.
        public static bool Entering { get { return State == EnteringPlay; } }

        // The run in progress, for the window: progress and what is measured so far.
        public static TourRun Current
        {
            get
            {
                if (!IsRunning) return null;
                if (run == null) run = Load();
                return run;
            }
        }

        public static void Start(TourPlan plan)
        {
            if (IsRunning || ProfilerCapture.IsRunning || plan.stops.Count == 0) return;
            plan.startedInPlay = EditorApplication.isPlaying;
            run = new TourRun(plan);
            Save();
            Debug.Log("[SceneAudit] Viewpoint tour: " + plan.stops.Count + " viewpoints, " + plan.Steps + " directions.");
            if (EditorApplication.isPlaying) Begin();
            else
            {
                State = EnteringPlay;
                EditorApplication.isPlaying = true;
            }
            Notify();
        }

        public static void Cancel()
        {
            if (IsRunning) Stop(true, "The viewpoint tour was canceled.");
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && State == EnteringPlay) Begin();
            else if (change == PlayModeStateChange.ExitingPlayMode && State == Running) Stop(true, "Play Mode stopped before the viewpoint tour finished.");
            else if (change == PlayModeStateChange.EnteredEditMode && State == EnteringPlay) Stop(true, "Play Mode did not start; viewpoint tour canceled.");
        }

        static void Begin()
        {
            run = Load();
            if (run == null)
            {
                State = Idle;
                return;
            }
            // The viewer is looked for after the settle frames: the game builds its rig and player in them.
            root = head = null;
            found = false;
            State = Running;
            lastFrame = -1;
            if (!hooked)
            {
                hooked = true;
                EditorApplication.update += OnFrame;
            }
            Notify();
        }

        // The moved object and the head the viewpoint is for: the VR rig, an object picked by its path or a camera.
        static bool FindViewer()
        {
            var vr = ViewpointPlacer.VrCamera();
            var main = Camera.main;
            switch (run.plan.mover)
            {
                case TourMover.Object:
                    var go = string.IsNullOrEmpty(run.plan.objectPath) ? null : GameObject.Find("/" + run.plan.objectPath.TrimStart('/'));
                    if (go == null) return false;
                    root = go.transform;
                    head = vr != null && vr.transform.IsChildOf(root) ? vr.transform : root;
                    return true;
                case TourMover.Camera:
                    var camera = main != null ? main : vr;
                    if (camera == null) return false;
                    root = head = camera.transform;
                    return true;
                default:
                    var eye = vr != null ? vr : main;
                    if (eye == null) return false;
                    head = eye.transform;
                    root = head.root;
                    return true;
            }
        }

        static void OnFrame()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || run == null || State != Running) return;
            int frame = Time.frameCount;
            if (frame == lastFrame) return;
            lastFrame = frame;
            if (found && (root == null || head == null))
            {
                Stop(true, "The moved object of the viewpoint tour was destroyed.");
                return;
            }
            bool wasWarmup = run.InWarmup;
            // Without a warm-up the head is looked at before the last measured frame instead.
            if (found && run.plan.warmup == 0 && run.LastFrameNext) run.NoteDisplacement(Vector3.Distance(head.position, run.CurrentStop.position));
            int done = run.StepsDone;
            var step = run.Next(FrameSample.Current(true));
            if (step == TourStepKind.Move && !found)
            {
                if (!FindViewer())
                {
                    Stop(true, "No camera to move for the viewpoint tour.");
                    return;
                }
                found = true;
                frozen = TourMath.Freeze(root);
            }
            if (wasWarmup && !run.InWarmup) run.NoteDisplacement(Vector3.Distance(head.position, run.CurrentStop.position));
            if (step == TourStepKind.Move) TourMath.Place(root, head, run.CurrentStop.position, run.CurrentYaw);
            if (run.StepsDone != done)
            {
                Save();
                Notify();
            }
            if (step == TourStepKind.Done) Stop(false, null);
        }

        static void Stop(bool interrupted, string reason)
        {
            if (hooked)
            {
                hooked = false;
                EditorApplication.update -= OnFrame;
            }
            if (run == null) run = Load();
            if (run != null)
            {
                bool startedInPlay = run.plan.startedInPlay;
                var result = run.Finish(interrupted);
                if (result.points.Count > 0)
                {
                    var set = ViewpointSet.Load(run.plan.scenes);
                    set.tour = result;
                    set.Save();
                    UnityEngine.Object.DestroyImmediate(set);
                }
                if (startedInPlay) TourMath.Restore(frozen);
                Debug.Log("[SceneAudit] Viewpoint tour " + (interrupted ? "stopped" : "done") + ": " + result.points.Count + " viewpoints measured" +
                          (result.headset ? ", with headset" : ", without headset") + ", " + result.seconds.ToString("F1", CultureInfo.InvariantCulture) + " s.");
                if (reason != null) Debug.LogWarning("[SceneAudit] " + reason);
                bool leave = !startedInPlay && EditorApplication.isPlaying;
                Clear();
                if (leave) EditorApplication.isPlaying = false;
            }
            else Clear();
            Notify();
        }

        static void Clear()
        {
            State = Idle;
            SessionState.EraseString(RunKey);
            run = null;
            root = head = null;
            found = false;
            frozen = null;
        }

        static void Save()
        {
            SessionState.SetString(RunKey, JsonUtility.ToJson(run));
        }

        static TourRun Load()
        {
            var json = SessionState.GetString(RunKey, "");
            if (json.Length == 0) return null;
            try
            {
                var loaded = new TourRun(new TourPlan());
                JsonUtility.FromJsonOverwrite(json, loaded);
                return loaded;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void Notify()
        {
            var handler = Changed;
            if (handler != null) handler();
        }
    }
}
