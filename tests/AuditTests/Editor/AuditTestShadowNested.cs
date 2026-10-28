using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // "Shadows" on nested objects (2.7.1, owner 28.10.2026: "turning off shadows and everything related does not work on
    // nested objects"): baked meshes deep in a group, in a prefab copy and in a prefab nested in it. With "Keep Cast Shadows
    // on baked" (on by default) and Contribute GI ticked one press turns everything off — before 2.7.1 the first press kept
    // Cast Shadows, the mesh was baked no more and stayed in the count. The settings stay off after the scene is saved and
    // opened again; one Undo brings them back.
    public static partial class AuditTestViewpoints
    {
        const string NestedPrefab = AuditTestAssets.Root + "/Prefabs/ShadowTree.prefab";
        const string InnerPrefab = AuditTestAssets.Root + "/Prefabs/ShadowRock.prefab";

        public static void RunShadowNested()
        {
            Run("shadow-nested", ShadowNested);
        }

        static GameObject Child(string name, Transform parent, Vector3 local, bool mesh)
        {
            var go = mesh ? MeshObject(name, Resources.GetBuiltinResource<Mesh>("Cube.fbx"), Vector3.zero, AuditTestAssets.Plain) : new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            if (mesh)
            {
                var r = go.GetComponent<MeshRenderer>();
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI);
            }
            return go;
        }

        static bool Gi(Renderer r)
        {
            return (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0;
        }

        static string State(Renderer r)
        {
            return r.name + ": " + r.shadowCastingMode + (r.receiveShadows ? " receives" : "") + (Gi(r) ? " GI" : "");
        }

        static void ShadowNested()
        {
            System.IO.Directory.CreateDirectory(AuditTestAssets.Root + "/Prefabs");
            var rockTemplate = new GameObject("Rock");
            Child("RockMesh", rockTemplate.transform, Vector3.zero, true);
            var rock = PrefabUtility.SaveAsPrefabAsset(rockTemplate, InnerPrefab);
            Object.DestroyImmediate(rockTemplate);

            var treeTemplate = new GameObject("Tree");
            var trunk = Child("Trunk", treeTemplate.transform, Vector3.zero, true);
            var crown = Child("Crown", trunk.transform, new Vector3(0f, 2f, 0f), false);
            Child("Leaves", crown.transform, Vector3.zero, true);
            var innerRock = (GameObject)PrefabUtility.InstantiatePrefab(rock);
            innerRock.transform.SetParent(treeTemplate.transform, false);
            innerRock.transform.localPosition = new Vector3(1f, 0f, 0f);
            var tree = PrefabUtility.SaveAsPrefabAsset(treeTemplate, NestedPrefab);
            Object.DestroyImmediate(treeTemplate);

            var path = NewScene("ShadowNested");
            var forest = new GameObject("Forest").transform;
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(tree);
            copy.transform.SetParent(forest, false);
            copy.transform.position = new Vector3(60f, 0f, 0f);
            var group = Child("Group", forest, new Vector3(0f, 0f, 70f), false);
            Child("Deep", Child("Level2", Child("Level1", group.transform, Vector3.zero, false).transform, Vector3.zero, false).transform, Vector3.zero, true);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            var rows = ShadowReport.Collect();
            var expected = new[] { "Deep", "Leaves", "RockMesh", "Trunk" };
            Check(rows.Where(r => expected.Contains(r.renderer.name)).All(r => r.Baked), "the nested meshes are baked (Contribute GI, lightmaps)");

            // The default boxes: Cast Shadows only, baked kept — baked meshes do not change.
            var castOnly = new ShadowZone { placed = true, center = Vector3.zero, radius = 30f };
            ShadowReport.Measure(rows, castOnly);
            Check(!rows.Any(r => ShadowReport.WillChange(r, castOnly)), "Cast Shadows alone keeps the shadow of baked meshes: nothing to change");

            var zone = new ShadowZone { placed = true, center = Vector3.zero, radius = 30f, offCast = true, offReceive = true, offGI = true, keepBaked = true };
            ShadowReport.Measure(rows, zone);
            var found = rows.Where(r => ShadowReport.Matches(r, "forest")).ToList();
            Check(Names(found).SequenceEqual(expected), "the search \"forest\" finds every nested mesh (" + string.Join(", ", Names(found)) + ")");
            var change = found.Where(r => ShadowReport.WillChange(r, zone)).ToList();
            Check(Names(change).SequenceEqual(expected), "all of them are far and will change (" + string.Join(", ", Names(change)) + ")");

            var fixes = ShadowReport.TurnOffFixes(change, zone);
            int applied = AutoFixer.Apply(fixes);
            var renderers = forest.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers) log.AppendLine("  after the fix: " + State(r));
            Check(applied == 4 && renderers.All(r => r.shadowCastingMode == ShadowCastingMode.Off && !r.receiveShadows && !Gi(r)),
                "one press turns every nested mesh off — Cast, Receive, GI, baked ones too (" + applied + " fixes)");
            var again = ShadowReport.Collect();
            ShadowReport.Measure(again, zone);
            Check(!again.Any(r => ShadowReport.WillChange(r, zone)), "nothing is left to change after one press");

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var reopened = GameObject.Find("Forest").GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in reopened) log.AppendLine("  after the scene is opened again: " + State(r));
            Check(reopened.Length == 4 && reopened.All(r => r.shadowCastingMode == ShadowCastingMode.Off && !r.receiveShadows && !Gi(r)),
                "the settings stay off after the scene is saved and opened again, prefab copies too");

            // The same through one row of a nested prefab mesh, then one Undo.
            var leaves = reopened.First(r => r.name == "Leaves");
            leaves.shadowCastingMode = ShadowCastingMode.On;
            leaves.receiveShadows = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(leaves);
            var rows2 = ShadowReport.Collect();
            ShadowReport.Measure(rows2, zone);
            var leavesRow = rows2.First(r => r.renderer == leaves);
            AutoFixer.Apply(ShadowReport.TurnOffFixes(new[] { leavesRow }, zone));
            Check(leaves.shadowCastingMode == ShadowCastingMode.Off && !leaves.receiveShadows, "the row of a nested prefab mesh turns it off");
            Undo.PerformUndo();
            Check(leaves.shadowCastingMode == ShadowCastingMode.On && leaves.receiveShadows, "one Undo brings it back");
            DeleteViewpointFile(path);
        }
    }
}
