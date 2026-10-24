using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace UnityTools.SceneOptimization
{
    // One frame as the editor sees it. Times in milliseconds; GPU -1 and dropped frames -1 when unknown.
    public struct FrameSample
    {
        public float dt;
        public float cpuMs;
        public float gpuMs;
        public int dropped;
        public int drawCalls;
        public int setPassCalls;
        public int batches;
        public long tris;
        public bool headset;

        // The current frame: unscaled delta time, main thread time and the counters of the Stats window; GPU from frame
        // timing, or with fromXr from the XR runtime when a headset is active (and dropped frames from it).
        public static FrameSample Current(bool fromXr)
        {
            var s = new FrameSample
            {
                dt = Time.unscaledDeltaTime,
                cpuMs = UnityStats.frameTime * 1000f,
                gpuMs = -1f,
                dropped = -1,
                drawCalls = UnityStats.drawCalls,
                setPassCalls = UnityStats.setPassCalls,
                batches = UnityStats.batches,
                tris = UnityStats.triangles,
                headset = XRSettings.enabled && XRSettings.isDeviceActive
            };
            if (fromXr && s.headset)
            {
                // The XR runtime reports seconds on some SDKs and milliseconds on others: a value below 0.2 is seconds.
                float gpu;
                if (XRStats.TryGetGPUTimeLastFrame(out gpu) && gpu > 0f) s.gpuMs = gpu < 0.2f ? gpu * 1000f : gpu;
                int dropped;
                if (XRStats.TryGetDroppedFrameCount(out dropped)) s.dropped = dropped;
            }
            if (s.gpuMs < 0f)
            {
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, Timings) > 0 && Timings[0].gpuFrameTime > 0) s.gpuMs = (float)Timings[0].gpuFrameTime;
            }
            return s;
        }

        static readonly FrameTiming[] Timings = new FrameTiming[1];
    }

    // Statistics over a run of frames: shared by the profiler snapshot and the viewpoint tour.
    [Serializable]
    public sealed class FrameSampler
    {
        public int frames;
        public double sumDt;
        public float minDt = float.MaxValue;
        public float maxDt;
        public double sumCpu;
        public float maxCpu;
        public double sumGpu;
        public float maxGpu;
        public int gpuFrames;
        public int dropped = -1;
        public double sumDraw;
        public double sumSetPass;
        public double sumBatches;
        public double sumTris;

        public void Add(FrameSample s)
        {
            frames++;
            sumDt += s.dt;
            minDt = Mathf.Min(minDt, s.dt);
            maxDt = Mathf.Max(maxDt, s.dt);
            sumCpu += s.cpuMs;
            maxCpu = Mathf.Max(maxCpu, s.cpuMs);
            if (s.gpuMs >= 0f)
            {
                sumGpu += s.gpuMs;
                maxGpu = Mathf.Max(maxGpu, s.gpuMs);
                gpuFrames++;
            }
            if (s.dropped >= 0) dropped = Math.Max(0, dropped) + s.dropped;
            sumDraw += s.drawCalls;
            sumSetPass += s.setPassCalls;
            sumBatches += s.batches;
            sumTris += s.tris;
        }

        int N { get { return Math.Max(1, frames); } }

        public float AvgFps { get { return sumDt > 0 ? (float)(frames / sumDt) : 0f; } }
        public float MinFps { get { return maxDt > 0f ? 1f / maxDt : 0f; } }
        public float MaxFps { get { return minDt > 0f && minDt < float.MaxValue ? 1f / minDt : 0f; } }
        public float AvgFrameMs { get { return frames > 0 ? (float)(sumDt * 1000.0 / frames) : 0f; } }
        public float PeakFrameMs { get { return maxDt * 1000f; } }
        public float AvgCpuMs { get { return (float)(sumCpu / N); } }
        public float PeakCpuMs { get { return maxCpu; } }
        public float AvgGpuMs { get { return gpuFrames > 0 ? (float)(sumGpu / gpuFrames) : -1f; } }
        public float PeakGpuMs { get { return gpuFrames > 0 ? maxGpu : -1f; } }
        public int DrawCalls { get { return (int)Math.Round(sumDraw / N); } }
        public int SetPassCalls { get { return (int)Math.Round(sumSetPass / N); } }
        public int Batches { get { return (int)Math.Round(sumBatches / N); } }
        public long Tris { get { return (long)Math.Round(sumTris / N); } }
    }
}
