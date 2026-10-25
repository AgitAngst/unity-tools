using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityTools.SceneOptimization
{
    // A reflection probe of the loaded scenes.
    public sealed class ReflectionRow
    {
        public ReflectionProbe probe;
        public ReflectionProbeMode mode;
        public int resolution;
        public bool hdr;
        // The cubemap with its mips: the baked one as imported, else estimated from the resolution (estimated).
        public double bytes;
        public bool estimated;
        // A Baked or Custom probe without its cubemap.
        public bool notBaked;
        public bool box;
        // The box of the probe in the world, as Unity uses it (axis-aligned).
        public Bounds bounds;
        // Room-sized (up to RoomProbeMeters on its largest side) without box projection.
        public bool noBox;
        // Nearly the same box as another probe (each shares more than ReflectionDuplicateShare of its volume with the
        // other): that probe and the shared part of this one's volume. A small probe nested in a large one is no duplicate.
        public ReflectionProbe duplicateOf;
        public float overlap;
        // Renderers that use reflection probes with the centre of their bounds inside.
        public int objects;

        public bool Duplicate { get { return duplicateOf != null; } }
        public bool Large { get { return resolution > VRBudgets.ReflectionResolutionWarning; } }
        public bool Marked { get { return notBaked || noBox || Duplicate || Large; } }
        public double MB { get { return bytes / (1024.0 * 1024.0); } }
    }

    // A renderer that uses reflection probes but stands in none: it reflects the sky.
    public sealed class OutsideReflection
    {
        public Renderer renderer;
        // To the nearest probe box, metres.
        public float distance;
    }

    public sealed partial class LightReport
    {
        public readonly List<ReflectionRow> reflections = new List<ReflectionRow>();
        public readonly List<OutsideReflection> outsideReflection = new List<OutsideReflection>();

        public double ReflectionMB { get { return reflections.Sum(r => r.bytes) / (1024.0 * 1024.0); } }

        void CollectReflections(List<GameObject> roots, List<Renderer> renderers)
        {
            foreach (var root in roots)
                foreach (var probe in root.GetComponentsInChildren<ReflectionProbe>(true))
                    if (AuditContext.IsActive(probe)) reflections.Add(ReadProbe(probe));

            // Of two nearly equal probes the smaller one (the later one when equal) is the duplicate of the other.
            for (int i = 0; i < reflections.Count; i++)
            {
                for (int j = i + 1; j < reflections.Count; j++)
                {
                    var a = reflections[i];
                    var b = reflections[j];
                    var small = Volume(b.bounds) <= Volume(a.bounds) ? b : a;
                    var large = small == b ? a : b;
                    float smallVolume = Volume(small.bounds);
                    if (small.Duplicate || smallVolume <= 0f) continue;
                    float shared = Intersection(a.bounds, b.bounds);
                    if (shared / Volume(large.bounds) <= VRBudgets.ReflectionDuplicateShare) continue;
                    small.duplicateOf = large.probe;
                    small.overlap = shared / smallVolume;
                }
            }

            if (reflections.Count > 0)
            {
                foreach (var r in renderers)
                {
                    if (r.reflectionProbeUsage == ReflectionProbeUsage.Off) continue;
                    var centre = r.bounds.center;
                    bool inside = false;
                    float best = float.MaxValue;
                    foreach (var row in reflections)
                    {
                        if (row.bounds.Contains(centre))
                        {
                            row.objects++;
                            inside = true;
                        }
                        else best = Mathf.Min(best, row.bounds.SqrDistance(centre));
                    }
                    if (!inside) outsideReflection.Add(new OutsideReflection { renderer = r, distance = Mathf.Sqrt(best) });
                }
            }
            // Nearest first: an object just outside a room's probe is the likely mistake; far ones stand outdoors.
            outsideReflection.Sort((a, b) => a.distance.CompareTo(b.distance));
            reflections.Sort((a, b) => a.Marked != b.Marked ? (a.Marked ? -1 : 1)
                : a.bytes != b.bytes ? b.bytes.CompareTo(a.bytes) : string.CompareOrdinal(a.probe.name, b.probe.name));
        }

        static ReflectionRow ReadProbe(ReflectionProbe probe)
        {
            var row = new ReflectionRow
            {
                probe = probe, mode = probe.mode, resolution = probe.resolution, hdr = probe.hdr, box = probe.boxProjection, bounds = probe.bounds
            };
            var cube = probe.mode == ReflectionProbeMode.Baked ? probe.bakedTexture : probe.mode == ReflectionProbeMode.Custom ? probe.customBakedTexture : null;
            if (cube != null) row.bytes = TextureMemory.EstimateBytes(cube, false);
            else
            {
                row.notBaked = probe.mode != ReflectionProbeMode.Realtime;
                row.estimated = true;
                // Realtime: a render texture of half floats or bytes; a bake to come: compressed as Unity imports it
                // (BC6H for HDR, DXT1 otherwise).
                double perPixel = probe.mode == ReflectionProbeMode.Realtime ? (probe.hdr ? 8.0 : 4.0) : (probe.hdr ? 1.0 : 0.5);
                row.bytes = 6.0 * probe.resolution * probe.resolution * perPixel * 4.0 / 3.0;
            }
            var size = row.bounds.size;
            row.noBox = !row.box && Mathf.Max(size.x, Mathf.Max(size.y, size.z)) <= VRBudgets.RoomProbeMeters;
            return row;
        }

        static float Volume(Bounds b)
        {
            var s = b.size;
            return s.x * s.y * s.z;
        }

        static float Intersection(Bounds a, Bounds b)
        {
            var d = Vector3.Min(a.max, b.max) - Vector3.Max(a.min, b.min);
            return d.x <= 0f || d.y <= 0f || d.z <= 0f ? 0f : d.x * d.y * d.z;
        }

        // Fix entries for the confirmation window: box projection on room-sized probes.
        public static List<AuditIssue> BoxFixes(IEnumerable<ReflectionRow> rows)
        {
            var fixes = new List<AuditIssue>();
            foreach (var row in rows)
            {
                var probe = row.probe;
                if (probe == null || probe.boxProjection) continue;
                var size = row.bounds.size;
                var issue = new AuditIssue
                {
                    checkId = "REFL_NO_BOX",
                    messageKey = "REFL_NO_BOX",
                    category = Category.Lighting,
                    severity = Severity.Info,
                    target = probe,
                    scene = probe.gameObject.scene.name,
                    objectPath = AuditContext.PathOf(probe.transform),
                    args = new object[] { System.Math.Round(Mathf.Max(size.x, Mathf.Max(size.y, size.z)), 1) }
                };
                issue.SetFix(FixKind.Review, probe, false, () => probe.boxProjection = true);
                fixes.Add(issue);
            }
            return fixes;
        }
    }
}
