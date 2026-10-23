using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Batch entry points. Each run regenerates the scene, applies one settings variant, runs the audit
    // and checks expectations as (checkId, path) pairs that must or must not be present.
    public static class AuditTests
    {
        const string ReportDir = @"E:\dev_personal\unity-tools-scratch\reports\audit-tests";

        sealed class Expect
        {
            public string ids;
            public Severity? severity;
            public string path = "";
            public string scene;
            public bool present = true;
            public string contains;
            public int? users;
            public bool? fixable;
            public int? count;

            public string Describe()
            {
                var sb = new StringBuilder(count.HasValue ? "count(" + ids + ")=" + count : (present ? "has " : "not ") + ids);
                if (severity.HasValue) sb.Append(" ").Append(severity.Value);
                if (path.Length > 0) sb.Append(" @ ").Append(path);
                if (scene != null) sb.Append(" scene=").Append(scene);
                if (contains != null) sb.Append(" ~\"").Append(contains).Append("\"");
                if (users.HasValue) sb.Append(" users=").Append(users);
                if (fixable.HasValue) sb.Append(" fixable=").Append(fixable);
                return sb.ToString();
            }
        }

        static List<Expect> expects;
        static List<string> failures;
        static StringBuilder log;

        // ---- entry points ----

        public static void Run()
        {
            var variant = Arg("-auditVariant") ?? "base";
            int code;
            try
            {
                code = RunVariant(variant);
            }
            catch (Exception e)
            {
                Debug.LogError("[AuditTests] " + variant + " crashed: " + e);
                WriteReport(variant, "CRASH\n" + e);
                code = 2;
            }
            EditorApplication.Exit(code);
        }

        public static void EnableSteamVrStub()
        {
            AuditTestSettings.SetSteamVrStub(true);
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        public static void DisableSteamVrStub()
        {
            AuditTestSettings.SetSteamVrStub(false);
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        public static void RunFixes()
        {
            int code;
            try
            {
                code = Fixes();
            }
            catch (Exception e)
            {
                Debug.LogError("[AuditTests] fixes crashed: " + e);
                WriteReport("fixes", "CRASH\n" + e);
                code = 2;
            }
            EditorApplication.Exit(code);
        }

        public static void RunExportUnit()
        {
            int code;
            try
            {
                code = ExportUnit();
            }
            catch (Exception e)
            {
                Debug.LogError("[AuditTests] export crashed: " + e);
                WriteReport("export-unit", "CRASH\n" + e);
                code = 2;
            }
            EditorApplication.Exit(code);
        }

        // ---- variants ----

        static void Setup(string variant)
        {
            AuditTestAssets.Prepare();
            AuditTestSettings.ApplyBaseline();
            switch (variant)
            {
                case "legacy-multipass": AuditTestSettings.SetLegacyVr(new[] { "OpenVR" }, StereoRenderingPath.MultiPass); break;
                case "legacy-spi": AuditTestSettings.SetLegacyVr(new[] { "OpenVR" }, StereoRenderingPath.Instancing); break;
                case "legacy-no-openvr": AuditTestSettings.SetLegacyVr(new[] { "None" }, StereoRenderingPath.Instancing); break;
                case "project": AuditTestSettings.ApplyProjectVariant(); break;
                case "fixed-step-90": Time.fixedDeltaTime = 1f / 90f; break;
                case "xr-both": AuditTestSettings.SetLegacyVr(new[] { "OpenVR" }, StereoRenderingPath.Instancing); break;
            }
            switch (variant)
            {
                case "xr-openvr-spi":
                case "xr-both": XrConfigure("openvr", "SinglePassInstanced"); break;
                case "xr-openvr-multipass": XrConfigure("openvr", "MultiPass"); break;
                case "xr-mock": XrConfigure("mock", "SinglePassInstanced"); break;
                default: XrConfigure(null, "SinglePassInstanced"); break;
            }
            AuditTestScene.Build(variant);
            AssetDatabase.SaveAssets();
        }

        // XR Plug-in Management setup lives in an assembly that exists only with the package.
        static Type XrSetupType()
        {
            return Type.GetType("AuditTestsXR.XrSetup, AuditTests.XRSetup");
        }

        static void XrConfigure(string loader, string stereo)
        {
            var type = XrSetupType();
            if (type == null)
            {
                if (loader != null) throw new InvalidOperationException("XR Management is not installed in the test project");
                return;
            }
            type.GetMethod("Configure").Invoke(null, new object[] { loader, stereo });
        }

        static int RunVariant(string variant)
        {
            Begin();
            Setup(variant);
            int qualityTarget = -1;
            if (variant == "quality") qualityTarget = AuditTestSettings.ApplyBadQualityOnOtherLevel();
            AssetDatabase.SaveAssets();

            int currentLevel = QualitySettings.GetQualityLevel();
            var hashesBefore = Hashes();
            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            AssetDatabase.SaveAssets();
            Check(result != null, "audit returned a result");
            Check(QualitySettings.GetQualityLevel() == currentLevel, "current quality level unchanged");
            Check(!SceneManager.GetSceneByPath(AuditTestScene.SeedPath).isDirty, "scene not dirtied by the audit");
            var hashesAfter = Hashes();
            foreach (var pair in hashesBefore)
                Check(hashesAfter[pair.Key] == pair.Value, "file unchanged by the audit: " + pair.Key);

            ExpectScene(variant);
            ExpectSettings(variant, result);
            Evaluate(result);

            if (variant == "quality")
            {
                Check(result.quality.index == qualityTarget && result.quality.isStandaloneDefault,
                    "audited the Standalone default level " + qualityTarget + " (got " + result.quality.index + ")");
            }
            if (variant == "legacy-multipass")
            {
                Check(result.xr.legacyEnabled && result.xr.HasOpenVR && result.xr.stereoMode == StereoMode.MultiPass, "XR: legacy OpenVR multi-pass detected");
                long multi = result.drawCallEstimate;
#pragma warning disable 0618
                PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
#pragma warning restore 0618
                var spi = SceneAuditor.RunFullAudit(new VRBudgets(), false);
                Check(spi.xr.stereoMode == StereoMode.SinglePassInstanced, "XR: instanced detected after switch");
                Check(multi == spi.drawCallEstimate * 2, "draw calls multi-pass " + multi + " = 2 x instanced " + spi.drawCallEstimate);
            }
            if (variant == "base")
            {
                Check(!result.xr.AnyEnabled && result.xr.SystemName == "None", "XR: none detected");
                Check(result.xr.managementInstalled == (XrSetupType() != null) && !result.xr.ManagementEnabled,
                    "XR Management detected as installed=" + (XrSetupType() != null) + " without loaders");
                log.AppendLine("info: estimates tris=" + result.sceneTris + " drawCalls=" + result.drawCallEstimate +
                               " textureMB=" + result.textureMemoryMB.ToString("F1"));
                var instancing = result.issues.FirstOrDefault(i => i.assetPath.EndsWith("M_NoInst.mat"));
                var near = GameObject.Find("Cam/Near");
                log.AppendLine("info: Cam/Near nearClipPlane = " + (near != null ? near.GetComponent<Camera>().nearClipPlane.ToString("R") : "?"));
                foreach (var id in new[] { "MESH_IMPORT_ANIMATION", "MESH_IMPORT_CAMERAS_LIGHTS", "MESH_EMBEDDED_MATERIALS", "TEX_ALPHA_UNUSED" })
                    log.AppendLine("info: " + id + " = " + string.Join("; ", result.issues.Where(i => i.checkId == id).Select(Brief).ToArray()));
                log.AppendLine("info: instancing check for M_NoInst = " + (instancing != null ? instancing.checkId : "none"));
            }
            if (variant == "budgets-low") BudgetsLow(result);
            if (variant == "xr-openvr-spi" || variant == "xr-openvr-multipass")
            {
                Check(result.xr.SystemName == "XRManagement" && result.xr.HasOpenVR, "XR: XR Management with OpenVR (" + result.xr.SystemName + ", " + string.Join(",", result.xr.loaders) + ")");
                var mode = variant == "xr-openvr-spi" ? StereoMode.SinglePassInstanced : StereoMode.MultiPass;
                Check(result.xr.stereoMode == mode, "XR: stereo " + mode + " read from OpenVRSettings (got " + result.xr.stereoMode + ")");
            }
            if (variant == "xr-mock") Check(result.xr.ManagementEnabled && !result.xr.HasOpenVR && result.xr.stereoMode == StereoMode.Unknown, "XR: non-OpenVR loader, stereo unknown");
            if (variant == "xr-both") Check(result.xr.BothEnabled && result.xr.SystemName == "Legacy+XRManagement", "XR: both systems detected");

            return Finish(variant, result);
        }

        static void BudgetsLow(AuditResult baseline)
        {
            var warnOnly = new VRBudgets
            {
                drawCallsWarning = (int)(baseline.drawCallEstimate / 2), drawCallsCritical = (int)(baseline.drawCallEstimate * 2),
                sceneTrisWarning = (int)(baseline.sceneTris / 2), sceneTrisCritical = (int)(baseline.sceneTris * 2),
                textureMemoryWarningMB = Math.Max(0, (int)(baseline.textureMemoryMB / 2)), textureMemoryCriticalMB = (int)(baseline.textureMemoryMB * 2) + 1
            };
            var r1 = SceneAuditor.RunFullAudit(warnOnly, false);
            ExpectNow(r1, "budgets warn", () =>
            {
                Has("DC_BUDGET", Severity.Warning);
                Has("MESH_SCENE_TRIS", Severity.Warning);
                Has("TEX_MEMORY", Severity.Warning);
            });

            var critical = new VRBudgets
            {
                drawCallsWarning = (int)(baseline.drawCallEstimate / 4), drawCallsCritical = (int)(baseline.drawCallEstimate / 2),
                sceneTrisWarning = (int)(baseline.sceneTris / 4), sceneTrisCritical = (int)(baseline.sceneTris / 2),
                textureMemoryWarningMB = 0, textureMemoryCriticalMB = Math.Max(0, (int)(baseline.textureMemoryMB / 2) - 1),
                textureSizeWarning = 256, textureSizeCritical = 512
            };
            var r2 = SceneAuditor.RunFullAudit(critical, false);
            ExpectNow(r2, "budgets critical", () =>
            {
                Has("DC_BUDGET", Severity.Critical);
                Has("MESH_SCENE_TRIS", Severity.Critical);
                Has("TEX_MEMORY", Severity.Critical);
                Has("TEX_SIZE", Severity.Critical, "T_1024.png");
                Has("TEX_SIZE", Severity.Warning, "T_Cube.png");
                Not("TEX_SIZE", "T_Cube.png", Severity.Critical);
            });
        }

        // ---- expectations ----

        static void ExpectScene(string variant)
        {
            // Draw calls
            Has("DC_MANY_MATERIALS", Severity.Warning, "DC/Five");
            Has("DC_EXTRA_MATERIALS", Severity.Warning, "DC/Five");
            Not("DC_MULTI_MATERIAL", "DC/Five");
            Has("DC_MULTI_MATERIAL", Severity.Info, "DC/Two");
            Not("DC_EXTRA_MATERIALS", "DC/Two");
            Has("DC_STATIC_CANDIDATE", Severity.Info, "DC/Candidate", fixable: true);
            Not("DC_STATIC_CANDIDATE", "DC/Static");
            Not("DC_STATIC_CANDIDATE", "DC/Moved");
            Not("DC_STATIC_CANDIDATE", "DC/UnderScript/Child");
            Has("DC_INSTANCING_OFF|DC_INSTANCING_UNKNOWN", null, "M_NoInst.mat", users: 4);
            Not("DC_INSTANCING_OFF|DC_INSTANCING_UNKNOWN", "M_NoInst3.mat");
            Has("DC_INSTANCING_OFF|DC_INSTANCING_UNKNOWN", null, "unity_builtin_extra", fixable: false);
            Has("DC_SHADOW_CASTERS", Severity.Info);

            // Meshes
            Has("MESH_NO_LOD", Severity.Warning, "Mesh/12k");
            Has("MESH_NO_LOD", Severity.Critical, "Mesh/30k");
            Not("MESH_NO_LOD", "Mesh/Lod/LOD0");
            Not("MESH_NO_LOD", "Mesh/Lod/LOD1");
            Has("MESH_READ_WRITE", Severity.Warning, "ReadWrite.obj", fixable: true);
            Has("MESH_READ_WRITE_NEEDED", Severity.Info, "ReadWriteCollider.obj");
            Not("MESH_READ_WRITE", "ReadWriteCollider.obj");
            Has("MESH_NOT_OPTIMIZED", Severity.Info, "NotOptimized.obj");
            Not("MESH_READ_WRITE|MESH_READ_WRITE_NEEDED|MESH_NOT_OPTIMIZED", "Clean.obj");
            Has("MESH_BONES_PER_VERTEX", Severity.Warning, "Skin8.asset");

            // 3D assets: meshes and models
            Has("MESH_UNUSED_CHANNELS", Severity.Info, "Channels.asset", contains: "Color, Tangent, UV2");
            Not("MESH_UNUSED_CHANNELS", "Grid12k.asset");
            Has("MESH_SPLIT_VERTICES", Severity.Info, "Split.asset");
            Not("MESH_SPLIT_VERTICES", "Grid12k.asset");
            Has("MESH_INDEX_32", Severity.Info, "Index32.obj", fixable: true);
            Not("MESH_INDEX_32", "Clean.obj");
            Has("MESH_COMPRESSION_OFF", Severity.Info, "Big12k.obj", fixable: true);
            Not("MESH_COMPRESSION_OFF", "Clean.obj");
            Has("MESH_IMPORT_SCALE", Severity.Info, "Scaled.obj");
            Not("MESH_IMPORT_SCALE", "Clean.obj");
            Has("MESH_EXTREME_SCALE", Severity.Warning, "Asset/Huge");
            Not("MESH_EXTREME_SCALE", "Gen/Mirrored");
            Has("MESH_DENSE_SMALL", Severity.Info, "Asset/DenseSmall");
            Not("MESH_DENSE_SMALL", "Mesh/12k");
            Has("MESH_LOD_WEAK", Severity.Warning, "Lod/Weak");
            Has("MESH_LOD_NO_CULL", Severity.Info, "Lod/Weak");
            Has("MESH_LOD_LATE", Severity.Info, "Lod/Late");
            Has("MESH_LOD_MATERIALS", Severity.Info, "Lod/Mats");
            Not("MESH_LOD_WEAK|MESH_LOD_NO_CULL|MESH_LOD_LATE|MESH_LOD_MATERIALS", "Mesh/Lod");
            Has("MESH_DEGENERATE", Severity.Info, "Degenerate.asset");
            Not("MESH_DEGENERATE", "Grid12k.asset");
            // The OBJ test models are all the same cube, so they form a duplicate group of their own.
            Has("MESH_DUPLICATE", Severity.Info, "DupA.asset", contains: "DupA, DupB");
            Has("MESH_IMPORT_ANIMATION", Severity.Info, "Animated.obj", fixable: true);
            Not("MESH_IMPORT_ANIMATION", "Clean.obj");
            Has("MESH_EMBEDDED_MATERIALS", Severity.Info, "Embedded.obj");
            Not("MESH_EMBEDDED_MATERIALS", "Clean.obj");
            Has("TEX_ALPHA_UNUSED", Severity.Info, "T_AlphaFull.png", fixable: true);
            Not("TEX_ALPHA_UNUSED", "T_Normal.png");
            Has("MESH_PIVOT_FAR", Severity.Info, "FarPivot.asset");
            Not("MESH_PIVOT_FAR", "Grid12k.asset");

            // 3D assets: textures
            Has("TEX_NPOT", Severity.Info, "T_Npot.png", fixable: true);
            Count("TEX_DUPLICATE", 1);
            Has("TEX_DUPLICATE", Severity.Info, contains: "T_DupA.png");
            Has("TEX_DENSITY_HIGH", Severity.Info, "T_Dense.png");
            Has("TEX_DENSITY_LOW", Severity.Info, "T_Wall.png");
            // With anisotropic filtering off for the whole level the per-texture setting does not matter.
            if (variant != "quality") Has("TEX_ANISO_LOW", Severity.Info, "T_Wall.png", fixable: true);
            Not("TEX_ANISO_LOW", "T_Dense.png");
            Has("TEX_SRGB_DATA", Severity.Warning, "T_Metal.png", fixable: true);
            Not("TEX_SRGB_DATA", "T_Small.png");
            Has("TEX_STREAMING_OFF", Severity.Info, "T_4096.png", fixable: true);
            Not("TEX_STREAMING_OFF", "T_Small.png");

            // Materials
            Count("MAT_DUPLICATE", 1);
            Has("MAT_DUPLICATE", Severity.Info, contains: "M_DupA, M_DupB", fixable: true);
            Has("MAT_TRANSPARENT", Severity.Warning, "M_Fade.mat");
            Has("MAT_TRANSPARENT", Severity.Info, "M_FadeSmall.mat");
            Not("MAT_TRANSPARENT", "M_Plain.mat");
            Has("MAT_KEYWORD_NO_TEXTURE", Severity.Info, "M_KeyNormal.mat", fixable: true, contains: "_NORMALMAP");
            Not("MAT_KEYWORD_NO_TEXTURE", "M_Metal.mat");
            Has("MAT_SIMPLE_STANDARD", Severity.Info);
            Has("MAT_DEFAULT", Severity.Warning, "InstBuiltin/A");
            Not("MAT_DEFAULT", "DC/Candidate");

            // Textures
            Has("TEX_SIZE", Severity.Warning, "T_4096.png");
            Has("TEX_SIZE", Severity.Critical, "T_8192.png");
            Not("TEX_SIZE", "T_1024.png");
            Has("TEX_NOT_NORMAL_MAP", Severity.Critical, "T_FakeNormal.png", fixable: true);
            Not("TEX_NOT_NORMAL_MAP", "T_Normal.png");
            Has("TEX_NO_MIPMAPS", Severity.Warning, "T_NoMips.png", users: 50, fixable: true);
            Not("TEX_NO_MIPMAPS", "T_OverlayOnly.png");
            Has("TEX_UNCOMPRESSED", Severity.Warning, "T_Raw.png", contains: "Uncompressed");
            Has("TEX_UNCOMPRESSED", Severity.Warning, "T_Rgba32.png", contains: "BC7");
            Has("TEX_READ_WRITE", Severity.Warning, "T_Readable.png", fixable: true);
            Count("TEX_NOT_TRILINEAR", 1);

            // Lighting
            Has("LIGHT_REALTIME", Severity.Warning, "Light/PointRealtime");
            Not("LIGHT_REALTIME", "Light/SpotMixed");
            Not("LIGHT_REALTIME", "Light/Directional");
            Has("LIGHT_SHADOWED", Severity.Critical);
            // Lightmap UVs: one issue per mesh asset.
            Has("LIGHT_NO_UV1", Severity.Warning, "Grid12k.asset", fixable: false, users: 1);
            Has("LIGHT_NO_UV1", Severity.Warning, "LmNoUv.obj", fixable: true, users: 2);
            Not("LIGHT_NO_UV1|LIGHT_UV1_OUT_OF_RANGE|LIGHT_UV1_OVERLAP", "LmGen.obj");
            Not("LIGHT_NO_UV1|LIGHT_UV1_OUT_OF_RANGE|LIGHT_UV1_OVERLAP", "Uv1Good.asset");
            Has("LIGHT_UV1_OUT_OF_RANGE", Severity.Warning, "Uv1Outside.asset");
            Not("LIGHT_UV1_OVERLAP", "Uv1Outside.asset");
            Has("LIGHT_UV1_OVERLAP", Severity.Warning, "Uv1Overlap.asset", contains: "100");
            Not("LIGHT_UV1_OUT_OF_RANGE", "Uv1Overlap.asset");
            Count("LIGHT_NO_UV1", 2);

            // Physics
            Has("PHYS_CONCAVE_DYNAMIC", Severity.Critical, "Phys/ConcaveDynamic");
            Has("PHYS_CONCAVE_KINEMATIC", Severity.Info, "Phys/ConcaveKinematic");
            Has("PHYS_CONCAVE_DYNAMIC", Severity.Critical, "Phys/Body/Child");
            Has("PHYS_RENDER_MESH_COLLIDER", Severity.Info, "Phys/RenderMesh12k");

            // Animators
            Has("ANIM_ALWAYS_ANIMATE", Severity.Warning, "Anim/Default", fixable: true);
            Has("ANIM_NO_CONTROLLER", Severity.Info, "Anim/Default");
            Not("ANIM_ROOT_MOTION", "Anim/Default");
            Has("ANIM_ROOT_MOTION", Severity.Info, "Anim/RootMotion");
            Not("ANIM_ALWAYS_ANIMATE", "Anim/RootMotion");
            Not("ANIM_ROOT_MOTION", "Anim/RootMotionAgent");
            Not("ANIM_ROOT_MOTION", "Anim/RootMotionScript");
            Has("ANIM_UPDATE_OFFSCREEN", Severity.Warning, "Anim/Offscreen", fixable: true);
            Has("ANIM_ALWAYS_ANIMATE", Severity.Warning, "PrefabAnim");
            if (variant != "project") Has("ANIM_HEAVY_SKIN", Severity.Warning, "Anim/Heavy/Body");
            if (variant != "counts") Not("ANIM_COUNT");

            // Audio
            Has("AUDIO_DECOMPRESS_LONG", Severity.Warning, "A_35s.wav", fixable: true);
            Has("AUDIO_PRELOAD_LONG", Severity.Warning, "A_35s.wav", fixable: true);
            Not("AUDIO_PCM_LARGE", "A_35s.wav");
            Has("AUDIO_DECOMPRESS_LONG", Severity.Warning, "A_15s_pcm.wav");
            Has("AUDIO_PCM_LARGE", Severity.Warning, "A_15s_pcm.wav", fixable: true);
            Not("AUDIO_PRELOAD_LONG", "A_15s_pcm.wav");
            Not("AUDIO_DECOMPRESS_LONG|AUDIO_PRELOAD_LONG|AUDIO_PCM_LARGE", "A_5s.wav");
            Has("AUDIO_NO_CLIP", Severity.Info, "Audio/NoClip");
            Not("AUDIO_2D_SOURCE", "Audio/NoClip");
            Has("AUDIO_2D_SOURCE", Severity.Info, "Audio/TwoD", fixable: true);
            Not("AUDIO_2D_SOURCE", "Audio/Short5");
            Has("AUDIO_2D_SOURCE", Severity.Info, "B/TwoD", scene: "AuditSeedB");
            Has("AUDIO_PRIORITY_ZERO", Severity.Warning);
            if (variant == "no-listener")
            {
                Has("AUDIO_NO_LISTENER", Severity.Warning);
                Not("AUDIO_LISTENERS_MANY");
            }
            else
            {
                Count("AUDIO_LISTENERS_MANY", 2, severity: Severity.Critical);
                Has("AUDIO_LISTENER_NO_CAMERA", Severity.Info, "Audio/ListenerNoCam");
                Not("AUDIO_LISTENER_NO_CAMERA", "Main Camera");
                Not("AUDIO_NO_LISTENER");
            }
            if (variant != "counts") Not("AUDIO_SOURCE_COUNT");

            // VR objects
            Has("VR_CAMERA_HDR", Severity.Warning, "Main Camera");
            Not("VR_CAMERA_HDR", "Cam/Far");
            Has("VR_FAR_CLIP", Severity.Info, "Cam/Far");
            // Unity clamps nearClipPlane to 0.01; the check fires below 0.05.
            Has("VR_NEAR_CLIP", Severity.Info, "Cam/Near");
            Not("VR_NEAR_CLIP", "Cam/Far");
            Has("VR_DEFERRED_CAMERA", Severity.Warning, "Cam/Deferred");
            Has("VR_CAMERA_NO_MSAA", Severity.Warning, "Cam/NoMsaa");
            Not("VR_CAMERA_HDR|VR_FAR_CLIP|VR_NEAR_CLIP|VR_CAMERA_NO_MSAA", "Cam/ToTexture");
            Has("VR_IMAGE_EFFECT", Severity.Warning, "Cam/Effect");
            Has("VR_CANVAS_OVERLAY", Severity.Critical, "UI/Overlay");
            Has("VR_CANVAS_CAMERA", Severity.Warning, "UI/ScreenCamera");
            Not("VR_CANVAS_OVERLAY|VR_CANVAS_CAMERA", "UI/World");
            Has("VR_PP_LAYER", Severity.Warning, "Main Camera");
            Has("VR_PP_EFFECTS", Severity.Warning, "PP/Volume", contains: "MotionBlur");
            Not("VR_PP_EFFECTS", "PP/VolumeOff");
            Has("VR_PROBE_REALTIME", Severity.Warning, "Probe/Realtime");
            Has("VR_PROBE_EVERY_FRAME", Severity.Critical, "Probe/EveryFrame");
            Not("VR_PROBE_REALTIME|VR_PROBE_EVERY_FRAME", "Probe/Baked");
            Not("VR_NO_CAMERA");
            if (variant != "counts") Not("VR_NO_OCCLUSION");
            if (variant == "steamvr-not-in-scene")
            {
                Has("VR_STEAMVR_NOT_IN_SCENE", Severity.Info);
                Not("VR_STEAMVR_MISSING");
            }
            else if (variant == "steamvr-in-scene")
            {
                Not("VR_STEAMVR_NOT_IN_SCENE|VR_STEAMVR_MISSING");
            }
            else
            {
                Has("VR_STEAMVR_MISSING", Severity.Info);
            }

            // General
            Has("GEN_EMPTY_LEAF", Severity.Info, "Gen/Empty");
            Has("GEN_NEGATIVE_SCALE", Severity.Info, "Gen/Mirrored");
            Count("GEN_DEEP_HIERARCHY", 1);
            Has("GEN_DEEP_HIERARCHY", Severity.Info, "Deep/D2/D3/D4/D5/D6/D7/D8/D9/D10/D11");
            Has("GEN_MISSING_SCRIPTS", Severity.Warning, "Gen/Missing");
            Has("GEN_INACTIVE_HEAVY_MESH", Severity.Info, "Gen/InactiveHeavy");
            Has("GEN_INACTIVE_HEAVY_TEXTURE", Severity.Info, "Gen/InactiveTex");
        }

        static void ExpectSettings(string variant, AuditResult result)
        {
            bool vrOn = variant.StartsWith("legacy-") || variant.StartsWith("xr-");
            if (vrOn) Not("VR_NOT_ENABLED");
            else Has("VR_NOT_ENABLED", Severity.Critical);

            if (variant == "legacy-multipass" || variant == "xr-openvr-multipass") Has("VR_STEREO_MODE", Severity.Warning, contains: "MultiPass");
            else Not("VR_STEREO_MODE");
            if (variant == "legacy-no-openvr" || variant == "xr-mock") Has("VR_NO_OPENVR", Severity.Warning);
            else Not("VR_NO_OPENVR");
            if (variant == "xr-both") Has("VR_BOTH_SYSTEMS", Severity.Warning);
            else Not("VR_BOTH_SYSTEMS");

            if (variant == "quality")
            {
                Has("VR_MSAA", Severity.Warning);
                Has("LIGHT_PIXEL_LIGHTS", Severity.Warning);
                Has("LIGHT_SHADOW_RESOLUTION", Severity.Warning);
                Has("LIGHT_SHADOW_DISTANCE", Severity.Info);
                Has("TEX_ANISO_DISABLED", Severity.Info);
                Has("MESH_QUALITY_SKIN_WEIGHTS", Severity.Info);
            }
            else
            {
                Not("VR_MSAA|LIGHT_PIXEL_LIGHTS|LIGHT_SHADOW_RESOLUTION|LIGHT_SHADOW_DISTANCE|TEX_ANISO_DISABLED|MESH_QUALITY_SKIN_WEIGHTS");
            }

            if (variant == "project")
            {
                Has("MESH_GPU_SKINNING_OFF", Severity.Warning);
                Has("ANIM_HEAVY_SKIN_CPU", Severity.Warning, "Anim/Heavy/Body");
                Not("ANIM_HEAVY_SKIN", "Anim/Heavy/Body");
                Has("PHYS_SOLVER_ITERATIONS", Severity.Info);
                Has("PHYS_FIXED_STEP_FAST", Severity.Info);
                Not("PHYS_FIXED_STEP_SLOW");
                Has("LIGHT_REALTIME_GI", Severity.Info);
                Has("VR_DEFERRED_TIER", Severity.Warning);
                Has("AUDIO_SPEAKER_MODE", Severity.Warning);
                Has("AUDIO_DSP_BUFFER", Severity.Info, contains: "Best Latency");
            }
            else
            {
                Not("MESH_GPU_SKINNING_OFF|ANIM_HEAVY_SKIN_CPU|PHYS_SOLVER_ITERATIONS|LIGHT_REALTIME_GI|VR_DEFERRED_TIER|AUDIO_SPEAKER_MODE");
                Has("AUDIO_DSP_BUFFER", Severity.Info, contains: "Default");
                if (variant == "fixed-step-90") Not("PHYS_FIXED_STEP_SLOW|PHYS_FIXED_STEP_FAST");
                else
                {
                    Has("PHYS_FIXED_STEP_SLOW", Severity.Warning, "Project/Time");
                    Not("PHYS_FIXED_STEP_FAST");
                }
            }
            Has("AUDIO_NO_SPATIALIZER", Severity.Info);

            if (variant == "counts")
            {
                Has("ANIM_COUNT", Severity.Warning);
                Has("AUDIO_SOURCE_COUNT", Severity.Warning);
                Has("VR_NO_OCCLUSION", Severity.Info);
            }
            // Base scene estimates about 940 draw calls: doubled by multi-pass or with the extra objects of "counts" it passes 1000.
            if (variant == "legacy-multipass" || variant == "xr-openvr-multipass" || variant == "counts")
            {
                // The severity depends on how many objects the seed scene has, so only the presence is checked.
                Has("DC_BUDGET");
                Not("MESH_SCENE_TRIS|TEX_MEMORY");
            }
            // The draw call estimate of the seed scene is close to the budget, so only budgets-low checks DC_BUDGET exactly.
            else if (variant != "budgets-low") Not("MESH_SCENE_TRIS|TEX_MEMORY");
        }

        static void Has(string ids, Severity? severity = null, string path = "", string contains = null, int? users = null,
            bool? fixable = null, string scene = null)
        {
            expects.Add(new Expect { ids = ids, severity = severity, path = path, contains = contains, users = users, fixable = fixable, scene = scene });
        }

        static void Not(string ids, string path = "", Severity? severity = null)
        {
            expects.Add(new Expect { ids = ids, path = path, present = false, severity = severity });
        }

        static void Count(string ids, int count, string path = "", Severity? severity = null)
        {
            expects.Add(new Expect { ids = ids, path = path, count = count, severity = severity });
        }

        static bool Matches(Expect e, AuditIssue issue)
        {
            if (Array.IndexOf(e.ids.Split('|'), issue.checkId) < 0) return false;
            if (e.severity.HasValue && issue.severity != e.severity.Value) return false;
            if (e.scene != null && issue.scene != e.scene) return false;
            if (e.path.Length == 0) return true;
            if (issue.objectPath == e.path || issue.settingsPath == e.path) return true;
            return !string.IsNullOrEmpty(issue.assetPath) && (issue.assetPath == e.path || issue.assetPath.EndsWith("/" + e.path));
        }

        static void Evaluate(AuditResult result)
        {
            foreach (var e in expects)
            {
                var hits = result.issues.Where(i => Matches(e, i)).ToList();
                if (e.count.HasValue)
                {
                    Check(hits.Count == e.count.Value, e.Describe() + " (got " + hits.Count + ")");
                    continue;
                }
                if (!e.present)
                {
                    Check(hits.Count == 0, e.Describe() + (hits.Count > 0 ? " (got " + Brief(hits[0]) + ")" : ""));
                    continue;
                }
                if (hits.Count == 0)
                {
                    var near = result.issues.Where(i => Array.IndexOf(e.ids.Split('|'), i.checkId) >= 0).Take(3).Select(Brief);
                    Check(false, e.Describe() + " (missing; same id: " + string.Join("; ", near.ToArray()) + ")");
                    continue;
                }
                var hit = hits[0];
                bool ok = true;
                var why = new List<string>();
                if (e.contains != null && (hit.Message + " " + hit.Suggestion).IndexOf(e.contains, StringComparison.Ordinal) < 0)
                {
                    ok = false;
                    why.Add("text \"" + hit.Message + " | " + hit.Suggestion + "\"");
                }
                if (e.users.HasValue && hit.users != e.users.Value)
                {
                    ok = false;
                    why.Add("users " + hit.users);
                }
                if (e.fixable.HasValue && hit.AutoFixable != e.fixable.Value)
                {
                    ok = false;
                    why.Add("fixable " + hit.AutoFixable);
                }
                Check(ok, e.Describe() + (ok ? "" : " (" + string.Join(", ", why.ToArray()) + ")"));
            }
            expects.Clear();
        }

        static void ExpectNow(AuditResult result, string title, Action declare)
        {
            log.AppendLine("-- " + title);
            expects.Clear();
            declare();
            Evaluate(result);
        }

        // ---- fixes ----

        static int Fixes()
        {
            Begin();
            Setup("base");
            var before = SceneAuditor.RunFullAudit(new VRBudgets(), false);

            // Safe batch.
            var safe = before.issues.Where(i => i.fixKind == FixKind.Safe && i.AutoFixable).ToList();
            log.AppendLine("info: safe fixes = " + string.Join(", ", safe.Select(Brief).ToArray()));
            Check(safe.Any(i => i.checkId == "TEX_NO_MIPMAPS"), "safe list has TEX_NO_MIPMAPS");
            Check(safe.All(i => i.checkId == "TEX_NO_MIPMAPS" || i.checkId == "DC_INSTANCING_OFF"), "safe list has only mipmaps and instancing");
            Check(AutoFixer.Apply(safe) == safe.Count, "all safe fixes applied");
            var afterSafe = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            ExpectNow(afterSafe, "after safe fixes", () =>
            {
                Not("TEX_NO_MIPMAPS", "T_NoMips.png");
                Not("DC_INSTANCING_OFF", "M_NoInst.mat");
                Has("TEX_READ_WRITE", Severity.Warning, "T_Readable.png");
                Has("ANIM_ALWAYS_ANIMATE", Severity.Warning, "Anim/Default");
            });

            // Scene review fixes in one Undo group.
            var sceneTargets = new[]
            {
                new[] { "DC_STATIC_CANDIDATE", "DC/Candidate" }, new[] { "ANIM_ALWAYS_ANIMATE", "Anim/Default" },
                new[] { "ANIM_UPDATE_OFFSCREEN", "Anim/Offscreen" }, new[] { "AUDIO_2D_SOURCE", "Audio/TwoD" }
            };
            var sceneFixes = sceneTargets.Select(t => afterSafe.issues.FirstOrDefault(i => i.checkId == t[0] && i.objectPath == t[1])).ToList();
            Check(sceneFixes.All(i => i != null && i.AutoFixable && !i.fixTouchesAsset), "scene fixes found and undoable");
            Check(AutoFixer.Apply(sceneFixes.Where(i => i != null).ToList()) == sceneFixes.Count(i => i != null), "scene fixes applied");
            var afterScene = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            ExpectNow(afterScene, "after scene fixes", () =>
            {
                foreach (var t in sceneTargets) Not(t[0], t[1]);
            });
            Undo.PerformUndo();
            var afterUndo = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            ExpectNow(afterUndo, "after one Undo", () =>
            {
                foreach (var t in sceneTargets) Has(t[0], null, t[1]);
            });

            // Prefab instance: the fix becomes an override, the prefab asset stays.
            var prefabIssue = afterUndo.issues.FirstOrDefault(i => i.checkId == "ANIM_ALWAYS_ANIMATE" && i.objectPath == "PrefabAnim");
            Check(prefabIssue != null && AutoFixer.BecomesOverride(prefabIssue), "prefab instance fix is marked as override");
            if (prefabIssue != null)
            {
                AutoFixer.Apply(new List<AuditIssue> { prefabIssue });
                var instance = GameObject.Find("PrefabAnim");
                Check(instance != null && PrefabUtility.HasPrefabInstanceAnyOverrides(instance, false), "prefab instance has an override");
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AuditTestAssets.Root + "/Prefabs/PrefabAnim.prefab");
                Check(asset.GetComponent<Animator>().cullingMode == AnimatorCullingMode.AlwaysAnimate, "prefab asset unchanged");
            }

            // Asset review fixes.
            var assetTargets = new[]
            {
                new[] { "MESH_READ_WRITE", "ReadWrite.obj" }, new[] { "TEX_NOT_NORMAL_MAP", "T_FakeNormal.png" },
                new[] { "TEX_UNCOMPRESSED", "T_Raw.png" }, new[] { "TEX_UNCOMPRESSED", "T_Rgba32.png" },
                new[] { "TEX_READ_WRITE", "T_Readable.png" }, new[] { "AUDIO_DECOMPRESS_LONG", "A_35s.wav" },
                new[] { "AUDIO_PRELOAD_LONG", "A_35s.wav" }, new[] { "AUDIO_DECOMPRESS_LONG", "A_15s_pcm.wav" },
                new[] { "AUDIO_PCM_LARGE", "A_15s_pcm.wav" }, new[] { "LIGHT_NO_UV1", "LmNoUv.obj" },
                new[] { "MESH_INDEX_32", "Index32.obj" }, new[] { "MESH_IMPORT_ANIMATION", "Animated.obj" }, new[] { "TEX_ALPHA_UNUSED", "T_AlphaFull.png" }, new[] { "MESH_COMPRESSION_OFF", "Big12k.obj" },
                new[] { "TEX_NPOT", "T_Npot.png" }, new[] { "TEX_SRGB_DATA", "T_Metal.png" }, new[] { "TEX_ANISO_LOW", "T_Wall.png" },
                new[] { "TEX_STREAMING_OFF", "T_4096.png" }, new[] { "MAT_KEYWORD_NO_TEXTURE", "M_KeyNormal.mat" }
            };
            var assetFixes = assetTargets.Select(t => afterUndo.issues.FirstOrDefault(i => i.checkId == t[0] && i.assetPath.EndsWith("/" + t[1]))).ToList();
            for (int k = 0; k < assetTargets.Length; k++)
                Check(assetFixes[k] != null && assetFixes[k].AutoFixable && assetFixes[k].fixTouchesAsset, "asset fix available: " + assetTargets[k][0] + " " + assetTargets[k][1]);
            AutoFixer.Apply(assetFixes.Where(i => i != null).ToList());
            var afterAssets = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            ExpectNow(afterAssets, "after asset fixes", () =>
            {
                foreach (var t in assetTargets) Not(t[0], t[1]);
                Has("MESH_READ_WRITE_NEEDED", Severity.Info, "ReadWriteCollider.obj");
            });
            var metal = (TextureImporter)AssetImporter.GetAtPath(AuditTestAssets.Root + "/Textures/T_Metal.png");
            Check(!metal.sRGBTexture, "T_Metal.png: sRGB off");
            Check(!AuditTestAssets.KeyNormal.IsKeywordEnabled("_NORMALMAP"), "M_KeyNormal: _NORMALMAP off");

            // Duplicate materials: the scene fix swaps renderers to the first one and Undo brings them back.
            var dup = afterAssets.issues.FirstOrDefault(i => i.checkId == "MAT_DUPLICATE" && i.assetPath.EndsWith("M_DupA.mat"));
            var dupB = GameObject.Find("Mat/DupB").GetComponent<Renderer>();
            Check(dup != null && dup.AutoFixable && !dup.fixTouchesAsset, "duplicate materials offer a scene fix");
            if (dup != null)
            {
                AutoFixer.Apply(new List<AuditIssue> { dup });
                Check(dupB.sharedMaterial == AuditTestAssets.MatDupA, "Mat/DupB now uses M_DupA");
                Undo.PerformUndo();
                Check(dupB.sharedMaterial == AuditTestAssets.MatDupB, "Undo brings M_DupB back");
            }

            var lm = (ModelImporter)AssetImporter.GetAtPath(AuditTestAssets.Root + "/Models/LmNoUv.obj");
            Check(lm.generateSecondaryUV, "LmNoUv.obj: Generate Lightmap UVs turned on");
            var rgba = (TextureImporter)AssetImporter.GetAtPath(AuditTestAssets.Root + "/Textures/T_Rgba32.png");
            Check(rgba.GetPlatformTextureSettings("Standalone").format == TextureImporterFormat.BC7, "T_Rgba32 override is BC7");
            var a35 = (AudioImporter)AssetImporter.GetAtPath(AuditTestAssets.Root + "/Audio/A_35s.wav");
            Check(a35.defaultSampleSettings.loadType == AudioClipLoadType.CompressedInMemory && !a35.preloadAudioData, "A_35s: Compressed In Memory, no preload");
            var pcm = (AudioImporter)AssetImporter.GetAtPath(AuditTestAssets.Root + "/Audio/A_15s_pcm.wav");
            Check(pcm.defaultSampleSettings.compressionFormat == AudioCompressionFormat.Vorbis, "A_15s_pcm: Vorbis");

            return Finish("fixes", afterAssets);
        }

        // ---- picking several rows ----

        public static void RunMultiSelect()
        {
            int code;
            try
            {
                code = MultiSelectTest();
            }
            catch (Exception e)
            {
                Debug.LogError("[AuditTests] multi-select crashed: " + e);
                WriteReport("multi-select", "CRASH\n" + e);
                code = 2;
            }
            EditorApplication.Exit(code);
        }

        static int MultiSelectTest()
        {
            Begin();
            Setup("base");
            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var picked = result.issues.Where(i =>
                (i.checkId == "DC_STATIC_CANDIDATE" && (i.objectPath == "DC/Candidate" || i.objectPath == "Gen/Mirrored")) ||
                (i.checkId == "TEX_READ_WRITE" && i.assetPath.EndsWith("T_Readable.png"))).ToList();
            var setting = result.issues.FirstOrDefault(i => i.checkId == "AUDIO_DSP_BUFFER");
            Check(picked.Count == 3 && picked.All(MultiSelect.CanPick), "three rows can be picked");
            Check(setting != null && !MultiSelect.CanPick(setting), "a settings row without a fix cannot be picked");
            var selection = MultiSelect.SelectionOf(picked);
            Check(selection.Length == 3 && selection.Count(o => o is GameObject) == 2 && selection.Any(o => o is Texture),
                "selection: 2 scene objects and 1 texture asset");
            var fixes = MultiSelect.Fixable(picked);
            Check(fixes.Count == 3, "all three picked rows are fixable");
            MultiSelect.Select(selection);
            Check(Selection.objects.Length == 3, "Unity selection holds the 3 picked objects");
            AutoFixer.Apply(fixes);
            var after = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            ExpectNow(after, "after fixing the picked rows", () =>
            {
                Not("DC_STATIC_CANDIDATE", "DC/Candidate");
                Not("DC_STATIC_CANDIDATE", "Gen/Mirrored");
                Not("TEX_READ_WRITE", "T_Readable.png");
                Has("DC_STATIC_CANDIDATE", Severity.Info, "Tex/Big4096");
            });

            var rows = LightmapUvReport.Build();
            var lmPicked = rows.Where(r => r.name == "LmNoUv.obj" || r.name == "ReadWrite.obj").ToList();
            Check(lmPicked.Count == 2, "two models picked");
            Check(LightmapUvReport.TurnOnFixes(lmPicked).Count == 2, "turn on for the two picked models");
            Check(MultiSelect.AssetsOf(lmPicked).Length == 2, "two model assets to select");
            Check(MultiSelect.SceneObjectsOf(lmPicked).Length == 3, "three scene objects use them (2 + 1)");
            AutoFixer.Apply(LightmapUvReport.TurnOnFixes(lmPicked));
            var rescanned = LightmapUvReport.Build();
            Check(rescanned.Where(r => r.name == "LmNoUv.obj" || r.name == "ReadWrite.obj").All(r => r.hasUv1), "picked models now have UV1");
            Check(rescanned.Any(r => r.name == "Clean.obj" && !r.hasUv1), "models that were not picked are untouched");
            return Finish("multi-select", after);
        }

        // ---- lightmap UV report ----

        public static void RunLightmapReport()
        {
            int code;
            try
            {
                code = LightmapReport();
            }
            catch (Exception e)
            {
                Debug.LogError("[AuditTests] lightmap report crashed: " + e);
                WriteReport("lightmap-report", "CRASH\n" + e);
                code = 2;
            }
            EditorApplication.Exit(code);
        }

        static int LightmapReport()
        {
            Begin();
            Setup("base");
            var rows = LightmapUvReport.Build();
            Func<string, LightmapUvRow> row = name => rows.FirstOrDefault(r => r.name == name);
            foreach (var r in rows)
                log.AppendLine("info: " + r.name + " gen=" + (r.importer == null ? "-" : r.GenerateOn.ToString()) + " uv1=" + r.hasUv1 +
                               " out=" + r.outsideShare.ToString("F3") + " overlap=" + r.overlapShare.ToString("F3") + " objects=" + r.objects + " baked=" + r.bakedObjects);

            var readWrite = row("ReadWrite.obj");
            Check(readWrite != null && readWrite.importer != null && !readWrite.GenerateOn && !readWrite.hasUv1 && readWrite.bakedObjects == 0 && readWrite.CanTurnOn,
                "model outside lightmaps without UV1 is listed and can be turned on");
            var gen = row("LmGen.obj");
            Check(gen != null && gen.GenerateOn && gen.hasUv1 && !gen.HasProblem, "model with Generate Lightmap UVs: option on, UV1, no problem");
            var noUv = row("LmNoUv.obj");
            Check(noUv != null && noUv.objects == 2 && noUv.bakedObjects == 2, "LmNoUv.obj: 2 objects, 2 baked");
            var overlap = row("Uv1Overlap");
            Check(overlap != null && overlap.importer == null && overlap.overlapShare > 0.9f && overlap.HasProblem, "mesh asset with overlapping UV1");
            var outside = row("Uv1Outside");
            Check(outside != null && outside.outsideShare > 0f && outside.HasProblem, "mesh asset with UV1 outside 0-1");
            var good = row("Uv1Good");
            Check(good != null && !good.HasProblem, "mesh asset with clean UV1 has no problem");
            var inactive = rows.Any(r => r.meshes.Contains(AuditTestAssets.Grid12k) && r.objects >= 5);
            Check(inactive, "inactive objects are counted too");

            var fixes = LightmapUvReport.TurnOnFixes(rows);
            log.AppendLine("info: turn-on fixes = " + string.Join(", ", fixes.Select(f => f.assetPath).ToArray()));
            int canTurnOn = rows.Count(r => r.CanTurnOn);
            Check(canTurnOn >= 5 && fixes.Count == canTurnOn && fixes.All(f => f.fixTouchesAsset && f.AutoFixable),
                "turn on is offered for every model without UV1 (" + fixes.Count + ")");
            AutoFixer.Apply(fixes);
            var after = LightmapUvReport.Build();
            Check(after.Where(r => r.importer != null).All(r => r.hasUv1 && r.GenerateOn), "after turning on, every model has the option on and UV1");
            Check(LightmapUvReport.TurnOnFixes(after).Count == 0, "nothing left to turn on");

            var csv = Path.Combine(ReportDir, "lightmap-uv.csv");
            LightmapUvReport.SaveCsv(after, csv);
            Check(File.ReadAllLines(csv).Length == after.Count + 1, "CSV has a header and one line per model");
            return Finish("lightmap-report", null);
        }

        // ---- export ----

        static int ExportUnit()
        {
            Begin();
            Setup("base");
            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);

            var without = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, null));
            Check(!without.hasProfilerSnapshot, "JSON without snapshot: hasProfilerSnapshot false");
            Check(without.issues.Length == result.issues.Count, "JSON issue count");
            Check(without.summary.critical == result.Count(Severity.Critical), "JSON summary critical");
            Check(without.xr.system == "None", "JSON xr.system None");
            Check(without.toolVersion == ToolVersion.Full && ToolVersion.Build > 0, "JSON toolVersion " + without.toolVersion);

            var snapshot = new ProfilerSnapshot { frames = 300, avgFPS = 88.5f, avgCPUms = 9.2f, avgGPUms = -1f, peakGPUms = -1f, drawCalls = 640 };
            var with = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, snapshot));
            Check(with.hasProfilerSnapshot && Math.Abs(with.profilerSnapshot.avgFPS - 88.5f) < 0.01f && with.profilerSnapshot.avgGPUms < 0f,
                "JSON with snapshot keeps values and -1 for n/a");

            Check(ReportExporter.Csv("a, \"b\"") == "\"a, \"\"b\"\"\"", "CSV escaping");
            Check(ReportExporter.Csv("plain") == "plain", "CSV plain");

            Strings.Current = Lang.Ru;
            var ru = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, null));
            var overlay = ru.issues.FirstOrDefault(i => i.checkId == "VR_CANVAS_OVERLAY");
            Check(overlay != null && overlay.severity == "Critical" && overlay.message.Contains("шлеме"), "RU export: English severity, Russian message");
            Strings.Current = Lang.En;

            Directory.CreateDirectory(ReportDir);
            ReportExporter.Save(result, Path.Combine(ReportDir, "window-export.json"));
            ReportExporter.Save(result, Path.Combine(ReportDir, "window-export.csv"));
            log.AppendLine("info: issues=" + result.issues.Count);
            return Finish("export-unit", result);
        }

        // ---- plumbing ----

        static void Begin()
        {
            expects = new List<Expect>();
            failures = new List<string>();
            log = new StringBuilder();
        }

        static void Check(bool ok, string what)
        {
            log.AppendLine((ok ? "PASS " : "FAIL ") + what);
            if (!ok) failures.Add(what);
        }

        static string Brief(AuditIssue i)
        {
            return i.checkId + " " + i.severity + " " + (i.objectPath.Length > 0 ? i.objectPath : i.assetPath.Length > 0 ? i.assetPath : i.settingsPath);
        }

        static int Finish(string name, AuditResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine(name + ": " + (failures.Count == 0 ? "OK" : failures.Count + " FAILED"));
            sb.Append(log);
            if (result != null)
            {
                sb.AppendLine("== all issues (" + result.issues.Count + ")");
                foreach (var i in result.issues) sb.AppendLine(Brief(i) + (i.users > 0 ? " users=" + i.users : "") + (i.AutoFixable ? " [" + i.fixKind + "]" : ""));
            }
            WriteReport(name, sb.ToString());
            Debug.Log("[AuditTests] " + name + ": " + (failures.Count == 0 ? "OK" : failures.Count + " FAILED"));
            return failures.Count == 0 ? 0 : 1;
        }

        static void WriteReport(string name, string text)
        {
            Directory.CreateDirectory(ReportDir);
            File.WriteAllText(Path.Combine(ReportDir, name + ".txt"), text, new UTF8Encoding(false));
        }

        static Dictionary<string, string> Hashes()
        {
            var files = new List<string>
            {
                "ProjectSettings/QualitySettings.asset", "ProjectSettings/ProjectSettings.asset", "ProjectSettings/AudioManager.asset",
                AuditTestScene.SeedPath, AuditTestScene.SeedBPath
            };
            files.AddRange(Directory.GetFiles(AuditTestAssets.Root, "*.meta", SearchOption.AllDirectories).Select(f => f.Replace('\\', '/')));
            var map = new Dictionary<string, string>();
            using (var sha = System.Security.Cryptography.SHA1.Create())
                foreach (var f in files)
                    map[f] = File.Exists(f) ? BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f))) : "absent";
            return map;
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
