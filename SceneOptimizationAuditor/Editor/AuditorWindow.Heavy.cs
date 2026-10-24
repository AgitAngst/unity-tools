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
        // ---- heaviest sources ----

        const int HvPageSize = 20;

        // 0 — triangles, 1 — draw calls, 2 — textures.
        [SerializeField] int hvMeasure;
        [SerializeField] bool hvOnlyNoLod;
        HeavyReport hvReport;
        int hvShown = HvPageSize;
        // Source whose copies were selected by a click.
        string hvSelected = "";

        void CollectHeavy()
        {
            hvReport = HeavyReport.Build(budgets.objectTrisWarning);
            hvShown = HvPageSize;
            Repaint();
        }

        void DrawHeavyTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Get("ui.hv.intro"), wrapMini);
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(new GUIContent(" " + Strings.Get("ui.hv.collect"), playIcon, Strings.Get("ui.hv.collect.tip")), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += CollectHeavy;
            EditorGUI.BeginDisabledGroup(hvReport == null || hvReport.sources.Count == 0);
            if (GUILayout.Button(Strings.Get("ui.lm.csv"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += SaveHeavyCsv;
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            hvOnlyNoLod = GUILayout.Toggle(hvOnlyNoLod, Strings.Get("ui.hv.onlyNoLod"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();

            if (hvReport == null)
            {
                DrawHeavyEmpty();
                return;
            }
            pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
            var r = hvReport;
            var labels = new[] { "ui.hv.seg.tris", "ui.hv.seg.dc", "ui.hv.seg.tex" }
                .Select(k => new GUIContent(Strings.Get(k), Strings.Get("ui.hv.seg.tip"))).ToArray();
            string summary = r.sources.Count == 0 ? Strings.Get("ui.hv.none")
                : Strings.Format("ui.hv.summary." + hvMeasure, FormatNumber(r.sources.Count), Math.Min(10, r.sources.Count), FormatNumber(Math.Round(r.TopShare(hvMeasure, 10) * 100.0)));
            int measure = SegmentRow(hvMeasure, labels, summary, Strings.Get("ui.hv.sub"));
            if (measure != hvMeasure)
            {
                hvMeasure = measure;
                hvShown = HvPageSize;
            }
            GUILayout.Space(4f);
            DrawHeavyTable(r);
            GUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Get("ui.hv.footer"), wrapMini);
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.EndScrollView();
        }

        void DrawHeavyEmpty()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label(Strings.Get("ui.hv.emptyTitle"), emptyTitle);
            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(40f);
            GUILayout.Label(Strings.Get("ui.hv.emptyText"), emptyText);
            GUILayout.Space(40f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.hv.collect"), GUILayout.Width(240f), GUILayout.Height(36f)))
                EditorApplication.delayCall += CollectHeavy;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        void DrawHeavyTable(HeavyReport r)
        {
            var rows = r.Sorted(hvMeasure);
            if (hvOnlyNoLod) rows = rows.Where(r.NoLod).ToList();
            if (rows.Count == 0)
            {
                // With no sources at all the summary already says so.
                if (r.sources.Count > 0) CenteredNote("ui.hv.allLod");
                return;
            }
            bool narrow = position.width < VpNarrowWidth;
            var widths = narrow ? new[] { 50f, 78f, 50f, 50f, 52f } : new[] { 64f, 56f, 76f, 96f, 76f, 56f, 76f, 56f };
            var keys = narrow
                ? new[] { "ui.hv.col.copies", "ui.hv.col.trisTotal.short|ui.hv.col.trisTotal", "ui.hv.col.dc.short|ui.hv.col.dc", "ui.hv.col.mb.short|ui.hv.col.mb", "ui.hv.col.share" }
                : new[] { "ui.hv.col.kind", "ui.hv.col.copies", "ui.hv.col.perCopy", "ui.hv.col.trisTotal", "ui.hv.col.dc", "ui.hv.col.materials", "ui.hv.col.mb", "ui.hv.col.share" };
            var right = narrow ? new[] { true, true, true, true, true } : new[] { false, true, true, true, true, true, true, true };
            var head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var headCells = LightColumns(head, widths, 0f);
            LightHeader(headCells, 1, new[] { "ui.hv.col.source" }.Concat(keys).ToArray(), new[] { false }.Concat(right).ToArray());
            int shown = Math.Min(rows.Count, hvShown);
            for (int i = 0; i < shown; i++)
            {
                var s = rows[i];
                bool noLod = r.NoLod(s);
                var rect = LightRow(i, s.key == hvSelected, noLod ? WarningColor : (Color?)null, 22f);
                var cells = LightColumns(rect, widths, 0f);
                var marks = noLod ? new List<string> { Strings.Get("ui.hv.mark.noLod") } : new List<string>();
                // The name shows the source in Project; the rest of the row selects the copies in the scene.
                float x = ObjectLink(cells[1], s.asset, s.label, s.assetPath, MarksWidth(marks));
                foreach (var m in marks) x = Chip(x, rect.y + 4f, m, Strings.Get("ui.hv.mark.noLod.tip"));
                var share = Percent((float)r.Share(s, hvMeasure));
                var all = new[]
                {
                    Strings.Get("ui.hv.kind." + s.kind), FormatNumber(s.Copies), FormatNumber(s.TrisPerCopy), FormatNumber(s.tris),
                    FormatNumber(s.drawCalls), FormatNumber(s.materials.Count), FormatNumber(Math.Round(s.TextureMB, 1)), share
                };
                var values = narrow ? new[] { all[1], all[3], all[4], all[6], all[7] } : all;
                for (int c = 0; c < values.Length; c++)
                {
                    if (right[c]) RightCell(cells[c + 2], values[c], null);
                    else Cell(cells[c + 2], values[c], null);
                }
                var e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
                {
                    hvSelected = s.key;
                    MultiSelect.Select(s.copies.Where(g => g != null).Cast<UnityEngine.Object>().ToArray());
                    e.Use();
                }
            }
            if (rows.Count > shown)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8f);
                if (GUILayout.Button(Strings.Format("ui.showMore", Math.Min(HvPageSize, rows.Count - shown)), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    hvShown += HvPageSize;
                EditorGUILayout.EndHorizontal();
            }
        }

        void SaveHeavyCsv()
        {
            if (hvReport == null) return;
            var path = EditorUtility.SaveFilePanel(Strings.Get("ui.lm.csv"), "", "heaviest-" + DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture), "csv");
            if (string.IsNullOrEmpty(path)) return;
            hvReport.SaveCsv(path);
            ShowNotification(new GUIContent(Strings.Format("ui.export.done", path)));
        }

        // Used by the screenshot test: collects the sources and shows a measure.
        void HeavyForShot(string measure)
        {
            CollectHeavy();
            int index = Array.IndexOf(new[] { "tris", "dc", "tex" }, measure);
            hvMeasure = index >= 0 ? index : 0;
        }
    }
}
