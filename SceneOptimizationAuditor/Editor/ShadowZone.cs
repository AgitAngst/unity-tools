using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityTools.SceneOptimization
{
    // The point and the radius of "Shadows" on the Lighting tab (owner, 27.10.2026): kept in the viewpoint file of the scenes.
    [Serializable]
    public sealed class ShadowZone
    {
        public const float DefaultRadius = 30f;
        public const float MinRadius = 1f;
        public const float MaxRadius = 1000f;

        public bool placed;
        public Vector3 center;
        public float radius = DefaultRadius;
        // What is turned off beyond the radius: Cast Shadows, Receive Shadows, Contribute GI (owner, 27.10.2026).
        public bool offCast = true;
        public bool offReceive;
        public bool offGI;
        // Cast Shadows decides the baked shadow too: objects lit from lightmaps keep it while this is on.
        public bool keepBaked = true;
    }

    // A renderer that casts or receives shadows or takes part in global illumination, and how far it is from the point.
    public sealed class ShadowRow
    {
        public Renderer renderer;
        public long tris;
        // Hierarchy path, for the search: a parent's name finds every renderer under it.
        public string path = "";
        // From the point to the nearest point of the bounds.
        public float distance;
        public bool far;

        public ShadowCastingMode Mode { get { return renderer != null ? renderer.shadowCastingMode : ShadowCastingMode.Off; } }

        public bool ShadowOnly { get { return Mode == ShadowCastingMode.ShadowsOnly; } }

        // Shadows Only objects exist for their shadow: they keep Cast Shadows.
        public bool Casts { get { return Mode == ShadowCastingMode.On || Mode == ShadowCastingMode.TwoSided; } }

        public bool Receives { get { return renderer != null && renderer.receiveShadows; } }

        public bool ContributesGI
        {
            get { return renderer != null && (GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.ContributeGI) != 0; }
        }

        // Contribute GI and lit from lightmaps.
        public bool Baked
        {
            get
            {
                var mesh = renderer as MeshRenderer;
                return mesh != null && mesh.receiveGI == ReceiveGI.Lightmaps && ContributesGI;
            }
        }
    }

    public static class ShadowReport
    {
        // Enabled mesh and skinned mesh renderers of the loaded scenes at any depth that cast or receive shadows or
        // contribute to global illumination.
        public static List<ShadowRow> Collect()
        {
            var rows = new List<ShadowRow>();
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                        if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                        var row = new ShadowRow { renderer = r, tris = TrisOf(r), path = AuditContext.PathOf(r.transform) };
                        if (row.Casts || row.ShadowOnly || row.Receives || row.ContributesGI) rows.Add(row);
                    }
            }
            return rows;
        }

        static long TrisOf(Renderer r)
        {
            var skinned = r as SkinnedMeshRenderer;
            if (skinned != null) return skinned.sharedMesh != null ? AuditContext.Tris(skinned.sharedMesh) : 0;
            var filter = r.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null ? AuditContext.Tris(filter.sharedMesh) : 0;
        }

        // Distances from the point, farthest first; while the point is not set nothing is far and the heaviest go first.
        public static void Measure(List<ShadowRow> rows, ShadowZone zone)
        {
            rows.RemoveAll(r => r.renderer == null);
            foreach (var row in rows)
            {
                row.distance = zone.placed ? Vector3.Distance(zone.center, row.renderer.bounds.ClosestPoint(zone.center)) : 0f;
                row.far = zone.placed && row.distance > zone.radius;
            }
            if (zone.placed) rows.Sort((a, b) => b.distance.CompareTo(a.distance));
            else rows.Sort((a, b) => b.tris.CompareTo(a.tris));
        }

        // A parent's name finds every renderer under it; case does not matter.
        public static bool Matches(ShadowRow row, string search)
        {
            return string.IsNullOrEmpty(search) || row.path.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool CastOff(ShadowRow row, ShadowZone zone)
        {
            return zone.offCast && row.Casts && !(zone.keepBaked && row.Baked);
        }

        public static bool ReceiveOff(ShadowRow row, ShadowZone zone)
        {
            return zone.offReceive && row.Receives;
        }

        public static bool GIOff(ShadowRow row, ShadowZone zone)
        {
            return zone.offGI && row.ContributesGI;
        }

        // What the boxes would turn off on this renderer, wherever it is.
        public static bool Changes(ShadowRow row, ShadowZone zone)
        {
            return row.renderer != null && (CastOff(row, zone) || ReceiveOff(row, zone) || GIOff(row, zone));
        }

        // What the button turns off: beyond the radius only.
        public static bool WillChange(ShadowRow row, ShadowZone zone)
        {
            return row.far && Changes(row, zone);
        }

        // The ticked settings off as fixes of the scene objects: the confirmation window, one Undo step, overrides of prefab copies.
        public static List<AuditIssue> TurnOffFixes(IEnumerable<ShadowRow> rows, ShadowZone zone)
        {
            var fixes = new List<AuditIssue>();
            foreach (var row in rows)
            {
                if (!Changes(row, zone)) continue;
                var r = row.renderer;
                bool cast = CastOff(row, zone), receive = ReceiveOff(row, zone), gi = GIOff(row, zone);
                var what = new List<string>();
                if (cast) what.Add("Cast Shadows");
                if (receive) what.Add("Receive Shadows");
                if (gi) what.Add("Contribute GI");
                var issue = new AuditIssue
                {
                    checkId = "SHADOW_FAR",
                    messageKey = "SHADOW_FAR",
                    category = Category.Lighting,
                    severity = Severity.Info,
                    target = r.gameObject,
                    scene = r.gameObject.scene.name,
                    objectPath = row.path,
                    args = new object[] { Math.Round(row.distance, 1), string.Join(", ", what.ToArray()) }
                };
                issue.SetFix(FixKind.Review, r, false, () =>
                {
                    if (cast) r.shadowCastingMode = ShadowCastingMode.Off;
                    if (receive) r.receiveShadows = false;
                    if (gi)
                    {
                        // The static flags live on the GameObject: recorded in the same Undo group as the renderer.
                        Undo.RecordObject(r.gameObject, AutoFixer.UndoName);
                        var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                        GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags & ~StaticEditorFlags.ContributeGI);
                    }
                });
                fixes.Add(issue);
            }
            return fixes;
        }
    }
}
