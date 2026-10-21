using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityTools.SceneOptimization
{
    public sealed class AuditorWindow : EditorWindow
    {
        const int PageSize = 200;
        const float CountColumn = 52f;
        const float LabelColumn = 210f;

        public static readonly Color CriticalColor = new Color(0.86f, 0.27f, 0.27f);
        public static readonly Color WarningColor = new Color(0.95f, 0.72f, 0.2f);
        public static readonly Color InfoColor = new Color(0.55f, 0.55f, 0.55f);
        public static readonly Color OkColor = new Color(0.3f, 0.7f, 0.35f);

        static readonly string[] LangLabels = { "EN", "RU" };
        static readonly Category[] Categories = (Category[])Enum.GetValues(typeof(Category));

        [SerializeField] bool[] showSeverity = { true, true, true };
        [SerializeField] string search = "";
        [SerializeField] bool[] foldouts = new bool[Categories.Length];
        [SerializeField] bool budgetsOpen;
        [SerializeField] bool profilerOpen;
        [SerializeField] bool compare;
        [SerializeField] Vector2 scroll;

        AuditResult result;
        XrInfo xrPreview;
        VRBudgets budgets;
        int[] limits = new int[Categories.Length];

        GUIStyle wrapLabel;
        GUIStyle wrapMini;
        GUIStyle linkMini;
        GUIStyle boldFoldout;
        GUIStyle centeredMini;
        GUIStyle searchField;
        Texture errorIcon;
        Texture warnIcon;
        Texture infoIcon;

        [MenuItem("Tools/VR Scene Optimization Audit")]
        public static void Open()
        {
            var window = GetWindow<AuditorWindow>();
            window.minSize = new Vector2(540f, 420f);
            window.Show();
        }

        void OnEnable()
        {
            budgets = VRBudgets.Load();
            xrPreview = XrInfo.Detect();
            ResetLimits();
            if (foldouts == null || foldouts.Length != Categories.Length) foldouts = new bool[Categories.Length];
            if (showSeverity == null || showSeverity.Length != 3) showSeverity = new[] { true, true, true };
            ProfilerCapture.Changed += Repaint;
        }

        void OnDisable()
        {
            ProfilerCapture.Changed -= Repaint;
        }

        void ResetLimits()
        {
            limits = new int[Categories.Length];
            for (int i = 0; i < limits.Length; i++) limits[i] = PageSize;
        }

        void InitStyles()
        {
            if (wrapLabel != null) return;
            wrapLabel = new GUIStyle(EditorStyles.label) { wordWrap = true, richText = false };
            wrapMini = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            linkMini = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            linkMini.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.55f, 0.7f, 0.95f) : new Color(0.1f, 0.3f, 0.7f);
            boldFoldout = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            centeredMini = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            searchField = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField;
            errorIcon = EditorGUIUtility.IconContent("console.erroricon.sml").image;
            warnIcon = EditorGUIUtility.IconContent("console.warnicon.sml").image;
            infoIcon = EditorGUIUtility.IconContent("console.infoicon.sml").image;
        }

        void OnGUI()
        {
            InitStyles();
            titleContent = new GUIContent(Strings.Get("ui.title"));
            DrawToolbar();
            DrawHeader();
            if (result != null) DrawBudgetOverview();
            DrawFilters();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (result == null) EditorGUILayout.HelpBox(Strings.Get("ui.notRun"), MessageType.Info);
            else DrawCategories();
            EditorGUILayout.Space();
            DrawBudgetEditor();
            DrawProfiler();
            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        // Long work starts after the GUI pass, so progress bars and dialogs do not break the layout.
        void Run()
        {
            var r = SceneAuditor.RunFullAudit(budgets, true);
            if (r == null) return;
            result = r;
            xrPreview = r.xr;
            ResetLimits();
            Repaint();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(Strings.Get("ui.run"), EditorStyles.toolbarButton, GUILayout.Width(130f)))
                EditorApplication.delayCall += Run;
            GUILayout.FlexibleSpace();
            int lang = GUILayout.Toolbar((int)Strings.Current, LangLabels, EditorStyles.toolbarButton, GUILayout.Width(70f));
            if (lang != (int)Strings.Current) Strings.Current = (Lang)lang;
            EditorGUILayout.EndHorizontal();
        }

        void DrawHeader()
        {
            EditorGUILayout.LabelField(Strings.Get("ui.target"), EditorStyles.boldLabel);
            var xr = result != null ? result.xr : xrPreview;
            if (xr != null) EditorGUILayout.LabelField(XrLine(xr) + "    " + StereoLine(xr));
            if (result == null) return;
            var q = result.quality;
            EditorGUILayout.LabelField(Strings.Format(q.isStandaloneDefault ? "ui.quality.default" : "ui.quality.current", q.name));
            EditorGUILayout.LabelField(Strings.Format("ui.scenes", string.Join(", ", result.sceneNames),
                result.time.ToString("HH:mm:ss", CultureInfo.InvariantCulture)), EditorStyles.miniLabel);
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

        void DrawBudgetOverview()
        {
            var b = result.budgets;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Strings.Get("ui.budgetOverview"), EditorStyles.boldLabel);
            BudgetBar(Strings.Get("ui.budget.tris"), result.sceneTris, b.sceneTrisWarning, b.sceneTrisCritical, "");
            BudgetBar(Strings.Get("ui.budget.drawCalls"), result.drawCallEstimate, b.drawCallsWarning, b.drawCallsCritical, "");
            BudgetBar(Strings.Get("ui.budget.texMem"), result.textureMemoryMB, b.textureMemoryWarningMB, b.textureMemoryCriticalMB, " MB");
        }

        void BudgetBar(string label, double value, double warning, double critical, string unit)
        {
            var rect = EditorGUILayout.GetControlRect(false, 18f);
            var labelRect = new Rect(rect.x, rect.y, LabelColumn, rect.height);
            var bar = new Rect(rect.x + LabelColumn, rect.y + 2f, rect.width - LabelColumn, rect.height - 4f);
            GUI.Label(labelRect, label);
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
            GUI.Label(bar, text, centeredMini);
        }

        public static string FormatNumber(double value)
        {
            return Math.Abs(value - Math.Round(value)) < 0.05
                ? ((long)Math.Round(value)).ToString("N0", CultureInfo.InvariantCulture)
                : value.ToString("N1", CultureInfo.InvariantCulture);
        }

        void DrawFilters()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var severities = new[] { Severity.Critical, Severity.Warning, Severity.Info };
            for (int i = 0; i < severities.Length; i++)
            {
                int count = result != null ? result.Count(severities[i]) : 0;
                var content = new GUIContent(" " + count, IconOf(severities[i]), Strings.SeverityName(severities[i]));
                showSeverity[i] = GUILayout.Toggle(showSeverity[i], content, EditorStyles.toolbarButton, GUILayout.Width(64f));
            }
            GUILayout.Space(10f);
            GUILayout.Label(Strings.Get("ui.search"), GUILayout.ExpandWidth(false));
            search = GUILayout.TextField(search ?? "", searchField, GUILayout.MinWidth(120f));
            EditorGUILayout.EndHorizontal();
        }

        bool Matches(AuditIssue issue)
        {
            if (!showSeverity[(int)issue.severity]) return false;
            if (string.IsNullOrEmpty(search)) return true;
            return Contains(issue.objectPath, search) || Contains(issue.assetPath, search) || Contains(issue.Message, search);
        }

        static bool Contains(string text, string part)
        {
            return !string.IsNullOrEmpty(text) && text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void DrawCategories()
        {
            for (int c = 0; c < Categories.Length; c++)
            {
                var category = Categories[c];
                var rect = EditorGUILayout.GetControlRect(false, 20f);
                var foldRect = new Rect(rect.x, rect.y, rect.width - CountColumn * 3f, rect.height);
                foldouts[c] = EditorGUI.Foldout(foldRect, foldouts[c], Strings.CategoryName(category), true, boldFoldout);
                float x = rect.xMax - CountColumn * 3f;
                CountLabel(new Rect(x, rect.y, CountColumn, rect.height), errorIcon, result.Count(category, Severity.Critical));
                CountLabel(new Rect(x + CountColumn, rect.y, CountColumn, rect.height), warnIcon, result.Count(category, Severity.Warning));
                CountLabel(new Rect(x + CountColumn * 2f, rect.y, CountColumn, rect.height), infoIcon, result.Count(category, Severity.Info));
                if (!foldouts[c]) continue;

                var visible = result.issues.Where(i => i.category == category && Matches(i)).ToList();
                if (visible.Count == 0)
                {
                    EditorGUILayout.LabelField("    " + Strings.Get("ui.noIssues"), EditorStyles.miniLabel);
                    continue;
                }
                int shown = Mathf.Min(visible.Count, limits[c]);
                for (int k = 0; k < shown; k++) DrawIssue(visible[k]);
                if (visible.Count > shown && GUILayout.Button(Strings.Format("ui.showMore", Mathf.Min(PageSize, visible.Count - shown))))
                    limits[c] += PageSize;
            }
        }

        static void CountLabel(Rect rect, Texture icon, int count)
        {
            GUI.Label(rect, new GUIContent(" " + count, icon));
        }

        void DrawIssue(AuditIssue issue)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);
            GUILayout.Label(IconOf(issue.severity), GUILayout.Width(18f), GUILayout.Height(18f));

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button(issue.Message, wrapLabel)) Select(issue);
            var location = issue.Location;
            if (issue.users > 0) location += (location.Length > 0 ? "  ·  " : "") + Strings.Format("ui.users", issue.users);
            if (location.Length > 0 && GUILayout.Button(location, linkMini)) Select(issue);
            var suggestion = issue.Suggestion;
            if (suggestion.Length > 0) GUILayout.Label(suggestion, wrapMini);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(84f));
            if (issue.AutoFixable && GUILayout.Button(Strings.Get("ui.fix")))
            {
                var one = issue;
                EditorApplication.delayCall += () => AutoFixer.Confirm(new List<AuditIssue> { one }, AfterFix);
            }
            if (!string.IsNullOrEmpty(issue.settingsPath) && GUILayout.Button(Strings.Get("ui.openSettings")))
                SettingsService.OpenProjectSettings(issue.settingsPath);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            var row = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(row.x + 6f, row.y + 2f, 3f, Mathf.Max(0f, row.height - 4f)), SeverityColor(issue.severity));
            GUILayout.Space(4f);
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

        void AfterFix()
        {
            EditorApplication.delayCall += Run;
        }

        void DrawBudgetEditor()
        {
            budgetsOpen = EditorGUILayout.Foldout(budgetsOpen, Strings.Get("ui.budgets"), true, boldFoldout);
            if (!budgetsOpen) return;
            EditorGUI.indentLevel++;

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

            EditorGUILayout.LabelField(VRBudgets.FileExists ? Strings.Format("ui.budgets.file", VRBudgets.FilePath) : Strings.Get("ui.budgets.defaults.note"),
                wrapMini);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.budgets.reset"), GUILayout.Width(120f))) budgets = new VRBudgets();
            if (GUILayout.Button(Strings.Get("ui.budgets.save"), GUILayout.Width(120f))) budgets.Save();
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        void IntPair(string key, ref int warning, ref int critical)
        {
            var rect = EditorGUILayout.GetControlRect();
            float column = (rect.width - LabelColumn) / 2f;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), Strings.Get(key));
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            warning = Mathf.Max(0, EditorGUI.IntField(new Rect(rect.x + LabelColumn, rect.y, column - 4f, rect.height), warning));
            critical = Mathf.Max(warning, EditorGUI.IntField(new Rect(rect.x + LabelColumn + column, rect.y, column - 4f, rect.height), critical));
            EditorGUI.indentLevel = indent;
        }

        void FloatPair(string key, ref float warning, ref float critical)
        {
            var rect = EditorGUILayout.GetControlRect();
            float column = (rect.width - LabelColumn) / 2f;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), Strings.Get(key));
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            warning = Mathf.Max(0f, EditorGUI.FloatField(new Rect(rect.x + LabelColumn, rect.y, column - 4f, rect.height), warning));
            critical = Mathf.Max(warning, EditorGUI.FloatField(new Rect(rect.x + LabelColumn + column, rect.y, column - 4f, rect.height), critical));
            EditorGUI.indentLevel = indent;
        }

        void IntSingle(string key, ref int value)
        {
            var rect = EditorGUILayout.GetControlRect();
            float column = (rect.width - LabelColumn) / 2f;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, LabelColumn, rect.height), Strings.Get(key));
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            value = Mathf.Max(0, EditorGUI.IntField(new Rect(rect.x + LabelColumn, rect.y, column - 4f, rect.height), value));
            EditorGUI.indentLevel = indent;
        }

        void DrawProfiler()
        {
            profilerOpen = EditorGUILayout.Foldout(profilerOpen, Strings.Get("ui.prof.title"), true, boldFoldout);
            if (!profilerOpen) return;
            EditorGUI.indentLevel++;

            bool running = ProfilerCapture.IsRunning;
            EditorGUI.BeginDisabledGroup(running);
            ProfilerCapture.Frames = Mathf.Clamp(EditorGUILayout.IntField(Strings.Get("ui.prof.frames"), ProfilerCapture.Frames), 30, 5000);
            ProfilerCapture.Warmup = Mathf.Clamp(EditorGUILayout.IntField(Strings.Get("ui.prof.warmup"), ProfilerCapture.Warmup), 0, 1000);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15f);
            if (!running)
            {
                if (GUILayout.Button(Strings.Get("ui.prof.capture"), GUILayout.Width(200f)))
                    EditorApplication.delayCall += ProfilerCapture.Start;
            }
            else if (GUILayout.Button(Strings.Get("ui.prof.cancel"), GUILayout.Width(200f)))
            {
                ProfilerCapture.Cancel();
            }
            GUILayout.Label(ProfilerCapture.StatusText(), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            compare = EditorGUILayout.ToggleLeft(Strings.Get("ui.prof.compare"), compare);
            EditorGUILayout.HelpBox(Strings.Get("ui.prof.note") + "\n" + Strings.Get("ui.prof.cpuNote"), MessageType.None);

            var scenePath = SceneManager.GetActiveScene().path;
            var last = ProfilerCapture.LoadLast(scenePath);
            var previous = compare ? ProfilerCapture.LoadPrevious(scenePath) : null;
            if (last == null)
            {
                EditorGUILayout.LabelField(Strings.Get("ui.prof.none"), EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField(Strings.Format("ui.prof.taken", last.timestamp, last.frames), EditorStyles.miniLabel);
                DrawSnapshot(last, previous);
                if (compare && previous == null) EditorGUILayout.LabelField(Strings.Get("ui.prof.noPrevious"), EditorStyles.miniLabel);
            }
            EditorGUI.indentLevel--;
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
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            GUI.Label(new Rect(x + 14f, rect.y, 170f, rect.height), value);
            if (delta.HasValue && Math.Abs(delta.Value) > 0.0001)
            {
                bool better = higherIsBetter ? delta.Value > 0 : delta.Value < 0;
                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = better ? OkColor : CriticalColor;
                var arrow = delta.Value > 0 ? "↑ +" : "↓ ";
                GUI.Label(new Rect(x + 190f, rect.y, rect.xMax - x - 190f, rect.height), arrow + FormatNumber(delta.Value), style);
            }
            EditorGUI.indentLevel = indent;
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

        void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (result != null)
                GUILayout.Label(Strings.Format("ui.summary", result.Count(Severity.Critical), result.Count(Severity.Warning), result.Count(Severity.Info)));
            GUILayout.FlexibleSpace();

            var safe = result != null ? result.issues.Where(i => i.fixKind == FixKind.Safe && i.AutoFixable).ToList() : new List<AuditIssue>();
            EditorGUI.BeginDisabledGroup(safe.Count == 0);
            if (GUILayout.Button(Strings.Format("ui.autoFix", safe.Count)))
                EditorApplication.delayCall += () => AutoFixer.Confirm(safe, AfterFix);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(result == null);
            if (GUILayout.Button(Strings.Get("ui.export")))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("JSON…"), false, () => Export("json"));
                menu.AddItem(new GUIContent("CSV…"), false, () => Export("csv"));
                menu.ShowAsContext();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

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
    }
}
