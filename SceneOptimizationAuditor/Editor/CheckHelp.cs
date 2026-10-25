using System.Collections.Generic;
using System.Linq;

namespace UnityTools.SceneOptimization
{
    // Help of the window: which checks have a card, its sections, and the pages of the guide in the repository.
    public static class CheckHelp
    {
        public const string GuideUrl = "https://github.com/AgitAngst/unity-tools/blob/main/docs/scene-optimization-auditor-guide.md";
        public const string ReferenceUrl = "https://github.com/AgitAngst/unity-tools/blob/main/docs/scene-optimization-auditor.md";
        // Anchor of the checks in the reference.
        public const string ChecksAnchor = "checks";

        // Sections of a card in their order; "gain" only for checks with a gain estimate.
        public static readonly string[] Sections = { "what", "why", "how", "gain", "keep" };

        // Sections of the guide about each tab, by tab index of the window.
        public static readonly string[] TabAnchors =
            { "tab-problems", "tab-viewpoints", "tab-lightmap", "tab-light", "tab-heavy", "tab-budgets", "tab-profiler", "tab-history" };

        public static bool Has(string checkId)
        {
            return Strings.Has(checkId + ".help.what");
        }

        // Checks that have a card.
        public static List<string> Ids
        {
            get
            {
                const string suffix = ".help.what";
                // "ui.help.what" is the heading of the section, not a check.
                return Strings.Keys.Where(k => k.EndsWith(suffix) && !k.StartsWith("ui.")).Select(k => k.Substring(0, k.Length - suffix.Length)).ToList();
            }
        }

        public static string Text(string checkId, string section)
        {
            var key = checkId + ".help." + section;
            return Strings.Has(key) ? Strings.Get(key) : "";
        }

        public static string GuideAt(string anchor)
        {
            return string.IsNullOrEmpty(anchor) ? GuideUrl : GuideUrl + "#" + anchor;
        }

        public static string TabUrl(int tab)
        {
            return GuideAt(tab >= 0 && tab < TabAnchors.Length ? TabAnchors[tab] : "");
        }
    }
}
