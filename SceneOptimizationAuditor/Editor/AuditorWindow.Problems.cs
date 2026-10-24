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
                GpuLine(result.budgets),
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
    }
}
