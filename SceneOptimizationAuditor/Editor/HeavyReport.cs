using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    public enum SourceKind { Prefab, Model, Mesh, Object }

    // Where a renderer comes from: its prefab or model (the asset of the nearest prefab instance root), else its mesh
    // asset, else the object itself. Shared by "What is heavy here" and the Heaviest tab.
    public struct RendererSource
    {
        public string key;
        public string label;
        // A copy of a prefab or model; else a mesh asset or an object.
        public bool prefabCopy;
        public string assetPath;
        public Mesh mesh;
        // The scene object that is one copy: the prefab instance root, else the renderer's object.
        public GameObject copy;
    }

    // One source of the loaded scenes with what its copies cost.
    public sealed class HeavySource
    {
        public string key = "";
        public string label = "";
        public SourceKind kind;
        public Object asset;
        public string assetPath = "";
        public readonly HashSet<GameObject> copies = new HashSet<GameObject>();
        public readonly HashSet<Material> materials = new HashSet<Material>();
        public readonly HashSet<Texture> textures = new HashSet<Texture>();
        // Triangles of each copy, and the copies with a renderer outside any LODGroup.
        public readonly Dictionary<GameObject, long> copyTris = new Dictionary<GameObject, long>();
        public readonly HashSet<GameObject> copiesWithoutLod = new HashSet<GameObject>();
        public long tris;
        public int drawCalls;
        public double textureBytes;
        public bool inLodGroup;

        public int Copies { get { return copies.Count; } }
        public long TrisPerCopy { get { return copies.Count > 0 ? tris / copies.Count : tris; } }
        public double TextureMB { get { return textureBytes / (1024.0 * 1024.0); } }
    }

    public sealed class HeavyReport
    {
        public readonly List<HeavySource> sources = new List<HeavySource>();
        public long tris;
        public int drawCalls;
        // Sum over the sources: a texture shared by two sources counts twice.
        public double textureBytes;
        // Threshold of the "no LOD" mark: triangles of one copy without a LODGroup.
        public int objectTrisWarning;

        // A copy has more triangles than the object threshold and a renderer outside any LODGroup.
        public bool NoLod(HeavySource s)
        {
            foreach (var copy in s.copiesWithoutLod)
            {
                long n;
                if (s.copyTris.TryGetValue(copy, out n) && n > objectTrisWarning) return true;
            }
            return false;
        }

        public static RendererSource Identify(Renderer r, Mesh mesh)
        {
            var root = PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
            if (root != null)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(root);
                var path = source != null ? AssetDatabase.GetAssetPath(source) : "";
                if (!string.IsNullOrEmpty(path))
                    return new RendererSource { key = path, label = Path.GetFileName(path), prefabCopy = true, assetPath = path, copy = root };
            }
            var meshPath = mesh != null ? AssetDatabase.GetAssetPath(mesh) : "";
            if (AuditContext.IsProjectAssetPath(meshPath))
            {
                return new RendererSource
                {
                    key = meshPath + "|" + mesh.name,
                    label = AssetDatabase.IsMainAsset(mesh) ? Path.GetFileName(meshPath) : Path.GetFileName(meshPath) + " · " + mesh.name,
                    assetPath = meshPath, mesh = mesh, copy = r.gameObject
                };
            }
            return new RendererSource { key = "object|" + r.gameObject.name, label = r.gameObject.name, assetPath = "", copy = r.gameObject };
        }

        // Kind and asset of a new source: looked up once per source, not per renderer.
        static void Describe(HeavySource s, RendererSource id)
        {
            if (id.prefabCopy)
            {
                s.asset = AssetDatabase.LoadMainAssetAtPath(id.assetPath);
                s.kind = s.asset != null && PrefabUtility.GetPrefabAssetType(s.asset) == PrefabAssetType.Model ? SourceKind.Model : SourceKind.Prefab;
            }
            else if (id.mesh != null)
            {
                s.asset = id.mesh;
                s.kind = AssetImporter.GetAtPath(id.assetPath) is ModelImporter ? SourceKind.Model : SourceKind.Mesh;
            }
            else s.kind = SourceKind.Object;
            s.assetPath = id.assetPath ?? "";
        }

        public static HeavyReport Build(int objectTrisWarning)
        {
            var roots = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) roots.AddRange(scene.GetRootGameObjects());
            }
            return Build(roots, objectTrisWarning);
        }

        // Active renderers of the scenes grouped by source: triangles of LOD0, a draw call per material, the memory of the
        // distinct textures of its materials.
        public static HeavyReport Build(List<GameObject> roots, int objectTrisWarning)
        {
            var report = new HeavyReport { objectTrisWarning = objectTrisWarning };
            var inLod = new HashSet<Renderer>();
            var lowerOnly = new HashSet<Renderer>();
            foreach (var root in roots)
            {
                foreach (var group in root.GetComponentsInChildren<LODGroup>(true))
                {
                    var lods = group.GetLODs();
                    var first = new HashSet<Renderer>();
                    for (int i = 0; i < lods.Length; i++)
                        foreach (var r in lods[i].renderers)
                        {
                            if (r == null) continue;
                            inLod.Add(r);
                            if (i == 0) first.Add(r);
                            else if (!first.Contains(r)) lowerOnly.Add(r);
                        }
                    lowerOnly.ExceptWith(first);
                }
            }
            var byKey = new Dictionary<string, HeavySource>();
            foreach (var root in roots)
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || !AuditContext.IsActive(r)) continue;
                    var mesh = AuditContext.MeshOf(r);
                    // Renderers only in LOD1 and lower are not drawn up close: no copy, no triangles, no draw calls.
                    if (mesh == null || lowerOnly.Contains(r)) continue;
                    var id = Identify(r, mesh);
                    HeavySource s;
                    if (!byKey.TryGetValue(id.key, out s))
                    {
                        s = new HeavySource { key = id.key, label = id.label };
                        Describe(s, id);
                        byKey[id.key] = s;
                        report.sources.Add(s);
                    }
                    s.copies.Add(id.copy);
                    if (inLod.Contains(r)) s.inLodGroup = true;
                    else s.copiesWithoutLod.Add(id.copy);
                    long tris = AuditContext.Tris(mesh);
                    s.tris += tris;
                    long copyTris;
                    s.copyTris.TryGetValue(id.copy, out copyTris);
                    s.copyTris[id.copy] = copyTris + tris;
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) continue;
                        s.drawCalls++;
                        s.materials.Add(m);
                    }
                }
            }
            foreach (var s in report.sources)
            {
                foreach (var m in s.materials)
                    foreach (var name in m.GetTexturePropertyNames())
                    {
                        var t = m.GetTexture(name);
                        if (t != null) s.textures.Add(t);
                    }
                foreach (var t in s.textures) s.textureBytes += TextureMemory.EstimateBytes(t, t.isReadable);
                // Shared textures count for every source, so the shares add up to 100 %.
                report.textureBytes += s.textureBytes;
                report.tris += s.tris;
                report.drawCalls += s.drawCalls;
            }
            report.sources.Sort((a, b) => b.tris != a.tris ? b.tris.CompareTo(a.tris) : string.CompareOrdinal(a.label, b.label));
            return report;
        }

        // Share of the scene by a measure: 0 triangles, 1 draw calls, 2 texture memory.
        public double Share(HeavySource s, int measure)
        {
            switch (measure)
            {
                case 1: return drawCalls > 0 ? (double)s.drawCalls / drawCalls : 0;
                case 2: return textureBytes > 0 ? s.textureBytes / textureBytes : 0;
                default: return tris > 0 ? (double)s.tris / tris : 0;
            }
        }

        public List<HeavySource> Sorted(int measure)
        {
            switch (measure)
            {
                case 1: return sources.OrderByDescending(s => s.drawCalls).ThenByDescending(s => s.tris).ThenBy(s => s.label, StringComparer.Ordinal).ToList();
                case 2: return sources.OrderByDescending(s => s.textureBytes).ThenByDescending(s => s.tris).ThenBy(s => s.label, StringComparer.Ordinal).ToList();
                default: return sources.OrderByDescending(s => s.tris).ThenByDescending(s => s.drawCalls).ThenBy(s => s.label, StringComparer.Ordinal).ToList();
            }
        }

        // Share of the first n sources by the measure.
        public double TopShare(int measure, int n)
        {
            return Sorted(measure).Take(n).Sum(s => Share(s, measure));
        }

        public void SaveCsv(string path)
        {
            var sb = new StringBuilder("Source,Kind,AssetPath,Copies,TrisPerCopy,TrisTotal,DrawCalls,Materials,TextureMB,NoLod\r\n");
            foreach (var s in sources)
            {
                sb.Append(ReportExporter.Csv(s.label)).Append(',')
                  .Append(s.kind).Append(',')
                  .Append(ReportExporter.Csv(s.assetPath)).Append(',')
                  .Append(s.Copies).Append(',')
                  .Append(s.TrisPerCopy).Append(',')
                  .Append(s.tris).Append(',')
                  .Append(s.drawCalls).Append(',')
                  .Append(s.materials.Count).Append(',')
                  .Append(Math.Round(s.TextureMB, 1).ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(NoLod(s) ? "true" : "false").Append("\r\n");
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            Debug.Log("[SceneAudit] Heaviest sources saved: " + path);
        }
    }
}
