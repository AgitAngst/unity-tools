using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Particle checks on systems made by the test: each check on its own system, none on a plain one.
    public static partial class AuditTestViewpoints
    {
        const string TorchPrefab = PrefabDir + "/Torch.prefab";

        static readonly string[] ParticleChecks =
        {
            "PART_MAX_PARTICLES", "PART_TOTAL", "PART_LARGE_TRANSPARENT", "PART_LIGHTS", "PART_COLLISION_WORLD", "PART_SHADOWS",
            "PART_MESH_NO_INSTANCING", "PART_NOISE_HIGH"
        };

        public static void RunParticles()
        {
            Run("particles", Particles);
        }

        static void Particles()
        {
            ParticleEstimates();
            ParticleIssues();
            ParticleFixes();
            ParticlePrefab();
            ParticleStrings();
        }

        // A system with the given emission: Max Particles, Rate over Time and Start Lifetime; shadows off, plain material.
        static ParticleSystem Particle(string name, int max, float rate, float lifetime, Material material = null)
        {
            var go = new GameObject(name);
            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.maxParticles = max;
            main.startLifetime = lifetime;
            main.startSize = 0.2f;
            var emission = system.emission;
            emission.rateOverTime = rate;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material != null ? material : AuditTestAssets.Plain;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return system;
        }

        static void BuildParticlesScene()
        {
            NewScene("Particles");
            Particle("Plain", 1000, 10f, 5f);
            Particle("Cap10k", 10000, 10f, 5f);
            Particle("Rain", 3000, 1000f, 2f);
            Particle("Storm", 8000, 4000f, 3f);
            for (int i = 0; i < 3; i++) Particle("Snow_" + i, 4000, 2000f, 2f);

            var burst = Particle("Burst", 2000, 0f, 2f);
            var emission = burst.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1500) });

            // A sub-emitter has no emission of its own: Max Particles bounds it.
            var spawner = Particle("Spawner", 10, 1f, 1f);
            var child = Particle("Spawner_Child", 1200, 0f, 1f);
            child.transform.SetParent(spawner.transform, false);
            var sub = spawner.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(child, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);

            var smoke = Particle("Smoke", 100, 10f, 5f, AuditTestAssets.Fade);
            var smokeMain = smoke.main;
            smokeMain.startSize = 2f;
            // Half-metre puffs under a parent scaled by 4: 2 m in the world.
            var scaled = new GameObject("Big_Parent");
            scaled.transform.localScale = Vector3.one * 4f;
            var puffs = Particle("Puffs", 100, 10f, 5f, AuditTestAssets.Fade);
            puffs.transform.SetParent(scaled.transform, false);
            var puffsMain = puffs.main;
            puffsMain.startSize = 0.5f;
            puffsMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
            Particle("Sparks_Small", 100, 10f, 5f, AuditTestAssets.Fade);
            // Large but opaque, large but few: not large transparent ones.
            var opaque = Particle("Big_Opaque", 100, 10f, 5f);
            var opaqueMain = opaque.main;
            opaqueMain.startSize = 2f;
            var few = Particle("Big_Few", 10, 10f, 5f, AuditTestAssets.Fade);
            var fewMain = few.main;
            fewMain.startSize = 2f;

            var lit = Particle("Sparks", 100, 10f, 1f);
            var lights = lit.lights;
            lights.enabled = true;
            lights.maxLights = 20;
            // The module takes a light as a template: an inactive one, so the light checks leave it alone.
            var template = new GameObject("Spark_Light");
            template.AddComponent<Light>().type = LightType.Point;
            template.SetActive(false);
            lights.light = template.GetComponent<Light>();
            // No light set: the module makes none.
            var unlit = Particle("Sparks_NoLight", 100, 10f, 1f);
            var unlitLights = unlit.lights;
            unlitLights.enabled = true;

            var debris = Particle("Debris", 100, 10f, 1f);
            var collision = debris.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.quality = ParticleSystemCollisionQuality.High;
            var planes = Particle("Debris_Planes", 100, 10f, 1f);
            var planeCollision = planes.collision;
            planeCollision.enabled = true;
            planeCollision.type = ParticleSystemCollisionType.Planes;

            Particle("Shadowy", 100, 10f, 1f).GetComponent<ParticleSystemRenderer>().shadowCastingMode = ShadowCastingMode.On;

            var rocks = Particle("Rocks", 100, 10f, 1f).GetComponent<ParticleSystemRenderer>();
            rocks.renderMode = ParticleSystemRenderMode.Mesh;
            rocks.mesh = AuditTestAssets.Grid12k;
            rocks.enableGPUInstancing = false;

            var noisy = Particle("Noisy", 100, 10f, 1f);
            var noise = noisy.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.High;

            // Not drawn, so not checked.
            var off = Particle("Off", 9000, 9000f, 5f);
            var offLights = off.lights;
            offLights.enabled = true;
            off.gameObject.SetActive(false);
        }

        static List<AuditIssue> ParticleIssuesOf(AuditResult result, string checkId)
        {
            return result.issues.Where(i => i.checkId == checkId).ToList();
        }

        static bool On(List<AuditIssue> issues, string name)
        {
            return issues.Any(i => i.target is Component && ((Component)i.target).name == name);
        }

        static void ParticleIssues()
        {
            BuildParticlesScene();
            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var particles = result.issues.Where(i => i.category == Category.Particles).ToList();
            Check(particles.All(i => !(i.target is Component) || ((Component)i.target).name != "Plain"), "a plain system has no particle issues");
            Check(particles.All(i => !(i.target is Component) || ((Component)i.target).name != "Off"), "an inactive system is not checked");

            var max = ParticleIssuesOf(result, "PART_MAX_PARTICLES");
            var rain = max.FirstOrDefault(i => ((Component)i.target).name == "Rain");
            Check(rain != null && rain.severity == Severity.Warning && (int)rain.args[0] == 2000 && (int)rain.args[2] == 3000,
                "Rain: 1000/s × 2 s = 2000 at once, Warning, Max Particles 3000");
            var storm = max.FirstOrDefault(i => ((Component)i.target).name == "Storm");
            Check(storm != null && storm.severity == Severity.Critical && (int)storm.args[0] == 8000, "Storm: capped by Max Particles 8000, Critical");
            Check(!On(max, "Cap10k"), "Max Particles 10 000 at 10/s × 5 s: no PART_MAX_PARTICLES");
            Check(On(max, "Burst"), "a burst of 1500: PART_MAX_PARTICLES");
            Check(On(max, "Spawner_Child") && !On(max, "Spawner"), "a sub-emitter is bounded by its Max Particles 1200");
            Check(max.Count(i => ((Component)i.target).name.StartsWith("Snow_")) == 3, "three snow systems of 4000");

            var total = ParticleIssuesOf(result, "PART_TOTAL");
            Check(total.Count == 1 && total[0].target == null && (long)total[0].args[0] > VRBudgets.ParticlesTotalWarning,
                "PART_TOTAL once for the scene (" + (total.Count > 0 ? total[0].args[0] : "—") + ")");

            var large = ParticleIssuesOf(result, "PART_LARGE_TRANSPARENT");
            var smoke = large.FirstOrDefault(i => ((Component)i.target).name == "Smoke");
            Check(smoke != null && System.Math.Abs((double)smoke.args[0] - 2.0) < 0.01 && (int)smoke.args[1] == 50, "Smoke: 2 m, 50 at once");
            Check(On(large, "Puffs"), "half-metre puffs under a parent scaled by 4 are large");
            Check(!On(large, "Sparks_Small") && !On(large, "Plain"), "small particles are not large transparent ones");
            Check(!On(large, "Big_Opaque") && !On(large, "Big_Few"), "2 m but opaque, 2 m but 10 at once: not large transparent ones");

            var lights = ParticleIssuesOf(result, "PART_LIGHTS");
            Check(lights.Count == 1 && On(lights, "Sparks") && (int)lights[0].args[0] == 20 && lights[0].fixKind == FixKind.Review,
                "Sparks: Lights module, 20 lights, a fix to review; no issue without a light set");
            var collision = ParticleIssuesOf(result, "PART_COLLISION_WORLD");
            Check(collision.Count == 1 && On(collision, "Debris"), "Debris: world collision of High quality; planes are not reported");
            var shadows = ParticleIssuesOf(result, "PART_SHADOWS");
            Check(shadows.Count == 1 && On(shadows, "Shadowy") && shadows[0].fixTarget is ParticleSystemRenderer, "Shadowy: casts shadows, the fix changes the renderer");
            var instancing = ParticleIssuesOf(result, "PART_MESH_NO_INSTANCING");
            Check(instancing.Count == 1 && On(instancing, "Rocks") && instancing[0].severity == Severity.Info, "Rocks: mesh particles without GPU Instancing");
            var noise = ParticleIssuesOf(result, "PART_NOISE_HIGH");
            Check(noise.Count == 1 && On(noise, "Noisy") && !noise[0].AutoFixable, "Noisy: Noise of High quality, no fix");
        }

        // The branches of the estimate on systems of their own.
        static void ParticleEstimates()
        {
            NewScene("Particles-Estimates");
            var once = Particle("Once", 1000, 100f, 5f);
            var onceMain = once.main;
            onceMain.loop = false;
            onceMain.duration = 2f;
            Check(SceneAuditor.PeakParticles(once, false) == 200, "a system that does not loop emits for its Duration: 100/s × 2 s (" + SceneAuditor.PeakParticles(once, false) + ")");

            var repeat = Particle("Repeat", 1000, 0f, 2f);
            var repeatEmission = repeat.emission;
            repeatEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) { cycleCount = 0, repeatInterval = 0.5f } });
            Check(SceneAuditor.PeakParticles(repeat, false) == 40, "a burst of 10 every 0.5 s with 2 s lifetime: 4 alive at once, 40 (" + SceneAuditor.PeakParticles(repeat, false) + ")");

            var distance = Particle("Distance", 700, 1f, 1f);
            var distanceEmission = distance.emission;
            distanceEmission.rateOverDistance = 5f;
            var off = Particle("Off_Emission", 600, 1f, 1f);
            var offEmission = off.emission;
            offEmission.enabled = false;
            Check(SceneAuditor.PeakParticles(distance, false) == 700 && SceneAuditor.PeakParticles(off, false) == 600 &&
                  SceneAuditor.PeakParticles(Particle("Sub", 500, 1f, 1f), true) == 500,
                "Rate over Distance, Emission off and a sub-emitter count Max Particles");

            var curve = Particle("Curve", 1000, 0f, 1f);
            var curveEmission = curve.emission;
            curveEmission.rateOverTime = new ParticleSystem.MinMaxCurve(100f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            Check(SceneAuditor.PeakParticles(curve, false) == 100, "a rate curve counts its highest key × the multiplier");

            var wide = Particle("Wide", 100, 1f, 1f);
            var wideMain = wide.main;
            wideMain.startSize3D = true;
            wideMain.startSizeX = 3f;
            wideMain.startSizeY = 1f;
            wideMain.startSizeZ = 1f;
            var grow = Particle("Grow", 100, 1f, 1f);
            var growMain = grow.main;
            growMain.startSize = 1f;
            var growSize = grow.sizeOverLifetime;
            growSize.enabled = true;
            growSize.size = new ParticleSystem.MinMaxCurve(2f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            Check(Mathf.Abs(SceneAuditor.LargestSize(wide) - 3f) < 0.01f && Mathf.Abs(SceneAuditor.LargestSize(grow) - 2f) < 0.01f,
                "3D start size takes the larger side; Size over Lifetime grows it");

            var parent = new GameObject("Scaled_10");
            parent.transform.localScale = Vector3.one * 10f;
            var local = Particle("Local", 100, 1f, 1f);
            local.transform.SetParent(parent.transform, false);
            local.transform.localScale = Vector3.one * 2f;
            var localMain = local.main;
            localMain.startSize = 1f;
            localMain.scalingMode = ParticleSystemScalingMode.Local;
            var shape = Particle("Shape", 100, 1f, 1f);
            shape.transform.SetParent(parent.transform, false);
            var shapeMain = shape.main;
            shapeMain.startSize = 1f;
            shapeMain.scalingMode = ParticleSystemScalingMode.Shape;
            Check(Mathf.Abs(SceneAuditor.LargestSize(local) - 2f) < 0.01f && Mathf.Abs(SceneAuditor.LargestSize(shape) - 1f) < 0.01f,
                "Scaling Mode Local takes its own scale, Shape none (" + SceneAuditor.LargestSize(local) + ", " + SceneAuditor.LargestSize(shape) + ")");
        }

        static void ParticleFixes()
        {
            BuildParticlesScene();
            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var fixes = result.issues.Where(i => i.category == Category.Particles && i.AutoFixable).ToList();
            Check(fixes.Count == 4 && fixes.All(i => !i.fixTouchesAsset), "four scene fixes: lights, collision, shadows, instancing (" + fixes.Count + ")");
            var sparks = GameObject.Find("Sparks").GetComponent<ParticleSystem>();
            var debris = GameObject.Find("Debris").GetComponent<ParticleSystem>();
            var shadowy = GameObject.Find("Shadowy").GetComponent<ParticleSystemRenderer>();
            var rocks = GameObject.Find("Rocks").GetComponent<ParticleSystemRenderer>();

            Undo.IncrementCurrentGroup();
            int applied = AutoFixer.Apply(fixes);
            Check(applied == 4 && !sparks.lights.enabled && debris.collision.quality == ParticleSystemCollisionQuality.Medium &&
                  shadowy.shadowCastingMode == ShadowCastingMode.Off && rocks.enableGPUInstancing,
                "the fixes turn the lights off, collision to Medium, shadows off, instancing on");
            var again = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            Check(again.issues.All(i => i.category != Category.Particles || !i.AutoFixable), "a new run finds nothing to fix");

            Undo.PerformUndo();
            Check(sparks.lights.enabled && debris.collision.quality == ParticleSystemCollisionQuality.High &&
                  shadowy.shadowCastingMode == ShadowCastingMode.On && !rocks.enableGPUInstancing,
                "Undo brings all four back");
        }

        // Five torches of one prefab with the Lights module: one fix in the prefab turns the module off for all.
        static void ParticlePrefab()
        {
            NewScene("Particles-Torches");
            Directory.CreateDirectory(PrefabDir);
            var template = Particle("Torch", 50, 10f, 1f);
            var lights = template.lights;
            lights.enabled = true;
            var torchLight = new GameObject("Torch_Light");
            torchLight.AddComponent<Light>();
            torchLight.SetActive(false);
            // A child of the torch, so the prefab keeps the reference.
            torchLight.transform.SetParent(template.transform, false);
            lights.light = torchLight.GetComponent<Light>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, TorchPrefab);
            Object.DestroyImmediate(template.gameObject);
            var torches = Enumerable.Range(0, 5).Select(i => ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<ParticleSystem>()).ToList();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            var result = SceneAuditor.RunFullAudit(new VRBudgets(), false);
            var issues = ParticleIssuesOf(result, "PART_LIGHTS");
            var rows = PrefabFixes.Rows(issues);
            Check(issues.Count == 5 && rows.Count == 1 && rows[0].copies != null && rows[0].copies.CanFixInPrefab, "five torches: one copies row, fixable in the prefab");
            int applied = AutoFixer.Apply(PrefabFixes.Plan(issues, true));
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(TorchPrefab).GetComponent<ParticleSystem>();
            Check(applied == 1 && !asset.lights.enabled && torches.All(t => !t.lights.enabled), "the prefab and all five torches have the module off");
            Check(torches.All(t => !new SerializedObject(t).FindProperty("LightsModule.enabled").prefabOverride), "no torch keeps an override of the module");
        }

        static void ParticleStrings()
        {
            var fixable = new[] { "PART_LIGHTS", "PART_COLLISION_WORLD", "PART_SHADOWS", "PART_MESH_NO_INSTANCING" };
            var missing = new List<string>();
            var lang = Strings.Current;
            foreach (var language in new[] { Lang.En, Lang.Ru })
            {
                Strings.Current = language;
                foreach (var id in ParticleChecks)
                    foreach (var key in new[] { id, id + ".title", id + ".fix" }.Concat(fixable.Contains(id) ? new[] { id + ".do" } : new string[0]))
                        if (!Strings.Has(key) || Strings.Get(key).Length == 0) missing.Add(language + " " + key);
            }
            Strings.Current = lang;
            Check(missing.Count == 0 && Strings.CategoryName(Category.Particles) != "cat.Particles",
                "every particle check has a title, a message, how to fix and what the fix does" + (missing.Count > 0 ? ": " + string.Join(", ", missing.ToArray()) : ""));
        }
    }
}
