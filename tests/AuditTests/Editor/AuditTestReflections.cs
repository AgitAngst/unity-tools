using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Reflection probes: the Reflections part of the Lighting tab and its checks, on probes placed by the test.
    public static partial class AuditTestViewpoints
    {
        const string CubePath = AuditTestAssets.Root + "/Textures/Cube_Probe.asset";

        static readonly string[] ReflectionChecks = { "REFL_RESOLUTION", "REFL_MEMORY", "REFL_NOT_BAKED", "REFL_NO_BOX", "REFL_DUPLICATE", "REFL_OUTSIDE" };

        public static void RunReflections()
        {
            Run("reflections", Reflections);
        }

        static void Reflections()
        {
            ReflectionReport();
            ReflectionIssues();
            ReflectionFix();
            ReflectionQuiet();
            ReflectionStrings();
        }

        // A small cubemap asset stands in for a bake.
        static Cubemap ProbeCube()
        {
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(CubePath);
            if (cube != null) return cube;
            Directory.CreateDirectory(Path.GetDirectoryName(CubePath));
            cube = new Cubemap(32, TextureFormat.RGBA32, true);
            AssetDatabase.CreateAsset(cube, CubePath);
            return cube;
        }

        static ReflectionProbe Probe(string name, Vector3 position, Vector3 size, ReflectionProbeMode mode, int resolution, bool box, bool baked)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = mode;
            probe.size = size;
            probe.resolution = resolution;
            probe.boxProjection = box;
            probe.hdr = mode != ReflectionProbeMode.Realtime;
            if (mode == ReflectionProbeMode.Realtime) probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            if (baked) probe.bakedTexture = ProbeCube();
            return probe;
        }

        static Renderer Shiny(string name, Vector3 position, ReflectionProbeUsage usage = ReflectionProbeUsage.BlendProbes)
        {
            var r = ShaderObject(name, AuditTestAssets.Plain);
            r.transform.position = position;
            r.transform.localScale = Vector3.one * 0.5f;
            r.reflectionProbeUsage = usage;
            return r;
        }

        // Eight probes: a room without box and its near copy, a kiosk nested in the street, a tavern of 1024 and a hall of 512 not baked, two atriums of
        // 2048 for the memory, a realtime street too large for box; objects in the room, in the tavern and outside.
        // Also used by the screenshot of the Reflections part.
        public static void BuildReflectionsScene()
        {
            NewScene("Reflections");
            Probe("Probe_Room", new Vector3(0f, 2f, 0f), new Vector3(10f, 4f, 8f), ReflectionProbeMode.Baked, 128, false, true);
            Probe("Probe_Room_Copy", new Vector3(0f, 2f, 0f), new Vector3(9.5f, 4f, 7.5f), ReflectionProbeMode.Baked, 128, true, true);
            Probe("Probe_Tavern", new Vector3(40f, 2f, 0f), new Vector3(14f, 5f, 12f), ReflectionProbeMode.Baked, 1024, true, false);
            Probe("Probe_Hall", new Vector3(80f, 2f, 0f), new Vector3(24f, 8f, 18f), ReflectionProbeMode.Baked, 512, true, false);
            Probe("Probe_Atrium_A", new Vector3(0f, 2f, 100f), new Vector3(20f, 10f, 20f), ReflectionProbeMode.Baked, 2048, true, false);
            Probe("Probe_Atrium_B", new Vector3(0f, 2f, 200f), new Vector3(20f, 10f, 20f), ReflectionProbeMode.Baked, 2048, true, false);
            Probe("Probe_Street", new Vector3(0f, 2f, -100f), new Vector3(60f, 6f, 10f), ReflectionProbeMode.Realtime, 128, false, false);
            // A kiosk inside the street: a nested probe, no duplicate.
            Probe("Probe_Kiosk", new Vector3(0f, 2f, -100f), new Vector3(3f, 3f, 3f), ReflectionProbeMode.Baked, 128, true, true);
            for (int i = 0; i < 3; i++) Shiny("Cup_" + i, new Vector3(i - 1f, 1f, 0f));
            Shiny("Lantern", new Vector3(40f, 1f, 0f));
            Shiny("Bucket", new Vector3(0f, 1f, 9f));
            Shiny("Statue", new Vector3(0f, 1f, 50f));
            Shiny("Rock_NoReflection", new Vector3(0f, 1f, 60f), ReflectionProbeUsage.Off);
        }

        static ReflectionRow RowOf(LightReport r, string name)
        {
            return r.reflections.FirstOrDefault(p => p.probe != null && p.probe.name == name);
        }

        static void ReflectionReport()
        {
            BuildReflectionsScene();
            var r = LightReport.Build();
            Check(r.reflections.Count == 8, "eight probes (" + r.reflections.Count + ")");
            var room = RowOf(r, "Probe_Room");
            var copy = RowOf(r, "Probe_Room_Copy");
            var tavern = RowOf(r, "Probe_Tavern");
            var street = RowOf(r, "Probe_Street");
            double cube = TextureMemory.EstimateBytes(ProbeCube(), false);
            Check(room != null && !room.estimated && System.Math.Abs(room.bytes - cube) < 1 && room.noBox && room.objects == 3 && !room.notBaked,
                "the room: baked cubemap as imported, no box, 3 objects inside");
            Check(copy != null && copy.Duplicate && copy.duplicateOf == room.probe && copy.overlap > 0.99f && !room.Duplicate,
                "the smaller of two nearly equal probes is the duplicate, the room is not");
            Check(!RowOf(r, "Probe_Kiosk").Duplicate && !street.Duplicate, "a kiosk nested in the street is no duplicate");
            Check(tavern != null && tavern.notBaked && tavern.estimated && System.Math.Abs(tavern.bytes - 6.0 * 1024 * 1024 * 4.0 / 3.0) < 1 && tavern.objects == 1,
                "the tavern: not baked, HDR estimated at 1 byte a pixel with mips, the lantern inside");
            Check(street != null && !street.noBox && !street.notBaked && System.Math.Abs(street.bytes - 6.0 * 128 * 128 * 4 * 4.0 / 3.0) < 1,
                "the street: realtime, a render texture of 4 bytes a pixel; 60 m is no room");
            Check(r.ReflectionMB > VRBudgets.ReflectionMemoryWarningMB, "the probes take more than 64 MB (" + System.Math.Round(r.ReflectionMB, 1) + ")");
            Check(r.outsideReflection.Count == 2 && r.outsideReflection[0].renderer.name == "Bucket" && r.outsideReflection[1].renderer.name == "Statue" &&
                  System.Math.Abs(r.outsideReflection[0].distance - 5f) < 0.3f,
                "two objects outside, nearest first: the bucket 5 m from the room, then the statue; Reflection Probes Off is not counted");
            Check(r.reflections.TakeWhile(p => p.Marked).Count() == r.reflections.Count(p => p.Marked), "marked probes first");
        }

        static void ReflectionIssues()
        {
            BuildReflectionsScene();
            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            System.Func<string, string, AuditIssue> of = (id, name) =>
                result.issues.FirstOrDefault(i => i.checkId == id && i.target is Component && ((Component)i.target).name == name);
            var tavern = of("REFL_RESOLUTION", "Probe_Tavern");
            var hall = of("REFL_RESOLUTION", "Probe_Hall");
            Check(tavern != null && tavern.severity == Severity.Critical && (int)tavern.args[0] == 1024 && hall != null && hall.severity == Severity.Warning &&
                  of("REFL_RESOLUTION", "Probe_Room") == null,
                "resolution: 1024 Critical, 512 Warning, 128 nothing");
            Check(of("REFL_NOT_BAKED", "Probe_Tavern") != null && of("REFL_NOT_BAKED", "Probe_Room") == null && of("REFL_NOT_BAKED", "Probe_Street") == null,
                "not baked: the tavern; neither the baked room nor the realtime street");
            var noBox = of("REFL_NO_BOX", "Probe_Room");
            Check(noBox != null && noBox.severity == Severity.Info && noBox.fixKind == FixKind.Review && of("REFL_NO_BOX", "Probe_Street") == null,
                "no box: the room with a fix to review; the 60 m street is not a room");
            var dup = of("REFL_DUPLICATE", "Probe_Room_Copy");
            Check(dup != null && (string)dup.args[0] == "Probe_Room" && (int)dup.args[1] == 100 && of("REFL_DUPLICATE", "Probe_Room") == null &&
                  of("REFL_DUPLICATE", "Probe_Kiosk") == null, "the copy is a duplicate of the room, 100 %; the kiosk is not");
            var memory = result.issues.Where(i => i.checkId == "REFL_MEMORY").ToList();
            Check(memory.Count == 1 && memory[0].target == null && (int)memory[0].args[2] == 8, "memory: one issue for the scene, 8 probes");
            var outside = result.issues.Where(i => i.checkId == "REFL_OUTSIDE").ToList();
            Check(outside.Count == 1 && (int)outside[0].args[0] == 2, "outside: one issue, 2 objects");
            Check(result.issues.Where(i => ReflectionChecks.Contains(i.checkId)).All(i => i.category == Category.Lighting), "reflection checks are Lighting");

            var report = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, null));
            var l = report.lighting;
            Check(l.reflectionProbes == 8 && l.outsideReflection == 2 && System.Math.Abs(l.reflectionMB - System.Math.Round(result.lighting.ReflectionMB, 1)) < 0.001,
                "JSON: reflectionProbes, reflectionMB, outsideReflection");
        }

        static void ReflectionFix()
        {
            BuildReflectionsScene();
            var r = LightReport.Build();
            var room = RowOf(r, "Probe_Room");
            var fixes = LightReport.BoxFixes(r.reflections.Where(p => p.noBox));
            Check(fixes.Count == 1 && fixes[0].target == room.probe && !fixes[0].fixTouchesAsset && fixes[0].FixDescription.Contains("Box Projection"),
                "\"Box On\": one scene fix for the room");
            Undo.IncrementCurrentGroup();
            int applied = AutoFixer.Apply(fixes);
            Check(applied == 1 && room.probe.boxProjection, "the room gets Box Projection");
            Check(LightReport.BoxFixes(new[] { room }).Count == 0, "a probe with box projection has nothing to fix");
            Undo.PerformUndo();
            Check(!room.probe.boxProjection, "Undo takes it back");
        }

        // No probes, or one good probe with the objects inside: no reflection issues.
        static void ReflectionQuiet()
        {
            NewScene("Reflections-None");
            Shiny("Cup", Vector3.zero);
            var none = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            Check(none.issues.All(i => !ReflectionChecks.Contains(i.checkId)) && none.lighting.reflections.Count == 0 && none.lighting.outsideReflection.Count == 0,
                "no probes: no reflection issues, nobody counted outside");
            Probe("Probe_Good", Vector3.zero, new Vector3(8f, 4f, 8f), ReflectionProbeMode.Baked, 128, true, true);
            var good = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            Check(good.issues.All(i => !ReflectionChecks.Contains(i.checkId)), "one good probe around the cup: no reflection issues");
        }

        static void ReflectionStrings()
        {
            var missing = new System.Collections.Generic.List<string>();
            var lang = Strings.Current;
            foreach (var language in new[] { Lang.En, Lang.Ru })
            {
                Strings.Current = language;
                foreach (var id in ReflectionChecks)
                    foreach (var key in new[] { id, id + ".title", id + ".fix" }.Concat(id == "REFL_NO_BOX" ? new[] { id + ".do" } : new string[0]))
                        if (!Strings.Has(key) || Strings.Get(key).Length == 0) missing.Add(language + " " + key);
                foreach (var key in new[] { "ui.lt.seg.reflections", "ui.lt.refl.summary", "ui.lt.refl.sub", "ui.lt.refl.footer", "ui.lt.refl.empty", "ui.lt.boxOn" })
                    if (!Strings.Has(key)) missing.Add(language + " " + key);
            }
            Strings.Current = lang;
            Check(missing.Count == 0, "every reflection check and the Reflections part have their texts" + (missing.Count > 0 ? ": " + string.Join(", ", missing.ToArray()) : ""));
        }
    }
}
