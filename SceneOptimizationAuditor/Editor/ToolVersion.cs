namespace UnityTools.SceneOptimization
{
    // Version of the utility: X.Y.Z from the plan, Build = commit count of the repository.
    // Updated by scripts\stamp-version.ps1 before each commit.
    public static class ToolVersion
    {
        public const string Version = "1.1.0";
        public const int Build = 15;

        public static string Full { get { return Version + "." + Build; } }
    }
}
