using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityTools.SceneOptimization;
using Object = UnityEngine.Object;

namespace AuditTestsEditor
{
    // Scope of a run: the selection, the build scenes one by one, a folder of assets and several scenes on the command line.
    public static partial class AuditTestViewpoints
    {
        public const string ScopeFolder = AuditTestAssets.Root + "/ScopeFolder";

        public static void RunScope()
        {
            Run("scope", Scope);
        }

        static void Scope()
        {
            ScopeSelection();
            var saved = EditorBuildSettings.scenes;
            try
            {
                ScopeBuild();
                ScopeCli();
            }
            finally
            {
                EditorBuildSettings.scenes = saved;
            }
            ScopeFolderCheck();
        }

        static Animator Animated(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var animator = go.AddComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return animator;
        }

        static GameObject Textured(string name, Material material, Transform parent = null)
        {
            var go = MeshObject(name, Resources.GetBuiltinResource<Mesh>("Cube.fbx"), Vector3.zero, material);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        static bool OnObject(AuditIssue issue, string name)
        {
            var component = issue.target as Component;
            var go = component != null ? component.gameObject : issue.target as GameObject;
            return go != null && go.name == name;
        }

        // ---- selection ----

        // A building (an animator and a roof with a texture without mips) and other objects. Also used by the screenshot.
        public static GameObject BuildSelectionScene()
        {
            NewScene("Scope-Selection");
            var building = new GameObject("Building");
            Animated("Hall", building.transform);
            Textured("Roof", AuditTestAssets.NoMips, building.transform);
            Animated("Other");
            Textured("Other_Statue", AuditTestAssets.Big4096);
            return building;
        }

        static void ScopeSelection()
        {
            var building = BuildSelectionScene();

            var r = AuditScope.RunSelection(new[] { building }, new VRBudgets(), false);
            var noMips = AssetDatabase.GetAssetPath(AuditTestAssets.NoMips.mainTexture);
            var big = AssetDatabase.GetAssetPath(AuditTestAssets.Big4096.mainTexture);
            Check(r.scope == ScopeKind.Selection && r.selected == 1 && r.selectedWithChildren == 3, "selection: 1 object, 3 with children");
            Check(r.issues.Any(i => i.checkId == "ANIM_ALWAYS_ANIMATE" && OnObject(i, "Hall")) && !r.issues.Any(i => OnObject(i, "Other")),
                "the hall of the building is in the list, the other animator is not");
            Check(r.issues.Any(i => i.checkId == "TEX_NO_MIPMAPS" && i.assetPath == noMips) && r.issues.All(i => i.assetPath != big),
                "the texture of the roof is in the list, the texture of the other statue is not");
            Check(r.issues.All(i => !ReferenceEquals(i.target, null)), "no project settings or whole-scene rows");
            Check(r.allIssues.Any(i => OnObject(i, "Other")) && r.allIssues.Count > r.issues.Count, "the whole run stays for copies of prefabs");

            // After a fix the same objects are checked again, whatever is selected now.
            Selection.activeGameObject = GameObject.Find("Other");
            var again = AuditScope.Rerun(r, new VRBudgets());
            Check(again != null && again.scope == ScopeKind.Selection && again.issues.Any(i => OnObject(i, "Hall")) && !again.issues.Any(i => OnObject(i, "Other")),
                "the check again keeps the selection of the run");
            Selection.activeGameObject = null;
        }

        // ---- build scenes ----

        // Three saved scenes; A uses the texture without mips and has two copies of a guard prefab, B both textures and two
        // duplicate materials, C the large texture. Each has an animator that always animates and no camera.
        public static List<string> BuildScopeScenes()
        {
            Directory.CreateDirectory(PrefabDir);
            var template = new GameObject("Guard");
            template.AddComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var guard = PrefabUtility.SaveAsPrefabAsset(template, PrefabDir + "/ScopeGuard.prefab");
            Object.DestroyImmediate(template);

            var a = NewScene("Scope-A");
            Animated("Guard_A");
            Textured("Rock_A", AuditTestAssets.NoMips);
            for (int i = 0; i < 2; i++) ((GameObject)PrefabUtility.InstantiatePrefab(guard)).name = "Copy_" + i;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var b = NewScene("Scope-B");
            Animated("Guard_B");
            Textured("Rock_B", AuditTestAssets.NoMips);
            Textured("Statue_B", AuditTestAssets.Big4096);
            Textured("Cup_A", AuditTestAssets.MatDupA);
            Textured("Cup_B", AuditTestAssets.MatDupB);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var c = NewScene("Scope-C");
            Animated("Guard_C");
            Textured("Statue_C", AuditTestAssets.Big4096);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var paths = new List<string> { a, b, c };
            EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            return paths;
        }

        static void ScopeBuild()
        {
            var paths = BuildScopeScenes();
            EditorSceneManager.OpenScene(paths[0], OpenSceneMode.Single);
            Check(AuditScope.BuildScenePaths().SequenceEqual(paths), "three build scenes");

            var r = AuditScope.RunScenes(AuditScope.BuildScenePaths(), new VRBudgets(), true, ScopeKind.BuildScenes, null);
            Check(r != null && SceneManager.sceneCount == 1 && SceneManager.GetSceneAt(0).path == paths[0], "after the check the open scene is A again");
            if (r == null) return;
            Check(r.scope == ScopeKind.BuildScenes && r.parts.Count == 3 && r.parts.Count(p => p.open) == 1 && r.parts.Single(p => p.open).scenePaths[0] == paths[0],
                "three runs, A checked last as the open scene");
            Check(r.scenes.Select(s => s.path).OrderBy(p => p).SequenceEqual(paths.OrderBy(p => p)) && r.scenes.All(s => s.tris >= 0 && s.warning > 0),
                "a row for every scene with its budgets");

            var guards = r.issues.Where(i => i.checkId == "ANIM_ALWAYS_ANIMATE" && i.objectPath.StartsWith("Guard_")).ToList();
            var guardA = guards.FirstOrDefault(i => i.scenePath == paths[0]);
            var guardB = guards.FirstOrDefault(i => i.scenePath == paths[1]);
            Check(guards.Count == 3 && guardA != null && guardA.AutoFixable && guardB != null && !guardB.AutoFixable && guardB.FixTargetGone,
                "an animator per scene: A can be fixed, B is closed");

            var big = AssetDatabase.GetAssetPath(AuditTestAssets.Big4096.mainTexture);
            var noMips = AssetDatabase.GetAssetPath(AuditTestAssets.NoMips.mainTexture);
            var size = r.issues.Where(i => i.checkId == "TEX_SIZE" && i.assetPath == big).ToList();
            Check(size.Count == 1 && size[0].inScenes == 2 && size[0].users == 2, "the large texture of B and C: one row, in 2 scenes, 2 users");
            var mips = r.issues.Where(i => i.checkId == "TEX_NO_MIPMAPS" && i.assetPath == noMips).ToList();
            Check(mips.Count == 1 && mips[0].inScenes == 2 && mips[0].AutoFixable, "the texture without mips of A and B: one row that can be fixed");
            var settings = r.issues.Where(i => i.checkId == "VR_NOT_ENABLED").ToList();
            Check(settings.Count == 1, "a project setting once, not per scene (" + settings.Count + ")");
            var noCamera = r.issues.Where(i => i.checkId == "VR_NO_CAMERA").ToList();
            Check(noCamera.Count == 3 && noCamera.Select(i => i.scenePath).Distinct().Count() == 3 && noCamera.All(i => paths.Contains(i.scenePath) && i.Location == ""),
                "a whole-scene problem per scene, with its scene path (the location stays as in a run of the open scenes)");
            var duplicates = r.issues.Where(i => i.checkId == "MAT_DUPLICATE").ToList();
            Check(duplicates.Count > 0 && duplicates.All(i => i.scenePath == paths[1] && !i.AutoFixable),
                "duplicate materials of the closed B: the row belongs to B, its fix of B's renderers is gone");

            // A fix of the shared texture: the open scene is checked again, the closed scene's copy of the row goes too.
            int applied = AutoFixer.Apply(mips);
            var refreshed = AuditScope.Refresh(r, new VRBudgets());
            Check(applied == 1 && refreshed.issues.All(i => !(i.checkId == "TEX_NO_MIPMAPS" && i.assetPath == noMips)),
                "after the fix no row of the texture is left, in the open or the closed scene");
            Check(refreshed.issues.Any(i => i.checkId == "ANIM_ALWAYS_ANIMATE" && i.scenePath == paths[1]) && refreshed.parts.Count == 3,
                "rows of the closed scenes stay");
            Check(refreshed.scenes.Select(s => s.path).SequenceEqual(paths), "the table of scenes in the order of the build");

            // Two copies of a prefab in A, then B is opened: A's rows are gone objects, its copies row must still draw.
            System.Func<AuditResult, List<AuditIssue>> anim = x => x.issues.Where(i => i.checkId == "ANIM_ALWAYS_ANIMATE").ToList();
            var before = PrefabFixes.Rows(anim(refreshed), CopyTotals.Of(refreshed.issues));
            Check(before.Any(row => row.copies != null && row.copies.Copies == 2), "two copies of the prefab in A: one copies row");
            EditorSceneManager.OpenScene(paths[1], OpenSceneMode.Single);
            var opened = AuditScope.Refresh(refreshed, new VRBudgets());
            bool drawn = true;
            try
            {
                foreach (var row in PrefabFixes.Rows(anim(opened), CopyTotals.Of(opened.issues)))
                    if (row.copies != null && (row.copies.Copies < 0 || row.copies.CanFixInPrefab)) drawn = false;
            }
            catch (System.Exception e)
            {
                drawn = false;
                log.AppendLine("copies rows failed: " + e.Message);
            }
            Check(drawn, "after opening B the rows of A (closed) draw without errors and fix nothing in the prefab");
            Check(anim(opened).Any(i => i.scenePath == paths[1] && i.AutoFixable) && anim(opened).All(i => i.scenePath != paths[0] || !i.AutoFixable),
                "after opening B its rows can be fixed, those of A cannot");
            EditorSceneManager.OpenScene(paths[0], OpenSceneMode.Single);
        }

        // ---- command line ----

        static ReportExporter.ReportDto ReportOf(string path)
        {
            return JsonUtility.FromJson<ReportExporter.ReportDto>(File.ReadAllText(path));
        }

        static void ScopeCli()
        {
            var paths = BuildScopeScenes();
            Directory.CreateDirectory(ReportDir);
            var two = Path.Combine(ReportDir, "scope-two.json");
            int code = ReportExporter.RunWithArgs(new[] { "-auditScene", paths[0] + "," + paths[1], "-auditReport", two, "-auditHistory" });
            var report = code == 0 ? ReportOf(two) : null;
            Check(report != null && report.scope == "scenes" && report.sceneDetails.Length == 2 && report.scenes.Length == 2 &&
                  report.issues.Any(i => i.scenePath == paths[1] && i.checkId == "ANIM_ALWAYS_ANIMATE"),
                "two scenes: scope, a detail per scene, rows name their scene");
            Check(File.Exists(AuditHistory.FileFor(new[] { paths[0] })) && File.Exists(AuditHistory.FileFor(new[] { paths[1] })),
                "-auditHistory: a point in the history of each scene");

            var csv = Path.Combine(ReportDir, "scope-two.csv");
            code = ReportExporter.RunWithArgs(new[] { "-auditScene", paths[0], "-auditScene", paths[2], "-auditReport", csv });
            var lines = code == 0 ? File.ReadAllLines(csv) : new string[0];
            Check(lines.Length > 1 && lines[0].EndsWith(",ScenePath") && lines.Any(l => l.EndsWith(paths[2])), "CSV: the repeated flag, a ScenePath column");

            var build = Path.Combine(ReportDir, "scope-build.json");
            code = ReportExporter.RunWithArgs(new[] { "-auditBuildScenes", "-auditReport", build });
            report = code == 0 ? ReportOf(build) : null;
            Check(report != null && report.scope == "buildScenes" && report.sceneDetails.Length == 3, "-auditBuildScenes: three scenes");

            var one = Path.Combine(ReportDir, "scope-one.json");
            code = ReportExporter.RunWithArgs(new[] { "-auditScene", paths[2], "-auditReport", one });
            report = code == 0 ? ReportOf(one) : null;
            Check(report != null && report.scope == "loaded" && report.sceneDetails.Length == 0, "one scene stays a plain report");
        }

        // ---- folder ----

        static void WriteWav(string path, int seconds)
        {
            const int rate = 8000;
            int samples = rate * seconds;
            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + samples);
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(rate);
                w.Write(rate);
                w.Write((short)1);
                w.Write((short)8);
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(samples);
                for (int i = 0; i < samples; i++) w.Write((byte)(128 + (int)(40 * Math.Sin(i * 0.05))));
            }
        }

