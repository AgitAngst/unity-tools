using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityTools.SceneOptimization
{
    public sealed partial class AuditorWindow
    {
        // ---- shadows by distance (Lighting tab, 2.6.0) ----

        const int LtShadows = 5;
        // Far objects marked in the Scene view at most.
        const int ShadowMarksMax = 300;
        static readonly Color ZoneColor = new Color32(0x6F, 0xA8, 0xE8, 0xFF);

        List<ShadowRow> shRows;
        string shMeasured = "";
        int shShown = LtPageSize;
        string shSearch = "";
        bool shDirty;

        // Collected with "Check Lighting", measured again when the point or the radius changes.
        List<ShadowRow> ShadowRows(ShadowZone zone)
        {
            if (shRows == null)
            {
                shRows = ShadowReport.Collect();
                shMeasured = "";
            }
            var key = zone.placed + "|" + zone.center.x + "|" + zone.center.y + "|" + zone.center.z + "|" + zone.radius;
            if (key != shMeasured)
            {
                ShadowReport.Measure(shRows, zone);
                shMeasured = key;
            }
            return shRows;
        }

        // The rows the search finds; the counts, the table and the button work on them.
        List<ShadowRow> FoundShadowRows(ShadowZone zone)
        {
            var rows = ShadowRows(zone);
            return string.IsNullOrEmpty(shSearch) ? rows : rows.Where(r => ShadowReport.Matches(r, shSearch)).ToList();
        }

        void ShadowTexts(out string summary, out string sub, out string footer)
        {
            var zone = Viewpoints().shadows;
            var rows = FoundShadowRows(zone);
            summary = zone.placed
                ? Strings.Format("ui.sh.summary", rows.Count, FormatNumber(zone.radius), rows.Count(r => r.far), rows.Count(r => ShadowReport.WillChange(r, zone)))
                : Strings.Format("ui.sh.summary.noPoint", rows.Count);
            if (!string.IsNullOrEmpty(shSearch)) summary += Strings.Format("ui.sh.summary.search", shSearch.Trim());
            sub = Strings.Get("ui.sh.sub");
            footer = Strings.Get("ui.sh.footer");
        }

        void DrawLightShadows()
        {
            var set = Viewpoints();
            var zone = set.shadows;
            var found = FoundShadowRows(zone);
            DrawShadowZonePanel(set, zone, found);
            if (ShadowRows(zone).Count == 0)
            {
                CenteredNote("ui.sh.none");
                return;
            }
            DrawShadowSearch();
            var rows = ltOnlyMarked ? found.Where(r => r.far).ToList() : found;
            bool narrow = position.width < VpNarrowWidth;
            var widths = narrow ? new[] { 66f, 90f, 54f, 44f, 84f } : new[] { 100f, 96f, 104f, 76f, 50f, 90f };
            var keys = narrow
                ? new[] { "ui.sh.col.distance.short|ui.sh.col.distance", "ui.sh.col.mode", "ui.sh.col.receive.short|ui.sh.col.receive", "ui.sh.col.gi", "" }
                : new[] { "ui.sh.col.distance", "ui.sh.col.tris", "ui.sh.col.mode", "ui.sh.col.receive", "ui.sh.col.gi", "" };
            var right = narrow ? new[] { true, false, false, false, false } : new[] { true, true, false, false, false, false };
            var head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var headCells = LightColumns(head, widths, 0f);
            LightHeader(headCells, 1, new[] { "ui.lt.col.object" }.Concat(keys).ToArray(), new[] { false }.Concat(right).ToArray());
            int shown = Math.Min(rows.Count, shShown);
            for (int i = 0; i < shown; i++) DrawShadowRow(rows[i], zone, i, narrow, widths);
            ShowMore(ref shShown, rows.Count);
            if (rows.Count == 0) CenteredNote("ui.sh.noneFound");
        }

        // The point, what to turn off, the radius, "Keep Cast Shadows on baked" and the button; edits go through Undo and
        // are saved with the viewpoints.
        void DrawShadowZonePanel(ViewpointSet set, ShadowZone zone, List<ShadowRow> rows)
        {
            var view = SceneView.lastActiveSceneView;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Get("ui.sh.point"), EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            if (zone.placed)
                GUILayout.Label(Strings.Format("ui.sh.point.value", FormatNumber(Math.Round(zone.center.x, 1)), FormatNumber(Math.Round(zone.center.y, 1)),
                    FormatNumber(Math.Round(zone.center.z, 1))), GUILayout.ExpandWidth(false));
            else GUILayout.Label(Strings.Get("ui.sh.point.none"), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(view == null);
            if (GUILayout.Button(new GUIContent(Strings.Get("ui.sh.center"), Strings.Get("ui.sh.center.tip")), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
            {
                Undo.RecordObject(set, "Shadow Point");
                zone.placed = true;
                zone.center = view.pivot;
                set.Save();
                SceneView.RepaintAll();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(view == null || !zone.placed);
            if (GUILayout.Button(Strings.Get("ui.sh.show"), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                view.Frame(new Bounds(zone.center, Vector3.one * Mathf.Min(zone.radius * 2f, 400f)), false);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Get("ui.sh.what"), GUILayout.ExpandWidth(false));
            bool offCast = ShadowToggle("ui.sh.cast", zone.offCast);
            bool offReceive = ShadowToggle("ui.sh.receive", zone.offReceive);
            bool offGI = ShadowToggle("ui.sh.gi", zone.offGI);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            var toChange = rows.Where(r => ShadowReport.WillChange(r, zone)).ToList();
            var turnOff = new GUIContent(Strings.Format("ui.sh.offAll", FormatNumber(zone.radius), toChange.Count), Strings.Get("ui.sh.offAll.tip"));
            // On a narrow window the button goes on its own row.
            bool wrap = position.width < 760f;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Get("ui.sh.radius"), GUILayout.ExpandWidth(false));
            float radius = EditorGUILayout.DelayedFloatField(zone.radius, GUILayout.Width(52f));
            GUILayout.Space(8f);
            EditorGUI.BeginDisabledGroup(!offCast);
            bool keepBaked = ShadowToggle("ui.sh.keepBaked", zone.keepBaked);
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            if (wrap)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
            }
            EditorGUI.BeginDisabledGroup(toChange.Count == 0);
            if (GUILayout.Button(turnOff, GUILayout.ExpandWidth(false)))
            {
                var fixes = ShadowReport.TurnOffFixes(toChange, zone);
                EditorApplication.delayCall += () => AutoFixer.Confirm(fixes, () => EditorApplication.delayCall += ScanLight);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);

            radius = Mathf.Clamp(radius, ShadowZone.MinRadius, ShadowZone.MaxRadius);
            if (!Mathf.Approximately(radius, zone.radius) || keepBaked != zone.keepBaked || offCast != zone.offCast || offReceive != zone.offReceive || offGI != zone.offGI)
            {
                Undo.RecordObject(set, "Shadow Zone");
                zone.radius = radius;
                zone.keepBaked = keepBaked;
                zone.offCast = offCast;
                zone.offReceive = offReceive;
                zone.offGI = offGI;
                set.Save();
                SceneView.RepaintAll();
            }
        }

        bool ShadowToggle(string key, bool value)
        {
            var content = new GUIContent(Strings.Get(key), Strings.Get(key + ".tip"));
            return EditorGUILayout.ToggleLeft(content, value, GUILayout.Width(EditorStyles.label.CalcSize(content).x + 20f));
        }

        void DrawShadowSearch()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(new GUIContent(Strings.Get("ui.search"), Strings.Get("ui.sh.search.tip")), GUILayout.ExpandWidth(false));
            var text = GUILayout.TextField(shSearch ?? "", searchField, GUILayout.MinWidth(140f), GUILayout.MaxWidth(320f));
            if (GUILayout.Button(GUIContent.none, searchCancel))
            {
                text = "";
                GUI.FocusControl(null);
            }
            if (text != shSearch)
            {
                shSearch = text;
                shShown = LtPageSize;
                SceneView.RepaintAll();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }

        void DrawShadowRow(ShadowRow row, ShadowZone zone, int index, bool narrow, float[] widths)
        {
            var rect = LightRow(index, false, ShadowReport.WillChange(row, zone) ? InfoColor : (Color?)null, 22f);
            var cells = LightColumns(rect, widths, 0f);
            var marks = new List<string>();
            if (row.far) marks.Add(Strings.Get("ui.sh.mark.far"));
            if (row.Baked) marks.Add(Strings.Get("ui.sh.mark.baked"));
            if (row.ShadowOnly) marks.Add(Strings.Get("ui.sh.mark.only"));
            var go = row.renderer != null ? row.renderer.gameObject : null;
            float x = ObjectLink(cells[1], go, go != null ? go.name : "—", row.path, MarksWidth(marks));
            foreach (var m in marks) x = Chip(x, rect.y + 4f, m, null);

            // Near objects: the distance in the colour of small text.
            int c = 2;
            RightCell(cells[c++], zone.placed ? FormatNumber(Math.Round(row.distance, 1)) : "—", row.far ? (Color?)null : EditorStyles.miniLabel.normal.textColor);
            if (!narrow) RightCell(cells[c++], FormatNumber(row.tris), null);
            Cell(cells[c++], Strings.Get(ModeKey(row.Mode)), null);
            Cell(cells[c++], Strings.Get(row.Receives ? "ui.lt.yes" : "ui.lt.no"), null);
            Cell(cells[c++], Strings.Get(row.ContributesGI ? "ui.lt.yes" : "ui.lt.no"), null);

            var action = cells[cells.Length - 1];
            if (ShadowReport.Changes(row, zone) &&
                GUI.Button(new Rect(action.x, action.y + 2f, action.width, 18f), new GUIContent(Strings.Get("ui.sh.offOne"), Strings.Get("ui.sh.offOne.tip")), EditorStyles.miniButton))
            {
                var fixes = ShadowReport.TurnOffFixes(new[] { row }, zone);
                EditorApplication.delayCall += () => AutoFixer.Confirm(fixes, () => EditorApplication.delayCall += ScanLight);
            }
        }

        static string ModeKey(ShadowCastingMode mode)
        {
            switch (mode)
            {
                case ShadowCastingMode.TwoSided: return "ui.sh.mode.two";
                case ShadowCastingMode.ShadowsOnly: return "ui.sh.mode.only";
                case ShadowCastingMode.Off: return "ui.sh.mode.off";
                default: return "ui.sh.mode.on";
            }
        }

        // The point with its handle, the radius on the ground and the far objects, while "Shadows" is open.
        void OnShadowsSceneGui(SceneView view)
        {
            if (tab != TabLight || ltSeg != LtShadows || ltReport == null || vpSet == null) return;
            var set = vpSet;
            var zone = set.shadows;
            if (!zone.placed) return;
            var rows = FoundShadowRows(zone);
            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = ZoneColor;
                Handles.DrawWireDisc(zone.center, Vector3.up, zone.radius);
                var facing = view.camera != null ? Quaternion.LookRotation(view.camera.transform.forward) : Quaternion.identity;
                int marked = 0;
                foreach (var row in rows)
                {
                    // Farthest first: the rest are inside the radius.
                    if (!row.far) break;
                    if (row.renderer == null) continue;
                    bool change = ShadowReport.WillChange(row, zone);
                    bool kept = !change && zone.offCast && zone.keepBaked && row.Baked && row.Casts && !ShadowReport.GIOff(row, zone);
                    if (!change && !kept) continue;
                    var p = row.renderer.bounds.center;
                    float size = HandleUtility.GetHandleSize(p) * 0.05f;
                    if (change)
                    {
                        Handles.color = InfoColor;
                        Handles.DotHandleCap(0, p, Quaternion.identity, size, EventType.Repaint);
                    }
                    else
                    {
                        Handles.color = Color.white;
                        Handles.CircleHandleCap(0, p, facing, size * 1.6f, EventType.Repaint);
                    }
                    if (++marked >= ShadowMarksMax) break;
                }
                var style = new GUIStyle(EditorStyles.whiteMiniLabel) { normal = { background = Texture2D.blackTexture } };
                float handle = HandleUtility.GetHandleSize(zone.center);
                Handles.Label(zone.center + Vector3.right * handle * 0.6f + Vector3.down * handle * 0.1f,
                    " " + Strings.Format("ui.sh.scene.label", FormatNumber(zone.radius), rows.Count(r => ShadowReport.WillChange(r, zone))) + " ", style);
            }
            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(zone.center, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(set, "Move Shadow Point");
                zone.center = moved;
                shDirty = true;
            }
            if (shDirty && GUIUtility.hotControl == 0)
            {
                set.Save();
                shDirty = false;
                Repaint();
            }
            DrawShadowsLegend();
        }

        void DrawShadowsLegend()
        {
            Handles.BeginGUI();
            var area = new Rect(10f, 10f, 260f, 118f);
            GUILayout.BeginArea(area, EditorStyles.helpBox);
            GUILayout.Label(Strings.Get("ui.sh.legend"), EditorStyles.boldLabel);
            LegendRow(ZoneColor, "ui.sh.legend.radius", true);
            LegendRow(InfoColor, "ui.sh.legend.off", false);
            LegendRow(Color.white, "ui.sh.legend.baked", true);
            GUILayout.Label(Strings.Get("ui.sh.legend.note"), wrapMini);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        // A filled square for a mark, an outlined one for a circle.
        void LegendRow(Color color, string key, bool outline)
        {
            var rect = GUILayoutUtility.GetRect(0f, 16f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                var box = new Rect(rect.x + 2f, rect.y + 4f, 9f, 9f);
                EditorGUI.DrawRect(box, color);
                if (outline) EditorGUI.DrawRect(new Rect(box.x + 2f, box.y + 2f, 5f, 5f), new Color(0.16f, 0.16f, 0.16f));
            }
            GUI.Label(new Rect(rect.x + 18f, rect.y, rect.width - 18f, rect.height), Strings.Get(key), EditorStyles.miniLabel);
        }
    }
}
