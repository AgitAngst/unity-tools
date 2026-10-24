using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityTools.SceneOptimization
{
    public sealed partial class AuditorWindow
    {
        // ---- history ----

        void RecordHistory(AuditResult r)
        {
            try
            {
                var point = AuditHistory.Record(r, "");
                var h = AuditHistory.Load(AuditHistory.FileFor(r.scenePaths));
                int index = h.points.FindIndex(p => p.time == point.time);
                prevPoint = index > 0 ? h.points[index - 1] : null;
            }
            catch (Exception e)
            {
                prevPoint = null;
                Debug.LogWarning("[SceneAudit] Could not save the history point: " + e.Message);
            }
            history = null;
            historyLabels.Clear();
            histA = histB = -1;
        }

        string CurrentHistoryFile()
        {
            if (result != null) return AuditHistory.FileFor(result.scenePaths);
            var paths = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) paths.Add(scene.path);
            }
            return AuditHistory.FileFor(paths.ToArray());
        }

        AuditHistory History(string path)
        {
            if (history == null || historyPath != path)
            {
                history = AuditHistory.Load(path);
                historyPath = path;
                noteFor = -1;
            }
            return history;
        }

        string HistoryLabel(string path, bool current)
        {
            string label;
            if (!historyLabels.TryGetValue(path, out label))
            {
                var scenes = AuditHistory.Load(path).scenes;
                label = scenes.Length > 0 ? string.Join(", ", scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s)).ToArray()) : System.IO.Path.GetFileNameWithoutExtension(path);
                historyLabels[path] = label;
            }
            // Slashes would make submenus in the popup.
            label = label.Replace('/', '∕');
            return current ? Strings.Format("ui.h.scene.current", label) : label;
        }

        void DrawHistoryTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(Strings.Get("ui.h.title"), EditorStyles.boldLabel);
            GUILayout.Label(Strings.Get("ui.h.intro"), wrapMini);
            GUILayout.Space(4f);

            var current = CurrentHistoryFile();
            var path = string.IsNullOrEmpty(historyFile) ? current : historyFile;
            var files = AuditHistory.Files().ToList();
            if (!files.Contains(current)) files.Insert(0, current);
            if (!files.Contains(path)) files.Insert(0, path);
            int index = files.IndexOf(path);
            var labels = files.Select(f => new GUIContent(HistoryLabel(f, f == current))).ToArray();
            int chosen = EditorGUILayout.Popup(new GUIContent(Strings.Get("ui.h.scene"), Strings.Get("ui.h.scene.tip")), index, labels);
            if (chosen != index)
            {
                historyFile = files[chosen] == current ? "" : files[chosen];
                histA = histB = -1;
            }

            var h = History(string.IsNullOrEmpty(historyFile) ? current : historyFile);
            if (h.points.Count == 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label(Strings.Get("ui.h.none"), hint);
                EndHistoryTab();
                return;
            }
            int count = h.points.Count;
            if (histB < 0 || histB >= count) histB = count - 1;
            if (histA < 0 || histA >= count) histA = 0;

            GUILayout.Space(8f);
            GUILayout.Label(Strings.Get("ui.h.trend"), EditorStyles.boldLabel);
            DrawTrends(h.points);
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Format("ui.h.points", count), EditorStyles.boldLabel);
            GUILayout.Label(Strings.Get("ui.h.points.note"), wrapMini);
            DrawPointTable(h.points);
            GUILayout.Space(8f);
            DrawPointNote(h);
            if (histA != histB)
            {
                GUILayout.Space(8f);
                DrawComparison(h.points[Math.Min(histA, histB)], h.points[Math.Max(histA, histB)]);
            }
            GUILayout.Label(Strings.Format("ui.h.file", historyPath), wrapMini);
            EndHistoryTab();
        }

        void EndHistoryTab()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void DrawTrends(List<HistoryPoint> points)
        {
            int from = Math.Min(histA, histB);
            int to = Math.Max(histA, histB);
            var last = points[to];
            var first = points[from];
            Trend("ui.h.critical", points.Select(p => (double)p.critical).ToList(), CriticalColor, null, last.critical, last.critical - first.critical, false);
            Trend("ui.h.warning", points.Select(p => (double)p.warning).ToList(), WarningColor, null, last.warning, last.warning - first.warning, false);
            Trend("ui.h.info", points.Select(p => (double)p.info).ToList(), InfoColor, null, last.info, last.info - first.info, false);
            Trend("ui.budget.tris", points.Select(p => (double)p.sceneTris).ToList(), StateColor(last.sceneTris, last.trisLimit), last.trisLimit,
                last.sceneTris, last.sceneTris - first.sceneTris, false);
            Trend("ui.budget.drawCalls", points.Select(p => (double)p.drawCalls).ToList(), StateColor(last.drawCalls, last.drawCallLimit), last.drawCallLimit,
                last.drawCalls, last.drawCalls - first.drawCalls, false);
            Trend("ui.budget.texMem", points.Select(p => p.textureMemoryMB).ToList(), StateColor(last.textureMemoryMB, last.textureMemoryLimitMB),
                last.textureMemoryLimitMB, last.textureMemoryMB, last.textureMemoryMB - first.textureMemoryMB, false);
            if (points.Any(p => p.hasProfiler))
            {
                var fps = points.Select(p => p.hasProfiler ? (double)p.avgFPS : double.NaN).ToList();
                var lastFps = points.Take(to + 1).LastOrDefault(p => p.hasProfiler);
                var firstFps = points.Skip(from).Take(to - from + 1).FirstOrDefault(p => p.hasProfiler);
                double value = lastFps != null ? lastFps.avgFPS : double.NaN;
                double delta = lastFps != null && firstFps != null ? lastFps.avgFPS - firstFps.avgFPS : 0;
                Trend("ui.m.fps", fps, OkColor, VRBudgets.FpsWarning, value, delta, true);
            }
            GUILayout.Label(Strings.Get("ui.h.trend.note"), wrapMini);
        }

        static Color StateColor(double value, double limit)
        {
            return limit > 0 && value > limit ? WarningColor : OkColor;
        }

        // One row: label, line over the points (NaN leaves a gap), dashed limit, value at B and change since A.
        void Trend(string key, List<double> values, Color color, double? limit, double value, double delta, bool higherIsBetter)
        {
            var rect = EditorGUILayout.GetControlRect(false, 34f);
            float labelWidth = Mathf.Min(LabelColumn, rect.width * 0.35f);
            const float valueWidth = 150f;
            GUI.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), Strings.Get(key));
            var area = new Rect(rect.x + labelWidth, rect.y + 2f, Mathf.Max(40f, rect.width - labelWidth - valueWidth - 6f), rect.height - 4f);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(area, EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.78f, 0.78f, 0.78f));
                double max = 0;
                foreach (var v in values) if (!double.IsNaN(v)) max = Math.Max(max, v);
                if (limit.HasValue) max = Math.Max(max, limit.Value * 1.1);
                if (max <= 0) max = 1;
                Func<int, float> x = i => values.Count == 1 ? area.center.x : area.x + 4f + (area.width - 8f) * i / (values.Count - 1);
                Func<double, float> y = v => area.yMax - 3f - (area.height - 6f) * (float)(v / max);
                // The compared points A and B.
                var mark = new Color(0.5f, 0.5f, 0.5f, 0.35f);
                EditorGUI.DrawRect(new Rect(x(histA), area.y, 1f, area.height), mark);
                EditorGUI.DrawRect(new Rect(x(histB), area.y, 1f, area.height), mark);
                if (limit.HasValue && limit.Value > 0)
                {
                    float ly = y(limit.Value);
                    for (float lx = area.x; lx < area.xMax; lx += 6f)
                        EditorGUI.DrawRect(new Rect(lx, ly, Mathf.Min(3f, area.xMax - lx), 1f), new Color(0f, 0f, 0f, 0.5f));
                }
                Handles.color = color;
                var line = new List<Vector3>();
                for (int i = 0; i <= values.Count; i++)
                {
                    if (i < values.Count && !double.IsNaN(values[i]))
                    {
                        line.Add(new Vector3(x(i), y(values[i]), 0f));
                        continue;
                    }
                    if (line.Count > 1) Handles.DrawAAPolyLine(2f, line.ToArray());
                    line.Clear();
                }
                for (int i = 0; i < values.Count; i++)
                    if (!double.IsNaN(values[i])) EditorGUI.DrawRect(new Rect(x(i) - 2f, y(values[i]) - 2f, 4f, 4f), color);
                Handles.color = Color.white;
            }
            var valueRect = new Rect(area.xMax + 6f, rect.y, valueWidth, rect.height);
            GUI.Label(new Rect(valueRect.x, valueRect.y, valueRect.width, rect.height / 2f), double.IsNaN(value) ? Strings.Get("ui.na") : FormatNumber(value));
            if (Math.Abs(delta) > 0.0001)
            {
                bool better = higherIsBetter ? delta > 0 : delta < 0;
                var style = new GUIStyle(EditorStyles.miniLabel);
                style.normal.textColor = better ? OkColor : CriticalColor;
                GUI.Label(new Rect(valueRect.x, valueRect.y + rect.height / 2f, valueRect.width, rect.height / 2f),
                    new GUIContent(Strings.Format("ui.h.sinceA", DeltaText(delta)), Strings.Get("ui.h.sinceA.tip")), style);
            }
        }

        void DrawPointTable(List<HistoryPoint> points)
        {
            var columns = new[] { 22f, 22f, 118f, 46f, 52f, 46f, 86f, 60f, 62f };
            var heads = new[] { "A", "B", Strings.Get("ui.h.col.date"), Strings.Get("ui.h.col.critical"), Strings.Get("ui.h.col.warning"),
                Strings.Get("ui.h.col.info"), Strings.Get("ui.h.col.tris"), Strings.Get("ui.h.col.drawCalls"), Strings.Get("ui.h.col.texMem") };
            var tips = new[] { Strings.Get("ui.h.col.a.tip"), Strings.Get("ui.h.col.b.tip"), "", Strings.Get("ui.h.critical"), Strings.Get("ui.h.warning"),
                Strings.Get("ui.h.info"), Strings.Get("ui.budget.tris"), Strings.Get("ui.budget.drawCalls"), Strings.Get("ui.budget.texMem") };
            var head = EditorGUILayout.GetControlRect();
            float hx = head.x;
            for (int c = 0; c < columns.Length; c++)
            {
                GUI.Label(new Rect(hx, head.y, columns[c], head.height), new GUIContent(heads[c], tips[c]), EditorStyles.miniBoldLabel);
                hx += columns[c];
            }
            GUI.Label(new Rect(hx, head.y, Mathf.Max(0f, head.xMax - hx), head.height), Strings.Get("ui.h.col.note"), EditorStyles.miniBoldLabel);

            // Newest first.
            for (int i = points.Count - 1; i >= 0; i--)
            {
                var p = points[i];
                var rect = EditorGUILayout.GetControlRect(false, 22f);
                if (Event.current.type == EventType.Repaint && (points.Count - 1 - i) % 2 == 1)
                    EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.04f) : new Color(0f, 0f, 0f, 0.07f));
                float x = rect.x;
                if (GUI.Toggle(new Rect(x + 3f, rect.y + 3f, 16f, 16f), histA == i, GUIContent.none) && histA != i) histA = i;
                x += columns[0];
                if (GUI.Toggle(new Rect(x + 3f, rect.y + 3f, 16f, 16f), histB == i, GUIContent.none) && histB != i) histB = i;
                x += columns[1];
                var cells = new[]
                {
                    DateText(p.Time), FormatNumber(p.critical), FormatNumber(p.warning), FormatNumber(p.info),
                    FormatNumber(p.sceneTris), FormatNumber(p.drawCalls), FormatNumber(p.textureMemoryMB)
                };
                for (int c = 0; c < cells.Length; c++)
                {
                    GUI.Label(new Rect(x, rect.y + 3f, columns[c + 2], 16f), cells[c]);
                    x += columns[c + 2];
                }
                GUI.Label(new Rect(x, rect.y + 3f, Mathf.Max(0f, rect.xMax - x), 16f), new GUIContent(p.note, p.note), EditorStyles.miniLabel);
            }
        }

        void DrawPointNote(AuditHistory h)
        {
            var p = h.points[histB];
            if (noteFor != histB)
            {
                noteFor = histB;
                noteDraft = p.note;
            }
            GUILayout.Label(Strings.Format("ui.h.noteOf", DateText(p.Time)), EditorStyles.boldLabel);
            GUILayout.Label(Strings.Get("ui.h.note.hint"), wrapMini);
            noteDraft = EditorGUILayout.TextField(noteDraft);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Format("ui.h.version", p.toolVersion), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(h.points.Count == 0);
            if (GUILayout.Button(Strings.Get("ui.h.delete"), GUILayout.Width(130f)) &&
                EditorUtility.DisplayDialog(Strings.Get("ui.h.title"), Strings.Format("ui.h.delete.ask", DateText(p.Time)), Strings.Get("ui.h.delete"), Strings.Get("ui.fix.cancel")))
            {
                h.points.RemoveAt(histB);
                h.Save(historyPath);
                histA = histB = -1;
                noteFor = -1;
                GUIUtility.ExitGUI();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(noteDraft == p.note);
            if (GUILayout.Button(Strings.Get("ui.h.note.save"), GUILayout.Width(130f)))
            {
                p.note = (noteDraft ?? "").Trim();
                h.Save(historyPath);
                noteDraft = p.note;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        void DrawComparison(HistoryPoint a, HistoryPoint b)
        {
            var diff = AuditHistory.Compare(a, b);
            GUILayout.Label(Strings.Format("ui.h.compare", DateText(a.Time), DateText(b.Time)), EditorStyles.boldLabel);
            CompareRow("ui.h.critical", a.critical, b.critical, false);
            CompareRow("ui.h.warning", a.warning, b.warning, false);
            CompareRow("ui.h.info", a.info, b.info, false);
            CompareRow("ui.h.accepted", a.accepted, b.accepted, null);
            CompareRow("ui.budget.tris", a.sceneTris, b.sceneTris, false);
            CompareRow("ui.budget.drawCalls", a.drawCalls, b.drawCalls, false);
            CompareRow("ui.budget.texMem", a.textureMemoryMB, b.textureMemoryMB, false);
            if (a.hasProfiler && b.hasProfiler)
            {
                CompareRow("ui.m.fps", a.avgFPS, b.avgFPS, true);
                CompareRow("ui.h.cpu", a.avgCPUms, b.avgCPUms, false);
                if (a.avgGPUms >= 0f && b.avgGPUms >= 0f) CompareRow("ui.h.gpu", a.avgGPUms, b.avgGPUms, false);
            }

            GUILayout.Space(6f);
            GUILayout.Label(Strings.Format("ui.h.checks", diff.checkChanges.Count), EditorStyles.boldLabel);
            if (diff.checkChanges.Count == 0) GUILayout.Label(Strings.Get("ui.h.checks.none"), EditorStyles.miniLabel);
            foreach (var change in diff.checkChanges.OrderBy(c => c.Value).ThenBy(c => c.Key, StringComparer.Ordinal))
                CompareRow(CheckTitle(change.Key), a.CountOf(change.Key), b.CountOf(change.Key), false, change.Key);

            GUILayout.Space(6f);
            if (!diff.problemsKnown)
            {
                GUILayout.Label(Strings.Get("ui.h.problems.old"), wrapMini);
                return;
            }
            if (diff.acceptedSince > 0) GUILayout.Label(Strings.Format("ui.h.acceptedSince", diff.acceptedSince), wrapMini);
            histFixedOpen = ProblemList(Strings.Format("ui.h.fixed", diff.fixedProblems.Count), diff.fixedProblems, histFixedOpen);
            histNewOpen = ProblemList(Strings.Format("ui.h.new", diff.newProblems.Count), diff.newProblems, histNewOpen);
        }

        static string CheckTitle(string checkId)
        {
            return Strings.Has(checkId + ".title") ? Strings.Get(checkId + ".title") : checkId;
        }

        // Label, "a → b", change coloured green when better and red when worse (no colour when neither).
        void CompareRow(string keyOrText, double a, double b, bool? higherIsBetter, string tip = "")
        {
            var rect = EditorGUILayout.GetControlRect();
            var label = Strings.Has(keyOrText) ? Strings.Get(keyOrText) : keyOrText;
            float labelWidth = Mathf.Max(LabelColumn, Mathf.Min(320f, rect.width * 0.45f));
            GUI.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), new GUIContent(label, tip));
            float x = rect.x + labelWidth;
            GUI.Label(new Rect(x, rect.y, 180f, rect.height), FormatNumber(a) + " → " + FormatNumber(b));
            double delta = b - a;
            if (Math.Abs(delta) < 0.0001) return;
            var style = new GUIStyle(EditorStyles.label);
            if (higherIsBetter.HasValue) style.normal.textColor = (higherIsBetter.Value ? delta > 0 : delta < 0) ? OkColor : CriticalColor;
            GUI.Label(new Rect(x + 186f, rect.y, Mathf.Max(0f, rect.xMax - x - 186f), rect.height), DeltaText(delta), style);
        }

        bool ProblemList(string title, List<string> problems, bool open)
        {
            open = EditorGUILayout.Foldout(open, title, true, boldFoldout);
            if (!open || problems.Count == 0) return open;
            foreach (var p in problems.Take(GroupPageSize))
            {
                int bar = p.IndexOf('|');
                var id = bar >= 0 ? p.Substring(0, bar) : p;
                var where = bar >= 0 ? p.Substring(bar + 1) : "";
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16f);
                GUILayout.Label(new GUIContent(CheckTitle(id) + " — " + where, id), wrapMini);
                EditorGUILayout.EndHorizontal();
            }
            if (problems.Count > GroupPageSize) GUILayout.Label(Strings.Format("ui.h.more", problems.Count - GroupPageSize), EditorStyles.miniLabel);
            return open;
        }

        static string DeltaText(double delta)
        {
            if (Math.Abs(delta) < 0.0001) return "0";
            return delta > 0 ? "↑ +" + FormatNumber(delta) : "↓ " + FormatNumber(delta);
        }

        static string DateText(DateTime time)
        {
            return time.ToString(Strings.Current == Lang.Ru ? "dd.MM.yyyy HH:mm" : "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }
    }
}
