using System;
using System.Collections.Generic;
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
                var issue = ctx.Report("SHADER_ERROR", Category.Shaders, Severity.Critical, pair.Key, pair.Key.name);
                issue.messageKey = "SHADER_MISSING";
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
                    if (passes > 1) ShaderIssue(ctx, "SHADER_MULTIPASS", Severity.Warning, shader, users, passes);
                    if (instanced && source.VertexWithoutStereo) ShaderIssue(ctx, "SHADER_NO_STEREO", Severity.Critical, shader, users);
                    ulong variants;
                    if (VariantCount(shader, out variants) && variants > VRBudgets.ShaderVariantsInfo)
                        ShaderIssue(ctx, "SHADER_VARIANTS", Severity.Info, shader, users, (long)variants, VRBudgets.ShaderVariantsInfo);
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

        // ShaderUtil.GetVariantCount is internal in 2019.4; false when it cannot be called.
        static bool VariantCount(Shader shader, out ulong count)
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
                count = Convert.ToUInt64(variantCountMethod.Invoke(null, new object[] { shader, false }));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
