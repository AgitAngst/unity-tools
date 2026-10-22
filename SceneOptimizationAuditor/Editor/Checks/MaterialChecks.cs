using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        // Standard-shader keywords and the texture each one needs.
        static readonly string[][] KeywordTextures =
        {
            new[] { "_NORMALMAP", "_BumpMap", "_DetailNormalMap" },
            new[] { "_METALLICGLOSSMAP", "_MetallicGlossMap" },
            new[] { "_SPECGLOSSMAP", "_SpecGlossMap" },
            new[] { "_PARALLAXMAP", "_ParallaxMap" },
            new[] { "_DETAIL_MULX2", "_DetailAlbedoMap", "_DetailNormalMap" }
        };

        static readonly string[] ExtraStandardMaps =
        {
            "_BumpMap", "_MetallicGlossMap", "_SpecGlossMap", "_OcclusionMap", "_EmissionMap", "_ParallaxMap", "_DetailAlbedoMap", "_DetailNormalMap", "_DetailMask"
        };

        static void AuditMaterials(AuditContext ctx)
        {
            var users = new Dictionary<Material, List<Renderer>>();
            foreach (var renderer in ctx.All<Renderer>())
            {
                if (!AuditContext.IsActive(renderer)) continue;
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    List<Renderer> list;
                    if (!users.TryGetValue(material, out list))
                    {
                        list = new List<Renderer>();
                        users[material] = list;
                    }
                    if (!list.Contains(renderer)) list.Add(renderer);
                    if (material.name == "Default-Material" && !AuditContext.IsProjectAssetPath(AssetDatabase.GetAssetPath(material)))
                        ctx.Report("MAT_DEFAULT", Category.Materials, Severity.Warning, renderer);
                }
            }

            var signatures = new Dictionary<string, List<Material>>();
            int simpleStandard = 0;
            foreach (var pair in users)
            {
                var material = pair.Key;
                var list = pair.Value;
                bool editable = AuditContext.IsEditableAsset(material);

                if (material.renderQueue >= VRBudgets.TransparentQueue)
                {
                    float largest = list.Max(r => MaxSize(r.bounds));
                    var issue = ctx.Report("MAT_TRANSPARENT", Category.Materials,
                        largest > VRBudgets.TransparentLargeMeters ? Severity.Warning : Severity.Info, material, Math.Round(largest, 1));
                    issue.users = list.Count;
                }

                foreach (var rule in KeywordTextures)
                {
                    if (!material.IsKeywordEnabled(rule[0])) continue;
                    bool hasTexture = false;
                    for (int i = 1; i < rule.Length; i++)
                        if (material.HasProperty(rule[i]) && material.GetTexture(rule[i]) != null) hasTexture = true;
                    if (hasTexture) continue;
                    var issue = ctx.Report("MAT_KEYWORD_NO_TEXTURE", Category.Materials, Severity.Info, material, rule[0]);
                    issue.users = list.Count;
                    if (editable)
                    {
                        var keyword = rule[0];
                        var target = material;
                        issue.SetFix(FixKind.Review, target, true, () => target.DisableKeyword(keyword));
                    }
                }

                if (material.shader != null && material.shader.name == "Standard" && material.renderQueue < VRBudgets.TransparentQueue &&
                    !ExtraStandardMaps.Any(p => material.HasProperty(p) && material.GetTexture(p) != null) &&
                    !material.IsKeywordEnabled("_EMISSION"))
                    simpleStandard++;

                if (!editable) continue;
                var signature = Signature(material);
                List<Material> same;
                if (!signatures.TryGetValue(signature, out same))
                {
                    same = new List<Material>();
                    signatures[signature] = same;
                }
                same.Add(material);
            }

            foreach (var group in signatures.Values)
            {
                if (group.Count < 2) continue;
                // The material with the first path is kept, so the choice does not depend on scene order.
                var same = group.OrderBy(m => AssetDatabase.GetAssetPath(m), StringComparer.Ordinal).ToList();
                var keep = same[0];
                var others = same.Skip(1).ToList();
                var renderers = others.SelectMany(m => users[m]).Distinct().ToList();
                var issue = ctx.Report("MAT_DUPLICATE", Category.Materials, Severity.Info, keep, same.Count, string.Join(", ", same.Select(m => m.name).ToArray()));
                issue.users = same.Sum(m => users[m].Count);
                // Scene fix: every renderer of the copies uses the first material. Each renderer is recorded for Undo here.
                issue.SetFix(FixKind.Review, null, false, () =>
                {
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        Undo.RecordObject(renderer, "Scene Audit Fix");
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                            if (others.Contains(materials[i])) materials[i] = keep;
                        renderer.sharedMaterials = materials;
                        if (PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
                    }
                });
            }

            if (simpleStandard > 0)
                ctx.Report("MAT_SIMPLE_STANDARD", Category.Materials, Severity.Info, null, simpleStandard);
        }

        // Shader, render queue, instancing, keywords and every property value.
        static string Signature(Material material)
        {
            var sb = new StringBuilder();
            var shader = material.shader;
            sb.Append(shader != null ? shader.name : "-").Append('|').Append(material.renderQueue).Append('|').Append(material.enableInstancing).Append('|');
            var keywords = material.shaderKeywords.OrderBy(k => k, StringComparer.Ordinal).ToArray();
            sb.Append(string.Join(",", keywords)).Append('|');
            if (shader == null) return sb.ToString();
            int count = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < count; i++)
            {
                var name = ShaderUtil.GetPropertyName(shader, i);
                sb.Append(name).Append('=');
                switch (ShaderUtil.GetPropertyType(shader, i))
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        sb.Append(material.GetColor(name).ToString("F4"));
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        sb.Append(material.GetVector(name).ToString("F4"));
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        sb.Append(material.GetFloat(name).ToString("F4", CultureInfo.InvariantCulture));
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        var texture = material.GetTexture(name);
                        sb.Append(texture != null ? AssetDatabase.GetAssetPath(texture) + ":" + texture.name : "-")
                          .Append(material.GetTextureScale(name).ToString("F4")).Append(material.GetTextureOffset(name).ToString("F4"));
                        break;
                }
                sb.Append(';');
            }
            return sb.ToString();
        }
    }
}
