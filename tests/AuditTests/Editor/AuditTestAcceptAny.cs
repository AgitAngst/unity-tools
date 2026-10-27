using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Any row can be marked acceptable (2.5.0): a row of a whole scene is kept for its scene, project settings for the project.
    public static partial class AuditTestViewpoints
    {
        public static void RunAcceptAny()
        {
            Run("accept-any", AcceptAny);
        }

        static void AcceptAny()
        {
            if (File.Exists(AcceptedList.FilePath)) File.Delete(AcceptedList.FilePath);
            var a = NewScene("Accept-A");
            var b = NewScene("Accept-B");
            var paths = new[] { a, b };
            var r = AuditScope.RunScenes(paths, new VRBudgets(), false, ScopeKind.Scenes, null);
            var rowA = r.issues.FirstOrDefault(i => i.checkId == "VR_NO_CAMERA" && i.scenePath == a);
            var rowB = r.issues.FirstOrDefault(i => i.checkId == "VR_NO_CAMERA" && i.scenePath == b);
            var setting = r.issues.FirstOrDefault(i => i.settingsPath.StartsWith("Project/"));
            Check(rowA != null && rowB != null && rowA.target == null && !rowA.AutoFixable, "an empty scene: a row of the whole scene without a fix, in each scene");
            if (rowA == null || rowB == null) return;
            Check(AcceptedList.KeyOf(rowA) == AcceptedList.SceneKey + AssetDatabase.AssetPathToGUID(a), "its key is the guid of its scene");
            Check(setting != null && AcceptedList.KeyOf(setting) == "*", "a project setting is kept for the project (" + (setting != null ? setting.checkId : "none") + ")");

            AcceptedList.Load().Accept(new[] { rowA });
            var again = AuditScope.RunScenes(paths, new VRBudgets(), false, ScopeKind.Scenes, null);
            var againA = again.issues.First(i => i.checkId == "VR_NO_CAMERA" && i.scenePath == a);
            var againB = again.issues.First(i => i.checkId == "VR_NO_CAMERA" && i.scenePath == b);
            Check(againA.accepted && !againB.accepted, "marked in scene A, scene B still counts");
            var list = AcceptedList.Load();
            Check(list.entries.Count == 1 && list.entries[0].location == a, "the file names the scene");

            // A mark "*" made before 2.5.0 holds for every scene; releasing a scene row removes it.
            list.Release(new[] { againA });
            list.entries.Add(new AcceptedList.Entry { checkId = "VR_NO_CAMERA", key = "*" });
            list.Save();
            var old = AuditScope.RunScenes(paths, new VRBudgets(), false, ScopeKind.Scenes, null);
            Check(old.issues.Where(i => i.checkId == "VR_NO_CAMERA").All(i => i.accepted), "an old mark * holds for both scenes");
            var reload = AcceptedList.Load();
            reload.Release(new[] { old.issues.First(i => i.checkId == "VR_NO_CAMERA" && i.scenePath == b) });
            Check(reload.entries.Count == 0 && !File.Exists(AcceptedList.FilePath), "releasing a scene row removes the old mark");

            // The usual run of the open scenes: one open scene keeps its scene, two open scenes together are the project.
            EditorSceneManager.OpenScene(a, OpenSceneMode.Single);
            var open = SceneAuditor.RunFullAudit(new VRBudgets(), false).issues.First(i => i.checkId == "VR_NO_CAMERA");
            Check(AcceptedList.KeyOf(open) == AcceptedList.KeyOf(rowA), "one open scene: the same key as scene A checked alone");
            EditorSceneManager.OpenScene(b, OpenSceneMode.Additive);
            var both = SceneAuditor.RunFullAudit(new VRBudgets(), false).issues.First(i => i.checkId == "VR_NO_CAMERA");
            Check(AcceptedList.KeyOf(both) == "*", "two open scenes together: for the project");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var unsaved = SceneAuditor.RunFullAudit(new VRBudgets(), false).issues.First(i => i.checkId == "VR_NO_CAMERA");
            Check(AcceptedList.KeyOf(unsaved) == "*", "a scene never saved: for the project");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
