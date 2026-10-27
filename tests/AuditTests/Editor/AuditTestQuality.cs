using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Settings of the quality level and frame rate for VR (2.2.0): thresholds by the advice for RTX 2070 and the new checks.
    public static partial class AuditTestViewpoints
    {
        public static void RunQualityChecks()
        {
            Run("quality-checks", QualityChecks);
        }

        // A script of the project that sets the frame rate: written by one command, checked after the reload by the next.
        const string ProbeScript = AuditTestAssets.Root + "/FrameRateProbe.cs";

        public static void RunFrameRateScriptMake()
        {
            Run("frame-rate-script-make", () =>
            {
                File.WriteAllText(ProbeScript, "namespace AuditGeneratedProbe { public class FrameRateProbe : UnityEngine.MonoBehaviour { void Start() { UnityEngine." +
                                               "Application." + "targetFrameRate = 60; } } }");
                AssetDatabase.ImportAsset(ProbeScript, ImportAssetOptions.ForceSynchronousImport);
                Check(File.Exists(ProbeScript), "the probe script is written");
            });
        }

        public static void RunFrameRateScriptCheck()
        {
            Run("frame-rate-script-check", () =>
            {
                try
                {
                    NewScene("FrameRateScript");
                    var rows = Audit().issues.Where(i => i.checkId == "VR_FRAME_RATE_SCRIPT").ToList();
                    Check(rows.Count == 1 && rows[0].assetPath == ProbeScript && rows[0].severity == Severity.Info &&
                          rows[0].Message.Contains(FrameRateScripts.TargetFrameRate) && rows[0].target is MonoScript,
                        "the script that sets targetFrameRate is one Info row on the script (" + rows.Count + ")");
                }
                finally
                {
                    AssetDatabase.DeleteAsset(ProbeScript);
                }
                Check(!File.Exists(ProbeScript), "the probe script is deleted");
            });
        }

        static bool Found(AuditResult r, string id, Severity? severity = null)
        {
            return r.issues.Any(i => i.checkId == id && (!severity.HasValue || i.severity == severity.Value));
        }

        static AuditResult Audit()
        {
            return SceneAuditor.RunFullAudit(new VRBudgets(), false);
        }

        static void QualityChecks()
        {
            QualityThresholds();
            QualitySoftShadows();
            QualityLevelToggles();
            FrameRateScriptsCheck();
        }

        static void QualityThresholds()
        {
            NewScene("Quality");
            var skinned = new GameObject("Skinned").AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            var msaa = new[] { 0, 2, 4, 8 }.Select(aa =>
            {
                QualitySettings.antiAliasing = aa;
                var r = Audit();
                return aa + ":" + (Found(r, "VR_MSAA", Severity.Warning) ? "off" : Found(r, "VR_MSAA_HIGH", Severity.Info) ? "high" : "ok");
            }).ToArray();
            Check(string.Join(" ", msaa) == "0:off 2:ok 4:ok 8:high", "MSAA: off is a warning, 2x and 4x pass, 8x is Info (" + string.Join(" ", msaa) + ")");
            QualitySettings.antiAliasing = 4;

            QualitySettings.shadowDistance = 40f;
            bool at40 = Found(Audit(), "LIGHT_SHADOW_DISTANCE");
            QualitySettings.shadowDistance = 41f;
            bool at41 = Found(Audit(), "LIGHT_SHADOW_DISTANCE", Severity.Info);
            Check(!at40 && at41, "shadow distance: 40 m passes, 41 m is Info");
            QualitySettings.shadowDistance = 40f;

            QualitySettings.skinWeights = SkinWeights.TwoBones;
            bool two = Found(Audit(), "MESH_QUALITY_SKIN_WEIGHTS");
            QualitySettings.skinWeights = SkinWeights.FourBones;
            bool four = Found(Audit(), "MESH_QUALITY_SKIN_WEIGHTS", Severity.Info);
            Check(!two && four, "Skin Weights: Two Bones pass, Four Bones are Info");
            QualitySettings.skinWeights = SkinWeights.TwoBones;
        }

        static void QualitySoftShadows()
        {
            NewScene("SoftShadows");
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            sun.shadows = LightShadows.Soft;
            var baked = new GameObject("Baked").AddComponent<Light>();
            baked.type = LightType.Spot;
            baked.lightmapBakeType = LightmapBakeType.Baked;
            baked.shadows = LightShadows.Soft;

            QualitySettings.shadows = ShadowQuality.All;
            var r = Audit();
            var row = r.issues.FirstOrDefault(i => i.checkId == "LIGHT_SOFT_SHADOWS" && OnObject(i, "Sun"));
            Check(row != null && row.severity == Severity.Info && row.AutoFixable && row.fixKind == FixKind.Review && !row.fixTouchesAsset,
                "a realtime soft light is Info with a reviewed scene fix");
            Check(!r.issues.Any(i => i.checkId == "LIGHT_SOFT_SHADOWS" && OnObject(i, "Baked")), "a baked light is not reported");
            if (row != null)
            {
                AutoFixer.Apply(new[] { row });
                Check(sun.shadows == LightShadows.Hard, "the fix sets Hard Shadows");
                Undo.PerformUndo();
                Check(sun.shadows == LightShadows.Soft, "Undo brings Soft Shadows back");
            }
            QualitySettings.shadows = ShadowQuality.HardOnly;
            Check(!Found(Audit(), "LIGHT_SOFT_SHADOWS"), "with Hard Shadows Only in Quality nothing is reported");
            QualitySettings.shadows = ShadowQuality.All;
        }

        static void QualityLevelToggles()
        {
            NewScene("Toggles");
            QualitySettings.realtimeReflectionProbes = true;
            QualitySettings.softParticles = true;
            AuditTestSettings.SetVSync(2);
            var r = Audit();
            Check(Found(r, "REFL_QUALITY_REALTIME", Severity.Info), "Realtime Reflection Probes in Quality are Info");
            Check(!Found(r, "PART_SOFT_PARTICLES"), "Soft Particles without particle systems are not reported");
            var vsync = r.issues.FirstOrDefault(i => i.checkId == "VR_VSYNC");
            Check(vsync != null && vsync.severity == Severity.Info && vsync.Message.Contains("Every Second V Blank"), "VSync Count other than Don't Sync is Info");

            new GameObject("Smoke").AddComponent<ParticleSystem>();
            Check(Found(Audit(), "PART_SOFT_PARTICLES", Severity.Info), "Soft Particles with a particle system are Info");

            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            AuditTestSettings.SetVSync(0);
            r = Audit();
            Check(!Found(r, "REFL_QUALITY_REALTIME") && !Found(r, "PART_SOFT_PARTICLES") && !Found(r, "VR_VSYNC"), "all three off: nothing is reported");
        }

        // Patterns are put together so this file itself does not assign the frame rate.
        static void FrameRateScriptsCheck()
        {
            const string target = "Application." + "targetFrameRate";
            const string vsync = "QualitySettings." + "vSyncCount";
            Check(FrameRateScripts.Assigned("void Start() { " + target + " = 60; }").SequenceEqual(new[] { FrameRateScripts.TargetFrameRate }), "an assignment of targetFrameRate is found");
            Check(FrameRateScripts.Assigned(vsync + "\n  = 0;").SequenceEqual(new[] { FrameRateScripts.VSyncCount }), "an assignment of vSyncCount over two lines is found");
            Check(FrameRateScripts.Assigned("// " + target + " = 60;\n/* " + vsync + " = 0; */").Length == 0, "comments do not count");
            Check(FrameRateScripts.Assigned("if (" + target + " == 60) Debug.Log(" + vsync + ");").Length == 0, "a comparison or a read does not count");

            var dir = Path.Combine("Temp", "AuditFrameRate");
            Directory.CreateDirectory(dir);
            var setter = Path.Combine(dir, "Setter.cs");
            var clean = Path.Combine(dir, "Clean.cs");
            File.WriteAllText(setter, "class A { void Start() { " + vsync + " = 0; } }");
            File.WriteAllText(clean, "class B { }");
            var found = FrameRateScripts.Find(new[] { setter, clean });
            Check(found.Count == 1 && found[0].Key == setter && found[0].Value.SequenceEqual(new[] { FrameRateScripts.VSyncCount }), "files: only the script that assigns");
            File.WriteAllText(setter, "class A { void Start() { " + target + " = 90; } }");
            File.SetLastWriteTimeUtc(setter, File.GetLastWriteTimeUtc(setter).AddMinutes(1));
            found = FrameRateScripts.Find(new[] { setter, clean });
            Check(found.Count == 1 && found[0].Value.SequenceEqual(new[] { FrameRateScripts.TargetFrameRate }), "a changed script is read again");
            Directory.Delete(dir, true);
        }
    }
}
