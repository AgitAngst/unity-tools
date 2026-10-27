using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        static void AuditPhysics(AuditContext ctx)
        {
            foreach (var collider in ctx.All<MeshCollider>())
            {
                if (!AuditContext.IsActive(collider)) continue;
                if (!collider.convex)
                {
                    var body = AuditContext.FindInParents<Rigidbody>(collider.transform);
                    if (body != null && !body.isKinematic)
                        ctx.Report("PHYS_CONCAVE_DYNAMIC", Category.Physics, Severity.Critical, collider);
                    else if (body != null)
                        ctx.Report("PHYS_CONCAVE_KINEMATIC", Category.Physics, Severity.Info, collider);
                }

                var mesh = collider.sharedMesh;
                var filter = collider.GetComponent<MeshFilter>();
                if (mesh != null && filter != null && filter.sharedMesh == mesh)
                {
                    long tris = AuditContext.Tris(mesh);
                    if (tris > ctx.budgets.colliderTrisInfo)
                        ctx.Report("PHYS_RENDER_MESH_COLLIDER", Category.Physics, Severity.Info, collider, tris, ctx.budgets.colliderTrisInfo);
                }
            }

            if (Physics.defaultSolverIterations > VRBudgets.SolverIterationsInfo)
                ctx.ReportSetting("PHYS_SOLVER_ITERATIONS", Category.Physics, Severity.Info, "Project/Physics",
                    Physics.defaultSolverIterations, VRBudgets.SolverIterationsInfo);

            // In VR the physics step should match the refresh rate of the headset, otherwise held objects jitter.
            int rate = ctx.budgets.refreshRate;
            float step = Time.fixedDeltaTime;
            float target = 1f / rate;
            float hz = step > 0f ? 1f / step : 0f;
            if (step > target + VRBudgets.FixedStepTolerance)
                ctx.ReportSetting("PHYS_FIXED_STEP_SLOW", Category.Physics, Severity.Warning, "Project/Time",
                    System.Math.Round(step, 4), System.Math.Round(hz, 1), rate);
            else if (step < target - VRBudgets.FixedStepTolerance)
                ctx.ReportSetting("PHYS_FIXED_STEP_FAST", Category.Physics, Severity.Info, "Project/Time",
                    System.Math.Round(step, 4), System.Math.Round(hz, 1), rate);
        }
    }
}
