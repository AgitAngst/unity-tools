using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace UnityTools.SceneOptimization
{
    [Serializable]
    public sealed class ProfilerSnapshot
    {
        public string scene = "";
        public string timestamp = "";
        public int frames;
        public float avgFPS;
        public float minFPS;
        public float maxFPS;
        public float avgCPUms;
        public float peakCPUms;
        // -1 when the editor cannot read GPU time.
        public float avgGPUms = -1f;
        public float peakGPUms = -1f;
        public int drawCalls;
        public int setPassCalls;
        public int batches;
        public long trisPerFrame;
        public float textureMemoryMB;
        public float meshMemoryMB;
        public float totalAllocatedMB;
    }

    // Measures frames in Play Mode. Entering Play Mode reloads the domain, so the phase lives in
    // SessionState and this class re-attaches itself on load.
    [InitializeOnLoad]
    public static class ProfilerCapture
    {
        enum Phase { Idle = 0, EnteringPlay = 1, Warmup = 2, Collecting = 3 }

        const string Prefix = "UnityTools.SceneAudit.";
        const string PhaseKey = Prefix + "Capture.Phase";
        const string StartedInPlayKey = Prefix + "Capture.StartedInPlay";
        const string SceneKey = Prefix + "Capture.Scene";
        const string FramesPref = Prefix + "Capture.Frames";
        const string WarmupPref = Prefix + "Capture.Warmup";
        const string SnapshotPref = Prefix + "Snapshot.";

        public static event Action Changed;

        static int warmupLeft;
        static int collected;
        static int lastFrame = -1;
        static bool hooked;
        static double sumDt;
        static float minDt;
        static float maxDt;
        static double sumCpu;
        static float maxCpu;
        static double sumGpu;
        static float maxGpu;
        static int gpuFrames;
        static double sumDraw;
        static double sumSetPass;
        static double sumBatches;
        static double sumTris;
        static readonly FrameTiming[] timings = new FrameTiming[1];

        static ProfilerCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            var phase = CurrentPhase;
            // A reload in the middle of measuring (script change) loses the counters: stop cleanly.
            if (phase == Phase.Warmup || phase == Phase.Collecting)
                Abort("Profiler capture was interrupted by a domain reload.");
        }

        public static int Frames
        {
            get { return EditorPrefs.GetInt(FramesPref, 300); }
            set { EditorPrefs.SetInt(FramesPref, value); }
        }

        public static int Warmup
        {
            get { return EditorPrefs.GetInt(WarmupPref, 60); }
            set { EditorPrefs.SetInt(WarmupPref, value); }
        }

        public static bool IsRunning { get { return CurrentPhase != Phase.Idle; } }

        static Phase CurrentPhase
        {
            get { return (Phase)SessionState.GetInt(PhaseKey, 0); }
            set { SessionState.SetInt(PhaseKey, (int)value); }
        }

        public static string StatusText()
        {
            switch (CurrentPhase)
            {
                case Phase.EnteringPlay: return Strings.Get("ui.prof.entering");
                case Phase.Warmup: return Strings.Get("ui.prof.warming");
                case Phase.Collecting: return Strings.Format("ui.prof.running", collected, Frames);
                default: return "";
            }
        }

        public static void Start()
        {
            if (IsRunning) return;
            SessionState.SetString(SceneKey, SceneManager.GetActiveScene().path);
            if (EditorApplication.isPlaying)
            {
                SessionState.SetBool(StartedInPlayKey, true);
                BeginWarmup();
            }
            else
            {
                SessionState.SetBool(StartedInPlayKey, false);
                CurrentPhase = Phase.EnteringPlay;
                EditorApplication.isPlaying = true;
            }
            Notify();
        }

        public static void Cancel()
        {
            bool leavePlay = !SessionState.GetBool(StartedInPlayKey, true) && EditorApplication.isPlaying;
            Unhook();
            CurrentPhase = Phase.Idle;
            Debug.Log("[SceneAudit] Profiler capture canceled.");
            if (leavePlay) EditorApplication.isPlaying = false;
            Notify();
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            var phase = CurrentPhase;
            if (change == PlayModeStateChange.EnteredPlayMode && phase == Phase.EnteringPlay)
                BeginWarmup();
            else if (change == PlayModeStateChange.ExitingPlayMode && (phase == Phase.Warmup || phase == Phase.Collecting))
                Abort("Play Mode stopped before the profiler capture finished.");
            else if (change == PlayModeStateChange.EnteredEditMode && phase == Phase.EnteringPlay)
                Abort("Play Mode did not start; profiler capture canceled.");
        }

        static void BeginWarmup()
        {
            ResetCounters();
            warmupLeft = Warmup;
            CurrentPhase = warmupLeft > 0 ? Phase.Warmup : Phase.Collecting;
            Hook();
            Notify();
        }

        static void Hook()
        {
            if (hooked) return;
            hooked = true;
            // Either callback may come first in a frame; the frame counter keeps one sample per frame.
            Application.onBeforeRender += OnFrame;
            EditorApplication.update += OnFrame;
        }

        static void Unhook()
        {
            if (!hooked) return;
            hooked = false;
            Application.onBeforeRender -= OnFrame;
            EditorApplication.update -= OnFrame;
        }

        static void ResetCounters()
        {
            collected = 0;
            lastFrame = -1;
            sumDt = 0;
            minDt = float.MaxValue;
            maxDt = 0f;
            sumCpu = 0;
            maxCpu = 0f;
            sumGpu = 0;
            maxGpu = 0f;
            gpuFrames = 0;
            sumDraw = 0;
            sumSetPass = 0;
            sumBatches = 0;
            sumTris = 0;
        }

        static void OnFrame()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
            int frame = Time.frameCount;
            if (frame == lastFrame) return;
            lastFrame = frame;

            var phase = CurrentPhase;
            if (phase == Phase.Warmup)
            {
                if (--warmupLeft <= 0) CurrentPhase = Phase.Collecting;
                return;
            }
            if (phase != Phase.Collecting) return;

            Sample();
            collected++;
            if (collected % 10 == 0) Notify();
            if (collected >= Frames) Finish();
        }

        static void Sample()
        {
            float dt = Time.unscaledDeltaTime;
            sumDt += dt;
            minDt = Mathf.Min(minDt, dt);
            maxDt = Mathf.Max(maxDt, dt);

            float cpu = UnityStats.frameTime * 1000f;
            sumCpu += cpu;
            maxCpu = Mathf.Max(maxCpu, cpu);

            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0)
            {
                float gpu = (float)timings[0].gpuFrameTime;
                sumGpu += gpu;
                maxGpu = Mathf.Max(maxGpu, gpu);
                gpuFrames++;
            }

            sumDraw += UnityStats.drawCalls;
            sumSetPass += UnityStats.setPassCalls;
            sumBatches += UnityStats.batches;
            sumTris += UnityStats.triangles;
        }

        static void Finish()
        {
            Unhook();
            var scene = SessionState.GetString(SceneKey, SceneManager.GetActiveScene().path);
            int n = Mathf.Max(1, collected);
            var snapshot = new ProfilerSnapshot
            {
                scene = scene,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                frames = collected,
                avgFPS = sumDt > 0 ? (float)(collected / sumDt) : 0f,
                minFPS = maxDt > 0f ? 1f / maxDt : 0f,
                maxFPS = minDt > 0f && minDt < float.MaxValue ? 1f / minDt : 0f,
                avgCPUms = (float)(sumCpu / n),
                peakCPUms = maxCpu,
                avgGPUms = gpuFrames > 0 ? (float)(sumGpu / gpuFrames) : -1f,
                peakGPUms = gpuFrames > 0 ? maxGpu : -1f,
                drawCalls = (int)Math.Round(sumDraw / n),
                setPassCalls = (int)Math.Round(sumSetPass / n),
                batches = (int)Math.Round(sumBatches / n),
                trisPerFrame = (long)Math.Round(sumTris / n),
                textureMemoryMB = (float)(Texture.currentTextureMemory / (1024.0 * 1024.0)),
                meshMemoryMB = MeshMemoryMB(),
                totalAllocatedMB = (float)(Profiler.GetTotalAllocatedMemoryLong() / (1024.0 * 1024.0))
            };
            Store(scene, snapshot);
            CurrentPhase = Phase.Idle;
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[SceneAudit] Profiler snapshot: {0} frames, {1:F1} FPS avg, CPU {2:F2} ms avg, {3} draw calls.",
                snapshot.frames, snapshot.avgFPS, snapshot.avgCPUms, snapshot.drawCalls));
            if (!SessionState.GetBool(StartedInPlayKey, true)) EditorApplication.isPlaying = false;
            Notify();
        }

        static float MeshMemoryMB()
        {
            long bytes = 0;
            foreach (var mesh in Resources.FindObjectsOfTypeAll<Mesh>()) bytes += Profiler.GetRuntimeMemorySizeLong(mesh);
            return (float)(bytes / (1024.0 * 1024.0));
        }

        static void Abort(string reason)
        {
            Unhook();
            CurrentPhase = Phase.Idle;
            Debug.LogWarning("[SceneAudit] " + reason);
            Notify();
        }

        static void Notify()
        {
            var handler = Changed;
            if (handler != null) handler();
        }

        // Snapshots live in EditorPrefs (they survive restarts); the key includes project and scene.
        static string KeyFor(string scenePath, string slot)
        {
            return SnapshotPref + Hash(Application.dataPath + "|" + scenePath) + "." + slot;
        }

        static string Hash(string text)
        {
            uint hash = 2166136261;
            foreach (var ch in text)
            {
                hash ^= ch;
                hash *= 16777619;
            }
            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }

        static void Store(string scenePath, ProfilerSnapshot snapshot)
        {
            var lastKey = KeyFor(scenePath, "Last");
            var previous = EditorPrefs.GetString(lastKey, "");
            if (previous.Length > 0) EditorPrefs.SetString(KeyFor(scenePath, "Previous"), previous);
            EditorPrefs.SetString(lastKey, JsonUtility.ToJson(snapshot));
        }

        public static ProfilerSnapshot LoadLast(string scenePath) { return Load(KeyFor(scenePath, "Last")); }

        public static ProfilerSnapshot LoadPrevious(string scenePath) { return Load(KeyFor(scenePath, "Previous")); }

        static ProfilerSnapshot Load(string key)
        {
            var json = EditorPrefs.GetString(key, "");
            if (json.Length == 0) return null;
            try
            {
                return JsonUtility.FromJson<ProfilerSnapshot>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
