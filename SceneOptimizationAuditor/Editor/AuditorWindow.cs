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
        [SerializeField] bool lmOnlyProblems = true;
        [SerializeField] bool showAccepted;
        // History tab: chosen file (empty for the audited scenes), compared points, open lists.
        [SerializeField] string historyFile = "";
        [SerializeField] int histA = -1;
        [SerializeField] int histB = -1;
        [SerializeField] bool histFixedOpen;
        [SerializeField] bool histNewOpen;
        AuditHistory history;
        string historyPath;
        string noteDraft;
        int noteFor = -1;
        readonly Dictionary<string, string> historyLabels = new Dictionary<string, string>();
        // Point recorded by the last run and the one before it, for the progress line.
        HistoryPoint prevPoint;
        AcceptedList acceptedList;
        List<LightmapUvRow> lmRows;
        // Rows picked with their checkboxes, for actions on several at once.
        readonly HashSet<AuditIssue> picked = new HashSet<AuditIssue>();
        readonly HashSet<LightmapUvRow> lmPicked = new HashSet<LightmapUvRow>();

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
                    DrawLightmapTab();
                    break;
                case 2:
                    pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
                    DrawBudgetsTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case 3:
                    pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
                    DrawProfilerTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case 4:
                    pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
                    DrawHistoryTab();
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
            acceptedList = AcceptedList.Load();
            RecordHistory(r);
            expanded.Clear();
            limits.Clear();
            picked.Clear();
            foreach (var g in groups) if (g.severity == Severity.Critical) expanded[g.key] = true;
            Repaint();
        }

        void AfterFix()
        {
            EditorApplication.delayCall += Run;
        }

        // Used by the screenshot test: picks the rows of the first group and the first lightmap rows.
        void PickForShot()
        {
            var first = groups.FirstOrDefault(IsShown);
            if (first != null) foreach (var issue in first.issues) if (MultiSelect.CanPick(issue)) picked.Add(issue);
            if (lmRows != null) foreach (var row in lmRows.Take(3)) lmPicked.Add(row);
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
            if (GUILayout.Button(new GUIContent(Strings.Get("ui.export"), Strings.Get("ui.export.tip")), EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("JSON…"), false, () => Export("json"));
                menu.AddItem(new GUIContent("CSV…"), false, () => Export("csv"));
                menu.ShowAsContext();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent(Strings.Format("ui.version", ToolVersion.Full), Strings.Get("ui.version.tip")), EditorStyles.miniLabel);
            var langs = new[] { new GUIContent(LangLabels[0], Strings.Get("ui.lang.tip")), new GUIContent(LangLabels[1], Strings.Get("ui.lang.tip")) };
            int lang = GUILayout.Toolbar((int)Strings.Current, langs, EditorStyles.toolbarButton, GUILayout.Width(70f));
            if (lang != (int)Strings.Current) Strings.Current = (Lang)lang;
            EditorGUILayout.EndHorizontal();
        }

        void DrawTabs()
        {
            GUILayout.Space(4f);
            var labels = new[]
            {
                result != null ? Strings.Format("ui.tab.problems.n", result.ActiveCount) : Strings.Get("ui.tab.problems"),
                Strings.Get("ui.tab.lightmap"),
                Strings.Get("ui.tab.budgets"),
                Strings.Get("ui.tab.profiler"),
                Strings.Get("ui.tab.history")
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
            return result != null ? result.issues.Where(i => !i.accepted && i.fixKind == FixKind.Safe && i.AutoFixable).ToList() : new List<AuditIssue>();
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
            GUILayout.Label(Strings.Format("ui.version", ToolVersion.Full), centeredMini);
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
            if (prevPoint == null) return;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            var since = Strings.Format("ui.h.since", DateText(prevPoint.Time),
                DeltaText(result.Count(Severity.Critical) - prevPoint.critical), DeltaText(result.Count(Severity.Warning) - prevPoint.warning));
            GUILayout.Label(new GUIContent(since, Strings.Get("ui.h.since.tip")), wrapMini);
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
            GUILayout.FlexibleSpace();
            showAccepted = GUILayout.Toggle(showAccepted, new GUIContent(Strings.Format("ui.acc.show", result != null ? result.AcceptedCount : 0), Strings.Get("ui.acc.show.tip")),
                EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            if (GUILayout.Button(new GUIContent(Strings.Get("ui.sel.all"), Strings.Get("ui.sel.all.tip")), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                foreach (var g in groups)
                    if (IsShown(g))
                        foreach (var issue in Visible(g)) if (MultiSelect.CanPick(issue)) picked.Add(issue);
            EditorGUILayout.EndHorizontal();
            DrawSelectionBar();
        }

        // Shown only while something is picked: count on one line, actions on the next.
        AcceptedList Accepted()
        {
            if (acceptedList == null) acceptedList = AcceptedList.Load();
            return acceptedList;
        }

        void DrawSelectionBar()
        {
            if (picked.Count == 0) return;
            var fixable = MultiSelect.Fixable(picked);
            var objects = MultiSelect.SelectionOf(picked);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            // Picked rows stay picked when a severity card hides them; say so instead of a puzzling count.
            int hidden = picked.Count(i => !show[(int)i.severity]);
            GUILayout.Label(Strings.Format("ui.sel.count", picked.Count) + (hidden > 0 ? "  ·  " + Strings.Format("ui.sel.hidden", hidden) : ""), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.sel.clear"), EditorStyles.miniButton, GUILayout.ExpandWidth(false))) picked.Clear();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(fixable.Count == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.sel.fix", fixable.Count), Strings.Get("ui.fix.tip")), GUILayout.ExpandWidth(false)))
                EditorApplication.delayCall += () => AutoFixer.Confirm(fixable, AfterFix);
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(objects.Length == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.sel.select", objects.Length), Strings.Get("ui.sel.select.tip")), GUILayout.ExpandWidth(false)))
                MultiSelect.Select(objects);
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Second row: what the team decided to live with.
            var toAccept = picked.Where(i => !i.accepted).ToList();
            var toRelease = picked.Where(i => i.accepted).ToList();
            var checks = picked.Select(i => i.checkId).Distinct().ToList();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(toAccept.Count == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.acc.mark", toAccept.Count), Strings.Get("ui.acc.mark.tip")), GUILayout.ExpandWidth(false)))
            {
                Accepted().Accept(toAccept);
                picked.Clear();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(toRelease.Count == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.acc.release", toRelease.Count), Strings.Get("ui.acc.release.tip")), GUILayout.ExpandWidth(false)))
            {
                Accepted().Release(toRelease);
                picked.Clear();
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.acc.disable", checks.Count), Strings.Get("ui.acc.disable.tip")), GUILayout.ExpandWidth(false)))
            {
                var names = string.Join("\n", checks.Select(c => "• " + Strings.Get(c + ".title")).ToArray());
                if (EditorUtility.DisplayDialog(Strings.Get("ui.acc.disable.title"), Strings.Format("ui.acc.disable.ask", names),
                        Strings.Get("ui.acc.disable.yes"), Strings.Get("ui.fix.cancel")))
                {
                    Accepted().Disable(checks);
                    Accepted().Apply(result.issues);
                    picked.Clear();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        void DrawNothing()
        {
            GUILayout.Space(16f);
            int hidden = groups.Where(g => !IsShown(g)).Sum(g => g.issues.Count(i => !i.accepted));
            var text = groups.Count == 0 ? Strings.Get("ui.noIssues") : Strings.Get("ui.nothingMatches");
            if (hidden > 0) text += "\n" + Strings.Format("ui.hiddenByFilter", hidden);
            if (!showAccepted && result.AcceptedCount > 0) text += "\n" + Strings.Format("ui.acc.hiddenNote", result.AcceptedCount);
            GUILayout.Label(text, centeredMini);
        }

        bool IsShown(IssueGroup g)
        {
            return show[(int)g.severity];
        }

        // Accepted problems are shown only with "Accepted" on.
        List<AuditIssue> Visible(IssueGroup g)
        {
            var issues = showAccepted ? g.issues : g.issues.Where(i => !i.accepted).ToList();
            if (string.IsNullOrEmpty(search)) return issues;
            if (Contains(g.Title, search) || Contains(g.checkId, search)) return issues;
            return issues.Where(i => Contains(i.objectPath, search) || Contains(i.assetPath, search) || Contains(i.Message, search)).ToList();
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
            var fixable = visible.Where(i => i.AutoFixable && !i.accepted).ToList();
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
            var pickable = visible.Where(MultiSelect.CanPick).ToList();
            if (pickable.Count > 0)
            {
                bool all = pickable.All(picked.Contains);
                bool now = GUI.Toggle(new Rect(head.x + 16f, head.y + 5f, 16f, 16f), all, new GUIContent("", Strings.Get("ui.sel.group.tip")));
                if (now != all)
                {
                    foreach (var issue in pickable)
                        if (now) picked.Add(issue);
                        else picked.Remove(issue);
                }
            }
            GUI.DrawTexture(new Rect(head.x + 36f, head.y + 5f, 16f, 16f), IconOf(g.severity));
            // Scene-wide problems have no objects, so a count would only confuse.
            bool hasObjects = visible.Any(i => i.objectPath.Length > 0 || i.assetPath.Length > 0);
            bool allAccepted = visible.All(i => i.accepted);
            var title = new GUIContent((hasObjects ? g.Title + "  ·  " + visible.Count : g.Title) +
                (allAccepted ? "  ·  " + Strings.Get("ui.acc.tag") : ""), g.checkId);
            GUI.Label(new Rect(head.x + 56f, head.y + 4f, head.width - 56f, 18f), title, EditorStyles.boldLabel);
            if (Event.current.type == EventType.MouseDown && head.Contains(Event.current.mousePosition) && Event.current.mousePosition.x > head.x + 34f)
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
            if (MultiSelect.CanPick(issue))
            {
                bool on = picked.Contains(issue);
                bool now = GUILayout.Toggle(on, new GUIContent("", Strings.Get("ui.sel.pick.tip")), GUILayout.Width(18f));
                if (now && !on) picked.Add(issue);
                else if (!now && on) picked.Remove(issue);
            }
            else GUILayout.Space(22f);
            EditorGUILayout.BeginVertical();
            var location = issue.Location;
            if (issue.users > 0) location += (location.Length > 0 ? "  ·  " : "") + Strings.Format("ui.users", issue.users);
            if (location.Length > 0)
            {
                if (GUILayout.Button(new GUIContent(location, Strings.Get(issue.target != null ? "ui.select.tip" : "ui.settings.tip")), link)) Select(issue);
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
            var message = issue.accepted ? Strings.Get("ui.acc.tag") + "  ·  " + issue.Message : issue.Message;
            GUILayout.Label(new GUIContent(message, issue.Suggestion), wrapMini);
            EditorGUILayout.EndVertical();
            if (ownFix && issue.AutoFixable && !issue.accepted)
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
                row.importer == null ? (Color?)null : row.GenerateOn ? OkColor : row.hasUv1 ? (Color?)null : CriticalColor);
            Cell(cells[3], Strings.Get(row.hasUv1 ? "ui.lm.yes" : "ui.lm.no"), row.hasUv1 ? OkColor : CriticalColor);
            Cell(cells[4], row.hasUv1 ? Percent(row.outsideShare) : "—", row.outsideShare > 0f ? WarningColor : (Color?)null);
            Cell(cells[5], row.hasUv1 ? Percent(row.overlapShare) : "—", row.overlapShare > VRBudgets.LightmapUvOverlapMinShare ? WarningColor : (Color?)null);
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

        // ---- history ----

        void RecordHistory(AuditResult r)
        {
            try
            {
                var point = AuditHistory.Record(r, "");
                var h = AuditHistory.Load(AuditHistory.FileFor(r.scenePaths));
                int index = h.points.FindIndex(p => p.time == point.time);
                prevPoint = index > 0 ? h.points[index - 1] : null;
            }
            catch (Exception e)
            {
                prevPoint = null;
                Debug.LogWarning("[SceneAudit] Could not save the history point: " + e.Message);
            }
            history = null;
            historyLabels.Clear();
            histA = histB = -1;
        }

        string CurrentHistoryFile()
        {
            if (result != null) return AuditHistory.FileFor(result.scenePaths);
            var paths = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) paths.Add(scene.path);
            }
            return AuditHistory.FileFor(paths.ToArray());
        }

        AuditHistory History(string path)
        {
            if (history == null || historyPath != path)
            {
                history = AuditHistory.Load(path);
                historyPath = path;
                noteFor = -1;
            }
            return history;
        }

        string HistoryLabel(string path, bool current)
        {
            string label;
            if (!historyLabels.TryGetValue(path, out label))
            {
                var scenes = AuditHistory.Load(path).scenes;
                label = scenes.Length > 0 ? string.Join(", ", scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s)).ToArray()) : System.IO.Path.GetFileNameWithoutExtension(path);
                historyLabels[path] = label;
            }
            // Slashes would make submenus in the popup.
            label = label.Replace('/', '∕');
            return current ? Strings.Format("ui.h.scene.current", label) : label;
        }

        void DrawHistoryTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical();
            GUILayout.Label(Strings.Get("ui.h.title"), EditorStyles.boldLabel);
            GUILayout.Label(Strings.Get("ui.h.intro"), wrapMini);
            GUILayout.Space(4f);

            var current = CurrentHistoryFile();
            var path = string.IsNullOrEmpty(historyFile) ? current : historyFile;
            var files = AuditHistory.Files().ToList();
            if (!files.Contains(current)) files.Insert(0, current);
            if (!files.Contains(path)) files.Insert(0, path);
            int index = files.IndexOf(path);
            var labels = files.Select(f => new GUIContent(HistoryLabel(f, f == current))).ToArray();
            int chosen = EditorGUILayout.Popup(new GUIContent(Strings.Get("ui.h.scene"), Strings.Get("ui.h.scene.tip")), index, labels);
            if (chosen != index)
            {
                historyFile = files[chosen] == current ? "" : files[chosen];
                histA = histB = -1;
            }

            var h = History(string.IsNullOrEmpty(historyFile) ? current : historyFile);
            if (h.points.Count == 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label(Strings.Get("ui.h.none"), hint);
                EndHistoryTab();
                return;
            }
            int count = h.points.Count;
            if (histB < 0 || histB >= count) histB = count - 1;
            if (histA < 0 || histA >= count) histA = 0;

            GUILayout.Space(8f);
            GUILayout.Label(Strings.Get("ui.h.trend"), EditorStyles.boldLabel);
            DrawTrends(h.points);
            GUILayout.Space(8f);
            GUILayout.Label(Strings.Format("ui.h.points", count), EditorStyles.boldLabel);
            GUILayout.Label(Strings.Get("ui.h.points.note"), wrapMini);
            DrawPointTable(h.points);
            GUILayout.Space(8f);
            DrawPointNote(h);
            if (histA != histB)
            {
                GUILayout.Space(8f);
                DrawComparison(h.points[Math.Min(histA, histB)], h.points[Math.Max(histA, histB)]);
            }
            GUILayout.Label(Strings.Format("ui.h.file", historyPath), wrapMini);
            EndHistoryTab();
        }

        void EndHistoryTab()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        void DrawTrends(List<HistoryPoint> points)
        {
            int from = Math.Min(histA, histB);
            int to = Math.Max(histA, histB);
            var last = points[to];
            var first = points[from];
            Trend("ui.h.critical", points.Select(p => (double)p.critical).ToList(), CriticalColor, null, last.critical, last.critical - first.critical, false);
            Trend("ui.h.warning", points.Select(p => (double)p.warning).ToList(), WarningColor, null, last.warning, last.warning - first.warning, false);
            Trend("ui.h.info", points.Select(p => (double)p.info).ToList(), InfoColor, null, last.info, last.info - first.info, false);
            Trend("ui.budget.tris", points.Select(p => (double)p.sceneTris).ToList(), StateColor(last.sceneTris, last.trisLimit), last.trisLimit,
                last.sceneTris, last.sceneTris - first.sceneTris, false);
            Trend("ui.budget.drawCalls", points.Select(p => (double)p.drawCalls).ToList(), StateColor(last.drawCalls, last.drawCallLimit), last.drawCallLimit,
                last.drawCalls, last.drawCalls - first.drawCalls, false);
            Trend("ui.budget.texMem", points.Select(p => p.textureMemoryMB).ToList(), StateColor(last.textureMemoryMB, last.textureMemoryLimitMB),
                last.textureMemoryLimitMB, last.textureMemoryMB, last.textureMemoryMB - first.textureMemoryMB, false);
            if (points.Any(p => p.hasProfiler))
            {
                var fps = points.Select(p => p.hasProfiler ? (double)p.avgFPS : double.NaN).ToList();
                var lastFps = points.Take(to + 1).LastOrDefault(p => p.hasProfiler);
                var firstFps = points.Skip(from).Take(to - from + 1).FirstOrDefault(p => p.hasProfiler);
                double value = lastFps != null ? lastFps.avgFPS : double.NaN;
                double delta = lastFps != null && firstFps != null ? lastFps.avgFPS - firstFps.avgFPS : 0;
                Trend("ui.m.fps", fps, OkColor, VRBudgets.FpsWarning, value, delta, true);
            }
            GUILayout.Label(Strings.Get("ui.h.trend.note"), wrapMini);
        }

        static Color StateColor(double value, double limit)
        {
            return limit > 0 && value > limit ? WarningColor : OkColor;
        }

        // One row: label, line over the points (NaN leaves a gap), dashed limit, value at B and change since A.
        void Trend(string key, List<double> values, Color color, double? limit, double value, double delta, bool higherIsBetter)
        {
            var rect = EditorGUILayout.GetControlRect(false, 34f);
            float labelWidth = Mathf.Min(LabelColumn, rect.width * 0.35f);
            const float valueWidth = 150f;
            GUI.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), Strings.Get(key));
            var area = new Rect(rect.x + labelWidth, rect.y + 2f, Mathf.Max(40f, rect.width - labelWidth - valueWidth - 6f), rect.height - 4f);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(area, EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.78f, 0.78f, 0.78f));
                double max = 0;
                foreach (var v in values) if (!double.IsNaN(v)) max = Math.Max(max, v);
                if (limit.HasValue) max = Math.Max(max, limit.Value * 1.1);
                if (max <= 0) max = 1;
                Func<int, float> x = i => values.Count == 1 ? area.center.x : area.x + 4f + (area.width - 8f) * i / (values.Count - 1);
                Func<double, float> y = v => area.yMax - 3f - (area.height - 6f) * (float)(v / max);
                // The compared points A and B.
                var mark = new Color(0.5f, 0.5f, 0.5f, 0.35f);
                EditorGUI.DrawRect(new Rect(x(histA), area.y, 1f, area.height), mark);
                EditorGUI.DrawRect(new Rect(x(histB), area.y, 1f, area.height), mark);
                if (limit.HasValue && limit.Value > 0)
                {
                    float ly = y(limit.Value);
                    for (float lx = area.x; lx < area.xMax; lx += 6f)
                        EditorGUI.DrawRect(new Rect(lx, ly, Mathf.Min(3f, area.xMax - lx), 1f), new Color(0f, 0f, 0f, 0.5f));
                }
                Handles.color = color;
                var line = new List<Vector3>();
                for (int i = 0; i <= values.Count; i++)
                {
                    if (i < values.Count && !double.IsNaN(values[i]))
                    {
                        line.Add(new Vector3(x(i), y(values[i]), 0f));
                        continue;
                    }
                    if (line.Count > 1) Handles.DrawAAPolyLine(2f, line.ToArray());
                    line.Clear();
                }
                for (int i = 0; i < values.Count; i++)
                    if (!double.IsNaN(values[i])) EditorGUI.DrawRect(new Rect(x(i) - 2f, y(values[i]) - 2f, 4f, 4f), color);
                Handles.color = Color.white;
            }
            var valueRect = new Rect(area.xMax + 6f, rect.y, valueWidth, rect.height);
            GUI.Label(new Rect(valueRect.x, valueRect.y, valueRect.width, rect.height / 2f), double.IsNaN(value) ? Strings.Get("ui.na") : FormatNumber(value));
            if (Math.Abs(delta) > 0.0001)
            {
                bool better = higherIsBetter ? delta > 0 : delta < 0;
                var style = new GUIStyle(EditorStyles.miniLabel);
                style.normal.textColor = better ? OkColor : CriticalColor;
                GUI.Label(new Rect(valueRect.x, valueRect.y + rect.height / 2f, valueRect.width, rect.height / 2f),
                    new GUIContent(Strings.Format("ui.h.sinceA", DeltaText(delta)), Strings.Get("ui.h.sinceA.tip")), style);
            }
        }

        void DrawPointTable(List<HistoryPoint> points)
        {
            var columns = new[] { 22f, 22f, 118f, 46f, 52f, 46f, 86f, 60f, 62f };
            var heads = new[] { "A", "B", Strings.Get("ui.h.col.date"), Strings.Get("ui.h.col.critical"), Strings.Get("ui.h.col.warning"),
                Strings.Get("ui.h.col.info"), Strings.Get("ui.h.col.tris"), Strings.Get("ui.h.col.drawCalls"), Strings.Get("ui.h.col.texMem") };
            var tips = new[] { Strings.Get("ui.h.col.a.tip"), Strings.Get("ui.h.col.b.tip"), "", Strings.Get("ui.h.critical"), Strings.Get("ui.h.warning"),
                Strings.Get("ui.h.info"), Strings.Get("ui.budget.tris"), Strings.Get("ui.budget.drawCalls"), Strings.Get("ui.budget.texMem") };
            var head = EditorGUILayout.GetControlRect();
            float hx = head.x;
            for (int c = 0; c < columns.Length; c++)
            {
                GUI.Label(new Rect(hx, head.y, columns[c], head.height), new GUIContent(heads[c], tips[c]), EditorStyles.miniBoldLabel);
                hx += columns[c];
            }
            GUI.Label(new Rect(hx, head.y, Mathf.Max(0f, head.xMax - hx), head.height), Strings.Get("ui.h.col.note"), EditorStyles.miniBoldLabel);

            // Newest first.
            for (int i = points.Count - 1; i >= 0; i--)
            {
                var p = points[i];
                var rect = EditorGUILayout.GetControlRect(false, 22f);
                if (Event.current.type == EventType.Repaint && (points.Count - 1 - i) % 2 == 1)
                    EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.04f) : new Color(0f, 0f, 0f, 0.07f));
                float x = rect.x;
                if (GUI.Toggle(new Rect(x + 3f, rect.y + 3f, 16f, 16f), histA == i, GUIContent.none) && histA != i) histA = i;
                x += columns[0];
                if (GUI.Toggle(new Rect(x + 3f, rect.y + 3f, 16f, 16f), histB == i, GUIContent.none) && histB != i) histB = i;
                x += columns[1];
                var cells = new[]
                {
                    DateText(p.Time), FormatNumber(p.critical), FormatNumber(p.warning), FormatNumber(p.info),
                    FormatNumber(p.sceneTris), FormatNumber(p.drawCalls), FormatNumber(p.textureMemoryMB)
                };
                for (int c = 0; c < cells.Length; c++)
                {
                    GUI.Label(new Rect(x, rect.y + 3f, columns[c + 2], 16f), cells[c]);
                    x += columns[c + 2];
                }
                GUI.Label(new Rect(x, rect.y + 3f, Mathf.Max(0f, rect.xMax - x), 16f), new GUIContent(p.note, p.note), EditorStyles.miniLabel);
            }
        }

        void DrawPointNote(AuditHistory h)
        {
            var p = h.points[histB];
            if (noteFor != histB)
            {
                noteFor = histB;
                noteDraft = p.note;
            }
            GUILayout.Label(Strings.Format("ui.h.noteOf", DateText(p.Time)), EditorStyles.boldLabel);
            GUILayout.Label(Strings.Get("ui.h.note.hint"), wrapMini);
            noteDraft = EditorGUILayout.TextField(noteDraft);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Format("ui.h.version", p.toolVersion), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(h.points.Count == 0);
            if (GUILayout.Button(Strings.Get("ui.h.delete"), GUILayout.Width(130f)) &&
                EditorUtility.DisplayDialog(Strings.Get("ui.h.title"), Strings.Format("ui.h.delete.ask", DateText(p.Time)), Strings.Get("ui.h.delete"), Strings.Get("ui.fix.cancel")))
            {
                h.points.RemoveAt(histB);
                h.Save(historyPath);
                histA = histB = -1;
                noteFor = -1;
                GUIUtility.ExitGUI();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(noteDraft == p.note);
            if (GUILayout.Button(Strings.Get("ui.h.note.save"), GUILayout.Width(130f)))
            {
                p.note = (noteDraft ?? "").Trim();
                h.Save(historyPath);
                noteDraft = p.note;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        void DrawComparison(HistoryPoint a, HistoryPoint b)
        {
            var diff = AuditHistory.Compare(a, b);
            GUILayout.Label(Strings.Format("ui.h.compare", DateText(a.Time), DateText(b.Time)), EditorStyles.boldLabel);
            CompareRow("ui.h.critical", a.critical, b.critical, false);
            CompareRow("ui.h.warning", a.warning, b.warning, false);
            CompareRow("ui.h.info", a.info, b.info, false);
            CompareRow("ui.h.accepted", a.accepted, b.accepted, null);
            CompareRow("ui.budget.tris", a.sceneTris, b.sceneTris, false);
            CompareRow("ui.budget.drawCalls", a.drawCalls, b.drawCalls, false);
            CompareRow("ui.budget.texMem", a.textureMemoryMB, b.textureMemoryMB, false);
            if (a.hasProfiler && b.hasProfiler)
            {
                CompareRow("ui.m.fps", a.avgFPS, b.avgFPS, true);
                CompareRow("ui.h.cpu", a.avgCPUms, b.avgCPUms, false);
                if (a.avgGPUms >= 0f && b.avgGPUms >= 0f) CompareRow("ui.h.gpu", a.avgGPUms, b.avgGPUms, false);
            }

            GUILayout.Space(6f);
            GUILayout.Label(Strings.Format("ui.h.checks", diff.checkChanges.Count), EditorStyles.boldLabel);
            if (diff.checkChanges.Count == 0) GUILayout.Label(Strings.Get("ui.h.checks.none"), EditorStyles.miniLabel);
            foreach (var change in diff.checkChanges.OrderBy(c => c.Value).ThenBy(c => c.Key, StringComparer.Ordinal))
                CompareRow(CheckTitle(change.Key), a.CountOf(change.Key), b.CountOf(change.Key), false, change.Key);

            GUILayout.Space(6f);
            if (!diff.problemsKnown)
            {
                GUILayout.Label(Strings.Get("ui.h.problems.old"), wrapMini);
                return;
            }
            if (diff.acceptedSince > 0) GUILayout.Label(Strings.Format("ui.h.acceptedSince", diff.acceptedSince), wrapMini);
            histFixedOpen = ProblemList(Strings.Format("ui.h.fixed", diff.fixedProblems.Count), diff.fixedProblems, histFixedOpen);
            histNewOpen = ProblemList(Strings.Format("ui.h.new", diff.newProblems.Count), diff.newProblems, histNewOpen);
        }

        static string CheckTitle(string checkId)
        {
            return Strings.Has(checkId + ".title") ? Strings.Get(checkId + ".title") : checkId;
        }

        // Label, "a → b", change coloured green when better and red when worse (no colour when neither).
        void CompareRow(string keyOrText, double a, double b, bool? higherIsBetter, string tip = "")
        {
            var rect = EditorGUILayout.GetControlRect();
            var label = Strings.Has(keyOrText) ? Strings.Get(keyOrText) : keyOrText;
            float labelWidth = Mathf.Max(LabelColumn, Mathf.Min(320f, rect.width * 0.45f));
            GUI.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), new GUIContent(label, tip));
            float x = rect.x + labelWidth;
            GUI.Label(new Rect(x, rect.y, 180f, rect.height), FormatNumber(a) + " → " + FormatNumber(b));
            double delta = b - a;
            if (Math.Abs(delta) < 0.0001) return;
            var style = new GUIStyle(EditorStyles.label);
            if (higherIsBetter.HasValue) style.normal.textColor = (higherIsBetter.Value ? delta > 0 : delta < 0) ? OkColor : CriticalColor;
            GUI.Label(new Rect(x + 186f, rect.y, Mathf.Max(0f, rect.xMax - x - 186f), rect.height), DeltaText(delta), style);
        }

        bool ProblemList(string title, List<string> problems, bool open)
        {
            open = EditorGUILayout.Foldout(open, title, true, boldFoldout);
            if (!open || problems.Count == 0) return open;
            foreach (var p in problems.Take(GroupPageSize))
            {
                int bar = p.IndexOf('|');
                var id = bar >= 0 ? p.Substring(0, bar) : p;
                var where = bar >= 0 ? p.Substring(bar + 1) : "";
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16f);
                GUILayout.Label(new GUIContent(CheckTitle(id) + " — " + where, id), wrapMini);
                EditorGUILayout.EndHorizontal();
            }
            if (problems.Count > GroupPageSize) GUILayout.Label(Strings.Format("ui.h.more", problems.Count - GroupPageSize), EditorStyles.miniLabel);
            return open;
        }

        static string DeltaText(double delta)
        {
            if (Math.Abs(delta) < 0.0001) return "0";
            return delta > 0 ? "↑ +" + FormatNumber(delta) : "↓ " + FormatNumber(delta);
        }

        static string DateText(DateTime time)
        {
            return time.ToString(Strings.Current == Lang.Ru ? "dd.MM.yyyy HH:mm" : "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
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