        // A prefab, a model, a loose material, texture and clip; the prefab also uses a texture from outside the folder.
        public static void BuildScopeFolder()
        {
            if (AssetDatabase.IsValidFolder(ScopeFolder)) AssetDatabase.DeleteAsset(ScopeFolder);
            AssetDatabase.CreateFolder(AuditTestAssets.Root, "ScopeFolder");

            var crate = new GameObject("Crate");
            Animated("Lid", crate.transform);
            Textured("Body", AuditTestAssets.NoMips, crate.transform);
            PrefabUtility.SaveAsPrefabAsset(crate, ScopeFolder + "/Crate.prefab");
            Object.DestroyImmediate(crate);

            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(AuditTestAssets.ObjReadWrite), ScopeFolder + "/Barrel.obj");

            var loose = new Material(Shader.Find("Standard"));
            loose.EnableKeyword("_NORMALMAP");
            AssetDatabase.CreateAsset(loose, ScopeFolder + "/Loose.mat");

            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            File.WriteAllBytes(ScopeFolder + "/Loose_RW.png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(ScopeFolder + "/Loose_RW.png", ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ScopeFolder + "/Loose_RW.png");
            importer.isReadable = true;
            importer.SaveAndReimport();

            WriteWav(ScopeFolder + "/Long.wav", 12);
            AssetDatabase.ImportAsset(ScopeFolder + "/Long.wav", ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
        }

        static void ScopeFolderCheck()
        {
            NewScene("Scope-Folder");
            BuildScopeFolder();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            int roots = EditorSceneManager.GetActiveScene().rootCount;
            var r = AuditScope.RunFolder(ScopeFolder, new VRBudgets(), false);
            Check(!EditorSceneManager.GetActiveScene().isDirty, "the open scene stays unmodified");
            Check(r != null && r.scope == ScopeKind.Folder && r.folder == ScopeFolder, "a folder run");
            if (r == null) return;
            log.AppendLine("folder issues: " + string.Join(", ", r.issues.Select(i => i.checkId + " " + i.Location).ToArray()));
            var c = r.folderCounts;
            Check(c.prefabs == 1 && c.models == 1 && c.materials == 1 && c.textures == 1 && c.clips == 1,
                "1 prefab, 1 model, 1 material, 1 texture, 1 clip (" + c.prefabs + "/" + c.models + "/" + c.materials + "/" + c.textures + "/" + c.clips + ")");
            Check(r.issues.Count > 0 && r.issues.All(i => i.assetPath.StartsWith(ScopeFolder + "/")), "every row is an asset of the folder");
            var noMips = AssetDatabase.GetAssetPath(AuditTestAssets.NoMips.mainTexture);
            Check(r.issues.All(i => i.assetPath != noMips), "the texture outside the folder is not listed");
            var lid = r.issues.FirstOrDefault(i => i.checkId == "ANIM_ALWAYS_ANIMATE");
            Check(lid != null && lid.Location == "Crate.prefab › Lid" && EditorUtility.IsPersistent(lid.target) && !lid.AutoFixable,
                "the animator of the prefab: \"Crate.prefab › Lid\", the object in the prefab, no fix");
            Check(r.issues.Any(i => i.checkId == "MESH_READ_WRITE" && i.assetPath == ScopeFolder + "/Barrel.obj" && i.AutoFixable), "the model with Read/Write, fixable");
            Check(r.issues.Any(i => i.checkId == "MAT_KEYWORD_NO_TEXTURE" && i.assetPath == ScopeFolder + "/Loose.mat"), "the loose material on a quad");
            Check(r.issues.Any(i => i.checkId == "AUDIO_DECOMPRESS_LONG" && i.assetPath == ScopeFolder + "/Long.wav"), "the loose clip");
            var readable = r.issues.FirstOrDefault(i => i.checkId == "TEX_READ_WRITE" && i.assetPath == ScopeFolder + "/Loose_RW.png");
            Check(readable != null && readable.AutoFixable, "the loose texture with Read/Write, fixable");
            if (readable != null)
            {
                AutoFixer.Apply(new[] { readable });
                Check(!((TextureImporter)AssetImporter.GetAtPath(ScopeFolder + "/Loose_RW.png")).isReadable, "the fix of the loose texture is applied");
            }
            Check(EditorSceneManager.GetActiveScene().rootCount == roots && GameObject.Find("Crate") == null && GameObject.Find("Loose") == null,
                "nothing of the hidden scene is left in the open scene");

            var json = Path.Combine(ReportDir, "scope-folder.json");
            int code = ReportExporter.RunWithArgs(new[] { "-auditFolder", ScopeFolder, "-auditReport", json });
            var report = code == 0 ? ReportOf(json) : null;
            Check(report != null && report.scope == "folder" && report.folder == ScopeFolder && report.issues.Length > 0, "-auditFolder: a folder report");
        }
    }
}
