using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public sealed partial class AuditorWindow
    {
        // ---- viewpoint tour in Play Mode ----

        // The table shows the tour instead of the estimate.
        [SerializeField] bool vpMeasured;
        Rect vpTourButton;
        GUIStyle vpRight;
        GUIStyle vpRightHead;

        static Color AccentColor { get { return EditorGUIUtility.isProSkin ? new Color32(0x4A, 0x8B, 0xD0, 0xFF) : new Color32(0x3A, 0x72, 0xB0, 0xFF); } }

        void OnTourChanged()
        {
            // A finished tour is saved to the file of the viewpoints: take its result into the window's copy.
            if (!ViewpointTour.IsRunning && vpSet != null)
            {
                var fresh = ViewpointSet.Load(vpSet.scenes);
                vpSet.tour = fresh.tour;
                DestroyImmediate(fresh);
            }
            Repaint();
        }

        // The tour shown: the one in progress, else the last one of the viewpoints.
        static TourMeasure TourOf(ViewpointSet set)
        {
            var run = ViewpointTour.Current;
            return run != null ? run.result : set.tour;
        }

        // The plan of the tour panel's settings: viewpoints in the order of the estimate table.
        internal TourPlan TourPlanNow()
        {
            var set = Viewpoints();
            var sorted = SortedViewpoints(set);
            List<Viewpoint> points;
            switch (ViewpointTourPanel.Which)
            {
                case 1: points = sorted.Where(p => vpPicked.Contains(p.id)).ToList(); break;
                case 2: points = sorted; break;
                default: points = sorted.Take(ViewpointTourPanel.Count).ToList(); break;
            }
            return TourMath.PlanOf(set, points, ViewpointTourPanel.Frames, ViewpointTourPanel.Warmup, ViewpointTourPanel.Directions,
                ViewpointTourPanel.Mover, ViewpointTourPanel.ObjectPath);
        }

        // Applied by the tour panel.
        internal void StartTour()
        {
            var plan = TourPlanNow();
            if (plan.stops.Count == 0) return;
            vpMeasured = true;
            vpDirection = -1;
            ViewpointTour.Start(plan);
        }

        // "11,1" in Russian, "11.1" in English, no digit groups; "—" when unknown.
        static string Ms(float ms)
        {
            if (ms < 0f) return "—";
            var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            if (Strings.Current == Lang.Ru) format.NumberDecimalSeparator = ",";
            return ms.ToString("F1", format);
        }

        // "45 s", "1 min 40 s", "12 min".
        internal static string Duration(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            if (s < 60) return Strings.Format("ui.vp.sec", s);
            int minutes = s / 60, rest = s % 60;
            if (minutes >= 10 || rest == 0) return Strings.Format("ui.vp.min", Mathf.RoundToInt(s / 60f));
            return Strings.Format("ui.vp.minsec", minutes, rest);
        }

        static string MoverText(TourMover mover, string objectPath, bool running)
        {
            var key = (running ? "ui.vp.tour.moving." : "ui.vp.tour.moved.") + mover;
            if (mover != TourMover.Object) return Strings.Get(key);
            var name = objectPath.Length > 0 ? objectPath.Substring(objectPath.LastIndexOf('/') + 1) : "—";
            return Strings.Format(key, name);
        }

        void DrawTourBar()
        {
            if (!ViewpointTour.IsRunning) return;
            var run = ViewpointTour.Current;
            string title = Strings.Get("ui.vp.tour.entering");
            float share = 0f;
            if (run != null && !ViewpointTour.Entering)
            {
                int stops = run.plan.stops.Count;
                var stop = run.CurrentStop;
                int yaws = stop != null ? stop.yaws.Count : 0;
                title = Strings.Format("ui.vp.tour.running", Math.Min(run.stop + 1, stops), stops, Math.Min(run.yaw + 1, yaws), yaws);
                share = run.plan.Steps > 0 ? run.StepsDone / (float)run.plan.Steps : 1f;
            }
            GUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(title, new GUIStyle(EditorStyles.boldLabel) { wordWrap = true });
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.fix.cancel"), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += ViewpointTour.Cancel;
            EditorGUILayout.EndHorizontal();
            var bar = GUILayoutUtility.GetRect(0f, 6f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(bar, Tint(EditorGUIUtility.isProSkin ? Color.black : Color.white, 0.35f));
                EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(share), bar.height), AccentColor);
            }
            GUILayout.Label(Strings.Format("ui.vp.tour.left", Duration(run != null ? run.SecondsLeft + (ViewpointTour.Entering ? run.plan.settle / (float)VRBudgets.TargetFps : 0f) : 0f)), wrapMini);
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        // Segments "Estimate | Measured" with the summary next to them and its details below.
        void DrawSummaryRow(string summary, string sub)
        {
            var labels = new[]
            {
                new GUIContent(Strings.Get("ui.vp.seg.estimate"), Strings.Get("ui.vp.seg.tip")),
                new GUIContent(Strings.Get("ui.vp.seg.measured"), Strings.Get("ui.vp.seg.tip"))
            };
            int now = SegmentRow(vpMeasured ? 1 : 0, labels, summary, sub);
            if ((now == 1) != vpMeasured)
            {
                vpMeasured = now == 1;
                vpDirection = -1;
                vpShown = VpPageSize;
                SceneView.RepaintAll();
            }
        }

        // Segments with the summary of the shown one next to them and its details below; returns the picked segment.
        int SegmentRow(int selected, GUIContent[] labels, string summary, string sub)
        {
            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            // Equal segments, each as wide as the longest label with the button's own padding.
            float each = labels.Max(l => EditorStyles.miniButton.CalcSize(l).x);
            int now = GUILayout.Toolbar(selected, labels, EditorStyles.miniButton, GUI.ToolbarButtonSize.Fixed, GUILayout.Width(each * labels.Length));
            GUILayout.Space(10f);
            GUILayout.Label(summary, new GUIStyle(EditorStyles.boldLabel) { wordWrap = true });
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            if (sub.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8f);
                GUILayout.Label(sub, wrapMini);
                GUILayout.Space(8f);
                EditorGUILayout.EndHorizontal();
            }
            return now;
        }

        void DrawTourSummary(ViewpointSet set, TourMeasure tour)
        {
            var run = ViewpointTour.Current;
            var measured = MeasuredOf(set, tour);
            var parts = new List<string>();
            string summary;
            if (run != null) summary = Strings.Format("ui.vp.tour.summary.running", measured.Count, run.plan.stops.Count);
            else if (!tour.Exists) summary = Strings.Get("ui.vp.tour.none");
            else
            {
                int slow = measured.Count(m => TourMath.IsSlow(m.Worst, tour.headset, budgets));
                summary = Strings.Format("ui.vp.tour.summary", Ms(budgets.frameTimeCriticalMs), slow, measured.Count);
                DateTime at;
                if (DateTime.TryParse(tour.time, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out at)) parts.Add(DateText(at));
                parts.Add(Strings.Get(tour.headset ? "ui.vp.tour.headset" : "ui.vp.tour.noHeadset"));
                if (tour.interrupted) parts.Add(Strings.Get("ui.vp.tour.interrupted"));
            }
            if (run != null)
            {
                parts.Add(Strings.Format("ui.vp.tour.params", run.plan.frames, run.plan.warmup, run.plan.directions));
                parts.Add(MoverText(run.plan.mover, run.plan.objectPath, true));
            }
            else if (tour.Exists)
            {
                parts.Add(Strings.Format("ui.vp.tour.params", tour.frames, tour.warmup, tour.directions));
                parts.Add(MoverText(tour.mover, tour.objectPath, false));
                if (tour.seconds > 0f) parts.Add(Strings.Format("ui.vp.tour.took", Duration(tour.seconds)));
            }
            DrawSummaryRow(summary, string.Join("  ·  ", parts.ToArray()));
        }

        // Measured viewpoints still in the set, worst first: slow, slower than the warning time, the rest; then by cost.
        List<PointMeasure> MeasuredOf(ViewpointSet set, TourMeasure tour)
        {
            // The tour in progress is not in the set yet.
            return tour.points
                .Where(m => m.Worst != null && set.Find(m.id) != null)
                .OrderBy(m => RankOf(TourMath.LevelOf(m.Worst, tour.headset, budgets)))
                .ThenByDescending(m => TourMath.Cost(m.Worst, tour.headset))
                .ToList();
        }

        float[] TourWidths(float total)
        {
            var fixedWidths = total < VpNarrowWidth ? new[] { 50f, 48f, 48f, 48f, 54f, 58f } : new[] { 72f, 66f, 58f, 58f, 80f, 80f, 66f };
            float name = Mathf.Max(90f, total - VpCheck - fixedWidths.Sum() - 8f);
            return new[] { VpCheck, name }.Concat(fixedWidths).ToArray();
        }

        Rect[] TourColumns(Rect rect)
        {
            var widths = TourWidths(rect.width);
            var cells = new Rect[widths.Length];
            float x = rect.x + 5f;
            for (int i = 0; i < widths.Length; i++)
            {
                cells[i] = new Rect(x, rect.y, widths[i] - 4f, rect.height);
                x += widths[i];
            }
            return cells;
        }

        void RightCell(Rect rect, string text, Color? color)
        {
            if (vpRight == null) vpRight = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            var style = vpRight;
            if (color.HasValue)
            {
                style = new GUIStyle(vpRight);
                style.normal.textColor = color.Value;
            }
            GUI.Label(new Rect(rect.x, rect.y + 3f, rect.width - 4f, 16f), text, style);
        }

        Color? TimeColor(float ms)
        {
            return ms < 0f ? null : ValueColor(ms, budgets.frameTimeWarningMs, budgets.frameTimeCriticalMs);
        }

        void DrawTourTable(ViewpointSet set, TourMeasure tour)
        {
            var run = ViewpointTour.Current;
            var every = MeasuredOf(set, tour);
            var measured = vpOnlyOver ? every.Where(m => TourMath.LevelOf(m.Worst, tour.headset, budgets) != null).ToList() : every;
            var pending = run == null ? new List<TourStop>() : run.plan.stops.Where(s => tour.Find(s.id) == null && set.Find(s.id) != null).ToList();
            if (measured.Count == 0 && pending.Count == 0)
            {
                if (every.Count > 0)
                {
                    GUILayout.Space(10f);
                    GUILayout.Label(Strings.Get("ui.vp.tour.allGood"), centeredMini);
                }
                return;
            }
            var head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var headCells = TourColumns(head);
            var ids = measured.Select(m => m.id).Concat(pending.Select(s => s.id)).ToList();
            bool all = ids.All(id => vpPicked.Contains(id));
            bool now = GUI.Toggle(new Rect(headCells[0].x, headCells[0].y + 2f, 16f, 16f), all, new GUIContent("", Strings.Get("ui.sel.all.tip")));
            if (now != all)
                foreach (var id in ids)
                    if (now) vpPicked.Add(id);
                    else vpPicked.Remove(id);
            bool narrow = head.width < VpNarrowWidth;
            // Label key, tooltip key.
            var columns = narrow
                ? new[] { "ui.vp.col.frame.short|ui.vp.col.frame", "ui.vp.col.cpu|ui.vp.col.cpu", "ui.vp.col.gpu|ui.vp.col.gpu", "ui.vp.col.dropped.short|ui.vp.col.dropped", "ui.vp.col.dc.short|ui.vp.col.dc", "ui.vp.col.setpass|ui.vp.col.setpass" }
                : new[] { "ui.vp.col.frame|ui.vp.col.frame", "ui.vp.col.peak|ui.vp.col.peak", "ui.vp.col.cpu|ui.vp.col.cpu", "ui.vp.col.gpu|ui.vp.col.gpu", "ui.vp.col.dropped|ui.vp.col.dropped", "ui.vp.col.dc|ui.vp.col.dc", "ui.vp.col.setpass|ui.vp.col.setpass" };
            if (vpRightHead == null) vpRightHead = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            GUI.Label(headCells[1], new GUIContent(Strings.Get("ui.vp.col.point"), Strings.Get("ui.vp.col.point.tip")), EditorStyles.miniBoldLabel);
            for (int i = 0; i < columns.Length; i++)
            {
                var keys = columns[i].Split('|');
                var cell = headCells[i + 2];
                GUI.Label(new Rect(cell.x, cell.y, cell.width - 4f, cell.height), new GUIContent(Strings.Get(keys[0]), Strings.Get(keys[1] + ".tip")), vpRightHead);
            }
            int shown = Math.Min(measured.Count + pending.Count, vpShown);
            for (int i = 0; i < shown; i++)
            {
                if (i < measured.Count) DrawTourRow(set, measured[i].id, measured[i], tour.headset, narrow, i);
                else DrawTourRow(set, pending[i - measured.Count].id, null, tour.headset, narrow, i);
            }
            int total = measured.Count + pending.Count;
            if (total > shown)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8f + VpCheck);
                if (GUILayout.Button(Strings.Format("ui.showMore", Math.Min(VpPageSize, total - shown)), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    vpShown += VpPageSize;
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawTourRow(ViewpointSet set, string id, PointMeasure m, bool headset, bool narrow, int index)
        {
            var p = set.Find(id);
            var rect = Padded(GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true)));
            var w = m != null ? m.Worst : null;
            var level = TourMath.LevelOf(w, headset, budgets);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, id == vpSelectedId ? SelectionColor : Tint(EditorGUIUtility.isProSkin ? Color.white : Color.black, index % 2 == 0 ? 0.04f : 0.07f));
                if (level.HasValue) EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), SeverityColor(level.Value));
            }
            var cells = TourColumns(rect);
            bool on = vpPicked.Contains(id);
            bool now = GUI.Toggle(new Rect(cells[0].x, cells[0].y + 3f, 16f, 16f), on, new GUIContent("", Strings.Get("ui.sel.pick.tip")));
            if (now && !on) vpPicked.Add(id);
            else if (!now && on) vpPicked.Remove(id);

            // Name, then the chip or the "waiting" mark right after it.
            string note = m == null ? Strings.Get("ui.vp.pending") : m.displaced > TourMath.MovedMetres ? Strings.Get("ui.vp.movedByGame") : null;
            float noteWidth = note != null ? EditorStyles.miniLabel.CalcSize(new GUIContent(note)).x + 16f : 0f;
            var nameRect = new Rect(cells[1].x, cells[1].y + 3f, Mathf.Max(30f, cells[1].width - noteWidth), 16f);
            var name = p != null ? p.Name : m != null ? m.name : id;
            var fitted = Fit(name, linkClipped, nameRect.width);
            nameRect.width = Mathf.Min(nameRect.width, linkClipped.CalcSize(new GUIContent(fitted)).x);
            if (GUI.Button(nameRect, new GUIContent(fitted, name + "\n" + Strings.Get("ui.vp.col.point.tip")), linkClipped) && p != null) SelectViewpoint(p, true);
            EditorGUIUtility.AddCursorRect(nameRect, MouseCursor.Link);
            if (note != null)
            {
                if (m == null) GUI.Label(new Rect(nameRect.xMax + 6f, rect.y + 3f, noteWidth, 16f), note, EditorStyles.miniLabel);
                else Chip(nameRect.xMax + 6f, rect.y + 4f, note, Strings.Get("ui.vp.movedByGame.tip"));
            }

            if (w == null)
            {
                for (int i = 2; i < cells.Length; i++) RightCell(cells[i], "—", null);
            }
            else
            {
                // With the headset the frame is paced to 90 Hz: its time says nothing, CPU, GPU and dropped frames do.
                var frameColor = headset ? null : TimeColor(w.frameMs);
                var values = new List<KeyValuePair<string, Color?>> { new KeyValuePair<string, Color?>(Ms(w.frameMs), frameColor) };
                if (!narrow) values.Add(new KeyValuePair<string, Color?>(Ms(w.peakFrameMs), headset ? null : TimeColor(w.peakFrameMs)));
                values.Add(new KeyValuePair<string, Color?>(Ms(w.cpuMs), TimeColor(w.cpuMs)));
                values.Add(new KeyValuePair<string, Color?>(Ms(w.gpuMs), TimeColor(w.gpuMs)));
                values.Add(new KeyValuePair<string, Color?>(w.dropped < 0 ? "—" : FormatNumber(w.dropped), w.dropped > 0 ? TextCritical : (Color?)null));
                values.Add(new KeyValuePair<string, Color?>(FormatNumber(w.drawCalls), null));
                values.Add(new KeyValuePair<string, Color?>(FormatNumber(w.setPassCalls), null));
                for (int i = 0; i < values.Count; i++) RightCell(cells[i + 2], values[i].Key, values[i].Value);
            }
            var e = Event.current;
            if (p != null && e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition) && !cells[0].Contains(e.mousePosition))
            {
                SelectViewpoint(p, true);
                e.Use();
            }
        }

        // The measured direction shown in the details: picked by the user, else the worst one.
        int MeasuredDirectionOf(PointMeasure m)
        {
            if (m == null || m.Worst == null) return -1;
            return vpDirection >= 0 && vpDirection < m.directions.Count ? vpDirection : m.worst;
        }

        // Yaw shown in the Scene view and looked along on a click: of the tour when the table shows it.
        float ShownYaw(Viewpoint p)
        {
            if (vpMeasured && vpSet != null)
            {
                var m = TourOf(vpSet).Find(p.id);
                int md = MeasuredDirectionOf(m);
                if (md >= 0) return m.directions[md].yaw;
            }
            int d = DirectionOf(p);
            return d >= 0 ? p.directions[d].yaw : p.hasYaw ? p.yaw : 0f;
        }

        void DrawTourDetails(ViewpointSet set, TourMeasure tour)
        {
            var p = set.Find(vpSelectedId);
            var m = p != null ? tour.Find(p.id) : null;
            if (p == null || m == null || m.Worst == null) return;
            GUILayout.Space(8f);
            var line = Padded(GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true)));
            if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(line, new Color(0f, 0f, 0f, 0.35f));
            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(p.Name, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            GUILayout.Label(Strings.Format("ui.vp.tour.meta", Strings.Get("ui.vp.kind." + p.kind), DirectionLabel(m.Worst.yaw)), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            if (m.displaced > TourMath.MovedMetres)
            {
                var text = Strings.Get("ui.vp.movedByGame");
                var chip = GUILayoutUtility.GetRect(EditorStyles.miniLabel.CalcSize(new GUIContent(text)).x + 16f, 18f, GUILayout.ExpandWidth(false));
                Chip(chip.x + 4f, chip.y + 2f, text, Strings.Get("ui.vp.movedByGame.tip"));
            }
            GUILayout.FlexibleSpace();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            bool twoColumns = position.width >= VpTwoColumnsWidth;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            if (twoColumns)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(360f));
                DrawMeasuredDirections(p, m, tour.headset);
                EditorGUILayout.EndVertical();
                GUILayout.Space(16f);
                EditorGUILayout.BeginVertical();
                DrawEstimateVsMeasured(p, m);
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical();
                DrawMeasuredDirections(p, m, tour.headset);
                GUILayout.Space(10f);
                DrawEstimateVsMeasured(p, m);
                EditorGUILayout.EndVertical();
            }
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void DrawMeasuredDirections(Viewpoint p, PointMeasure m, bool headset)
        {
            GUILayout.Label(Strings.Get("ui.vp.dirs"), EditorStyles.boldLabel);
            var head = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
            var widths = new[] { 0f, 62f, 54f, 54f, 86f };
            widths[0] = Mathf.Max(90f, head.width - widths.Skip(1).Sum());
            var keys = new[] { "ui.vp.col.az", "ui.vp.col.frame.short", "ui.vp.col.cpu", "ui.vp.col.gpu", "ui.vp.col.dropped" };
            if (vpRightHead == null) vpRightHead = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            float x = head.x + 4f;
            for (int i = 0; i < keys.Length; i++)
            {
                GUI.Label(new Rect(x, head.y, widths[i] - 8f, head.height), Strings.Get(keys[i]), i == 0 ? EditorStyles.miniBoldLabel : vpRightHead);
                x += widths[i];
            }
            int shown = MeasuredDirectionOf(m);
            for (int i = 0; i < m.directions.Count; i++)
            {
                var d = m.directions[i];
                var rect = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(rect, i == shown ? SelectionColor : Tint(EditorGUIUtility.isProSkin ? Color.white : Color.black, i % 2 == 0 ? 0.04f : 0.07f));
                x = rect.x + 4f;
                var label = DirectionLabel(d.yaw) + (i == m.worst ? "  " + Strings.Get("ui.vp.worst") : "");
                GUI.Label(new Rect(x, rect.y + 2f, widths[0] - 4f, 16f), label, i == m.worst ? EditorStyles.boldLabel : EditorStyles.label);
                x += widths[0];
                var cells = new[]
                {
                    new KeyValuePair<string, Color?>(Ms(d.frameMs), headset ? null : TimeColor(d.frameMs)),
                    new KeyValuePair<string, Color?>(Ms(d.cpuMs), TimeColor(d.cpuMs)),
                    new KeyValuePair<string, Color?>(Ms(d.gpuMs), TimeColor(d.gpuMs)),
                    new KeyValuePair<string, Color?>(d.dropped < 0 ? "—" : FormatNumber(d.dropped), d.dropped > 0 ? TextCritical : (Color?)null)
                };
                for (int c = 0; c < cells.Length; c++)
                {
                    RightCell(new Rect(x, rect.y - 1f, widths[c + 1], rect.height), cells[c].Key, cells[c].Value);
                    x += widths[c + 1];
                }
                var e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
                {
                    vpDirection = i;
                    LookFrom(p.position, d.yaw);
                    SceneView.RepaintAll();
                    e.Use();
                }
            }
        }

        void DrawEstimateVsMeasured(Viewpoint p, PointMeasure m)
        {
            var d = m.directions[MeasuredDirectionOf(m)];
            var estimate = p.Estimated ? p.directions.FirstOrDefault(x => Mathf.Abs(Mathf.DeltaAngle(x.yaw, d.yaw)) < 0.5f) : null;
            GUILayout.Label(Strings.Format("ui.vp.cmp", DirectionLabel(d.yaw)), EditorStyles.boldLabel);
            var head = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
            float column = 96f;
            float label = Mathf.Max(90f, head.width - column * 2f);
            if (vpRightHead == null) vpRightHead = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(head.x + label, head.y, column - 8f, head.height), Strings.Get("ui.vp.cmp.estimate"), vpRightHead);
            GUI.Label(new Rect(head.x + label + column, head.y, column - 8f, head.height), Strings.Get("ui.vp.cmp.measured"), vpRightHead);
            var rows = new[]
            {
                new[] { Strings.Get("ui.vp.col.dc"), estimate != null ? FormatNumber(estimate.drawCalls) : "—", FormatNumber(d.drawCalls) },
                new[] { Strings.Get("ui.vp.cmp.tris"), estimate != null ? FormatNumber(estimate.tris) : "—", FormatNumber(d.tris) },
                new[] { Strings.Get("ui.vp.col.setpass"), "—", FormatNumber(d.setPassCalls) }
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var rect = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(rect, Tint(EditorGUIUtility.isProSkin ? Color.white : Color.black, i % 2 == 0 ? 0.04f : 0.07f));
                GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, label - 8f, 16f), rows[i][0], EditorStyles.label);
                RightCell(new Rect(rect.x + label, rect.y - 1f, column, rect.height), rows[i][1], null);
                RightCell(new Rect(rect.x + label + column, rect.y - 1f, column, rect.height), rows[i][2], null);
            }
            GUILayout.Space(4f);
            GUILayout.Label(Strings.Get("ui.vp.cmp.note"), wrapMini);
        }

        // Used by the screenshot test: a finished tour on made-up numbers over the estimated viewpoints.
        void TourForShot(bool headset)
        {
            var set = Viewpoints();
            var tour = new TourMeasure
            {
                time = DateTime.Now.ToString("o", CultureInfo.InvariantCulture),
                toolVersion = ToolVersion.Full,
                headset = headset,
                frames = 30,
                warmup = 15,
                directions = 8,
                mover = TourMover.Rig,
                seconds = 131f
            };
            var points = SortedViewpoints(set).Where(p => p.Estimated).Take(12).ToList();
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var m = new PointMeasure { id = p.id, name = p.Name, displaced = i == 1 ? 0.8f : 0f };
                float load = 1f - i / (float)Math.Max(1, points.Count);
                foreach (var e in p.directions)
                {
                    float share = e.drawCalls / (float)Math.Max(1, p.Worst.drawCalls);
                    float cpu = 4f + 4f * load * share;
                    float gpu = 6f + 5f * load * share;
                    m.directions.Add(new DirectionMeasure
                    {
                        yaw = e.yaw,
                        frames = 30,
                        frameMs = headset ? 11.1f : Mathf.Max(cpu, gpu) + 0.4f,
                        peakFrameMs = Mathf.Max(cpu, gpu) + 3f * load,
                        cpuMs = cpu,
                        gpuMs = gpu,
                        dropped = headset ? (i < 2 ? Mathf.RoundToInt(3f * share * share) : 0) : -1,
                        drawCalls = Mathf.RoundToInt(e.drawCalls * 0.3f),
                        batches = Mathf.RoundToInt(e.drawCalls * 0.3f),
                        setPassCalls = Mathf.RoundToInt(e.drawCalls * 0.12f),
                        tris = (long)(e.tris * 0.55f)
                    });
                }
                m.worst = TourMath.WorstOf(m.directions, headset);
                tour.points.Add(m);
            }
            set.tour = tour;
            set.Save();
            vpMeasured = true;
            vpDirection = -1;
            var worst = MeasuredOf(set, tour).FirstOrDefault();
            if (worst != null) SelectViewpoint(set.Find(worst.id), false);
        }
    }
}
