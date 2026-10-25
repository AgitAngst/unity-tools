using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityTools.SceneOptimization;
using Object = UnityEngine.Object;

namespace AuditTestsEditor
{
    // Gains of fixes on assets and objects whose numbers are known: memory, draw calls, triangles, the order by gain.
    public static partial class AuditTestViewpoints
    {
        const string GainClip = AuditTestAssets.Root + "/Audio/Gain_Long.wav";

        public static void RunGain()
        {
            Run("gain", Gain);
        }

        static bool Near(double a, double b)
        {
            return Math.Abs(a - b) < 0.01;
        }

        static AuditIssue IssueOn(AuditResult r, string checkId, string name)
        {
            return r.issues.FirstOrDefault(i => i.checkId == checkId && OnObject(i, name));
        }

        static AuditIssue IssueAt(AuditResult r, string checkId, Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return r.issues.FirstOrDefault(i => i.checkId == checkId && i.assetPath == path);
        }

        // Also used by the screenshot of the Problems tab with gains.
        public static void BuildGainScene()
        {
            NewScene("Gain");
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            MeshObject("Wall_4096", cube, new Vector3(0f, 0f, 0f), AuditTestAssets.Big4096);
            MeshObject("Raw_Plate", cube, new Vector3(2f, 0f, 0f), AuditTestAssets.Raw);
            MeshObject("Readable_Plate", cube, new Vector3(4f, 0f, 0f), AuditTestAssets.Readable);
            for (int i = 0; i < 5; i++) MeshObject("Rocks_" + i, cube, new Vector3(i * 2f, 0f, 6f), AuditTestAssets.NoInst);
            var multi = MeshObject("Multi", AuditTestAssets.TwoSub, new Vector3(0f, 0f, 10f), AuditTestAssets.Plain);
            multi.GetComponent<MeshRenderer>().sharedMaterials = new[] { AuditTestAssets.Plain, AuditTestAssets.Plain, AuditTestAssets.Plain };
            MeshObject("Heavy", AuditTestAssets.Grid30k, new Vector3(0f, 0f, 14f), AuditTestAssets.Plain);
            Animated("Npc");

            var probe = new GameObject("Probe_Big").AddComponent<ReflectionProbe>();
            probe.transform.position = new Vector3(0f, 0f, -40f);
            probe.resolution = 1024;
            probe.size = new Vector3(4f, 4f, 4f);
            probe.boxProjection = true;

            var lamp = new GameObject("Lamp").AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.lightmapBakeType = LightmapBakeType.Realtime;
            lamp.range = 5f;
            lamp.shadows = LightShadows.None;
            lamp.transform.position = new Vector3(100f, 0f, 0f);
            for (int i = 0; i < 3; i++) MeshObject("Lit_" + i, cube, new Vector3(100f + i, 0f, 0f), AuditTestAssets.Plain);

            var outline = WriteShader("Outline", ShaderText("Outline", "", StereoPass("") + StereoPass("", "Cull Front")));
            var outlined = new Material(outline);
            for (int i = 0; i < 2; i++) MeshObject("Outlined_" + i, cube, new Vector3(i * 2f, 0f, 20f), outlined);

            Directory.CreateDirectory(Path.GetDirectoryName(GainClip));
            if (!File.Exists(GainClip))
            {
                WriteWav(GainClip, 12);
                AssetDatabase.ImportAsset(GainClip, ImportAssetOptions.ForceSynchronousImport);
            }
            var source = new GameObject("Music").AddComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GainClip);
            source.spatialBlend = 1f;
        }

        static void Gain()
        {
            BuildGainScene();
            var r = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var b = r.budgets;

            var big = AuditTestAssets.Big4096.mainTexture;
            var size = IssueAt(r, "TEX_SIZE", big);
            Check(size != null && Near(size.gainMB, FixGain.TextureMB(big) * 0.75) && !size.gainApprox && size.gainMB > 1,
                "a 4096 texture down to 2048: three quarters of its memory (" + (size != null ? size.gainMB.ToString("F2") : "—") + " MB)");

            var raw = AuditTestAssets.Raw.mainTexture;
            var rawIssue = IssueAt(r, "TEX_UNCOMPRESSED", raw);
            double bpp = TextureMemory.BitsPerPixel(TextureMemory.FormatOf(raw));
            Check(rawIssue != null && bpp >= 24 && Near(rawIssue.gainMB, FixGain.TextureMB(raw) * (1 - 4 / bpp)),
                "an uncompressed texture to DXT1: 4 bits a pixel instead of " + bpp);

            var readable = AuditTestAssets.Readable.mainTexture;
            var rw = IssueAt(r, "TEX_READ_WRITE", readable);
            Check(rw != null && Near(rw.gainMB, FixGain.TextureMB(readable)), "Read/Write: the copy in CPU memory");

            var instancing = IssueAt(r, "DC_INSTANCING_OFF", AuditTestAssets.NoInst);
            Check(instancing != null && instancing.gainDrawCalls == 4 && !instancing.gainApprox, "5 rocks without instancing: 4 draw calls");
            var multi = IssueOn(r, "DC_MULTI_MATERIAL", "Multi");
            var extra = IssueOn(r, "DC_EXTRA_MATERIALS", "Multi");
            Check(multi != null && multi.gainDrawCalls == 2 && multi.gainApprox && extra != null && extra.gainDrawCalls == 1,
                "3 materials on 2 submeshes: ≈ 2 draw calls for an atlas, 1 for the extra material");

            var heavy = IssueOn(r, "MESH_NO_LOD", "Heavy");
            long tris = AuditContext.Tris(AuditTestAssets.Grid30k);
            Check(heavy != null && heavy.gainTris == tris / 2 && heavy.gainApprox, "a heavy mesh without LOD: ≈ half of its triangles");

            var probe = r.issues.FirstOrDefault(i => i.checkId == "REFL_RESOLUTION");
            var row = r.lighting.reflections.FirstOrDefault(p => p.probe != null && p.probe.name == "Probe_Big");
            Check(probe != null && row != null && Near(probe.gainMB, row.MB * (1 - 1.0 / 16)) && probe.gainApprox,
                "a probe of 1024 down to 256: 15/16 of its memory, estimated as it is not baked");

            var lamp = IssueOn(r, "LIGHT_REALTIME", "Lamp");
            Check(lamp != null && lamp.gainDrawCalls == 3 && lamp.gainApprox, "a realtime point light over 3 objects: ≈ 3 draw calls (" + (lamp != null ? lamp.gainDrawCalls : -1) + ")");

            var passes = r.issues.FirstOrDefault(i => i.checkId == "SHADER_MULTIPASS");
            Check(passes != null && passes.gainDrawCalls == 2, "an outline pass on 2 objects: 2 draw calls");

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GainClip);
            var audio = IssueAt(r, "AUDIO_DECOMPRESS_LONG", clip);
            Check(audio != null && Near(audio.gainMB, FixGain.PcmMB(clip) * 0.85) && audio.gainApprox, "a long clip decompressed on load: ≈ 85 % of its PCM");

