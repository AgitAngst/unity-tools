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
        // ---- lighting ----

        const int LtAtlases = 0;
        const int LtObjects = 1;
        const int LtLights = 2;
        const int LtProbes = 3;
        const int LtReflections = 4;
        const int LtPageSize = 20;

        [SerializeField] int ltSeg;
        [SerializeField] bool ltOnlyMarked;
        LightReport ltReport;
        readonly HashSet<LightObject> ltPicked = new HashSet<LightObject>();
        int ltShown = LtPageSize;
        int ltOutsideShown = LtPageSize;
        int ltReflOutsideShown = LtPageSize;

        void ScanLight()
        {
            ltReport = LightReport.Build();
            ltPicked.Clear();
            ltShown = LtPageSize;
            ltOutsideShown = LtPageSize;
            ltReflOutsideShown = LtPageSize;
            Repaint();
        }

        // Small objects still lit from lightmaps: what "Small Ones to Probes" would switch.
        List<LightObject> SmallInLightmaps()
        {
            if (ltReport == null) return new List<LightObject>();
            return ltReport.objects.Where(o => o.small && o.renderer != null && o.renderer.receiveGI == ReceiveGI.Lightmaps).ToList();
        }

        void DrawLightTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Get("ui.lt.intro"), wrapMini);
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            DrawLightToolbar();
            if (ltReport == null)
            {
                DrawLightEmpty();
                return;
            }
            pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
            var labels = new[] { "ui.lt.seg.atlases", "ui.lt.seg.objects", "ui.lt.seg.lights", "ui.lt.seg.probes", "ui.lt.seg.reflections" }
                .Select(k => new GUIContent(Strings.Get(k), Strings.Get("ui.lt.seg.tip"))).ToArray();
            string summary, sub, footer;
            LightTexts(out summary, out sub, out footer);
            int seg = SegmentRow(ltSeg, labels, summary, sub);
            if (seg != ltSeg)
            {
                ltSeg = seg;
                ltShown = LtPageSize;
            }
            GUILayout.Space(4f);
            switch (ltSeg)
            {
                case LtObjects: DrawLightObjects(); break;
                case LtLights: DrawLightSources(); break;
                case LtProbes: DrawLightProbes(); break;
                case LtReflections: DrawLightReflections(); break;
                default: DrawLightAtlases(); break;
            }
            GUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(footer, wrapMini);
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.EndScrollView();
        }

        // Actions on the left, the filter on the right; a second row when they do not fit.
        void DrawLightToolbar()
        {
            var small = SmallInLightmaps();
            var check = new GUIContent(" " + Strings.Get("ui.lt.check"), playIcon, Strings.Get("ui.lt.check.tip"));
            var toProbes = new GUIContent(Strings.Format("ui.lt.toProbesAll", small.Count), Strings.Get("ui.lt.toProbes.tip"));
            var open = new GUIContent(Strings.Get("ui.lt.openLighting"), Strings.Get("ui.lt.openLighting.tip"));
            var marked = new GUIContent(Strings.Get("ui.lt.onlyMarked"));
            var button = EditorStyles.toolbarButton;
            bool twoRows = button.CalcSize(check).x + button.CalcSize(toProbes).x + button.CalcSize(open).x + button.CalcSize(marked).x + 24f > position.width;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(check, button, GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += ScanLight;
            EditorGUI.BeginDisabledGroup(small.Count == 0);
            if (GUILayout.Button(toProbes, button, GUILayout.ExpandWidth(false)))
            {
                var fixes = LightReport.ToProbesFixes(small);
                EditorApplication.delayCall += () => AutoFixer.Confirm(fixes, () => EditorApplication.delayCall += ScanLight);
            }
            EditorGUI.EndDisabledGroup();
            if (twoRows)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            }
            if (GUILayout.Button(open, button, GUILayout.ExpandWidth(false)))
                OpenSettings(SceneAuditor.LightingWindow);
            GUILayout.FlexibleSpace();
            ltOnlyMarked = GUILayout.Toggle(ltOnlyMarked, marked, button, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
        }

        void DrawLightEmpty()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label(Strings.Get("ui.lt.emptyTitle"), emptyTitle);
            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(40f);
            GUILayout.Label(Strings.Get("ui.lt.emptyText"), emptyText);
            GUILayout.Space(40f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.lt.check"), GUILayout.Width(240f), GUILayout.Height(36f)))
                EditorApplication.delayCall += ScanLight;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        void LightTexts(out string summary, out string sub, out string footer)
        {
            var r = ltReport;
            switch (ltSeg)
            {
                case LtObjects:
                    summary = Strings.Format("ui.lt.objects.summary", r.objects.Count, r.objects.Count(o => o.small), r.objects.Count(o => o.much), r.NoLightmap);
                    sub = Strings.Format("ui.lt.objects.sub", FormatNumber(VRBudgets.SmallLightmapMeters));
                    footer = Strings.Get("ui.lt.objects.footer");
                    return;
                case LtLights:
                    // A light deleted since the check is skipped until the next one.
                    var alive = r.sources.Where(s => s.light != null).ToList();
                    int realtime = alive.Count(s => s.light.lightmapBakeType == LightmapBakeType.Realtime);
                    int shadowed = alive.Count(s => s.light.shadows != LightShadows.None && s.light.lightmapBakeType != LightmapBakeType.Baked);
                    summary = Strings.Format("ui.lt.lights.summary", alive.Count, realtime, shadowed, alive.Count(s => s.wide));
                    sub = Strings.Format("ui.lt.lights.sub", r.pixelLightCount, r.sources.Count(s => s.important), r.NotBakedLights);
                    footer = Strings.Format("ui.lt.lights.footer", VRBudgets.WideLightRenderers);
                    return;
                case LtProbes:
                    summary = Strings.Format("ui.lt.probes.summary", FormatNumber(r.probes), r.groups.Count, r.probeObjects, r.outside.Count);
                    sub = Strings.Format("ui.lt.probes.sub", FormatNumber(VRBudgets.ProbeAreaMargin));
                    footer = Strings.Get("ui.lt.probes.footer");
                    return;
                case LtReflections:
                    var probes = r.reflections.Where(p => p.probe != null).ToList();
                    summary = probes.Count == 0 ? Strings.Get("ui.lt.refl.none")
                        : Strings.Format("ui.lt.refl.summary", probes.Count, FormatNumber(Math.Round(r.ReflectionMB, 1)), probes.Count(p => p.noBox && !p.probe.boxProjection),
                            probes.Count(p => p.notBaked), r.outsideReflection.Count);
                    sub = Strings.Format("ui.lt.refl.sub", FormatNumber(VRBudgets.RoomProbeMeters));
                    footer = Strings.Get("ui.lt.refl.footer");
                    return;
                default:
                    summary = r.atlases.Count == 0 ? Strings.Get("ui.lt.atlases.none")
                        : Strings.Format("ui.lt.atlases.summary", r.atlases.Count, FormatNumber(Math.Round(r.MB, 1)), budgets.lightmapMemoryWarningMB);
                    sub = BakeSettingsText(r);
                    footer = Strings.Get("ui.lt.atlases.footer");
                    return;
            }
        }

        static string BakeSettingsText(LightReport r)
        {
            var parts = new List<string> { r.lightmapper, Strings.Format("ui.lt.set.size", r.atlasSize), Strings.Format("ui.lt.set.resolution", FormatNumber(r.resolution)) };
            parts.Add(Strings.Get(r.directional ? "ui.lt.set.directional" : "ui.lt.set.nonDirectional"));
            parts.Add(Strings.Get(r.compressed ? "ui.lt.set.compressed" : "ui.lt.set.uncompressed"));
            parts.Add(Strings.Format("ui.lt.set.mixed", LightReport.Spaced(r.mixedMode.ToString())));
            parts.Add(Strings.Format("ui.lt.set.shadowmask", LightReport.Spaced(r.shadowmaskMode.ToString())));
            if (!r.bakedGI) parts.Add(Strings.Get("ui.lt.set.noBakedGI"));
            return string.Join("  ·  ", parts.ToArray());
        }

        // Columns of a table: the first one takes what the fixed ones leave; cells keep a 4 px gap.
        static Rect[] LightColumns(Rect rect, float[] fixedWidths, float lead)
        {
            float first = Mathf.Max(90f, rect.width - lead - fixedWidths.Sum() - 8f);
            var widths = new[] { lead, first }.Concat(fixedWidths).ToArray();
            var cells = new Rect[widths.Length];
            float x = rect.x + 5f;
            for (int i = 0; i < widths.Length; i++)
            {
                cells[i] = new Rect(x, rect.y, widths[i] - 4f, rect.height);
                x += widths[i];
            }
            return cells;
        }

        void LightHeader(Rect[] cells, int from, string[] keys, bool[] right)
        {
            if (vpRightHead == null) vpRightHead = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].Length == 0) continue;
                var parts = keys[i].Split('|');
                var tip = parts.Length > 1 && Strings.Has(parts[1] + ".tip") ? Strings.Get(parts[1] + ".tip") : Strings.Has(parts[0] + ".tip") ? Strings.Get(parts[0] + ".tip") : "";
                var cell = cells[from + i];
                GUI.Label(new Rect(cell.x, cell.y, cell.width - 4f, cell.height), new GUIContent(Strings.Get(parts[0]), tip), right[i] ? vpRightHead : EditorStyles.miniBoldLabel);
            }
        }

        Rect LightRow(int index, bool selected, Color? stripe, float height)
        {
            var rect = Padded(GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true)));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, selected ? SelectionColor : Tint(EditorGUIUtility.isProSkin ? Color.white : Color.black, index % 2 == 0 ? 0.04f : 0.07f));
                if (stripe.HasValue) EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), stripe.Value);
            }
            return rect;
        }

        // A link to a scene object that leaves room for the marks after it; returns where the marks start.
        float ObjectLink(Rect cell, UnityEngine.Object target, string name, string tip, float reserve)
        {
            var nameRect = new Rect(cell.x, cell.y + 3f, Mathf.Max(30f, cell.width - reserve), 16f);
            var fitted = Fit(name, linkClipped, nameRect.width);
            nameRect.width = Mathf.Min(nameRect.width, linkClipped.CalcSize(new GUIContent(fitted)).x);
            if (GUI.Button(nameRect, new GUIContent(fitted, string.IsNullOrEmpty(tip) ? name : name + "\n" + tip), linkClipped) && target != null)
            {
                Selection.activeObject = target;
                EditorGUIUtility.PingObject(target);
            }
            EditorGUIUtility.AddCursorRect(nameRect, MouseCursor.Link);
            return nameRect.xMax + 6f;
        }

        static float MarksWidth(IEnumerable<string> marks)
        {
            float width = 0f;
            foreach (var m in marks) width += EditorStyles.miniLabel.CalcSize(new GUIContent(m)).x + 16f;
            return width;
        }

        void ShowMore(ref int shown, int total)
        {
            if (total <= shown) return;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f + VpCheck);
            if (GUILayout.Button(Strings.Format("ui.showMore", Math.Min(LtPageSize, total - shown)), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                shown += LtPageSize;
            EditorGUILayout.EndHorizontal();
        }

        void CenteredNote(string key)
        {
            GUILayout.Space(10f);
            GUILayout.Label(Strings.Get(key), centeredMini);
        }

        // ---- atlases ----

        void DrawLightAtlases()
        {
            var r = ltReport;
            if (r.atlases.Count == 0)
            {
                CenteredNote("ui.lt.atlases.empty");
                return;
            }
            BudgetBar(Strings.Get("ui.lt.memory"), Math.Round(r.MB, 1), budgets.lightmapMemoryWarningMB, budgets.lightmapMemoryCriticalMB, Strings.Get("ui.mb"));
            GUILayout.Space(4f);
            bool narrow = position.width < VpNarrowWidth;
            var widths = narrow ? new[] { 86f, 56f, 76f, 70f } : new[] { 100f, 80f, 70f, 86f, 90f };
            var keys = narrow
                ? new[] { "ui.lt.col.size", "ui.lt.col.mb", "ui.lt.col.count", "ui.lt.col.filled.short|ui.lt.col.filled" }
                : new[] { "ui.lt.col.size", "ui.lt.col.format", "ui.lt.col.mb", "ui.lt.col.count", "ui.lt.col.filled" };
            var head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var headCells = LightColumns(head, widths, 0f);
            LightHeader(headCells, 1, new[] { "ui.lt.col.atlas" }.Concat(keys).ToArray(), new[] { false }.Concat(keys.Select(k => true)).ToArray());
            for (int i = 0; i < r.atlases.Count; i++)
            {
                var a = r.atlases[i];
                var rect = LightRow(i, false, null, 22f);
                var cells = LightColumns(rect, widths, 0f);
                var name = new GUIContent("#" + a.index, Strings.Get("ui.lt.col.atlas.tip"));
                var nameRect = new Rect(cells[1].x, cells[1].y + 3f, Mathf.Min(cells[1].width, 60f), 16f);
                if (GUI.Button(nameRect, name, linkClipped))
                {
                    int index = a.index;
                    Selection.objects = r.objects.Where(o => o.atlas == index && o.renderer != null).Select(o => (UnityEngine.Object)o.renderer.gameObject).ToArray();
                }
                EditorGUIUtility.AddCursorRect(nameRect, MouseCursor.Link);
                var size = a.width + " × " + a.height;
                var values = narrow
                    ? new[] { a.width + "×" + a.height, FormatNumber(Math.Round(a.bytes / (1024.0 * 1024.0), 1)), FormatNumber(a.objects), Percent(a.filled) }
                    : new[] { size, a.format, FormatNumber(Math.Round(a.bytes / (1024.0 * 1024.0), 1)), FormatNumber(a.objects), Percent(a.filled) };
                for (int c = 0; c < values.Length; c++) RightCell(cells[c + 2], values[c], null);
            }
        }

        // ---- objects ----

        List<LightObject> LightObjectRows()
        {
            var rows = ltReport.objects.OrderByDescending(o => o.Marked).ThenByDescending(o => o.share).ToList();
            return ltOnlyMarked ? rows.Where(o => o.Marked).ToList() : rows;
        }

        void DrawLightObjects()
        {
            var r = ltReport;
            if (r.objects.Count == 0)
            {
                CenteredNote(r.bakedGI ? "ui.lt.objects.none" : "ui.lt.set.noBakedGI");
                return;
            }
            // The selection is cleared on every check, so only objects deleted since can be stale.
            ltPicked.RemoveWhere(o => o.renderer == null);
            DrawLightSelectionBar();
            var rows = LightObjectRows();
            bool narrow = position.width < VpNarrowWidth;
            var widths = narrow ? new[] { 50f, 56f, 52f, 76f } : new[] { 60f, 70f, 60f, 80f, 90f };
            var keys = narrow
                ? new[] { "ui.lt.col.atlas", "ui.lt.col.share", "ui.lt.col.meters.short|ui.lt.col.meters", "" }
                : new[] { "ui.lt.col.atlas", "ui.lt.col.share", "ui.lt.col.scale", "ui.lt.col.meters", "" };
            var head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var headCells = LightColumns(head, widths, VpCheck);
            // The header box picks the rows shown, not those behind "Show more".
            var shownRows = rows.Take(ltShown).ToList();
            if (shownRows.Count > 0)
            {
                bool all = shownRows.All(ltPicked.Contains);
                bool now = GUI.Toggle(new Rect(headCells[0].x, headCells[0].y + 2f, 16f, 16f), all, new GUIContent("", Strings.Get("ui.sel.all.tip")));
                if (now != all)
                    foreach (var o in shownRows)
                        if (now) ltPicked.Add(o);
                        else ltPicked.Remove(o);
            }
            LightHeader(headCells, 1, new[] { "ui.lt.col.object" }.Concat(keys).ToArray(), new[] { false }.Concat(keys.Select(k => true)).ToArray());
            int shown = Math.Min(rows.Count, ltShown);
            for (int i = 0; i < shown; i++) DrawLightObjectRow(rows[i], i, narrow, widths);
            ShowMore(ref ltShown, rows.Count);
            if (rows.Count == 0) CenteredNote("ui.lt.objects.allGood");
        }

        void DrawLightSelectionBar()
        {
            if (ltPicked.Count == 0) return;
            // Counted here, built only on a click: the bar is drawn every frame.
            int switchable = ltPicked.Count(o => o.renderer != null && o.renderer.receiveGI == ReceiveGI.Lightmaps);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Format("ui.lt.sel.count", ltPicked.Count), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.sel.clear"), EditorStyles.miniButton, GUILayout.ExpandWidth(false))) ltPicked.Clear();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(switchable == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.lt.sel.toProbes", switchable), Strings.Get("ui.lt.toProbes.tip")), GUILayout.ExpandWidth(false)))
            {
                var fixes = LightReport.ToProbesFixes(ltPicked);
                EditorApplication.delayCall += () => AutoFixer.Confirm(fixes, () => EditorApplication.delayCall += ScanLight);
            }
            EditorGUI.EndDisabledGroup();
            var objects = ltPicked.Where(o => o.renderer != null).Select(o => (UnityEngine.Object)o.renderer.gameObject).ToArray();
            if (GUILayout.Button(Strings.Format("ui.lt.sel.select", objects.Length), GUILayout.ExpandWidth(false)))
                MultiSelect.Select(objects);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void DrawLightObjectRow(LightObject o, int index, bool narrow, float[] widths)
        {
            // Warnings in yellow, a small object (Info) in grey.
            var stripe = o.much || o.noLightmap ? WarningColor : o.small ? InfoColor : (Color?)null;
            var rect = LightRow(index, false, stripe, 22f);
            var cells = LightColumns(rect, widths, VpCheck);
            bool on = ltPicked.Contains(o);
            bool now = GUI.Toggle(new Rect(cells[0].x, cells[0].y + 3f, 16f, 16f), on, new GUIContent("", Strings.Get("ui.sel.pick.tip")));
            if (now && !on) ltPicked.Add(o);
            else if (!now && on) ltPicked.Remove(o);

            var marks = new List<string>();
            if (o.much) marks.Add(Strings.Get("ui.lt.mark.much"));
            if (o.noLightmap) marks.Add(Strings.Get("ui.lt.mark.noLightmap"));
            if (o.small) marks.Add(Strings.Get("ui.lt.mark.small"));
            var go = o.renderer != null ? o.renderer.gameObject : null;
            float x = ObjectLink(cells[1], go, go != null ? go.name : "—", go != null ? AuditContext.PathOf(go.transform) : "", MarksWidth(marks));
            foreach (var m in marks) x = Chip(x, rect.y + 4f, m, null);

            var share = o.noLightmap ? "—" : Percent(o.share);
            var values = narrow
                ? new[] { o.noLightmap ? "—" : "#" + o.atlas, share, FormatNumber(Math.Round(o.size, 2)) }
                : new[] { o.noLightmap ? "—" : "#" + o.atlas, share, FormatNumber(Math.Round(o.scale, 2)), FormatNumber(Math.Round(o.size, 2)) };
            for (int c = 0; c < values.Length; c++) RightCell(cells[c + 2], values[c], c == 1 && o.much ? TextWarning : (Color?)null);

            var action = cells[cells.Length - 1];
            if (o.small && o.renderer != null && o.renderer.receiveGI == ReceiveGI.Lightmaps &&
                GUI.Button(new Rect(action.x, action.y + 2f, action.width, 18f), new GUIContent(Strings.Get("ui.lt.toProbes"), Strings.Get("ui.lt.toProbes.tip")), EditorStyles.miniButton))
            {
                var fixes = LightReport.ToProbesFixes(new[] { o });
                EditorApplication.delayCall += () => AutoFixer.Confirm(fixes, () => EditorApplication.delayCall += ScanLight);
            }
        }

        // ---- lights ----

        void DrawLightSources()
        {
            var r = ltReport;
            if (r.sources.Count == 0)
            {
                CenteredNote("ui.lt.lights.none");
                return;
            }
            var rows = ltOnlyMarked ? r.sources.Where(s => s.Marked).ToList() : r.sources;
            bool narrow = position.width < VpNarrowWidth;
            var widths = narrow ? new[] { 72f, 62f, 62f, 56f } : new[] { 80f, 80f, 76f, 76f, 70f, 70f };
            var keys = narrow
                ? new[] { "ui.lt.col.mode", "ui.lt.col.shadows", "ui.lt.col.inRange", "ui.lt.col.baked.short|ui.lt.col.baked" }
                : new[] { "ui.lt.col.type", "ui.lt.col.mode", "ui.lt.col.shadows", "ui.lt.col.range", "ui.lt.col.inRange", "ui.lt.col.baked" };
            var right = narrow ? new[] { false, false, true, true } : new[] { false, false, false, true, true, true };
            var head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var headCells = LightColumns(head, widths, 0f);
            LightHeader(headCells, 1, new[] { "ui.lt.col.light" }.Concat(keys).ToArray(), new[] { false }.Concat(right).ToArray());
            int shown = Math.Min(rows.Count, ltShown);
            for (int i = 0; i < shown; i++)
            {
                var s = rows[i];
                var light = s.light;
                if (light == null) continue;
                var rect = LightRow(i, false, s.Marked ? WarningColor : (Color?)null, 22f);
                var cells = LightColumns(rect, widths, 0f);
                var marks = new List<string>();
                if (s.wide) marks.Add(Strings.Get("ui.lt.mark.wide"));
                if (s.important) marks.Add(Strings.Get("ui.lt.mark.important"));
                if (!s.baked) marks.Add(Strings.Get("ui.lt.mark.notBaked"));
                float x = ObjectLink(cells[1], light.gameObject, light.name, AuditContext.PathOf(light.transform), MarksWidth(marks));
                foreach (var m in marks) x = Chip(x, rect.y + 4f, m, null);

                bool local = light.type == LightType.Point || light.type == LightType.Spot;
                var mode = light.lightmapBakeType;
                var baked = mode == LightmapBakeType.Realtime ? "—" : Strings.Get(s.baked ? "ui.lt.yes" : "ui.lt.no");
                var all = new[]
                {
                    light.type.ToString(), mode.ToString(), Strings.Get("ui.lt.shadows." + light.shadows),
                    local ? FormatNumber(Math.Round(light.range, 1)) : "—", FormatNumber(s.inRange), baked
                };
                var values = narrow ? new[] { all[1], all[2], all[4], all[5] } : all;
                for (int c = 0; c < values.Length; c++)
                {
                    // The last two columns: objects in range and baked.
                    bool isRange = c == values.Length - 2;
                    bool isBaked = c == values.Length - 1;
                    Color? color = isRange && s.wide ? TextWarning : isBaked && !s.baked ? TextWarning : (Color?)null;
                    if (right[c]) RightCell(cells[c + 2], values[c], color);
                    else Cell(cells[c + 2], values[c], color);
                }
            }
            ShowMore(ref ltShown, rows.Count);
            if (rows.Count == 0) CenteredNote("ui.lt.lights.allGood");
        }

        // ---- probes ----

        void DrawLightProbes()
        {
            var r = ltReport;
            if (r.groups.Count == 0)
            {
                CenteredNote("ui.lt.probes.noGroups");
                return;
            }
            SubTitle("ui.lt.groups");
            var widths = new[] { 90f, 140f };
            var head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var headCells = LightColumns(head, widths, 0f);
            LightHeader(headCells, 1, new[] { "ui.lt.col.group", "ui.lt.col.probes", "ui.lt.col.box" }, new[] { false, true, true });
            for (int i = 0; i < r.groups.Count; i++)
            {
                var g = r.groups[i];
                if (g.group == null) continue;
                var rect = LightRow(i, false, null, 20f);
                var cells = LightColumns(rect, widths, 0f);
                ObjectLink(new Rect(cells[1].x, cells[1].y - 1f, cells[1].width, cells[1].height), g.group.gameObject, g.group.name, AuditContext.PathOf(g.group.transform), 0f);
                RightCell(new Rect(cells[2].x, cells[2].y - 1f, cells[2].width, cells[2].height), FormatNumber(g.probes), null);
                var box = FormatNumber(Math.Round(g.size.x, 1)) + " × " + FormatNumber(Math.Round(g.size.y, 1)) + " × " + FormatNumber(Math.Round(g.size.z, 1));
                RightCell(new Rect(cells[3].x, cells[3].y - 1f, cells[3].width, cells[3].height), box, null);
            }

            GUILayout.Space(10f);
            SubTitle("ui.lt.outside");
            if (r.outside.Count == 0)
            {
                CenteredNote("ui.lt.outside.none");
                return;
            }
            var outWidths = new[] { 130f, 130f };
            head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            headCells = LightColumns(head, outWidths, 0f);
            LightHeader(headCells, 1, new[] { "ui.lt.col.object", "ui.lt.col.usage", "ui.lt.col.nearest" }, new[] { false, true, true });
            int shown = Math.Min(r.outside.Count, ltOutsideShown);
            for (int i = 0; i < shown; i++)
            {
                var o = r.outside[i];
                if (o.renderer == null) continue;
                var rect = LightRow(i, false, WarningColor, 20f);
                var cells = LightColumns(rect, outWidths, 0f);
                ObjectLink(new Rect(cells[1].x, cells[1].y - 1f, cells[1].width, cells[1].height), o.renderer.gameObject, o.renderer.name, AuditContext.PathOf(o.renderer.transform), 0f);
                RightCell(new Rect(cells[2].x, cells[2].y - 1f, cells[2].width, cells[2].height), LightReport.Spaced(o.renderer.lightProbeUsage.ToString()), null);
                RightCell(new Rect(cells[3].x, cells[3].y - 1f, cells[3].width, cells[3].height), FormatNumber(Math.Round(o.distance, 1)), null);
            }
            ShowMore(ref ltOutsideShown, r.outside.Count);
        }

        // ---- reflections ----

        void DrawLightReflections()
        {
            var r = ltReport;
            if (r.reflections.Count == 0)
            {
                CenteredNote("ui.lt.refl.empty");
                return;
            }
            var rows = ltOnlyMarked ? r.reflections.Where(p => p.Marked).ToList() : r.reflections;
            // The last column holds "Box On": as wide as its label. The full table needs room for the names too.
            float action = EditorStyles.miniButton.CalcSize(new GUIContent(Strings.Get("ui.lt.boxOn"))).x + 8f;
            var full = new[] { 64f, 78f, 40f, 50f, 40f, 96f, 66f, action };
            bool narrow = position.width < VpNarrowWidth || position.width - 40f < full.Sum() + 120f;
            var widths = narrow ? new[] { 64f, 48f, 46f, 40f, 58f, action } : full;
            var keys = narrow
                ? new[] { "ui.lt.col.mode", "ui.lt.col.resolution.short|ui.lt.col.resolution", "ui.lt.col.mb|ui.lt.col.reflMb", "ui.lt.col.boxProjection",
                    "ui.lt.col.objects.short|ui.lt.col.objects", "" }
                : new[] { "ui.lt.col.mode", "ui.lt.col.resolution", "ui.lt.col.hdr", "ui.lt.col.mb|ui.lt.col.reflMb", "ui.lt.col.boxProjection",
                    "ui.lt.col.meters|ui.lt.col.reflSize", "ui.lt.col.objects", "" };
            var right = narrow ? new[] { false, true, true, false, true, false } : new[] { false, true, false, true, false, true, true, false };
            var head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var headCells = LightColumns(head, widths, 0f);
            LightHeader(headCells, 1, new[] { "ui.lt.col.probe" }.Concat(keys).ToArray(), new[] { false }.Concat(right).ToArray());
            int shown = Math.Min(rows.Count, ltShown);
            for (int i = 0; i < shown; i++) DrawReflectionRow(rows[i], i, narrow, widths, right);
            ShowMore(ref ltShown, rows.Count);
            if (rows.Count == 0) CenteredNote("ui.lt.refl.allGood");

            GUILayout.Space(10f);
            SubTitle("ui.lt.reflOutside");
            if (r.outsideReflection.Count == 0)
            {
                CenteredNote("ui.lt.reflOutside.none");
                return;
            }
            var outWidths = new[] { narrow ? 150f : 170f, 110f };
            head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            headCells = LightColumns(head, outWidths, 0f);
            LightHeader(headCells, 1, new[] { "ui.lt.col.object", "ui.lt.col.reflUsage", "ui.lt.col.nearest" }, new[] { false, true, true });
            int outShown = Math.Min(r.outsideReflection.Count, ltReflOutsideShown);
            for (int i = 0; i < outShown; i++)
            {
                var o = r.outsideReflection[i];
                if (o.renderer == null) continue;
                var rect = LightRow(i, false, InfoColor, 20f);
                var cells = LightColumns(rect, outWidths, 0f);
                ObjectLink(new Rect(cells[1].x, cells[1].y - 1f, cells[1].width, cells[1].height), o.renderer.gameObject, o.renderer.name, AuditContext.PathOf(o.renderer.transform), 0f);
                RightCell(new Rect(cells[2].x, cells[2].y - 1f, cells[2].width, cells[2].height), LightReport.Spaced(o.renderer.reflectionProbeUsage.ToString()), null);
                RightCell(new Rect(cells[3].x, cells[3].y - 1f, cells[3].width, cells[3].height), FormatNumber(Math.Round(o.distance, 1)), null);
            }
            ShowMore(ref ltReflOutsideShown, r.outsideReflection.Count);
        }

        void DrawReflectionRow(ReflectionRow row, int index, bool narrow, float[] widths, bool[] right)
        {
            var probe = row.probe;
            if (probe == null) return;
            // Box projection may have been turned on since the check.
            bool noBox = row.noBox && !probe.boxProjection;
            bool critical = row.resolution >= VRBudgets.ReflectionResolutionCritical;
            var stripe = critical ? CriticalColor : row.Large || row.notBaked || row.Duplicate ? WarningColor : noBox ? InfoColor : (Color?)null;
            var rect = LightRow(index, false, stripe, 22f);
            var cells = LightColumns(rect, widths, 0f);
            var marks = new List<string>();
            if (row.Duplicate) marks.Add(Strings.Get("ui.lt.mark.duplicate"));
            if (noBox) marks.Add(Strings.Get("ui.lt.mark.noBox"));
            if (row.notBaked) marks.Add(Strings.Get("ui.lt.mark.probeNotBaked"));
            float x = ObjectLink(cells[1], probe.gameObject, probe.name, AuditContext.PathOf(probe.transform), MarksWidth(marks));
            foreach (var m in marks) x = Chip(x, rect.y + 4f, m, null);

            var size = row.bounds.size;
            var mb = (row.estimated ? "≈" : "") + FormatNumber(Math.Round(row.MB, 1));
            var box = Strings.Get(probe.boxProjection ? "ui.lt.yes" : "ui.lt.no");
            var all = new[]
            {
                row.mode.ToString(), row.resolution.ToString(), Strings.Get(row.hdr ? "ui.lt.yes" : "ui.lt.no"), mb, box,
                FormatNumber(Math.Round(size.x, 1)) + " × " + FormatNumber(Math.Round(size.y, 1)) + " × " + FormatNumber(Math.Round(size.z, 1)),
                FormatNumber(row.objects)
            };
            var values = narrow ? new[] { all[0], all[1], all[3], all[4], all[6] } : all;
            int boxColumn = narrow ? 3 : 4;
            for (int c = 0; c < values.Length; c++)
            {
                Color? color = c == 1 && critical ? TextCritical : c == 1 && row.Large ? TextWarning : c == boxColumn && noBox ? TextWarning : (Color?)null;
                if (right[c]) RightCell(cells[c + 2], values[c], color);
                else Cell(cells[c + 2], values[c], color);
            }

            var action = cells[cells.Length - 1];
            if (noBox && GUI.Button(new Rect(action.x, action.y + 2f, action.width, 18f), new GUIContent(Strings.Get("ui.lt.boxOn"), Strings.Get("ui.lt.boxOn.tip")), EditorStyles.miniButton))
            {
                var fixes = LightReport.BoxFixes(new[] { row });
                EditorApplication.delayCall += () => AutoFixer.Confirm(fixes, () => EditorApplication.delayCall += ScanLight);
            }
        }

        void SubTitle(string key)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Get(key), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        // Used by the screenshot test: checks lighting and shows a segment.
        void LightForShot(string segment)
        {
            ScanLight();
            int index = Array.IndexOf(new[] { "atlases", "objects", "lights", "probes", "reflections" }, segment);
            ltSeg = index >= 0 ? index : 0;
            if (ltSeg == LtObjects) foreach (var o in LightObjectRows().Where(o => o.small).Take(3)) ltPicked.Add(o);
        }
    }
}
