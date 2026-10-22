using System;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

namespace AuditTestsEditor
{
    // Creates the test assets once and resets their import settings on every run,
    // so fixes applied by an earlier run do not leak into the next one.
    public static class AuditTestAssets
    {
        public const string Root = "Assets/AuditGenerated";

        public static Material Sky, Plain, NoInst, NoInst3, Big4096, Big8192, Small1024, FakeNormal, Normal, NoMips, Raw, Rgba32, Readable;
        public static Texture2D TOverlayOnly;
        public static Cubemap TCube;
        public static Mesh ObjAnimated, Channels, Split, Degenerate, DupA, DupB, FarPivot, ObjIndex32, ObjBig, ObjScaled, ObjEmbedded;
        public static Material Dense, Wall, Metal, MatDupA, MatDupB, Fade, FadeSmall, KeyNormal, Npot, TexDupA, TexDupB, AlphaFull;
        public static Mesh Uv1Good, Uv1Outside, Uv1Overlap, ObjLmNoUv, ObjLmGen;
        public static Mesh Grid12k, Grid30k, Grid16kSkin, TwoSub, Skin8, ObjReadWrite, ObjReadWriteCollider, ObjNotOptimized, ObjClean;
        public static AudioClip A35, A15Pcm, A5;
        public static PostProcessProfile ProfileOn, ProfileOff;
        public static RenderTexture TargetTexture;