            var npc = IssueOn(r, "ANIM_ALWAYS_ANIMATE", "Npc");
            Check(npc != null && !npc.HasGain, "an animator has no measurable gain");

            // The order of "By gain": the share of the budgets.
            var groups = IssueGroup.Build(r.issues);
            var ordered = groups.OrderByDescending(g => FixGain.GroupShare(g.issues, b)).ToList();
            var first = ordered[0];
            double best = FixGain.GroupShare(first.issues, b);
            Check(best > 0 && groups.All(g => FixGain.GroupShare(g.issues, b) <= best + 1e-9) &&
                  ordered.SkipWhile(g => FixGain.GroupShare(g.issues, b) > 0).All(g => FixGain.GroupShare(g.issues, b) == 0),
                "by gain: the largest share first, groups without a gain last (" + first.checkId + ")");
            var heavyGroup = groups.First(g => g.checkId == "MESH_NO_LOD");
            Check(Near(FixGain.GroupShare(heavyGroup.issues, b), (double)heavy.gainTris / b.sceneTrisWarning), "the share of triangles is of the triangle threshold");

            // Accepted rows leave the total.
            var total = FixGain.Sum(r.issues);
            // Rows of one object count once: Multi has 2 (atlas) and 1 (extra material) — 2; then 4 rocks, 3 lit, 2 outlined.
            Check(total.Any && total.drawCalls == 11 && total.tris == tris / 2 && total.approx &&
                  total.mb <= r.issues.Where(i => !i.accepted && i.HasGain).Sum(i => i.gainMB) + 0.001,
                "the total counts every object once (" + total.mb.ToString("F2") + " MB, " + total.drawCalls + " DC, " + total.tris + " tris)");
            var tooLarge = new AuditIssue { checkId = "TEX_SIZE", assetPath = "Assets/X.png" }.Gain(64, 0, 0, false);
            var uncompressed = new AuditIssue { checkId = "TEX_UNCOMPRESSED", assetPath = "Assets/X.png" }.Gain(75, 0, 0, false);
            var another = new AuditIssue { checkId = "TEX_SIZE", assetPath = "Assets/Y.png" }.Gain(10, 0, 0, false);
            Check(Near(FixGain.Sum(new[] { tooLarge, uncompressed, another }).mb, 85), "two rows of one texture count with the larger one: 75 + 10");
            size.accepted = true;
            Check(Near(FixGain.Sum(r.issues).mb, total.mb - size.gainMB), "an acceptable row is left out of the total");
            size.accepted = false;

            var report = JsonUtility.FromJson<ReportExporter.ReportDto>(ReportExporter.ToJson(r, null));
            var dto = report.issues.FirstOrDefault(i => i.checkId == "TEX_SIZE" && i.assetPath == size.assetPath);
            Check(dto != null && Near(dto.gainMB, Math.Round(size.gainMB, 1)) && report.issues.Any(i => i.checkId == "MESH_NO_LOD" && i.gainTris == tris / 2 && i.gainApprox) &&
                  Near(report.summary.gainMB, Math.Round(total.mb, 1)) && report.summary.gainDrawCalls == total.drawCalls,
                "JSON: the gains of a row and the totals");
            var csv = ReportExporter.ToCsv(r).Split('\n');
            Check(csv[0].TrimEnd().EndsWith(",GainMB,GainDrawCalls,GainTris") && csv.Any(l => l.StartsWith("MESH_NO_LOD") && l.TrimEnd().EndsWith(",0,0," + tris / 2)),
                "CSV: three gain columns at the end");
        }
    }
}
