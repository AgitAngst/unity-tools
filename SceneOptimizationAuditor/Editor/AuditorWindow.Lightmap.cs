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
        // ---- lightmap UVs ----

        const float LmFlag = 86f;
        const float LmUv = 56f;
        const float LmOutside = 64f;
        const float LmOverlap = 84f;
        const float LmObjects = 120f;
        const float LmAction = 140f;
        const float LmCheck = 22f;

        void ScanLightmapUvs()
        {
            lmRows = LightmapUvReport.Build();
            lmPicked.Clear();
            Repaint();
        }

        void DrawLightmapTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Get("ui.lm.intro"), wrapMini);
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(new GUIContent(" " + Strings.Get("ui.lm.scan"), playIcon, Strings.Get("ui.lm.scan.tip")), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += ScanLightmapUvs;
            var turnOn = lmRows != null ? LightmapUvReport.TurnOnFixes(lmRows) : new List<AuditIssue>();
            EditorGUI.BeginDisabledGroup(turnOn.Count == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.lm.enableAll", turnOn.Count), Strings.Get("ui.fix.tip")), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += () => AutoFixer.Confirm(turnOn, () => EditorApplication.delayCall += ScanLightmapUvs);
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(lmRows == null || lmRows.Count == 0);
            if (GUILayout.Button(Strings.Get("ui.lm.csv"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += SaveLightmapCsv;
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            lmOnlyProblems = GUILayout.Toggle(lmOnlyProblems, Strings.Get("ui.lm.onlyProblems"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();

            if (lmRows == null)
            {
                GUILayout.Space(16f);
                GUILayout.Label(Strings.Get("ui.lm.notScanned"), centeredMini);
                return;
            }
            int noUv = lmRows.Count(r => !r.hasUv1);
            int problems = lmRows.Count(r => r.HasProblem);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Format("ui.lm.summary", lmRows.Count, noUv, lmRows.Count(r => r.GenerateOn), problems), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            var visible = lmOnlyProblems ? lmRows.Where(r => r.HasProblem).ToList() : lmRows;
            DrawLightmapSelectionBar();
            DrawLightmapHeader(visible);
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            for (int i = 0; i < visible.Count; i++) DrawLightmapRow(visible[i], i);
            if (visible.Count == 0)
            {
                GUILayout.Space(12f);
                GUILayout.Label(Strings.Get(lmRows.Count == 0 ? "ui.lm.none" : "ui.lm.allGood"), centeredMini);
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawLightmapSelectionBar()
        {
            if (lmPicked.Count == 0) return;
            var turnOn = LightmapUvReport.TurnOnFixes(lmPicked);
            var assets = MultiSelect.AssetsOf(lmPicked);
            var objects = MultiSelect.SceneObjectsOf(lmPicked);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Format("ui.lm.sel.count", lmPicked.Count), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.sel.clear"), EditorStyles.miniButton, GUILayout.ExpandWidth(false))) lmPicked.Clear();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(turnOn.Count == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.lm.sel.enable", turnOn.Count), Strings.Get("ui.fix.tip")), GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += () => AutoFixer.Confirm(turnOn, () => EditorApplication.delayCall += ScanLightmapUvs);
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(assets.Length == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.lm.sel.assets", assets.Length), Strings.Get("ui.lm.sel.assets.tip")), GUILayout.ExpandWidth(false)))
                MultiSelect.Select(assets);
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(objects.Length == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.lm.sel.objects", objects.Length), Strings.Get("ui.lm.sel.objects.tip")), GUILayout.ExpandWidth(false)))
                MultiSelect.Select(objects);
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // Checkbox and model columns first; the model column takes what the fixed columns leave.
        Rect[] LightmapColumns(Rect rect)
        {
            float fixedWidth = LmCheck + LmFlag + LmUv + LmOutside + LmOverlap + LmObjects + LmAction;
            float model = Mathf.Max(120f, rect.width - fixedWidth - 8f);
            float x = rect.x + 8f;
            var cells = new Rect[8];
            var widths = new[] { LmCheck, model, LmFlag, LmUv, LmOutside, LmOverlap, LmObjects, LmAction };
            for (int i = 0; i < widths.Length; i++)
            {
                cells[i] = new Rect(x, rect.y, widths[i] - 4f, rect.height);
                x += widths[i];
            }
            return cells;
        }

        void DrawLightmapHeader(List<LightmapUvRow> visible)
        {
            var rect = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var cells = LightmapColumns(rect);
            if (visible.Count > 0)
            {
                bool all = visible.All(lmPicked.Contains);
                bool now = GUI.Toggle(new Rect(cells[0].x, cells[0].y + 2f, 16f, 16f), all, new GUIContent("", Strings.Get("ui.lm.sel.all.tip")));
                if (now != all)
                    foreach (var row in visible)
                        if (now) lmPicked.Add(row);
                        else lmPicked.Remove(row);
            }
            var keys = new[] { "", "ui.lm.col.model", "ui.lm.col.flag", "ui.lm.col.uv1", "ui.lm.col.outside", "ui.lm.col.overlap", "ui.lm.col.objects", "" };
            for (int i = 0; i < keys.Length; i++)
                if (keys[i].Length > 0) GUI.Label(cells[i], new GUIContent(Strings.Get(keys[i]), Strings.Get(keys[i] + ".tip")), EditorStyles.miniBoldLabel);
        }

        void DrawLightmapRow(LightmapUvRow row, int index)
        {
            var rect = Padded(GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true)));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, Tint(EditorGUIUtility.isProSkin ? Color.white : Color.black, index % 2 == 0 ? 0.04f : 0.07f));
                if (row.HasProblem) EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), row.hasUv1 ? WarningColor : CriticalColor);
            }
            var cells = LightmapColumns(rect);
            bool on = lmPicked.Contains(row);
            bool now = GUI.Toggle(new Rect(cells[0].x, cells[0].y + 3f, 16f, 16f), on, new GUIContent("", Strings.Get("ui.sel.pick.tip")));
            if (now && !on) lmPicked.Add(row);
            else if (!now && on) lmPicked.Remove(row);
            var name = new GUIContent(row.name, string.IsNullOrEmpty(row.assetPath) ? Strings.Get("ui.lm.builtIn") : row.assetPath);
            if (GUI.Button(new Rect(cells[1].x, cells[1].y + 3f, cells[1].width, 16f), name, link) && row.asset != null)
            {
                Selection.activeObject = row.asset;
                EditorGUIUtility.PingObject(row.asset);
            }
            EditorGUIUtility.AddCursorRect(cells[1], MouseCursor.Link);

            Cell(cells[2], row.importer == null ? "—" : Strings.Get(row.GenerateOn ? "ui.lm.on" : "ui.lm.off"),
                row.importer == null ? (Color?)null : row.GenerateOn ? TextOk : row.hasUv1 ? (Color?)null : TextCritical);
            Cell(cells[3], Strings.Get(row.hasUv1 ? "ui.lm.yes" : "ui.lm.no"), row.hasUv1 ? TextOk : TextCritical);
            Cell(cells[4], row.hasUv1 ? Percent(row.outsideShare) : "—", row.outsideShare > 0f ? TextWarning : (Color?)null);
            Cell(cells[5], row.hasUv1 ? Percent(row.overlapShare) : "—", row.overlapShare > VRBudgets.LightmapUvOverlapMinShare ? TextWarning : (Color?)null);
            Cell(cells[6], Strings.Format("ui.lm.objects", row.objects, row.bakedObjects), null);
            if (row.CanTurnOn && GUI.Button(new Rect(cells[7].x, cells[7].y + 2f, cells[7].width, 18f),
                    new GUIContent(Strings.Get("ui.lm.enable"), Strings.Get("ui.fix.tip")), EditorStyles.miniButton))
            {
                var fixes = LightmapUvReport.TurnOnFixes(new[] { row });
                EditorApplication.delayCall += () => AutoFixer.Confirm(fixes, () => EditorApplication.delayCall += ScanLightmapUvs);
            }
        }

        static string Percent(float share)
        {
            return share <= 0f ? "0" : FormatNumber(Math.Round(share * 100.0, share < 0.1f ? 1 : 0)) + "%";
        }

        void Cell(Rect rect, string text, Color? color)
        {
            var style = new GUIStyle(EditorStyles.label);
            if (color.HasValue) style.normal.textColor = color.Value;
            GUI.Label(new Rect(rect.x, rect.y + 3f, rect.width, 16f), text, style);
        }

        void SaveLightmapCsv()
        {
            if (lmRows == null) return;
            var path = EditorUtility.SaveFilePanel(Strings.Get("ui.lm.csv"), "", "lightmap-uv-" + DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture), "csv");
            if (string.IsNullOrEmpty(path)) return;
            LightmapUvReport.SaveCsv(lmRows, path);
            ShowNotification(new GUIContent(Strings.Format("ui.export.done", path)));
        }
    }
}
