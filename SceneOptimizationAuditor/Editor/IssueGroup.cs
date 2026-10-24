using System.Collections.Generic;
using System.Linq;

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
}
