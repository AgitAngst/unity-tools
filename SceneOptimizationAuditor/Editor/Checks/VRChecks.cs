using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        static readonly string[] ExpensiveEffects = { "MotionBlur", "DepthOfField", "ChromaticAberration", "ScreenSpaceReflections", "AmbientOcclusion" };

        static readonly string[] SteamVrTypes =
        {
            "Valve.VR.SteamVR_PlayArea", "SteamVR_PlayArea",
            "Valve.VR.SteamVR_Behaviour_Pose",
            "Valve.VR.SteamVR_TrackedObject", "SteamVR_TrackedObject"
        };

        // The names of the Quality page; 3 and 4 exist only in the API.
        static string VSyncName(int count)
        {
            if (count == 1) return "Every V Blank";
            if (count == 2) return "Every Second V Blank";
            return count.ToString();
        }

        static void AuditVRSpecific(AuditContext ctx)
        {
            var xr = ctx.xr;
            var q = ctx.quality;
            var xrPage = xr.managementInstalled ? "Project/XR Plug-in Management" : "Project/Player";

            if (!xr.AnyEnabled)
                ctx.ReportSetting("VR_NOT_ENABLED", Category.VRSpecific, Severity.Critical, xrPage);
            else
            {
                if (!xr.HasOpenVR)
                    ctx.ReportSetting("VR_NO_OPENVR", Category.VRSpecific, Severity.Warning, xrPage, xr.DeviceName);
                if (xr.BothEnabled)
                    ctx.ReportSetting("VR_BOTH_SYSTEMS", Category.VRSpecific, Severity.Warning, xrPage);
                if (xr.stereoMode != StereoMode.Unknown && xr.stereoMode != StereoMode.SinglePassInstanced)
                    ctx.ReportSetting("VR_STEREO_MODE", Category.VRSpecific, Severity.Warning, xrPage, xr.stereoMode.ToString());
            }

            bool tierDeferred = false;
            try
            {
                var tier = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Standalone, GraphicsTier.Tier3);
                tierDeferred = IsDeferred(tier.renderingPath);
            }
            catch (Exception)
            {
                tierDeferred = false;
            }
            if (tierDeferred)
                ctx.ReportSetting("VR_DEFERRED_TIER", Category.VRSpecific, Severity.Warning, "Project/Graphics");

            if (q.antiAliasing < VRBudgets.MinAntiAliasing)
                ctx.ReportSetting("VR_MSAA", Category.VRSpecific, Severity.Warning, "Project/Quality", q.antiAliasing);
            else if (q.antiAliasing > VRBudgets.MaxAntiAliasing)
                ctx.ReportSetting("VR_MSAA_HIGH", Category.VRSpecific, Severity.Info, "Project/Quality", q.antiAliasing);
            // In VR the SDK of the headset sets the frame rate; VSync and targetFrameRate still cap the Game view of the
            // editor and the build without a headset, and so the tour without a headset.
            if (q.vSyncCount != 0)
                ctx.ReportSetting("VR_VSYNC", Category.VRSpecific, Severity.Info, "Project/Quality", VSyncName(q.vSyncCount));
            foreach (var found in FrameRateScripts.Find(FrameRateScripts.ProjectScripts()))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(found.Key);
                if (script == null) continue;
                ctx.Report("VR_FRAME_RATE_SCRIPT", Category.VRSpecific, Severity.Info, script, string.Join(", ", found.Value));
            }

            var cameras = new List<Camera>();
            foreach (var camera in ctx.All<Camera>())
                if (AuditContext.IsActive(camera) && camera.stereoTargetEye != StereoTargetEyeMask.None && camera.targetTexture == null)
                    cameras.Add(camera);
            if (cameras.Count == 0)
                ctx.Report("VR_NO_CAMERA", Category.VRSpecific, Severity.Info, null);

            foreach (var camera in cameras)
            {
                if (IsDeferred(camera.renderingPath))
                    ctx.Report("VR_DEFERRED_CAMERA", Category.VRSpecific, Severity.Warning, camera);
                if (!camera.allowMSAA)
                    ctx.Report("VR_CAMERA_NO_MSAA", Category.VRSpecific, Severity.Warning, camera);
                if (camera.allowHDR)
                    ctx.Report("VR_CAMERA_HDR", Category.VRSpecific, Severity.Warning, camera);
                if (camera.farClipPlane > VRBudgets.FarClipInfo)
                    ctx.Report("VR_FAR_CLIP", Category.VRSpecific, Severity.Info, camera, camera.farClipPlane, VRBudgets.FarClipInfo);
                if (camera.nearClipPlane < VRBudgets.NearClipInfo)
                    ctx.Report("VR_NEAR_CLIP", Category.VRSpecific, Severity.Info, camera, camera.nearClipPlane, VRBudgets.NearClipInfo);
                foreach (var behaviour in camera.GetComponents<MonoBehaviour>())
                    if (behaviour != null && ReflectionUtil.DeclaresMethod(behaviour.GetType(), "OnRenderImage"))
                        ctx.Report("VR_IMAGE_EFFECT", Category.VRSpecific, Severity.Warning, camera, behaviour.GetType().Name);
            }

            foreach (var canvas in ctx.All<Canvas>())
            {
                if (!AuditContext.IsActive(canvas) || !canvas.isRootCanvas) continue;
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    ctx.Report("VR_CANVAS_OVERLAY", Category.VRSpecific, Severity.Critical, canvas);
                else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    ctx.Report("VR_CANVAS_CAMERA", Category.VRSpecific, Severity.Warning, canvas);
            }

            AuditPostProcessing(ctx);
            AuditSteamVr(ctx);

            foreach (var probe in ctx.All<ReflectionProbe>())
            {
                if (!AuditContext.IsActive(probe) || probe.mode != ReflectionProbeMode.Realtime) continue;
                if (probe.refreshMode == ReflectionProbeRefreshMode.EveryFrame)
                    ctx.Report("VR_PROBE_EVERY_FRAME", Category.VRSpecific, Severity.Critical, probe);
                else
                    ctx.Report("VR_PROBE_REALTIME", Category.VRSpecific, Severity.Warning, probe);
            }

            int staticRenderers = 0;
            const StaticEditorFlags occlusionFlags = StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
            foreach (var renderer in ctx.All<Renderer>())
                if (AuditContext.IsActive(renderer) && (GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & occlusionFlags) != 0)
                    staticRenderers++;
            if (staticRenderers > VRBudgets.OcclusionStaticRenderers && StaticOcclusionCulling.umbraDataSize == 0)
                ctx.Report("VR_NO_OCCLUSION", Category.VRSpecific, Severity.Info, null, staticRenderers);
        }

        static bool IsDeferred(RenderingPath path)
        {
#pragma warning disable 0618
            return path == RenderingPath.DeferredShading || path == RenderingPath.DeferredLighting;
#pragma warning restore 0618
        }

        // Post Processing v2 is a package: its types are found by name and read by reflection.
        static void AuditPostProcessing(AuditContext ctx)
        {
            var layerType = ReflectionUtil.FindType("UnityEngine.Rendering.PostProcessing.PostProcessLayer");
            var volumeType = ReflectionUtil.FindType("UnityEngine.Rendering.PostProcessing.PostProcessVolume");

            if (layerType != null)
                foreach (var layer in ctx.All(layerType))
                    if (AuditContext.IsActive(layer))
                        ctx.Report("VR_PP_LAYER", Category.VRSpecific, Severity.Warning, layer);

            if (volumeType == null) return;
            foreach (var volume in ctx.All(volumeType))
            {
                if (!AuditContext.IsActive(volume)) continue;
                // sharedProfile, not profile: the profile getter makes a copy of the asset.
                var settings = ReflectionUtil.GetMember(ReflectionUtil.GetMember(volume, "sharedProfile"), "settings") as IEnumerable;
                if (settings == null) continue;
                var enabled = new List<string>();
                var expensive = new List<string>();
                foreach (var effect in settings)
                {
                    if (effect == null) continue;
                    var active = ReflectionUtil.GetMember(effect, "active");
                    var on = ReflectionUtil.GetMember(ReflectionUtil.GetMember(effect, "enabled"), "value");
                    if (!(active is bool) || !(bool)active || !(on is bool) || !(bool)on) continue;
                    var name = effect.GetType().Name;
                    enabled.Add(name);
                    if (Array.IndexOf(ExpensiveEffects, name) >= 0) expensive.Add(name);
                }
                if (enabled.Count == 0) continue;
                ctx.Report("VR_PP_EFFECTS", Category.VRSpecific, Severity.Warning, volume,
                    string.Join(", ", enabled.ToArray()), expensive.Count > 0 ? string.Join(", ", expensive.ToArray()) : "-");
            }
        }

        static void AuditSteamVr(AuditContext ctx)
        {
            var types = new List<Type>();
            foreach (var name in SteamVrTypes)
            {
                var type = ReflectionUtil.FindType(name);
                if (type != null && typeof(Component).IsAssignableFrom(type) && !types.Contains(type)) types.Add(type);
            }
            if (types.Count == 0)
            {
                ctx.Report("VR_STEAMVR_MISSING", Category.VRSpecific, Severity.Info, null);
                return;
            }
            foreach (var type in types)
                if (ctx.All(type).Count > 0) return;
            ctx.Report("VR_STEAMVR_NOT_IN_SCENE", Category.VRSpecific, Severity.Info, null);
        }
    }
}
