using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // "Shadows" on the Lighting tab (2.6.0): who is listed, distances from the point, what the boxes turn off, the search
    // through parents, the fixes with Undo, the point in the viewpoint file.
    public static partial class AuditTestViewpoints
    {
        public static void RunShadowZone()
        {
            Run("shadow-zone", ShadowZoneChecks);
        }

        static MeshRenderer Caster(string name, Vector3 position, Transform parent)
        {
            var go = MeshObject(name, Resources.GetBuiltinResource<Mesh>("Cube.fbx"), position, AuditTestAssets.Plain);
            if (parent != null) go.transform.SetParent(parent, true);
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.On;
            return r;
        }

        static string[] Names(System.Collections.Generic.IEnumerable<ShadowRow> rows)
        {
            return rows.Select(r => r.renderer.name).OrderBy(n => n).ToArray();
        }

        static void ShadowZoneChecks()
        {
            var path = NewScene("ShadowZone");
            var near = Caster("Near", new Vector3(5f, 0f, 0f), null);
            var trees = new GameObject("Trees").transform;
            trees.SetParent(new GameObject("Forest").transform, false);
            var pine = Caster("Pine_01", new Vector3(50f, 0f, 0f), trees);
            var cliff = Caster("Cliff", new Vector3(0f, 0f, 60f), null);
            GameObjectUtility.SetStaticEditorFlags(cliff.gameObject, StaticEditorFlags.ContributeGI);
            cliff.receiveGI = ReceiveGI.Lightmaps;
            var proxy = Caster("Proxy", new Vector3(-70f, 0f, 0f), null);
            proxy.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            // Casts nothing but receives shadows: listed for "Receive Shadows".
            MeshObject("Ground_Far", Resources.GetBuiltinResource<Mesh>("Cube.fbx"), new Vector3(0f, 0f, -80f), AuditTestAssets.Plain);
            var none = MeshObject("Nothing", Resources.GetBuiltinResource<Mesh>("Cube.fbx"), new Vector3(0f, 0f, -90f), AuditTestAssets.Plain);
            none.GetComponent<MeshRenderer>().receiveShadows = false;
            Caster("Hidden", new Vector3(90f, 0f, 0f), null).gameObject.SetActive(false);

            var rows = ShadowReport.Collect();
            Check(Names(rows).SequenceEqual(new[] { "Cliff", "Ground_Far", "Near", "Pine_01", "Proxy" }),
                "listed: casting, Shadows Only, receiving; not the inactive one, not one that does nothing (" + string.Join(", ", Names(rows)) + ")");
            var pineRow = rows.First(r => r.renderer == pine);
            Check(pineRow.path == "Forest/Trees/Pine_01" && rows.First(r => r.renderer == cliff).Baked && !pineRow.Baked, "a nested mesh with its path; Contribute GI with lightmaps is baked");

            var zone = new ShadowZone { placed = true, center = Vector3.zero, radius = 30f };
            ShadowReport.Measure(rows, zone);
            Check(Mathf.Abs(pineRow.distance - 49.5f) < 0.01f && pineRow.far && !rows.First(r => r.renderer == near).far,
                "distance to the nearest point of the bounds: 49.5 m, beyond 30 m; Near is inside");
            Check(rows.Select(r => r.renderer.name).SequenceEqual(new[] { "Ground_Far", "Proxy", "Cliff", "Pine_01", "Near" }), "farthest first");

            Check(Names(rows.Where(r => ShadowReport.WillChange(r, zone))).SequenceEqual(new[] { "Pine_01" }),
                "Cast Shadows, baked kept: only Pine_01 (Cliff baked, Proxy Shadows Only, Ground_Far casts nothing)");
            zone.keepBaked = false;
            Check(Names(rows.Where(r => ShadowReport.WillChange(r, zone))).SequenceEqual(new[] { "Cliff", "Pine_01" }), "without the box Cliff too");
            zone.offReceive = true;
            Check(Names(rows.Where(r => ShadowReport.WillChange(r, zone))).SequenceEqual(new[] { "Cliff", "Ground_Far", "Pine_01", "Proxy" }),
                "Receive Shadows: every far receiver");
            var giOnly = new ShadowZone { placed = true, radius = 30f, offCast = false, offGI = true };
            Check(Names(rows.Where(r => ShadowReport.WillChange(r, giOnly))).SequenceEqual(new[] { "Cliff" }), "Contribute GI: only Cliff has it");
            Check(Names(rows.Where(r => ShadowReport.Matches(r, "forest"))).SequenceEqual(new[] { "Pine_01" }), "the search finds a mesh by a parent's name, any case");

            zone.offGI = true;
            var fixes = ShadowReport.TurnOffFixes(rows.Where(r => ShadowReport.WillChange(r, zone)), zone);
            AutoFixer.Apply(fixes);
            bool cliffGI = (GameObjectUtility.GetStaticEditorFlags(cliff.gameObject) & StaticEditorFlags.ContributeGI) != 0;
            Check(pine.shadowCastingMode == ShadowCastingMode.Off && !pine.receiveShadows && cliff.shadowCastingMode == ShadowCastingMode.Off && !cliffGI &&
                  proxy.shadowCastingMode == ShadowCastingMode.ShadowsOnly && !proxy.receiveShadows && near.shadowCastingMode == ShadowCastingMode.On,
                "fixes: far ones off, Shadows Only keeps its mode, Near untouched");
            Undo.PerformUndo();
            cliffGI = (GameObjectUtility.GetStaticEditorFlags(cliff.gameObject) & StaticEditorFlags.ContributeGI) != 0;
            Check(pine.shadowCastingMode == ShadowCastingMode.On && pine.receiveShadows && cliffGI && proxy.receiveShadows, "one Undo brings everything back, Contribute GI too");

            ShadowReport.Measure(rows, new ShadowZone());
            Check(rows.All(r => !r.far && !ShadowReport.WillChange(r, zone)), "no point: nothing is far, the button changes nothing");

            var set = ViewpointSet.Load(new[] { path });
            Check(!set.shadows.placed && Mathf.Approximately(set.shadows.radius, ShadowZone.DefaultRadius) && set.shadows.offCast && set.shadows.keepBaked,
                "a new viewpoint file: no point, 30 m, Cast Shadows, baked kept");
            set.shadows = new ShadowZone { placed = true, center = new Vector3(12f, 0f, -4.5f), radius = 45f, offGI = true };
            set.Save();
            var again = ViewpointSet.Load(new[] { path });
            Check(again.shadows.placed && again.shadows.center == new Vector3(12f, 0f, -4.5f) && Mathf.Approximately(again.shadows.radius, 45f) && again.shadows.offGI,
                "the point, the radius and the boxes are kept with the viewpoints");
            DeleteViewpointFile(path);
        }
    }
}
