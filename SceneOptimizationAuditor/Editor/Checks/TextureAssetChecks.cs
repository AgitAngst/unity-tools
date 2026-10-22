using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        // Standard-shader slots that hold data, not colour: they must be linear (sRGB off).
        static readonly string[] DataSlots = { "_MetallicGlossMap", "_OcclusionMap", "_ParallaxMap", "_DetailMask" };
        static readonly string[] AlphaFormats = { "DXT5", "DXT5Crunched", "BC7", "RGBA32", "ARGB32", "BGRA32", "RGBA4444", "ARGB4444", "RGBAHalf", "RGBAFloat" };

        static void AuditTextureAssets(AuditContext ctx)
        {
            // Renderers and slot names of every texture used by scene materials.
            var renderers = new Dictionary<Texture, List<Renderer>>();
            var slots = new Dictionary<Texture, HashSet<string>>();
            foreach (var renderer in ctx.All<Renderer>())
            {
                if (!AuditContext.IsActive(renderer)) continue;
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null) continue;
                    int count = ShaderUtil.GetPropertyCount(material.shader);
                    for (int i = 0; i < count; i++)
                    {
                        if (ShaderUtil.GetPropertyType(material.shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                        var name = ShaderUtil.GetPropertyName(material.shader, i);
                        var texture = material.GetTexture(name);
                        if (texture == null) continue;
                        List<Renderer> list;
                        if (!renderers.TryGetValue(texture, out list))
                        {
                            list = new List<Renderer>();
                            renderers[texture] = list;
                            slots[texture] = new HashSet<string>();
                        }
                        if (!list.Contains(renderer)) list.Add(renderer);
                        slots[texture].Add(name);
                    }
                }
            }

            bool linear = PlayerSettings.colorSpace == ColorSpace.Linear;
            bool perTextureAniso = ctx.quality.anisotropicFiltering == AnisotropicFiltering.Enable;
            var files = new Dictionary<string, List<Texture>>();

            foreach (var pair in ctx.textures)
            {
                var texture = pair.Key;
                var use = pair.Value;
                var path = AssetDatabase.GetAssetPath(texture);
                var importer = AuditContext.IsProjectAssetPath(path) ? AssetImporter.GetAtPath(path) as TextureImporter : null;
                if (importer == null || use.IsBaked) continue;
                var texture2D = texture as Texture2D;
                int side = Mathf.Max(texture.width, texture.height);
                List<Renderer> users;
                renderers.TryGetValue(texture, out users);

                if (texture2D != null && importer.npotScale == TextureImporterNPOTScale.None &&
                    importer.textureType != TextureImporterType.Sprite && use.in3D &&
                    (!Mathf.IsPowerOfTwo(texture.width) || !Mathf.IsPowerOfTwo(texture.height)))
                {
                    var issue = ctx.Report("TEX_NPOT", Category.Textures, Severity.Info, texture, texture.width + "×" + texture.height);
                    issue.users = use.users;
                    issue.SetFix(FixKind.Review, importer, true, () =>
                    {
                        importer.npotScale = TextureImporterNPOTScale.ToNearest;
                        importer.SaveAndReimport();
                    });
                }

                if (users != null && users.Count > 0 && texture2D != null)
                {
                    float smallest = users.Min(r => MaxSize(r.bounds));
                    float largest = users.Max(r => MaxSize(r.bounds));
                    if (smallest > 0.001f && side >= VRBudgets.DensityHighMinSide && side / smallest > VRBudgets.DensityHighPxPerMeter)
                        ctx.Report("TEX_DENSITY_HIGH", Category.Textures, Severity.Info, texture, Math.Round(side / smallest), side, Math.Round(smallest, 2)).users = use.users;
                    else if (largest > VRBudgets.DensityLowMinMeters && side / largest < VRBudgets.DensityLowPxPerMeter)
                        ctx.Report("TEX_DENSITY_LOW", Category.Textures, Severity.Info, texture, Math.Round(side / largest, 1), side, Math.Round(largest, 1)).users = use.users;

                    if (perTextureAniso && importer.anisoLevel <= 1 && users.Any(IsLargeFlat))
                    {
                        var issue = ctx.Report("TEX_ANISO_LOW", Category.Textures, Severity.Info, texture, importer.anisoLevel);
                        issue.users = use.users;
                        issue.SetFix(FixKind.Review, importer, true, () =>
                        {
                            importer.anisoLevel = VRBudgets.AnisoLevelFix;
                            importer.SaveAndReimport();
                        });
                    }
                }

                HashSet<string> textureSlots;
                if (linear && importer.sRGBTexture && importer.textureType == TextureImporterType.Default &&
                    slots.TryGetValue(texture, out textureSlots) && textureSlots.Any(s => Array.IndexOf(DataSlots, s) >= 0))
                {
                    var slot = textureSlots.First(s => Array.IndexOf(DataSlots, s) >= 0);
                    var issue = ctx.Report("TEX_SRGB_DATA", Category.Textures, Severity.Warning, texture, slot);
                    issue.users = use.users;
                    issue.SetFix(FixKind.Review, importer, true, () =>
                    {
                        importer.sRGBTexture = false;
                        importer.SaveAndReimport();
                    });
                }

                // Normal maps keep data in alpha (DXT5nm), so only colour textures count.
                if (texture2D != null && importer.textureType == TextureImporterType.Default && importer.alphaSource == TextureImporterAlphaSource.FromInput &&
                    Array.IndexOf(AlphaFormats, texture2D.format.ToString()) >= 0 && AlphaFullyOpaque(path))
                {
                    var issue = ctx.Report("TEX_ALPHA_UNUSED", Category.Textures, Severity.Info, texture, texture2D.format.ToString());
                    issue.users = use.users;
                    issue.SetFix(FixKind.Review, importer, true, () =>
                    {
                        importer.alphaSource = TextureImporterAlphaSource.None;
                        importer.SaveAndReimport();
                    });
                }

                if (QualitySettings.streamingMipmapsActive && texture2D != null && side >= VRBudgets.StreamingMinSide &&
                    importer.mipmapEnabled && !importer.streamingMipmaps)
                {
                    var issue = ctx.Report("TEX_STREAMING_OFF", Category.Textures, Severity.Info, texture, side);
                    issue.users = use.users;
                    issue.SetFix(FixKind.Review, importer, true, () =>
                    {
                        importer.streamingMipmaps = true;
                        importer.SaveAndReimport();
                    });
                }

                var hash = FileHash(path);
                if (hash == null) continue;
                List<Texture> same;
                if (!files.TryGetValue(hash, out same))
                {
                    same = new List<Texture>();
                    files[hash] = same;
                }
                same.Add(texture);
            }

            foreach (var same in files.Values)
            {
                if (same.Count < 2) continue;
                ctx.Report("TEX_DUPLICATE", Category.Textures, Severity.Info, same[0], same.Count,
                    string.Join(", ", same.Select(t => Path.GetFileName(AssetDatabase.GetAssetPath(t))).ToArray()));
            }
        }

        static float MaxSize(Bounds bounds)
        {
            return Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        }

        // Floors and walls: large in two directions, thin in the third.
        static bool IsLargeFlat(Renderer renderer)
        {
            var size = renderer.bounds.size;
            float max = Mathf.Max(size.x, size.y, size.z);
            float min = Mathf.Min(size.x, size.y, size.z);
            return max > VRBudgets.LargeFlatMinMeters && min < max * VRBudgets.LargeFlatThinShare;
        }

        static string FileHash(string path)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(path))
                    return BitConverter.ToString(md5.ComputeHash(stream));
            }
            catch (Exception)
            {
                return null;
            }
        }

        // PNG only: JPG has no alpha, other formats cannot be decoded without the importer.
        static bool AlphaFullyOpaque(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".png") return false;
            try
            {
                var info = new FileInfo(path);
                if (info.Length > VRBudgets.AlphaScanMaxBytes) return false;
                var probe = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!probe.LoadImage(File.ReadAllBytes(path))) return false;
                    foreach (var pixel in probe.GetPixels32())
                        if (pixel.a != 255) return false;
                    return true;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(probe);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