        public static void Prepare()
        {
            foreach (var dir in new[] { "", "/Materials", "/Textures", "/Meshes", "/Models", "/Audio", "/Misc", "/Scenes", "/Prefabs" })
                Directory.CreateDirectory(Root + dir);
            AssetDatabase.Refresh();

            var t4096 = Tex("T_4096", 4096, 4096, false, i => i.maxTextureSize = 4096);
            var t8192 = Tex("T_8192", 8192, 8192, false, i => i.maxTextureSize = 8192);
            var t1024 = Tex("T_1024", 1024, 1024, false, i => { });
            var tFakeNormal = Tex("T_FakeNormal", 256, 256, false, i => { });
            var tNormal = Tex("T_Normal", 256, 256, false, i => i.textureType = TextureImporterType.NormalMap);
            var tNoMips = Tex("T_NoMips", 256, 256, false, i => i.mipmapEnabled = false);
            TOverlayOnly = Tex("T_OverlayOnly", 256, 256, false, i => i.mipmapEnabled = false);
            var tRaw = Tex("T_Raw", 256, 256, false, i => i.textureCompression = TextureImporterCompression.Uncompressed);
            var tRgba = Tex("T_Rgba32", 256, 256, true, i =>
            {
                var s = i.GetPlatformTextureSettings("Standalone");
                s.overridden = true;
                s.format = TextureImporterFormat.RGBA32;
                s.maxTextureSize = 2048;
                i.SetPlatformTextureSettings(s);
            });
            var tReadable = Tex("T_Readable", 256, 256, false, i => i.isReadable = true);
            var tSmall = Tex("T_Small", 128, 128, false, i => { });
            Tex("T_Cube", 6144, 1024, false, i =>
            {
                i.textureShape = TextureImporterShape.TextureCube;
                i.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            });
            TCube = AssetDatabase.LoadAssetAtPath<Cubemap>(Root + "/Textures/T_Cube.png");

            var skyPath = Root + "/Materials/M_Sky.mat";
            Sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (Sky == null)
            {
                Sky = new Material(Shader.Find("Skybox/Cubemap"));
                AssetDatabase.CreateAsset(Sky, skyPath);
            }
            Sky.SetTexture("_Tex", TCube);
            EditorUtility.SetDirty(Sky);

            var tNpot = Tex("T_Npot", 300, 200, false, i => i.npotScale = TextureImporterNPOTScale.None);
            var tDupA = Tex("T_DupA", 64, 64, false, i => { }, "dup");
            var tDupB = Tex("T_DupB", 64, 64, false, i => { }, "dup");
            var tDense = Tex("T_Dense", 2048, 2048, false, i => { });
            var tWall = Tex("T_Wall", 128, 128, false, i => { });
            var tAlphaFull = Tex("T_AlphaFull", 256, 256, false, i =>
            {
                var s = i.GetPlatformTextureSettings("Standalone");
                s.overridden = true;
                s.format = TextureImporterFormat.DXT5;
                i.SetPlatformTextureSettings(s);
            });
            var tMetal = Tex("T_Metal", 256, 256, false, i => { });
            Dense = Mat("M_Dense", m => m.mainTexture = tDense);
            Wall = Mat("M_Wall", m => m.mainTexture = tWall);
            Metal = Mat("M_Metal", m =>
            {
                m.SetTexture("_MetallicGlossMap", tMetal);
                m.EnableKeyword("_METALLICGLOSSMAP");
            });
            MatDupA = Mat("M_DupA", m => { m.mainTexture = tSmall; m.enableInstancing = false; m.color = new Color(0.5f, 0.6f, 0.7f); });
            MatDupB = Mat("M_DupB", m => { m.mainTexture = tSmall; m.enableInstancing = false; m.color = new Color(0.5f, 0.6f, 0.7f); });
            Fade = Mat("M_Fade", m => { m.SetFloat("_Mode", 2f); m.renderQueue = 3000; m.EnableKeyword("_ALPHABLEND_ON"); });
            FadeSmall = Mat("M_FadeSmall", m => { m.color = new Color(1f, 1f, 1f, 0.5f); m.SetFloat("_Mode", 2f); m.renderQueue = 3000; m.EnableKeyword("_ALPHABLEND_ON"); });
            KeyNormal = Mat("M_KeyNormal", m => m.EnableKeyword("_NORMALMAP"));
            Npot = Mat("M_Npot", m => m.mainTexture = tNpot);
            TexDupA = Mat("M_TexDupA", m => m.mainTexture = tDupA);
            TexDupB = Mat("M_TexDupB", m => m.mainTexture = tDupB);
            AlphaFull = Mat("M_AlphaFull", m => m.mainTexture = tAlphaFull);

            Plain = Mat("M_Plain", m => { m.mainTexture = tSmall; m.enableInstancing = true; });
            NoInst = Mat("M_NoInst", m => { m.mainTexture = tSmall; m.enableInstancing = false; });
            NoInst3 = Mat("M_NoInst3", m => { m.mainTexture = tSmall; m.enableInstancing = false; m.color = new Color(0.9f, 0.9f, 0.9f); });
            Big4096 = Mat("M_Big4096", m => m.mainTexture = t4096);
            Big8192 = Mat("M_Big8192", m => m.mainTexture = t8192);
            Small1024 = Mat("M_1024", m => m.mainTexture = t1024);
            FakeNormal = Mat("M_FakeNormal", m => { m.mainTexture = tSmall; m.SetTexture("_BumpMap", tFakeNormal); });
            Normal = Mat("M_Normal", m => { m.mainTexture = tSmall; m.SetTexture("_BumpMap", tNormal); });
            NoMips = Mat("M_NoMips", m => m.mainTexture = tNoMips);
            Raw = Mat("M_Raw", m => m.mainTexture = tRaw);
            Rgba32 = Mat("M_Rgba32", m => m.mainTexture = tRgba);
            Readable = Mat("M_Readable", m => m.mainTexture = tReadable);

            Grid12k = MeshAsset("Grid12k", () => Grid(60, 100));
            Grid30k = MeshAsset("Grid30k", () => Grid(100, 150));
            Grid16kSkin = MeshAsset("Grid16kSkin", () =>
            {
                var m = Grid(80, 100);
                var weights = new BoneWeight[m.vertexCount];
                for (int i = 0; i < weights.Length; i++) weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1f };
                m.boneWeights = weights;
                m.bindposes = new[] { Matrix4x4.identity };
                return m;
            });
            TwoSub = MeshAsset("TwoSub", BuildTwoSubmeshes);
            Skin8 = MeshAsset("Skin8", BuildSkin8);

