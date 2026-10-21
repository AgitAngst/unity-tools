using System.IO;
using Unity.XR.OpenVR;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

namespace AuditTestsXR
{
    // XR Plug-in Management settings for Standalone; called by the test runner through reflection,
    // so the runner still compiles in a project without the package.
    public static class XrSetup
    {
        const string Dir = "Assets/AuditGenerated/XR";

        // loader: "openvr", "mock" or null (no loaders); stereo: "MultiPass" or "SinglePassInstanced".
        public static void Configure(string loader, string stereo)
        {
            Directory.CreateDirectory(Dir);
            AssetDatabase.Refresh();
            var perTarget = PerTarget();
            var general = perTarget.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (general == null)
            {
                general = ScriptableObject.CreateInstance<XRGeneralSettings>();
                general.name = "Standalone Settings";
                AssetDatabase.AddObjectToAsset(general, perTarget);
                perTarget.SetSettingsForBuildTarget(BuildTargetGroup.Standalone, general);
            }
            var manager = general.Manager;
            if (manager == null)
            {
                manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = "Standalone Providers";
                AssetDatabase.AddObjectToAsset(manager, perTarget);
                general.Manager = manager;
            }
#pragma warning disable 0618
            manager.loaders.Clear();
            if (loader == "openvr") manager.loaders.Add(Asset<OpenVRLoader>("OpenVRLoader"));
            if (loader == "mock") manager.loaders.Add(Asset<MockLoader>("MockLoader"));
#pragma warning restore 0618

            var settings = Asset<OpenVRSettings>("OpenVRSettings");
            settings.StereoRenderingMode = stereo == "MultiPass"
                ? OpenVRSettings.StereoRenderingModes.MultiPass
                : OpenVRSettings.StereoRenderingModes.SinglePassInstanced;
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(perTarget);
            AssetDatabase.SaveAssets();
        }

        static XRGeneralSettingsPerBuildTarget PerTarget()
        {
            XRGeneralSettingsPerBuildTarget perTarget;
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget) && perTarget != null)
                return perTarget;
            var path = Dir + "/XRGeneralSettings.asset";
            perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
            if (perTarget == null)
            {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, path);
            }
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            return perTarget;
        }

        static T Asset<T>(string name) where T : ScriptableObject
        {
            var path = Dir + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
