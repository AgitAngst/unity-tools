using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityTools.SceneOptimization;
using Object = UnityEngine.Object;

namespace AuditTestsEditor
{
    // Help of the window: the cards of the checks in both languages, the Help menu and the pages it opens, the task cards
    // of "Getting started". The guide is read from the repository: -helpDocs <repo>\docs.
    public static partial class AuditTestViewpoints
    {
        static readonly string[] HelpChecks =
        {
            "DC_MULTI_MATERIAL", "DC_INSTANCING_OFF", "DC_STATIC_CANDIDATE", "DC_SHADOW_CASTERS", "MESH_NO_LOD", "MESH_READ_WRITE",
            "MESH_DUPLICATE", "TEX_SIZE", "TEX_UNCOMPRESSED", "TEX_NO_MIPMAPS", "TEX_READ_WRITE", "TEX_ALPHA_UNUSED", "TEX_DUPLICATE",
            "TEX_NPOT", "LIGHT_REALTIME", "LIGHT_SHADOWED", "LIGHT_NO_UV1", "LIGHT_SMALL_IN_LIGHTMAP", "LIGHT_LIGHTMAP_UNCOMPRESSED",
            "ANIM_ALWAYS_ANIMATE", "AUDIO_DECOMPRESS_LONG", "AUDIO_2D_SOURCE", "VR_STEREO_MODE", "VR_CANVAS_OVERLAY", "VR_MSAA",
            "VR_NO_OCCLUSION", "MAT_TRANSPARENT", "SHADER_NO_STEREO", "PART_LARGE_TRANSPARENT", "REFL_RESOLUTION"
        };

        // Checks of the list whose code estimates a gain: their card has the section "Gain".
        static readonly string[] HelpWithGain =
        {
            "DC_MULTI_MATERIAL", "DC_INSTANCING_OFF", "MESH_NO_LOD", "MESH_READ_WRITE", "MESH_DUPLICATE", "TEX_SIZE", "TEX_UNCOMPRESSED",
            "TEX_READ_WRITE", "TEX_ALPHA_UNUSED", "TEX_DUPLICATE", "LIGHT_REALTIME", "LIGHT_SMALL_IN_LIGHTMAP", "LIGHT_LIGHTMAP_UNCOMPRESSED",
            "AUDIO_DECOMPRESS_LONG", "REFL_RESOLUTION"
        };

        public static void RunHelp()
        {
            Run("help", Help);
        }

        static void Help()
        {
            HelpCards();
            HelpGains();
            HelpMenu();
            HelpStart();
            HelpShadowCasters();
        }

        // Fixed with the card text: casters without a shadow pass are not a row "drawn again in 0 shadow passes".
        static void HelpShadowCasters()
        {
            NewScene("HelpShadows");
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            // The helper makes objects without shadows: this one casts.
            MeshObject("Caster", cube, Vector3.zero, AuditTestAssets.Plain).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var r = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            Check(r.issues.All(i => i.checkId != "DC_SHADOW_CASTERS"), "shadow casters without a shadowed light: no row");
            var lamp = new GameObject("Lamp").AddComponent<Light>();
            lamp.type = LightType.Spot;
            lamp.lightmapBakeType = LightmapBakeType.Realtime;
            lamp.shadows = LightShadows.Hard;
            r = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            Check(r.issues.Any(i => i.checkId == "DC_SHADOW_CASTERS"), "shadow casters with a realtime shadowed spot: the row");
        }

        static bool Cyrillic(string text)
        {
            return text.Any(c => c >= 'А' && c <= 'я' || c == 'ё' || c == 'Ё');
        }

