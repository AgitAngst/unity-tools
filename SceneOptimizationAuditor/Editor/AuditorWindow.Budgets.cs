using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public sealed partial class AuditorWindow
    {
        // ---- budgets ----

        void DrawBudgetBars(bool full)
        {
            if (result == null) return;
            var b = result.budgets;
            var f = result.frame;
            // A folder is checked in a hidden scene: no frame and no scene totals, texture memory only.
            bool folder = result.scope == ScopeKind.Folder;
            EditorGUILayout.BeginVertical();
            // Triangles and draw calls per frame at the worst viewpoints, against the budgets per frame (2.8.0); the whole
            // scene, which is no frame, only in the line under the bars.
            if (!folder && f != null)
            {
                BudgetBar(Strings.Get("ui.budget.frameTris"), f.tris, b.sceneTrisWarning, b.sceneTrisCritical, "");
                BudgetBar(Strings.Get("ui.budget.frameDrawCalls"), f.drawCalls, b.drawCallsWarning, b.drawCallsCritical, "");
            }
            else if (!folder)
            {
                EmptyBar(Strings.Get("ui.budget.frameTris"));
                EmptyBar(Strings.Get("ui.budget.frameDrawCalls"));
            }
            BudgetBar(Strings.Get("ui.budget.texMem"), result.textureMemoryMB, b.textureMemoryWarningMB, b.textureMemoryCriticalMB, Strings.Get("ui.mb"));
            EditorGUILayout.EndVertical();
            if (!folder) DrawFrameLine();
            if (!full) return;
            GUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Get("ui.budget.note"), hint);
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void BudgetBar(string label, double value, double warning, double critical, string unit)
        {
            var rect = Padded(EditorGUILayout.GetControlRect(false, 18f));
            float labelWidth = Mathf.Min(LabelColumn, rect.width * 0.35f);
            var bar = new Rect(rect.x + labelWidth, rect.y + 2f, rect.width - labelWidth, rect.height - 4f);
            GUI.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), label);
            float fill = critical > 0 ? Mathf.Clamp01((float)(value / critical)) : 0f;
            float filled = Mathf.Round(bar.width * fill);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(bar, EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.78f, 0.78f, 0.78f));
                var color = value > critical ? CriticalColor : value > warning ? WarningColor : OkColor;
                EditorGUI.DrawRect(new Rect(bar.x, bar.y, filled, bar.height), color);
                float mark = critical > 0 ? Mathf.Clamp01((float)(warning / critical)) : 0f;
                EditorGUI.DrawRect(new Rect(bar.x + bar.width * mark, bar.y, 1f, bar.height), new Color(0f, 0f, 0f, 0.5f));
            }
            // The figures change colour at the edge of the fill: near black over it, the skin's text colour after it.
            var text = new GUIContent(Strings.Format("ui.budget.value", FormatNumber(value) + unit, FormatNumber(warning) + unit));
            GUI.BeginClip(new Rect(bar.x, bar.y, filled, bar.height));
            GUI.Label(new Rect(0f, 0f, bar.width, bar.height), text, onFill);
            GUI.EndClip();
            GUI.BeginClip(new Rect(bar.x + filled, bar.y, bar.width - filled, bar.height));
            GUI.Label(new Rect(-filled, 0f, bar.width, bar.height), text, centeredMini);
            GUI.EndClip();
            GUI.Label(bar, new GUIContent("", Strings.Get("ui.budget.tip")), GUIStyle.none);
        }

        // A frame bar without viewpoints: the track only, "no viewpoints" in the middle.
        void EmptyBar(string label)
        {
            var rect = Padded(EditorGUILayout.GetControlRect(false, 18f));
            float labelWidth = Mathf.Min(LabelColumn, rect.width * 0.35f);
            var bar = new Rect(rect.x + labelWidth, rect.y + 2f, rect.width - labelWidth, rect.height - 4f);
            GUI.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), label);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(bar, EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.78f, 0.78f, 0.78f));
            GUI.Label(bar, new GUIContent(Strings.Get("ui.budget.noPoints"), Strings.Get("ui.budget.place.tip")), centeredMini);
        }

        // Which viewpoints are the worst and the whole scene for reference; without viewpoints a way to place them.
        void DrawFrameLine()
        {
            var f = result.frame;
            var text = f != null
                ? Strings.Format("ui.budget.frame.line", f.drawCallsPoint, f.trisPoint, f.points, FormatNumber(result.sceneTris), FormatNumber(result.drawCallEstimate))
                : Strings.Format("ui.budget.frame.none", FormatNumber(result.sceneTris), FormatNumber(result.drawCallEstimate));
            GUILayout.Space(2f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(text, wrapMini);
            if (f == null && GUILayout.Button(new GUIContent(Strings.Get("ui.budget.place"), Strings.Get("ui.budget.place.tip")), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
            {
                tab = TabViewpoints;
                OnTabChanged();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void DrawBudgetsTab()
        {
            if (result != null)
            {
                SectionTitle(Strings.Get("ui.budgetOverview"));
                DrawBudgetBars(true);
                GUILayout.Space(10f);
            }
            SectionTitle(Strings.Get("ui.budgets"));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical();
            DrawGpuRow();
            DrawHeadsetRows();
            var head = EditorGUILayout.GetControlRect();
            float column = (head.width - LabelColumn) / 2f;
            GUI.Label(new Rect(head.x + LabelColumn, head.y, column, head.height), Strings.Get("ui.budgets.warning"), EditorStyles.miniBoldLabel);
            GUI.Label(new Rect(head.x + LabelColumn + column, head.y, column, head.height), Strings.Get("ui.budgets.critical"), EditorStyles.miniBoldLabel);

            IntPair("ui.b.drawCalls", ref budgets.drawCallsWarning, ref budgets.drawCallsCritical);
            IntPair("ui.b.sceneTris", ref budgets.sceneTrisWarning, ref budgets.sceneTrisCritical);
            IntPair("ui.b.objectTris", ref budgets.objectTrisWarning, ref budgets.objectTrisCritical);
            IntPair("ui.b.texMem", ref budgets.textureMemoryWarningMB, ref budgets.textureMemoryCriticalMB);
            IntPair("ui.b.texSize", ref budgets.textureSizeWarning, ref budgets.textureSizeCritical);
            IntPair("ui.b.shadowLights", ref budgets.shadowLightsWarning, ref budgets.shadowLightsCritical);
            FloatPair("ui.b.frameTime", ref budgets.frameTimeWarningMs, ref budgets.frameTimeCriticalMs);
            IntPair("ui.b.transparent", ref budgets.transparentWarningPercent, ref budgets.transparentCriticalPercent);
            IntPair("ui.b.lightmapMem", ref budgets.lightmapMemoryWarningMB, ref budgets.lightmapMemoryCriticalMB);
            IntSingle("ui.b.animators", ref budgets.animatorsWarning);
            IntSingle("ui.b.skinnedTris", ref budgets.skinnedTrisWarning);
            IntSingle("ui.b.audioSources", ref budgets.audioSourcesWarning);
            IntSingle("ui.b.colliderTris", ref budgets.colliderTrisInfo);

            GUILayout.Space(4f);
            GUILayout.Label(VRBudgets.FileExists ? Strings.Format("ui.budgets.file", VRBudgets.FilePath) : Strings.Get("ui.budgets.defaults.note"), wrapMini);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(Strings.Get("ui.budgets.reset"), Strings.Get("ui.budgets.reset.tip")), GUILayout.Width(130f)))
                budgets = VRBudgets.DefaultsFor(budgets.targetGpu, budgets.targetHeadset, budgets.refreshRate, budgets.renderScale);
            if (GUILayout.Button(Strings.Get("ui.budgets.save"), GUILayout.Width(130f))) budgets.Save();
            EditorGUILayout.EndHorizontal();
            GUILayout.Label(Strings.Get("ui.budgets.rerun"), wrapMini);
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10f);
            DrawAcceptedSection();
        }

        // Target GPU: picking one fills triangles and texture memory with its starting values; the other thresholds
        // are the same for every GPU.
        void DrawGpuRow()
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), new GUIContent(Strings.Get("ui.gpu"), Strings.Get("ui.gpu.tip")));
            var options = VRBudgets.Gpus.Select(g => Strings.Format("ui.gpu.item", g.name, g.memoryGB)).ToArray();
            int index = System.Array.FindIndex(VRBudgets.Gpus, g => g.id == budgets.targetGpu);
            float width = Mathf.Min(220f, rect.width - LabelColumn);
            int chosen = EditorGUI.Popup(new Rect(rect.x + LabelColumn, rect.y, width, rect.height), index, options);
            if (chosen != index && chosen >= 0) budgets.ApplyGpu(VRBudgets.Gpus[chosen].id);
            if (budgets.IsCustom)
                GUI.Label(new Rect(rect.x + LabelColumn + width + 8f, rect.y, rect.width - LabelColumn - width - 8f, rect.height),
                    new GUIContent(Strings.Get("ui.gpu.custom"), Strings.Get("ui.gpu.custom.tip")), EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(LabelColumn);
            GUILayout.Label(Strings.Get("ui.gpu.note"), wrapMini);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        // Headset, refresh rate and SteamVR percent; changing any fills the frame time and the transparent share.
        void DrawHeadsetRows()
        {
            var h = budgets.Headset;
            float width = 0f;
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), new GUIContent(Strings.Get("ui.hs"), Strings.Get("ui.hs.tip")));
            width = Mathf.Min(220f, rect.width - LabelColumn);
            int index = System.Array.FindIndex(VRBudgets.Headsets, x => x.id == h.id);
            int chosen = EditorGUI.Popup(new Rect(rect.x + LabelColumn, rect.y, width, rect.height), index, VRBudgets.Headsets.Select(x => x.name).ToArray());
            if (chosen != index && chosen >= 0)
            {
                var next = VRBudgets.Headsets[chosen];
                budgets.ApplyHeadset(next.id, budgets.refreshRate, budgets.renderScale);
            }
            if (budgets.HeadsetCustom)
                GUI.Label(new Rect(rect.x + LabelColumn + width + 8f, rect.y, rect.width - LabelColumn - width - 8f, rect.height),
                    new GUIContent(Strings.Get("ui.gpu.custom"), Strings.Get("ui.hs.custom.tip")), EditorStyles.miniLabel);

            h = budgets.Headset;
            rect = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), new GUIContent(Strings.Get("ui.hs.rate"), Strings.Get("ui.hs.rate.tip")));
            int rateIndex = System.Array.IndexOf(h.rates, budgets.refreshRate);
            EditorGUI.BeginDisabledGroup(h.rates.Length < 2);
            int rate = EditorGUI.Popup(new Rect(rect.x + LabelColumn, rect.y, width, rect.height), rateIndex,
                h.rates.Select(r => Strings.Format("ui.hs.hz", r)).ToArray());
            EditorGUI.EndDisabledGroup();
            if (rate != rateIndex && rate >= 0) budgets.ApplyHeadset(h.id, h.rates[rate], budgets.renderScale);

            rect = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), new GUIContent(Strings.Get("ui.hs.scale"), Strings.Get("ui.hs.scale.tip")));
            int scale = EditorGUI.DelayedIntField(new Rect(rect.x + LabelColumn, rect.y, 70f, rect.height), budgets.renderScale);
            if (scale != budgets.renderScale) budgets.ApplyHeadset(h.id, budgets.refreshRate, scale);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(LabelColumn);
            var note = Strings.Format("ui.hs.note", FormatNumber(budgets.RenderWidth), FormatNumber(budgets.RenderHeight), budgets.refreshRate,
                FormatNumber(VRBudgets.FrameCriticalFor(budgets.refreshRate)), FormatNumber(System.Math.Round(budgets.PixelFactor, 1)));
            // In the note, not beside the rate: a narrow window has no room there.
            if (h.wireless) note = Strings.Get("ui.hs.wireless") + " " + note;
            GUILayout.Label(note, wrapMini);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        // "HTC Vive Pro 2 Wireless, 90 Hz", for the context line.
        static string HeadsetLine(VRBudgets b)
        {
            return Strings.Format(b.HeadsetCustom ? "ui.hs.context.edited" : "ui.hs.context", b.Headset.name, b.refreshRate);
        }

        // "RTX 2070 (8 GB)" or "custom thresholds", for the context line.
        static string GpuLine(VRBudgets b)
        {
            var gpu = b.Gpu;
            if (gpu == null) return Strings.Get("ui.gpu.context.custom");
            return Strings.Format(b.IsCustom ? "ui.gpu.context.edited" : "ui.gpu.context", gpu.name);
        }

        void DrawAcceptedSection()
        {
            var list = Accepted();
            SectionTitle(Strings.Get("ui.acc.section"));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(Strings.Get("ui.acc.section.note"), wrapMini);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Format("ui.acc.marked", list.entries.Count));
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(list.entries.Count == 0 && list.disabledChecks.Count == 0);
            if (GUILayout.Button(Strings.Get("ui.acc.clearAll"), GUILayout.Width(200f)) &&
                EditorUtility.DisplayDialog(Strings.Get("ui.acc.section"), Strings.Get("ui.acc.clearAll.ask"), Strings.Get("ui.acc.clearAll"), Strings.Get("ui.fix.cancel")))
            {
                list.Clear();
                if (result != null)
                {
                    list.Apply(result.issues);
                    ListChanged();
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            if (list.disabledChecks.Count == 0)
                GUILayout.Label(Strings.Get("ui.acc.noDisabled"), EditorStyles.miniLabel);
            foreach (var id in list.disabledChecks.ToList())
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(Strings.Has(id + ".title") ? Strings.Get(id + ".title") : id);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Strings.Get("ui.acc.enable"), EditorStyles.miniButton, GUILayout.Width(120f)))
                {
                    list.Enable(id);
                    if (result != null)
                    {
                        list.Apply(result.issues);
                        ListChanged();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Label(Strings.Format("ui.acc.file", AcceptedList.FilePath), wrapMini);
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void SectionTitle(string text)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(text, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        void IntPair(string key, ref int warning, ref int critical)
        {
            var rect = EditorGUILayout.GetControlRect();
            float column = (rect.width - LabelColumn) / 2f;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), Strings.Get(key));
            warning = Mathf.Max(0, EditorGUI.IntField(new Rect(rect.x + LabelColumn, rect.y, column - 4f, rect.height), warning));
            critical = Mathf.Max(warning, EditorGUI.IntField(new Rect(rect.x + LabelColumn + column, rect.y, column - 4f, rect.height), critical));
        }

        void FloatPair(string key, ref float warning, ref float critical)
        {
            var rect = EditorGUILayout.GetControlRect();
            float column = (rect.width - LabelColumn) / 2f;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), Strings.Get(key));
            warning = Mathf.Max(0f, EditorGUI.FloatField(new Rect(rect.x + LabelColumn, rect.y, column - 4f, rect.height), warning));
            critical = Mathf.Max(warning, EditorGUI.FloatField(new Rect(rect.x + LabelColumn + column, rect.y, column - 4f, rect.height), critical));
        }

        void IntSingle(string key, ref int value)
        {
            var rect = EditorGUILayout.GetControlRect();
            float column = (rect.width - LabelColumn) / 2f;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), Strings.Get(key));
            value = Mathf.Max(0, EditorGUI.IntField(new Rect(rect.x + LabelColumn, rect.y, column - 4f, rect.height), value));
        }
    }
}
