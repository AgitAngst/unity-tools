using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        // Objects that use a shader through their materials, and those of them with an alpha-tested material.
        sealed class ShaderUse
        {
            public readonly Shader shader;
            public readonly HashSet<Renderer> renderers = new HashSet<Renderer>();
            public readonly HashSet<Renderer> cutout = new HashSet<Renderer>();

            public ShaderUse(Shader shader) { this.shader = shader; }
        }

        const string ErrorShader = "Hidden/InternalErrorShader";

        static void AuditShaders(AuditContext ctx)
        {
            var uses = new List<ShaderUse>();
            var byShader = new Dictionary<Shader, ShaderUse>();
            // Materials whose shader is gone: Unity draws them with its error shader.
            var missing = new Dictionary<Material, int>();
            foreach (var renderer in ctx.All<Renderer>())
            {
                if (!AuditContext.IsActive(renderer)) continue;
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    if (material.shader == null || material.shader.name == ErrorShader)
                    {
                        int n;
                        missing.TryGetValue(material, out n);
                        missing[material] = n + 1;
                        continue;
                    }
                    ShaderUse use;
                    if (!byShader.TryGetValue(material.shader, out use))
                    {
                        use = new ShaderUse(material.shader);
                        byShader[material.shader] = use;
                        uses.Add(use);
                    }
                    use.renderers.Add(renderer);
                    if (material.GetTag("RenderType", false, "") == "TransparentCutout") use.cutout.Add(renderer);
                }
            }

            foreach (var pair in missing)
            {
                var issue = ctx.Report("SHADER_MISSING", Category.Shaders, Severity.Critical, pair.Key, pair.Key.name);
                issue.users = pair.Value;
            }
            bool instanced = ctx.xr.stereoMode == StereoMode.SinglePassInstanced;
            bool msaa = ctx.quality.antiAliasing > 1;
            foreach (var use in uses)
            {
                var shader = use.shader;
                int users = use.renderers.Count;
                if (ShaderUtil.ShaderHasError(shader) || !shader.isSupported)
                {
                    ShaderIssue(ctx, "SHADER_ERROR", Severity.Critical, shader, users);
                    continue;
                }
                var source = ShaderSource.Read(shader);
                if (source != null)
                {
                    if (source.grabUnnamed) ShaderIssue(ctx, "SHADER_GRABPASS", Severity.Warning, shader, users);
                    else if (source.grabPass) ShaderIssue(ctx, "SHADER_GRABPASS", Severity.Info, shader, users).messageKey = "SHADER_GRABPASS_NAMED";
                    int passes = source.DrawPasses;
                    // Every extra pass draws every object once more.
                    if (passes > 1) ShaderIssue(ctx, "SHADER_MULTIPASS", Severity.Warning, shader, users, passes).Gain(0, (long)(passes - 1) * users, 0, false);
                    if (instanced && source.VertexWithoutStereo) ShaderIssue(ctx, "SHADER_NO_STEREO", Severity.Critical, shader, users);
                    ulong variants, needed;
                    if (VariantCount(shader, false, out variants) && variants > VRBudgets.ShaderVariantsInfo)
                    {
                        // Without the shader_feature variants no material uses and the fog and instancing variants the
                        // Graphics stripping drops for the scene (the test sees both): close to what the build gets.
                        if (!VariantCount(shader, true, out needed)) needed = variants;
                        var issue = ShaderIssue(ctx, "SHADER_VARIANTS", Severity.Info, shader, users, (long)variants, VRBudgets.ShaderVariantsInfo,
                            (long)needed, Multipliers(source));
                        if (needed <= VRBudgets.ShaderVariantsInfo) issue.messageKey = "SHADER_VARIANTS_FEW";
                    }
                }
                // Built-in shaders do not use alpha to coverage.
                if (msaa && use.cutout.Count > 0 && (source == null || !source.alphaToMask))
                    ShaderIssue(ctx, "SHADER_ALPHA_TEST_MSAA", Severity.Info, shader, use.cutout.Count, ctx.quality.antiAliasing);
            }
        }

        // One issue per shader: its name first, the objects that use it counted.
        static AuditIssue ShaderIssue(AuditContext ctx, string checkId, Severity severity, Shader shader, int users, params object[] more)
        {
            var args = new object[more.Length + 1];
            // A shader that does not parse has no name: its file names it.
            args[0] = shader.name.Length > 0 ? shader.name : System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(shader));
            Array.Copy(more, 0, args, 1, more.Length);
            var issue = ctx.Report(checkId, Category.Shaders, severity, shader, args);
            issue.users = users;
            return issue;
        }

        static MethodInfo variantCountMethod;
        static bool variantCountLookedUp;

        const int MultipliersShown = 4;

        // The keyword lines that multiply the variants most, "—" when the source has none.
        static string Multipliers(ShaderSource source)
        {
            var lines = source.KeywordLines;
            if (lines.Count == 0) return "—";
            var text = string.Join(" · ", lines.Take(MultipliersShown).Select(l => l.Short).ToArray());
            return lines.Count > MultipliersShown ? text + " · +" + (lines.Count - MultipliersShown) : text;
        }

        // ShaderUtil.GetVariantCount is internal in 2019.4; false when it cannot be called. used: what the scene needs, without
        // the shader_feature variants no material uses (the "Skip unused shader_features" count of the shader Inspector).
        static bool VariantCount(Shader shader, bool used, out ulong count)
        {
            count = 0;
            if (!variantCountLookedUp)
            {
                variantCountLookedUp = true;
                variantCountMethod = ReflectionUtil.FindStaticMethod(typeof(ShaderUtil), "GetVariantCount", new[] { typeof(Shader), typeof(bool) });
            }
            if (variantCountMethod == null) return false;
            try
            {
                count = Convert.ToUInt64(variantCountMethod.Invoke(null, new object[] { shader, used }));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
