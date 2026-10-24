using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    // Issues of copies of one prefab or model object: one row of a group instead of a row per copy.
    public sealed class CopyRow
    {
        // The object in the nearest prefab or model asset the copies come from.
        public Object source;
        public string assetPath = "";
        // Path of the object inside the asset, without the asset's root ("" for the root itself).
        public string pathInAsset = "";
        public readonly List<AuditIssue> issues = new List<AuditIssue>();
        // The row holds every issue of this check on copies of the source in the run, none of them acceptable: only then
        // may the prefab change, since that changes every copy.
        public bool complete = true;

        // Scene objects of the copies (two issues on one object are one copy).
        public int Copies
        {
            get { return issues.Select(i => i.target is Component ? ((Component)i.target).gameObject : i.target).Distinct().Count(); }
        }

        public string Label
        {
            get { return Path.GetFileName(assetPath) + (pathInAsset.Length > 0 ? " › " + pathInAsset : ""); }
        }

        // Fixable issues of copies still in the scene.
        public List<AuditIssue> Fixable { get { return issues.Where(i => i.AutoFixable && !i.accepted && i.fixTarget != null).ToList(); } }

        bool? canFix;

        // The whole set of copies is here, the prefab can be edited and every fix changes a scene object of a copy.
        // Worked out once per row: rows are rebuilt when the list changes.
        public bool CanFixInPrefab
        {
            get
            {
                if (!canFix.HasValue) canFix = Decide();
                return canFix.Value;
            }
        }

        bool Decide()
        {
            if (!complete) return false;
            var fixable = Fixable;
            return fixable.Count > 1 && fixable.Count == issues.Count && PrefabFixes.Editable(source, assetPath) &&
                   fixable.All(i => !i.fixTouchesAsset && !EditorUtility.IsPersistent(i.fixTarget));
        }
    }

    // How many issues of each check every source has in the run, and how many of them are acceptable.
    public sealed class CopyTotals
    {
        public readonly Dictionary<string, int> all = new Dictionary<string, int>();
        public readonly Dictionary<string, int> accepted = new Dictionary<string, int>();

        public static CopyTotals Of(IEnumerable<AuditIssue> issues)
        {
            var totals = new CopyTotals();
            foreach (var issue in issues)
            {
                var source = PrefabFixes.SourceOf(issue);
                if (source == null) continue;
                var key = PrefabFixes.Key(issue, source);
                int n;
                totals.all.TryGetValue(key, out n);
                totals.all[key] = n + 1;
                totals.accepted.TryGetValue(key, out n);
                totals.accepted[key] = n + (issue.accepted ? 1 : 0);
            }
            return totals;
        }
    }

    // One row of a group: a single issue or the copies of one source.
    public sealed class IssueRow
    {
        public AuditIssue issue;
        public CopyRow copies;
    }

    public static class PrefabFixes
    {
        const string UndoName = "Scene Audit Fix";

        // The object in the nearest prefab or model asset the issue's scene object is a copy of; null for objects that are
        // not copies, for assets and settings. Looked up once per issue.
        public static Object SourceOf(AuditIssue issue)
        {
            if (issue.sourceKnown) return issue.source;
            issue.sourceKnown = true;
            var t = issue.target;
            if (t != null && !EditorUtility.IsPersistent(t) && PrefabUtility.IsPartOfPrefabInstance(t))
                issue.source = PrefabUtility.GetCorrespondingObjectFromSource(t);
            return issue.source;
        }

        // A prefab or variant in Assets/ that is not a model and not otherwise read-only.
        public static bool Editable(Object source, string assetPath)
        {
            if (source == null || !AuditContext.IsProjectAssetPath(assetPath)) return false;
            if (PrefabUtility.IsPartOfImmutablePrefab(source)) return false;
            return PrefabUtility.GetPrefabAssetType(source) != PrefabAssetType.Model;
        }

        public static string Key(AuditIssue issue, Object source)
        {
            return issue.checkId + "|" + source.GetInstanceID();
        }

        // Rows in the order of the issues: two or more issues of one check with one source become a copies row. With the
        // totals of the run, a row that misses some copies (a search, a selection, acceptable ones) is marked incomplete.
        public static List<IssueRow> Rows(IList<AuditIssue> issues, CopyTotals totals = null)
        {
            var rows = new List<IssueRow>();
            var byKey = new Dictionary<string, IssueRow>();
            foreach (var issue in issues)
            {
                var source = SourceOf(issue);
                if (source == null)
                {
                    rows.Add(new IssueRow { issue = issue });
                    continue;
                }
                var key = Key(issue, source);
                IssueRow row;
                if (!byKey.TryGetValue(key, out row))
                {
                    row = new IssueRow { issue = issue, copies = new CopyRow { source = source, assetPath = AssetDatabase.GetAssetPath(source), pathInAsset = PathInAsset(source) } };
                    byKey[key] = row;
                    rows.Add(row);
                }
                row.copies.issues.Add(issue);
            }
            // A source with one copy here is an ordinary row.
            foreach (var row in rows)
            {
                if (row.copies == null) continue;
                if (row.copies.issues.Count < 2)
                {
                    row.copies = null;
                    continue;
                }
                if (totals == null) continue;
                var key = Key(row.copies.issues[0], row.copies.source);
                int all, accepted;
                totals.all.TryGetValue(key, out all);
                totals.accepted.TryGetValue(key, out accepted);
                row.copies.complete = all == row.copies.issues.Count && accepted == 0;
            }
            return rows;
        }

        static string PathInAsset(Object source)
        {
            var component = source as Component;
            var go = component != null ? component.gameObject : source as GameObject;
            if (go == null) return "";
            var path = AuditContext.PathOf(go.transform);
            int slash = path.IndexOf('/');
            return slash >= 0 ? path.Substring(slash + 1) : "";
        }

        // How many fix entries the plan has, without building them.
        public static int PlanCount(IList<AuditIssue> issues, bool together, CopyTotals totals = null)
        {
            var fixable = issues.Where(i => i.AutoFixable && !i.accepted).ToList();
            if (!together) return fixable.Count;
            return Rows(fixable, totals).Sum(r => r.copies == null ? 1 : r.copies.CanFixInPrefab ? 1 : r.copies.issues.Count);
        }

        // Fix entries for the confirmation window: with copies together, one prefab fix per complete copies row that allows
        // it; acceptable issues are not fixed.
        public static List<AuditIssue> Plan(IList<AuditIssue> issues, bool together, CopyTotals totals = null)
        {
            var fixable = issues.Where(i => i.AutoFixable && !i.accepted).ToList();
            if (!together) return fixable;
            var plan = new List<AuditIssue>();
            foreach (var row in Rows(fixable, totals))
            {
                if (row.copies == null) plan.Add(row.issue);
                else if (row.copies.CanFixInPrefab) plan.Add(PrefabFix(row.copies));
                else plan.AddRange(row.copies.issues);
            }
            return plan;
        }

        // The fix of a copies row made once in the prefab: listed with the assets, it names the prefab and the copies.
        public static AuditIssue PrefabFix(CopyRow row)
        {
            var copies = row.Fixable;
            var first = copies[0];
            var entry = new AuditIssue
            {
                checkId = first.checkId,
                messageKey = first.messageKey,
                category = first.category,
                severity = first.severity,
                target = row.source,
                assetPath = row.assetPath,
                args = first.args,
                fixScope = Strings.Format("ui.fix.inPrefab.scope", row.assetPath, row.Copies),
                inPrefab = true
            };
            var path = row.assetPath;
            entry.SetFix(FixKind.Review, row.source, true, () => Apply(copies, path));
            return entry;
        }

        // Fixes one copy, applies the properties the fix changed to the prefab, then fixes the copies that keep their own
        // value of any of them.
        static void Apply(List<AuditIssue> copies, string assetPath)
        {
            // The first copy still in the scene does the fix.
            var first = copies.FirstOrDefault(c => c.fixTarget != null);
            if (first == null) return;
            var target = first.fixTarget;
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(target);
            var source = PrefabUtility.GetCorrespondingObjectFromSource(target);
            var before = Modifications(root, source);
            RunOnCopy(first);
            var after = Modifications(root, source);
            var changed = after.Where(m =>
            {
                string old;
                return !before.TryGetValue(m.Key, out old) || old != m.Value;
            }).Select(m => m.Key).ToList();
            var so = new SerializedObject(target);
            foreach (var path in changed)
            {
                var property = so.FindProperty(path);
                if (property == null) continue;
                PrefabUtility.ApplyPropertyOverride(property, assetPath, InteractionMode.UserAction);
                so.Update();
            }
            foreach (var copy in copies)
            {
                if (copy == first || copy.fixTarget == null) continue;
                // Nothing reached the prefab: every copy is fixed on itself.
                if (changed.Count == 0 || OverridesAny(copy.fixTarget, changed)) RunOnCopy(copy);
            }
            Debug.Log("[SceneAudit] Fixed " + first.checkId + " in prefab " + assetPath + " (" + copies.Count + " copies, " + changed.Count + " properties).");
        }

        static bool OverridesAny(Object target, List<string> paths)
        {
            var so = new SerializedObject(target);
            foreach (var path in paths)
            {
                var property = so.FindProperty(path);
                if (property != null && property.prefabOverride) return true;
            }
            return false;
        }

        static void RunOnCopy(AuditIssue copy)
        {
            Undo.RecordObject(copy.fixTarget, UndoName);
            copy.fixAction();
            PrefabUtility.RecordPrefabInstancePropertyModifications(copy.fixTarget);
            var component = copy.fixTarget as Component;
            var go = component != null ? component.gameObject : copy.fixTarget as GameObject;
            if (go != null) EditorSceneManager.MarkSceneDirty(go.scene);
        }

        // Overridden properties of the source object on this copy: property path → value.
        static Dictionary<string, string> Modifications(GameObject root, Object source)
        {
            var map = new Dictionary<string, string>();
            if (root == null || source == null) return map;
            foreach (var m in PrefabUtility.GetPropertyModifications(root) ?? new PropertyModification[0])
                if (m.target == source)
                    map[m.propertyPath] = m.value + "|" + (m.objectReference != null ? m.objectReference.GetInstanceID().ToString() : "");
            return map;
        }
    }
}