        static void HelpCards()
        {
            var ids = CheckHelp.Ids;
            Check(ids.Count == HelpChecks.Length && HelpChecks.All(ids.Contains),
                "30 checks have a card (" + ids.Count + "; missing: " + string.Join(", ", HelpChecks.Where(i => !ids.Contains(i)).ToArray()) + ")");
            var was = Strings.Current;
            var bad = new List<string>();
            foreach (var lang in new[] { Lang.En, Lang.Ru })
            {
                Strings.Current = lang;
                foreach (var id in HelpChecks)
                {
                    if (!Strings.Has(id + ".title")) bad.Add(id + ": no title");
                    foreach (var section in CheckHelp.Sections)
                    {
                        var text = CheckHelp.Text(id, section);
                        bool wanted = section != "gain" || HelpWithGain.Contains(id);
                        if (wanted != (text.Length > 0)) bad.Add(id + "." + section + (wanted ? ": empty" : ": not expected"));
                        if (text.Length > 0 && Cyrillic(text) != (lang == Lang.Ru)) bad.Add(id + "." + section + ": wrong language " + lang);
                        if (text.Length > 0 && !Strings.Has("ui.help." + section)) bad.Add("ui.help." + section + ": no heading");
                    }
                }
            }
            Strings.Current = was;
            Check(bad.Count == 0, "every card has what, why, how and keep in both languages, gain only with a gain estimate" +
                (bad.Count > 0 ? " (" + string.Join("; ", bad.Take(12).ToArray()) + ")" : ""));
            Check(!CheckHelp.Has("ANIM_COUNT") && CheckHelp.Text("ANIM_COUNT", "what") == "", "a check without a card has no link");
        }

        // A check of the list that reports a gain on the known assets must have the section "Gain".
        static void HelpGains()
        {
            BuildGainScene();
            var r = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var withGain = r.issues.Where(i => i.HasGain && HelpChecks.Contains(i.checkId)).Select(i => i.checkId).Distinct().ToList();
            Check(withGain.Count >= 8 && withGain.All(HelpWithGain.Contains),
                "checks with a gain on the test scene have the section Gain (" + string.Join(", ", withGain.ToArray()) + ")");
        }

        static object CallWindow(AuditorWindow window, string method, params object[] args)
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            // By the number of arguments: EditorWindow has its own public Show().
            var m = typeof(AuditorWindow).GetMethods(flags).FirstOrDefault(x => x.Name == method && x.GetParameters().Length == args.Length);
            if (m == null) throw new MissingMethodException("AuditorWindow." + method);
            return m.Invoke(m.IsStatic ? null : window, args);
        }

        static object FieldOf(AuditorWindow window, string name)
        {
            var f = typeof(AuditorWindow).GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            if (f == null) throw new MissingFieldException("AuditorWindow." + name);
            return f.GetValue(window);
        }

        static string HelpDocs()
        {
            var a = AuditTestServer.Args;
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-helpDocs") return a[i + 1];
            return null;
        }

        static void HelpMenu()
        {
            var tabs = new[] { "ui.tab.problems", "ui.tab.viewpoints", "ui.tab.lightmap", "ui.tab.light", "ui.tab.heavy", "ui.tab.budgets", "ui.tab.profiler", "ui.tab.history" };
            var was = Strings.Current;
            Strings.Current = Lang.Ru;
            bool labels = true;
            for (int t = 0; t < tabs.Length; t++)
            {
                var items = (List<KeyValuePair<string, string>>)CallWindow(null, "HelpMenuItems", t);
                labels &= items.Count == 5 && items[0].Value == "start" && items[1].Key.Contains("«" + Strings.Get(tabs[t]) + "»") && items[2].Key == "" &&
                          items[3].Value == "guide" && items[4].Value == "reference";
                labels &= (string)CallWindow(null, "HelpUrl", "tab", t) == CheckHelp.GuideUrl + "#" + CheckHelp.TabAnchors[t];
            }
            Strings.Current = was;
            Check(labels, "Help menu: getting started, the open tab by name, the guide and the reference; a tab opens its section");
            Check((string)CallWindow(null, "HelpUrl", "reference", 0) == CheckHelp.ReferenceUrl + "#checks" &&
                  CheckHelp.GuideUrl.StartsWith("https://github.com/") && CheckHelp.ReferenceUrl.EndsWith("docs/scene-optimization-auditor.md"),
                "the reference opens at its checks");

            var docs = HelpDocs();
            if (docs == null || !Directory.Exists(docs))
            {
                Check(false, "-helpDocs names the docs folder of the repository (" + docs + ")");
                return;
            }
            var guide = File.ReadAllText(Path.Combine(docs, "scene-optimization-auditor-guide.md"));
            var reference = File.ReadAllText(Path.Combine(docs, "scene-optimization-auditor.md"));
            var anchors = CheckHelp.TabAnchors.ToList();
            var missing = anchors.Where(a => !guide.Contains("<a id=\"" + a + "\"></a>")).ToList();
            Check(missing.Count == 0, "the guide has every section the window opens (" + string.Join(", ", missing.ToArray()) + ")");
            Check(reference.Contains("<a id=\"" + CheckHelp.ChecksAnchor + "\"></a>"), "the reference has the anchor of the checks");
        }

