using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Copies together, the fix in a prefab and the Heaviest tab on prefabs and models made by the test.
    public static partial class AuditTestViewpoints
    {
        const string PrefabDir = AuditTestAssets.Root + "/Prefabs";
        const string VillagerPrefab = PrefabDir + "/Villager.prefab";
        const string HousePrefab = PrefabDir + "/House.prefab";

        public static void RunHeavy()
        {
            Run("heavy", Heavy);
        }

        static void Heavy()
        {
            Copies();
            PartialCopies();
            ModelCopies();
            Heaviest();
        }

        // 30 copies of a villager prefab whose Rig animates always (the last one with its own value), and a guard that is
        // not a copy. Also used by the screenshot of the Problems tab.
        public static System.Collections.Generic.List<GameObject> BuildCopiesScene()
        {
            NewScene("Heavy-Copies");
            Directory.CreateDirectory(PrefabDir);
            var template = new GameObject("Villager");
            var rig = new GameObject("Rig");
            rig.transform.SetParent(template.transform, false);
            rig.AddComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var prefab = PrefabUtility.SaveAsPrefabAsset(template, VillagerPrefab);
            Object.DestroyImmediate(template);
            var copies = Enumerable.Range(0, 30).Select(i =>
            {
                var copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                copy.name = "Villager (" + i + ")";
                copy.transform.position = new Vector3(i * 2f, 0f, 0f);
                return copy;
            }).ToList();
            // The last copy keeps its own value of the culling mode: an override equal to the prefab's.
            var own = copies[29].transform.Find("Rig").GetComponent<Animator>();
            var sourceAnimator = PrefabUtility.GetCorrespondingObjectFromSource(own);
            var mods = PrefabUtility.GetPropertyModifications(copies[29]).ToList();
            mods.Add(new PropertyModification { target = sourceAnimator, propertyPath = "m_CullingMode", value = ((int)AnimatorCullingMode.AlwaysAnimate).ToString() });
            PrefabUtility.SetPropertyModifications(copies[29], mods.ToArray());
            copies[29].name = "Villager (29)";
            new GameObject("Guard_Captain").AddComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return copies;
        }

        // A villager prefab whose Rig animates always: one copies row, one fix in the prefab.
        static void Copies()
        {
            var copies = BuildCopiesScene();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            var own = copies[29].transform.Find("Rig").GetComponent<Animator>();
            Check(new SerializedObject(own).FindProperty("m_CullingMode").prefabOverride, "the last copy has its own culling mode");

            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var always = result.issues.Where(i => i.checkId == "ANIM_ALWAYS_ANIMATE" && i.objectPath.StartsWith("Villager")).ToList();
            Check(always.Count == 30, "30 copies report ANIM_ALWAYS_ANIMATE (" + always.Count + ")");
            var rows = PrefabFixes.Rows(always);
            Check(rows.Count == 1 && rows[0].copies != null && rows[0].copies.issues.Count == 30, "copies together: one row of 30 copies");
            var row = rows[0].copies;
            Check(row.assetPath == VillagerPrefab && row.pathInAsset == "Rig" && row.Label == "Villager.prefab › Rig", "the row names the prefab and the Rig inside it");
            Check(row.CanFixInPrefab, "a prefab of the project can be fixed in the prefab");
            Check(PrefabFixes.PlanCount(always, true) == 1 && PrefabFixes.PlanCount(always, false) == 30, "the plan: one fix with copies together, 30 without");
            var plan = PrefabFixes.Plan(always, true);
            Check(plan.Count == 1 && plan[0].inPrefab && plan[0].fixTouchesAsset && plan[0].FixDescription.Contains(VillagerPrefab) && plan[0].FixDescription.Contains("30") &&
                  row.Copies == 30,
                "the prefab fix is an asset change naming the prefab and the copies");

            int applied = AutoFixer.Apply(plan);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerPrefab).transform.Find("Rig").GetComponent<Animator>();
            Check(applied == 1 && asset.cullingMode == AnimatorCullingMode.CullUpdateTransforms, "the prefab asset now has Cull Update Transforms");
            var animators = copies.Select(c => c.transform.Find("Rig").GetComponent<Animator>()).ToList();
            Check(animators.All(a => a.cullingMode == AnimatorCullingMode.CullUpdateTransforms), "every copy has Cull Update Transforms");
            int overridden = animators.Take(29).Count(a => new SerializedObject(a).FindProperty("m_CullingMode").prefabOverride);
            Check(overridden == 0, "the 29 copies got it from the prefab, not as overrides (" + overridden + ")");
            Check(SceneAuditor.RunFullAudit(new VRBudgets(), false).issues.All(i => i.checkId != "ANIM_ALWAYS_ANIMATE" || !i.objectPath.StartsWith("Villager")), "a new run finds none on the copies");

            // Ctrl+Z takes back the prefab and the copies together (the apply is recorded as a user action).
            Undo.PerformUndo();
            asset = AssetDatabase.LoadAssetAtPath<GameObject>(VillagerPrefab).transform.Find("Rig").GetComponent<Animator>();
            Check(asset.cullingMode == AnimatorCullingMode.AlwaysAnimate && animators[0].cullingMode == AnimatorCullingMode.AlwaysAnimate &&
                  !new SerializedObject(animators[0]).FindProperty("m_CullingMode").prefabOverride && animators[1].cullingMode == AnimatorCullingMode.AlwaysAnimate,
                "Ctrl+Z brings back the prefab and the copies, with no override left on the first copy");
        }

        // A part of the copies (a search, a pick, acceptable ones) or a deleted copy: never the prefab, copy by copy.
        static void PartialCopies()
        {
            var copies = BuildCopiesScene();
            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var always = result.issues.Where(i => i.checkId == "ANIM_ALWAYS_ANIMATE" && i.objectPath.StartsWith("Villager")).ToList();
            var totals = CopyTotals.Of(result.issues);
            var five = always.Take(5).ToList();
            var rows = PrefabFixes.Rows(five, totals);
            Check(rows.Count == 1 && rows[0].copies != null && !rows[0].copies.complete && !rows[0].copies.CanFixInPrefab,
                "5 of 30 copies picked: one row, not the prefab");
            Check(PrefabFixes.PlanCount(five, true, totals) == 5 && PrefabFixes.Plan(five, true, totals).All(i => !i.inPrefab), "5 picked copies: 5 fixes on the copies");
            Check(PrefabFixes.Rows(always, totals)[0].copies.CanFixInPrefab && PrefabFixes.PlanCount(always, true, totals) == 1, "all 30: the prefab");

            always[3].accepted = true;
            var withAccepted = CopyTotals.Of(result.issues);
            Check(!PrefabFixes.Rows(always, withAccepted)[0].copies.CanFixInPrefab && PrefabFixes.PlanCount(always, true, withAccepted) == 29,
                "one copy acceptable: not the prefab, the other 29 one by one");
            always[3].accepted = false;

            Object.DestroyImmediate(copies[0]);
            // The deleted copy leaves the copies row (a row of its own, nothing to fix); the row is no longer complete.
            var deletedRows = PrefabFixes.Rows(always, totals);
            var row = deletedRows.First(r => r.copies != null).copies;
            Check(!row.CanFixInPrefab && row.issues.Count == 29 && row.Fixable.Count == 29 && deletedRows.Count == 2,
                "a deleted copy: not the prefab, 29 fixes on the copies left");
            int applied = AutoFixer.Apply(PrefabFixes.Plan(always, true, totals));
            Check(applied == 29 && copies.Skip(1).All(c => c.transform.Find("Rig").GetComponent<Animator>().cullingMode == AnimatorCullingMode.CullUpdateTransforms),
                "the 29 copies left are fixed on themselves");
        }

        // Copies of a model (OBJ): one row, but a model cannot be edited, so each copy is fixed on itself.
        static void ModelCopies()
        {
            NewScene("Heavy-ModelCopies");
            var model = AssetDatabase.LoadMainAssetAtPath(AuditTestAssets.Root + "/Models/Clean.obj");
            var issues = Enumerable.Range(0, 3).Select(i =>
            {
                var copy = (GameObject)PrefabUtility.InstantiatePrefab(model);
                var r = copy.GetComponentInChildren<MeshRenderer>();
                var issue = new AuditIssue { checkId = "TEST_MODEL", messageKey = "TEST_MODEL", category = Category.Lighting, severity = Severity.Info, target = r, args = new object[0] };
                issue.SetFix(FixKind.Review, r, false, () => r.receiveGI = ReceiveGI.LightProbes);
                return issue;
            }).ToList();
            var rows = PrefabFixes.Rows(issues);
            Check(rows.Count == 1 && rows[0].copies != null && rows[0].copies.issues.Count == 3, "model copies together: one row of 3");
            Check(!rows[0].copies.CanFixInPrefab && PrefabFixes.PlanCount(issues, true) == 3, "a model is not fixed in the prefab: 3 fixes on the copies");
            var scene = new GameObject("Plain").AddComponent<Animator>();
            var single = new AuditIssue { checkId = "TEST_MODEL", messageKey = "TEST_MODEL", target = scene, args = new object[0] };
            Check(PrefabFixes.SourceOf(single) == null && PrefabFixes.Rows(new[] { single }.ToList())[0].copies == null, "an object that is not a copy stays a single row");
        }

        static void Heaviest()
        {
            BuildHousesScene();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            long grid = AuditContext.Tris(AuditTestAssets.Grid12k);
            var r = HeavyReport.Build(new VRBudgets().objectTrisWarning);
            HeaviestChecks(r, grid);
        }

        // Forty houses of 12 000 triangles without LODGroup, three loose grid objects and a LOD group of the same grid.
        // Also used by the screenshot of the Heaviest tab.
        public static void BuildHousesScene()
        {
            NewScene("Heavy-Houses");
            var template = new GameObject("House");
            template.AddComponent<MeshFilter>().sharedMesh = AuditTestAssets.Grid12k;
            template.AddComponent<MeshRenderer>().sharedMaterial = AuditTestAssets.Plain;
            var prefab = PrefabUtility.SaveAsPrefabAsset(template, HousePrefab);
            Object.DestroyImmediate(template);
            for (int i = 0; i < 40; i++) ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).transform.position = new Vector3(i * 3f, 0f, 0f);
            for (int i = 0; i < 3; i++) MeshObject("Grid_" + i, AuditTestAssets.Grid12k, new Vector3(i * 3f, 0f, 10f), AuditTestAssets.Plain);
            var lodRoot = new GameObject("Lod_House");
            var near = MeshObject("Near", AuditTestAssets.Grid30k, Vector3.zero, AuditTestAssets.Plain);
            var far = MeshObject("Far", AuditTestAssets.Grid30k, Vector3.zero, AuditTestAssets.Plain);
            near.transform.SetParent(lodRoot.transform, false);
            far.transform.SetParent(lodRoot.transform, false);
            lodRoot.AddComponent<LODGroup>().SetLODs(new[]
            {
                new LOD(0.5f, new Renderer[] { near.GetComponent<Renderer>() }), new LOD(0.1f, new Renderer[] { far.GetComponent<Renderer>() })
            });
        }

        static void HeaviestChecks(HeavyReport r, long grid)
        {
            var house = r.sources.FirstOrDefault(s => s.assetPath == HousePrefab);
            Check(house != null && house.kind == SourceKind.Prefab && house.Copies == 40 && house.TrisPerCopy == grid && house.tris == grid * 40,
                "forty houses: one prefab row, 40 copies, " + grid + " per copy (" + (house != null ? house.tris.ToString() : "—") + " in total)");
            Check(house != null && r.NoLod(house) && house.drawCalls == 40 && house.materials.Count == 1, "the house: no LOD, 40 draw calls, 1 material");
            Check(r.sources[0] == house && r.Sorted(0)[0] == house, "the houses are the heaviest source");
            long grid30 = AuditContext.Tris(AuditTestAssets.Grid30k);
            var loose = r.sources.FirstOrDefault(s => s.kind == SourceKind.Mesh && s.label.StartsWith("Grid12k"));
            Check(loose != null && loose.Copies == 3 && loose.TrisPerCopy == grid && r.NoLod(loose), "3 loose grid objects: 3 copies of " + grid + ", no LOD");
            var lod = r.sources.FirstOrDefault(s => s.label.StartsWith("Grid30k"));
            Check(lod != null && lod.Copies == 1 && lod.tris == grid30 && !r.NoLod(lod) && lod.inLodGroup,
                "the LOD group: one copy (LOD1 is no copy), LOD0 triangles only, no \"no LOD\" mark (" + (lod != null ? lod.Copies + "/" + lod.tris : "—") + ")");
            Check(System.Math.Abs(r.Share(house, 0) - (double)house.tris / (house.tris + loose.tris + lod.tris)) < 0.001 && r.tris == house.tris + loose.tris + lod.tris,
                "the share of the houses is their part of all triangles");
            Check(System.Math.Abs(r.sources.Sum(s => r.Share(s, 2)) - (r.textureBytes > 0 ? 1.0 : 0.0)) < 0.001, "texture shares add up to 100 % with shared textures");

            var csv = Path.Combine(ReportDir, "heavy.csv");
            r.SaveCsv(csv);
            var lines = File.ReadAllLines(csv);
            Check(lines[0].StartsWith("Source,Kind,AssetPath,Copies") && lines.Length == r.sources.Count + 1, "CSV: header and one row per source");

            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var report = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, null));
            Check(report.heavy.Length >= 2 && report.heavy[0].name == "House.prefab" && report.heavy[0].copies == 40 && report.heavy[0].kind == "Prefab",
                "JSON: heavy[0] is the house prefab with 40 copies");
        }
    }
}
