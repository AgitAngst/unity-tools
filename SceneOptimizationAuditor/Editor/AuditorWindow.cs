using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityTools.SceneOptimization
{
    // Issues of one check and one severity, shown as one row of the list.
    public sealed class IssueGroup
    {
        public string key;
        public string checkId;
        public Severity severity;
        public Category category;
        public readonly List<AuditIssue> issues = new List<AuditIssue>();

        public string Title
        {
            get { return Strings.Has(checkId + ".title") ? Strings.Get(checkId + ".title") : checkId; }
        }

        public static List<IssueGroup> Build(IEnumerable<AuditIssue> issues)
        {
            var map = new Dictionary<string, IssueGroup>();
            var order = new List<IssueGroup>();
            foreach (var issue in issues)
            {
                var key = issue.checkId + "|" + issue.severity;
                IssueGroup group;
                if (!map.TryGetValue(key, out group))
                {
                    group = new IssueGroup { key = key, checkId = issue.checkId, severity = issue.severity, category = issue.category };
                    map[key] = group;
                    order.Add(group);
                }
                group.issues.Add(issue);
            }
            return order.OrderBy(g => (int)g.severity).ThenByDescending(g => g.issues.Count).ThenBy(g => g.checkId).ToList();
        }
    }

    public sealed class AuditorWindow : EditorWindow
    {
        const int GroupPageSize = 100;
        const float LabelColumn = 210f;
        const string ShowPref = "UnityTools.SceneAudit.Show.";

        public static readonly Color CriticalColor = new Color(0.86f, 0.27f, 0.27f);
        public static readonly Color WarningColor = new Color(0.95f, 0.72f, 0.2f);
        public static readonly Color InfoColor = new Color(0.55f, 0.55f, 0.55f);
        public static readonly Color OkColor = new Color(0.3f, 0.7f, 0.35f);

        static readonly string[] LangLabels = { "EN", "RU" };
        static readonly Category[] Categories = (Category[])Enum.GetValues(typeof(Category));
        static readonly Severity[] Severities = { Severity.Critical, Severity.Warning, Severity.Info };

        [SerializeField] int tab;
        [SerializeField] bool byCategory;
        [SerializeField] string search = "";
        [SerializeField] bool compare;
        [SerializeField] Vector2 listScroll;
        [SerializeField] Vector2 pageScroll;

        AuditResult result;
        List<IssueGroup> groups = new List<IssueGroup>();
        XrInfo xrPreview;
        VRBudgets budgets;
        readonly bool[] show = new bool[3];
        readonly Dictionary<string, bool> expanded = new Dictionary<string, bool>();
        readonly Dictionary<string, int> limits = new Dictionary<string, int>();
        readonly HashSet<Category> closedCategories = new HashSet<Category>();

        GUIStyle wrapMini;
        GUIStyle hint;
        GUIStyle link;
        GUIStyle bigNumber;
        GUIStyle verdictStyle;
        GUIStyle emptyTitle;
        GUIStyle emptyText;
        GUIStyle chip;
        GUIStyle centeredMini;
        GUIStyle boldFoldout;
        GUIStyle searchField;
        GUIStyle searchCancel;
        Texture errorIcon;
        Texture warnIcon;
        Texture infoIcon;
        Texture playIcon;

        [MenuItem("Tools/VR Scene Optimization Audit")]
        public static void Open()
        {
            var window = GetWindow<AuditorWindow>();
            window.minSize = new Vector2(560f, 460f);
            window.Show();
        }

        void OnEnable()
        {
            budgets = VRBudgets.Load();
            xrPreview = XrInfo.Detect();
            for (int i = 0; i < Severities.Length; i++)
                show[i] = EditorPrefs.GetBool(ShowPref + Severities[i], Severities[i] != Severity.Info);
            ProfilerCapture.Changed += Repaint;
        }

        void OnDisable()
        {
            ProfilerCapture.Changed -= Repaint;
        }

        void InitStyles()
        {
            if (wrapMini != null) return;
            bool pro = EditorGUIUtility.isProSkin;
            wrapMini = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            hint = new GUIStyle(EditorStyles.helpBox) { wordWrap = true, fontSize = 11, padding = new RectOffset(8, 8, 6, 6) };
            link = new GUIStyle(EditorStyles.label) { wordWrap = true };
            link.normal.textColor = pro ? new Color(0.55f, 0.72f, 1f) : new Color(0.1f, 0.3f, 0.75f);
            bigNumber = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
            verdictStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            emptyTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            emptyText = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            chip = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            centeredMini = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            boldFoldout = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            searchField = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField;
            searchCancel = GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? EditorStyles.toolbarButton;
            errorIcon = EditorGUIUtility.IconContent("console.erroricon.sml").image;
            warnIcon = EditorGUIUtility.IconContent("console.warnicon.sml").image;
            infoIcon = EditorGUIUtility.IconContent("console.infoicon.sml").image;
            playIcon = EditorGUIUtility.IconContent("PlayButton").image;
        }

        void OnGUI()
        {
            InitStyles();
            titleContent = new GUIContent(Strings.Get("ui.title"));
            DrawToolbar();
            DrawTabs();
            switch (tab)
            {
                case 1:
                    pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
                    DrawBudgetsTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case 2:
                    pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
                    DrawProfilerTab();
                    EditorGUILayout.EndScrollView();
                    break;
                default:
                    DrawProblemsTab();
                    break;
            }
        }

        // ---- run ----

        // Long work starts after the GUI pass, so progress bars and dialogs do not break the layout.
        void Run()
        {
            var r = SceneAuditor.RunFullAudit(budgets, true);
            if (r == null) return;
            result = r;
            xrPreview = r.xr;
            groups = IssueGroup.Build(r.issues);
            expanded.Clear();
            limits.Clear();
            foreach (var g in groups) if (g.severity == Severity.Critical) expanded[g.key] = true;
            Repaint();
        }

        void AfterFix()
        {
            EditorApplication.delayCall += Run;
        }

        // Used by the screenshot test: "all" expands every group.
        void ExpandForShot(string what)
        {
            foreach (var g in groups) if (what == "all" || g.severity != Severity.Info) expanded[g.key] = true;
        }

        // ---- toolbar and tabs ----

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var run = new GUIContent(" " + Strings.Get(result == null ? "ui.run" : "ui.rerun"), playIcon, Strings.Get("ui.run.tip"));
            if (GUILayout.Button(run, EditorStyles.toolbarButton, GUILayout.Width(170f)))
                EditorApplication.delayCall += Run;

            var safe = SafeFixes();
            EditorGUI.BeginDisabledGroup(safe.Count == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.autoFix", safe.Count), Strings.Get("ui.autoFix.tip")), EditorStyles.toolbarButton))
                EditorApplication.delayCall += () => AutoFixer.Confirm(safe, AfterFix);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(result == null);
            if (GUILayout.Button(Strings.Get("ui.export"), EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("JSON…"), false, () => Export("json"));
                menu.AddItem(new GUIContent("CSV…"), false, () => Export("csv"));
                menu.ShowAsContext();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            int lang = GUILayout.Toolbar((int)Strings.Current, LangLabels, EditorStyles.toolbarButton, GUILayout.Width(70f));
            if (lang != (int)Strings.Current) Strings.Current = (Lang)lang;
            EditorGUILayout.EndHorizontal();
        }

        void DrawTabs()
        {
            GUILayout.Space(4f);
            var labels = new[]
            {
                result != null ? Strings.Format("ui.tab.problems.n", result.issues.Count) : Strings.Get("ui.tab.problems"),
                Strings.Get("ui.tab.budgets"),
                Strings.Get("ui.tab.profiler")
            };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6f);
            tab = GUILayout.Toolbar(tab, labels, GUILayout.Height(24f));
            GUILayout.Space(6f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        List<AuditIssue> SafeFixes()
        {
            return result != null ? result.issues.Where(i => i.fixKind == FixKind.Safe && i.AutoFixable).ToList() : new List<AuditIssue>();
        }

        // ---- problems tab ----

        void DrawProblemsTab()
        {
            if (result == null)
            {
                DrawEmptyState();
                return;
            }
            DrawVerdict();
            DrawSeverityCards();
            DrawContext();
            DrawBudgetBars(false);
            DrawListToolbar();

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            int drawn = 0;
            if (byCategory)
            {
                foreach (var category in Categories)
                {
                    var inCategory = groups.Where(g => g.category == category && IsShown(g) && Visible(g).Count > 0).ToList();
                    if (inCategory.Count == 0) continue;
                    bool open = !closedCategories.Contains(category);
                    var title = Strings.CategoryName(category) + "  ·  " + inCategory.Sum(g => Visible(g).Count);
                    bool now = EditorGUILayout.Foldout(open, title, true, boldFoldout);
                    if (now) closedCategories.Remove(category);
                    else closedCategories.Add(category);
                    if (!now) continue;
                    foreach (var g in inCategory) drawn += DrawGroup(g, drawn);
                    GUILayout.Space(4f);
                }
            }
            else
            {
                foreach (var g in groups) if (IsShown(g)) drawn += DrawGroup(g, drawn);
            }
            if (drawn == 0) DrawNothing();
            GUILayout.Space(8f);
            EditorGUILayout.EndScrollView();
        }

        void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(30f);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(Strings.Get("ui.empty.title"), emptyTitle);
            GUILayout.Space(6f);
            GUILayout.Label(Strings.Get("ui.empty.text"), emptyText);
            GUILayout.Space(14f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(" " + Strings.Get("ui.run"), playIcon), GUILayout.Width(240f), GUILayout.Height(36f)))
                EditorApplication.delayCall += Run;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10f);
            if (xrPreview != null) GUILayout.Label(XrLine(xrPreview) + "  ·  " + StereoLine(xrPreview), centeredMini);
            EditorGUILayout.EndVertical();
            GUILayout.Space(30f);
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        void DrawVerdict()
        {
            int critical = result.Count(Severity.Critical);
            int warning = result.Count(Severity.Warning);
            string text;
            Color color;
            if (critical > 0)
            {
                text = Strings.Format("ui.verdict.critical", critical);
                color = CriticalColor;
            }
            else if (warning > 0)
            {
                text = Strings.Format("ui.verdict.warning", warning);
                color = WarningColor;
            }
            else
            {
                text = Strings.Get("ui.verdict.clean");
                color = OkColor;
            }
            var rect = Padded(GUILayoutUtility.GetRect(0f, 34f, GUILayout.ExpandWidth(true)));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, Tint(color, 0.14f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), color);
            }
            GUI.Label(new Rect(rect.x + 12f, rect.y, rect.width - 16f, rect.height), text, verdictStyle);
            GUILayout.Space(6f);
        }

        void DrawSeverityCards()
        {
            var row = Padded(GUILayoutUtility.GetRect(0f, 46f, GUILayout.ExpandWidth(true)));
            float width = (row.width - 8f) / 3f;
            for (int i = 0; i < Severities.Length; i++)
            {
                var severity = Severities[i];
                var rect = new Rect(row.x + i * (width + 4f), row.y, width, row.height);
                var color = SeverityColor(severity);
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rect, Tint(color, show[i] ? 0.2f : 0.05f));
                    if (show[i]) EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), color);
                }
                GUI.DrawTexture(new Rect(rect.x + 10f, rect.y + 14f, 18f, 18f), IconOf(severity));
                GUI.Label(new Rect(rect.x + 34f, rect.y + 3f, rect.width - 38f, 24f), result.Count(severity).ToString(CultureInfo.InvariantCulture), bigNumber);
                var caption = Strings.SeverityName(severity) + (show[i] ? "" : "  ·  " + Strings.Get("ui.card.hidden"));
                GUI.Label(new Rect(rect.x + 34f, rect.y + 25f, rect.width - 38f, 16f), caption, EditorStyles.miniLabel);
                GUI.Label(rect, new GUIContent("", Strings.Get(show[i] ? "ui.card.hide.tip" : "ui.card.show.tip")));
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    show[i] = !show[i];
                    EditorPrefs.SetBool(ShowPref + severity, show[i]);
                    Event.current.Use();
                }
            }
            GUILayout.Space(6f);
        }

        void DrawContext()
        {
            var q = result.quality;
            var parts = new List<string>
            {
                XrLine(result.xr), StereoLine(result.xr),
                Strings.Format(q.isStandaloneDefault ? "ui.quality.default" : "ui.quality.current", q.name),
                Strings.Format("ui.scenes", string.Join(", ", result.sceneNames), result.time.ToString("HH:mm", CultureInfo.InvariantCulture))
            };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(string.Join("  ·  ", parts.ToArray()), wrapMini);
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void DrawListToolbar()
        {
            GUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(new GUIContent(Strings.Get("ui.search"), Strings.Get("ui.search.tip")), GUILayout.ExpandWidth(false));
            search = GUILayout.TextField(search ?? "", searchField, GUILayout.MinWidth(140f));
            if (GUILayout.Button(GUIContent.none, searchCancel))
            {
                search = "";
                GUI.FocusControl(null);
            }
            GUILayout.Space(8f);
            byCategory = GUILayout.Toggle(byCategory, new GUIContent(Strings.Get("ui.byCategory"), Strings.Get("ui.byCategory.tip")),
                EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            if (GUILayout.Button(Strings.Get("ui.expandAll"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                foreach (var g in groups) expanded[g.key] = true;
            if (GUILayout.Button(Strings.Get("ui.collapseAll"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                expanded.Clear();
            EditorGUILayout.EndHorizontal();
        }

        void DrawNothing()
        {
            GUILayout.Space(16f);
            int hidden = groups.Where(g => !IsShown(g)).Sum(g => g.issues.Count);
            var text = groups.Count == 0 ? Strings.Get("ui.noIssues") : Strings.Get("ui.nothingMatches");
            if (hidden > 0) text += "\n" + Strings.Format("ui.hiddenByFilter", hidden);
            GUILayout.Label(text, centeredMini);
        }

        bool IsShown(IssueGroup g)
        {
            return show[(int)g.severity];
        }

        List<AuditIssue> Visible(IssueGroup g)
        {
            if (string.IsNullOrEmpty(search)) return g.issues;
            if (Contains(g.Title, search) || Contains(g.checkId, search)) return g.issues;
            return g.issues.Where(i => Contains(i.objectPath, search) || Contains(i.assetPath, search) || Contains(i.Message, search)).ToList();
        }

        static bool Contains(string text, string part)
        {
            return !string.IsNullOrEmpty(text) && text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // One group row and, when open, its advice and objects. Returns 1 when something was drawn.
        int DrawGroup(IssueGroup g, int index)
        {
            var visible = Visible(g);
            if (visible.Count == 0) return 0;
            bool open;
            expanded.TryGetValue(g.key, out open);

            var rect = Padded(GUILayoutUtility.GetRect(0f, 26f, GUILayout.ExpandWidth(true)));
            var color = SeverityColor(g.severity);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, Tint(EditorGUIUtility.isProSkin ? Color.white : Color.black, index % 2 == 0 ? 0.04f : 0.07f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), color);
            }

            float right = rect.xMax - 4f;
            var fixable = visible.Where(i => i.AutoFixable).ToList();
            if (fixable.Count > 0)
            {
                var label = fixable.Count == 1 ? Strings.Get("ui.fix") : Strings.Format("ui.fixAll", fixable.Count);
                var width = EditorStyles.miniButton.CalcSize(new GUIContent(label)).x + 8f;
                var button = new Rect(right - width, rect.y + 4f, width, 18f);
                if (GUI.Button(button, new GUIContent(label, Strings.Get("ui.fix.tip")), EditorStyles.miniButton))
                    EditorApplication.delayCall += () => AutoFixer.Confirm(fixable, AfterFix);
                right = button.x - 4f;
            }
            var settings = visible.Select(i => i.settingsPath).FirstOrDefault(p => !string.IsNullOrEmpty(p));
            if (settings != null)
            {
                var label = Strings.Get("ui.openSettings");
                var width = EditorStyles.miniButton.CalcSize(new GUIContent(label)).x + 8f;
                var button = new Rect(right - width, rect.y + 4f, width, 18f);
                if (GUI.Button(button, label, EditorStyles.miniButton)) SettingsService.OpenProjectSettings(settings);
                right = button.x - 4f;
            }
            if (!byCategory)
            {
                var name = Strings.CategoryName(g.category);
                var width = EditorStyles.miniLabel.CalcSize(new GUIContent(name)).x + 6f;
                GUI.Label(new Rect(right - width, rect.y + 5f, width, 16f), name, chip);
                right -= width + 4f;
            }

            var head = new Rect(rect.x + 8f, rect.y, right - rect.x - 8f, rect.height);
            bool toggled = EditorGUI.Foldout(new Rect(head.x, head.y + 5f, 14f, 16f), open, GUIContent.none, true);
            GUI.DrawTexture(new Rect(head.x + 16f, head.y + 5f, 16f, 16f), IconOf(g.severity));
            // Scene-wide problems have no objects, so a count would only confuse.
            bool hasObjects = visible.Any(i => i.objectPath.Length > 0 || i.assetPath.Length > 0);
            var title = new GUIContent(hasObjects ? g.Title + "  ·  " + visible.Count : g.Title, g.checkId);
            GUI.Label(new Rect(head.x + 36f, head.y + 4f, head.width - 36f, 18f), title, EditorStyles.boldLabel);
            if (Event.current.type == EventType.MouseDown && head.Contains(Event.current.mousePosition) && Event.current.mousePosition.x > head.x + 14f)
            {
                toggled = !open;
                Event.current.Use();
            }
            open = toggled;
            expanded[g.key] = open;
            if (!open) return 1;

            var advice = visible[0].Suggestion;
            if (advice.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(40f);
                GUILayout.Label(Strings.Format("ui.howToFix", advice), hint);
                GUILayout.Space(8f);
                EditorGUILayout.EndHorizontal();
            }
            int limit;
            if (!limits.TryGetValue(g.key, out limit)) limit = GroupPageSize;
            int shown = Mathf.Min(limit, visible.Count);
            for (int k = 0; k < shown; k++) DrawIssueRow(visible[k], visible.Count > 1);
            if (visible.Count > shown)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(40f);
                if (GUILayout.Button(Strings.Format("ui.showMore", Mathf.Min(GroupPageSize, visible.Count - shown)), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    limits[g.key] = limit + GroupPageSize;
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(6f);
            return 1;
        }

        void DrawIssueRow(AuditIssue issue, bool ownFix)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(40f);
            EditorGUILayout.BeginVertical();
            var location = issue.Location;
            if (issue.users > 0) location += (location.Length > 0 ? "  ·  " : "") + Strings.Format("ui.users", issue.users);
            if (location.Length > 0)
            {
                if (GUILayout.Button(new GUIContent(location, Strings.Get(issue.target != null ? "ui.select.tip" : "ui.settings.tip")), link)) Select(issue);
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
            GUILayout.Label(new GUIContent(issue.Message, issue.Suggestion), wrapMini);
            EditorGUILayout.EndVertical();
            if (ownFix && issue.AutoFixable)
            {
                if (GUILayout.Button(new GUIContent(Strings.Get("ui.fix"), issue.FixDescription), EditorStyles.miniButton, GUILayout.Width(84f)))
                {
                    var one = issue;
                    EditorApplication.delayCall += () => AutoFixer.Confirm(new List<AuditIssue> { one }, AfterFix);
                }
            }
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }

        static void Select(AuditIssue issue)
        {
            if (issue.target == null)
            {
                if (!string.IsNullOrEmpty(issue.settingsPath)) SettingsService.OpenProjectSettings(issue.settingsPath);
                return;
            }
            var component = issue.target as Component;
            var go = component != null ? component.gameObject : issue.target as GameObject;
            if (go != null && !EditorUtility.IsPersistent(go))
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
            else
            {
                Selection.activeObject = issue.target;
                EditorGUIUtility.PingObject(issue.target);
            }
        }

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

        // ---- profiler ----

        void DrawProfilerTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(Strings.Get("ui.prof.title"), EditorStyles.boldLabel);
            GUILayout.Label(Strings.Get("ui.prof.intro"), wrapMini);
            GUILayout.Space(6f);

            bool running = ProfilerCapture.IsRunning;
            EditorGUI.BeginDisabledGroup(running);
            ProfilerCapture.Frames = Mathf.Clamp(EditorGUILayout.IntField(Strings.Get("ui.prof.frames"), ProfilerCapture.Frames), 30, 5000);
            ProfilerCapture.Warmup = Mathf.Clamp(EditorGUILayout.IntField(Strings.Get("ui.prof.warmup"), ProfilerCapture.Warmup), 0, 1000);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            if (!running)
            {
                if (GUILayout.Button(new GUIContent(" " + Strings.Get("ui.prof.capture"), playIcon), GUILayout.Width(220f), GUILayout.Height(26f)))
                    EditorApplication.delayCall += ProfilerCapture.Start;
            }
            else if (GUILayout.Button(Strings.Get("ui.prof.cancel"), GUILayout.Width(220f), GUILayout.Height(26f)))
            {
                ProfilerCapture.Cancel();
            }
            GUILayout.Label(ProfilerCapture.StatusText(), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            compare = EditorGUILayout.ToggleLeft(Strings.Get("ui.prof.compare"), compare);
            GUILayout.Label(Strings.Get("ui.prof.note") + "\n" + Strings.Get("ui.prof.cpuNote"), hint);

            var scenePath = SceneManager.GetActiveScene().path;
            var last = ProfilerCapture.LoadLast(scenePath);
            var previous = compare ? ProfilerCapture.LoadPrevious(scenePath) : null;
            if (last == null)
            {
                GUILayout.Label(Strings.Get("ui.prof.none"), EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label(Strings.Format("ui.prof.taken", last.timestamp, last.frames), EditorStyles.miniLabel);
                DrawSnapshot(last, previous);
                if (compare && previous == null) GUILayout.Label(Strings.Get("ui.prof.noPrevious"), EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void DrawSnapshot(ProfilerSnapshot s, ProfilerSnapshot p)
        {
            var b = budgets;
            MetricRow("ui.m.fps", Triple(s.avgFPS, s.minFPS, s.maxFPS), FpsColor(s.avgFPS), p != null ? (double?)(s.avgFPS - p.avgFPS) : null, true);
            MetricRow("ui.m.cpu", Pair(s.avgCPUms, s.peakCPUms), FrameColor(s.avgCPUms, b), p != null ? Delta(s.avgCPUms, p.avgCPUms) : null, false);
            MetricRow("ui.m.gpu", Pair(s.avgGPUms, s.peakGPUms), FrameColor(s.avgGPUms, b), p != null ? Delta(s.avgGPUms, p.avgGPUms) : null, false);
            MetricRow("ui.m.drawCalls", FormatNumber(s.drawCalls), RangeColor(s.drawCalls, b.drawCallsWarning, b.drawCallsCritical),
                p != null ? (double?)(s.drawCalls - p.drawCalls) : null, false);
            MetricRow("ui.m.setPass", FormatNumber(s.setPassCalls), null, p != null ? (double?)(s.setPassCalls - p.setPassCalls) : null, false);
            MetricRow("ui.m.batches", FormatNumber(s.batches), null, p != null ? (double?)(s.batches - p.batches) : null, false);
            MetricRow("ui.m.tris", FormatNumber(s.trisPerFrame), RangeColor(s.trisPerFrame, b.sceneTrisWarning, b.sceneTrisCritical),
                p != null ? (double?)(s.trisPerFrame - p.trisPerFrame) : null, false);
            MetricRow("ui.m.texMem", FormatNumber(s.textureMemoryMB), RangeColor(s.textureMemoryMB, b.textureMemoryWarningMB, b.textureMemoryCriticalMB),
                p != null ? (double?)(s.textureMemoryMB - p.textureMemoryMB) : null, false);
            MetricRow("ui.m.meshMem", FormatNumber(s.meshMemoryMB), null, p != null ? (double?)(s.meshMemoryMB - p.meshMemoryMB) : null, false);
            MetricRow("ui.m.alloc", FormatNumber(s.totalAllocatedMB), null, p != null ? (double?)(s.totalAllocatedMB - p.totalAllocatedMB) : null, false);
        }

        void MetricRow(string key, string value, Color? color, double? delta, bool higherIsBetter)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), Strings.Get(key));
            float x = rect.x + LabelColumn;
            if (color.HasValue && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(x, rect.y + rect.height / 2f - 4f, 8f, 8f), color.Value);
            GUI.Label(new Rect(x + 14f, rect.y, 170f, rect.height), value);
            if (delta.HasValue && Math.Abs(delta.Value) > 0.0001)
            {
                bool better = higherIsBetter ? delta.Value > 0 : delta.Value < 0;
                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = better ? OkColor : CriticalColor;
                var arrow = delta.Value > 0 ? "↑ +" : "↓ ";
                GUI.Label(new Rect(x + 190f, rect.y, rect.xMax - x - 190f, rect.height), arrow + FormatNumber(delta.Value), style);
            }
        }

        static string Triple(float a, float b, float c)
        {
            return FormatNumber(a) + " / " + FormatNumber(b) + " / " + FormatNumber(c);
        }

        static string Pair(float avg, float peak)
        {
            if (avg < 0f) return Strings.Get("ui.na");
            return avg.ToString("F2", CultureInfo.InvariantCulture) + " / " + peak.ToString("F2", CultureInfo.InvariantCulture);
        }

        static double? Delta(float current, float previous)
        {
            if (current < 0f || previous < 0f) return null;
            return current - previous;
        }

        static Color? FpsColor(float fps)
        {
            if (fps >= VRBudgets.FpsWarning) return OkColor;
            return fps >= VRBudgets.FpsCritical ? WarningColor : CriticalColor;
        }

        static Color? FrameColor(float ms, VRBudgets b)
        {
            if (ms < 0f) return null;
            if (ms <= b.frameTimeWarningMs) return OkColor;
            return ms <= b.frameTimeCriticalMs ? WarningColor : CriticalColor;
        }

        static Color? RangeColor(double value, double warning, double critical)
        {
            if (value > critical) return CriticalColor;
            return value > warning ? WarningColor : OkColor;
        }

        // ---- shared ----

        void Export(string extension)
        {
            if (result == null) return;
            var name = (result.sceneNames.Length > 0 ? result.sceneNames[0] : "scene") + "-audit-" +
                       result.time.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
            var path = EditorUtility.SaveFilePanel(Strings.Get("ui.export.title"), "", name, extension);
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                ReportExporter.Save(result, path);
                ShowNotification(new GUIContent(Strings.Format("ui.export.done", path)));
            }
            catch (Exception e)
            {
                Debug.LogError("[SceneAudit] Could not save the report: " + e);
                EditorUtility.DisplayDialog(Strings.Get("ui.export.title"), Strings.Format("ui.export.error", e.Message), Strings.Get("ui.fix.ok"));
            }
        }

        public static string XrLine(XrInfo xr)
        {
            string inner;
            if (xr.BothEnabled) inner = Strings.Format("ui.xr.both", xr.DeviceName);
            else if (xr.legacyEnabled) inner = Strings.Format("ui.xr.legacy", string.Join(", ", xr.legacySdks));
            else if (xr.ManagementEnabled) inner = Strings.Format("ui.xr.management", string.Join(", ", xr.loaders));
            else inner = Strings.Get("ui.xr.none");
            return Strings.Format("ui.xr", inner);
        }

        public static string StereoLine(XrInfo xr)
        {
            var mode = xr.stereoMode == StereoMode.Unknown ? Strings.Get("ui.stereo.unknown") : xr.stereoMode.ToString();
            return Strings.Format("ui.stereo", mode);
        }

        // Russian groups digits with spaces and uses a decimal comma, English with commas and a point.
        public static string FormatNumber(double value)
        {
            var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            if (Strings.Current == Lang.Ru)
            {
                format.NumberGroupSeparator = " ";
                format.NumberDecimalSeparator = ",";
            }
            return Math.Abs(value - Math.Round(value)) < 0.05
                ? ((long)Math.Round(value)).ToString("N0", format)
                : value.ToString("N1", format);
        }

        Texture IconOf(Severity severity)
        {
            switch (severity)
            {
                case Severity.Critical: return errorIcon;
                case Severity.Warning: return warnIcon;
                default: return infoIcon;
            }
        }

        public static Color SeverityColor(Severity severity)
        {
            switch (severity)
            {
                case Severity.Critical: return CriticalColor;
                case Severity.Warning: return WarningColor;
                default: return InfoColor;
            }
        }

        static Color Tint(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        // Side gutter of 8 px for blocks drawn by rect.
        static Rect Padded(Rect rect)
        {
            return new Rect(rect.x + 8f, rect.y, rect.width - 16f, rect.height);
        }
    }
}
