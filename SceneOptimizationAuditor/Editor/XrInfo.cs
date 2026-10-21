using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    public enum StereoMode { Unknown, MultiPass, SinglePass, SinglePassInstanced }

    // VR state for Standalone: legacy built-in VR and XR Plug-in Management (read by reflection).
    public sealed class XrInfo
    {
        public bool legacyEnabled;
        public string[] legacySdks = new string[0];
        public bool managementInstalled;
        public string[] loaders = new string[0];
        public StereoMode stereoMode = StereoMode.Unknown;

        public bool ManagementEnabled { get { return loaders.Length > 0; } }
        public bool AnyEnabled { get { return legacyEnabled || ManagementEnabled; } }
        public bool BothEnabled { get { return legacyEnabled && ManagementEnabled; } }

        public bool HasOpenVR
        {
            get
            {
                if (legacyEnabled && Contains(legacySdks, "OpenVR")) return true;
                return ManagementEnabled && Contains(loaders, "OpenVR");
            }
        }

        // Every stereo mode except Single Pass Instanced issues each draw call once per eye.
        public bool DoublesDrawCalls
        {
            get { return AnyEnabled && (stereoMode == StereoMode.MultiPass || stereoMode == StereoMode.SinglePass); }
        }

        // Machine-readable name for reports.
        public string SystemName
        {
            get
            {
                if (BothEnabled) return "Legacy+XRManagement";
                if (legacyEnabled) return "Legacy";
                if (ManagementEnabled) return "XRManagement";
                return "None";
            }
        }

        public string DeviceName
        {
            get
            {
                if (HasOpenVR) return "OpenVR";
                if (ManagementEnabled) return string.Join(", ", loaders);
                if (legacyEnabled) return string.Join(", ", legacySdks);
                return "";
            }
        }

        public static XrInfo Detect()
        {
            var info = new XrInfo();
            info.DetectLegacy();
            info.DetectManagement();
            return info;
        }

        void DetectLegacy()
        {
#pragma warning disable 0618
            try
            {
                legacySdks = PlayerSettings.GetVirtualRealitySDKs(BuildTargetGroup.Standalone) ?? new string[0];
            }
            catch (Exception)
            {
                legacySdks = new string[0];
            }

            bool found;
            legacyEnabled = ReadLegacyEnabledForStandalone(out found);
            if (!found && EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Standalone)
                legacyEnabled = PlayerSettings.virtualRealitySupported;

            if (legacyEnabled)
            {
                switch (PlayerSettings.stereoRenderingPath)
                {
                    case StereoRenderingPath.MultiPass: stereoMode = StereoMode.MultiPass; break;
                    case StereoRenderingPath.SinglePass: stereoMode = StereoMode.SinglePass; break;
                    case StereoRenderingPath.Instancing: stereoMode = StereoMode.SinglePassInstanced; break;
                }
            }
#pragma warning restore 0618
        }

        // Reads m_BuildTargetVRSettings from ProjectSettings.asset: the flag for Standalone only.
        static bool ReadLegacyEnabledForStandalone(out bool found)
        {
            found = false;
            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (assets == null || assets.Length == 0) return false;
                var so = new SerializedObject(assets[0]);
                var list = so.FindProperty("m_BuildTargetVRSettings");
                if (list == null || !list.isArray) return false;
                for (int i = 0; i < list.arraySize; i++)
                {
                    var entry = list.GetArrayElementAtIndex(i);
                    var target = entry.FindPropertyRelative("m_BuildTarget");
                    var enabled = entry.FindPropertyRelative("m_Enabled");
                    if (target == null || enabled == null || target.stringValue != "Standalone") continue;
                    found = true;
                    return enabled.boolValue;
                }
            }
            catch (Exception)
            {
                found = false;
            }
            return false;
        }

        void DetectManagement()
        {
            var perTarget = ReflectionUtil.FindType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget");
            managementInstalled = perTarget != null;
            if (!managementInstalled) return;

            var names = new List<string>();
            try
            {
                var method = ReflectionUtil.FindStaticMethod(perTarget, "XRGeneralSettingsForBuildTarget",
                    new[] { typeof(BuildTargetGroup) });
                var general = method != null ? method.Invoke(null, new object[] { BuildTargetGroup.Standalone }) : null;
                var manager = ReflectionUtil.GetMember(general, "Manager") ?? ReflectionUtil.GetMember(general, "AssignedSettings");
                var list = (ReflectionUtil.GetMember(manager, "activeLoaders") ?? ReflectionUtil.GetMember(manager, "loaders")) as IEnumerable;
                if (list != null)
                {
                    foreach (var loader in list)
                    {
                        var obj = loader as Object;
                        if (obj != null) names.Add(obj.GetType().Name);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SceneAudit] Could not read XR Plug-in Management settings: " + e.Message);
            }
            loaders = names.ToArray();

            if (ManagementEnabled && Contains(loaders, "OpenVR"))
            {
                var mode = ReadOpenVrStereoMode();
                if (mode != StereoMode.Unknown) stereoMode = mode;
            }
        }

        // OpenVR XR Plugin keeps its stereo mode in an OpenVRSettings asset; it is found, never created.
        static StereoMode ReadOpenVrStereoMode()
        {
            try
            {
                var guids = AssetDatabase.FindAssets("t:OpenVRSettings");
                foreach (var guid in guids)
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                    var value = ReflectionUtil.GetMember(asset, "StereoRenderingMode");
                    if (value == null) continue;
                    var text = value.ToString();
                    if (text.IndexOf("Instanc", StringComparison.OrdinalIgnoreCase) >= 0) return StereoMode.SinglePassInstanced;
                    if (text.IndexOf("Multi", StringComparison.OrdinalIgnoreCase) >= 0) return StereoMode.MultiPass;
                    if (text.IndexOf("Single", StringComparison.OrdinalIgnoreCase) >= 0) return StereoMode.SinglePass;
                }
            }
            catch (Exception)
            {
                return StereoMode.Unknown;
            }
            return StereoMode.Unknown;
        }

        static bool Contains(string[] items, string part)
        {
            foreach (var item in items)
                if (item != null && item.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
