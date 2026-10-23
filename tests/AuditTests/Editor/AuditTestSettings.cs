using System.IO;
using UnityTools.SceneOptimization;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuditTestsEditor
{
    // Project settings for each variant. Every run first restores the baseline, so variants do not leak.
    public static class AuditTestSettings
    {
        public static int StandaloneLevel;

        public static void ApplyBaseline()
        {
#pragma warning disable 0618
            PlayerSettings.SetVirtualRealitySDKs(BuildTargetGroup.Standalone, new string[0]);
            PlayerSettings.virtualRealitySupported = false;
            PlayerSettings.stereoRenderingPath = StereoRenderingPath.MultiPass;
#pragma warning restore 0618
            PlayerSettings.gpuSkinning = true;
            if (File.Exists(AcceptedList.FilePath)) File.Delete(AcceptedList.FilePath);
            if (Directory.Exists(AuditHistory.Folder)) Directory.Delete(AuditHistory.Folder, true);
            if (PlayerSettings.colorSpace != ColorSpace.Linear) PlayerSettings.colorSpace = ColorSpace.Linear;
            Physics.defaultSolverIterations = 6;
            Time.fixedDeltaTime = 0.02f;
            SetTierPath(RenderingPath.Forward);
            SetAudio("m_RequestedDSPBufferSize", 0);
            SetAudio("Default Speaker Mode", (int)AudioSpeakerMode.Stereo);
            SetAudioString("m_SpatializerPlugin", "");

            StandaloneLevel = QualitySettings.GetQualityLevel();
            SetStandaloneDefault(StandaloneLevel);
            ConfigureLevel(StandaloneLevel, 4, 1, ShadowResolution.Medium, 40f, AnisotropicFiltering.Enable, SkinWeights.FourBones);
        }

        public static void SetLegacyVr(string[] sdks, StereoRenderingPath path)
        {
#pragma warning disable 0618
            PlayerSettings.SetVirtualRealitySDKs(BuildTargetGroup.Standalone, sdks);
            PlayerSettings.virtualRealitySupported = true;
            PlayerSettings.stereoRenderingPath = path;
#pragma warning restore 0618
        }

        // Makes Standalone use another level with bad values while the current level stays as it is.
        public static int ApplyBadQualityOnOtherLevel()
        {
            int current = QualitySettings.GetQualityLevel();
            int other = current == 0 ? 1 : 0;
            ConfigureLevel(other, 2, 4, ShadowResolution.VeryHigh, 150f, AnisotropicFiltering.Disable, SkinWeights.Unlimited);
            QualitySettings.SetQualityLevel(current, false);
            SetStandaloneDefault(other);
            return other;
        }

        public static void ApplyProjectVariant()
        {
            PlayerSettings.gpuSkinning = false;
            Physics.defaultSolverIterations = 10;
            Time.fixedDeltaTime = 0.005f;
            SetTierPath(RenderingPath.DeferredShading);
            SetAudio("Default Speaker Mode", (int)AudioSpeakerMode.Mode5point1);
            SetAudio("m_RequestedDSPBufferSize", 256);
        }

        static void ConfigureLevel(int level, int aa, int pixelLights, ShadowResolution resolution, float distance,
            AnisotropicFiltering aniso, SkinWeights weights)
        {
            QualitySettings.SetQualityLevel(level, false);
            QualitySettings.antiAliasing = aa;
            QualitySettings.pixelLightCount = pixelLights;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = resolution;
            QualitySettings.shadowDistance = distance;
            QualitySettings.shadowCascades = 2;
            QualitySettings.anisotropicFiltering = aniso;
            QualitySettings.skinWeights = weights;
            QualitySettings.streamingMipmapsActive = true;
        }

        static void SetStandaloneDefault(int level)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            var so = new SerializedObject(assets[0]);
            var map = so.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < map.arraySize; i++)
            {
                var pair = map.GetArrayElementAtIndex(i);
                if (pair.FindPropertyRelative("first").stringValue == "Standalone")
                    pair.FindPropertyRelative("second").intValue = level;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetTierPath(RenderingPath path)
        {
            var tier = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Standalone, GraphicsTier.Tier3);
            tier.renderingPath = path;
            EditorGraphicsSettings.SetTierSettings(BuildTargetGroup.Standalone, GraphicsTier.Tier3, tier);
        }

        static SerializedObject AudioManager()
        {
            return new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0]);
        }

        static void SetAudio(string property, int value)
        {
            var so = AudioManager();
            var p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogError("[AuditTests] AudioManager has no " + property);
                return;
            }
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetAudioString(string property, string value)
        {
            var so = AudioManager();
            var p = so.FindProperty(property);
            if (p == null) return;
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetSteamVrStub(bool on)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            var list = new System.Collections.Generic.List<string>(defines.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
            list.Remove("AUDIT_STEAMVR_STUB");
            if (on) list.Add("AUDIT_STEAMVR_STUB");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Join(";", list.ToArray()));
        }
    }
}
