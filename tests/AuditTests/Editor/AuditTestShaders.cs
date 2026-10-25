using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Shader checks on shaders the test writes into the project, on Standard, and the parser on short sources.
    public static partial class AuditTestViewpoints
    {
        const string ShaderDir = AuditTestAssets.Root + "/Shaders";

        static readonly string[] ShaderChecks =
        {
            "SHADER_ERROR", "SHADER_GRABPASS", "SHADER_MULTIPASS", "SHADER_NO_STEREO", "SHADER_VARIANTS", "SHADER_ALPHA_TEST_MSAA"
        };

        public static void RunShaders()
        {
            Run("shaders", Shaders);
        }

        static void Shaders()
        {
            ShaderParse();
            ShaderIssues();
            ShaderStrings();
        }

        // A drawing pass with the stereo macros: tags, render state and extra pragmas of its own.
        static string StereoPass(string tags, string state = "", string pragmas = "")
        {
            return @"
        Pass
        {
            " + tags + @"
            " + state + @"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            " + pragmas + @"
            #include ""UnityCG.cginc""
            struct appdata { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert (appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.vertex = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag (v2f i) : SV_Target { return fixed4(1, 1, 1, 1); }
            ENDCG
        }";
        }

        // The Unlit Shader template of Unity 2019.4: no stereo macros.
        const string UnlitTemplatePass = @"
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include ""UnityCG.cginc""
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) float4 vertex : SV_POSITION; };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }";

        static string ShaderText(string name, string subShaderTags, string body)
        {
            return "Shader \"AuditTest/" + name + "\"\n{\n    Properties { _MainTex (\"Texture\", 2D) = \"white\" {} }\n    SubShader\n    {\n        " +
                   subShaderTags + "\n" + body + "\n    }\n}\n";
        }

        static Shader WriteShader(string name, string text)
        {
            Directory.CreateDirectory(ShaderDir);
            var path = ShaderDir + "/" + name + ".shader";
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Shader>(path);
        }

        static Renderer ShaderObject(string name, Material material)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return r;
        }

        // Passes, GrabPass, AlphaToMask and the vertex functions read from sources of a few lines.
        static void ShaderParse()
        {
            var commented = ShaderSource.Parse("Shader \"A\" { SubShader { // Pass { }\n /* GrabPass { } Pass { } */ Pass { } } }", null);
            Check(commented.passes.Count == 1 && !commented.grabPass, "passes in comments do not count");
            var vertexLit = ShaderSource.Parse("Shader \"A\" { SubShader { Pass { Tags { \"LightMode\" = \"Vertex\" } } Pass { Tags { \"LightMode\" = \"VertexLMRGBM\" } } Pass { Tags { \"LightMode\" = \"VertexLM\" } } } }", null);
            Check(vertexLit.DrawPasses == 1, "Vertex, VertexLM and VertexLMRGBM are one pass for an object (" + vertexLit.DrawPasses + ")");
            var surface = ShaderSource.Parse("Shader \"A\" { SubShader { CGPROGRAM\n#pragma surface surf Standard\nvoid surf() { }\nENDCG\n Pass { Name \"OUT{LINE\" } } }", null);
            Check(surface.surface && surface.DrawPasses == 2, "a surface shader is one pass, an untagged pass named with a brace another");
            var use = ShaderSource.Parse("Shader \"A\" { SubShader { Pass { Tags { \"LightMode\" = \"ForwardBase\" } } UsePass \"Legacy Shaders/VertexLit/SHADOWCASTER\" UsePass \"Toon/Basic Outline/OUTLINE\" } }", null);
            Check(use.passes.Count == 3 && use.DrawPasses == 2, "UsePass of a shadow caster does not draw, UsePass of an outline does");
            var category = ShaderSource.Parse("Shader \"A\" { Category { SubShader { Pass { } Pass { } } SubShader { Pass { } } } }", null);
            Check(category.DrawPasses == 2, "the first SubShader inside a Category");
            var grab = ShaderSource.Parse("Shader \"A\" { SubShader { GrabPass { Tags { \"LightMode\" = \"Always\" } } GrabPass { \"_Bg\" } } }", null);
            Check(grab.grabPass && grab.grabUnnamed && grab.passes.Count == 0, "GrabPass without a name; GrabPass is no drawing pass");
            var named = ShaderSource.Parse("Shader \"A\" { SubShader { GrabPass { Name \"G\" \"_Bg\" } AlphaToMask [_A2C] } }", null);
            Check(named.grabPass && !named.grabUnnamed && named.alphaToMask, "a named GrabPass; AlphaToMask from a property");
            var template = ShaderSource.Parse(ShaderText("T", "", UnlitTemplatePass), null);
            Check(template.vertexFunctions.SequenceEqual(new[] { "vert" }) && template.VertexWithoutStereo && template.DrawPasses == 1,
                "the Unlit template: one pass, vertex function vert without the stereo macros");
            var stereo = ShaderSource.Parse(ShaderText("S", "", StereoPass("")), null);
            Check(!stereo.VertexWithoutStereo, "a pass with the stereo macros");
            var img = ShaderSource.Parse("Shader \"A\" { SubShader { Pass { CGPROGRAM\n#pragma vertex vert_img\n#pragma fragment frag\n#include \"UnityCG.cginc\"\nfixed4 frag (v2f_img i) : SV_Target { return 1; }\nENDCG } } }", null);
            Check(!img.VertexWithoutStereo, "vert_img of UnityCG.cginc is not the project's own vertex function");
        }

        static void ShaderIssues()
        {
            NewScene("Shaders");
            Directory.CreateDirectory(ShaderDir);
            File.WriteAllText(ShaderDir + "/AuditVert.cginc",
                "struct appdata { float4 vertex : POSITION; };\nstruct v2f { float4 vertex : SV_POSITION; };\nv2f vertInc (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); return o; }\n");
            AssetDatabase.ImportAsset(ShaderDir + "/AuditVert.cginc", ImportAssetOptions.ForceSynchronousImport);

            var shaders = new Dictionary<string, Shader>
            {
                { "Good", WriteShader("Good", ShaderText("Good", "", StereoPass(""))) },
                { "Grab", WriteShader("Grab", ShaderText("Grab", "Tags { \"Queue\" = \"Transparent\" }", "        GrabPass { }" + StereoPass(""))) },
                { "GrabNamed", WriteShader("GrabNamed", ShaderText("GrabNamed", "Tags { \"Queue\" = \"Transparent\" }", "        GrabPass { \"_Bg\" }" + StereoPass(""))) },
                { "Outline", WriteShader("Outline", ShaderText("Outline", "", StereoPass("") + StereoPass("", "Cull Front"))) },
                { "Forward", WriteShader("Forward", ShaderText("Forward", "",
                    StereoPass("Tags { \"LightMode\" = \"ForwardBase\" }") + StereoPass("Tags { \"LightMode\" = \"ForwardAdd\" }", "Blend One One") +
                    StereoPass("Tags { \"LightMode\" = \"ShadowCaster\" }") + "\n        UsePass \"Legacy Shaders/VertexLit/SHADOWCASTER\"")) },
                { "UnlitTemplate", WriteShader("UnlitTemplate", ShaderText("UnlitTemplate", "Tags { \"RenderType\" = \"Opaque\" }", UnlitTemplatePass)) },
                { "IncludeVert", WriteShader("IncludeVert", ShaderText("IncludeVert", "",
                    "        Pass\n        {\n            CGPROGRAM\n            #pragma vertex vertInc\n            #pragma fragment frag\n            #include \"UnityCG.cginc\"\n            #include \"AuditVert.cginc\"\n            fixed4 frag (v2f i) : SV_Target { return 1; }\n            ENDCG\n        }")) },
                { "Cutout", WriteShader("Cutout", ShaderText("Cutout", "Tags { \"RenderType\" = \"TransparentCutout\" \"Queue\" = \"AlphaTest\" }", StereoPass("", "AlphaToMask On"))) },
                { "CutoutNoMask", WriteShader("CutoutNoMask", ShaderText("CutoutNoMask", "Tags { \"RenderType\" = \"TransparentCutout\" \"Queue\" = \"AlphaTest\" }", StereoPass(""))) },
                { "Variants", WriteShader("Variants", ShaderText("Variants", "", StereoPass("", "",
                    string.Join("\n            ", Enumerable.Range(0, 10).Select(i => "#pragma multi_compile _ AUDIT_KW_" + i).ToArray())))) },
                { "Broken", WriteShader("Broken", "Shader \"AuditTest/Broken\" { SubShader { Pass { Blend Wrong Wrong } } }\n") },
            };
            foreach (var pair in shaders)
            {
                Check(pair.Value != null, "shader " + pair.Key + " is imported");
                if (pair.Value != null) ShaderObject("Uses_" + pair.Key, new Material(pair.Value));
            }
            ShaderObject("Uses_Good_2", new Material(shaders["Good"]));
            ShaderObject("Uses_Grab_2", new Material(shaders["Grab"]));

            var standard = Shader.Find("Standard");
            ShaderObject("Standard_Opaque", new Material(standard));
            var cut = new Material(standard);
            cut.SetFloat("_Mode", 1f);
            cut.SetOverrideTag("RenderType", "TransparentCutout");
            cut.EnableKeyword("_ALPHATEST_ON");
            cut.renderQueue = 2450;
            ShaderObject("Standard_Cutout", cut);

            // A material asset whose shader file is deleted.
            var gone = WriteShader("Gone", ShaderText("Gone", "", StereoPass("")));
            var orphanPath = ShaderDir + "/M_Orphan.mat";
            AssetDatabase.CreateAsset(new Material(gone), orphanPath);
            AssetDatabase.DeleteAsset(ShaderDir + "/Gone.shader");
            var orphan = AssetDatabase.LoadAssetAtPath<Material>(orphanPath);
            ShaderObject("Uses_Orphan", orphan);

            AuditTestSettings.SetLegacyVr(new[] { "OpenVR" }, StereoRenderingPath.Instancing);
            AuditResult result;
            try
            {
                result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            }
            finally
            {
                AuditTestSettings.ApplyBaseline();
            }
            var issues = result.issues.Where(i => i.category == Category.Shaders).ToList();
            System.Func<string, string, List<AuditIssue>> of = (id, name) =>
                issues.Where(i => i.checkId == id && i.args.Length > 0 && (string)i.args[0] == "AuditTest/" + name).ToList();
            System.Func<string, List<AuditIssue>> on = name => issues.Where(i => i.args.Length > 0 && (string)i.args[0] == "AuditTest/" + name).ToList();
            log.AppendLine("shader issues: " + string.Join(", ", issues.Select(i => i.checkId + " " + (i.args.Length > 0 ? i.args[0] : "")).ToArray()));

            Check(on("Good").Count == 0 && on("Forward").Count == 0 && on("Cutout").Count == 0,
                "a good pass, ForwardBase + ForwardAdd + ShadowCaster + UsePass shadow, cutout with AlphaToMask: no shader issues");
            var onStandard = issues.Where(i => i.target == standard).ToList();
            Check(onStandard.Count == 1 && onStandard[0].checkId == "SHADER_ALPHA_TEST_MSAA" && onStandard[0].users == 1,
                "Standard: only the cutout material with MSAA, one object");
            var grab = of("SHADER_GRABPASS", "Grab");
            Check(grab.Count == 1 && grab[0].severity == Severity.Warning && grab[0].users == 2 && grab[0].target == shaders["Grab"] &&
                  grab[0].assetPath == ShaderDir + "/Grab.shader",
                "GrabPass without a name: Warning, 2 objects, the shader asset");
            var grabNamed = of("SHADER_GRABPASS", "GrabNamed");
            Check(grabNamed.Count == 1 && grabNamed[0].severity == Severity.Info && grabNamed[0].messageKey == "SHADER_GRABPASS_NAMED", "named GrabPass: Info");
            var multi = of("SHADER_MULTIPASS", "Outline");
            Check(multi.Count == 1 && (int)multi[0].args[1] == 2, "an outline in a second pass: 2 passes");
            Check(of("SHADER_NO_STEREO", "UnlitTemplate").Count == 1 && of("SHADER_NO_STEREO", "UnlitTemplate")[0].severity == Severity.Critical,
                "the Unlit template with Single Pass Instanced: Critical");
            Check(of("SHADER_NO_STEREO", "IncludeVert").Count == 1, "a vertex function from a .cginc of the project without the macros");
            Check(issues.Count(i => i.checkId == "SHADER_NO_STEREO") == 2, "no stereo: only the template and the include (" +
                string.Join(", ", issues.Where(i => i.checkId == "SHADER_NO_STEREO").Select(i => (string)i.args[0]).ToArray()) + ")");
            var alpha = of("SHADER_ALPHA_TEST_MSAA", "CutoutNoMask");
            Check(alpha.Count == 1 && (int)alpha[0].args[1] == 4, "cutout without AlphaToMask at MSAA 4x: Info");
            var variants = of("SHADER_VARIANTS", "Variants");
            Check(variants.Count == 1 && (long)variants[0].args[1] >= 1024, "10 multi_compile: 1024 variants or more (" +
                (variants.Count > 0 ? variants[0].args[1] : "—") + ")");
            var broken = issues.Where(i => i.checkId == "SHADER_ERROR").ToList();
            var parse = broken.FirstOrDefault(i => i.target == shaders["Broken"]);
            Check(parse != null && broken.All(i => i.severity == Severity.Critical) && ((string)parse.args[0]).Length > 0,
                "a shader with a parse error: SHADER_ERROR, Critical, named (" + (parse != null ? parse.args[0] : "—") + ")");
            var missing = broken.Where(i => i.messageKey == "SHADER_MISSING").ToList();
            Check(missing.Count == 1 && missing[0].target == orphan && missing[0].assetPath == orphanPath, "a material whose shader is deleted: SHADER_MISSING on the material");

            // Not Single Pass Instanced, MSAA off: no stereo and no alpha-test issues.
            QualitySettings.SetQualityLevel(AuditTestSettings.StandaloneLevel, false);
            QualitySettings.antiAliasing = 0;
            var plain = SceneAuditor.RunFullAudit(new VRBudgets(), false).issues.Where(i => i.category == Category.Shaders).ToList();
            QualitySettings.antiAliasing = 4;
            Check(plain.All(i => i.checkId != "SHADER_NO_STEREO" && i.checkId != "SHADER_ALPHA_TEST_MSAA") && plain.Any(i => i.checkId == "SHADER_GRABPASS"),
                "Multi Pass and no MSAA: no stereo or alpha-test issues, the others stay");
        }

        static void ShaderStrings()
        {
            var missing = new List<string>();
            var lang = Strings.Current;
            foreach (var language in new[] { Lang.En, Lang.Ru })
            {
                Strings.Current = language;
                foreach (var id in ShaderChecks.Concat(new[] { "SHADER_MISSING", "SHADER_GRABPASS_NAMED" }))
                    foreach (var key in id.Contains("MISSING") || id.Contains("NAMED") ? new[] { id, id + ".fix" } : new[] { id, id + ".title", id + ".fix" })
                        if (!Strings.Has(key) || Strings.Get(key).Length == 0) missing.Add(language + " " + key);
            }
            Strings.Current = lang;
            Check(missing.Count == 0 && Strings.CategoryName(Category.Shaders) != "cat.Shaders",
                "every shader check has a title, a message and how to fix" + (missing.Count > 0 ? ": " + string.Join(", ", missing.ToArray()) : ""));
        }
    }
}
