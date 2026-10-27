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
        // ---- viewpoints ----

        const float VpCheck = 22f;
        const float VpNarrowWidth = 640f;
        const float VpTwoColumnsWidth = 720f;
        const float VpFrustumMeters = 6f;
        // Rows of the table shown at first and added by "Show N more", so the details below stay in reach.
        const int VpPageSize = 20;

        [SerializeField] bool vpOnlyOver;
        [SerializeField] bool vpWalls;
        [SerializeField] bool vpByDrawCalls;
        [SerializeField] string vpSelectedId = "";
        // Direction picked in the details; -1 for the worst one.
        [SerializeField] int vpDirection = -1;
        ViewpointSet vpSet;
        string vpSetFile = "";
        readonly HashSet<string> vpPicked = new HashSet<string>();
        List<HeavyEntry> vpHeavy;
        string vpHeavyKey = "";
        ViewpointPlacer.Survey vpSurvey;
        Rect vpPlaceButton;
        bool vpDirty;
        int vpShown = VpPageSize;

        void ViewpointsEnable()
        {
            SceneView.duringSceneGui += OnViewpointsSceneGui;
            SceneView.duringSceneGui += OnShadowsSceneGui;
            Undo.undoRedoPerformed += OnViewpointsUndo;
            ViewpointTour.Changed += OnTourChanged;
        }

        void ViewpointsDisable()
        {
            SceneView.duringSceneGui -= OnViewpointsSceneGui;
            SceneView.duringSceneGui -= OnShadowsSceneGui;
            Undo.undoRedoPerformed -= OnViewpointsUndo;
            ViewpointTour.Changed -= OnTourChanged;
            if (vpSet != null) DestroyImmediate(vpSet);
            vpSet = null;
        }

        void OnTabChanged()
        {
            SceneView.RepaintAll();
        }

        void OnViewpointsUndo()
        {
            if (vpSet == null) return;
            vpSet.Save();
            vpHeavy = null;
            Repaint();
            SceneView.RepaintAll();
        }

        // Viewpoints of the loaded scenes; a new scene set loads its own file.
        ViewpointSet Viewpoints()
        {
            var scenes = ViewpointSet.LoadedScenePaths();
            var file = ViewpointSet.FileFor(scenes);
            if (vpSet != null && vpSetFile == file) return vpSet;
            if (vpSet != null) DestroyImmediate(vpSet);
            vpSet = ViewpointSet.Load(scenes);
            vpSetFile = file;
            vpPicked.Clear();
            vpHeavy = null;
            vpSurvey = null;
            vpShown = VpPageSize;
            return vpSet;
        }

        void DrawViewpointsTab()
        {
            var set = Viewpoints();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Get("ui.vp.intro"), wrapMini);
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            DrawViewpointsToolbar(set);
            DrawTourBar();
            if (set.points.Count == 0)
            {
                DrawViewpointsEmpty();
                return;
            }
            pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
            if (vpMeasured)
            {
                var tour = TourOf(set);
                DrawTourSummary(set, tour);
                DrawViewpointsSelectionBar(set);
                DrawTourTable(set, tour);
                if (!ViewpointTour.IsRunning) DrawTourDetails(set, tour);
            }
            else
            {
                DrawViewpointsSummary(set);
                DrawViewpointsSelectionBar(set);
                DrawViewpointsTable(set);
                DrawViewpointDetails(set);
            }
            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Get(vpMeasured ? "ui.vp.tour.footer" : "ui.vp.footer"), wrapMini);
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.EndScrollView();
        }

        // Actions on the left, switches on the right; when they do not fit, a second row instead of cut buttons.
        // While a tour runs, estimating and placing wait for it.
        void DrawViewpointsToolbar(ViewpointSet set)
        {
            bool touring = ViewpointTour.IsRunning;
            var estimate = new GUIContent(" " + Strings.Get("ui.vp.estimate"), playIcon, Strings.Get("ui.vp.estimate.tip"));
            var place = new GUIContent(Strings.Get("ui.vp.place"), Strings.Get("ui.vp.place.tip"));
            var measure = new GUIContent(Strings.Get("ui.vp.measure"), Strings.Get("ui.vp.measure.tip"));
            var addHere = new GUIContent(Strings.Get("ui.vp.addHere"), Strings.Get("ui.vp.addHere.tip"));
            var csv = new GUIContent(Strings.Get("ui.lm.csv"));
            var walls = new GUIContent(Strings.Get("ui.vp.walls"), Strings.Get("ui.vp.walls.tip"));
            var onlyOver = new GUIContent(Strings.Get("ui.vp.onlyOver"));
            var button = EditorStyles.toolbarButton;
            var dropDown = EditorStyles.toolbarDropDown;
            float width = button.CalcSize(estimate).x + dropDown.CalcSize(place).x + dropDown.CalcSize(measure).x + button.CalcSize(addHere).x +
                          button.CalcSize(csv).x + button.CalcSize(walls).x + button.CalcSize(onlyOver).x + 24f;
            bool twoRows = width > position.width;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginDisabledGroup(set.points.Count == 0 || touring);
            if (GUILayout.Button(estimate, button, GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += () => EstimateViewpoints(null);
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(touring);
            if (GUILayout.Button(place, dropDown, GUILayout.ExpandWidth(false)))
                PopupWindow.Show(vpPlaceButton, new ViewpointPlacePanel(this, set.placement));
            EditorGUI.EndDisabledGroup();
            if (Event.current.type == EventType.Repaint) vpPlaceButton = GUILayoutUtility.GetLastRect();
            if (touring)
            {
                // Held down and greyed out until the tour ends.
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Toggle(true, measure, button, GUILayout.ExpandWidth(false));
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(set.points.Count == 0 || ProfilerCapture.IsRunning);
                if (GUILayout.Button(measure, dropDown, GUILayout.ExpandWidth(false)))
                    PopupWindow.Show(vpTourButton, new ViewpointTourPanel(this, vpPicked.Count, set.points.Count));
                EditorGUI.EndDisabledGroup();
                if (Event.current.type == EventType.Repaint) vpTourButton = GUILayoutUtility.GetLastRect();
            }
            if (twoRows)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            }
            if (GUILayout.Button(addHere, button, GUILayout.ExpandWidth(false)))
                AddViewpointHere();
            EditorGUI.BeginDisabledGroup(!set.HasEstimate);
            if (GUILayout.Button(csv, button, GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += SaveViewpointsCsv;
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            vpWalls = GUILayout.Toggle(vpWalls, walls, button, GUILayout.ExpandWidth(false));
            vpOnlyOver = GUILayout.Toggle(vpOnlyOver, onlyOver, button, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
        }

        void DrawViewpointsEmpty()
        {
            if (vpSurvey == null) vpSurvey = ViewpointPlacer.Look();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Strings.Get("ui.vp.empty.title"), emptyTitle);
            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(40f);
            GUILayout.Label(Strings.Get("ui.vp.empty.text"), emptyText);
            GUILayout.Space(40f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var button = GUILayoutUtility.GetRect(240f, 36f, GUILayout.Width(240f), GUILayout.Height(36f));
            if (GUI.Button(button, Strings.Get("ui.vp.empty.button")))
                PopupWindow.Show(button, new ViewpointPlacePanel(this, Viewpoints().placement));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8f);
            var yes = Strings.Get("ui.vp.yes");
            var no = Strings.Get("ui.vp.no");
            GUILayout.Label(Strings.Format("ui.vp.found", vpSurvey.navMesh ? yes : no,
                vpSurvey.teleportPoints + vpSurvey.teleportAreas, vpSurvey.cameras), centeredMini);
            GUILayout.Label(Strings.Get("ui.vp.empty.manual"), centeredMini);
            GUILayout.FlexibleSpace();
        }

        void DrawViewpointsSummary(ViewpointSet set)
        {
            var estimated = set.points.Where(p => p.Estimated).ToList();
            string summary;
            if (estimated.Count == 0) summary = Strings.Format("ui.vp.summary.notEstimated", set.points.Count);
            else
            {
                int over = estimated.Count(p => ViewpointEstimator.LevelOf(p.Worst, budgets) != null);
                var worst = estimated.OrderByDescending(p => p.Worst.drawCalls).ThenByDescending(p => p.Worst.tris).First();
                summary = Strings.Format("ui.vp.summary", set.points.Count, over, worst.Name, FormatNumber(worst.Worst.drawCalls));
            }
            var parts = new List<string>();
            var kinds = new[]
            {
                new KeyValuePair<ViewpointKind, string>(ViewpointKind.NavMesh, "ui.vp.sources.navmesh"),
                new KeyValuePair<ViewpointKind, string>(ViewpointKind.Teleport, "ui.vp.sources.teleport"),
                new KeyValuePair<ViewpointKind, string>(ViewpointKind.Camera, "ui.vp.sources.camera"),
                new KeyValuePair<ViewpointKind, string>(ViewpointKind.Ray, "ui.vp.sources.ray"),
                new KeyValuePair<ViewpointKind, string>(ViewpointKind.Manual, "ui.vp.sources.manual")
            };
            foreach (var kind in kinds)
            {
                int n = set.points.Count(p => p.kind == kind.Key);
                if (n > 0) parts.Add(Strings.Format(kind.Value, n));
            }
            if (set.estimatedAt.Length > 0)
            {
                DateTime at;
                if (DateTime.TryParse(set.estimatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out at))
                    parts.Add(Strings.Format("ui.vp.estimated", DateText(at), FormatNumber(set.estimateSeconds)));
                parts.Add(Strings.Get(set.estimateWalls ? "ui.vp.walls.on" : "ui.vp.walls.off"));
            }
            if (set.placedStep > set.placement.step + 0.01f) parts.Add(Strings.Format("ui.vp.stepGrown", FormatNumber(Math.Round(set.placedStep, 1))));
            DrawSummaryRow(summary, string.Join("  ·  ", parts.ToArray()));
        }

        void DrawViewpointsSelectionBar(ViewpointSet set)
        {
            vpPicked.RemoveWhere(id => set.Find(id) == null);
            if (vpPicked.Count == 0) return;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Format("ui.vp.sel.count", vpPicked.Count), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.sel.clear"), EditorStyles.miniButton, GUILayout.ExpandWidth(false))) vpPicked.Clear();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            // An estimate in the middle of a tour would stall its frames.
            EditorGUI.BeginDisabledGroup(ViewpointTour.IsRunning);
            if (GUILayout.Button(Strings.Format("ui.vp.sel.estimate", vpPicked.Count), GUILayout.ExpandWidth(false)))
            {
                var only = set.points.Where(p => vpPicked.Contains(p.id)).ToList();
                EditorApplication.delayCall += () => EstimateViewpoints(only);
            }
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.vp.sel.remove", vpPicked.Count), Strings.Get("ui.vp.sel.remove.tip")), GUILayout.ExpandWidth(false)))
                RemoveViewpoints(vpPicked.ToList());
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // Worst first: critical, warning, within budget, not estimated; then by draw calls and triangles.
        List<Viewpoint> SortedViewpoints(ViewpointSet set)
        {
            return set.points
                .OrderBy(p => !p.Estimated ? 3 : RankOf(ViewpointEstimator.LevelOf(p.Worst, budgets)))
                .ThenByDescending(p => p.Estimated ? p.Worst.drawCalls : 0)
                .ThenByDescending(p => p.Estimated ? p.Worst.tris : 0)
                .ToList();
        }

        static int RankOf(Severity? level)
        {
            if (level == Severity.Critical) return 0;
            return level == Severity.Warning ? 1 : 2;
        }

        float[] ViewpointWidths(float total)
        {
            bool narrow = total < VpNarrowWidth;
            var fixedWidths = narrow ? new[] { 62f, 80f, 72f, 78f, 90f } : new[] { 84f, 98f, 86f, 94f, 104f };
            float name = Mathf.Max(90f, total - VpCheck - fixedWidths.Sum() - 8f);
            return new[] { VpCheck, name }.Concat(fixedWidths).ToArray();
        }

        Rect[] ViewpointColumns(Rect rect)
        {
            var widths = ViewpointWidths(rect.width);
            var cells = new Rect[widths.Length];
            float x = rect.x + 5f;
            for (int i = 0; i < widths.Length; i++)
            {
                cells[i] = new Rect(x, rect.y, widths[i] - 4f, rect.height);
                x += widths[i];
            }
            return cells;
        }

        void DrawViewpointsTable(ViewpointSet set)
        {
            var rows = SortedViewpoints(set);
            if (vpOnlyOver) rows = rows.Where(p => p.Estimated && ViewpointEstimator.LevelOf(p.Worst, budgets) != null).ToList();
            var head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var headCells = ViewpointColumns(head);
            if (rows.Count > 0)
            {
                bool all = rows.All(p => vpPicked.Contains(p.id));
                bool now = GUI.Toggle(new Rect(headCells[0].x, headCells[0].y + 2f, 16f, 16f), all, new GUIContent("", Strings.Get("ui.sel.all.tip")));
                if (now != all)
                    foreach (var p in rows)
                        if (now) vpPicked.Add(p.id);
                        else vpPicked.Remove(p.id);
            }
            var keys = new[] { "", "ui.vp.col.point", "ui.vp.col.kind", "ui.vp.col.tris", "ui.vp.col.dc", "ui.vp.col.transparent", "ui.vp.col.dir" };
            for (int i = 1; i < keys.Length; i++)
                GUI.Label(headCells[i], new GUIContent(Strings.Get(keys[i]), Strings.Get(keys[i] + ".tip")), EditorStyles.miniBoldLabel);
            int shown = Math.Min(rows.Count, vpShown);
            for (int i = 0; i < shown; i++) DrawViewpointRow(rows[i], i);
            if (rows.Count > shown)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8f + VpCheck);
                if (GUILayout.Button(Strings.Format("ui.showMore", Math.Min(VpPageSize, rows.Count - shown)), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    vpShown += VpPageSize;
                EditorGUILayout.EndHorizontal();
            }
            if (rows.Count == 0)
            {
                GUILayout.Space(10f);
                GUILayout.Label(Strings.Get("ui.vp.allGood"), centeredMini);
            }
        }

        void DrawViewpointRow(Viewpoint p, int index)
        {
            var rect = Padded(GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true)));
            var level = p.Estimated ? ViewpointEstimator.LevelOf(p.Worst, budgets) : null;
            bool selected = p.id == vpSelectedId;
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, selected ? SelectionColor : Tint(EditorGUIUtility.isProSkin ? Color.white : Color.black, index % 2 == 0 ? 0.04f : 0.07f));
                if (level.HasValue) EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), SeverityColor(level.Value));
            }
            var cells = ViewpointColumns(rect);
            bool on = vpPicked.Contains(p.id);
            bool now = GUI.Toggle(new Rect(cells[0].x, cells[0].y + 3f, 16f, 16f), on, new GUIContent("", Strings.Get("ui.sel.pick.tip")));
            if (now && !on) vpPicked.Add(p.id);
            else if (!now && on) vpPicked.Remove(p.id);
            var name = new GUIContent(Fit(p.Name, linkClipped, cells[1].width), p.Name + "\n" + Strings.Get("ui.vp.col.point.tip"));
            if (GUI.Button(new Rect(cells[1].x, cells[1].y + 3f, cells[1].width, 16f), name, linkClipped)) SelectViewpoint(p, true);
            EditorGUIUtility.AddCursorRect(cells[1], MouseCursor.Link);
            GUI.Label(new Rect(cells[2].x, cells[2].y + 3f, cells[2].width, 16f), Strings.Get("ui.vp.kind." + p.kind), EditorStyles.miniLabel);
            var w = p.Worst;
            if (w == null)
            {
                for (int i = 3; i < cells.Length; i++) Cell(cells[i], "—", null);
            }
            else
            {
                Cell(cells[3], FormatNumber(w.tris), ValueColor(w.tris, budgets.sceneTrisWarning, budgets.sceneTrisCritical));
                Cell(cells[4], FormatNumber(w.drawCalls), ValueColor(w.drawCalls, budgets.drawCallsWarning, budgets.drawCallsCritical));
                Cell(cells[5], FormatNumber(w.transparentPercent) + " %", ValueColor(w.transparentPercent, budgets.transparentWarningPercent, budgets.transparentCriticalPercent));
                Cell(cells[6], DirectionLabel(w.yaw), null);
            }
            // A click anywhere else on the row selects it too.
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition) && !cells[0].Contains(e.mousePosition))
            {
                SelectViewpoint(p, true);
                e.Use();
            }
        }

        // Text cut with an ellipsis to fit the width; the full text goes to the tooltip.
        static string Fit(string text, GUIStyle style, float width)
        {
            if (style.CalcSize(new GUIContent(text)).x <= width) return text;
            for (int n = text.Length - 1; n > 1; n--)
            {
                var cut = text.Substring(0, n) + "…";
                if (style.CalcSize(new GUIContent(cut)).x <= width) return cut;
            }
            return "…";
        }

        static Color? ValueColor(double value, double warning, double critical)
        {
            if (value > critical) return TextCritical;
            return value > warning ? TextWarning : (Color?)null;
        }

        // "180° (−Z)": 0° looks along +Z, 90° along +X.
        static string DirectionLabel(float yaw)
        {
            int degrees = Mathf.RoundToInt(Mathf.Repeat(yaw, 360f)) % 360;
            switch (degrees)
            {
                case 0: return "0° (+Z)";
                case 90: return "90° (+X)";
                case 180: return "180° (−Z)";
                case 270: return "270° (−X)";
                default: return degrees + "°";
            }
        }

        // The direction shown in the details and in the Scene view: picked by the user, else the worst one.
        int DirectionOf(Viewpoint p)
        {
            if (!p.Estimated) return -1;
            return vpDirection >= 0 && vpDirection < p.directions.Count ? vpDirection : p.worst;
        }

        void SelectViewpoint(Viewpoint p, bool look)
        {
            if (vpSelectedId != p.id) vpDirection = -1;
            vpSelectedId = p.id;
            vpHeavy = null;
            if (look) LookFrom(p.position, ShownYaw(p));
            Repaint();
            SceneView.RepaintAll();
        }

        // Puts the Scene view camera at the eye looking along the yaw; the Scene view field of view is not changed.
        static void LookFrom(Vector3 eye, float yaw)
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null) return;
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            const float distance = 1f;
            float size = distance * Mathf.Sin(view.cameraSettings.fieldOfView * 0.5f * Mathf.Deg2Rad);
            view.LookAt(eye + rotation * Vector3.forward * distance, rotation, size, false, false);
            view.Repaint();
        }

        void DrawViewpointDetails(ViewpointSet set)
        {
            var p = set.Find(vpSelectedId);
            if (p == null) return;
            GUILayout.Space(8f);
            var line = Padded(GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true)));
            if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(line, new Color(0f, 0f, 0f, 0.35f));
            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(p.Name, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            Func<float, string> f = v => FormatNumber(Math.Round(v, 1));
            GUILayout.Label(Strings.Format("ui.vp.meta", Strings.Get("ui.vp.kind." + p.kind), f(p.position.x), f(p.position.y), f(p.position.z)), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(ViewpointTour.IsRunning);
            if (GUILayout.Button(new GUIContent(Strings.Get("ui.vp.remove"), Strings.Get("ui.vp.sel.remove.tip")), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                RemoveViewpoints(new List<string> { p.id });
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            if (!p.Estimated)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8f);
                GUILayout.Label(Strings.Get("ui.vp.notEstimated"), wrapMini);
                EditorGUILayout.EndHorizontal();
                return;
            }
            bool twoColumns = position.width >= VpTwoColumnsWidth;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            if (twoColumns)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(344f));
                DrawDirections(p);
                EditorGUILayout.EndVertical();
                GUILayout.Space(16f);
                EditorGUILayout.BeginVertical();
                DrawHeavy(p);
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical();
                DrawDirections(p);
                GUILayout.Space(10f);
                DrawHeavy(p);
                EditorGUILayout.EndVertical();
            }
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void DrawDirections(Viewpoint p)
        {
            GUILayout.Label(new GUIContent(Strings.Get("ui.vp.dirs"), Strings.Get("ui.vp.dirs.tip")), EditorStyles.boldLabel);
            var head = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
            var widths = new[] { 0f, 92f, 76f, 84f };
            widths[0] = Mathf.Max(90f, head.width - widths.Skip(1).Sum());
            var keys = new[] { "ui.vp.col.az", "ui.vp.col.tris", "ui.vp.col.dc", "ui.vp.col.transparent" };
            float x = head.x + 4f;
            for (int i = 0; i < keys.Length; i++)
            {
                GUI.Label(new Rect(x, head.y, widths[i] - 4f, head.height), Strings.Get(keys[i]), EditorStyles.miniBoldLabel);
                x += widths[i];
            }
            int shown = DirectionOf(p);
            for (int i = 0; i < p.directions.Count; i++)
            {
                var d = p.directions[i];
                var rect = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(rect, i == shown ? SelectionColor : Tint(EditorGUIUtility.isProSkin ? Color.white : Color.black, i % 2 == 0 ? 0.04f : 0.07f));
                x = rect.x + 4f;
                var label = DirectionLabel(d.yaw) + (i == p.worst ? "  " + Strings.Get("ui.vp.worst") : "");
                GUI.Label(new Rect(x, rect.y + 2f, widths[0] - 4f, 16f), label, i == p.worst ? EditorStyles.boldLabel : EditorStyles.label);
                x += widths[0];
                DirectionCell(new Rect(x, rect.y, widths[1] - 4f, rect.height), FormatNumber(d.tris), ValueColor(d.tris, budgets.sceneTrisWarning, budgets.sceneTrisCritical));
                x += widths[1];
                DirectionCell(new Rect(x, rect.y, widths[2] - 4f, rect.height), FormatNumber(d.drawCalls), ValueColor(d.drawCalls, budgets.drawCallsWarning, budgets.drawCallsCritical));
                x += widths[2];
                DirectionCell(new Rect(x, rect.y, widths[3] - 4f, rect.height), FormatNumber(d.transparentPercent) + " %",
                    ValueColor(d.transparentPercent, budgets.transparentWarningPercent, budgets.transparentCriticalPercent));
                var e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
                {
                    vpDirection = i;
                    vpHeavy = null;
                    LookFrom(p.position, d.yaw);
                    SceneView.RepaintAll();
                    e.Use();
                }
            }
        }

        void DirectionCell(Rect rect, string text, Color? color)
        {
            var style = new GUIStyle(EditorStyles.label);
            if (color.HasValue) style.normal.textColor = color.Value;
            GUI.Label(new Rect(rect.x, rect.y + 2f, rect.width, 16f), text, style);
        }

        void DrawHeavy(Viewpoint p)
        {
            int d = DirectionOf(p);
            var key = p.id + "|" + d + "|" + vpByDrawCalls + "|" + vpWalls + "|" + vpSet.estimatedAt;
            if (vpHeavy == null || vpHeavyKey != key)
            {
                vpHeavy = ViewpointEstimator.Heavy(p, d, budgets, vpWalls, vpByDrawCalls);
                vpHeavyKey = key;
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Format("ui.vp.heavy", DirectionLabel(p.directions[d].yaw)), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            var sorts = new[] { Strings.Get("ui.vp.sort.tris"), Strings.Get("ui.vp.sort.dc") };
            int sort = GUILayout.Toolbar(vpByDrawCalls ? 1 : 0, sorts, EditorStyles.miniButton, GUILayout.ExpandWidth(false));
            if ((sort == 1) != vpByDrawCalls)
            {
                vpByDrawCalls = sort == 1;
                vpHeavy = null;
            }
            EditorGUILayout.EndHorizontal();
            if (vpHeavy == null) return;
            if (vpHeavy.Count == 0) GUILayout.Label(Strings.Get("ui.vp.heavy.none"), EditorStyles.miniLabel);
            for (int i = 0; i < vpHeavy.Count; i++)
            {
                var entry = vpHeavy[i];
                var rect = GUILayoutUtility.GetRect(0f, entry.marks.Count > 0 || entry.transparentPercent > 0f ? 36f : 22f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(rect, Tint(EditorGUIUtility.isProSkin ? Color.white : Color.black, i % 2 == 0 ? 0.04f : 0.07f));
                var numbers = Strings.Format("ui.vp.heavy.tris", FormatNumber(entry.tris)) + " · " + Strings.Format("ui.vp.heavy.dc", FormatNumber(entry.drawCalls));
                var numbersSize = EditorStyles.label.CalcSize(new GUIContent(numbers));
                float right = rect.xMax - 8f - numbersSize.x;
                var titleRect = new Rect(rect.x + 6f, rect.y + 3f, Mathf.Max(40f, right - rect.x - 12f), 16f);
                var title = new GUIContent(Fit(entry.label + "  ×" + entry.copies, linkClipped, titleRect.width), entry.key + "\n" + Strings.Get("ui.vp.heavy.tip"));
                if (GUI.Button(titleRect, title, linkClipped))
                {
                    Selection.objects = entry.objects.Where(o => o != null).Cast<UnityEngine.Object>().ToArray();
                    if (entry.objects.Count > 0 && entry.objects[0] != null) EditorGUIUtility.PingObject(entry.objects[0]);
                }
                EditorGUIUtility.AddCursorRect(titleRect, MouseCursor.Link);
                GUI.Label(new Rect(right, rect.y + 3f, numbersSize.x, 16f), numbers, EditorStyles.label);
                float chipX = rect.x + 6f;
                if (entry.transparentPercent > 0f)
                    chipX = Chip(chipX, rect.y + 19f, Strings.Format("ui.vp.heavy.screen", FormatNumber(entry.transparentPercent) + " %"), null);
                foreach (var mark in entry.marks)
                    chipX = Chip(chipX, rect.y + 19f, Strings.Get("ui.vp.mark." + mark), Strings.Get("ui.vp.mark." + mark + ".tip"));
            }
        }

        // A small label on a soft background; returns where the next one starts.
        float Chip(float x, float y, string text, string tip)
        {
            var content = new GUIContent(text, tip);
            var size = EditorStyles.miniLabel.CalcSize(content);
            var rect = new Rect(x, y, size.x + 10f, 15f);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, Tint(EditorGUIUtility.isProSkin ? Color.white : Color.black, 0.09f));
            GUI.Label(new Rect(rect.x + 5f, rect.y, size.x, rect.height), content, EditorStyles.miniLabel);
            return rect.xMax + 6f;
        }

        // ---- actions ----

        // Applied by the placement panel: new automatic viewpoints replace the old ones, manual ones stay.
        internal void PlaceViewpoints(PlacementSettings settings)
        {
            var set = Viewpoints();
            float step;
            var auto = ViewpointPlacer.Place(settings, out step);
            Undo.RecordObject(set, "Place Viewpoints");
            set.placement = settings;
            set.ReplaceAuto(auto, step);
            set.Save();
            vpPicked.Clear();
            vpHeavy = null;
            vpSurvey = null;
            vpShown = VpPageSize;
            ShowNotification(new GUIContent(Strings.Format("ui.vp.placed", set.points.Count)));
            Repaint();
            SceneView.RepaintAll();
        }

        void EstimateViewpoints(List<Viewpoint> only)
        {
            var set = Viewpoints();
            var which = only ?? set.points;
            if (which.Count == 0) return;
            bool done;
            try
            {
                done = ViewpointEstimator.Estimate(set, which, vpWalls,
                    (name, share) => EditorUtility.DisplayCancelableProgressBar(Strings.Get("ui.vp.progress"), name, share), budgets);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (!done) return;
            set.Save();
            vpHeavy = null;
            if (set.Find(vpSelectedId) == null || !set.Find(vpSelectedId).Estimated)
            {
                var worst = SortedViewpoints(set).FirstOrDefault();
                if (worst != null) SelectViewpoint(worst, false);
            }
            Repaint();
            SceneView.RepaintAll();
        }

        void AddViewpointHere()
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null || view.camera == null)
            {
                ShowNotification(new GUIContent(Strings.Get("ui.vp.noSceneView")));
                return;
            }
            var set = Viewpoints();
            Undo.RecordObject(set, "Add Viewpoint");
            var p = set.AddManual(view.camera.transform.position, view.camera.transform.eulerAngles.y);
            set.Save();
            SelectViewpoint(p, false);
        }

        // Removing is not asked about: Ctrl+Z brings the viewpoints back.
        void RemoveViewpoints(List<string> ids)
        {
            var set = Viewpoints();
            Undo.RecordObject(set, "Remove Viewpoints");
            set.points.RemoveAll(p => ids.Contains(p.id));
            set.Save();
            foreach (var id in ids) vpPicked.Remove(id);
            vpHeavy = null;
            Repaint();
            SceneView.RepaintAll();
        }

        void SaveViewpointsCsv()
        {
            var set = Viewpoints();
            var path = EditorUtility.SaveFilePanel(Strings.Get("ui.lm.csv"), "", "viewpoints-" + DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture), "csv");
            if (string.IsNullOrEmpty(path)) return;
            ViewpointCsv.Save(set, budgets, path);
            ShowNotification(new GUIContent(Strings.Format("ui.export.done", path)));
        }

        // Used by the screenshot test: places viewpoints from every source, estimates them and selects the worst.
        void ViewpointsForShot()
        {
            var set = Viewpoints();
            float step;
            var settings = new PlacementSettings { navMesh = true, teleports = true, cameras = true, rays = true, step = 6f };
            set.placement = settings;
            set.ReplaceAuto(ViewpointPlacer.Place(settings, out step), step);
            ViewpointEstimator.Estimate(set, set.points, false, null, budgets);
            set.Save();
            var worst = SortedViewpoints(set).FirstOrDefault();
            if (worst != null) SelectViewpoint(worst, false);
        }

        // ---- Scene view ----

        void OnViewpointsSceneGui(SceneView view)
        {
            if (tab != TabViewpoints || vpSet == null) return;
            var set = vpSet;
            foreach (var p in set.points)
            {
                var level = p.Estimated ? ViewpointEstimator.LevelOf(p.Worst, budgets) : null;
                Handles.color = !p.Estimated ? InfoColor : level.HasValue ? SeverityColor(level.Value) : OkColor;
                float size = HandleUtility.GetHandleSize(p.position) * 0.16f;
                if (Handles.Button(p.position, Quaternion.identity, size, size, Handles.SphereHandleCap)) SelectViewpoint(p, false);
            }

            var selected = set.Find(vpSelectedId);
            if (selected != null) DrawSelectedInScene(set, selected);

            if (vpDirty && GUIUtility.hotControl == 0)
            {
                set.Save();
                vpDirty = false;
                vpHeavy = null;
                Repaint();
            }
            DrawViewpointsLegend();
        }

        void DrawSelectedInScene(ViewpointSet set, Viewpoint p)
        {
            var rotation = Quaternion.Euler(0f, ShownYaw(p), 0f);
            float handle = HandleUtility.GetHandleSize(p.position);
            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = Color.white;
                Handles.ArrowHandleCap(0, p.position, rotation, handle * 1.1f, EventType.Repaint);
                float halfHeight = Mathf.Tan(budgets.Headset.fieldOfView * 0.5f * Mathf.Deg2Rad) * VpFrustumMeters;
                float halfWidth = halfHeight * budgets.Headset.aspect;
                var corners = new[]
                {
                    new Vector3(-halfWidth, -halfHeight, VpFrustumMeters), new Vector3(halfWidth, -halfHeight, VpFrustumMeters),
                    new Vector3(halfWidth, halfHeight, VpFrustumMeters), new Vector3(-halfWidth, halfHeight, VpFrustumMeters)
                }.Select(c => p.position + rotation * c).ToArray();
                var level = p.Estimated ? ViewpointEstimator.LevelOf(p.Worst, budgets) : null;
                var w = p.Worst;
                var text = w != null ? Strings.Format("ui.vp.label", p.Name, FormatNumber(w.drawCalls), DirectionLabel(w.yaw)) : p.Name;
                // With "Measured" on, the colour and the label follow the measured direction shown.
                var tour = vpMeasured ? TourOf(set) : null;
                var m = tour != null ? tour.Find(p.id) : null;
                int md = MeasuredDirectionOf(m);
                if (md >= 0)
                {
                    var d = m.directions[md];
                    level = TourMath.LevelOf(d, tour.headset, budgets);
                    text = Strings.Format("ui.vp.label.tour", p.Name, Ms(TourMath.Cost(d, tour.headset)), DirectionLabel(d.yaw));
                }
                Handles.color = level.HasValue ? SeverityColor(level.Value) : OkColor;
                for (int i = 0; i < 4; i++)
                {
                    Handles.DrawLine(p.position, corners[i]);
                    Handles.DrawLine(corners[i], corners[(i + 1) % 4]);
                }
                var style = new GUIStyle(EditorStyles.whiteMiniLabel) { normal = { background = Texture2D.blackTexture } };
                Handles.Label(p.position + Vector3.up * handle * 0.4f + rotation * Vector3.right * handle * 0.4f, " " + text + " ", style);
            }
            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(p.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(set, "Move Viewpoint");
                p.position = moved;
                set.MakeManual(p);
                p.ClearEstimate();
                // The tour of the old place says nothing about the new one.
                set.tour.points.RemoveAll(m => m.id == p.id);
                vpDirty = true;
            }
        }

        void DrawViewpointsLegend()
        {
            Handles.BeginGUI();
            var rows = new[]
            {
                new KeyValuePair<Color, string>(OkColor, "ui.vp.legend.ok"),
                new KeyValuePair<Color, string>(WarningColor, "ui.vp.legend.warning"),
                new KeyValuePair<Color, string>(CriticalColor, "ui.vp.legend.critical"),
                new KeyValuePair<Color, string>(InfoColor, "ui.vp.legend.none")
            };
            var area = new Rect(10f, 10f, 240f, 132f);
            GUILayout.BeginArea(area, EditorStyles.helpBox);
            GUILayout.Label(Strings.Get("ui.vp.legend"), EditorStyles.boldLabel);
            foreach (var row in rows)
            {
                var rect = GUILayoutUtility.GetRect(0f, 16f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(new Rect(rect.x + 2f, rect.y + 4f, 9f, 9f), row.Key);
                GUI.Label(new Rect(rect.x + 18f, rect.y, rect.width - 18f, rect.height), Strings.Get(row.Value), EditorStyles.miniLabel);
            }
            GUILayout.Label(Strings.Get("ui.vp.legend.note"), wrapMini);
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}
