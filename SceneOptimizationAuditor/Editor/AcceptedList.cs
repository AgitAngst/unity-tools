using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    // Problems the team accepted on purpose (big houses, a planned transparent window) and checks turned off for
    // the whole project. Stored in ProjectSettings so it travels with the project in version control.
    [Serializable]
    public sealed class AcceptedList
    {
        [Serializable]
        public sealed class Entry
        {
            public string checkId;
            // GlobalObjectId of the object or asset; "scene:<guid>" for a problem of a whole scene (budgets, counts) when
            // one scene was checked; "*" for project settings and for scenes checked together (before 2.5.0 — every
            // problem without an object, for the whole project).
            public string key;
            // Where it was, for people reading the file.
            public string location;
            public string date;
        }

        public List<Entry> entries = new List<Entry>();
        public List<string> disabledChecks = new List<string>();

        public const string FileName = "SceneOptimizationAuditorAccepted.json";

        public static string FilePath
        {
            get { return Path.Combine(Path.Combine(Path.GetDirectoryName(Application.dataPath), "ProjectSettings"), FileName); }
        }

        public static AcceptedList Load()
        {
            var list = new AcceptedList();
            if (!File.Exists(FilePath)) return list;
            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), list);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneAudit] Could not read accepted problems from " + FilePath + ": " + e.Message);
            }
            return list;
        }

        public void Save()
        {
            if (entries.Count == 0 && disabledChecks.Count == 0)
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                return;
            }
            File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
        }

        public const string SceneKey = "scene:";

        // Scene objects and assets by GlobalObjectId: it survives renames and moves. Components count as their GameObject.
        // A problem without an object gets the key of its scene from the run (KeepScene), else "*".
        public static string KeyOf(AuditIssue issue)
        {
            if (!string.IsNullOrEmpty(issue.acceptKey)) return issue.acceptKey;
            if (issue.target == null) return "*";
            var component = issue.target as Component;
            Object owner = component != null ? (Object)component.gameObject : issue.target;
            issue.acceptKey = GlobalObjectId.GetGlobalObjectIdSlow(owner).ToString();
            return issue.acceptKey;
        }

        // Problems without an object that are about the project, not the scene.
        static readonly string[] ProjectChecks = { "VR_STEAMVR_MISSING" };

        // A run of one scene: its problems without an object (budgets, counts, lighting of the scene) belong to that scene by
        // its guid, which survives moving the scene. Project settings stay "*"; so does a scene never saved.
        public static void KeepScene(IEnumerable<AuditIssue> issues, string scenePath)
        {
            var guid = string.IsNullOrEmpty(scenePath) ? "" : AssetDatabase.AssetPathToGUID(scenePath);
            if (string.IsNullOrEmpty(guid)) return;
            foreach (var issue in issues)
                if (issue.target == null && string.IsNullOrEmpty(issue.acceptKey) && !issue.settingsPath.StartsWith("Project/", StringComparison.Ordinal) &&
                    Array.IndexOf(ProjectChecks, issue.checkId) < 0)
                {
                    issue.acceptKey = SceneKey + guid;
                    issue.acceptScene = scenePath;
                }
        }

        // A mark "*" of a problem without an object holds for every scene: marks made before 2.5.0 stay as they were.
        static bool Matches(Entry e, string checkId, string key)
        {
            return e.checkId == checkId && (e.key == key || (e.key == "*" && key.StartsWith(SceneKey, StringComparison.Ordinal)));
        }

        public bool IsAccepted(AuditIssue issue)
        {
            if (disabledChecks.Contains(issue.checkId)) return true;
            var key = KeyOf(issue);
            return entries.Any(e => Matches(e, issue.checkId, key));
        }

        public void Apply(IEnumerable<AuditIssue> issues)
        {
            foreach (var issue in issues) issue.accepted = IsAccepted(issue);
        }

        public void Accept(IEnumerable<AuditIssue> issues)
        {
            foreach (var issue in issues)
            {
                var key = KeyOf(issue);
                if (!entries.Any(e => e.checkId == issue.checkId && e.key == key))
                    entries.Add(new Entry { checkId = issue.checkId, key = key, location = issue.acceptScene.Length == 0 ? issue.Location : issue.Location.Length > 0 ? issue.acceptScene + ": " + issue.Location : issue.acceptScene,
                        date = DateTime.Now.ToString("yyyy-MM-dd") });
                issue.accepted = true;
            }
            Save();
            Debug.Log("[SceneAudit] Accepted problems saved: " + entries.Count + " in " + FilePath);
        }

        // Releases the per-object marks; a problem of a disabled check stays accepted until the check is enabled. Releasing
        // a scene row held by an old project-wide mark "*" removes that mark: the problem counts again in every scene.
        public void Release(IEnumerable<AuditIssue> issues)
        {
            foreach (var issue in issues)
            {
                var key = KeyOf(issue);
                entries.RemoveAll(e => Matches(e, issue.checkId, key));
                issue.accepted = disabledChecks.Contains(issue.checkId);
            }
            Save();
        }

        public void Disable(IEnumerable<string> checkIds)
        {
            foreach (var id in checkIds) if (!disabledChecks.Contains(id)) disabledChecks.Add(id);
            Save();
            Debug.Log("[SceneAudit] Checks turned off: " + string.Join(", ", disabledChecks.ToArray()));
        }

        public void Enable(string checkId)
        {
            disabledChecks.Remove(checkId);
            Save();
        }

        public void Clear()
        {
            entries.Clear();
            disabledChecks.Clear();
            Save();
        }
    }
}
