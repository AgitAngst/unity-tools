using System;
using System.IO;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    // Starting thresholds of one target GPU: only triangles and texture memory differ between GPUs.
    public sealed class GpuPreset
    {
        public string id;
        public string name;
        public int memoryGB;
        public int trisWarning;
        public int trisCritical;
        public int textureMemoryWarningMB;
        public int textureMemoryCriticalMB;
    }

    // Thresholds of the audit. Defaults are starting values for HTC Vive Pro on the target GPU (RTX 2070 unless
    // the project picks another); projects override them in ProjectSettings/SceneOptimizationAuditor.json.
    [Serializable]
    public sealed class VRBudgets
    {
        // Texture memory takes the share of GPU memory the old GTX 1070 defaults took (19 % / 25 % of 8 GB),
        // triangles follow the GPU speed against the GTX 1070 (2 / 4 million). Owner, 24.10.2026.
        public static readonly GpuPreset[] Gpus =
        {
            new GpuPreset { id = "RTX2060", name = "RTX 2060", memoryGB = 6, trisWarning = 2500000, trisCritical = 5000000, textureMemoryWarningMB = 1152, textureMemoryCriticalMB = 1536 },
            new GpuPreset { id = "RTX2070", name = "RTX 2070", memoryGB = 8, trisWarning = 3000000, trisCritical = 6000000, textureMemoryWarningMB = 1536, textureMemoryCriticalMB = 2048 },
            new GpuPreset { id = "RTX2080", name = "RTX 2080", memoryGB = 8, trisWarning = 3500000, trisCritical = 7000000, textureMemoryWarningMB = 1536, textureMemoryCriticalMB = 2048 },
            new GpuPreset { id = "RTX3060", name = "RTX 3060", memoryGB = 12, trisWarning = 3000000, trisCritical = 6000000, textureMemoryWarningMB = 2304, textureMemoryCriticalMB = 3072 },
            // About as fast as an RTX 2080 (owner, 27.10.2026: "add 2070 and 4060, 2070 by default").
            new GpuPreset { id = "RTX4060", name = "RTX 4060", memoryGB = 8, trisWarning = 3500000, trisCritical = 7000000, textureMemoryWarningMB = 1536, textureMemoryCriticalMB = 2048 },
        };
        public const string DefaultGpu = "RTX2070";

        // Id of the target GPU; empty in a budget file saved before GPUs could be picked.
        public string targetGpu = DefaultGpu;
        public int drawCallsWarning = 1000;
        public int drawCallsCritical = 2000;
        public int sceneTrisWarning = 3000000;
        public int sceneTrisCritical = 6000000;
        public int objectTrisWarning = 10000;
        public int objectTrisCritical = 25000;
        public int textureMemoryWarningMB = 1536;
        public int textureMemoryCriticalMB = 2048;
        public int textureSizeWarning = 2048;
        public int textureSizeCritical = 4096;
        public int shadowLightsWarning = 1;
        public int shadowLightsCritical = 2;
        public int animatorsWarning = 20;
        public int skinnedTrisWarning = 15000;
        public int audioSourcesWarning = 32;
        public int colliderTrisInfo = 5000;
        public float frameTimeWarningMs = 10f;
        public float frameTimeCriticalMs = 11.1f;
        // Share of the screen under transparent objects in one view, percent; above 100 means layers of overdraw.
        public int transparentWarningPercent = 100;
        public int transparentCriticalPercent = 200;
        // Memory of the lightmaps of the loaded scenes: color, direction and shadowmask textures.
        public int lightmapMemoryWarningMB = 128;
        public int lightmapMemoryCriticalMB = 256;

        public static GpuPreset FindGpu(string id)
        {
            foreach (var gpu in Gpus) if (gpu.id == id) return gpu;
            return null;
        }

        public GpuPreset Gpu { get { return FindGpu(targetGpu); } }

        // Thresholds edited by hand, or a file saved before GPUs could be picked.
        public bool IsCustom
        {
            get
            {
                var gpu = Gpu;
                return gpu == null || sceneTrisWarning != gpu.trisWarning || sceneTrisCritical != gpu.trisCritical ||
                       textureMemoryWarningMB != gpu.textureMemoryWarningMB || textureMemoryCriticalMB != gpu.textureMemoryCriticalMB;
            }
        }

        // Id written to reports: the GPU, or "Custom" when the thresholds differ from its set.
        public string GpuReportName { get { return IsCustom ? "Custom" : targetGpu; } }

        public void ApplyGpu(string id)
        {
            var gpu = FindGpu(id);
            if (gpu == null) return;
            targetGpu = gpu.id;
            sceneTrisWarning = gpu.trisWarning;
            sceneTrisCritical = gpu.trisCritical;
            textureMemoryWarningMB = gpu.textureMemoryWarningMB;
            textureMemoryCriticalMB = gpu.textureMemoryCriticalMB;
        }

        // The other thresholds back to defaults, triangles and texture memory to the set of the target GPU.
        public static VRBudgets DefaultsFor(string id)
        {
            var budgets = new VRBudgets();
            budgets.ApplyGpu(FindGpu(id) != null ? id : DefaultGpu);
            return budgets;
        }

        // Fixed limits, kept here so every number of the audit lives in one place.
        public const int TargetFps = 90;
        public const float TargetFrameMs = 1000f / TargetFps;
        public const int MultiMaterialWarning = 4;
        public const int InstancingMinInstances = 3;
        public const int MaxBonesPerVertex = 4;
        public const float DecompressOnLoadMaxSeconds = 10f;
        public const float StreamingFromSeconds = 60f;
        public const long PcmMaxBytes = 1024 * 1024;
        public const float PreloadMaxSeconds = 30f;
        public const int PriorityZeroMax = 5;
        public const float ShadowDistanceInfo = 50f;
        public const int SolverIterationsInfo = 6;
        public const float FixedStepTolerance = 0.0002f;
        public const float FarClipInfo = 1000f;
        // Unity clamps the near plane to 0.01, so the check looks at values close to that minimum (owner, 22.10.2026).
        public const float NearClipInfo = 0.05f;
        public const int OcclusionStaticRenderers = 100;
        public const float LightmapUvTolerance = 0.001f;
        public const int LightmapUvOverlapGrid = 256;
        public const float LightmapUvOverlapMinShare = 0.01f;
        public const int HierarchyDepthInfo = 10;
        // 3D assets.
        public const int SplitMeshMinTris = 500;
        public const float SplitVerticesPerTriangle = 1.5f;
        public const float DegenerateShareInfo = 0.01f;
        public const float PivotFarMinMeters = 1f;
        public const float PivotFarExtents = 3f;
        public const float ExtremeScaleMax = 50f;
        public const float ExtremeScaleMin = 0.02f;
        public const float DenseSmallMaxMeters = 0.3f;
        public const int DenseSmallMinTris = 2000;
        public const float WeakLodShare = 0.7f;
        public const float LateLodTransition = 0.15f;
        public const int DensityHighMinSide = 1024;
        public const float DensityHighPxPerMeter = 4096f;
        public const float DensityLowMinMeters = 4f;
        public const float DensityLowPxPerMeter = 64f;
        public const float LargeFlatMinMeters = 4f;
        public const float LargeFlatThinShare = 0.1f;
        public const int AnisoLevelFix = 4;
        public const int StreamingMinSide = 2048;
        public const long AlphaScanMaxBytes = 16 * 1024 * 1024;
        public const int TransparentQueue = 2501;
        public const float TransparentLargeMeters = 2f;
        public const int MinAntiAliasing = 4;
        public const float FpsWarning = 89f;
        public const float FpsCritical = 80f;
        // Viewpoints: one view frustum for both eyes of the Vive Pro, placement grid and limits.
        public const float ViewFieldOfView = 110f;
        public const float ViewAspect = 0.9f;
        public const float ViewNear = 0.05f;
        public const float ViewFar = 1000f;
        public const int ViewDirections = 8;
        public const float EyeHeight = 1.7f;
        public const float GridStep = 4f;
        public const float GridStepMin = 1f;
        public const float GridStepMax = 20f;
        public const int MaxViewpoints = 500;
        public const float GridStepGrowth = 1.25f;
        public const float RayMaxSlope = 45f;
        public const int HeavyTop = 10;
        // Marks of "What is heavy here".
        public const float FarLod0ScreenShare = 0.05f;
        public const int FarLod0MinTris = 2000;
        public const float SmallShadowMeters = 0.3f;
        public const float LargeTransparentShare = 0.25f;
        // Lighting tab: a lightmapped object smaller than this is cheaper and seamless on light probes.
        public const float SmallLightmapMeters = 0.5f;
        public const float LightmapShareWarning = 0.2f;
        // A realtime point or spot light reaching more renderers than this costs a pass on each of them.
        public const int WideLightRenderers = 50;
        // The probe area is the box of all probes grown by this much on every side.
        public const float ProbeAreaMargin = 1f;
        // Particles: alive at once per system and in the scene, and the size of a large transparent particle.
        public const int ParticlesWarning = 1000;
        public const int ParticlesCritical = 5000;
        public const int ParticlesTotalWarning = 20000;
        public const float LargeParticleMeters = 1f;
        public const int LargeParticleMinCount = 20;
        // Reflection probes: face resolution, memory of all probes, a room-sized probe and the overlap of a duplicate.
        public const int ReflectionResolutionWarning = 256;
        public const int ReflectionResolutionCritical = 1024;
        public const int ReflectionMemoryWarningMB = 64;
        public const float RoomProbeMeters = 30f;
        public const float ReflectionDuplicateShare = 0.8f;
        // Gains: a texture denser than needed comes down to this many pixels per metre of its smallest object; a clip
        // compressed in memory takes about this part of its PCM; compressed lightmaps this part of uncompressed ones;
        // a LOD level this part of the mesh.
        public const float DensityGainPxPerMeter = 1024f;
        public const double CompressedAudioShare = 0.15;
        public const double CompressedLightmapShare = 0.25;
        public const double LodTrisShare = 0.5;
        // A shader of the project with more variants than this builds and loads slowly.
        public const int ShaderVariantsInfo = 512;

        public const string FileName = "SceneOptimizationAuditor.json";

        public static string FilePath
        {
            get
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                return Path.Combine(Path.Combine(projectRoot, "ProjectSettings"), FileName);
            }
        }

        public static bool FileExists { get { return File.Exists(FilePath); } }

        public static VRBudgets Load()
        {
            var budgets = new VRBudgets();
            if (!File.Exists(FilePath)) return budgets;
            try
            {
                var json = File.ReadAllText(FilePath);
                JsonUtility.FromJsonOverwrite(json, budgets);
                // A file from before GPUs could be picked keeps its thresholds as custom ones.
                if (json.IndexOf("\"targetGpu\"", StringComparison.Ordinal) < 0) budgets.targetGpu = "";
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneAudit] Could not read budgets from " + FilePath + ": " + e.Message);
            }
            return budgets;
        }

        public void Save()
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
            Debug.Log("[SceneAudit] Budgets saved to " + FilePath);
        }
    }
}