        static void HelpStart()
        {
            var savedScenes = EditorBuildSettings.scenes;
            var savedKind = AuditScope.Kind;
            bool savedGain = EditorPrefs.GetBool("UnityTools.SceneAudit.ByGain", false);
            var window = ScriptableObject.CreateInstance<AuditorWindow>();
            try
            {
                Check(!(bool)CallWindow(window, "PrepareTask", "gpu") && (int)FieldOf(window, "tab") == 5, "GPU \"Change…\" opens Budgets");
                Check(!(bool)CallWindow(window, "PrepareTask", "fps") && (int)FieldOf(window, "tab") == 1, "\"The level misses 90 FPS\" opens Viewpoints");
                Check(!(bool)CallWindow(window, "PrepareTask", "memory") && (int)FieldOf(window, "tab") == 4 && (int)FieldOf(window, "hvMeasure") == 2,
                    "\"Out of GPU memory\" opens Heaviest on Textures");

                SetByGainPref(false);
                Check((bool)CallWindow(window, "PrepareTask", "first") && EditorPrefs.GetBool("UnityTools.SceneAudit.ByGain", false),
                    "\"First pass\" turns on By gain and runs the audit");

                EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];
                AuditScope.Kind = ScopeKind.Loaded;
                Check(!(bool)CallWindow(window, "PrepareTask", "build") && AuditScope.Kind == ScopeKind.Loaded, "\"Before a build\" without build scenes does nothing");
                var path = NewScene("HelpBuild");
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
                Check((bool)CallWindow(window, "PrepareTask", "build") && AuditScope.Kind == ScopeKind.BuildScenes,
                    "\"Before a build\" sets the scope to the build scenes and runs the audit");

                CallWindow(window, "ShowStart");
                Check((bool)FieldOf(window, "startShown") && (int)FieldOf(window, "tab") == 0, "\"Getting started\" from the menu opens it on Problems");
                var r = SceneAuditor.RunFullAudit(new VRBudgets(), false);
                CallWindow(window, "Show", r, false);
                Check(!(bool)FieldOf(window, "startShown"), "a new run hides \"Getting started\"");
            }
            finally
            {
                Object.DestroyImmediate(window);
                EditorBuildSettings.scenes = savedScenes;
                AuditScope.Kind = savedKind;
                SetByGainPref(savedGain);
            }
        }

        // The window keeps "By gain" in a static cache: the preference and the cache together.
        static void SetByGainPref(bool on)
        {
            EditorPrefs.SetBool("UnityTools.SceneAudit.ByGain", on);
            var f = typeof(AuditorWindow).GetField("byGain", BindingFlags.Static | BindingFlags.NonPublic);
            if (f != null) f.SetValue(null, null);
        }
    }
}
