using System;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    // Values of the quality level Standalone uses by default (or the current level as a fallback).
    public sealed class QualitySnapshot
    {
        public int index;
        public string name = "";
        public bool isStandaloneDefault;
        public int pixelLightCount;
        public int antiAliasing;
        public ShadowQuality shadows;
        public ShadowResolution shadowResolution;
        public float shadowDistance;
        public int shadowCascades;
        public AnisotropicFiltering anisotropicFiltering;
        public SkinWeights skinWeights;

        public static QualitySnapshot Capture()
        {
            int current = QualitySettings.GetQualityLevel();
            bool found;
            int target = ReadStandaloneDefault(out found);
            var names = QualitySettings.names;
            if (!found || target < 0 || target >= names.Length) target = current;

            var snapshot = new QualitySnapshot { index = target, isStandaloneDefault = found };
            if (target >= 0 && target < names.Length) snapshot.name = names[target];

            // Public API only reads the current level, so switch for a moment and switch back.
            bool switched = target != current;
            try
            {
                if (switched) QualitySettings.SetQualityLevel(target, false);
                snapshot.pixelLightCount = QualitySettings.pixelLightCount;
                snapshot.antiAliasing = QualitySettings.antiAliasing;
                snapshot.shadows = QualitySettings.shadows;
                snapshot.shadowResolution = QualitySettings.shadowResolution;
                snapshot.shadowDistance = QualitySettings.shadowDistance;
                snapshot.shadowCascades = QualitySettings.shadowCascades;
                snapshot.anisotropicFiltering = QualitySettings.anisotropicFiltering;
                snapshot.skinWeights = QualitySettings.skinWeights;
            }
            finally
            {
                if (switched) QualitySettings.SetQualityLevel(current, false);
            }
            return snapshot;
        }

        static int ReadStandaloneDefault(out bool found)
        {
            found = false;
            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
                if (assets == null || assets.Length == 0) return -1;
                var so = new SerializedObject(assets[0]);
                var map = so.FindProperty("m_PerPlatformDefaultQuality");
                if (map == null || !map.isArray) return -1;
                for (int i = 0; i < map.arraySize; i++)
                {
                    var pair = map.GetArrayElementAtIndex(i);
                    var key = pair.FindPropertyRelative("first");
                    var value = pair.FindPropertyRelative("second");
                    if (key == null || value == null || key.stringValue != "Standalone") continue;
                    found = true;
                    return value.intValue;
                }
            }
            catch (Exception)
            {
                found = false;
            }
            return -1;
        }
    }
}
