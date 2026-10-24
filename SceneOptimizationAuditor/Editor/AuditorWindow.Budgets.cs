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
            EditorGUILayout.BeginVertical();
            BudgetBar(Strings.Get("ui.budget.tris"), result.sceneTris, b.sceneTrisWarning, b.sceneTrisCritical, "");
            BudgetBar(Strings.Get("ui.budget.drawCalls"), result.drawCallEstimate, b.drawCallsWarning, b.drawCallsCritical, "");
            BudgetBar(Strings.Get("ui.budget.texMem"), result.textureMemoryMB, b.textureMemoryWarningMB, b.textureMemoryCriticalMB, Strings.Get("ui.mb"));
            EditorGUILayout.EndVertical();
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
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(bar, EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.78f, 0.78f, 0.78f));
                float fill = critical > 0 ? Mathf.Clamp01((float)(value / critical)) : 0f;
                var color = value > critical ? CriticalColor : value > warning ? WarningColor : OkColor;
                EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * fill, bar.height), color);
                float mark = critical > 0 ? Mathf.Clamp01((float)(warning / critical)) : 0f;
                EditorGUI.DrawRect(new Rect(bar.x + bar.width * mark, bar.y, 1f, bar.height), new Color(0f, 0f, 0f, 0.5f));
            }
            var text = Strings.Format("ui.budget.value", FormatNumber(value) + unit, FormatNumber(warning) + unit);
            GUI.Label(bar, new GUIContent(text, Strings.Get("ui.budget.tip")), centeredMini);
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
            IntSingle("ui.b.animators", ref budgets.animatorsWarning);
            IntSingle("ui.b.skinnedTris", ref budgets.skinnedTrisWarning);
            IntSingle("ui.b.audioSources", ref budgets.audioSourcesWarning);
            IntSingle("ui.b.colliderTris", ref budgets.colliderTrisInfo);

            GUILayout.Space(4f);
            GUILayout.Label(VRBudgets.FileExists ? Strings.Format("ui.budgets.file", VRBudgets.FilePath) : Strings.Get("ui.budgets.defaults.note"), wrapMini);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.budgets.reset"), GUILayout.Width(130f))) budgets = new VRBudgets();
            if (GUILayout.Button(Strings.Get("ui.budgets.save"), GUILayout.Width(130f))) budgets.Save();
            EditorGUILayout.EndHorizontal();
            GUILayout.Label(Strings.Get("ui.budgets.rerun"), wrapMini);
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10f);
            DrawAcceptedSection();
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
                if (result != null) list.Apply(result.issues);
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
                    if (result != null) list.Apply(result.issues);
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
