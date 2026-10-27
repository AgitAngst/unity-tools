using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Quick re-check (2.7.0): re-checking every row of an unchanged scene gives the same rows (so no check over the whole
    // scene is taken for a check of one object), a row fixed by hand goes, a row still there is marked, a new problem of a
    // re-checked object appears, rows over the whole scene are left as they were, a deleted object takes its rows away.
    public static partial class AuditTestViewpoints
    {
        public static void RunRecheck()
        {
            Run("recheck", RecheckChecks);
        }

        static string RowKey(AuditIssue i)
        {
            return i.checkId + "|" + i.severity + "|" + i.scenePath + "|" + i.objectPath + "|" + i.assetPath + "|" + i.settingsPath + "|" + i.Message;
        }

        static GameObject TargetObject(AuditIssue i)
        {
            var c = i.target as Component;
            return c != null ? c.gameObject : i.target as GameObject;
        }

        static void RecheckChecks()
        {
            AuditTestScene.Build("base");
            var budgets = new VRBudgets();
            var r = SceneAuditor.RunFullAudit(budgets, false);
            var before = r.issues.Select(RowKey).OrderBy(k => k).ToList();
            var order = r.issues.Select(RowKey).ToList();
            var rows = r.issues.Where(i => Recheck.KindOf(i, r) <= RecheckKind.Setting).ToList();
            var sceneRows = r.issues.Where(i => Recheck.KindOf(i, r) == RecheckKind.Scene).ToList();
            log.AppendLine("info: rows " + r.issues.Count + ", re-checkable " + rows.Count + ", over the whole scene " + sceneRows.Count);
            Check(rows.Count > 50 && sceneRows.Count > 5, "the seed has rows of both kinds");
            Check(rows.Any(i => Recheck.KindOf(i, r) == RecheckKind.Object) && rows.Any(i => Recheck.KindOf(i, r) == RecheckKind.Asset) &&
                  rows.Any(i => Recheck.KindOf(i, r) == RecheckKind.Setting), "objects, assets and settings can be re-checked");

            // One row at a time: the checks see only that object (or the users of that asset), so a check over the whole
            // scene taken for a check of one object loses or adds rows here.
            var wrong = new Dictionary<string, string>();
            foreach (var rowKey in rows.Select(RowKey).Distinct().ToList())
            {
                var row = r.issues.FirstOrDefault(i => RowKey(i) == rowKey);
                if (row == null) continue;
                Recheck.Run(r, new List<AuditIssue> { row }, budgets, false);
                var now = r.issues.Select(RowKey).OrderBy(k => k).ToList();
                if (now.SequenceEqual(before)) continue;
                foreach (var k in before.Except(now).Concat(now.Except(before)))
                {
                    var id = k.Substring(0, k.IndexOf('|'));
                    if (!wrong.ContainsKey(id)) wrong[id] = "after " + row.checkId + " " + row.objectPath + row.assetPath + ": " + (now.Contains(k) ? "extra " : "lost ") + k;
                }
                r = SceneAuditor.RunFullAudit(budgets, false);
            }
            foreach (var w in wrong.Values) log.AppendLine("  " + w);
            Check(wrong.Count == 0, "re-checking any one row of an unchanged scene gives the same rows (wrong checks: " + string.Join(", ", wrong.Keys.ToArray()) + ")");

            var all = Recheck.Run(r, r.issues.Where(i => Recheck.KindOf(i, r) <= RecheckKind.Setting).ToList(), budgets, false);
            Check(r.issues.Select(RowKey).OrderBy(k => k).SequenceEqual(before) && all.went == 0 && all.added == 0,
                "re-checking every row at once gives the same rows");
            Check(r.issues.Select(RowKey).SequenceEqual(order), "the rows keep their places in the list");
            sceneRows = r.issues.Where(i => Recheck.KindOf(i, r) == RecheckKind.Scene).ToList();
            Check(sceneRows.All(i => r.issues.Contains(i)), "rows over the whole scene are the same objects as after the full run");
            Check(r.issues.Where(i => Recheck.KindOf(i, r) <= RecheckKind.Setting).All(i => i.recheckedAt != default(System.DateTime)),
                "re-checked rows are marked");
            Check(r.rechecked == all.items && all.items > 0, "the run counts the re-checked items: " + all.items);

            // Fixed by hand: a LOD Group on the heavy mesh.
            var heavy = GameObject.Find("Mesh/30k");
            var noLod = r.issues.First(i => i.checkId == "MESH_NO_LOD" && TargetObject(i) == heavy);
            int noLodRows = r.issues.Count(i => i.checkId == "MESH_NO_LOD");
            var group = heavy.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(0.2f, new Renderer[] { heavy.GetComponent<Renderer>() }), new LOD(0.05f, new Renderer[0]) });
            var fixedByHand = Recheck.Run(r, new List<AuditIssue> { noLod }, budgets, false);
            Check(!r.issues.Any(i => i.checkId == "MESH_NO_LOD" && TargetObject(i) == heavy) && r.issues.Count(i => i.checkId == "MESH_NO_LOD") == noLodRows - 1,
                "the row of a mesh that got a LOD Group goes, the other meshes keep theirs");
            Check(fixedByHand.items == 1 && fixedByHand.onlyObjects && fixedByHand.went == 1, "one object re-checked, its row went (" + fixedByHand.went + ")");

            // Still there: another mesh, still over the threshold — the row stays with the new count and is marked.
            var light12k = GameObject.Find("Mesh/12k");
            var row12k = r.issues.First(i => i.checkId == "MESH_NO_LOD" && TargetObject(i) == light12k);
            var oldText = row12k.Message;
            light12k.GetComponent<MeshFilter>().sharedMesh = AuditTestAssets.Grid30k;
            var stayed = Recheck.Run(r, new List<AuditIssue> { row12k }, budgets, false);
            var updated = r.issues.FirstOrDefault(i => i.checkId == "MESH_NO_LOD" && TargetObject(i) == light12k);
            Check(updated != null && updated.Message != oldText && updated.recheckedAt != default(System.DateTime) && stayed.left >= 1 && stayed.went == 0,
                "a changed mesh keeps its row with the new text and the mark (" + (updated != null ? updated.Message : "gone") + ")");
            light12k.GetComponent<MeshFilter>().sharedMesh = AuditTestAssets.Grid12k;

            // A new problem of a re-checked object appears.
            heavy.transform.localScale = new Vector3(-1f, 1f, 1f);
            var anyRow = r.issues.First(i => TargetObject(i) == heavy || i.objectPath == "Mesh/30k");
            var grown = Recheck.Run(r, new List<AuditIssue> { anyRow }, budgets, false);
            Check(r.issues.Any(i => i.checkId == "GEN_NEGATIVE_SCALE" && TargetObject(i) == heavy) && grown.added >= 1, "a negative scale shows up as a new row");

            // A deleted object takes its rows away.
            var deadRow = r.issues.First(i => i.checkId == "GEN_NEGATIVE_SCALE" && TargetObject(i) == heavy);
            Object.DestroyImmediate(heavy);
            var deleted = Recheck.Run(r, new List<AuditIssue> { deadRow }, budgets, false);
            Check(!r.issues.Any(i => i.objectPath == "Mesh/30k") && deleted.went >= 1, "rows of a deleted object go");

            // A row over the whole scene cannot be re-checked and is never replaced.
            var budget = sceneRows.FirstOrDefault(i => i.checkId == "MESH_SCENE_TRIS" || i.checkId == "DC_BUDGET" || i.checkId == "TEX_MEMORY");
            if (budget != null)
            {
                var nothing = Recheck.Run(r, new List<AuditIssue> { budget }, budgets, false);
                Check(nothing.items == 0 && r.issues.Contains(budget), "a budget row is not re-checked and stays");
            }

            // The window: the icon of a whole-scene row is off, Re-check Picked works on picked rows.
            var window = ScriptableObject.CreateInstance<AuditorWindow>();
            try
            {
                var fresh = SceneAuditor.RunFullAudit(budgets, false);
                CallWindow(window, "Show", fresh, false);
                Check(!(bool)CallWindow(window, "CanRecheck", fresh.issues.First(i => Recheck.KindOf(i, fresh) == RecheckKind.Scene)),
                    "the window does not offer to re-check a whole-scene row");
                var three = fresh.issues.Where(i => Recheck.KindOf(i, fresh) == RecheckKind.Object).GroupBy(TargetObject).Take(3).Select(g => g.First()).ToList();
                var objects = new HashSet<GameObject>(three.Select(TargetObject));
                CallWindow(window, "RecheckRows", three);
                var shown = (AuditResult)FieldOf(window, "result");
                Check(shown.rechecked == 3 && shown.recheckedObjects, "Re-check Picked (3) re-checks three objects (" + shown.rechecked + ")");
                Check(shown.issues.Where(i => i.recheckedAt != default(System.DateTime)).All(i => objects.Contains(TargetObject(i)) || i.assetPath.Length > 0),
                    "only rows of those objects and their assets are marked");
                Check(((string)CallWindow(window, "BarStatus")).Contains(System.DateTime.Now.ToString("HH:")),
                    "the run bar tells about the re-check: " + (string)CallWindow(window, "BarStatus"));
            }
            finally
            {
                Object.DestroyImmediate(window);
                SetWindowField(null, "kept", null);
            }
        }
    }
}
