using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        static readonly string[] UncompressedFormats =
        {
            "RGB24", "RGBA32", "ARGB32", "BGRA32", "RGB16", "RGBA16", "ARGB16", "R16", "RG16",
            "RHalf", "RGHalf", "RGBAHalf", "RFloat", "RGFloat", "RGBAFloat", "RGB48", "RGBA64"
        };

        static void AuditTextures(AuditContext ctx)
        {
            var b = ctx.budgets;
            double totalBytes = 0;
            int notTrilinear = 0;

            foreach (var pair in ctx.textures)
            {
                var texture = pair.Key;
                var use = pair.Value;
                var path = AssetDatabase.GetAssetPath(texture);
                var importer = AuditContext.IsProjectAssetPath(path) ? AssetImporter.GetAtPath(path) as TextureImporter : null;
                totalBytes += TextureMemory.EstimateBytes(texture, importer != null && importer.isReadable);
                if (importer == null) continue;

                var settings = EffectiveSettings(importer);
                bool known = use.in3D || use.inWorldUI || use.inScreenUI || use.skybox;

                if (!use.IsBaked)
                {
                    int size = Mathf.Max(texture.width, texture.height);
                    if (settings.maxTextureSize > 0 && settings.maxTextureSize < size) size = settings.maxTextureSize;
                    bool cube = texture is Cubemap || importer.textureShape == TextureImporterShape.TextureCube || use.skybox;
                    // The gain: down to the Warning threshold.
                    if (size > b.textureSizeCritical)
                        ctx.Report("TEX_SIZE", Category.Textures, cube ? Severity.Warning : Severity.Critical, texture, size, b.textureSizeCritical)
                            .Gain(FixGain.ResizedMB(texture, b.textureSizeWarning), 0, 0, false).users = use.users;
                    else if (size > b.textureSizeWarning)
                        ctx.Report("TEX_SIZE", Category.Textures, Severity.Warning, texture, size, b.textureSizeWarning)
                            .Gain(FixGain.ResizedMB(texture, b.textureSizeWarning), 0, 0, false).users = use.users;

                    if (use.normalSlot && importer.textureType != TextureImporterType.NormalMap)
                    {
                        var issue = ctx.Report("TEX_NOT_NORMAL_MAP", Category.Textures, Severity.Critical, texture);
                        issue.users = use.users;
                        issue.SetFix(FixKind.Review, importer, true, () =>
                        {
                            importer.textureType = TextureImporterType.NormalMap;
                            importer.SaveAndReimport();
                        });
                    }

                    // Mipmaps matter for 3D and world-space UI; a skybox is always at the same distance.
                    bool needsMips = (use.in3D || use.inWorldUI) && !use.OnlyScreenUI;
                    if (needsMips && !importer.mipmapEnabled)
                    {
                        var issue = ctx.Report("TEX_NO_MIPMAPS", Category.Textures, Severity.Warning, texture);
                        issue.users = use.users;
                        issue.SetFix(FixKind.Safe, importer, true, () =>
                        {
                            importer.mipmapEnabled = true;
                            importer.SaveAndReimport();
                        });
                    }

                    if (known && IsUncompressed(settings))
                    {
                        var suggested = SuggestFormat(importer, settings, path);
                        var formatName = settings.overridden ? settings.format.ToString() : "Uncompressed";
                        var issue = ctx.Report("TEX_UNCOMPRESSED", Category.Textures, Severity.Warning, texture, formatName, suggested.ToString());
                        issue.users = use.users;
                        issue.Gain(FixGain.ReformattedMB(texture, suggested.ToString()), 0, 0, false);
                        issue.SetFix(FixKind.Review, importer, true, () =>
                        {
                            var current = EffectiveSettings(importer);
                            if (current.overridden)
                            {
                                current.format = suggested;
                                importer.SetPlatformTextureSettings(current);
                            }
                            else
                            {
                                importer.textureCompression = TextureImporterCompression.Compressed;
                            }
                            importer.SaveAndReimport();
                        });
                    }

                    if (use.in3D && importer.mipmapEnabled && importer.filterMode != FilterMode.Trilinear) notTrilinear++;
                }

                if (importer.isReadable)
                {
                    var issue = ctx.Report("TEX_READ_WRITE", Category.Textures, Severity.Warning, texture);
                    issue.users = use.users;
                    // The copy kept in CPU memory.
                    issue.Gain(FixGain.TextureMB(texture), 0, 0, false);
                    issue.SetFix(FixKind.Review, importer, true, () =>
                    {
                        importer.isReadable = false;
                        importer.SaveAndReimport();
                    });
                }
            }

            ctx.textureMemoryMB = totalBytes / (1024.0 * 1024.0);
            Budget(ctx, "TEX_MEMORY", Category.Textures, ctx.textureMemoryMB, b.textureMemoryWarningMB, b.textureMemoryCriticalMB);

            if (ctx.quality.anisotropicFiltering == AnisotropicFiltering.Disable)
                ctx.ReportSetting("TEX_ANISO_DISABLED", Category.Textures, Severity.Info, "Project/Quality");
            if (notTrilinear > 0)
                ctx.Report("TEX_NOT_TRILINEAR", Category.Textures, Severity.Info, null, notTrilinear);

            AuditTextureAssets(ctx);
        }

        // Standalone override when set, otherwise the default platform settings.
        static TextureImporterPlatformSettings EffectiveSettings(TextureImporter importer)
        {
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            return standalone.overridden ? standalone : importer.GetDefaultPlatformTextureSettings();
        }

        static bool IsUncompressed(TextureImporterPlatformSettings settings)
        {
            if (settings.overridden) return Array.IndexOf(UncompressedFormats, settings.format.ToString()) >= 0;
            return settings.textureCompression == TextureImporterCompression.Uncompressed;
        }

        static TextureImporterFormat SuggestFormat(TextureImporter importer, TextureImporterPlatformSettings settings, string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var format = settings.format.ToString();
            if (ext == ".exr" || ext == ".hdr" || format.Contains("Half") || format.Contains("Float")) return TextureImporterFormat.BC6H;
            if (importer.textureType == TextureImporterType.NormalMap) return TextureImporterFormat.BC5;
            bool alpha = importer.alphaSource != TextureImporterAlphaSource.None && importer.DoesSourceTextureHaveAlpha();
            return alpha ? TextureImporterFormat.BC7 : TextureImporterFormat.DXT1;
        }
    }
}
