using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    // What the window does with several picked rows: fix them together or select them for the Inspector.
    public static class MultiSelect
    {
        // A problem row can be picked when it can be fixed or points to an object or asset.
        public static bool CanPick(AuditIssue issue)
        {
            return issue.AutoFixable || issue.target != null;
        }

        public static List<AuditIssue> Fixable(IEnumerable<AuditIssue> issues)
        {
            return issues.Where(i => i.AutoFixable).ToList();
        }

        // Scene objects as GameObjects (components resolved), assets as they are; settings rows are skipped.
        public static Object[] SelectionOf(IEnumerable<AuditIssue> issues)
        {
            var result = new List<Object>();
            foreach (var issue in issues)
            {
                if (issue.target == null) continue;
                var component = issue.target as Component;
                Object item = component != null ? component.gameObject : issue.target;
                if (!result.Contains(item)) result.Add(item);
            }
            return result.ToArray();
        }

        public static Object[] AssetsOf(IEnumerable<LightmapUvRow> rows)
        {
            return rows.Where(r => r.asset != null && EditorUtility.IsPersistent(r.asset)).Select(r => r.asset).Distinct().ToArray();
        }

        public static Object[] SceneObjectsOf(IEnumerable<LightmapUvRow> rows)
        {
            return rows.SelectMany(r => r.users).Where(g => g != null).Distinct().Cast<Object>().ToArray();
        }

        public static void Select(Object[] objects)
        {
            if (objects.Length == 0) return;
            Selection.objects = objects;
            EditorGUIUtility.PingObject(objects[0]);
        }
    }
}
