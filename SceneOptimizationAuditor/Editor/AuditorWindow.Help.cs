using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public sealed partial class AuditorWindow
    {
        // ---- help ----

        // Names of the tabs for "Help on the … tab", by tab index.
        static readonly string[] TabNameKeys =
            { "ui.tab.problems", "ui.tab.viewpoints", "ui.tab.lightmap", "ui.tab.light", "ui.tab.heavy", "ui.tab.budgets", "ui.tab.profiler", "ui.tab.history" };

        // Segment "Textures" of the Heaviest tab.
        const int HeavyTextures = 2;

        // "Getting started" over a result, opened from the Help menu; a new run hides it.
        [SerializeField] bool startShown;
        // Groups with their card "More about the check" open.
        readonly HashSet<string> helpOpen = new HashSet<string>();

        [NonSerialized] Texture helpIcon;
        [NonSerialized] GUIStyle hintText;
        [NonSerialized] GUIStyle linkMini;
        [NonSerialized] GUIStyle taskCard;
        // Text of a step card: from the top, so the texts of one row start on one line.
        [NonSerialized] GUIStyle stepText;

        // Part of InitStyles.
        void InitHelpStyles()
        {
            helpIcon = EditorGUIUtility.IconContent("_Help").image;
            hintText = new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 11 };
            linkMini = new GUIStyle(link) { wordWrap = false, fontSize = 11 };
            stepText = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            taskCard = new GUIStyle(GUI.skin.button) { wordWrap = true, richText = true, alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
        }

        GUIContent HelpContent()
        {
            return new GUIContent(" " + Strings.Get("ui.help"), helpIcon, Strings.Get("ui.help.tip"));
        }

        void HelpButton()
        {
            if (GUILayout.Button(HelpContent(), EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(false))) HelpMenu();
        }

        void HelpMenu()
        {
            var menu = new GenericMenu();
            int current = tab;
            foreach (var item in HelpMenuItems(current))
            {
                if (item.Key.Length == 0) menu.AddSeparator("");
                else
                {
                    var action = item.Value;
                    menu.AddItem(new GUIContent(item.Key), false, () => HelpAction(action, current));
                }
            }
            menu.ShowAsContext();
        }

        // Label and action of the Help menu items; an empty label is a separator.
        static List<KeyValuePair<string, string>> HelpMenuItems(int current)
        {
            var name = Strings.Get(TabNameKeys[Mathf.Clamp(current, 0, TabNameKeys.Length - 1)]);
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(Strings.Get("ui.help.start"), "start"),
                new KeyValuePair<string, string>(Strings.Format("ui.help.tab", name), "tab"),
                new KeyValuePair<string, string>("", ""),
                new KeyValuePair<string, string>(Strings.Get("ui.help.guide"), "guide"),
                new KeyValuePair<string, string>(Strings.Get("ui.help.reference"), "reference")
            };
        }

        // Where a menu item leads: the window, or a page of the guide for the browser.
        static string HelpUrl(string action, int current)
        {
            switch (action)
            {
                case "tab": return CheckHelp.TabUrl(current);
                case "guide": return CheckHelp.GuideUrl;
                case "reference": return CheckHelp.ReferenceUrl + "#" + CheckHelp.ChecksAnchor;
                default: return "";
            }
        }

        void HelpAction(string action, int current)
        {
            if (action == "start")
            {
                ShowStart();
                return;
            }
            Application.OpenURL(HelpUrl(action, current));
        }

        void ShowStart()
        {
            tab = TabProblems;
            startShown = true;
            pageScroll = Vector2.zero;
            OnTabChanged();
            Repaint();
        }

        // ---- getting started ----

        // The Problems tab before the first run, or from the Help menu: three steps and the task cards.
        void DrawStart()
        {
            bool wide = position.width >= 640f;
            pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
            if (result != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8f);
                if (GUILayout.Button(Strings.Get("ui.start.back"), EditorStyles.miniButton, GUILayout.ExpandWidth(false))) startShown = false;
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(Strings.Get("ui.empty.title"), emptyTitle);
            GUILayout.Space(4f);
            GUILayout.Label(Strings.Get("ui.empty.text"), emptyText);
            GUILayout.Space(12f);

            // Cards of equal width side by side; on a narrow window one under another.
            float inner = position.width - 48f;
            float step = wide ? (inner - 8f) / 3f : inner;
            var texts = new[] { GpuStepText(), Strings.Get("ui.start.scope.text"), Strings.Get("ui.start.run.text") };
            float textHeight = wide ? texts.Max(t => stepText.CalcHeight(new GUIContent(t), step - 12f)) : 0f;
            if (wide) EditorGUILayout.BeginHorizontal();
            StartStep(1, Strings.Get("ui.start.gpu"), texts[0], step, textHeight, () =>
            {
                if (GUILayout.Button(new GUIContent(Strings.Get("ui.start.gpu.change"), Strings.Get("ui.start.gpu.change.tip"))))
                    PrepareTask("gpu");
            });
            if (wide) GUILayout.Space(4f);
            StartStep(2, Strings.Get("ui.start.scope"), texts[1], step, textHeight, () =>
            {
                if (GUILayout.Button(new GUIContent(ScopeLabel(), Strings.Get("ui.scope.tip")), EditorStyles.popup)) ScopeMenu();
            });
            if (wide) GUILayout.Space(4f);
            StartStep(3, Strings.Get("ui.start.run"), texts[2], step, textHeight, () =>
            {
                if (GUILayout.Button(new GUIContent(" " + Strings.Get("ui.run"), playIcon, Strings.Get("ui.run.tip")), GUILayout.Height(30f)))
                {
                    startShown = false;
                    EditorApplication.delayCall += Run;
                }
            });
            if (wide) EditorGUILayout.EndHorizontal();

            GUILayout.Space(12f);
            GUILayout.Label(Strings.Get("ui.start.tasks"), EditorStyles.boldLabel);
            float card = wide ? (inner - 4f) / 2f : inner;
            bool noBuild = AuditScope.BuildScenePaths().Count == 0;
            var tasks = new[] { "first", "fps", "memory", "build" };
            // A button does not grow with its wrapped text: every card gets the height of the tallest.
            var cards = tasks.Select(t => new GUIContent("<b>" + Strings.Get("ui.start." + t) + "</b>\n" + Strings.Get("ui.start." + t + ".text"))).ToArray();
            float cardHeight = Mathf.Max(44f, cards.Max(c => taskCard.CalcHeight(c, card)));
            for (int i = 0; i < tasks.Length; i++)
            {
                if (wide && i % 2 == 0) EditorGUILayout.BeginHorizontal();
                var task = tasks[i];
                bool off = task == "build" && noBuild;
                EditorGUI.BeginDisabledGroup(off);
                var tip = off ? Strings.Get("ui.start.build.none") : "";
                if (GUILayout.Button(new GUIContent(cards[i].text, tip), taskCard, GUILayout.Width(card), GUILayout.Height(cardHeight)) && PrepareTask(task))
                    EditorApplication.delayCall += Run;
                EditorGUI.EndDisabledGroup();
                if (wide && i % 2 == 0)
                {
                    GUILayout.Space(4f);
                    continue;
                }
                if (wide) EditorGUILayout.EndHorizontal();
                GUILayout.Space(4f);
            }

            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.start.guide"), link, GUILayout.ExpandWidth(false))) Application.OpenURL(CheckHelp.GuideUrl);
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10f);
            if (xrPreview != null) GUILayout.Label(XrLine(xrPreview) + "  ·  " + StereoLine(xrPreview), centeredMini);
            GUILayout.Label(Strings.Format("ui.version", ToolVersion.Full), centeredMini);
            EditorGUILayout.EndVertical();
            GUILayout.Space(16f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.EndScrollView();
        }

        void StartStep(int n, string title, string text, float width, float textHeight, Action button)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width));
            GUILayout.Label(n + ". " + title, EditorStyles.boldLabel);
            if (textHeight > 0f) GUILayout.Label(text, stepText, GUILayout.Height(textHeight));
            else GUILayout.Label(text, stepText);
            GUILayout.Space(4f);
            button();
            GUILayout.Space(2f);
            EditorGUILayout.EndVertical();
        }

        string GpuStepText()
        {
            var b = budgets;
            var gpu = b.Gpu;
            var name = gpu == null || b.IsCustom ? (gpu != null ? gpu.name + ", " : "") + Strings.Get("ui.start.gpu.custom") : gpu.name;
            return Strings.Format("ui.start.gpu.text", name, FormatNumber(b.sceneTrisWarning), FormatNumber(b.textureMemoryWarningMB));
        }

        // The first step of a task card; true when the audit has to run next.
        bool PrepareTask(string task)
        {
            switch (task)
            {
                case "gpu":
                    tab = TabBudgets;
                    break;
                case "first":
                    ByGain = true;
                    startShown = false;
                    return true;
                case "fps":
                    tab = TabViewpoints;
                    break;
                case "memory":
                    tab = TabHeavy;
                    hvMeasure = HeavyTextures;
                    hvShown = HvPageSize;
                    break;
                case "build":
                    if (AuditScope.BuildScenePaths().Count == 0) return false;
                    AuditScope.Kind = ScopeKind.BuildScenes;
                    startShown = false;
                    return true;
                default:
                    return false;
            }
            pageScroll = Vector2.zero;
            OnTabChanged();
            return false;
        }

        // ---- more about the check ----

        // The advice of an open group and, for a check with help, the link to its card.
        void DrawAdvice(IssueGroup g, List<AuditIssue> visible)
        {
            // Rows of one check may advise differently (SHADER_VARIANTS_FEW, MAT_SIMPLE_STANDARD_MODEL): then the check's own.
            var first = visible[0];
            var own = g.checkId + ".fix";
            var advice = visible.All(i => i.messageKey == first.messageKey) ? first.Suggestion :
                Strings.Has(own) ? Strings.Format(own, first.args) : "";
            bool hasHelp = CheckHelp.Has(g.checkId);
            if (advice.Length == 0 && !hasHelp) return;
            bool cardOpen = hasHelp && helpOpen.Contains(g.key);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(40f);
            EditorGUILayout.BeginHorizontal(hint);
            if (advice.Length > 0) GUILayout.Label(Strings.Format("ui.howToFix", advice), hintText);
            else GUILayout.FlexibleSpace();
            if (hasHelp)
            {
                var label = new GUIContent(Strings.Get(cardOpen ? "ui.help.less" : "ui.help.more"), Strings.Get("ui.help.more.tip"));
                if (GUILayout.Button(label, linkMini, GUILayout.ExpandWidth(false)))
                {
                    if (cardOpen) helpOpen.Remove(g.key);
                    else helpOpen.Add(g.key);
                }
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            if (cardOpen) DrawCheckHelp(g, visible);
        }

        void DrawCheckHelp(IssueGroup g, List<AuditIssue> visible)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(40f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Get(g.checkId + ".title"), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(g.checkId, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
            foreach (var section in CheckHelp.Sections)
            {
                var text = CheckHelp.Text(g.checkId, section);
                if (text.Length == 0) continue;
                GUILayout.Space(2f);
                GUILayout.Label(Strings.Get("ui.help." + section), EditorStyles.miniBoldLabel);
                GUILayout.Label(text, hintText);
            }
            GUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            var objects = MultiSelect.SelectionOf(visible);
            EditorGUI.BeginDisabledGroup(objects.Length == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.sel.select", objects.Length), Strings.Get("ui.sel.select.tip")), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                MultiSelect.Select(objects);
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(8f);
            if (GUILayout.Button(Strings.Get("ui.help.reference"), linkMini, GUILayout.ExpandWidth(false)))
                Application.OpenURL(CheckHelp.ReferenceUrl + "#" + CheckHelp.ChecksAnchor);
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        // Used by the screenshot test: only the group of a check open, with its card.
        void HelpForShot(string checkId)
        {
            expanded.Clear();
            foreach (var g in groups.Where(g => g.checkId == checkId))
            {
                expanded[g.key] = true;
                helpOpen.Add(g.key);
            }
        }
    }
}
