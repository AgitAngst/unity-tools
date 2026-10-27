using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityTools.SceneOptimization
{
    public sealed partial class AuditorWindow
    {
        // ---- profiler ----

        void DrawProfilerTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(Strings.Get("ui.prof.title"), EditorStyles.boldLabel);
            GUILayout.Label(Strings.Get("ui.prof.intro"), wrapMini);
            GUILayout.Space(6f);

            bool running = ProfilerCapture.IsRunning;
            EditorGUI.BeginDisabledGroup(running);
            ProfilerCapture.Frames = Mathf.Clamp(EditorGUILayout.IntField(Strings.Get("ui.prof.frames"), ProfilerCapture.Frames), 30, 5000);
            ProfilerCapture.Warmup = Mathf.Clamp(EditorGUILayout.IntField(Strings.Get("ui.prof.warmup"), ProfilerCapture.Warmup), 0, 1000);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            if (!running)
            {
                // One Play Mode measurement at a time: the viewpoint tour holds Play Mode too.
                EditorGUI.BeginDisabledGroup(ViewpointTour.IsRunning);
                if (GUILayout.Button(new GUIContent(" " + Strings.Get("ui.prof.capture"), playIcon), GUILayout.Width(220f), GUILayout.Height(26f)))
                    EditorApplication.delayCall += ProfilerCapture.Start;
                EditorGUI.EndDisabledGroup();
            }
            else if (GUILayout.Button(Strings.Get("ui.prof.cancel"), GUILayout.Width(220f), GUILayout.Height(26f)))
            {
                ProfilerCapture.Cancel();
            }
            GUILayout.Label(ProfilerCapture.StatusText(), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            compare = EditorGUILayout.ToggleLeft(Strings.Get("ui.prof.compare"), compare);
            GUILayout.Label(Strings.Get("ui.prof.note") + "\n" + Strings.Get("ui.prof.cpuNote"), hint);

            var scenePath = SceneManager.GetActiveScene().path;
            var last = ProfilerCapture.LoadLast(scenePath);
            var previous = compare ? ProfilerCapture.LoadPrevious(scenePath) : null;
            if (last == null)
            {
                GUILayout.Label(Strings.Get("ui.prof.none"), EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label(Strings.Format("ui.prof.taken", last.timestamp, last.frames), EditorStyles.miniLabel);
                DrawSnapshot(last, previous);
                if (compare && previous == null) GUILayout.Label(Strings.Get("ui.prof.noPrevious"), EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void DrawSnapshot(ProfilerSnapshot s, ProfilerSnapshot p)
        {
            var b = budgets;
            MetricRow("ui.m.fps", Triple(s.avgFPS, s.minFPS, s.maxFPS), FpsColor(s.avgFPS, budgets), p != null ? (double?)(s.avgFPS - p.avgFPS) : null, true);
            MetricRow("ui.m.cpu", Pair(s.avgCPUms, s.peakCPUms), FrameColor(s.avgCPUms, b), p != null ? Delta(s.avgCPUms, p.avgCPUms) : null, false);
            MetricRow("ui.m.gpu", Pair(s.avgGPUms, s.peakGPUms), FrameColor(s.avgGPUms, b), p != null ? Delta(s.avgGPUms, p.avgGPUms) : null, false);
            MetricRow("ui.m.drawCalls", FormatNumber(s.drawCalls), RangeColor(s.drawCalls, b.drawCallsWarning, b.drawCallsCritical),
                p != null ? (double?)(s.drawCalls - p.drawCalls) : null, false);
            MetricRow("ui.m.setPass", FormatNumber(s.setPassCalls), null, p != null ? (double?)(s.setPassCalls - p.setPassCalls) : null, false);
            MetricRow("ui.m.batches", FormatNumber(s.batches), null, p != null ? (double?)(s.batches - p.batches) : null, false);
            MetricRow("ui.m.tris", FormatNumber(s.trisPerFrame), RangeColor(s.trisPerFrame, b.sceneTrisWarning, b.sceneTrisCritical),
                p != null ? (double?)(s.trisPerFrame - p.trisPerFrame) : null, false);
            MetricRow("ui.m.texMem", FormatNumber(s.textureMemoryMB), RangeColor(s.textureMemoryMB, b.textureMemoryWarningMB, b.textureMemoryCriticalMB),
                p != null ? (double?)(s.textureMemoryMB - p.textureMemoryMB) : null, false);
            MetricRow("ui.m.meshMem", FormatNumber(s.meshMemoryMB), null, p != null ? (double?)(s.meshMemoryMB - p.meshMemoryMB) : null, false);
            MetricRow("ui.m.alloc", FormatNumber(s.totalAllocatedMB), null, p != null ? (double?)(s.totalAllocatedMB - p.totalAllocatedMB) : null, false);
        }

        void MetricRow(string key, string value, Color? color, double? delta, bool higherIsBetter)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), Strings.Get(key));
            float x = rect.x + LabelColumn;
            if (color.HasValue && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(x, rect.y + rect.height / 2f - 4f, 8f, 8f), color.Value);
            GUI.Label(new Rect(x + 14f, rect.y, 170f, rect.height), value);
            if (delta.HasValue && Math.Abs(delta.Value) > 0.0001)
            {
                bool better = higherIsBetter ? delta.Value > 0 : delta.Value < 0;
                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = better ? TextOk : TextCritical;
                var arrow = delta.Value > 0 ? "↑ +" : "↓ ";
                GUI.Label(new Rect(x + 190f, rect.y, rect.xMax - x - 190f, rect.height), arrow + FormatNumber(delta.Value), style);
            }
        }

        static string Triple(float a, float b, float c)
        {
            return FormatNumber(a) + " / " + FormatNumber(b) + " / " + FormatNumber(c);
        }

        static string Pair(float avg, float peak)
        {
            if (avg < 0f) return Strings.Get("ui.na");
            return avg.ToString("F2", CultureInfo.InvariantCulture) + " / " + peak.ToString("F2", CultureInfo.InvariantCulture);
        }

        static double? Delta(float current, float previous)
        {
            if (current < 0f || previous < 0f) return null;
            return current - previous;
        }

        static Color? FpsColor(float fps, VRBudgets b)
        {
            if (fps >= b.FpsWarning) return OkColor;
            return fps >= b.FpsCritical ? WarningColor : CriticalColor;
        }

        static Color? FrameColor(float ms, VRBudgets b)
        {
            if (ms < 0f) return null;
            if (ms <= b.frameTimeWarningMs) return OkColor;
            return ms <= b.frameTimeCriticalMs ? WarningColor : CriticalColor;
        }

        static Color? RangeColor(double value, double warning, double critical)
        {
            if (value > critical) return CriticalColor;
            return value > warning ? WarningColor : OkColor;
        }
    }
}
