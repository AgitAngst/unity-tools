using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityTools.SceneOptimization
{
    // A group of what one view sees, by source prefab or model: "What is heavy here".
    public sealed class HeavyEntry
    {
        public string key = "";
        public string label = "";
        public int copies;
        public long tris;
        public int drawCalls;
        public float transparentPercent;
        // Keys of the marks: "far", "nolod", "shadow", "transparent".
        public readonly List<string> marks = new List<string>();
        public readonly List<GameObject> objects = new List<GameObject>();
    }

    // Upper estimate of what the player sees from a viewpoint: renderers in the view frustum at the LOD level Unity would
    // pick, their triangles and draw calls (with extra pixel light and shadow passes) and the share of the screen under
    // transparent objects. No batching, no occlusion culling, no shader cost.
    public static class ViewpointEstimator
    {
        sealed class Item
        {
            public Renderer renderer;
            public Transform root;
            public Bounds bounds;
            public long tris;
            public int slots;
            public bool casts;
            public bool transparent;
            public int layer;
            public int group = -1;
            // LOD levels of the group the renderer belongs to, a bit per level.
            public int lodMask;
            public int pixelLights;
            public string sourceKey;
            public string sourceLabel;
        }

        sealed class Group
        {
            public Vector3 reference;
            public float size;
            public float[] transitions;
        }

        sealed class ShadowLight
        {
            public Bounds range;
            public int passes;
            public int casterSlots;
        }

        // Data of the loaded scenes collected once and shared by every viewpoint of an estimate.
        sealed class ViewCache
        {
            public readonly List<Item> items = new List<Item>();
            public readonly List<Group> groups = new List<Group>();
            public readonly List<ShadowLight> localShadows = new List<ShadowLight>();
            public int directionalShadowLights;
            public int cascades = 1;
            public float shadowDistance;
            public bool doublesDrawCalls;
            public int cullingMask = ~0;
            public float[] layerCull;
            public float far = VRBudgets.ViewFar;
            public float lodBias = 1f;
            public int maximumLodLevel;
        }

        // Estimates the viewpoints. The callback gets the viewpoint name and the share done and returns true to stop;
        // then the set keeps its previous estimate and false comes back.
        public static bool Estimate(ViewpointSet set, IList<Viewpoint> which, bool walls, Func<string, float, bool> cancelled)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var cache = Collect();
            if (walls) Physics.SyncTransforms();
            var results = new List<KeyValuePair<Viewpoint, List<ViewDirection>>>();
            for (int i = 0; i < which.Count; i++)
            {
                if (cancelled != null && cancelled(which[i].Name, (float)i / Math.Max(1, which.Count))) return false;
                results.Add(new KeyValuePair<Viewpoint, List<ViewDirection>>(which[i], EstimatePoint(cache, which[i], walls)));
            }
            foreach (var pair in results)
            {
                pair.Key.directions = pair.Value;
                pair.Key.worst = WorstOf(pair.Value);
            }
            set.estimatedAt = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);
            set.estimateVersion = ToolVersion.Full;
            set.estimateSeconds = (float)Math.Round(watch.Elapsed.TotalSeconds, 1);
            set.estimateWalls = walls;
            Debug.Log("[SceneAudit] Viewpoints: " + which.Count + " estimated, " + set.estimateSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s.");
            return true;
        }

        // Worst direction: most draw calls, then most triangles.
        public static int WorstOf(List<ViewDirection> directions)
        {
            int worst = -1;
            for (int i = 0; i < directions.Count; i++)
            {
                if (worst < 0) { worst = i; continue; }
                var d = directions[i];
                var w = directions[worst];
                if (d.drawCalls > w.drawCalls || (d.drawCalls == w.drawCalls && d.tris > w.tris)) worst = i;
            }
            return worst;
        }

        // Worst severity of a direction against the frame thresholds; null within the budgets.
        public static Severity? LevelOf(ViewDirection d, VRBudgets b)
        {
            if (d == null) return null;
            if (d.tris > b.sceneTrisCritical || d.drawCalls > b.drawCallsCritical || d.transparentPercent > b.transparentCriticalPercent) return Severity.Critical;
            if (d.tris > b.sceneTrisWarning || d.drawCalls > b.drawCallsWarning || d.transparentPercent > b.transparentWarningPercent) return Severity.Warning;
            return null;
        }

        // Directions of a viewpoint: every 45 degrees, plus its own one for manual viewpoints and cameras.
        public static List<float> Yaws(Viewpoint p)
        {
            var yaws = new List<float>();
            for (int i = 0; i < VRBudgets.ViewDirections; i++) yaws.Add(i * 360f / VRBudgets.ViewDirections);
            if (p.hasYaw)
            {
                float own = Mathf.Repeat(p.yaw, 360f);
                if (!yaws.Any(y => Mathf.Abs(Mathf.DeltaAngle(y, own)) < 0.5f)) yaws.Add(own);
            }
            return yaws;
        }

        static List<ViewDirection> EstimatePoint(ViewCache c, Viewpoint p, bool walls)
        {
            var eye = p.position;
            var levels = LevelsFor(c, eye);
            var hidden = walls ? WallTest(c, eye) : null;
            int directionalSlots = DirectionalCasterSlots(c, eye);
            return Yaws(p).Select(yaw => Evaluate(c, eye, yaw, levels, hidden, directionalSlots, null)).ToList();
        }

        // ---- one view ----

        static ViewDirection Evaluate(ViewCache c, Vector3 eye, float yaw, int[] levels, Func<int, bool> hidden, int directionalSlots, List<int> seen)
        {
            var viewProj = ViewProjection(eye, yaw, c.far);
            var planes = GeometryUtility.CalculateFrustumPlanes(viewProj);
            long tris = 0;
            long calls = 0;
            double cover = 0;
            for (int i = 0; i < c.items.Count; i++)
            {
                var it = c.items[i];
                if (!Visible(c, it, eye, levels, planes)) continue;
                if (hidden != null && hidden(i)) continue;
                tris += it.tris;
                calls += it.slots * (1 + it.pixelLights);
                if (it.transparent) cover += Coverage(viewProj, eye, it.bounds);
                if (seen != null) seen.Add(i);
            }
            calls += (long)directionalSlots * c.cascades * c.directionalShadowLights;
            foreach (var light in c.localShadows)
                if (GeometryUtility.TestPlanesAABB(planes, light.range)) calls += (long)light.casterSlots * light.passes;
            if (c.doublesDrawCalls) calls *= 2;
            return new ViewDirection
            {
                yaw = yaw,
                tris = tris,
                drawCalls = (int)Math.Min(int.MaxValue, calls),
                transparentPercent = (float)Math.Round(cover * 100.0, 1)
            };
        }

        static bool Visible(ViewCache c, Item it, Vector3 eye, int[] levels, Plane[] planes)
        {
            if (((1 << it.layer) & c.cullingMask) == 0) return false;
            if (c.layerCull != null && it.layer < c.layerCull.Length && c.layerCull[it.layer] > 0f &&
                it.bounds.SqrDistance(eye) > c.layerCull[it.layer] * c.layerCull[it.layer]) return false;
            if (it.group >= 0)
            {
                int level = levels[it.group];
                if (level < 0 || (it.lodMask & (1 << level)) == 0) return false;
            }
            return GeometryUtility.TestPlanesAABB(planes, it.bounds);
        }

        // Camera at the eye looking along the yaw with zero pitch; one frustum for both eyes.
        public static Matrix4x4 ViewProjection(Vector3 eye, float yaw, float far)
        {
            var view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * Matrix4x4.TRS(eye, Quaternion.Euler(0f, yaw, 0f), Vector3.one).inverse;
            var projection = Matrix4x4.Perspective(VRBudgets.ViewFieldOfView, VRBudgets.ViewAspect, VRBudgets.ViewNear, far);
            return projection * view;
        }

        // Share of the screen the bounds cover, 0 to 1. Bounds that reach behind the eye count as the whole screen.
        static double Coverage(Matrix4x4 viewProj, Vector3 eye, Bounds b)
        {
            if (b.Contains(eye)) return 1;
            float minX = 1f, maxX = -1f, minY = 1f, maxY = -1f;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                var v = viewProj * new Vector4(corner.x, corner.y, corner.z, 1f);
                if (v.w <= VRBudgets.ViewNear) return 1;
                float x = v.x / v.w, y = v.y / v.w;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
            minX = Mathf.Max(-1f, minX);
            maxX = Mathf.Min(1f, maxX);
            minY = Mathf.Max(-1f, minY);
            maxY = Mathf.Min(1f, maxY);
            if (maxX <= minX || maxY <= minY) return 0;
            return (maxX - minX) * (maxY - minY) / 4.0;
        }

        // ---- LOD ----

        // Screen height share of an object of the given world size at the distance, as Unity measures it for LOD.
        public static float RelativeHeight(float worldSize, float distance, float fieldOfView)
        {
            if (distance <= 0.0001f) return float.MaxValue;
            return worldSize / (2f * distance * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad));
        }

        // Level Unity would draw: the first whose transition height the (biased) screen height reaches, never above the
        // maximum LOD level; -1 below the last transition, the group is culled then.
        public static int LodLevel(float relativeHeight, float[] transitions, float lodBias, int maximumLevel)
        {
            float height = relativeHeight * lodBias;
            for (int i = 0; i < transitions.Length; i++)
                if (height >= transitions[i]) return Mathf.Min(Mathf.Max(i, maximumLevel), transitions.Length - 1);
            return -1;
        }

        static int[] LevelsFor(ViewCache c, Vector3 eye)
        {
            var levels = new int[c.groups.Count];
            for (int g = 0; g < c.groups.Count; g++)
            {
                var group = c.groups[g];
                float height = RelativeHeight(group.size, Vector3.Distance(eye, group.reference), VRBudgets.ViewFieldOfView);
                levels[g] = LodLevel(height, group.transitions, c.lodBias, c.maximumLodLevel);
            }
            return levels;
        }

        // ---- shadows and walls ----

        // Material slots of shadow casters within the shadow distance: each is drawn once per cascade of every
        // directional light with shadows, wherever the player looks.
        static int DirectionalCasterSlots(ViewCache c, Vector3 eye)
        {
            if (c.directionalShadowLights == 0) return 0;
            float max = c.shadowDistance * c.shadowDistance;
            int slots = 0;
            foreach (var it in c.items)
                if (it.casts && it.bounds.SqrDistance(eye) <= max) slots += it.slots;
            return slots;
        }

        // An object is behind a wall when rays from the eye to its centre and top corners all hit colliders of other
        // objects first. Without colliders on walls nothing is hidden.
        static Func<int, bool> WallTest(ViewCache c, Vector3 eye)
        {
            var known = new Dictionary<int, bool>();
            var hits = new RaycastHit[16];
            return i =>
            {
                bool hidden;
                if (known.TryGetValue(i, out hidden)) return hidden;
                hidden = Behind(c.items[i], eye, hits);
                known[i] = hidden;
                return hidden;
            };
        }

        static bool Behind(Item it, Vector3 eye, RaycastHit[] hits)
        {
            var b = it.bounds;
            var targets = new[]
            {
                b.center,
                new Vector3(b.min.x, b.max.y, b.min.z), new Vector3(b.max.x, b.max.y, b.min.z),
                new Vector3(b.min.x, b.max.y, b.max.z), new Vector3(b.max.x, b.max.y, b.max.z)
            };
            foreach (var target in targets)
            {
                var direction = target - eye;
                float distance = direction.magnitude;
                if (distance < 0.05f) return false;
                int n = Physics.RaycastNonAlloc(eye, direction / distance, hits, distance - 0.02f, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int k = 0; k < n && !blocked; k++)
                    if (!hits[k].collider.transform.IsChildOf(it.root)) blocked = true;
                if (!blocked) return false;
            }
            return true;
        }

        // ---- what is heavy here ----

        // What the direction of the viewpoint sees, grouped by source prefab or model, heaviest first.
        public static List<HeavyEntry> Heavy(Viewpoint p, int directionIndex, VRBudgets budgets, bool walls, bool byDrawCalls)
        {
            var list = new List<HeavyEntry>();
            if (!p.Estimated || directionIndex < 0 || directionIndex >= p.directions.Count) return list;
            var c = Collect();
            if (walls) Physics.SyncTransforms();
            var eye = p.position;
            var levels = LevelsFor(c, eye);
            var seen = new List<int>();
            var yaw = p.directions[directionIndex].yaw;
            Evaluate(c, eye, yaw, levels, walls ? WallTest(c, eye) : null, 0, seen);
            var viewProj = ViewProjection(eye, yaw, c.far);
            var byKey = new Dictionary<string, HeavyEntry>();
            foreach (var i in seen)
            {
                var it = c.items[i];
                HeavyEntry entry;
                if (!byKey.TryGetValue(it.sourceKey, out entry))
                {
                    entry = new HeavyEntry { key = it.sourceKey, label = it.sourceLabel };
                    byKey[it.sourceKey] = entry;
                    list.Add(entry);
                }
                if (!entry.objects.Contains(it.renderer.gameObject))
                {
                    entry.objects.Add(it.renderer.gameObject);
                    entry.copies++;
                }
                entry.tris += it.tris;
                entry.drawCalls += it.slots * (1 + it.pixelLights);
                double share = it.transparent ? Coverage(viewProj, eye, it.bounds) : 0;
                entry.transparentPercent += (float)Math.Round(share * 100.0, 1);
                float size = Mathf.Max(it.bounds.size.x, Mathf.Max(it.bounds.size.y, it.bounds.size.z));
                bool lod0 = it.group < 0 || levels[it.group] == 0;
                if (lod0 && it.tris > VRBudgets.FarLod0MinTris &&
                    RelativeHeight(size, Vector3.Distance(eye, it.bounds.center), VRBudgets.ViewFieldOfView) < VRBudgets.FarLod0ScreenShare)
                    Mark(entry, "far");
                if (it.group < 0 && it.tris > budgets.objectTrisWarning) Mark(entry, "nolod");
                if (it.casts && size < VRBudgets.SmallShadowMeters) Mark(entry, "shadow");
                if (it.transparent && share > VRBudgets.LargeTransparentShare) Mark(entry, "transparent");
            }
            return (byDrawCalls
                    ? list.OrderByDescending(e => e.drawCalls).ThenByDescending(e => e.tris)
                    : list.OrderByDescending(e => e.tris).ThenByDescending(e => e.drawCalls))
                .Take(VRBudgets.HeavyTop).ToList();
        }

        static void Mark(HeavyEntry entry, string mark)
        {
            if (!entry.marks.Contains(mark)) entry.marks.Add(mark);
        }

        // ---- data of the scenes ----

        static ViewCache Collect()
        {
            var c = new ViewCache();
            var quality = QualitySnapshot.Capture();
            c.lodBias = quality.lodBias;
            c.maximumLodLevel = quality.maximumLODLevel;
            c.shadowDistance = quality.shadowDistance;
            c.cascades = Mathf.Max(1, quality.shadowCascades);
            c.doublesDrawCalls = XrInfo.Detect().DoublesDrawCalls;
            var vr = ViewpointPlacer.VrCamera();
            if (vr != null)
            {
                c.far = vr.farClipPlane;
                c.cullingMask = vr.cullingMask;
                c.layerCull = vr.layerCullDistances;
            }

            var index = new Dictionary<Renderer, Item>();
            foreach (var r in ViewpointPlacer.AllActive<Renderer>())
            {
                int slots = SceneAuditor.SlotCount(r);
                if (slots == 0) continue;
                var mesh = AuditContext.MeshOf(r);
                var item = new Item
                {
                    renderer = r,
                    root = r.transform,
                    bounds = r.bounds,
                    tris = AuditContext.Tris(mesh),
                    slots = slots,
                    casts = r.shadowCastingMode != ShadowCastingMode.Off,
                    transparent = r is ParticleSystemRenderer || r.sharedMaterials.Any(m => m != null && m.renderQueue >= VRBudgets.TransparentQueue),
                    layer = r.gameObject.layer
                };
                Source(r, mesh, out item.sourceKey, out item.sourceLabel);
                index[r] = item;
                c.items.Add(item);
            }

            foreach (var lodGroup in ViewpointPlacer.AllActive<LODGroup>())
            {
                var lods = lodGroup.GetLODs();
                if (lods.Length == 0) continue;
                var scale = lodGroup.transform.lossyScale;
                var group = new Group
                {
                    reference = lodGroup.transform.TransformPoint(lodGroup.localReferencePoint),
                    size = lodGroup.size * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z))),
                    transitions = lods.Select(l => l.screenRelativeTransitionHeight).ToArray()
                };
                int g = c.groups.Count;
                c.groups.Add(group);
                for (int level = 0; level < lods.Length && level < 31; level++)
                    foreach (var r in lods[level].renderers)
                    {
                        Item item;
                        if (r == null || !index.TryGetValue(r, out item)) continue;
                        item.group = g;
                        item.lodMask |= 1 << level;
                    }
            }

            var pixelLights = new List<Light>();
            bool shadowsOn = quality.shadows != ShadowQuality.Disable;
            foreach (var light in ViewpointPlacer.AllActive<Light>())
            {
                if (light.lightmapBakeType == LightmapBakeType.Baked) continue;
                bool local = light.type == LightType.Point || light.type == LightType.Spot;
                if (local && light.renderMode != LightRenderMode.ForceVertex) pixelLights.Add(light);
                if (!shadowsOn || light.shadows == LightShadows.None) continue;
                if (light.type == LightType.Directional) c.directionalShadowLights++;
                else if (local)
                {
                    var range = new Bounds(light.transform.position, Vector3.one * light.range * 2f);
                    var shadow = new ShadowLight { range = range, passes = light.type == LightType.Point ? 6 : 1 };
                    foreach (var it in c.items)
                        if (it.casts && it.bounds.SqrDistance(light.transform.position) <= light.range * light.range) shadow.casterSlots += it.slots;
                    c.localShadows.Add(shadow);
                }
            }
            if (pixelLights.Count > 0)
                foreach (var it in c.items)
                {
                    int n = 0;
                    foreach (var light in pixelLights)
                        if (it.bounds.SqrDistance(light.transform.position) <= light.range * light.range) n++;
                    it.pixelLights = Mathf.Min(n, quality.pixelLightCount);
                }
            return c;
        }

        // Source of a renderer for grouping: its prefab or model asset, else its mesh asset, else the object name.
        static void Source(Renderer r, Mesh mesh, out string key, out string label)
        {
            var root = PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
            if (root != null)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(root);
                var path = source != null ? AssetDatabase.GetAssetPath(source) : "";
                if (!string.IsNullOrEmpty(path))
                {
                    key = path;
                    label = Path.GetFileName(path);
                    return;
                }
            }
            var meshPath = mesh != null ? AssetDatabase.GetAssetPath(mesh) : "";
            if (AuditContext.IsProjectAssetPath(meshPath))
            {
                key = meshPath + "|" + (mesh != null ? mesh.name : "");
                label = AssetDatabase.IsMainAsset(mesh) ? Path.GetFileName(meshPath) : Path.GetFileName(meshPath) + " · " + mesh.name;
                return;
            }
            key = "object|" + r.gameObject.name;
            label = r.gameObject.name;
        }
    }
}
