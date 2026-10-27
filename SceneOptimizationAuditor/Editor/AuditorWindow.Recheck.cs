using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public sealed partial class AuditorWindow
    {
        // ---- quick re-check (2.7.0, owner: "a quick re-check of a single element, to update what I changed") ----

        const float RecheckIconWidth = 22f;

        bool CanRecheck(AuditIssue issue)
        {
            return result != null && Recheck.KindOf(issue, result) <= RecheckKind.Setting;
        }

        // Why a row cannot be re-checked, or what its re-check takes.
        string RecheckTip(AuditIssue issue)
        {
            switch (Recheck.KindOf(issue, result))
            {
                case RecheckKind.Object: return Strings.Format("ui.rc.tip", issue.objectPath.Length > 0 ? LastName(issue.objectPath) : issue.Location);
                case RecheckKind.Asset: return Strings.Format("ui.rc.tip.asset", System.IO.Path.GetFileName(issue.assetPath));
                case RecheckKind.Setting: return Strings.Get("ui.rc.tip.setting");
                case RecheckKind.Closed: return Strings.Get("ui.rc.tip.closed");
                case RecheckKind.Folder: return Strings.Get("ui.rc.tip.folder");
                default: return Strings.Get("ui.rc.tip.scene");
            }
        }

        static string LastName(string path)
        {
            var slash = path.LastIndexOf('/');
            return slash >= 0 ? path.Substring(slash + 1) : path;
        }

        // The icon at the end of a row: active when the row can be re-checked, otherwise greyed out with the reason.
        void RecheckIcon(AuditIssue issue)
        {
            bool can = CanRecheck(issue);
            EditorGUI.BeginDisabledGroup(!can);
            if (GUILayout.Button(new GUIContent(refreshIcon, RecheckTip(issue)), EditorStyles.miniButton, GUILayout.Width(RecheckIconWidth), GUILayout.Height(18f)))
            {
                var one = issue;
                EditorApplication.delayCall += () => RecheckRows(new List<AuditIssue> { one });
            }
            EditorGUI.EndDisabledGroup();
        }

        // The icon of a group header or a copies row: every row of it that can be re-checked.
        void RecheckIcon(Rect rect, List<AuditIssue> rows, string tipKey)
        {
            var can = rows.Where(CanRecheck).ToList();
            var tip = can.Count > 0 ? Strings.Format(tipKey, can.Count) : rows.Count > 0 ? RecheckTip(rows[0]) : "";
            EditorGUI.BeginDisabledGroup(can.Count == 0);
            if (GUI.Button(rect, new GUIContent(refreshIcon, tip), EditorStyles.miniButton))
                EditorApplication.delayCall += () => RecheckRows(can);
            EditorGUI.EndDisabledGroup();
        }

        // Checks the rows again without a full run; the rows of their objects, assets and settings are replaced.
        void RecheckRows(List<AuditIssue> rows)
        {
            if (result == null) return;
            var r = result;
            var summary = Recheck.Run(r, rows.Where(CanRecheck).ToList(), budgets, true);
            if (summary == null) return;
            if (summary.items == 0)
            {
                ShowNotification(new GUIContent(Strings.Get("ui.rc.nothing")));
                return;
            }
            groups = IssueGroup.Build(r.issues);
            picked.RemoveWhere(i => !r.issues.Contains(i));
            // Copies of a prefab are counted again from the new rows.
            totalsFor = null;
            ListChanged();
            var items = Strings.Count(summary.onlyObjects ? "ui.rc.objects" : "ui.rc.items", summary.items);
            var text = Strings.Format("ui.rc.done", items, FormatNumber(System.Math.Max(0.1, System.Math.Round(summary.seconds, 1))),
                Strings.Count("ui.rc.rows", summary.went), Strings.Count("ui.rc.rows", summary.left));
            if (summary.added > 0) text += Strings.Format("ui.rc.done.new", Strings.Count("ui.rc.rows", summary.added));
            ShowNotification(new GUIContent(text));
            Repaint();
        }

        // Used by the screenshot test: re-checks the first rows of the open groups.
        void RecheckForShot()
        {
            var rows = groups.Where(g => { bool open; return IsShown(g) && expanded.TryGetValue(g.key, out open) && open; })
                .SelectMany(g => g.issues).Where(CanRecheck).Take(3).ToList();
            if (rows.Count == 0) rows = result.issues.Where(CanRecheck).Take(3).ToList();
            RecheckRows(rows);
        }

        // "re-checked at 14:41" after the text of a row a quick re-check confirmed.
        static string RecheckMark(AuditIssue issue)
        {
            return issue.recheckedAt == default(System.DateTime) ? "" :
                "  ·  " + Strings.Format("ui.rc.mark", issue.recheckedAt.ToString("HH:mm", CultureInfo.InvariantCulture));
        }
    }
}
