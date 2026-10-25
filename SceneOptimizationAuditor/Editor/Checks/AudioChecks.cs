using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        static void AuditAudio(AuditContext ctx)
        {
            var b = ctx.budgets;

            foreach (var pair in ctx.clips)
            {
                var clip = pair.Key;
                var path = AssetDatabase.GetAssetPath(clip);
                if (!AuditContext.IsProjectAssetPath(path)) continue;
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                var settings = ReadSampleSettings(importer);
                float length = clip.length;
                var lengthText = Math.Round(length, 1);

                if (settings.loadType == AudioClipLoadType.DecompressOnLoad && length > VRBudgets.DecompressOnLoadMaxSeconds)
                {
                    var issue = ctx.Report("AUDIO_DECOMPRESS_LONG", Category.Audio, Severity.Warning, clip, lengthText);
                    issue.users = pair.Value;
                    // Decompressed PCM against the clip kept compressed or streamed.
                    issue.Gain(FixGain.PcmMB(clip) * (1 - VRBudgets.CompressedAudioShare), 0, 0, true);
                    var newType = length >= VRBudgets.StreamingFromSeconds ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
                    issue.SetFix(FixKind.Review, importer, true, () =>
                    {
                        var s = ReadSampleSettings(importer);
                        s.loadType = newType;
                        WriteSampleSettings(importer, s);
                        importer.SaveAndReimport();
                    });
                }

                if (settings.compressionFormat == AudioCompressionFormat.PCM)
                {
                    long bytes = (long)clip.samples * clip.channels * 2;
                    if (bytes > VRBudgets.PcmMaxBytes)
                    {
                        var issue = ctx.Report("AUDIO_PCM_LARGE", Category.Audio, Severity.Warning, clip, Math.Round(bytes / (1024.0 * 1024.0), 1));
                        issue.users = pair.Value;
                        // Decompressed on load, the clip is PCM in memory whatever its format: only the build gets smaller.
                        if (settings.loadType != AudioClipLoadType.DecompressOnLoad)
                            issue.Gain(bytes / (1024.0 * 1024.0) * (1 - VRBudgets.CompressedAudioShare), 0, 0, true);
                        issue.SetFix(FixKind.Review, importer, true, () =>
                        {
                            var s = ReadSampleSettings(importer);
                            s.compressionFormat = AudioCompressionFormat.Vorbis;
                            s.quality = 0.7f;
                            WriteSampleSettings(importer, s);
                            importer.SaveAndReimport();
                        });
                    }
                }

                if (importer.preloadAudioData && length > VRBudgets.PreloadMaxSeconds)
                {
                    var issue = ctx.Report("AUDIO_PRELOAD_LONG", Category.Audio, Severity.Warning, clip, lengthText);
                    issue.users = pair.Value;
                    issue.SetFix(FixKind.Review, importer, true, () =>
                    {
                        importer.preloadAudioData = false;
                        importer.SaveAndReimport();
                    });
                }
            }

            int active = 0;
            int priorityZero = 0;
            foreach (var source in ctx.All<AudioSource>())
            {
                if (!AuditContext.IsActive(source)) continue;
                active++;
                if (source.priority == 0) priorityZero++;
                if (source.clip == null)
                    ctx.Report("AUDIO_NO_CLIP", Category.Audio, Severity.Info, source);
                if (source.spatialBlend <= 0f)
                {
                    var issue = ctx.Report("AUDIO_2D_SOURCE", Category.Audio, Severity.Info, source);
                    var target = source;
                    issue.SetFix(FixKind.Review, target, false, () => target.spatialBlend = 1f);
                }
            }
            if (active > b.audioSourcesWarning)
                ctx.Report("AUDIO_SOURCE_COUNT", Category.Audio, Severity.Warning, null, active, b.audioSourcesWarning,
                    AudioSettings.GetConfiguration().numRealVoices);
            if (priorityZero > VRBudgets.PriorityZeroMax)
                ctx.Report("AUDIO_PRIORITY_ZERO", Category.Audio, Severity.Warning, null, priorityZero, VRBudgets.PriorityZeroMax);

            var listeners = new List<AudioListener>();
            foreach (var listener in ctx.All<AudioListener>())
                if (AuditContext.IsActive(listener)) listeners.Add(listener);
            if (listeners.Count > 1)
                foreach (var listener in listeners)
                    ctx.Report("AUDIO_LISTENERS_MANY", Category.Audio, Severity.Critical, listener, listeners.Count);
            else if (listeners.Count == 0)
                ctx.Report("AUDIO_NO_LISTENER", Category.Audio, Severity.Warning, null);
            foreach (var listener in listeners)
                if (listener.GetComponent<Camera>() == null)
                    ctx.Report("AUDIO_LISTENER_NO_CAMERA", Category.Audio, Severity.Info, listener);

            // m_DSPBufferSize holds the resolved size; the setting itself is m_RequestedDSPBufferSize (0 = Default).
            int dspBuffer = ReadAudioManagerInt("m_RequestedDSPBufferSize", ReadAudioManagerInt("m_DSPBufferSize", -1));
            if (dspBuffer == 0)
                ctx.ReportSetting("AUDIO_DSP_BUFFER", Category.Audio, Severity.Info, "Project/Audio", "Default");
            else if (dspBuffer == 256)
                ctx.ReportSetting("AUDIO_DSP_BUFFER", Category.Audio, Severity.Info, "Project/Audio", "Best Latency");

            int speakerValue = ReadAudioManagerInt("Default Speaker Mode", ReadAudioManagerInt("m_DefaultSpeakerMode", -1));
            var speakerMode = speakerValue >= 0 ? ((AudioSpeakerMode)speakerValue).ToString() : AudioSettings.GetConfiguration().speakerMode.ToString();
            if (speakerMode != "Mono" && speakerMode != "Stereo" && speakerMode != "Raw")
                ctx.ReportSetting("AUDIO_SPEAKER_MODE", Category.Audio, Severity.Warning, "Project/Audio", speakerMode);

            if (string.IsNullOrEmpty(AudioSettings.GetSpatializerPluginName()))
                ctx.ReportSetting("AUDIO_NO_SPATIALIZER", Category.Audio, Severity.Info, "Project/Audio");
        }

        // Standalone override when set, otherwise the default sample settings.
        static AudioImporterSampleSettings ReadSampleSettings(AudioImporter importer)
        {
            return importer.ContainsSampleSettingsOverride("Standalone")
                ? importer.GetOverrideSampleSettings("Standalone")
                : importer.defaultSampleSettings;
        }

        static void WriteSampleSettings(AudioImporter importer, AudioImporterSampleSettings settings)
        {
            if (importer.ContainsSampleSettingsOverride("Standalone")) importer.SetOverrideSampleSettings("Standalone", settings);
            else importer.defaultSampleSettings = settings;
        }

        static int ReadAudioManagerInt(string property, int fallback)
        {
            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset");
                if (assets == null || assets.Length == 0) return fallback;
                var value = new SerializedObject(assets[0]).FindProperty(property);
                return value != null ? value.intValue : fallback;
            }
            catch (Exception)
            {
                return fallback;
            }
        }
    }
}
