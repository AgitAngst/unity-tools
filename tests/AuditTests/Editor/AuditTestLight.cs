using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // The Lighting tab and its checks on made-up lightmaps: atlases, indices and scale-offsets, probes and lights are
    // set in the editor, nothing is baked.
    public static partial class AuditTestViewpoints
    {
        const string LightScene = "Light-Atlases";

        static readonly string[] LightChecks =
        {
            "LIGHT_LIGHTMAP_MEMORY", "LIGHT_LIGHTMAP_UNCOMPRESSED", "LIGHT_LIGHTMAP_DIRECTIONAL", "LIGHT_NOT_BAKED",
            "LIGHT_SMALL_IN_LIGHTMAP", "LIGHT_LIGHTMAP_SHARE", "LIGHT_NO_PROBES", "LIGHT_OUTSIDE_PROBES",
            "LIGHT_DISTANCE_SHADOWMASK", "LIGHT_SHADOW_CASCADES", "LIGHT_WIDE_REALTIME", "LIGHT_IMPORTANT"
        };

        public static void RunLight()
        {
            Run("light", Light);
        }

        static void Light()
        {
            var quality = SetAllQuality(4, ShadowmaskMode.DistanceShadowmask);
            try
            {
                BuildLightScene();
                LightReportChecks();
                LightAuditChecks();
                LightFix();
                NothingBakedScene();
                QuietLightScene();
            }
            finally
            {
                RestoreQuality(quality);
                LightmapSettings.lightmaps = new LightmapData[0];
            }
        }

        // Cascades and shadowmask mode on every quality level, so the level the audit reads has them too.
        static List<KeyValuePair<int, ShadowmaskMode>> SetAllQuality(int cascades, ShadowmaskMode mask)
        {
            var old = new List<KeyValuePair<int, ShadowmaskMode>>();
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                old.Add(new KeyValuePair<int, ShadowmaskMode>(QualitySettings.shadowCascades, QualitySettings.shadowmaskMode));
                QualitySettings.shadowCascades = cascades;
                QualitySettings.shadowmaskMode = mask;
                if (QualitySettings.shadows == ShadowQuality.Disable) QualitySettings.shadows = ShadowQuality.All;
            }
            QualitySettings.SetQualityLevel(current, false);
            return old;
        }

        static void RestoreQuality(List<KeyValuePair<int, ShadowmaskMode>> old)
        {
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < old.Count && i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.shadowCascades = old[i].Key;
                QualitySettings.shadowmaskMode = old[i].Value;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        static void Lightmapped(GameObject go, int atlas, float scale, Vector2 offset)
        {
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            var r = go.GetComponent<MeshRenderer>();
            r.receiveGI = ReceiveGI.Lightmaps;
            r.lightmapIndex = atlas;
            r.lightmapScaleOffset = new Vector4(scale, scale, offset.x, offset.y);
        }

        static Light AddLight(string name, LightType type, LightmapBakeType mode, Vector3 position, float range, LightShadows shadows)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = type;
            light.lightmapBakeType = mode;
            light.transform.position = position;
            light.range = range;
            light.shadows = shadows;
            if (type == LightType.Directional) light.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            return light;
        }

        // A lit room, lightmaps not compressed and Mixed lighting in Shadowmask mode: two atlases (directional 1024 and 512), a big floor taking 30 % of atlas #0, a small cup, a lamp
        // without a lightmap, 60 small moving props under probes, one crate 6 m outside them, and four lights.
        public static string BuildLightScene()
        {
            var path = NewScene(LightScene);
            // Lighting settings belong to the scene: set them on the new one.
            LightmapEditorSettings.textureCompression = false;
            LightmapEditorSettings.mixedBakeMode = MixedLightingMode.Shadowmask;
#pragma warning disable 0618
            Lightmapping.bakedGI = true;
#pragma warning restore 0618
            LightmapSettings.lightmapsMode = LightmapsMode.CombinedDirectional;
            LightmapSettings.lightmaps = new[]
            {
                new LightmapData { lightmapColor = new Texture2D(1024, 1024, TextureFormat.DXT5, true), lightmapDir = new Texture2D(1024, 1024, TextureFormat.DXT5, true) },
                new LightmapData { lightmapColor = new Texture2D(512, 512, TextureFormat.DXT5, true) }
            };

            var floor = Box("Floor_Big", new Vector3(0f, -0.1f, 0f), new Vector3(20f, 0.2f, 20f), AuditTestAssets.Plain, true);
            Lightmapped(floor, 0, Mathf.Sqrt(0.3f), Vector2.zero);
            var cup = Box("Cup_01", new Vector3(2f, 0.15f, 2f), Vector3.one * 0.3f, AuditTestAssets.Plain, false);
            Lightmapped(cup, 1, 0.06f, new Vector2(0.5f, 0.5f));
            var wall = Box("Wall_Long_03", new Vector3(0f, 2f, 9f), new Vector3(18f, 4f, 0.3f), AuditTestAssets.Plain, false);
            Lightmapped(wall, 1, 0.3f, new Vector2(0f, 0.5f));
            var lamp = Box("Lamp_Post_07", new Vector3(-3f, 0.5f, -3f), Vector3.one, AuditTestAssets.Plain, false);
            Lightmapped(lamp, -1, 1f, Vector2.zero);

            var props = new GameObject("Props");
            for (int i = 0; i < 60; i++)
            {
                var prop = Box("Prop_" + i.ToString("00"), new Vector3(-7.5f + (i % 10) * 1.6f, 0.25f, -7.5f + (i / 10) * 2.5f), Vector3.one * 0.5f, AuditTestAssets.Plain, false);
                prop.transform.SetParent(props.transform, true);
            }
            Box("Barrel_Outside", new Vector3(16f, 0.5f, 0f), Vector3.one, AuditTestAssets.Plain, false);

            var group = new GameObject("Probes_Room").AddComponent<LightProbeGroup>();
            var probes = new List<Vector3>();
            foreach (var x in new[] { -10f, 0f, 10f })
                foreach (var y in new[] { 0f, 4f })
                    foreach (var z in new[] { -10f, 0f, 10f })
                        probes.Add(new Vector3(x, y, z));
            group.probePositions = probes.ToArray();

            var sun = AddLight("Sun", LightType.Directional, LightmapBakeType.Mixed, new Vector3(0f, 10f, 0f), 10f, LightShadows.Soft);
            sun.bakingOutput = new LightBakingOutput { isBaked = true, lightmapBakeType = LightmapBakeType.Mixed, mixedLightingMode = MixedLightingMode.Shadowmask };
            AddLight("Street_Point", LightType.Point, LightmapBakeType.Realtime, new Vector3(0f, 3f, 0f), 30f, LightShadows.None);
            var spot = AddLight("Market_Spot", LightType.Spot, LightmapBakeType.Realtime, new Vector3(6f, 3f, 6f), 4f, LightShadows.Hard);
            spot.renderMode = LightRenderMode.ForcePixel;
            AddLight("Torch_Baked", LightType.Point, LightmapBakeType.Baked, new Vector3(-6f, 2f, -6f), 5f, LightShadows.None);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return path;
        }

        static void LightReportChecks()
        {
            var r = LightReport.Build();
            Check(r.atlases.Count == 2 && r.atlases[0].width == 1024 && r.atlases[0].format == "DXT5" && r.atlases[0].hasDirection && !r.atlases[1].hasDirection,
                "atlases: 1024 DXT5 with direction and 512 without");
            double expected = TextureMemory.EstimateBytes(LightmapSettings.lightmaps[0].lightmapColor, false) * 2 + TextureMemory.EstimateBytes(LightmapSettings.lightmaps[1].lightmapColor, false);
            Check(Math.Abs(r.Bytes - expected) < 1 && r.DirectionMB > 0.5, "memory: color + direction like textures (" + Math.Round(r.MB, 2) + " MB)");
            Check(r.directional && !r.compressed && r.bakedGI && r.mixedMode == MixedLightingMode.Shadowmask, "settings: directional, not compressed, Shadowmask");
            Check(r.objects.Count == 4 && r.atlases[0].objects == 1 && r.atlases[1].objects == 2, "4 objects in lightmaps: 1 in #0, 2 in #1, 1 without");
            var floor = r.objects.First(o => o.renderer.name == "Floor_Big");
            var cup = r.objects.First(o => o.renderer.name == "Cup_01");
            var lamp = r.objects.First(o => o.renderer.name == "Lamp_Post_07");
            Check(Math.Abs(floor.share - 0.3f) < 0.001f && floor.much && !floor.small && Math.Abs(r.atlases[0].filled - 0.3f) < 0.001f, "the floor takes 30 % of atlas #0: much of the atlas");
            Check(cup.small && !cup.much && Math.Abs(cup.size - 0.3f) < 0.01f, "the 0.3 m cup is small");
            Check(lamp.noLightmap && lamp.atlas == -1 && r.NoLightmap == 1, "the lamp has no lightmap");
            Check(Math.Abs(r.atlases[1].filled - (0.06f * 0.06f + 0.09f)) < 0.001f, "atlas #1 is filled by the sum of its shares");

            var point = r.sources.First(s => s.light.name == "Street_Point");
            var spot = r.sources.First(s => s.light.name == "Market_Spot");
            var torch = r.sources.First(s => s.light.name == "Torch_Baked");
            var sun = r.sources.First(s => s.light.name == "Sun");
            Check(point.wide && point.inRange >= 60 && !point.important, "the realtime point light reaches " + point.inRange + " objects: wide");
            Check(spot.important && !spot.wide && spot.inRange < 50, "the spot light with Render Mode Important is marked, not wide (" + spot.inRange + ")");
            Check(!torch.baked && sun.baked && r.NotBakedLights == 1, "the baked torch is not baked, the sun is");
            Check(sun.inRange == r.objects.Count + r.probeObjects, "a directional light reaches every renderer");
            Check(r.sources.First().Marked, "marked lights come first");

            Check(r.groups.Count == 1 && r.probes == 18 && Math.Abs(r.groups[0].size.x - 20f) < 0.01f, "one group of 18 probes over 20 m");
            Check(r.probeObjects == 61, "61 objects on probes: 60 props and the barrel (" + r.probeObjects + ")");
            Check(r.outside.Count == 1 && r.outside[0].renderer.name == "Barrel_Outside" && Math.Abs(r.outside[0].distance - 6.02f) < 0.05f,
                "the barrel is outside the probes, 6 m from the nearest one (" + (r.outside.Count > 0 ? r.outside[0].distance.ToString("F2") : "—") + ")");
        }

        static void LightAuditChecks()
        {
            var budgets = new VRBudgets { lightmapMemoryWarningMB = 1, lightmapMemoryCriticalMB = 100 };
            var result = SceneAuditor.RunFullAudit(budgets, false);
            Func<string, List<AuditIssue>> of = id => result.issues.Where(i => i.checkId == id).ToList();
            log.AppendLine("info: lighting issues " + string.Join(", ", LightChecks.Select(id => id + "=" + of(id).Count).ToArray()));
            Check(of("LIGHT_LIGHTMAP_MEMORY").Count == 1 && of("LIGHT_LIGHTMAP_MEMORY")[0].severity == Severity.Warning, "LIGHT_LIGHTMAP_MEMORY: over the 1 MB warning");
            Check(of("LIGHT_LIGHTMAP_UNCOMPRESSED").Count == 1 && of("LIGHT_LIGHTMAP_UNCOMPRESSED")[0].settingsPath == SceneAuditor.LightingWindow,
                "LIGHT_LIGHTMAP_UNCOMPRESSED: a setting of the Lighting window");
            Check(of("LIGHT_LIGHTMAP_DIRECTIONAL").Count == 1 && of("LIGHT_LIGHTMAP_DIRECTIONAL")[0].severity == Severity.Info, "LIGHT_LIGHTMAP_DIRECTIONAL: Info");
            var notBaked = of("LIGHT_NOT_BAKED");
            Check(notBaked.Count == 1 && (int)notBaked[0].args[0] == 1 && (int)notBaked[0].args[1] == 1, "LIGHT_NOT_BAKED: 1 object and 1 light");
            var small = of("LIGHT_SMALL_IN_LIGHTMAP");
            Check(small.Count == 1 && small[0].objectPath == "Cup_01" && small[0].fixKind == FixKind.Review && small[0].AutoFixable, "LIGHT_SMALL_IN_LIGHTMAP: the cup, with a fix");
            Check(of("LIGHT_LIGHTMAP_SHARE").Count == 1 && of("LIGHT_LIGHTMAP_SHARE")[0].objectPath == "Floor_Big", "LIGHT_LIGHTMAP_SHARE: the floor");
            Check(of("LIGHT_NO_PROBES").Count == 0, "LIGHT_NO_PROBES: silent with a probe group");
            Check(of("LIGHT_OUTSIDE_PROBES").Count == 1 && of("LIGHT_OUTSIDE_PROBES")[0].objectPath == "Barrel_Outside", "LIGHT_OUTSIDE_PROBES: the barrel");
            Check(of("LIGHT_DISTANCE_SHADOWMASK").Count == 1, "LIGHT_DISTANCE_SHADOWMASK: Distance Shadowmask with a Mixed sun");
            Check(of("LIGHT_SHADOW_CASCADES").Count == 1, "LIGHT_SHADOW_CASCADES: 4 cascades with a realtime sun shadow");
            Check(of("LIGHT_WIDE_REALTIME").Count == 1 && of("LIGHT_WIDE_REALTIME")[0].objectPath == "Street_Point", "LIGHT_WIDE_REALTIME: the street point light");
            Check(of("LIGHT_IMPORTANT").Count == 1 && of("LIGHT_IMPORTANT")[0].objectPath == "Market_Spot", "LIGHT_IMPORTANT: the market spot");
            Check(result.lighting != null && result.lighting.atlases.Count == 2, "the audit keeps the lighting report");

            var critical = SceneAuditor.RunFullAudit(new VRBudgets { lightmapMemoryWarningMB = 1, lightmapMemoryCriticalMB = 2 }, false)
                .issues.Where(i => i.checkId == "LIGHT_LIGHTMAP_MEMORY").ToList();
            Check(critical.Count == 1 && critical[0].severity == Severity.Critical && (int)critical[0].args[1] == 2, "LIGHT_LIGHTMAP_MEMORY: Critical over the 2 MB critical limit");

            var report = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(result, null));
            var l = report.lighting;
            Check(report.hasLighting && l.lightmaps == 2 && l.objectsInLightmaps == 4 && l.withoutLightmap == 1 && l.probes == 18 && l.probeGroups == 1
                  && l.outsideProbes == 1 && l.directional && !l.compressed && l.lightmapLimitMB == 1 && Math.Abs(l.lightmapMB - Math.Round(result.lighting.MB, 1)) < 0.001,
                "JSON: lighting with atlases, objects, probes and the limit");
            var point = AuditHistory.PointOf(result, null, null, "", DateTime.Now);
            Check(Math.Abs(point.lightmapMB - Math.Round(result.lighting.MB, 1)) < 0.001 && point.lightmapLimitMB == 1, "history: the lightmap memory");
            var old = JsonUtility.FromJson<HistoryPoint>("{\"time\":\"2026-10-01T10:00:00\"}");
            Check(old.lightmapMB < 0, "history: a point from before 1.4.0 has no lightmap memory");

            UnityEngine.Object.DestroyImmediate(GameObject.Find("Probes_Room"));
            var noProbes = SceneAuditor.RunFullAudit(budgets, false);
            var none = noProbes.issues.Where(i => i.checkId == "LIGHT_NO_PROBES").ToList();
            Check(none.Count == 1 && (int)none[0].args[0] == 61 && noProbes.issues.All(i => i.checkId != "LIGHT_OUTSIDE_PROBES"), "without the probe group: LIGHT_NO_PROBES for 61 objects");
        }

        static void LightFix()
        {
            var r = LightReport.Build();
            var cup = r.objects.First(o => o.renderer.name == "Cup_01");
            var fixes = LightReport.ToProbesFixes(new[] { cup });
            Check(fixes.Count == 1 && !fixes[0].fixTouchesAsset && fixes[0].FixDescription.Contains("Light Probes"), "\"To Probes\": one scene fix");
            Undo.IncrementCurrentGroup();
            int applied = AutoFixer.Apply(fixes);
            Check(applied == 1 && cup.renderer.receiveGI == ReceiveGI.LightProbes, "the cup gets its light from probes");
            Check(LightReport.ToProbesFixes(new[] { cup }).Count == 0, "a cup already on probes has nothing to fix");
            var after = LightReport.Build();
            Check(after.objects.All(o => o.renderer.name != "Cup_01") && after.probeObjects == 62, "the cup leaves the lightmaps and joins the objects on probes");
            Undo.PerformUndo();
            Check(cup.renderer.receiveGI == ReceiveGI.Lightmaps, "Undo brings the lightmap back");
        }

        // Lightmapped objects but no lightmaps: everything is "no lightmap"; without Baked GI nothing is expected to be baked.
        static void NothingBakedScene()
        {
            NewScene("Light-NothingBaked");
            LightmapSettings.lightmaps = new LightmapData[0];
#pragma warning disable 0618
            Lightmapping.bakedGI = true;
#pragma warning restore 0618
            Lightmapped(Box("Hall_Floor", new Vector3(0f, -0.1f, 0f), new Vector3(10f, 0.2f, 10f), AuditTestAssets.Plain, true), -1, 1f, Vector2.zero);
            Lightmapped(Box("Hall_Wall", new Vector3(0f, 2f, 5f), new Vector3(10f, 4f, 0.2f), AuditTestAssets.Plain, false), -1, 1f, Vector2.zero);
            AddLight("Hall_Lamp", LightType.Point, LightmapBakeType.Baked, new Vector3(0f, 3f, 0f), 6f, LightShadows.None);
            var r = LightReport.Build();
            Check(r.atlases.Count == 0 && r.objects.Count == 2 && r.objects.All(o => o.noLightmap) && r.NotBakedLights == 1,
                "nothing baked: no atlases, both objects without a lightmap, the lamp not baked");
            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var notBaked = result.issues.Where(i => i.checkId == "LIGHT_NOT_BAKED").ToList();
            Check(notBaked.Count == 1 && (int)notBaked[0].args[0] == 2 && (int)notBaked[0].args[1] == 1, "nothing baked: LIGHT_NOT_BAKED for 2 objects and 1 light");
            Check(result.issues.All(i => i.checkId != "LIGHT_LIGHTMAP_MEMORY" && i.checkId != "LIGHT_LIGHTMAP_DIRECTIONAL" && i.checkId != "LIGHT_LIGHTMAP_UNCOMPRESSED"),
                "nothing baked: no lightmap memory, direction or compression checks");
#pragma warning disable 0618
            Lightmapping.bakedGI = false;
            try
            {
                var off = LightReport.Build();
                Check(off.objects.Count == 0 && off.NotBakedLights == 0 && !off.bakedGI, "Baked GI off: no objects in lightmaps, nothing counted as not baked");
                Check(SceneAuditor.RunFullAudit(new VRBudgets(), false).issues.All(i => i.checkId != "LIGHT_NOT_BAKED"), "Baked GI off: no LIGHT_NOT_BAKED");
            }
            finally
            {
                Lightmapping.bakedGI = true;
            }
#pragma warning restore 0618
        }

        static void QuietLightScene()
        {
            NewScene("Light-Quiet");
            LightmapSettings.lightmaps = new LightmapData[0];
            Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(10f, 0.2f, 10f), AuditTestAssets.Plain, true);
            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var found = result.issues.Where(i => LightChecks.Contains(i.checkId)).Select(i => i.checkId).Distinct().ToList();
            Check(found.Count == 0, "a scene without lightmaps, probes and lights: no lighting checks (" + string.Join(", ", found.ToArray()) + ")");
            var r = result.lighting;
            Check(r != null && r.atlases.Count == 0 && r.objects.Count == 0 && r.sources.Count == 0 && r.groups.Count == 0, "the lighting report is empty");
        }
    }
}