            ObjReadWrite = Model("ReadWrite", true, MeshOptimizationFlags.Everything);
            ObjReadWriteCollider = Model("ReadWriteCollider", true, MeshOptimizationFlags.Everything);
            ObjNotOptimized = Model("NotOptimized", false, MeshOptimizationFlags.PolygonOrder);
            ObjClean = Model("Clean", false, MeshOptimizationFlags.Everything);
            ObjLmNoUv = Model("LmNoUv", false, MeshOptimizationFlags.Everything);
            ObjLmGen = Model("LmGen", false, MeshOptimizationFlags.Everything, true);
            ObjIndex32 = Model("Index32", false, MeshOptimizationFlags.Everything, false, i => i.indexFormat = ModelImporterIndexFormat.UInt32);
            ObjBig = Model("Big12k", false, MeshOptimizationFlags.Everything, false, null, GridObj(60, 100));
            ObjAnimated = Model("Animated", false, MeshOptimizationFlags.Everything, false, i =>
            {
                i.animationType = ModelImporterAnimationType.Generic;
                i.importAnimation = true;
            });
            ObjScaled = Model("Scaled", false, MeshOptimizationFlags.Everything, false, i => i.globalScale = 0.01f);
            ObjEmbedded = Model("Embedded", false, MeshOptimizationFlags.Everything, false, i =>
            {
                i.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                i.materialLocation = ModelImporterMaterialLocation.InPrefab;
            });

            Channels = MeshAsset("Channels", () =>
            {
                var m = Grid(10, 10);
                m.colors = Enumerable.Repeat(Color.white, m.vertexCount).ToArray();
                m.RecalculateTangents();
                m.SetUVs(2, m.uv.ToList());
                return m;
            });
            Split = MeshAsset("Split", () =>
            {
                var source = Grid(20, 15);
                var v = source.vertices;
                var tri = source.triangles;
                var mesh = new Mesh { vertices = tri.Select(i => v[i]).ToArray(), triangles = Enumerable.Range(0, tri.Length).ToArray() };
                mesh.RecalculateNormals();
                return mesh;
            });
            Degenerate = MeshAsset("Degenerate", () =>
            {
                var m = Grid(10, 10);
                var tri = m.triangles;
                for (int i = 0; i < tri.Length / 2; i += 3) tri[i + 2] = tri[i + 1];
                m.triangles = tri;
                return m;
            });
            DupA = MeshAsset("DupA", () => Grid(5, 5));
            DupB = MeshAsset("DupB", () => Grid(5, 5));
            FarPivot = MeshAsset("FarPivot", () =>
            {
                var m = Grid(5, 5);
                m.vertices = m.vertices.Select(p => p + new Vector3(50f, 0f, 0f)).ToArray();
                m.RecalculateBounds();
                return m;
            });

            Uv1Good = MeshAsset("Uv1Good", () => { var m = Grid(10, 10); m.uv2 = m.uv; return m; });
            Uv1Outside = MeshAsset("Uv1Outside", () =>
            {
                var m = Grid(10, 10);
                m.uv2 = m.uv.Select(p => p * 2f).ToArray();
                return m;
            });
            Uv1Overlap = MeshAsset("Uv1Overlap", BuildOverlap);

            A35 = Clip("A_35s", 35f, AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.Vorbis, true);
            A15Pcm = Clip("A_15s_pcm", 15f, AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.PCM, false);
            A5 = Clip("A_5s", 5f, AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.Vorbis, true);

            ProfileOn = Profile("PP_On", true);
            ProfileOff = Profile("PP_Off", false);

            var rtPath = Root + "/Misc/RT_Target.renderTexture";
            TargetTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            if (TargetTexture == null)
            {
                TargetTexture = new RenderTexture(256, 256, 16);
                AssetDatabase.CreateAsset(TargetTexture, rtPath);
            }
            AssetDatabase.SaveAssets();
        }

        // ---- textures ----

