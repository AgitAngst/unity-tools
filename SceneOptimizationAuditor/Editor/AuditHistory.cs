using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    [Serializable]
    public sealed class CheckCount
    {
        public string checkId;
        public int count;
    }

    // One audit run of a scene set, kept to show progress over weeks.
    [Serializable]
    public sealed class HistoryPoint
    {
        public string time = "";
        public string toolVersion = "";
        // What was done before this run, written by the user.
        public string note = "";
        public int critical;
        public int warning;
        public int info;
        public int accepted;
        public long sceneTris;
        public int trisLimit;
        public long drawCalls;
        public int drawCallLimit;
        public double textureMemoryMB;
        public int textureMemoryLimitMB;
        // Profiler snapshot taken since the previous point.
        public bool hasProfiler;
        public string profilerTime = "";
        public float avgFPS;
        public float avgCPUms;
        public float avgGPUms = -1f;
        // Viewpoint estimate made since the previous point: the worst viewpoint and how many are over budget.
        public bool hasViewpoints;
        public string vpTime = "";
        public int vpCount;
        public int vpOver;
        public long vpTris;
        public int vpDrawCalls;
        public float vpTransparent;
        public List<CheckCount> checks = new List<CheckCount>();
        // "C|checkId|location" (C, W, I by severity, A for acceptable); dropped on old points to keep the file small.
        public List<string> problems = new List<string>();
        public bool problemsKept = true;

        public DateTime Time
        {
            get
            {
                DateTime t;
                return DateTime.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out t) ? t : DateTime.MinValue;
            }
        }

        public int CountOf(string checkId)
        {
            var c = checks.FirstOrDefault(x => x.checkId == checkId);
            return c != null ? c.count : 0;
        }
    }

    // Problems gone and new between two points, and the checks whose count changed.
    public sealed class HistoryDiff
    {
        public HistoryPoint a;
        public HistoryPoint b;
        public readonly List<KeyValuePair<string, int>> checkChanges = new List<KeyValuePair<string, int>>();
        // "checkId|location"
        public readonly List<string> fixedProblems = new List<string>();
        public readonly List<string> newProblems = new List<string>();
        public int acceptedSince;
        public bool problemsKnown;
    }

    // History of one scene set in SceneAuditHistory/ next to Assets/: outside the import, can be committed.
    [Serializable]
    public sealed class AuditHistory
    {
        public const int MaxPoints = 200;
        // Older points keep counts only.
        public const int PointsWithProblems = 50;
        public const string FolderName = "SceneAuditHistory";

        public string[] scenes = new string[0];
        public List<HistoryPoint> points = new List<HistoryPoint>();

        public static string Folder
        {
            get { return Path.Combine(Path.GetDirectoryName(Application.dataPath), FolderName); }
        }

        public static string FileFor(string[] scenePaths)
        {
            return Path.Combine(Folder, FileNameFor(scenePaths));
        }

        // "<first scene>[+N]-<hash of the set>.json": one name per scene set, order does not matter. Viewpoints use it too.
        public static string FileNameFor(string[] scenePaths)
        {
            var sorted = scenePaths.Select(p => string.IsNullOrEmpty(p) ? "untitled" : p.Replace('\\', '/')).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            var first = sorted.Length > 0 ? Path.GetFileNameWithoutExtension(sorted[0]) : "untitled";
            foreach (var ch in Path.GetInvalidFileNameChars()) first = first.Replace(ch, '_');
            if (sorted.Length > 1) first += "+" + (sorted.Length - 1);
            return first + "-" + Hash(string.Join("|", sorted)) + ".json";
        }

        public static string[] Files()
        {
            if (!Directory.Exists(Folder)) return new string[0];
            return Directory.GetFiles(Folder, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static AuditHistory Load(string path)
        {
            var history = new AuditHistory();
            if (!File.Exists(path)) return history;
            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), history);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneAudit] Could not read the audit history " + path + ": " + e.Message);
            }
            return history;
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(this, true));
        }

        // Adds the run to the history of its scenes and returns the point. Runs of one day replace the last point
        // of that day, unless it has a note.
        public static HistoryPoint Record(AuditResult result, string note, DateTime now)
        {
            var path = FileFor(result.scenePaths);
            var history = Load(path);
            history.scenes = result.scenePaths;
            var replaced = history.Replaceable(now);
            var before = history.points.Where(p => p != replaced).LastOrDefault();
            var point = PointOf(result, ProfilerCapture.LoadLast(result.activeScenePath), before, note, now);
            history.Add(point, now);
            history.Save(path);
            return point;
        }

        public static HistoryPoint Record(AuditResult result, string note)
        {
            return Record(result, note, DateTime.Now);
        }

        HistoryPoint Replaceable(DateTime now)
        {
            var last = points.LastOrDefault();
            if (last == null || !string.IsNullOrEmpty(last.note)) return null;
            return last.Time.Date == now.Date ? last : null;
        }

        public void Add(HistoryPoint point, DateTime now)
        {
            var replaced = Replaceable(now);
            if (replaced != null) points.Remove(replaced);
            points.Add(point);
            while (points.Count > MaxPoints) points.RemoveAt(0);
            for (int i = 0; i < points.Count - PointsWithProblems; i++)
            {
                if (!points[i].problemsKept) continue;
                points[i].problems.Clear();
                points[i].problemsKept = false;
            }
        }

        public static HistoryPoint PointOf(AuditResult result, ProfilerSnapshot snapshot, HistoryPoint previous, string note, DateTime now)
        {
            var point = new HistoryPoint
            {
                time = now.ToString("o", CultureInfo.InvariantCulture),
                toolVersion = ToolVersion.Full,
                note = note ?? "",
                critical = result.Count(Severity.Critical),
                warning = result.Count(Severity.Warning),
                info = result.Count(Severity.Info),
                accepted = result.AcceptedCount,
                sceneTris = result.sceneTris,
                trisLimit = result.budgets.sceneTrisWarning,
                drawCalls = result.drawCallEstimate,
                drawCallLimit = result.budgets.drawCallsWarning,
                textureMemoryMB = Math.Round(result.textureMemoryMB, 1),
                textureMemoryLimitMB = result.budgets.textureMemoryWarningMB
            };
            var counts = new Dictionary<string, int>();
            foreach (var issue in result.issues)
            {
                point.problems.Add(Letter(issue) + "|" + issue.checkId + "|" + issue.Location);
                if (issue.accepted) continue;
                int n;
                counts.TryGetValue(issue.checkId, out n);
                counts[issue.checkId] = n + 1;
            }
            foreach (var pair in counts.OrderBy(p => p.Key, StringComparer.Ordinal))
                point.checks.Add(new CheckCount { checkId = pair.Key, count = pair.Value });
            if (snapshot != null && (previous == null || previous.profilerTime != snapshot.timestamp))
            {
                point.hasProfiler = true;
                point.profilerTime = snapshot.timestamp;
                point.avgFPS = snapshot.avgFPS;
                point.avgCPUms = snapshot.avgCPUms;
                point.avgGPUms = snapshot.avgGPUms;
            }
            AddViewpoints(point, result, previous);
            return point;
        }

        static void AddViewpoints(HistoryPoint point, AuditResult result, HistoryPoint previous)
        {
            var set = ViewpointSet.Load(result.scenePaths);
            try
            {
                if (!set.HasEstimate || (previous != null && previous.vpTime == set.estimatedAt)) return;
                var estimated = set.points.Where(p => p.Estimated).ToList();
                var worst = estimated.OrderByDescending(p => p.Worst.drawCalls).ThenByDescending(p => p.Worst.tris).First();
                point.hasViewpoints = true;
                point.vpTime = set.estimatedAt;
                point.vpCount = estimated.Count;
                point.vpOver = estimated.Count(p => ViewpointEstimator.LevelOf(p.Worst, result.budgets) != null);
                point.vpTris = worst.Worst.tris;
                point.vpDrawCalls = worst.Worst.drawCalls;
                point.vpTransparent = worst.Worst.transparentPercent;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(set);
            }
        }

        static string Letter(AuditIssue issue)
        {
            if (issue.accepted) return "A";
            switch (issue.severity)
            {
                case Severity.Critical: return "C";
                case Severity.Warning: return "W";
                default: return "I";
            }
        }

        public static HistoryDiff Compare(HistoryPoint a, HistoryPoint b)
        {
            var diff = new HistoryDiff { a = a, b = b, problemsKnown = a.problemsKept && b.problemsKept };
            foreach (var id in a.checks.Select(c => c.checkId).Union(b.checks.Select(c => c.checkId)).OrderBy(x => x, StringComparer.Ordinal))
            {
                int delta = b.CountOf(id) - a.CountOf(id);
                if (delta != 0) diff.checkChanges.Add(new KeyValuePair<string, int>(id, delta));
            }
            if (!diff.problemsKnown) return diff;
            var aActive = Bag(a.problems, false);
            var bActive = Bag(b.problems, false);
            var bAccepted = Bag(b.problems, true);
            foreach (var pair in aActive)
            {
                int left = pair.Value - Get(bActive, pair.Key);
                int nowAccepted = Math.Min(left, Get(bAccepted, pair.Key));
                diff.acceptedSince += Math.Max(0, nowAccepted);
                for (int i = 0; i < left - Math.Max(0, nowAccepted); i++) diff.fixedProblems.Add(pair.Key);
            }
            foreach (var pair in bActive)
                for (int i = 0; i < pair.Value - Get(aActive, pair.Key); i++) diff.newProblems.Add(pair.Key);
            return diff;
        }

        // Counts of "checkId|location", active or acceptable ones.
        static Dictionary<string, int> Bag(List<string> problems, bool accepted)
        {
            var bag = new Dictionary<string, int>();
            foreach (var p in problems)
            {
                int bar = p.IndexOf('|');
                if (bar < 0 || (p.StartsWith("A|", StringComparison.Ordinal) != accepted)) continue;
                var key = p.Substring(bar + 1);
                bag[key] = Get(bag, key) + 1;
            }
            return bag;
        }

        static int Get(Dictionary<string, int> bag, string key)
        {
            int n;
            return bag.TryGetValue(key, out n) ? n : 0;
        }

        static string Hash(string text)
        {
            uint hash = 2166136261;
            foreach (var ch in text)
            {
                hash ^= ch;
                hash *= 16777619;
            }
            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }
}
