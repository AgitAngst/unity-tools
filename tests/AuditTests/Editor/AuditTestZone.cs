using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // The player zone of the Viewpoints tab (3.0.0, owner 29.10.2026: "the zone of the play field for the player, from which
    // the script measures and where it puts the viewpoints"): a NavMesh larger than the zone, a turned rectangle on the
    // upper floor of two, rays only over the zone, manual viewpoints outside it, the file and the command line.
    public static partial class AuditTestViewpoints
    {
        public static void RunPlayerZone()
        {
            Run("player-zone", PlayerZone);
        }

        static string Point(Vector3 p)
        {
            return "(" + p.x.ToString("F1") + ", " + p.y.ToString("F1") + ", " + p.z.ToString("F1") + ")";
        }

        static void PlayerZone()
        {
            // Rectangle geometry: inside seen from above, between the floor and the top; turned by 45°.
            var square = new ZoneRect { center = new Vector3(0f, 0f, 0f), size = new Vector2(10f, 4f), height = 3f };
            Check(square.Contains(new Vector3(4.9f, 1.7f, 1.9f)) && !square.Contains(new Vector3(4.9f, 1.7f, 2.1f)) &&
                  !square.Contains(new Vector3(0f, 3.2f, 0f)) && !square.Contains(new Vector3(0f, -0.2f, 0f)),
                "a 10 × 4 m rectangle 3 m high: inside up to its edges and between the floor and the top");
            var turned = new ZoneRect { center = Vector3.zero, size = new Vector2(10f, 4f), yaw = 90f, height = 3f };
            Check(turned.Contains(new Vector3(1.9f, 1f, 4.9f)) && !turned.Contains(new Vector3(4.9f, 1f, 1.9f)), "turned by 90°: width along Z");
            Check(Mathf.Abs(turned.Area - 40f) < 0.01f, "area 40 m²");

            NavMeshZone();
            FloorsZone();
            ZoneFileAndCli();

            foreach (var language in new[] { Lang.En, Lang.Ru })
            {
                var lang = Strings.Current;
                Strings.Current = language;
                foreach (var key in Strings.Keys.Where(k => k.StartsWith("ui.vp.zone", System.StringComparison.Ordinal) ||
                                                            k.StartsWith("ui.vp.panel.zone", System.StringComparison.Ordinal) ||
                                                            k == "ui.vp.outside" || k == "ui.vp.outside.tip" || k == "ui.vp.sources.zone" ||
                                                            k == "ui.vp.legend.zone" || k == "ui.vp.legend.outside").ToList())
                    Check(Strings.Get(key).Length > 0, language + " " + key);
                log.AppendLine(language + ": " + Strings.Count("ui.vp.zone.rects", 2) + "; " + Strings.Count("ui.vp.zone.rects", 5) + "; " +
                               Strings.Format("ui.vp.sources.zone", Strings.Count("ui.vp.zone.rects", 1), 100));
                Strings.Current = lang;
            }
        }

        // A 30 × 30 m NavMesh and a 10 × 10 m zone in one corner: every source keeps only what is inside.
        static void NavMeshZone()
        {
            var path = NewScene("Zone-NavMesh");
            var floor = Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 30f), AuditTestAssets.Plain, false);
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            var inside = new GameObject("CameraInside").AddComponent<Camera>();
            inside.transform.position = new Vector3(-8f, 1.7f, -8f);
            var outside = new GameObject("CameraOutside").AddComponent<Camera>();
            outside.transform.position = new Vector3(8f, 1.7f, 8f);
            // Not stereo: a stereo camera would be taken for the player's own.
            inside.stereoTargetEye = StereoTargetEyeMask.None;
            outside.stereoTargetEye = StereoTargetEyeMask.None;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            DeleteViewpointFile(path);

            var zone = new System.Collections.Generic.List<ZoneRect>
            {
                new ZoneRect { center = new Vector3(-7.5f, -0.5f, -7.5f), size = new Vector2(10f, 10f), height = 3f }
            };
            var settings = new PlacementSettings { navMesh = true, teleports = false, cameras = true, rays = false, step = 2f };
            float step;
            var all = ViewpointPlacer.Place(settings, null, out step);
            var inZone = ViewpointPlacer.Place(settings, zone, out step);
            log.AppendLine("NavMesh: " + all.Count + " on the level, " + inZone.Count + " in the zone");
            Check(inZone.Count > 0 && inZone.Count < all.Count / 4 && inZone.All(p => ViewpointPlacer.InZone(p.position, zone)),
                "a zone of a ninth of the NavMesh: only viewpoints inside it (" + inZone.Count + " of " + all.Count + ")");
            Check(inZone.Any(p => p.kind == ViewpointKind.Camera && p.objectName == "CameraInside") && !inZone.Any(p => p.objectName == "CameraOutside"),
                "a scene camera inside the zone stays, the one outside goes");
            Check(inZone.Count(p => p.kind == ViewpointKind.NavMesh) >= 20, "NavMesh nodes fill the zone: 2 m step in 10 × 10 m (" + inZone.Count(p => p.kind == ViewpointKind.NavMesh) + ")");

            // The set: placing with the zone, a manual viewpoint outside stays and is marked; without "only in the zone" no marks.
            var set = ViewpointSet.Load(new[] { path });
            set.zone = zone;
            set.placement = settings;
            var manualOut = set.AddManual(new Vector3(10f, 1.7f, 10f), 0f);
            var manualIn = set.AddManual(new Vector3(-7f, 1.7f, -7f), 0f);
            set.ReplaceAuto(ViewpointPlacer.Place(settings, set.ActiveZone, out step), step);
            Check(set.Find(manualOut.id) != null && set.OutsideZone(manualOut) && !set.OutsideZone(manualIn) && set.points.Where(p => p.IsAuto).All(p => !set.OutsideZone(p)),
                "placing again keeps the manual viewpoint outside the zone and marks it; the one inside and the automatic ones have no mark");
            set.placement.onlyZone = false;
            Check(set.ActiveZone == null && !set.OutsideZone(manualOut), "\"Only in the player zone\" off: the zone is not used, no marks");
            set.placement.onlyZone = true;
            Check(Mathf.Abs(set.ZoneArea - 100f) < 0.01f, "zone area 100 m²");
            Object.DestroyImmediate(set);
        }

        // Two floors: the ground at 0 m and an upper floor at 4 m; no NavMesh, so the rays find the floors.
        static void FloorsZone()
        {
            NewScene("Zone-Floors");
            Box("Ground", new Vector3(0f, -0.1f, 0f), new Vector3(60f, 0.2f, 60f), AuditTestAssets.Plain, true);
            Box("Upper", new Vector3(0f, 3.9f, 0f), new Vector3(20f, 0.2f, 20f), AuditTestAssets.Plain, true);
            var settings = new PlacementSettings { navMesh = true, teleports = true, rays = true, step = 2f };
            float step;
            var level = ViewpointPlacer.Place(settings, null, out step);

            var upper = AuditorWindow.NewZoneRect(new Vector3(0f, 10f, 0f));
            Check(Mathf.Abs(upper.center.y - 3.5f) < 0.01f && upper.size == new Vector2(10f, 10f) && Mathf.Abs(upper.height - 3f) < 0.01f,
                "a new rectangle under a pivot above the upper floor: its floor half a metre under it, 10 × 10 m, 3 m high (" + Point(upper.center) + ")");
            var between = AuditorWindow.NewZoneRect(new Vector3(0f, 2f, 0f));
            Check(Mathf.Abs(between.center.y + 0.5f) < 0.01f,
                "a pivot between the floors: the rectangle stands on the ground under it, not on the upper floor above (" + Point(between.center) + ")");
            upper.yaw = 45f;
            var onUpper = ViewpointPlacer.Place(settings, new System.Collections.Generic.List<ZoneRect> { upper }, out step);
            log.AppendLine("rays: " + level.Count + " on the level, " + onUpper.Count + " in the turned zone on the upper floor");
            Check(onUpper.Count >= 15 && onUpper.All(p => p.kind == ViewpointKind.Ray && Mathf.Abs(p.position.y - 5.7f) < 0.05f && upper.Contains(p.position)),
                "a rectangle turned by 45° on the upper floor: rays only over it, every viewpoint on the upper floor inside it (" + onUpper.Count + ")");
            Check(onUpper.Count < level.Count / 10, "the rays cover the zone, not the whole level");

            var ground = new ZoneRect { center = new Vector3(20f, -0.5f, 20f), size = new Vector2(10f, 10f), height = 3f };
            var under = new ZoneRect { center = new Vector3(0f, -0.5f, 0f), size = new Vector2(6f, 6f), height = 3f };
            var below = ViewpointPlacer.Place(settings, new System.Collections.Generic.List<ZoneRect> { ground, under }, out step);
            Check(below.Count > 0 && below.All(p => Mathf.Abs(p.position.y - 1.7f) < 0.05f) && below.Any(p => under.Contains(p.position)),
                "ground floor rectangles, one of them under the upper floor: viewpoints on the ground only, under the upper floor too");
        }

        // The zone is kept in the viewpoint file; -auditViewpoints places inside it when the file has no viewpoints yet.
        static void ZoneFileAndCli()
        {
            var path = NewScene("Zone-Cli");
            Box("Ground", new Vector3(0f, -0.1f, 0f), new Vector3(40f, 0.2f, 40f), AuditTestAssets.Plain, true);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            DeleteViewpointFile(path);
            var set = ViewpointSet.Load(new[] { path });
            set.zone.Add(new ZoneRect { center = new Vector3(5f, -0.5f, 5f), size = new Vector2(8f, 6f), yaw = 30f, height = 3f });
            set.Save();
            Object.DestroyImmediate(set);
            var again = ViewpointSet.Load(new[] { path });
            var r = again.zone.FirstOrDefault();
            Check(again.zone.Count == 1 && r != null && r.size == new Vector2(8f, 6f) && Mathf.Abs(r.yaw - 30f) < 0.01f && again.placement.onlyZone,
                "the zone is read back from the viewpoint file; \"only in the zone\" is on by default");

            Undo.RecordObject(again, "Edit Player Zone");
            again.zone[0].size = new Vector2(2f, 2f);
            Undo.PerformUndo();
            Check(again.zone[0].size == new Vector2(8f, 6f), "Undo brings the size back (" + again.zone[0].size + ")");
            Object.DestroyImmediate(again);

            var report = Path.Combine(ReportDir, "cli-zone.json");
            int code = ReportExporter.RunWithArgs(new[] { "Unity.exe", "-auditScene", path, "-auditReport", report, "-auditViewpoints" });
            var placed = ViewpointSet.Load(new[] { path });
            log.AppendLine("cli: " + placed.points.Count + " viewpoints");
            Check(code == 0 && placed.points.Count > 0 && placed.points.All(p => ViewpointPlacer.InZone(p.position, placed.zone)),
                "-auditViewpoints places inside the zone of the file (" + placed.points.Count + ")");
            Object.DestroyImmediate(placed);
            DeleteViewpointFile(path);
        }
    }
}
