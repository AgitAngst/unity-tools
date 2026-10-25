using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityTools.SceneOptimization
{
    // One lightmap of the loaded scenes.
    public sealed class LightAtlas
    {
        public int index;
        public int width;
        public int height;
        public string format = "";
        public bool hasDirection;
        public bool hasShadowmask;
        // Color, direction and shadowmask together, estimated like the textures on the Budgets tab.
        public double bytes;
        public double directionBytes;
        public int objects;
        // Sum of the shares of its objects: how full the atlas is.
        public float filled;
    }

    // A renderer baked into lightmaps.
    public sealed class LightObject
    {
        public MeshRenderer renderer;
        // -1 when the renderer has no lightmap.
        public int atlas = -1;
        // Part of the atlas its lightmap UVs take: lightmapScaleOffset x × y.
        public float share;
        public float scale = 1f;
        // Largest side of the bounds, metres.
        public float size;
        public bool small;
        public bool much;
        public bool noLightmap;

        public bool Marked { get { return small || much || noLightmap; } }
    }

    public sealed class LightSource
    {
        public Light light;
        // Renderers whose bounds meet the range sphere (all of them for a directional light).
        public int inRange;
        // Point or spot with Render Mode Important, not baked: per-pixel regardless of the Pixel Light Count.
        public bool important;
        // Point or spot, not baked, reaching more than WideLightRenderers renderers.
        public bool wide;
        // Baked and Mixed lights with Baked GI on: false while the bake is missing. Otherwise always true.
        public bool baked = true;

        public bool Marked { get { return wide || important || !baked; } }
    }

    public sealed class ProbeGroupRow
    {
        public LightProbeGroup group;
        public int probes;
        public Vector3 size;
    }

    // A renderer lit by light probes whose bounds centre lies outside the probe area.
    public sealed class OutsideProbes
    {
        public Renderer renderer;
        public float distance;
    }

    // Lightmaps, the objects in them, the lights, the light probes and the reflection probes of the loaded scenes. Built by the Lighting tab
    // and by the lighting checks, so that both show the same numbers.
    public sealed partial class LightReport
    {
        public readonly List<LightAtlas> atlases = new List<LightAtlas>();
        public readonly List<LightObject> objects = new List<LightObject>();
        public readonly List<LightSource> sources = new List<LightSource>();
        public readonly List<ProbeGroupRow> groups = new List<ProbeGroupRow>();
        public readonly List<OutsideProbes> outside = new List<OutsideProbes>();
        // Renderers lit by light probes: moving ones and those with Receive GI = Light Probes.
        public int probeObjects;
        public int probes;

        // Bake settings of the scene (2019.4: LightmapEditorSettings) and of the quality level.
        public bool bakedGI;
        public bool directional;
        public bool compressed;
        public int atlasSize;
        public float resolution;
        public string lightmapper = "";
        public MixedLightingMode mixedMode;
        public ShadowmaskMode shadowmaskMode;
        public int pixelLightCount;

        public double Bytes { get { return atlases.Sum(a => a.bytes); } }
        public double MB { get { return Bytes / (1024.0 * 1024.0); } }
        public double DirectionMB { get { return atlases.Sum(a => a.directionBytes) / (1024.0 * 1024.0); } }
        // Lightmaps with direction textures, as baked (the scene setting may have changed since).
        public bool HasDirectionTextures { get { return atlases.Any(a => a.hasDirection); } }
        public int NoLightmap { get { return objects.Count(o => o.noLightmap); } }
        public int NotBakedLights { get { return sources.Count(s => !s.baked); } }
        public bool HasMixedLights { get { return sources.Any(s => s.light.lightmapBakeType == LightmapBakeType.Mixed); } }

        // Baked lighting is used: there are lightmaps, or Baked and Mixed lights.
        public bool UsesBakedLighting
        {
            get { return atlases.Count > 0 || sources.Any(s => s.light.lightmapBakeType != LightmapBakeType.Realtime); }
        }

        public static LightReport Build()
        {
            var roots = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) roots.AddRange(scene.GetRootGameObjects());
            }
            // The quality level the checks read: the Standalone default, not the one picked in the editor.
            var q = QualitySnapshot.Capture();
            return Build(roots, q.pixelLightCount, q.shadowmaskMode);
        }

        public static LightReport Build(List<GameObject> roots, int pixelLightCount, ShadowmaskMode shadowmaskMode)
        {
            var report = new LightReport { pixelLightCount = pixelLightCount, shadowmaskMode = shadowmaskMode };
            report.ReadSettings();
            var renderers = new List<Renderer>();
            foreach (var root in roots)
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    if ((r is MeshRenderer || r is SkinnedMeshRenderer) && AuditContext.IsActive(r)) renderers.Add(r);
            report.CollectAtlases();
            report.CollectObjects(renderers);
            report.CollectSources(roots, renderers);
            report.CollectProbes(roots, renderers);
            report.CollectReflections(roots, renderers);
            return report;
        }

        void ReadSettings()
        {
#pragma warning disable 0618
            bakedGI = Lightmapping.bakedGI;
#pragma warning restore 0618
            directional = LightmapSettings.lightmapsMode == LightmapsMode.CombinedDirectional;
            compressed = LightmapEditorSettings.textureCompression;
            atlasSize = LightmapEditorSettings.maxAtlasSize;
            resolution = LightmapEditorSettings.bakeResolution;
            lightmapper = LightmapperName(LightmapEditorSettings.lightmapper);
            mixedMode = LightmapEditorSettings.mixedBakeMode;
        }

        // As the Lighting window names them; ToString gives the obsolete "PathTracer" for Progressive CPU.
        static string LightmapperName(LightmapEditorSettings.Lightmapper lightmapper)
        {
            switch ((int)lightmapper)
            {
                case 0: return "Enlighten";
                case 1: return "Progressive CPU";
                case 2: return "Progressive GPU";
                default: return Spaced(lightmapper.ToString());
            }
        }

        // "ProgressiveGPU" → "Progressive GPU".
        public static string Spaced(string name)
        {
            return Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
        }

        void CollectAtlases()
        {
            var maps = LightmapSettings.lightmaps ?? new LightmapData[0];
            for (int i = 0; i < maps.Length; i++)
            {
                var map = maps[i];
                var color = map != null ? map.lightmapColor : null;
                var atlas = new LightAtlas { index = i };
                if (color != null)
                {
                    atlas.width = color.width;
                    atlas.height = color.height;
                    atlas.format = color.format.ToString();
                    atlas.bytes += TextureMemory.EstimateBytes(color, false);
                }
                if (map != null && map.lightmapDir != null)
                {
                    atlas.hasDirection = true;
                    atlas.directionBytes = TextureMemory.EstimateBytes(map.lightmapDir, false);
                    atlas.bytes += atlas.directionBytes;
                }
                if (map != null && map.shadowMask != null)
                {
                    atlas.hasShadowmask = true;
                    atlas.bytes += TextureMemory.EstimateBytes(map.shadowMask, false);
                }
                atlases.Add(atlas);
            }
        }

        // Objects baked into lightmaps: Contribute GI with Receive GI = Lightmaps while Baked GI is on.
        public static bool InLightmaps(Renderer r)
        {
            var mesh = r as MeshRenderer;
            if (mesh == null || mesh.receiveGI != ReceiveGI.Lightmaps) return false;
            return (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0;
        }

        void CollectObjects(List<Renderer> renderers)
        {
            if (!bakedGI) return;
            foreach (var r in renderers)
            {
                if (!InLightmaps(r) || AuditContext.MeshOf(r) == null) continue;
                var o = new LightObject { renderer = (MeshRenderer)r };
                int index = r.lightmapIndex;
                if (index >= 0 && index < atlases.Count)
                {
                    o.atlas = index;
                    var so = r.lightmapScaleOffset;
                    o.share = Mathf.Abs(so.x * so.y);
                    atlases[index].objects++;
                    atlases[index].filled += o.share;
                }
                else o.noLightmap = true;
                var scale = new SerializedObject(r).FindProperty("m_ScaleInLightmap");
                if (scale != null) o.scale = scale.floatValue;
                var size = r.bounds.size;
                o.size = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                o.small = o.size < VRBudgets.SmallLightmapMeters;
                o.much = o.share > VRBudgets.LightmapShareWarning;
                objects.Add(o);
            }
            objects.Sort((a, b) => b.share.CompareTo(a.share));
        }

        void CollectSources(List<GameObject> roots, List<Renderer> renderers)
        {
            foreach (var root in roots)
            {
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    if (!AuditContext.IsActive(light)) continue;
                    var s = new LightSource { light = light };
                    bool pointOrSpot = light.type == LightType.Point || light.type == LightType.Spot;
                    var mode = light.lightmapBakeType;
                    s.inRange = InRange(light, renderers);
                    s.important = pointOrSpot && mode != LightmapBakeType.Baked && light.renderMode == LightRenderMode.ForcePixel;
                    s.wide = pointOrSpot && mode != LightmapBakeType.Baked && s.inRange > VRBudgets.WideLightRenderers;
                    // Without Baked GI nothing is baked, and the lightmapped objects are not listed either.
                    if (mode != LightmapBakeType.Realtime && bakedGI) s.baked = light.bakingOutput.isBaked;
                    sources.Add(s);
                }
            }
            // Marked first, then by reach.
            sources.Sort((a, b) => a.Marked != b.Marked ? (a.Marked ? -1 : 1) : b.inRange.CompareTo(a.inRange));
        }

        // Renderers in the light's culling mask whose bounds meet its range sphere; a spot uses the same sphere.
        public static int InRange(Light light, List<Renderer> renderers)
        {
            int mask = light.cullingMask;
            bool all = light.type == LightType.Directional;
            if (!all && light.type != LightType.Point && light.type != LightType.Spot) return 0;
            var position = light.transform.position;
            float range2 = light.range * light.range;
            int count = 0;
            foreach (var r in renderers)
            {
                if ((mask & (1 << r.gameObject.layer)) == 0) continue;
                if (all || r.bounds.SqrDistance(position) <= range2) count++;
            }
            return count;
        }

        void CollectProbes(List<GameObject> roots, List<Renderer> renderers)
        {
            var positions = new List<Vector3>();
            foreach (var root in roots)
            {
                foreach (var group in root.GetComponentsInChildren<LightProbeGroup>(true))
                {
                    if (!AuditContext.IsActive(group)) continue;
                    var local = group.probePositions ?? new Vector3[0];
                    var row = new ProbeGroupRow { group = group, probes = local.Length };
                    if (local.Length > 0)
                    {
                        var box = new Bounds(group.transform.TransformPoint(local[0]), Vector3.zero);
                        foreach (var p in local)
                        {
                            var world = group.transform.TransformPoint(p);
                            box.Encapsulate(world);
                            positions.Add(world);
                        }
                        row.size = box.size;
                    }
                    groups.Add(row);
                }
            }
            probes = positions.Count;
            groups.Sort((a, b) => b.probes.CompareTo(a.probes));

            var area = new Bounds();
            if (positions.Count > 0)
            {
                area = new Bounds(positions[0], Vector3.zero);
                foreach (var p in positions) area.Encapsulate(p);
                area.Expand(VRBudgets.ProbeAreaMargin * 2f);
            }
            foreach (var r in renderers)
            {
                if (r.lightProbeUsage != LightProbeUsage.BlendProbes) continue;
                if (bakedGI && InLightmaps(r)) continue;
                probeObjects++;
                if (positions.Count == 0) continue;
                var centre = r.bounds.center;
                if (area.Contains(centre)) continue;
                float best = float.MaxValue;
                foreach (var p in positions) best = Mathf.Min(best, (p - centre).sqrMagnitude);
                outside.Add(new OutsideProbes { renderer = r, distance = Mathf.Sqrt(best) });
            }
            outside.Sort((a, b) => b.distance.CompareTo(a.distance));
        }

        // Fix entries for the confirmation window: light from probes instead of the lightmap.
        public static List<AuditIssue> ToProbesFixes(IEnumerable<LightObject> rows)
        {
            var fixes = new List<AuditIssue>();
            foreach (var row in rows)
            {
                var r = row.renderer;
                if (r == null || r.receiveGI != ReceiveGI.Lightmaps) continue;
                var issue = new AuditIssue
                {
                    checkId = "LIGHT_SMALL_IN_LIGHTMAP",
                    messageKey = "LIGHT_SMALL_IN_LIGHTMAP",
                    category = Category.Lighting,
                    severity = Severity.Info,
                    target = r.gameObject,
                    scene = r.gameObject.scene.name,
                    objectPath = AuditContext.PathOf(r.transform),
                    args = new object[] { Math.Round(row.size, 2) }
                };
                issue.SetFix(FixKind.Review, r, false, () => r.receiveGI = ReceiveGI.LightProbes);
                fixes.Add(issue);
            }
            return fixes;
        }
    }
}
