using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace UnityTools.SceneOptimization
{
    public enum ViewpointKind { Manual = 0, NavMesh = 1, Teleport = 2, Camera = 3, Ray = 4 }

    // What one direction of a viewpoint sees.
    [Serializable]
    public sealed class ViewDirection
    {
        public float yaw;
        public long tris;
        public int drawCalls;
        public float transparentPercent;
    }

    // A place where the player stands: eye position and, for manual viewpoints and cameras, their own direction.
    [Serializable]
    public sealed class Viewpoint
    {
        public string id = "";
        public ViewpointKind kind;
        // Running number among viewpoints of the same kind; teleports and cameras keep the object name instead.
        public int number;
        public string objectName = "";
        public Vector3 position;
        public bool hasYaw;
        public float yaw;
        public List<ViewDirection> directions = new List<ViewDirection>();
        public int worst = -1;

        public bool Estimated { get { return worst >= 0 && worst < directions.Count; } }

        public ViewDirection Worst { get { return Estimated ? directions[worst] : null; } }

        public bool IsAuto { get { return kind != ViewpointKind.Manual; } }

        public string Name
        {
            get
            {
                switch (kind)
                {
                    case ViewpointKind.NavMesh: return "NavMesh " + number;
                    case ViewpointKind.Ray: return Strings.Format("ui.vp.rayName", number);
                    case ViewpointKind.Manual: return Strings.Format("ui.vp.manualName", number);
                    default: return objectName;
                }
            }
        }

        public void ClearEstimate()
        {
            directions.Clear();
            worst = -1;
        }
    }

    [Serializable]
    public sealed class PlacementSettings
    {
        public bool navMesh = true;
        public bool teleports = true;
        public bool cameras;
        public bool rays;
        public float step = VRBudgets.GridStep;
        public float eyeHeight = VRBudgets.EyeHeight;
    }

    // Viewpoints of one scene set in SceneAuditPoints/<scene>-<hash>.json next to Assets/, named like the audit history:
    // outside the import, can be committed. In memory it is a ScriptableObject that is never saved as an asset, so Undo
    // covers adding, moving and removing viewpoints; the scenes themselves are never changed.
    public sealed class ViewpointSet : ScriptableObject
    {
        public const string FolderName = "SceneAuditPoints";

        public string[] scenes = new string[0];
        public PlacementSettings placement = new PlacementSettings();
        // Grid step the last placement really used: it grows when a grid would give more than MaxViewpoints.
        public float placedStep;
        public List<Viewpoint> points = new List<Viewpoint>();
        // Last estimate: time (ISO 8601), tool version, how long it took and whether walls were counted.
        public string estimatedAt = "";
        public string estimateVersion = "";
        public float estimateSeconds;
        public bool estimateWalls;
        // Last tour in Play Mode (ViewpointTour).
        public TourMeasure tour = new TourMeasure();

        [NonSerialized] public string filePath = "";

        public static string Folder
        {
            get { return Path.Combine(Path.GetDirectoryName(Application.dataPath), FolderName); }
        }

        public static string FileFor(string[] scenePaths)
        {
            return Path.Combine(Folder, AuditHistory.FileNameFor(scenePaths));
        }

        public static string[] LoadedScenePaths()
        {
            var paths = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) paths.Add(scene.path);
            }
            return paths.ToArray();
        }

        public static ViewpointSet Load(string[] scenePaths)
        {
            var set = CreateInstance<ViewpointSet>();
            set.hideFlags = HideFlags.DontSave;
            set.filePath = FileFor(scenePaths);
            if (File.Exists(set.filePath))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(set.filePath), set);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SceneAudit] Could not read viewpoints from " + set.filePath + ": " + e.Message);
                }
            }
            set.scenes = scenePaths;
            return set;
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, JsonUtility.ToJson(this, true));
        }

        public bool HasEstimate { get { return estimatedAt.Length > 0 && points.Any(p => p.Estimated); } }

        public bool HasTour { get { return tour != null && tour.Exists; } }

        // Measured viewpoints of the last tour that are still in the set, in the order they were measured.
        public List<PointMeasure> MeasuredPoints()
        {
            if (tour == null) return new List<PointMeasure>();
            return tour.points.Where(m => m.Worst != null && Find(m.id) != null).ToList();
        }

        public int NextNumber(ViewpointKind kind)
        {
            int max = 0;
            foreach (var p in points) if (p.kind == kind) max = Math.Max(max, p.number);
            return max + 1;
        }

        public Viewpoint AddManual(Vector3 position, float yaw)
        {
            var point = new Viewpoint
            {
                id = Guid.NewGuid().ToString("N"),
                kind = ViewpointKind.Manual,
                number = NextNumber(ViewpointKind.Manual),
                position = position,
                hasYaw = true,
                yaw = yaw
            };
            points.Add(point);
            return point;
        }

        // A moved automatic viewpoint is the user's from now on: placing again keeps it.
        public void MakeManual(Viewpoint point)
        {
            if (!point.IsAuto) return;
            point.number = NextNumber(ViewpointKind.Manual);
            point.kind = ViewpointKind.Manual;
            point.objectName = "";
        }

        // New automatic viewpoints replace the old ones; manual ones stay. Every estimate and the tour are cleared.
        public void ReplaceAuto(List<Viewpoint> auto, float step)
        {
            points.RemoveAll(p => p.IsAuto);
            points.AddRange(auto);
            placedStep = step;
            foreach (var p in points) p.ClearEstimate();
            estimatedAt = "";
            tour = new TourMeasure();
        }

        public Viewpoint Find(string id)
        {
            return points.FirstOrDefault(p => p.id == id);
        }
    }

    // CSV of the viewpoints tab: one row per viewpoint with its worst direction (UTF-8 with BOM, RFC 4180).
    public static class ViewpointCsv
    {
        public const string Header = "Point,Kind,X,Y,Z,Yaw,Tris,DrawCalls,TransparentPercent,OverBudget";

        public static string ToCsv(ViewpointSet set, VRBudgets budgets)
        {
            var c = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder();
            sb.Append(Header).Append("\r\n");
            foreach (var p in set.points)
            {
                var w = p.Worst;
                sb.Append(ReportExporter.Csv(p.Name)).Append(',').Append(p.kind).Append(',')
                  .Append(p.position.x.ToString("0.##", c)).Append(',').Append(p.position.y.ToString("0.##", c)).Append(',')
                  .Append(p.position.z.ToString("0.##", c)).Append(',');
                if (w != null)
                    sb.Append(w.yaw.ToString("0.#", c)).Append(',').Append(w.tris.ToString(c)).Append(',').Append(w.drawCalls.ToString(c)).Append(',')
                      .Append(w.transparentPercent.ToString("0.#", c)).Append(',').Append(ViewpointEstimator.LevelOf(w, budgets) != null ? "true" : "false");
                else sb.Append(",,,,");
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        public static void Save(ViewpointSet set, VRBudgets budgets, string path)
        {
            File.WriteAllText(path, ToCsv(set, budgets), new System.Text.UTF8Encoding(true));
            Debug.Log("[SceneAudit] Viewpoints CSV saved: " + path);
        }
    }

    // Automatic viewpoints from the baked NavMesh, SteamVR teleports, scene cameras or rays down on the level.
    public static class ViewpointPlacer
    {
        public const string TeleportPointType = "Valve.VR.InteractionSystem.TeleportPoint";
        public const string TeleportAreaType = "Valve.VR.InteractionSystem.TeleportArea";

        // What the loaded scenes offer as sources, for the placement panel.
        public sealed class Survey
        {
            public bool navMesh;
            public int teleportPoints;
            public int teleportAreas;
            public int cameras;

            public bool HasTeleports { get { return teleportPoints + teleportAreas > 0; } }
        }

        public static Survey Look()
        {
            var tri = NavMesh.CalculateTriangulation();
            return new Survey
            {
                navMesh = tri.vertices != null && tri.vertices.Length > 0,
                teleportPoints = Components(TeleportPointType).Count,
                teleportAreas = Components(TeleportAreaType).Count,
                cameras = SceneCameras().Count
            };
        }

        // Places automatic viewpoints; the grid step grows until there are at most MaxViewpoints of them.
        public static List<Viewpoint> Place(PlacementSettings settings, out float step)
        {
            step = Mathf.Clamp(settings.step, VRBudgets.GridStepMin, VRBudgets.GridStepMax);
            Physics.SyncTransforms();
            var survey = Look();
            var fixedPoints = new List<Viewpoint>();
            if (settings.teleports)
                foreach (var c in Components(TeleportPointType))
                    fixedPoints.Add(Auto(ViewpointKind.Teleport, c.transform.position + Vector3.up * settings.eyeHeight, c.name));
            if (settings.cameras)
                foreach (var camera in SceneCameras())
                {
                    var p = Auto(ViewpointKind.Camera, camera.transform.position, camera.name);
                    p.hasYaw = true;
                    p.yaw = camera.transform.eulerAngles.y;
                    fixedPoints.Add(p);
                }
            bool rays = settings.rays && !survey.navMesh && !survey.HasTeleports;

            List<Viewpoint> grid;
            while (true)
            {
                grid = new List<Viewpoint>();
                if (settings.navMesh && survey.navMesh) grid.AddRange(NavMeshGrid(step, settings.eyeHeight));
                if (settings.teleports)
                    foreach (var c in Components(TeleportAreaType))
                        grid.AddRange(AreaGrid(c, step, settings.eyeHeight));
                if (rays) grid.AddRange(RayGrid(step, settings.eyeHeight));
                if (grid.Count + fixedPoints.Count <= VRBudgets.MaxViewpoints || step >= VRBudgets.GridStepMax * 10f) break;
                step *= VRBudgets.GridStepGrowth;
            }

            var all = fixedPoints.Concat(Dedupe(grid, step * 0.5f)).Take(VRBudgets.MaxViewpoints).ToList();
            // Numbers follow the position, so a new placement of the same level gives the same names.
            foreach (var group in all.GroupBy(p => p.kind))
            {
                int n = 0;
                foreach (var p in group.OrderBy(p => Mathf.Round(p.position.x)).ThenBy(p => Mathf.Round(p.position.z)).ThenBy(p => p.position.y))
                    p.number = ++n;
            }
            return all;
        }

        static Viewpoint Auto(ViewpointKind kind, Vector3 position, string objectName)
        {
            return new Viewpoint { id = Guid.NewGuid().ToString("N"), kind = kind, position = position, objectName = objectName ?? "" };
        }

        // Grid nodes that fall on NavMesh triangles, one per node and floor.
        static IEnumerable<Viewpoint> NavMeshGrid(float step, float eye)
        {
            var tri = NavMesh.CalculateTriangulation();
            var v = tri.vertices;
            var idx = tri.indices;
            var seen = new HashSet<long>();
            for (int t = 0; t + 2 < idx.Length; t += 3)
            {
                Vector3 a = v[idx[t]], b = v[idx[t + 1]], c = v[idx[t + 2]];
                int x0 = Mathf.CeilToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)) / step), x1 = Mathf.FloorToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) / step);
                int z0 = Mathf.CeilToInt(Mathf.Min(a.z, Mathf.Min(b.z, c.z)) / step), z1 = Mathf.FloorToInt(Mathf.Max(a.z, Mathf.Max(b.z, c.z)) / step);
                for (int gx = x0; gx <= x1; gx++)
                    for (int gz = z0; gz <= z1; gz++)
                    {
                        float y;
                        if (!HeightOn(a, b, c, gx * step, gz * step, out y)) continue;
                        long key = ((long)(gx & 0xFFFFF) << 40) | ((long)(gz & 0xFFFFF) << 20) | (long)(Mathf.RoundToInt(y / 1.5f) & 0xFFFFF);
                        if (!seen.Add(key)) continue;
                        yield return Auto(ViewpointKind.NavMesh, new Vector3(gx * step, y + eye, gz * step), "");
                    }
            }
        }

        // Height of the triangle at (x, z), when the point lies inside it seen from above.
        static bool HeightOn(Vector3 a, Vector3 b, Vector3 c, float x, float z, out float y)
        {
            y = 0f;
            float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(d) < 1e-6f) return false;
            float u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / d;
            float w = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / d;
            float s = 1f - u - w;
            const float eps = -1e-4f;
            if (u < eps || w < eps || s < eps) return false;
            y = u * a.y + w * b.y + s * c.y;
            return true;
        }

        // Grid over a teleport area: a ray down hits its own collider.
        static IEnumerable<Viewpoint> AreaGrid(Component area, float step, float eye)
        {
            var collider = area.GetComponent<Collider>();
            if (collider == null) yield break;
            var b = collider.bounds;
            for (int gx = Mathf.CeilToInt(b.min.x / step); gx <= Mathf.FloorToInt(b.max.x / step); gx++)
                for (int gz = Mathf.CeilToInt(b.min.z / step); gz <= Mathf.FloorToInt(b.max.z / step); gz++)
                {
                    RaycastHit hit;
                    var ray = new Ray(new Vector3(gx * step, b.max.y + 1f, gz * step), Vector3.down);
                    if (collider.Raycast(ray, out hit, b.size.y + 2f))
                        yield return Auto(ViewpointKind.Teleport, hit.point + Vector3.up * eye, area.name);
                }
        }

        // Without a NavMesh and teleports: rays down over the level, surfaces that a player can stand on.
        static IEnumerable<Viewpoint> RayGrid(float step, float eye)
        {
            var renderers = AllActive<MeshRenderer>();
            if (renderers.Count == 0) yield break;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            float minUp = Mathf.Cos(VRBudgets.RayMaxSlope * Mathf.Deg2Rad);
            for (int gx = Mathf.CeilToInt(b.min.x / step); gx <= Mathf.FloorToInt(b.max.x / step); gx++)
                for (int gz = Mathf.CeilToInt(b.min.z / step); gz <= Mathf.FloorToInt(b.max.z / step); gz++)
                {
                    RaycastHit hit;
                    var origin = new Vector3(gx * step, b.max.y + 1f, gz * step);
                    if (!Physics.Raycast(origin, Vector3.down, out hit, b.size.y + 2f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.normal.y < minUp) continue;
                    yield return Auto(ViewpointKind.Ray, hit.point + Vector3.up * eye, "");
                }
        }

        // Grid points from different sources (two teleport areas side by side) closer than the distance merge into one.
        static IEnumerable<Viewpoint> Dedupe(List<Viewpoint> points, float distance)
        {
            var cells = new Dictionary<Vector3Int, List<Vector3>>();
            float size = Mathf.Max(0.01f, distance);
            foreach (var p in points)
            {
                var cell = Vector3Int.FloorToInt(p.position / size);
                bool near = false;
                for (int x = -1; x <= 1 && !near; x++)
                    for (int y = -1; y <= 1 && !near; y++)
                        for (int z = -1; z <= 1 && !near; z++)
                        {
                            List<Vector3> list;
                            if (!cells.TryGetValue(cell + new Vector3Int(x, y, z), out list)) continue;
                            foreach (var q in list)
                                if ((q - p.position).sqrMagnitude < distance * distance) { near = true; break; }
                        }
                if (near) continue;
                List<Vector3> own;
                if (!cells.TryGetValue(cell, out own)) cells[cell] = own = new List<Vector3>();
                own.Add(p.position);
                yield return p;
            }
        }

        // Enabled cameras of the scenes except the one the player looks through and cameras that render to a texture.
        public static List<Camera> SceneCameras()
        {
            var vr = VrCamera();
            return AllActive<Camera>().Where(c => c != vr && c.targetTexture == null).ToList();
        }

        // The camera the player looks through: a stereo camera that renders to the screen, MainCamera first.
        public static Camera VrCamera()
        {
            var stereo = AllActive<Camera>().Where(c => c.stereoTargetEye != StereoTargetEyeMask.None && c.targetTexture == null).ToList();
            return stereo.FirstOrDefault(c => c.CompareTag("MainCamera")) ?? stereo.FirstOrDefault();
        }

        public static List<T> AllActive<T>() where T : Component
        {
            var list = new List<T>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var c in root.GetComponentsInChildren<T>(true))
                        if (AuditContext.IsActive(c)) list.Add(c);
            }
            return list;
        }

        // Components of a type found by name (SteamVR Plugin is optional), active ones only.
        static List<Component> Components(string typeName)
        {
            var type = ReflectionUtil.FindType(typeName);
            var list = new List<Component>();
            if (type == null) return list;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var c in root.GetComponentsInChildren(type, true))
                        if (AuditContext.IsActive(c)) list.Add(c);
            }
            return list;
        }
    }
}
