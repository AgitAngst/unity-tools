using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // The frame budget (2.8.0, owner 28.10.2026: "so it shows everything in the scene, and the limit of 1000 is per
    // frame?"): triangles and draw calls are judged at the worst viewpoint, the whole scene is only for reference, the
    // viewpoint file is not changed by a run, and without viewpoints an Info row says the frame was not estimated.
    public static partial class AuditTestViewpoints
    {
        public static void RunFrameBudget()
        {
            Run("frame-budget", FrameBudgetChecks);
        }

        static void FrameBudgetChecks()
        {
            var path = NewScene("FrameBudget");
            // A ring of 48 cubes 20 m around the middle: a viewpoint in the middle sees a part of them in any direction.
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48f;
                MeshObject("Cube_" + i, Resources.GetBuiltinResource<Mesh>("Cube.fbx"), new Vector3(Mathf.Sin(a) * 20f, 0f, Mathf.Cos(a) * 20f), AuditTestAssets.Plain);
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            var low = new VRBudgets { drawCallsWarning = 5, drawCallsCritical = 1000, sceneTrisWarning = 10, sceneTrisCritical = 100000 };

            var none = SceneAuditor.RunFullAudit(low, false);
            var info = none.issues.FirstOrDefault(i => i.checkId == "FRAME_NO_VIEWPOINTS");
            Check(none.frame == null && info != null && info.severity == Severity.Info, "no viewpoints: the frame is not estimated, an Info row says so");
            Check(info != null && info.Message.Contains(none.drawCallEstimate.ToString()), "the Info row gives the whole scene: " + (info != null ? info.Message : ""));
            Check(!none.issues.Any(i => i.checkId == "DC_BUDGET" || i.checkId == "MESH_SCENE_TRIS"),
                "the whole scene (" + none.drawCallEstimate + " draw calls) is no budget problem, however far over the threshold");

            var set = ViewpointSet.Load(new[] { path });
            set.points.Clear();
            set.AddManual(new Vector3(0f, 1.7f, 0f), 0f).hasYaw = false;
            set.Save();
            Object.DestroyImmediate(set);
            var file = ViewpointSet.FileFor(new[] { path });
            var before = File.ReadAllText(file);

            var r = SceneAuditor.RunFullAudit(low, false);
            Check(r.frame != null && r.frame.points == 1 && r.frame.drawCallsPoint.Length > 0, "a viewpoint: the run estimates it");
            Check(r.frame != null && r.frame.drawCalls > 0 && r.frame.drawCalls < r.drawCallEstimate,
                "a frame sees part of the ring: " + (r.frame != null ? r.frame.drawCalls : -1) + " of " + r.drawCallEstimate + " draw calls");
            Check(File.ReadAllText(file) == before, "the viewpoint file is not changed by the run");
            var dc = r.issues.FirstOrDefault(i => i.checkId == "DC_BUDGET");
            Check(dc != null && dc.severity == Severity.Warning && dc.Message.Contains(r.frame.drawCallsPoint) && !r.issues.Any(i => i.checkId == "FRAME_NO_VIEWPOINTS"),
                "draw calls per frame over the threshold: a Warning names the viewpoint (" + (dc != null ? dc.Message : "none") + ")");

            // Whole scene over the threshold, every frame within: no problem.
            var frameOnly = new VRBudgets { drawCallsWarning = r.frame.drawCalls + 1, drawCallsCritical = r.frame.drawCalls * 2 + 2 };
            var within = SceneAuditor.RunFullAudit(frameOnly, false);
            Check(r.drawCallEstimate > frameOnly.drawCallsWarning && !within.issues.Any(i => i.checkId == "DC_BUDGET"),
                "the whole scene over the threshold, the worst frame within it: no draw call budget problem");

            var json = Path.Combine(ReportDir, "frame-budget.json");
            ReportExporter.Save(r, json);
            var text = File.ReadAllText(json);
            Check(text.Contains("\"hasFrame\": true") && text.Contains("\"drawCallsViewpoint\": \"" + r.frame.drawCallsPoint + "\""), "the JSON report has the frame");
            DeleteViewpointFile(path);
        }
    }
}
