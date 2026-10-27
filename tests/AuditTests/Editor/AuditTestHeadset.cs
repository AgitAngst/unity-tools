using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // The target headset (2.3.0): sets, defaults and old files, the thresholds that follow it, physics, field of view, report.
    public static partial class AuditTestViewpoints
    {
        public static void RunHeadset()
        {
            Run("headset", Headset);
        }

        static VRBudgets ViveProBudgets()
        {
            var b = new VRBudgets();
            b.ApplyHeadset("VivePro", 90, 100);
            return b;
        }

        static bool Near1(float a, float b)
        {
            return Math.Abs(a - b) < 0.001f;
        }

        static void Headset()
        {
            Check(VRBudgets.Headsets.Select(h => h.id).SequenceEqual(new[] { "VivePro", "VivePro2", "VivePro2Wireless", "Index" }),
                "headsets: Vive Pro, Vive Pro 2, Vive Pro 2 Wireless, Index");
            Check(VRBudgets.FindHeadset("VivePro2Wireless").rates.SequenceEqual(new[] { 90 }) && VRBudgets.FindHeadset("VivePro2").rates.SequenceEqual(new[] { 90, 120 }) &&
                  VRBudgets.FindHeadset("Index").rates.SequenceEqual(new[] { 80, 90, 120, 144 }), "refresh rates of the headsets");

            var file = VRBudgets.FilePath;
            var saved = File.Exists(file) ? File.ReadAllText(file) : null;
            try
            {
                if (File.Exists(file)) File.Delete(file);
                var b = VRBudgets.Load();
                Check(b.targetHeadset == "VivePro2Wireless" && b.refreshRate == 90 && b.renderScale == 100 && !b.HeadsetCustom,
                    "without a budget file: Vive Pro 2 Wireless at 90 Hz and 100 %");
                Check(Near1(b.frameTimeWarningMs, 10f) && Near1(b.frameTimeCriticalMs, 11.1f) && b.transparentWarningPercent == 38 && b.transparentCriticalPercent == 77,
                    "its frame time 10 / 11.1 ms and transparent share 38 / 77 %");
                Check(b.RenderWidth == 3427 && b.RenderHeight == 3427 && Math.Abs(b.PixelFactor - 2.6) < 0.01, "render 3427 × 3427 per eye, 2.6 times the pixels of the Vive Pro (" + b.PixelFactor.ToString("F3") + ")");
                var vivePro = ViveProBudgets();
                Check(vivePro.RenderWidth == 2016 && vivePro.RenderHeight == 2240 && Math.Abs(vivePro.PixelFactor - 1) < 1e-9 &&
                      vivePro.transparentWarningPercent == 100 && vivePro.transparentCriticalPercent == 200, "Vive Pro: 2016 × 2240, transparent 100 / 200 %");
                Check(b.FpsWarning == 89 && b.FpsCritical == 80, "FPS colors at 90 Hz: 89 and 80");

                b.ApplyHeadset("VivePro2", 120, 100);
                Check(b.refreshRate == 120 && Near1(b.frameTimeWarningMs, 7.5f) && Near1(b.frameTimeCriticalMs, 8.3f) && b.FpsWarning == 119 && b.FpsCritical == 107,
                    "Vive Pro 2 at 120 Hz: frame 7.5 / 8.3 ms, FPS 119 and 107");
                Check(b.transparentWarningPercent == 29 && b.transparentCriticalPercent == 58, "transparent share falls with the pixel rate: 29 / 58 % (" + b.transparentWarningPercent + " / " + b.transparentCriticalPercent + ")");
                b.ApplyHeadset("VivePro2Wireless", 120, 100);
                Check(b.refreshRate == 90, "a rate the headset has not goes to its default: the wireless adapter carries 90 Hz");
                b.ApplyHeadset("VivePro2Wireless", 90, 50);
                Check(b.RenderWidth == 2423 && b.transparentWarningPercent == 77 && b.transparentCriticalPercent == 154, "50 % of SteamVR: 2423 per side, transparent 77 / 154 %");
                b.ApplyHeadset("Index", 144, 1000);
                Check(b.renderScale == 500 && Near1(b.frameTimeCriticalMs, 6.9f) && Near1(b.frameTimeWarningMs, 6.3f), "Index at 144 Hz: 6.3 / 6.9 ms; the percent is held to 500");

                b.ApplyHeadset("VivePro2", 120, 100);
                b.frameTimeCriticalMs = 9f;
                Check(b.HeadsetCustom, "an edited frame time makes the headset values custom");
                var back = VRBudgets.DefaultsFor(b.targetGpu, b.targetHeadset, b.refreshRate, b.renderScale);
                Check(!back.HeadsetCustom && back.targetHeadset == "VivePro2" && back.refreshRate == 120 && Near1(back.frameTimeCriticalMs, 8.3f), "Defaults keep the headset and bring its values back");
                back.Save();
                var again = VRBudgets.Load();
                Check(again.targetHeadset == "VivePro2" && again.refreshRate == 120 && again.renderScale == 100 && !again.HeadsetCustom &&
                      File.ReadAllText(file).Contains("\"targetHeadset\": \"VivePro2\""), "the budget file stores the headset, the rate and the percent");

                File.WriteAllText(file, "{ \"targetGpu\": \"RTX2070\", \"drawCallsWarning\": 1000 }");
                var old = VRBudgets.Load();
                Check(old.targetHeadset == "VivePro" && old.refreshRate == 90 && old.renderScale == 100 && !old.HeadsetCustom &&
                      Near1(old.frameTimeCriticalMs, 11.1f) && old.transparentWarningPercent == 100 && old.transparentCriticalPercent == 200,
                    "a budget file from before 2.3.0: Vive Pro at 90 Hz with its values, not custom");
            }
            finally
            {
                if (saved != null) File.WriteAllText(file, saved);
                else if (File.Exists(file)) File.Delete(file);
            }

            // The physics step follows the rate.
            NewScene("Headset");
            Time.fixedDeltaTime = 1f / 90f;
            var at90 = new VRBudgets();
            at90.ApplyHeadset("VivePro2", 90, 100);
            var at120 = new VRBudgets();
            at120.ApplyHeadset("VivePro2", 120, 100);
            var r90 = SceneAuditor.RunFullAudit(at90, false);
            var r120 = SceneAuditor.RunFullAudit(at120, false);
            var slow = r120.issues.FirstOrDefault(i => i.checkId == "PHYS_FIXED_STEP_SLOW");
            Check(!r90.issues.Any(i => i.checkId.StartsWith("PHYS_FIXED_STEP")) && slow != null && slow.Message.Contains("120") && slow.Suggestion.Contains("1/120"),
                "a step of 1/90 s fits 90 Hz and is slower than 120 Hz");

            // The field of view of viewpoints is the headset's.
            var set = ViewpointSet.Load(new[] { EditorSceneManager.GetActiveScene().path });
            set.points.Clear();
            set.AddManual(new Vector3(0f, 1.7f, 0f), 0f);
            ViewpointEstimator.Estimate(set, set.points, false, null, at120);
            bool pro2 = Math.Abs(ViewpointEstimator.FieldOfView - 116f) < 0.01f && Math.Abs(ViewpointEstimator.Aspect - 1f) < 0.001f;
            ViewpointEstimator.Estimate(set, set.points, false, null, ViveProBudgets());
            bool pro = Math.Abs(ViewpointEstimator.FieldOfView - 110f) < 0.01f && Math.Abs(ViewpointEstimator.Aspect - 0.9f) < 0.001f;
            Check(pro2 && pro, "viewpoints look with the field of view of the headset: 116° 1:1, 110° 0.9:1");

            // The report names the headset.
            var report = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(r120, null));
            Check(report.headset == "VivePro2" && report.refreshRate == 120 && report.renderScale == 100 && report.targetFPS == 120 && report.platform == "PC VR - HTC Vive Pro 2",
                "JSON: headset, refresh rate, percent, target FPS and platform");
        }
    }
}
