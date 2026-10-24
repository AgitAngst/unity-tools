using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public sealed partial class AuditorWindow : EditorWindow
    {
        const int TabProblems = 0;
        const int TabViewpoints = 1;
        const int TabLightmap = 2;
        const int TabLight = 3;
        const int TabBudgets = 4;
        const int TabProfiler = 5;
        const int TabHistory = 6;
        const int GroupPageSize = 100;
        const float LabelColumn = 210f;
        const string ShowPref = "UnityTools.SceneAudit.Show.";

        public static readonly Color CriticalColor = new Color(0.86f, 0.27f, 0.27f);
        public static readonly Color WarningColor = new Color(0.95f, 0.72f, 0.2f);
        public static readonly Color InfoColor = new Color(0.55f, 0.55f, 0.55f);
        public static readonly Color OkColor = new Color(0.3f, 0.7f, 0.35f);

        // Text in a severity colour: a shade per editor skin that keeps 4.5:1 against the background; stripes, dots,
        // backgrounds and icons keep the colours above (owner, 24.10.2026).
        public static Color TextCritical { get { return EditorGUIUtility.isProSkin ? new Color32(0xFF, 0x8A, 0x8A, 0xFF) : new Color32(0x9E, 0x1C, 0x1C, 0xFF); } }
        public static Color TextWarning { get { return EditorGUIUtility.isProSkin ? new Color32(0xF2, 0xB8, 0x33, 0xFF) : new Color32(0x6B, 0x47, 0x00, 0xFF); } }
        public static Color TextOk { get { return EditorGUIUtility.isProSkin ? new Color32(0x5C, 0xC9, 0x68, 0xFF) : new Color32(0x18, 0x5A, 0x22, 0xFF); } }
        // Background of a table row picked by a click: the editor selection colour.
        static Color SelectionColor { get { return EditorGUIUtility.isProSkin ? new Color(0.17f, 0.36f, 0.53f, 0.8f) : new Color(0.23f, 0.45f, 0.69f, 0.32f); } }

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
        // A link in a table cell: one line, cut at the cell (the full text is in the tooltip).
        GUIStyle linkClipped;
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
            ViewpointsEnable();
        }

        void OnDisable()
        {
            ProfilerCapture.Changed -= Repaint;
            ViewpointsDisable();
        }

        void InitStyles()
        {
            if (wrapMini != null) return;
            bool pro = EditorGUIUtility.isProSkin;
            wrapMini = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            hint = new GUIStyle(EditorStyles.helpBox) { wordWrap = true, fontSize = 11, padding = new RectOffset(8, 8, 6, 6) };
            link = new GUIStyle(EditorStyles.label) { wordWrap = true };
            link.normal.textColor = pro ? new Color(0.55f, 0.72f, 1f) : new Color(0.1f, 0.3f, 0.75f);
            linkClipped = new GUIStyle(link) { wordWrap = false, clipping = TextClipping.Clip };
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
                case TabViewpoints:
                    DrawViewpointsTab();
                    break;
                case TabLightmap:
                    DrawLightmapTab();
                    break;
                case TabLight:
                    DrawLightTab();
                    break;
                case TabBudgets:
                    pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
                    DrawBudgetsTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case TabProfiler:
                    pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
                    DrawProfilerTab();
                    EditorGUILayout.EndScrollView();
                    break;
                case TabHistory:
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
            // A lighting check already on the tab follows the new run.
            if (ltReport != null)
            {
                ltReport = LightReport.Build();
                ltPicked.Clear();
            }
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

            // A narrow window gets the version and the language on a second row instead of cutting them.
            if (position.width < 640f)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            }
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
                Strings.Get("ui.tab.viewpoints"),
                Strings.Get("ui.tab.lightmap"),
                Strings.Get("ui.tab.light"),
                Strings.Get("ui.tab.budgets"),
                Strings.Get("ui.tab.profiler"),
                Strings.Get("ui.tab.history")
            };
            // Equal tabs fill a wide window; when the longest label does not fit, every tab is as wide as its label;
            // when even that does not fit, the tabs go on two rows.
            float available = position.width - 12f;
            var widths = labels.Select(l => GUI.skin.button.CalcSize(new GUIContent(l)).x).ToArray();
            int was = tab;
            if (widths.Max() * labels.Length <= available) TabRow(labels, 0, labels.Length, GUI.ToolbarButtonSize.Fixed);
            else if (widths.Sum() <= available) TabRow(labels, 0, labels.Length, GUI.ToolbarButtonSize.FitToContents);
            else
            {
                int first = labels.Length - labels.Length / 2;
                TabRow(labels, 0, first, GUI.ToolbarButtonSize.FitToContents);
                TabRow(labels, first, labels.Length - first, GUI.ToolbarButtonSize.FitToContents);
            }
            if (tab != was) OnTabChanged();
            GUILayout.Space(4f);
        }

        // One row of tabs: labels[from .. from + count); a row without the current tab shows none pressed.
        void TabRow(string[] labels, int from, int count, GUI.ToolbarButtonSize size)
        {
            var row = labels.Skip(from).Take(count).ToArray();
            int selected = tab >= from && tab < from + count ? tab - from : -1;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6f);
            int picked = GUILayout.Toolbar(selected, row, GUI.skin.button, size, GUILayout.Height(24f));
            if (picked != selected && picked >= 0) tab = from + picked;
            GUILayout.Space(6f);
            EditorGUILayout.EndHorizontal();
        }

        List<AuditIssue> SafeFixes()
        {
            return result != null ? result.issues.Where(i => !i.accepted && i.fixKind == FixKind.Safe && i.AutoFixable).ToList() : new List<AuditIssue>();
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
