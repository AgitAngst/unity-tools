using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Opens the auditor window in the normal (not batch) editor for a screenshot taken from outside by
    // scripts\window-shot.ps1 (PrintWindow). The window stays open until <out>.done appears; then it closes:
    // an editor started for one shot quits, the test server stays open for the next command.
    //   Unity.exe -projectPath <test project> -executeMethod AuditTestsEditor.AuditTestShots.Shot
    //     -shotOut E:/.../window.png [-shotLang ru|en] [-shotTab <tab index>] [-shotEmpty] [-shotSkin light|dark] [-shotExpand critical|all]
    //     [-shotWidth <px>] [-shotViewpoints [-shotTour headset|noheadset] | -shotViewpointsEmpty]
    //     [-shotLight atlases|objects|lights|probes|reflections] [-shotCopies] [-shotHeavy tris|dc|tex]
    public static class AuditTestShots
    {
        static AuditorWindow window;
        static int frames;
        static string output;
        static double started;
        static int originalSkin = -1;

        public static void Shot()
        {
            output = Arg("-shotOut") ?? @"E:\dev_personal\unity-tools-scratch\reports\shots\window.png";
            originalSkin = -1;
            File.Delete(output + ".ready");
            File.Delete(output + ".done");
            AuditTestAssets.Prepare();
            AuditTestSettings.ApplyBaseline();
            AuditTestScene.Build("base");
            Strings.Current = Arg("-shotLang") == "en" ? Lang.En : Lang.Ru;

            // The skin is a per-user editor preference: switch only for the shot and restore before exit.
            var skin = Arg("-shotSkin");
            if (skin != null && (skin == "light") == EditorGUIUtility.isProSkin)
            {
                originalSkin = EditorGUIUtility.isProSkin ? 1 : 0;
                SwitchSkin();
            }

            window = ScriptableObject.CreateInstance<AuditorWindow>();
            window.titleContent = new GUIContent(Strings.Get("ui.title"));
            window.ShowUtility();
            var width = Arg("-shotWidth");
            window.position = new Rect(80, 80, width != null ? int.Parse(width) : 760, 860);
            if (HasArg("-shotHistory")) SeedHistory();
            if (HasArg("-shotCopies")) AuditTestViewpoints.BuildCopiesScene();
            if (!HasArg("-shotEmpty")) Invoke("Run");
            if (HasArg("-shotCopies")) Invoke("CopiesForShot", "ANIM_ALWAYS_ANIMATE");
            if (HasArg("-shotHeavy"))
            {
                AuditTestViewpoints.BuildHousesScene();
                Invoke("HeavyForShot", Arg("-shotHeavy") ?? "tris");
            }
            if (HasArg("-shotViewpointsEmpty"))
            {
                var file = ViewpointSet.FileFor(ViewpointSet.LoadedScenePaths());
                if (File.Exists(file)) File.Delete(file);
            }
            if (HasArg("-shotViewpoints")) Invoke("ViewpointsForShot");
            if (HasArg("-shotTour")) Invoke("TourForShot", Arg("-shotTour") != "noheadset");
            if (HasArg("-shotLight"))
            {
                if (Arg("-shotLight") == "reflections") AuditTestViewpoints.BuildReflectionsScene();
                else AuditTestViewpoints.BuildLightScene();
                Invoke("LightForShot", Arg("-shotLight") ?? "atlases");
            }
            var expand = Arg("-shotExpand");
            if (!string.IsNullOrEmpty(expand)) Invoke("ExpandForShot", expand);
            var tab = Arg("-shotTab");
            if (tab != null) SetField("tab", int.Parse(tab));
            if (HasArg("-shotScan"))
            {
                SetField("lmOnlyProblems", false);
                Invoke("ScanLightmapUvs");
            }
            if (HasArg("-shotPick")) Invoke("PickForShot");
            if (HasArg("-shotHistory")) SetField("histFixedOpen", true);
            frames = 0;
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        // Made-up weeks of work before today's run, so the History tab has a trend to show.
        static void SeedHistory()
        {
            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var history = new AuditHistory { scenes = result.scenePaths };
            var notes = new[] { "", "LOD on houses", "Texture atlas for props", "Baked lights, fewer shadow casters" };
            var start = DateTime.Now.Date.AddDays(-28).AddHours(11);
            for (int week = 0; week < notes.Length; week++)
            {
                var time = start.AddDays(7 * week);
                var p = AuditHistory.PointOf(result, null, null, notes[week], time);
                int extra = (notes.Length - week) * 3;
                p.critical += extra / 2;
                p.warning += extra * 2;
                p.sceneTris += (long)(extra * 90000);
                p.drawCalls += extra * 40;
                p.textureMemoryMB += extra * 35;
                for (int i = 0; i < extra; i++) p.problems.Add("W|MESH_NO_LOD|Demo: Town/House_" + (i + 1).ToString("00"));
                var noLod = p.checks.FirstOrDefault(c => c.checkId == "MESH_NO_LOD");
                if (noLod == null) p.checks.Add(noLod = new CheckCount { checkId = "MESH_NO_LOD" });
                noLod.count += extra;
                history.Add(p, time);
            }
            history.Save(AuditHistory.FileFor(result.scenePaths));
        }

        static void Tick()
        {
            frames++;
            window.Repaint();
            if (frames == 40) File.WriteAllText(output + ".ready", Strings.Get("ui.title"));
            bool done = File.Exists(output + ".done");
            if (frames < 40 || (!done && EditorApplication.timeSinceStartup - started < 120)) return;
            EditorApplication.update -= Tick;
            if (originalSkin >= 0 && (originalSkin == 1) != EditorGUIUtility.isProSkin) SwitchSkin();
            // The test server keeps the editor open for the next shot.
            if (window != null) window.Close();
            AuditTestServer.Exit(0);
        }

        // Not public in 2019.4: called by reflection.
        static void SwitchSkin()
        {
            var m = typeof(InternalEditorUtility).GetMethod("SwitchSkinAndRepaintAll", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null) m.Invoke(null, null);
            else Debug.LogWarning("[AuditTests] cannot switch the editor skin");
        }

        static void Invoke(string method, params object[] args)
        {
            var m = typeof(AuditorWindow).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (m == null)
            {
                Debug.LogWarning("[AuditTests] AuditorWindow has no " + method);
                return;
            }
            m.Invoke(window, args);
        }

        static void SetField(string name, object value)
        {
            var f = typeof(AuditorWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null) f.SetValue(window, value);
        }

        static bool HasArg(string name)
        {
            return Array.IndexOf(AuditTestServer.Args, name) >= 0;
        }

        static string Arg(string name)
        {
            var a = AuditTestServer.Args;
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