        static Texture2D Tex(string name, int width, int height, bool alpha, Action<TextureImporter> setup, string seed = null)
        {
            var path = Root + "/Textures/" + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color32[width * height];
                byte salt = (byte)((seed ?? name).Sum(c => (int)c) % 251);
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        pixels[y * width + x] = new Color32((byte)(x * 255 / width), (byte)(y * 255 / height), salt, (byte)(alpha ? 128 : 255));
                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var before = Signature(importer);
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.ClearPlatformTextureSettings("Standalone");
            importer.npotScale = TextureImporterNPOTScale.ToNearest;
            importer.sRGBTexture = true;
            importer.anisoLevel = 1;
            importer.streamingMipmaps = false;
            setup(importer);
            if (Signature(importer) != before) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static string Signature(TextureImporter i)
        {
            var s = i.GetPlatformTextureSettings("Standalone");
            return string.Join("|", new object[]
            {
                i.textureType, i.textureShape, i.mipmapEnabled, i.isReadable, i.textureCompression, i.maxTextureSize,
                i.filterMode, i.alphaSource, i.generateCubemap, s.overridden, s.format, s.maxTextureSize,
                i.npotScale, i.sRGBTexture, i.anisoLevel, i.streamingMipmaps
            }.Select(o => o.ToString()).ToArray());
        }

        // ---- materials ----

        static Material Mat(string name, Action<Material> setup)
        {
            var path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.mainTexture = null;
            material.shaderKeywords = new string[0];
            material.renderQueue = -1;
            material.color = Color.white;
            material.SetFloat("_Mode", 0f);
            material.SetTexture("_MetallicGlossMap", null);
            material.SetTexture("_BumpMap", null);
            material.enableInstancing = true;
            setup(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---- meshes ----

        static Mesh MeshAsset(string name, Func<Mesh> build)
        {
            var path = Root + "/Meshes/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = build();
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // w x h quads = 2*w*h triangles, only UV0 (no UV1 on purpose).
        public static Mesh Grid(int w, int h)
        {
            var vertices = new Vector3[(w + 1) * (h + 1)];
            var uv = new Vector2[vertices.Length];
            for (int y = 0; y <= h; y++)
                for (int x = 0; x <= w; x++)
                {
                    vertices[y * (w + 1) + x] = new Vector3(x * 0.01f, 0f, y * 0.01f);
                    uv[y * (w + 1) + x] = new Vector2((float)x / w, (float)y / h);
                }
            var triangles = new int[w * h * 6];
            int t = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * (w + 1) + x;
                    triangles[t++] = i;
                    triangles[t++] = i + w + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + w + 1;
                    triangles[t++] = i + w + 2;
                }
            var mesh = new Mesh { vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildTwoSubmeshes()
        {
            var mesh = Grid(2, 2);
            var all = mesh.triangles;
            int half = all.Length / 2;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(all.Take(half).ToArray(), 0);
            mesh.SetTriangles(all.Skip(half).ToArray(), 1);
            return mesh;
        }

        static Mesh BuildSkin8()
        {
            var mesh = Grid(2, 2);
            const int bones = 8;
            var perVertex = new NativeArray<byte>(mesh.vertexCount, Allocator.Temp);
            var weights = new NativeArray<BoneWeight1>(mesh.vertexCount * bones, Allocator.Temp);
            for (int v = 0; v < mesh.vertexCount; v++)
            {
                perVertex[v] = bones;
                for (int b = 0; b < bones; b++) weights[v * bones + b] = new BoneWeight1 { boneIndex = b, weight = 1f / bones };
            }
            mesh.SetBoneWeights(perVertex, weights);
            perVertex.Dispose();
            weights.Dispose();
            var poses = new Matrix4x4[bones];
            for (int b = 0; b < bones; b++) poses[b] = Matrix4x4.identity;
            mesh.bindposes = poses;
            return mesh;
        }

        const string CubeObj =
            "o Cube\nv -0.5 -0.5 -0.5\nv 0.5 -0.5 -0.5\nv 0.5 0.5 -0.5\nv -0.5 0.5 -0.5\n" +
            "v -0.5 -0.5 0.5\nv 0.5 -0.5 0.5\nv 0.5 0.5 0.5\nv -0.5 0.5 0.5\n" +
            "f 1 2 3\nf 1 3 4\nf 5 7 6\nf 5 8 7\nf 1 5 6\nf 1 6 2\nf 2 6 7\nf 2 7 3\nf 3 7 8\nf 3 8 4\nf 4 8 5\nf 4 5 1\n";

        // Two quads with the same lightmap UVs: they overlap completely.
        static Mesh BuildOverlap()
        {
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 1),
                    new Vector3(2, 0, 0), new Vector3(3, 0, 0), new Vector3(2, 0, 1), new Vector3(3, 0, 1)
                },
                triangles = new[] { 0, 2, 1, 1, 2, 3, 4, 6, 5, 5, 6, 7 }
            };
            var uv = new[]
            {
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.05f), new Vector2(0.05f, 0.95f), new Vector2(0.95f, 0.95f),
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.05f), new Vector2(0.05f, 0.95f), new Vector2(0.95f, 0.95f)
            };
            mesh.uv = uv;
            mesh.uv2 = uv;
            mesh.RecalculateNormals();
            return mesh;
        }

