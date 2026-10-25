using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityTools.SceneOptimization
{
    // What fixes would free: the sums of a list of issues.
    public sealed class GainTotal
    {
        public double mb;
        public long drawCalls;
        public long tris;
        public bool approx;

        public bool Any { get { return mb >= 0.05 || drawCalls > 0 || tris > 0; } }

        public void Add(AuditIssue issue)
        {
            if (!issue.HasGain) return;
            mb += issue.gainMB;
            drawCalls += issue.gainDrawCalls;
            tris += issue.gainTris;
            approx |= issue.gainApprox;
        }
    }

    // Estimates of what a fix frees: memory, draw calls, triangles. Rough ones are marked approximate by the checks.
    public static class FixGain
    {
        const double Mega = 1024.0 * 1024.0;

        public static double TextureMB(Texture texture)
        {
            return TextureMemory.EstimateBytes(texture, false) / Mega;
        }

        // Memory freed when the largest side comes down to newSide: the mip chain shrinks with the area.
        public static double ResizedMB(Texture texture, int newSide)
        {
            if (texture == null) return 0;
            int side = Mathf.Max(texture.width, texture.height);
            if (newSide <= 0 || newSide >= side) return 0;
            double k = (double)newSide / side;
            return TextureMB(texture) * (1 - k * k);
        }

        // Memory freed by another format of the same size.
        public static double ReformattedMB(Texture texture, string newFormat)
        {
            if (texture == null) return 0;
            double from = TextureMemory.BitsPerPixel(TextureMemory.FormatOf(texture));
            double to = TextureMemory.BitsPerPixel(newFormat);
            return to >= from ? 0 : TextureMB(texture) * (1 - to / from);
        }

        // Vertex and index buffers of a mesh: what a CPU copy (Read/Write) or a duplicate costs.
        public static double MeshMB(Mesh mesh)
        {
            if (mesh == null) return 0;
            long stride = 0;
            foreach (var attribute in mesh.GetVertexAttributes()) stride += FormatSize(attribute.format) * attribute.dimension;
            double bytes = (double)stride * mesh.vertexCount;
            int indexSize = mesh.indexFormat == IndexFormat.UInt16 ? 2 : 4;
            for (int i = 0; i < mesh.subMeshCount; i++) bytes += (double)mesh.GetIndexCount(i) * indexSize;
            return bytes / Mega;
        }

        static int FormatSize(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32:
                case VertexAttributeFormat.UInt32:
                case VertexAttributeFormat.SInt32:
                    return 4;
                case VertexAttributeFormat.Float16:
                case VertexAttributeFormat.UNorm16:
                case VertexAttributeFormat.SNorm16:
                case VertexAttributeFormat.UInt16:
                case VertexAttributeFormat.SInt16:
                    return 2;
                default:
                    return 1;
            }
        }

        // Bytes of a clip decompressed into memory: 16-bit PCM.
        public static double PcmMB(AudioClip clip)
        {
            return clip == null ? 0 : (double)clip.samples * clip.channels * 2 / Mega;
        }

        // Share of the budgets an issue frees: memory of the texture memory threshold, draw calls and triangles of theirs.
        public static double Share(AuditIssue issue, VRBudgets b)
        {
            if (!issue.HasGain) return 0;
            return issue.gainMB / Mathf.Max(1, b.textureMemoryWarningMB) +
                   (double)issue.gainDrawCalls / Mathf.Max(1, b.drawCallsWarning) +
                   (double)issue.gainTris / Mathf.Max(1, b.sceneTrisWarning);
        }

        // Share of the budgets the fixes of a group free, without the acceptable rows: the order of "By gain".
        public static double GroupShare(IEnumerable<AuditIssue> issues, VRBudgets b)
        {
            double share = 0;
            foreach (var issue in issues)
                if (!issue.accepted) share += Share(issue, b);
            return share;
        }

        // The rows of one asset, object or lighting setting may promise the same memory twice (a texture too large and
        // uncompressed): each of them counts with the largest of its estimates in every unit, so the total never adds up
        // one thing twice and stays below what the things take.
        public static GainTotal Sum(IEnumerable<AuditIssue> issues)
        {
            var best = new Dictionary<string, GainTotal>();
            foreach (var issue in issues)
            {
                if (issue.accepted || !issue.HasGain) continue;
                var key = KeyOf(issue);
                GainTotal one;
                if (!best.TryGetValue(key, out one))
                {
                    one = new GainTotal();
                    best[key] = one;
                }
                one.mb = System.Math.Max(one.mb, issue.gainMB);
                one.drawCalls = System.Math.Max(one.drawCalls, issue.gainDrawCalls);
                one.tris = System.Math.Max(one.tris, issue.gainTris);
                one.approx |= issue.gainApprox;
            }
            var total = new GainTotal();
            foreach (var one in best.Values)
            {
                total.mb += one.mb;
                total.drawCalls += one.drawCalls;
                total.tris += one.tris;
                total.approx |= one.approx;
            }
            return total;
        }

        // What an issue's gain is about: an asset, a scene object, or the lighting settings of a scene.
        static string KeyOf(AuditIssue issue)
        {
            if (!string.IsNullOrEmpty(issue.objectPath)) return "o|" + issue.scenePath + "|" + issue.scene + "|" + issue.objectPath;
            if (!string.IsNullOrEmpty(issue.assetPath)) return "a|" + issue.assetPath;
            return "s|" + issue.settingsPath + "|" + issue.scenePath;
        }
    }
}
