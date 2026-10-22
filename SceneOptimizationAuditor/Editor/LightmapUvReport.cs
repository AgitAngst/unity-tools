using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    // One model file (or mesh asset without a model) used in the loaded scenes.
    public sealed class LightmapUvRow
    {
        public string name;
        public string assetPath;
        public Object asset;
        // null for meshes that do not come from a model file.
        public ModelImporter importer;
        public bool hasUv1 = true;
        public float outsideShare;
        public float overlapShare;
        public int objects;
        public int bakedObjects;
        public readonly List<Mesh> meshes = new List<Mesh>();

        public bool GenerateOn { get { return importer != null && importer.generateSecondaryUV; } }
        public bool CanTurnOn { get { return importer != null && !importer.generateSecondaryUV && !hasUv1; } }

        public bool HasProblem
        {
            get { return !hasUv1 || outsideShare > 0f || overlapShare > VRBudgets.LightmapUvOverlapMinShare; }
        }
    }

    // Every mesh of the loaded scenes, inactive objects included, regardless of lightmap flags.
    public static class LightmapUvReport
    {
        public static List<LightmapUvRow> Build()
        {
            var rows = new Dictionary<string, LightmapUvRow>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                        Add(rows, filter.sharedMesh, filter.GetComponent<MeshRenderer>());
                    foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        Add(rows, skinned.sharedMesh, null);
                }
            }

            foreach (var row in rows.Values)
            {
                foreach (var mesh in row.meshes)
                {
                    if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord1))
                    {
                        row.hasUv1 = false;
                        continue;
                    }
                    var uv = new List<Vector2>();
                    mesh.GetUVs(1, uv);
                    row.outsideShare = Mathf.Max(row.outsideShare, SceneAuditor.OutsideShare(uv));
                    row.overlapShare = Mathf.Max(row.overlapShare, SceneAuditor.OverlapShare(mesh, uv));
                }
            }
            return rows.Values
                .OrderByDescending(r => r.HasProblem)
                .ThenByDescending(r => r.bakedObjects)
                .ThenBy(r => r.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        static void Add(Dictionary<string, LightmapUvRow> rows, Mesh mesh, MeshRenderer renderer)
        {
            if (mesh == null) return;
            var path = AssetDatabase.GetAssetPath(mesh);
            // Built-in primitives and scene-only meshes: keyed by the mesh itself.
            var key = string.IsNullOrEmpty(path) || !AuditContext.IsProjectAssetPath(path) ? "mesh:" + mesh.GetInstanceID() : path;
            LightmapUvRow row;
            if (!rows.TryGetValue(key, out row))
            {
                var importer = AuditContext.IsProjectAssetPath(path) ? AssetImporter.GetAtPath(path) as ModelImporter : null;
                row = new LightmapUvRow
                {
                    name = importer != null ? Path.GetFileName(path) : mesh.name,
                    assetPath = AuditContext.IsProjectAssetPath(path) ? path : "",
                    asset = importer != null ? AssetDatabase.LoadMainAssetAtPath(path) : mesh,
                    importer = importer
                };
                rows[key] = row;
            }
            if (!row.meshes.Contains(mesh)) row.meshes.Add(mesh);
            row.objects++;
            if (renderer != null && renderer.receiveGI == ReceiveGI.Lightmaps &&
                (GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.ContributeGI) != 0)
                row.bakedObjects++;
        }

        // Fix entries for the confirmation window: turn on Generate Lightmap UVs.
        public static List<AuditIssue> TurnOnFixes(IEnumerable<LightmapUvRow> rows)
        {
            var fixes = new List<AuditIssue>();
            foreach (var row in rows)
            {
                if (!row.CanTurnOn) continue;
                var importer = row.importer;
                var issue = new AuditIssue
                {
                    checkId = "LIGHT_NO_UV1",
                    messageKey = "LIGHT_NO_UV1",
                    category = Category.Lighting,
                    severity = Severity.Warning,
                    target = row.asset,
                    assetPath = row.assetPath,
                    users = row.objects,
                    args = new object[] { row.name }
                };
                issue.SetFix(FixKind.Review, importer, true, () =>
                {
                    importer.generateSecondaryUV = true;
                    importer.SaveAndReimport();
                });
                fixes.Add(issue);
            }
            return fixes;
        }

        public static void SaveCsv(IEnumerable<LightmapUvRow> rows, string path)
        {
            var sb = new StringBuilder("Model,AssetPath,GenerateLightmapUVs,HasUV1,OutsidePercent,OverlapPercent,Objects,BakedObjects\r\n");
            foreach (var r in rows)
            {
                sb.Append(ReportExporter.Csv(r.name)).Append(',')
                  .Append(ReportExporter.Csv(r.assetPath)).Append(',')
                  .Append(r.importer == null ? "" : r.GenerateOn ? "true" : "false").Append(',')
                  .Append(r.hasUv1 ? "true" : "false").Append(',')
                  .Append(Math.Round(r.outsideShare * 100.0, 1).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                  .Append(Math.Round(r.overlapShare * 100.0, 1).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                  .Append(r.objects).Append(',')
                  .Append(r.bakedObjects).Append("\r\n");
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            Debug.Log("[SceneAudit] Lightmap UV report saved: " + path);
        }
    }
}
