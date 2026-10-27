using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace UnityTools.SceneOptimization
{
    // Scripts of the project that set the frame rate themselves: in VR the headset runtime paces the frames.
    public static class FrameRateScripts
    {
        public const string TargetFrameRate = "Application.targetFrameRate";
        public const string VSyncCount = "QualitySettings.vSyncCount";

        static readonly Regex Comments = new Regex(@"//[^\n]*|/\*.*?\*/", RegexOptions.Singleline);
        static readonly Regex SetsTarget = new Regex(@"\bApplication\s*\.\s*targetFrameRate\s*=(?!=)");
        static readonly Regex SetsVSync = new Regex(@"\bQualitySettings\s*\.\s*vSyncCount\s*=(?!=)");

        // What each script assigns, by path and write time: a run reads only the scripts changed since.
        static readonly Dictionary<string, KeyValuePair<DateTime, string[]>> cache = new Dictionary<string, KeyValuePair<DateTime, string[]>>();

        // What the source assigns: TargetFrameRate, VSyncCount or nothing; comments do not count.
        public static string[] Assigned(string source)
        {
            var code = Comments.Replace(source ?? "", " ");
            var found = new List<string>();
            if (SetsTarget.IsMatch(code)) found.Add(TargetFrameRate);
            if (SetsVSync.IsMatch(code)) found.Add(VSyncCount);
            return found.ToArray();
        }

        // The files that assign either, with what they assign.
        public static List<KeyValuePair<string, string[]>> Find(IEnumerable<string> files)
        {
            var result = new List<KeyValuePair<string, string[]>>();
            foreach (var file in files)
            {
                if (!File.Exists(file)) continue;
                var time = File.GetLastWriteTimeUtc(file);
                KeyValuePair<DateTime, string[]> known;
                string[] assigned;
                if (cache.TryGetValue(file, out known) && known.Key == time) assigned = known.Value;
                else
                {
                    try
                    {
                        assigned = Assigned(File.ReadAllText(file));
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    cache[file] = new KeyValuePair<DateTime, string[]>(time, assigned);
                }
                if (assigned.Length > 0) result.Add(new KeyValuePair<string, string[]>(file, assigned));
            }
            return result;
        }

        // C# scripts of the project in Assets/.
        public static IEnumerable<string> ProjectScripts()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) yield return path;
            }
        }
    }
}
