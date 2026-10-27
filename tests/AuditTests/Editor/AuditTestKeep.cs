using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // The run kept across a window opened again and a script reload (2.7.0): the same list at once, the same scope checked
    // again with the open groups kept and without a history point, build scenes waiting for the user.
    public static partial class AuditTestViewpoints
    {
        public static void RunKeepResult()
        {
            Run("keep-result", KeepResult);
        }

        static void SetWindowField(AuditorWindow window, string name, object value)
        {
            var f = typeof(AuditorWindow).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (f == null) throw new System.MissingFieldException("AuditorWindow." + name);
            f.SetValue(window, value);
        }

        static void KeepResult()
        {
            var path = NewScene("KeepResult");
            MeshObject("Heavy", AuditTestAssets.Grid30k, Vector3.zero, AuditTestAssets.Plain);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            SetWindowField(null, "kept", null);
            var historyFile = AuditHistory.FileFor(new[] { path });
            if (File.Exists(historyFile)) File.Delete(historyFile);

            var first = ScriptableObject.CreateInstance<AuditorWindow>();
            AuditorWindow again = null, reloaded = null, scenes = null;
            try
            {
                Check(FieldOf(first, "result") == null && !(bool)FieldOf(first, "hadRun"), "a new window without a run shows nothing kept");
                var r = SceneAuditor.RunFullAudit(VRBudgets.Load(), false);
                CallWindow(first, "Show", r, false);
                var keys = ((System.Collections.IList)FieldOf(first, "groups")).Cast<IssueGroup>().Take(2).Select(g => g.key).ToList();
                var expanded = (Dictionary<string, bool>)FieldOf(first, "expanded");
                expanded.Clear();
                foreach (var k in keys) expanded[k] = true;
                CallWindow(first, "KeepOpenGroups");
                var kept = (List<string>)FieldOf(first, "keptOpen");
                Check(kept.Count == 2 && keys.All(kept.Contains), "the open groups are kept with the window when it goes (OnDisable)");
                Object.DestroyImmediate(first);
                first = null;

                again = ScriptableObject.CreateInstance<AuditorWindow>();
                Check(ReferenceEquals(FieldOf(again, "result"), r), "a window opened again shows the same run at once");
                Check(((System.Collections.IList)FieldOf(again, "groups")).Count > 0, "with its groups");
                int points = AuditHistory.Load(historyFile).points.Count;
                Check(points == 1, "opening the window again writes no history point (" + points + ")");

                // A script reload: statics are gone, the serialized fields of the window stay.
                SetWindowField(null, "kept", null);
                reloaded = ScriptableObject.CreateInstance<AuditorWindow>();
                SetWindowField(reloaded, "hadRun", true);
                SetWindowField(reloaded, "keptScope", ScopeKind.Loaded);
                SetWindowField(reloaded, "keptTime", "12:00");
                SetWindowField(reloaded, "keptOpen", new List<string>(keys));
                CallWindow(reloaded, "RestoreKept");
                Check((bool)FieldOf(reloaded, "rerunDue") && FieldOf(reloaded, "result") == null, "after a script reload the open scenes wait to be checked again");
                Check(((string)CallWindow(reloaded, "BarStatus")).Contains("12:00"), "while it waits the run bar names the kept run");
                CallWindow(reloaded, "RunKept");
                var after = (AuditResult)FieldOf(reloaded, "result");
                Check(after != null && after.afterReload && after.issues.Count == r.issues.Count, "the same scope is checked again by itself");
                var open = (Dictionary<string, bool>)FieldOf(reloaded, "expanded");
                Check(open.Count(e => e.Value) == 2 && keys.All(k => open.ContainsKey(k) && open[k]), "both open groups stay open");
                Check(AuditHistory.Load(historyFile).points.Count == points, "the check after a reload writes no history point");
                var status = (string)CallWindow(reloaded, "BarStatus");
                Check(status.Contains(Strings.Get("ui.bar.afterReload")), "the run bar says the run came after the reload: " + status);

                SetWindowField(null, "kept", null);
                scenes = ScriptableObject.CreateInstance<AuditorWindow>();
                SetWindowField(scenes, "hadRun", true);
                SetWindowField(scenes, "keptScope", ScopeKind.BuildScenes);
                SetWindowField(scenes, "keptScenes", 6);
                SetWindowField(scenes, "keptTime", "12:00");
                CallWindow(scenes, "RestoreKept");
                Check(!(bool)FieldOf(scenes, "rerunDue") && (bool)FieldOf(scenes, "dropped"), "build scenes are not checked again by themselves");
                Check(((string)CallWindow(scenes, "BarStatus")).Contains("12:00"), "the run bar names the dropped run");
            }
            finally
            {
                foreach (var w in new[] { first, again, reloaded, scenes }) if (w != null) Object.DestroyImmediate(w);
                SetWindowField(null, "kept", null);
            }
        }
    }
}
