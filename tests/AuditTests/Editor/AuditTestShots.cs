using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Opens the auditor window in the normal (not batch) editor for a screenshot taken from outside by
    // scripts\window-shot.ps1 (PrintWindow). The window stays open until <out>.done appears.
    //   Unity.exe -projectPath <test project> -executeMethod AuditTestsEditor.AuditTestShots.Shot
    //     -shotOut E:/.../window.png [-shotLang ru|en] [-shotTab 0|1|2] [-shotEmpty] [-shotSkin light|dark] [-shotExpand critical|all]
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
            window.position = new Rect(80, 80, 760, 860);
            if (!HasArg("-shotEmpty")) Invoke("Run");
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
            frames = 0;
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
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
            EditorApplication.Exit(0);
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
            return Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
