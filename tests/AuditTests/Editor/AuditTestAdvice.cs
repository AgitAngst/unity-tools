using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Specific rows with fixes (2.9.0, owner 29.10.2026): a row per simple Standard material with the shader that fits it
    // and a fix that switches it, a row per texture without trilinear filtering with a fix that turns it on.
    public static partial class AuditTestViewpoints
    {
        const string AdviceDir = AuditTestAssets.Root + "/Advice";

        public static void RunSpecificAdvice()
        {
            Run("specific-advice", SpecificAdvice);
        }

        // A 64x64 texture with mipmaps and the given filter mode.
        static Texture2D AdviceTexture(string name, FilterMode filter)
        {
            var path = AdviceDir + "/" + name + ".png";
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.mipmapEnabled = true;
            importer.filterMode = filter;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material AdviceMaterial(string name, Texture texture, System.Action<Material> setup)
        {
            var material = new Material(Shader.Find("Standard")) { mainTexture = texture };
            setup(material);
            AssetDatabase.CreateAsset(material, AdviceDir + "/" + name + ".mat");
            return material;
        }

        // Keyword lines of the shot shader: 24576 variants; the scene needs 768, without the shader_feature, fog and instancing
        // variants it does not use.
        const string AdviceShaderPragmas = "#pragma multi_compile _ _LAYERS_4 _LAYERS_8\n#pragma shader_feature _ _SNOW _MOSS _WET\n#pragma multi_compile_fog\n" +
            "#pragma multi_compile_instancing\n#pragma multi_compile _ _WIND\n#pragma multi_compile _ _RAIN\n#pragma multi_compile _ _FADE\n#pragma multi_compile _ _HOLES\n" +
            "#pragma multi_compile _ _PUDDLES\n#pragma multi_compile _ _DECALS\n#pragma multi_compile _ _TINT\n#pragma multi_compile _ _EDGES";

        // The scene of the test and of the window shot: simple Standard materials, textures of every filter mode and a
        // shader of many variants. The scene objects stay; the shot leaves the assets in place.
        public static void BuildAdviceScene()
        {
            if (Directory.Exists(AdviceDir)) AssetDatabase.DeleteAsset(AdviceDir);
            Directory.CreateDirectory(AdviceDir);
            AssetDatabase.Refresh();
            var bilinear = AdviceTexture("T_Bilinear", FilterMode.Bilinear);
            var point = AdviceTexture("T_Point", FilterMode.Point);
            var trilinear = AdviceTexture("T_Trilinear", FilterMode.Trilinear);
            var normal = AdviceTexture("T_Normal", FilterMode.Trilinear);

            var white = AdviceMaterial("M_White", bilinear, m => { m.SetFloat("_Metallic", 0.25f); m.SetFloat("_Glossiness", 0.7f); });
            var red = AdviceMaterial("M_Red", point, m => m.color = Color.red);
            var cut = AdviceMaterial("M_Cut", trilinear, m =>
            {
                m.SetFloat("_Mode", 1f);
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)RenderQueue.AlphaTest;
            });
            var bumped = AdviceMaterial("M_Bumped", trilinear, m => { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); });
            var unsaved = new Material(Shader.Find("Standard")) { name = "M_Unsaved", mainTexture = trilinear };
            white.SetTextureScale("_MainTex", new Vector2(3f, 2f));
            var shaderPath = AdviceDir + "/TerrainUber.shader";
            File.WriteAllText(shaderPath, ShaderText("TerrainUber", "", StereoPass("", "", AdviceShaderPragmas.Replace("\n", "\n            "))).Replace("AuditTest/", "Demo/"));
            AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceSynchronousImport);
            var uber = new Material(AssetDatabase.LoadAssetAtPath<Shader>(shaderPath));
            AssetDatabase.CreateAsset(uber, AdviceDir + "/M_Ground.mat");
            AssetDatabase.SaveAssets();

            NewScene("Advice");
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            MeshObject("White_1", cube, new Vector3(0f, 0f, 0f), white);
            MeshObject("White_2", cube, new Vector3(2f, 0f, 0f), white);
            MeshObject("Red", cube, new Vector3(4f, 0f, 0f), red);
            MeshObject("Cut", cube, new Vector3(6f, 0f, 0f), cut);
            MeshObject("Bumped", cube, new Vector3(8f, 0f, 0f), bumped);
            MeshObject("Unsaved", cube, new Vector3(10f, 0f, 0f), unsaved);
            MeshObject("Ground", cube, new Vector3(12f, 0f, 0f), uber);
        }

        static T AdviceAsset<T>(string file) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(AdviceDir + "/" + file);
        }

        static void SpecificAdvice()
        {
            BuildAdviceScene();
            var bilinear = AdviceAsset<Texture2D>("T_Bilinear.png");
            var point = AdviceAsset<Texture2D>("T_Point.png");
            var trilinear = AdviceAsset<Texture2D>("T_Trilinear.png");
            var white = AdviceAsset<Material>("M_White.mat");
            var red = AdviceAsset<Material>("M_Red.mat");
            var cut = AdviceAsset<Material>("M_Cut.mat");
            var bumped = AdviceAsset<Material>("M_Bumped.mat");
            var unsaved = GameObject.Find("Unsaved").GetComponent<Renderer>().sharedMaterial;

            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            System.Func<string, Object, AuditIssue> on = (id, target) => result.issues.FirstOrDefault(i => i.checkId == id && i.target == target);

            // Materials: the shader that fits, Metallic and Smoothness, a fix for assets only.
            var simple = result.issues.Where(i => i.checkId == "MAT_SIMPLE_STANDARD").ToList();
            log.AppendLine("simple Standard: " + string.Join("; ", simple.Select(i => i.target.name + " " + i.Message).ToArray()));
            var w = on("MAT_SIMPLE_STANDARD", white);
            Check(w != null && (string)w.args[0] == "Mobile/Diffuse" && (double)w.args[1] == 0.25 && (double)w.args[2] == 0.7 && (string)w.args[3] == "M_White" && w.users == 2 &&
                  w.fixKind == FixKind.Review && w.fixTouchesAsset && w.AutoFixable && w.severity == Severity.Info,
                "a white opaque material: Mobile/Diffuse, Metallic 0.25, Smoothness 0.7, two objects, a reviewed asset fix");
            var r = on("MAT_SIMPLE_STANDARD", red);
            Check(r != null && (string)r.args[0] == "Legacy Shaders/Diffuse", "a red material: Legacy Diffuse keeps the color");
            var c = on("MAT_SIMPLE_STANDARD", cut);
            Check(c != null && (string)c.args[0] == "Legacy Shaders/Transparent/Cutout/Diffuse", "a Cutout material: Legacy Cutout Diffuse");
            Check(on("MAT_SIMPLE_STANDARD", bumped) == null, "a material with a normal map is not simple");
            Check(on("MAT_SIMPLE_STANDARD", unsaved) == null, "a material made in code, not an asset: no row, there is nothing to switch");
            Check(simple.Count == 3, "three simple materials (" + simple.Count + ")");
            Check(!Recheck.SceneChecks.Contains("MAT_SIMPLE_STANDARD") && !Recheck.SceneChecks.Contains("TEX_NOT_TRILINEAR"),
                "both checks are re-checked with their assets");

            // Textures: a row per texture with its filter mode and a safe fix.
            var tri = result.issues.Where(i => i.checkId == "TEX_NOT_TRILINEAR").ToList();
            log.AppendLine("not trilinear: " + string.Join("; ", tri.Select(i => i.assetPath + " " + i.args[0]).ToArray()));
            var b = on("TEX_NOT_TRILINEAR", bilinear);
            Check(b != null && (string)b.args[0] == "Bilinear" && b.users == 2 && b.fixKind == FixKind.Safe && b.AutoFixable,
                "a Bilinear texture: its own row, two objects, a safe fix");
            var p = on("TEX_NOT_TRILINEAR", point);
            Check(p != null && (string)p.args[0] == "Point" && p.fixKind == FixKind.Review && p.AutoFixable,
                "a Point texture names Point; its fix is reviewed, not among the safe ones (sharp pixels may be meant)");
            Check(on("TEX_NOT_TRILINEAR", trilinear) == null && tri.Count == 2, "a Trilinear texture has no row (" + tri.Count + " rows)");

            // The shot shader: its own and built-in lines, more than 512 needed by the material.
            var uber = result.issues.FirstOrDefault(i => i.checkId == "SHADER_VARIANTS" && i.args.Length > 4 && (string)i.args[0] == "Demo/TerrainUber");
            log.AppendLine("shot shader: " + (uber != null ? uber.Message : "—"));
            Check(uber != null && uber.messageKey == "SHADER_VARIANTS" && (long)uber.args[3] > 512 && (long)uber.args[3] < (long)uber.args[1] &&
                  ((string)uber.args[4]).StartsWith("multi_compile _ _LAYERS_4 _LAYERS_8 ×3 · multi_compile _ _WIND ×2 · ", System.StringComparison.Ordinal) &&
                  ((string)uber.args[4]).EndsWith(" · +8", System.StringComparison.Ordinal),
                "a shader with shader_feature: the scene needs fewer than all, more than 512; its own multi_compile lines first, the rest +8");

            var applied = AutoFixer.Apply(new List<AuditIssue> { w, c, b });
            Check(applied == 3, "three fixes applied (" + applied + ")");
            Check(white.shader.name == "Mobile/Diffuse" && white.shaderKeywords.Length == 0 && white.mainTexture == bilinear &&
                  white.GetTextureScale("_MainTex") == new Vector2(3f, 2f),
                "the white material is Mobile/Diffuse without keywords, the texture and tiling stay");
            Check(cut.shader.name == "Legacy Shaders/Transparent/Cutout/Diffuse" && cut.renderQueue == (int)RenderQueue.AlphaTest && !cut.IsKeywordEnabled("_ALPHATEST_ON"),
                "the Cutout material is Legacy Cutout Diffuse and stays in the AlphaTest queue");
            var reopened = AssetDatabase.LoadAssetAtPath<Material>(AdviceDir + "/M_White.mat");
            Check(reopened != null && reopened.shader.name == "Mobile/Diffuse" && !EditorUtility.IsDirty(reopened), "the material asset is saved");
            Check(((TextureImporter)AssetImporter.GetAtPath(AdviceDir + "/T_Bilinear.png")).filterMode == FilterMode.Trilinear,
                "the Bilinear texture is Trilinear in its importer");

            var again = SceneAuditor.RunFullAudit(new VRBudgets(), false).issues;
            Check(!again.Any(i => i.checkId == "MAT_SIMPLE_STANDARD" && (i.target == white || i.target == cut)) &&
                  again.Any(i => i.checkId == "MAT_SIMPLE_STANDARD" && i.target == red),
                "after the fixes: the switched materials have no row, the red one stays");
            Check(!again.Any(i => i.checkId == "TEX_NOT_TRILINEAR" && i.target == bilinear) && again.Any(i => i.checkId == "TEX_NOT_TRILINEAR" && i.target == point),
                "after the fixes: the Bilinear texture has no row, the Point one stays");

            foreach (var language in new[] { Lang.En, Lang.Ru })
            {
                var lang = Strings.Current;
                Strings.Current = language;
                foreach (var key in new[] { "MAT_SIMPLE_STANDARD", "MAT_SIMPLE_STANDARD.fix", "MAT_SIMPLE_STANDARD.do", "MAT_SIMPLE_STANDARD_MODEL",
                                            "MAT_SIMPLE_STANDARD_MODEL.fix", "TEX_NOT_TRILINEAR", "TEX_NOT_TRILINEAR.fix", "TEX_NOT_TRILINEAR.do" })
                    Check(Strings.Has(key) && Strings.Get(key).Length > 0, language + " " + key);
                log.AppendLine(language + ": " + w.Message + " | " + w.Suggestion + " | " + w.FixDescription + " | " + b.Message);
                Strings.Current = lang;
            }
            // Several scenes: one shader row with the most a scene needs, as the scenes need different numbers.
            System.Func<long, string, AuditResult> part = (needed, key) => new AuditResult
            {
                sceneNames = new[] { "S" + needed }, scenePaths = new[] { AdviceDir + "/S" + needed + ".unity" },
                issues = new List<AuditIssue>
                {
                    new AuditIssue { checkId = "SHADER_VARIANTS", messageKey = key, category = Category.Shaders, severity = Severity.Info,
                        assetPath = AdviceDir + "/X.shader", users = 1, args = new object[] { "X", 4096L, 512, needed, "multi_compile _ A ×2" } }
                }
            };
            var parts = new List<AuditResult> { part(64, "SHADER_VARIANTS_FEW"), part(1024, "SHADER_VARIANTS") };
            var merged = AuditScope.Merge(parts, ScopeKind.BuildScenes).issues.Where(i => i.checkId == "SHADER_VARIANTS").ToList();
            Check(merged.Count == 1 && (long)merged[0].args[3] == 1024 && merged[0].messageKey == "SHADER_VARIANTS" && merged[0].inScenes == 2 &&
                  (long)parts[0].issues[0].args[3] == 64,
                "two scenes needing 64 and 1024 variants: one row with 1024 and the advice for many; the scene keeps its own");

            Object.DestroyImmediate(unsaved);
            // The folder scope of other steps reads the generated assets.
            NewScene("AdviceDone");
            AssetDatabase.DeleteAsset(AdviceDir);
        }
    }
}
