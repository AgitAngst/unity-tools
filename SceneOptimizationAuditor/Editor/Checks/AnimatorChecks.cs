using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        static void AuditAnimators(AuditContext ctx)
        {
            var b = ctx.budgets;
            int active = 0;
            foreach (var animator in ctx.All<Animator>())
            {
                if (!AuditContext.IsActive(animator)) continue;
                active++;

                if (animator.cullingMode == AnimatorCullingMode.AlwaysAnimate)
                {
                    var issue = ctx.Report("ANIM_ALWAYS_ANIMATE", Category.Animators, Severity.Warning, animator);
                    var target = animator;
                    issue.SetFix(FixKind.Review, target, false, () => target.cullingMode = AnimatorCullingMode.CullUpdateTransforms);
                }
                if (animator.runtimeAnimatorController == null)
                    ctx.Report("ANIM_NO_CONTROLLER", Category.Animators, Severity.Info, animator);
                if (animator.applyRootMotion && !HasRootMotionHandler(animator))
                    ctx.Report("ANIM_ROOT_MOTION", Category.Animators, Severity.Info, animator);
            }
            if (active > b.animatorsWarning)
                ctx.Report("ANIM_COUNT", Category.Animators, Severity.Warning, null, active, b.animatorsWarning);

            foreach (var skinned in ctx.All<SkinnedMeshRenderer>())
            {
                if (!AuditContext.IsActive(skinned)) continue;
                if (skinned.updateWhenOffscreen)
                {
                    var issue = ctx.Report("ANIM_UPDATE_OFFSCREEN", Category.Animators, Severity.Warning, skinned);
                    var target = skinned;
                    issue.SetFix(FixKind.Review, target, false, () => target.updateWhenOffscreen = false);
                }
                if (skinned.sharedMesh == null || AuditContext.FindInParents<Animator>(skinned.transform) == null) continue;
                long tris = AuditContext.Tris(skinned.sharedMesh);
                if (tris > b.skinnedTrisWarning)
                    ctx.Report(PlayerSettings.gpuSkinning ? "ANIM_HEAVY_SKIN" : "ANIM_HEAVY_SKIN_CPU", Category.Animators,
                        Severity.Warning, skinned, tris, b.skinnedTrisWarning);
            }
        }

        static bool HasRootMotionHandler(Animator animator)
        {
            var go = animator.gameObject;
            if (go.GetComponent<NavMeshAgent>() != null || go.GetComponent<CharacterController>() != null) return true;
            if (go.GetComponent<Rigidbody>() != null) return true;
            foreach (var behaviour in go.GetComponents<MonoBehaviour>())
                if (behaviour != null && ReflectionUtil.DeclaresMethod(behaviour.GetType(), "OnAnimatorMove")) return true;
            return false;
        }
    }
}
