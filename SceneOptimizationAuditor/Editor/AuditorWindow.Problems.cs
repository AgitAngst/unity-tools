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
            if (result == null || startShown)
            {
                DrawStart();
                return;
            }
            DrawVerdict();
            DrawSeverityCards();
            DrawContext();
            DrawScopeBudgets();
            DrawGainTotal();
            DrawListToolbar();

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            int drawn = 0;
            if (byCategory)
            {
                foreach (var category in Categories)
                {
                    var inCategory = Ordered(groups.Where(g => g.category == category && IsShown(g) && Visible(g).Count > 0)).ToList();
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
                foreach (var g in Ordered(groups)) if (IsShown(g)) drawn += DrawGroup(g, drawn);
            }
            if (drawn == 0) DrawNothing();
            GUILayout.Space(8f);
            EditorGUILayout.EndScrollView();
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
                HeadsetLine(result.budgets),
                ScopeContext()
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
            ByGain = GUILayout.Toggle(ByGain, new GUIContent(Strings.Get("ui.gain.sort"), Strings.Get("ui.gain.sort.tip")),
                EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            CopiesTogether = GUILayout.Toggle(CopiesTogether, new GUIContent(Strings.Get("ui.copies"), Strings.Get("ui.copies.tip")),
                EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            var button = EditorStyles.toolbarButton;
            float toggles = 230f + button.CalcSize(new GUIContent(Strings.Get("ui.byCategory"))).x + button.CalcSize(new GUIContent(Strings.Get("ui.gain.sort"))).x +
                            button.CalcSize(new GUIContent(Strings.Get("ui.copies"))).x;
            float expanders = button.CalcSize(new GUIContent(Strings.Get("ui.expandAll"))).x + button.CalcSize(new GUIContent(Strings.Get("ui.collapseAll"))).x;
            float rest = button.CalcSize(new GUIContent(Strings.Format("ui.acc.show", 0) + "000")).x + button.CalcSize(new GUIContent(Strings.Get("ui.sel.all"))).x;
            // What does not fit goes on a second row: first the filter and "Select all", then also "Expand all" and "Collapse all".
            bool expandersBelow = toggles + expanders > position.width;
            if (expandersBelow) NextToolbarRow();
            if (GUILayout.Button(Strings.Get("ui.expandAll"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                foreach (var g in groups) expanded[g.key] = true;
            if (GUILayout.Button(Strings.Get("ui.collapseAll"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                expanded.Clear();
            if (!expandersBelow && toggles + expanders + rest > position.width) NextToolbarRow();
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

        void NextToolbarRow()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
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
            // Counted again only when the pick or the list changes.
            var planKey = picked.Count + "|" + listVersion + "|" + CopiesTogether;
            if (planKey != pickedPlanKey)
            {
                pickedPlanKey = planKey;
                pickedPlanCount = PrefabFixes.PlanCount(picked.ToList(), CopiesTogether, Totals());
            }
            int fixCount = pickedPlanCount;
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
            EditorGUI.BeginDisabledGroup(fixCount == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.sel.fix", fixCount), Strings.Get(CopiesTogether ? "ui.fix.plan.tip" : "ui.fix.tip")), GUILayout.ExpandWidth(false)))
            {
                var plan = PrefabFixes.Plan(picked.ToList(), CopiesTogether, Totals());
                EditorApplication.delayCall += () => AutoFixer.Confirm(plan, AfterFix);
            }
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
                ListChanged();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(toRelease.Count == 0);
            if (GUILayout.Button(new GUIContent(Strings.Format("ui.acc.release", toRelease.Count), Strings.Get("ui.acc.release.tip")), GUILayout.ExpandWidth(false)))
            {
                Accepted().Release(toRelease);
                picked.Clear();
                ListChanged();
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
                    ListChanged();
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
            // With copies together a copies row is fixed once in its prefab: the count is that of fix entries.
            int fixCount = CopiesTogether ? PlanCountOf(GroupRows(g, visible)) : visible.Count(i => i.AutoFixable && !i.accepted);
            if (fixCount > 0)
            {
                var label = fixCount == 1 ? Strings.Get("ui.fix") : Strings.Format("ui.fixAll", fixCount);
                var width = EditorStyles.miniButton.CalcSize(new GUIContent(label)).x + 8f;
                var button = new Rect(right - width, rect.y + 4f, width, 18f);
                if (GUI.Button(button, new GUIContent(label, Strings.Get(CopiesTogether ? "ui.fix.plan.tip" : "ui.fix.tip")), EditorStyles.miniButton))
                {
                    var plan = PrefabFixes.Plan(visible, CopiesTogether, Totals());
                    EditorApplication.delayCall += () => AutoFixer.Confirm(plan, AfterFix);
                }
                right = button.x - 4f;
            }
            var settings = visible.Select(i => i.settingsPath).FirstOrDefault(p => !string.IsNullOrEmpty(p));
            // Lighting settings live in the Lighting window, the rest on Project Settings pages.
            if (settings != null)
            {
                var label = Strings.Get("ui.openSettings");
                var width = EditorStyles.miniButton.CalcSize(new GUIContent(label)).x + 8f;
                var button = new Rect(right - width, rect.y + 4f, width, 18f);
                if (GUI.Button(button, label, EditorStyles.miniButton)) OpenSettings(settings);
                right = button.x - 4f;
            }
            if (!byCategory)
            {
                var name = Strings.CategoryName(g.category);
                var width = EditorStyles.miniLabel.CalcSize(new GUIContent(name)).x + 6f;
                GUI.Label(new Rect(right - width, rect.y + 5f, width, 16f), name, chip);
                right -= width + 4f;
            }
            var gain = FixGain.Sum(visible);
            if (gain.Any)
            {
                var text = GainText(gain.mb, gain.drawCalls, gain.tris, gain.approx, false);
                var width = gainHead.CalcSize(new GUIContent(text)).x + 6f;
                GUI.Label(new Rect(right - width, rect.y + 5f, width, 16f), new GUIContent(text, Strings.Get("ui.gain.group.tip")), gainHead);
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

            DrawAdvice(g, visible);
            int limit;
            if (!limits.TryGetValue(g.key, out limit)) limit = GroupPageSize;
            // Rows: single issues and, with copies together, copies of one source as one row.
            var rows = CopiesTogether ? GroupRows(g, visible) : visible.Select(i => new IssueRow { issue = i }).ToList();
            int shown = Mathf.Min(limit, rows.Count);
            for (int k = 0; k < shown; k++)
            {
                if (rows[k].copies != null) DrawCopyRow(g, rows[k].copies);
                else DrawIssueRow(rows[k].issue, visible.Count > 1, 40f);
            }
            if (rows.Count > shown)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(40f);
                if (GUILayout.Button(Strings.Format("ui.showMore", Mathf.Min(GroupPageSize, rows.Count - shown)), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    limits[g.key] = limit + GroupPageSize;
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(6f);
            return 1;
        }

        // Copies of one prefab or model object: one row with the source and the count, opened to the copies one by one.
        void DrawCopyRow(IssueGroup g, CopyRow row)
        {
            var key = g.key + "|" + row.source.GetInstanceID();
            bool open = openCopies.Contains(key);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24f);
            var arrow = GUILayoutUtility.GetRect(14f, 16f, GUILayout.Width(14f), GUILayout.Height(16f));
            bool now = EditorGUI.Foldout(arrow, open, GUIContent.none, true);
            GUILayout.Space(2f);
            if (now != open)
            {
                if (now) openCopies.Add(key);
                else openCopies.Remove(key);
            }
            var pickable = row.issues.Where(MultiSelect.CanPick).ToList();
            bool all = pickable.Count > 0 && pickable.All(picked.Contains);
            bool on = GUILayout.Toggle(all, new GUIContent("", Strings.Get("ui.copies.pick.tip")), GUILayout.Width(18f));
            if (on != all)
                foreach (var issue in pickable)
                    if (on) picked.Add(issue);
                    else picked.Remove(issue);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            var label = new GUIContent(row.Label, row.assetPath + "\n" + Strings.Get("ui.copies.select.tip"));
            if (GUILayout.Button(label, link, GUILayout.ExpandWidth(false))) MultiSelect.Select(MultiSelect.SelectionOf(row.issues));
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUILayout.Label(Strings.Format("ui.copies.count", row.Copies), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            var first = row.issues[0];
            GUILayout.Label(new GUIContent(first.Message, first.Suggestion), wrapMini);
            EditorGUILayout.EndVertical();
            var rowGain = FixGain.Sum(row.issues);
            if (rowGain.Any)
                GUILayout.Label(new GUIContent(GainText(rowGain.mb, rowGain.drawCalls, rowGain.tris, rowGain.approx, false), Strings.Get("ui.gain.tip")), gainRow,
                    GUILayout.ExpandWidth(false));
            var fixable = row.Fixable;
            if (row.CanFixInPrefab)
            {
                var tip = Strings.Format("ui.fix.inPrefab.scope", row.assetPath, row.Copies);
                if (GUILayout.Button(new GUIContent(Strings.Get("ui.fix.inPrefab"), tip), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    var entry = PrefabFixes.PrefabFix(row);
                    EditorApplication.delayCall += () => AutoFixer.Confirm(new List<AuditIssue> { entry }, AfterFix);
                }
            }
            else if (fixable.Count > 0)
            {
                if (GUILayout.Button(new GUIContent(Strings.Format("ui.fix.copies", fixable.Count), Strings.Get("ui.fix.copies.tip")), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    EditorApplication.delayCall += () => AutoFixer.Confirm(fixable, AfterFix);
            }
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2f);
            if (!now) return;
            int limit;
            if (!limits.TryGetValue(key, out limit)) limit = GroupPageSize;
            int shown = Mathf.Min(limit, row.issues.Count);
            for (int k = 0; k < shown; k++) DrawIssueRow(row.issues[k], true, 62f);
            if (row.issues.Count > shown)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(62f);
                if (GUILayout.Button(Strings.Format("ui.showMore", Mathf.Min(GroupPageSize, row.issues.Count - shown)), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    limits[key] = limit + GroupPageSize;
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawIssueRow(AuditIssue issue, bool ownFix, float indent)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            if (MultiSelect.CanPick(issue))
            {
                bool on = picked.Contains(issue);
                bool now = GUILayout.Toggle(on, new GUIContent("", Strings.Get("ui.sel.pick.tip")), GUILayout.Width(18f));
                if (now && !on) picked.Add(issue);
                else if (!now && on) picked.Remove(issue);
            }
            else GUILayout.Space(22f);
            EditorGUILayout.BeginVertical();
            var location = RowLocation(issue);
            if (issue.users > 0) location += (location.Length > 0 ? "  ·  " : "") + Strings.Format("ui.users", issue.users);
            if (issue.inScenes > 1) location += "  ·  " + Strings.Format("ui.scope.inScenes", issue.inScenes);
            bool closed = IsClosed(issue);
            if (closed) location += "  ·  " + Strings.Get("ui.scope.closed");
            if (location.Length > 0)
            {
                var tip = closed ? "ui.scope.openScene.tip" : !ReferenceEquals(issue.target, null) ? "ui.select.tip"
                    : issue.settingsPath == SceneAuditor.LightingWindow ? "ui.lighting.tip" : "ui.settings.tip";
                if (GUILayout.Button(new GUIContent(location, Strings.Get(tip)), link)) SelectOrOpen(issue);
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
            var message = issue.accepted ? Strings.Get("ui.acc.tag") + "  ·  " + issue.Message : issue.Message;
            GUILayout.Label(new GUIContent(message, issue.Suggestion), wrapMini);
            EditorGUILayout.EndVertical();
            if (issue.HasGain)
                GUILayout.Label(new GUIContent(GainText(issue.gainMB, issue.gainDrawCalls, issue.gainTris, issue.gainApprox, false), Strings.Get("ui.gain.tip")),
                    gainRow, GUILayout.ExpandWidth(false));
            if (ownFix && issue.AutoFixable && !issue.accepted)
            {
                if (GUILayout.Button(new GUIContent(Strings.Get("ui.fix"), issue.FixDescription), EditorStyles.miniButton, GUILayout.Width(84f)))
                {
                    var one = issue;
                    EditorApplication.delayCall += () => AutoFixer.Confirm(new List<AuditIssue> { one }, AfterFix);
                }
            }
            else if (closed)
            {
                if (GUILayout.Button(new GUIContent(Strings.Get("ui.scope.openScene"), Strings.Get("ui.scope.openScene.tip")), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    var scenePath = issue.scenePath;
                    var objectPath = issue.objectPath;
                    EditorApplication.delayCall += () => OpenSceneOf(scenePath, objectPath);
                }
            }
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }

        // Groups by the share of the budgets their fixes free when "By gain" is on; groups without a gain keep their order.
        IEnumerable<IssueGroup> Ordered(IEnumerable<IssueGroup> list)
        {
            if (!ByGain || result == null) return list;
            var b = result.budgets;
            return list.OrderByDescending(g => FixGain.GroupShare(Visible(g), b));
        }

        // "−34 MB  ·  −120 DC"; the total names the units in full.
        static string GainText(double mb, long drawCalls, long tris, bool approx, bool longUnits)
        {
            var parts = new List<string>();
            if (mb >= 0.05) parts.Add(Strings.Format(longUnits ? "ui.gain.mb.long" : "ui.gain.mb", FormatNumber(mb >= 10 ? Math.Round(mb) : Math.Round(mb, 1))));
            if (drawCalls > 0) parts.Add(Strings.Format(longUnits ? "ui.gain.dc.long" : "ui.gain.dc", FormatNumber(drawCalls)));
            if (tris > 0) parts.Add(Strings.Format(longUnits ? "ui.gain.tris.long" : "ui.gain.tris", FormatNumber(tris)));
            var text = string.Join("  ·  ", parts.ToArray());
            return approx ? "≈ " + text : text;
        }

        // What fixing every problem that is not acceptable would free, under the budget bars.
        void DrawGainTotal()
        {
            var total = FixGain.Sum(result.issues);
            if (!total.Any) return;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Get("ui.gain.total"), EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            GUILayout.Label(new GUIContent(GainText(total.mb, total.drawCalls, total.tris, total.approx, true), Strings.Get("ui.gain.total.tip")), gainTotal);
            EditorGUILayout.EndHorizontal();
            GUILayout.Label(Strings.Get("ui.gain.total.note"), wrapMini);
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        static void Select(AuditIssue issue)
        {
            if (issue.target == null)
            {
                if (!string.IsNullOrEmpty(issue.settingsPath)) OpenSettings(issue.settingsPath);
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

        // The list changed (a run, acceptable marks): rows and plans are built again.
        void ListChanged()
        {
            // The table of scenes counts without the acceptable rows.
            if (result != null && result.parts != null) AuditScope.Summarize(result);
            listVersion++;
            rowCache.Clear();
            pickedPlanKey = "";
        }

        CopyTotals Totals()
        {
            if (totalsFor != result)
            {
                totalsFor = result;
                copyTotals = result != null ? CopyTotals.Of(result.allIssues ?? result.issues) : new CopyTotals();
            }
            return copyTotals;
        }

        // Rows of a group with copies together, kept until the visible issues change.
        List<IssueRow> GroupRows(IssueGroup g, List<AuditIssue> visible)
        {
            var key = g.key + "|" + visible.Count + "|" + listVersion + "|" + showAccepted + "|" + search;
            List<IssueRow> rows;
            if (!rowCache.TryGetValue(key, out rows))
            {
                rows = PrefabFixes.Rows(visible, Totals());
                rowCache[key] = rows;
            }
            return rows;
        }

        static int PlanCountOf(List<IssueRow> rows)
        {
            int n = 0;
            foreach (var r in rows)
            {
                if (r.copies == null) n += r.issue.AutoFixable && !r.issue.accepted ? 1 : 0;
                else n += r.copies.CanFixInPrefab ? 1 : r.copies.Fixable.Count;
            }
            return n;
        }

        // Used by the screenshot test: only the group of a check open, with its first copies row opened too.
        void CopiesForShot(string checkId)
        {
            expanded.Clear();
            foreach (var g in groups.Where(g => g.checkId == checkId))
            {
                expanded[g.key] = true;
                var row = GroupRows(g, Visible(g)).FirstOrDefault(r => r.copies != null);
                if (row != null) openCopies.Add(g.key + "|" + row.copies.source.GetInstanceID());
            }
        }

        internal static void OpenSettings(string path)
        {
            if (path != SceneAuditor.LightingWindow) SettingsService.OpenProjectSettings(path);
            else if (!EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting Settings"))
                Debug.LogWarning("[SceneAudit] No Window/Rendering/Lighting Settings menu item in this editor.");
        }
    }
}
