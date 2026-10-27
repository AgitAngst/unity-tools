using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // The real window in the visible editor: runs of every scope while the window repaints, all groups and help cards
    // open. Any exception logged while it draws fails the step. Visible editor only (scripts\run-window-runs.ps1).
    public static class AuditTestWindowRuns
    {
        static AuditorWindow window;
        static readonly List<string> errors = new List<string>();
        static readonly StringBuilder log = new StringBuilder();
        static int frames;
        static int stage;
        static List<string> buildScenes;
        static EditorBuildSettingsScene[] savedScenes;
        static int savedKind;

        public static void Run()
        {
            errors.Clear();
            log.Length = 0;
            frames = 0;
            stage = 0;
            savedScenes = EditorBuildSettings.scenes;
            savedKind = (int)AuditScope.Kind;
            Application.logMessageReceived += OnLog;
            AuditTestAssets.Prepare();
            AuditTestSettings.ApplyBaseline();
            buildScenes = HeavyScenes();
            EditorBuildSettings.scenes = buildScenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
            EditorSceneManager.OpenScene(buildScenes[0], OpenSceneMode.Single);
            window = EditorWindow.GetWindow<AuditorWindow>();
            window.position = new Rect(60, 60, 820, 900);
            window.Show();
            EditorApplication.update += Tick;
        }

        // Scenes big enough for a scan to take seconds, so the window repaints while scenes open and close.
        static List<string> HeavyScenes()
        {
            var paths = new List<string>();
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            for (int s = 0; s < 3; s++)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                for (int i = 0; i < 2500; i++)
                {
                    var go = new GameObject("Crate_" + i);
                    go.transform.position = new Vector3(i % 50, 0f, i / 50);
                    go.AddComponent<MeshFilter>().sharedMesh = cube;
                    go.AddComponent<MeshRenderer>().sharedMaterials = new[] { AuditTestAssets.Plain, AuditTestAssets.NoInst };
                }
                var path = AuditTestAssets.Root + "/Scenes/WindowRuns_" + s + ".unity";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                EditorSceneManager.SaveScene(scene, path);
                paths.Add(path);
            }
            return paths;
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
                errors.Add(message + "\n" + stack);
        }

        static void Call(string method, params object[] args)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var m = typeof(AuditorWindow).GetMethods(flags).FirstOrDefault(x => x.Name == method && x.GetParameters().Length == args.Length);
            m.Invoke(window, args);
        }

        static void SetField(string name, object value)
        {
            typeof(AuditorWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(window, value);
        }

        static object GetField(string name)
        {
            return typeof(AuditorWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
        }

        static void OpenEverything()
        {
            Call("ExpandForShot", "all");
            var groups = (System.Collections.IList)typeof(AuditorWindow).GetField("groups", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
            var open = (HashSet<string>)typeof(AuditorWindow).GetField("helpOpen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
            foreach (IssueGroup g in groups) open.Add(g.key);
        }

        static void Step(string what, Action action)
        {
            log.AppendLine("step " + stage + ": " + what + " (errors so far " + errors.Count + ")");
            try
            {
                action();
            }
            catch (Exception e)
            {
                errors.Add(what + ": " + e);
            }
        }

        static void Tick()
        {
            frames++;
            window.Repaint();
            if (frames % 20 != 0) return;
            switch (stage++)
            {
                case 0: Step("run, open scenes", () => { Call("Run"); OpenEverything(); }); break;
                case 1: Step("run again", () => { Call("Run"); OpenEverything(); }); break;
                case 2: Step("build scenes over a shown result", () => { AuditScope.Kind = ScopeKind.BuildScenes; Call("Run"); OpenEverything(); }); break;
                case 3: Step("build scenes again", () => { Call("Run"); OpenEverything(); }); break;
                case 4: Step("open scenes after build scenes", () => { AuditScope.Kind = ScopeKind.Loaded; Call("Run"); OpenEverything(); }); break;
                case 5: Step("getting started over the result", () => Call("ShowStart")); break;
                case 6: Step("run from getting started", () => { Call("Run"); OpenEverything(); }); break;
                case 7: Step("gain scene", () => { AuditTestViewpoints.BuildGainScene(); Call("Run"); OpenEverything(); }); break;
                case 8: Step("narrow window", () => window.position = new Rect(60, 60, 560, 700)); break;
                case 9: Step("by category", () => { SetField("byCategory", true); OpenEverything(); }); break;
                // As after a script reload with the window open, when an older version kept some styles and newer
                // fields came back empty.
                case 10: Step("styles after a reload", () =>
                {
                    SetField("stylesReady", false);
                    SetField("gainHead", null);
                    SetField("gainRow", null);
                    SetField("hintText", null);
                    SetField("linkMini", null);
                    window.position = new Rect(60, 60, 820, 900);
                }); break;
                case 11: Step("styles made again", () =>
                {
                    if (GetField("gainHead") == null || GetField("hintText") == null) errors.Add("styles were not made again after a reload");
                    SetField("byCategory", false);
                }); break;
                default: Finish(); break;
            }
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            EditorBuildSettings.scenes = savedScenes;
            AuditScope.Kind = (ScopeKind)savedKind;
            if (window != null) window.Close();
            var text = "window-runs: " + (errors.Count == 0 ? "OK" : errors.Count + " ERRORS") + "\n" + log + string.Join("\n----\n", errors.Take(20).ToArray());
            var dir = @"E:\dev_personal\unity-tools-scratch\reports\audit-tests";
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "window-runs.txt"), text, new UTF8Encoding(false));
            AuditTestServer.Exit(errors.Count == 0 ? 0 : 1);
        }
    }
}
