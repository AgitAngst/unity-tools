using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityTools.SceneOptimization
{
    public static partial class SceneAuditor
    {
        static void AuditParticles(AuditContext ctx)
        {
            var subEmitters = SubEmitters(ctx);
            long total = 0;
            int systems = 0;
            foreach (var system in ctx.All<ParticleSystem>())
            {
                if (!AuditContext.IsActive(system)) continue;
                var main = system.main;
                int peak = PeakParticles(system, subEmitters.Contains(system));
                total += peak;
                systems++;
                if (peak > VRBudgets.ParticlesCritical)
                    ctx.Report("PART_MAX_PARTICLES", Category.Particles, Severity.Critical, system, peak, VRBudgets.ParticlesCritical, main.maxParticles);
                else if (peak > VRBudgets.ParticlesWarning)
                    ctx.Report("PART_MAX_PARTICLES", Category.Particles, Severity.Warning, system, peak, VRBudgets.ParticlesWarning, main.maxParticles);

                var lights = system.lights;
                // Without a light the module makes none.
                if (lights.enabled && lights.light != null)
                {
                    var issue = ctx.Report("PART_LIGHTS", Category.Particles, Severity.Warning, system, lights.maxLights);
                    var target = system;
                    issue.SetFix(FixKind.Review, target, false, () =>
                    {
                        var module = target.lights;
                        module.enabled = false;
                    });
                }

                var collision = system.collision;
                if (collision.enabled && collision.type == ParticleSystemCollisionType.World && collision.quality == ParticleSystemCollisionQuality.High)
                {
                    var issue = ctx.Report("PART_COLLISION_WORLD", Category.Particles, Severity.Warning, system);
                    var target = system;
                    issue.SetFix(FixKind.Review, target, false, () =>
                    {
                        var module = target.collision;
                        module.quality = ParticleSystemCollisionQuality.Medium;
                    });
                }

                var noise = system.noise;
                if (noise.enabled && noise.quality == ParticleSystemNoiseQuality.High)
                    ctx.Report("PART_NOISE_HIGH", Category.Particles, Severity.Info, system);

                var renderer = system.GetComponent<ParticleSystemRenderer>();
                if (renderer == null || !renderer.enabled) continue;
                var material = renderer.sharedMaterial;
                float size = LargestSize(system);
                if (IsSprite(renderer.renderMode) && material != null && material.renderQueue >= VRBudgets.TransparentQueue &&
                    size >= VRBudgets.LargeParticleMeters && peak >= VRBudgets.LargeParticleMinCount)
                    ctx.Report("PART_LARGE_TRANSPARENT", Category.Particles, Severity.Warning, system,
                        System.Math.Round(size, 1), peak, System.Math.Round(renderer.maxParticleSize, 2));

                if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                {
                    var issue = ctx.Report("PART_SHADOWS", Category.Particles, Severity.Warning, system);
                    var target = renderer;
                    issue.SetFix(FixKind.Review, target, false, () => target.shadowCastingMode = ShadowCastingMode.Off);
                }
                if (renderer.renderMode == ParticleSystemRenderMode.Mesh && renderer.mesh != null && !renderer.enableGPUInstancing)
                {
                    var issue = ctx.Report("PART_MESH_NO_INSTANCING", Category.Particles, Severity.Info, system, renderer.mesh.name);
                    var target = renderer;
                    issue.SetFix(FixKind.Review, target, false, () => target.enableGPUInstancing = true);
                }
            }
            if (total > VRBudgets.ParticlesTotalWarning)
                ctx.Report("PART_TOTAL", Category.Particles, Severity.Warning, null, total, VRBudgets.ParticlesTotalWarning, systems);
        }

        // Systems emitted by other systems: their own emission settings do not bound them.
        static HashSet<ParticleSystem> SubEmitters(AuditContext ctx)
        {
            var set = new HashSet<ParticleSystem>();
            foreach (var system in ctx.All<ParticleSystem>())
            {
                var sub = system.subEmitters;
                if (!sub.enabled) continue;
                for (int i = 0; i < sub.subEmittersCount; i++)
                {
                    var child = sub.GetSubEmitterSystem(i);
                    if (child != null) set.Add(child);
                }
            }
            return set;
        }

        // Particles alive at once: Max Particles, but no more than Rate over Time × Start Lifetime plus the bursts can
        // keep alive. Systems emitted by distance, by script or by another system are bounded by Max Particles only.
        public static int PeakParticles(ParticleSystem system, bool subEmitter)
        {
            var main = system.main;
            int max = main.maxParticles;
            var emission = system.emission;
            if (subEmitter || !emission.enabled || MaxOf(emission.rateOverDistance) > 0f) return max;
            float lifetime = MaxOf(main.startLifetime);
            float span = main.loop ? lifetime : Mathf.Min(lifetime, main.duration);
            double alive = MaxOf(emission.rateOverTime) * span;
            for (int i = 0; i < emission.burstCount; i++)
            {
                var burst = emission.GetBurst(i);
                // Repeating bursts overlap while the first ones still live.
                int overlap = Mathf.Max(1, Mathf.CeilToInt(lifetime / Mathf.Max(burst.repeatInterval, 0.01f)));
                int repeats = burst.cycleCount == 0 ? overlap : Mathf.Min(burst.cycleCount, overlap);
                alive += MaxOf(burst.count) * Mathf.Max(1, repeats);
            }
            return (int)System.Math.Min(max, System.Math.Ceiling(alive));
        }

        // The largest particle in metres: start size, the growth over its lifetime and the scale of the transform.
        public static float LargestSize(ParticleSystem system)
        {
            var main = system.main;
            float size = main.startSize3D ? Mathf.Max(MaxOf(main.startSizeX), MaxOf(main.startSizeY)) : MaxOf(main.startSize);
            var overLifetime = system.sizeOverLifetime;
            if (overLifetime.enabled)
                size *= overLifetime.separateAxes ? Mathf.Max(MaxOf(overLifetime.x), MaxOf(overLifetime.y)) : MaxOf(overLifetime.size);
            if (main.scalingMode != ParticleSystemScalingMode.Shape)
            {
                var scale = main.scalingMode == ParticleSystemScalingMode.Local ? system.transform.localScale : system.transform.lossyScale;
                size *= Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            }
            return size;
        }

        static bool IsSprite(ParticleSystemRenderMode mode)
        {
            return mode == ParticleSystemRenderMode.Billboard || mode == ParticleSystemRenderMode.Stretch ||
                   mode == ParticleSystemRenderMode.HorizontalBillboard || mode == ParticleSystemRenderMode.VerticalBillboard;
        }

        static float MaxOf(ParticleSystem.MinMaxCurve value)
        {
            switch (value.mode)
            {
                case ParticleSystemCurveMode.Constant: return value.constant;
                case ParticleSystemCurveMode.TwoConstants: return Mathf.Max(value.constantMin, value.constantMax);
                case ParticleSystemCurveMode.Curve: return value.curveMultiplier * MaxKey(value.curve);
                default: return value.curveMultiplier * Mathf.Max(MaxKey(value.curveMin), MaxKey(value.curveMax));
            }
        }

        static float MaxKey(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0) return 1f;
            float max = float.MinValue;
            foreach (var key in curve.keys) max = Mathf.Max(max, key.value);
            return max;
        }
    }
}