        static Mesh Model(string name, bool readable, MeshOptimizationFlags optimization, bool lightmapUv = false,
            Action<ModelImporter> extra = null, string obj = null)
        {
            var path = Root + "/Models/" + name + ".obj";
            if (!File.Exists(path))
            {
                File.WriteAllText(path, obj ?? CubeObj);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var before = ModelSignature(importer);
            importer.generateSecondaryUV = lightmapUv;
            importer.isReadable = readable;
            importer.meshOptimizationFlags = optimization;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.indexFormat = ModelImporterIndexFormat.Auto;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.globalScale = 1f;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            if (extra != null) extra(importer);
            if (ModelSignature(importer) != before) importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().First();
        }

        static string ModelSignature(ModelImporter i)
        {
            return string.Join("|", new object[]
            {
                i.generateSecondaryUV, i.isReadable, i.meshOptimizationFlags, i.materialImportMode, i.indexFormat, i.meshCompression,
                i.globalScale, i.animationType, i.importAnimation, i.materialLocation
            }.Select(o => o.ToString()).ToArray());
        }

        static string GridObj(int w, int h)
        {
            var sb = new System.Text.StringBuilder("o Grid\n");
            for (int y = 0; y <= h; y++)
                for (int x = 0; x <= w; x++)
                    sb.Append("v ").Append(x * 0.01f).Append(" 0 ").Append(y * 0.01f).Append('\n');
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * (w + 1) + x + 1;
                    sb.Append("f ").Append(i).Append(' ').Append(i + w + 1).Append(' ').Append(i + 1).Append('\n');
                    sb.Append("f ").Append(i + 1).Append(' ').Append(i + w + 1).Append(' ').Append(i + w + 2).Append('\n');
                }
            return sb.ToString().Replace(',', '.');
        }

        // ---- audio ----

        static AudioClip Clip(string name, float seconds, AudioClipLoadType loadType, AudioCompressionFormat format, bool preload)
        {
            var path = Root + "/Audio/" + name + ".wav";
            if (!File.Exists(path))
            {
                WriteWav(path, seconds, 44100);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var s = importer.defaultSampleSettings;
            bool changed = s.loadType != loadType || s.compressionFormat != format || importer.preloadAudioData != preload ||
                           importer.ContainsSampleSettingsOverride("Standalone") || Math.Abs(s.quality - 0.5f) > 0.001f;
            if (changed)
            {
                importer.ClearSampleSettingOverride("Standalone");
                s.loadType = loadType;
                s.compressionFormat = format;
                s.quality = 0.5f;
                importer.defaultSampleSettings = s;
                importer.preloadAudioData = preload;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static void WriteWav(string path, float seconds, int rate)
        {
            int samples = (int)(seconds * rate);
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samples * 2);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(rate);
                writer.Write(rate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(samples * 2);
                for (int i = 0; i < samples; i++)
                    writer.Write((short)(Math.Sin(2 * Math.PI * 440 * i / rate) * 6000));
            }
        }

        // ---- post processing stub profiles ----

        static PostProcessProfile Profile(string name, bool enabled)
        {
            var path = Root + "/Misc/" + name + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(path);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var bloom = ScriptableObject.CreateInstance<Bloom>();
            bloom.enabled.value = true;
            bloom.name = "Bloom";
            AssetDatabase.AddObjectToAsset(bloom, profile);
            profile.settings.Add(bloom);
            if (enabled)
            {
                var blur = ScriptableObject.CreateInstance<MotionBlur>();
                blur.enabled.value = true;
                blur.name = "MotionBlur";
                AssetDatabase.AddObjectToAsset(blur, profile);
                profile.settings.Add(blur);
            }
            else
            {
                bloom.enabled.value = false;
            }
            EditorUtility.SetDirty(profile);
            return profile;
        }
    }
}
