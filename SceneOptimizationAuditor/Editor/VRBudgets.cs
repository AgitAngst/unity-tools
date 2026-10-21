using System;
using System.IO;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    // Thresholds of the audit. Defaults are starting values for HTC Vive Pro on a GTX 1070;
    // projects override them in ProjectSettings/SceneOptimizationAuditor.json.
    [Serializable]
    public sealed class VRBudgets
    {
        public int drawCallsWarning = 1000;
        public int drawCallsCritical = 2000;
        public int sceneTrisWarning = 2000000;
        public int sceneTrisCritical = 4000000;
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
        public const int HierarchyDepthInfo = 10;
        public const int MinAntiAliasing = 4;
        public const float FpsWarning = 89f;
        public const float FpsCritical = 80f;

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
                JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), budgets);
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
