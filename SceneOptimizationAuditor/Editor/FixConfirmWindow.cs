using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    // Lists every change before it is applied; scene and asset changes are shown apart.
    public sealed class FixConfirmWindow : EditorWindow
    {
        List<AuditIssue> items;
        bool[] selected;
        Action<List<AuditIssue>> onApply;
        Vector2 scroll;
        [NonSerialized] GUIStyle wrapLabel;
        [NonSerialized] GUIStyle wrapMini;

        public static void Show(List<AuditIssue> items, Action<List<AuditIssue>> onApply)
        {
            var window = CreateInstance<FixConfirmWindow>();
            window.items = items;
            window.selected = new bool[items.Count];
            for (int i = 0; i < items.Count; i++) window.selected[i] = true;
            window.onApply = onApply;
            window.titleContent = new GUIContent(Strings.Get("ui.fix.title"));
            window.minSize = new Vector2(520f, 340f);
            window.ShowUtility();
        }

        void OnGUI()
        {
            // After a domain reload the list is gone: there is nothing left to confirm.
            if (items == null || selected == null)
            {
                Close();
                return;
            }
            if (wrapLabel == null)
            {
                wrapLabel = new GUIStyle(EditorStyles.label) { wordWrap = true };
                wrapMini = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            }

            EditorGUILayout.LabelField(Strings.Get("ui.fix.intro"), wrapLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawGroup(Strings.Get("ui.fix.groupScene"), false);
            DrawGroup(Strings.Get("ui.fix.groupAssets"), true);
            EditorGUILayout.EndScrollView();

            int count = 0;
            foreach (var on in selected) if (on) count++;
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.fix.cancel"), GUILayout.Width(120f))) Close();
            EditorGUI.BeginDisabledGroup(count == 0);
            if (GUILayout.Button(Strings.Format("ui.fix.apply", count), GUILayout.Width(140f)))
            {
                var chosen = new List<AuditIssue>();
                for (int i = 0; i < items.Count; i++) if (selected[i]) chosen.Add(items[i]);
                var callback = onApply;
                Close();
                if (callback != null) EditorApplication.delayCall += () => callback(chosen);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        void DrawGroup(string title, bool assets)
        {
            bool any = false;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].fixTouchesAsset != assets) continue;
                if (!any)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    any = true;
                }
                var issue = items[i];
                EditorGUILayout.BeginHorizontal();
                selected[i] = EditorGUILayout.Toggle(selected[i], GUILayout.Width(18f));
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(issue.FixDescription, wrapLabel);
                var location = issue.Location;
                if (AutoFixer.BecomesOverride(issue)) location += "  ·  " + Strings.Get("ui.fix.override");
                EditorGUILayout.LabelField(location, wrapMini);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
