namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        static void AddParticles()
        {
            // Particle checks: titles, messages, how to fix.
            Add("PART_MAX_PARTICLES.title", "Many particles at once", "Много частиц одновременно");
            Add("PART_MAX_PARTICLES", "Up to {0} particles at once (more than {1}); Max Particles is {2}.",
                "До {0} частиц одновременно (больше {1}); Max Particles = {2}.");
            Add("PART_MAX_PARTICLES.fix", "Lower Rate over Time, Start Lifetime or Max Particles: every particle is drawn for both eyes.",
                "Снизьте Rate over Time, Start Lifetime или Max Particles: каждая частица рисуется для обоих глаз.");
            Add("PART_TOTAL.title", "Many particles in the scene", "Много частиц в сцене");
            Add("PART_TOTAL", "Up to {0} particles at once in the scene (more than {1}) in {2} systems.",
                "До {0} частиц одновременно в сцене (больше {1}) в {2} системах.");
            Add("PART_TOTAL.fix", "Start effects only near the player and stop them out of sight (culling of the particle systems).",
                "Запускайте эффекты только рядом с игроком и останавливайте вне поля зрения.");
            Add("PART_LARGE_TRANSPARENT.title", "Large transparent particles", "Крупные прозрачные частицы");
            Add("PART_LARGE_TRANSPARENT", "Transparent particles up to {0} m, up to {1} at once: layers of overdraw on the screen.",
                "Прозрачные частицы до {0} м, одновременно до {1}: экран перерисовывается слоями.");
            Add("PART_LARGE_TRANSPARENT.fix",
                "Fewer and smaller particles; thick smoke looks the same with fewer layers and a denser texture. Max Particle Size of the renderer limits particles close to the eyes (now {2} of the screen).",
                "Меньше частиц и меньше размер; густой дым выглядит так же с меньшим числом слоёв и плотной текстурой. Max Particle Size рендерера ограничивает частицы у глаз (сейчас {2} экрана).");
            Add("PART_LIGHTS.title", "Particles with lights", "Частицы со светом");
            Add("PART_LIGHTS", "The Lights module adds up to {0} realtime lights.", "Модуль Lights добавляет до {0} источников реального времени.");
            Add("PART_LIGHTS.fix", "Each light is one more pass for the objects it reaches. Make the glow with an emissive material or one light per effect.",
                "Каждый источник — ещё проход для объектов, до которых он достаёт. Свечение — материалом с emission или одним источником на эффект.");
            Add("PART_LIGHTS.do", "Turn off the Lights module", "Выключить модуль Lights");
            Add("PART_COLLISION_WORLD.title", "World collision of High quality", "Коллизии с миром качества High");
            Add("PART_COLLISION_WORLD", "World collision of High quality: every particle is traced on the CPU every frame.",
                "Коллизии с миром качества High: каждая частица трассируется на CPU каждый кадр.");
            Add("PART_COLLISION_WORLD.fix",
                "Medium or Low caches the hits in voxels and is many times cheaper, but collides with static colliders only; sparks often need only Planes.",
                "Medium или Low кэшируют столкновения в вокселях и в разы дешевле, но сталкиваются только со статичными коллайдерами; искрам часто хватает Planes.");
            Add("PART_COLLISION_WORLD.do", "Collision Quality = Medium (static colliders only)", "Collision Quality = Medium (только статичные коллайдеры)");
            Add("PART_SHADOWS.title", "Particles cast shadows", "Частицы отбрасывают тени");
            Add("PART_SHADOWS", "The particles cast shadows: one more pass into the shadow map of each light.",
                "Частицы отбрасывают тени: ещё проход в карту теней каждого источника.");
            Add("PART_SHADOWS.fix", "Shadows of particles are hardly seen; turn them off.", "Тени частиц почти не видны — выключите их.");
            Add("PART_SHADOWS.do", "Cast Shadows = Off", "Cast Shadows = Off");
            Add("PART_MESH_NO_INSTANCING.title", "Mesh particles without GPU Instancing", "Меш-частицы без GPU Instancing");
            Add("PART_MESH_NO_INSTANCING", "Mesh particles ({0}) without GPU Instancing.", "Меш-частицы ({0}) без GPU Instancing.");
            Add("PART_MESH_NO_INSTANCING.fix", "With Enable GPU Instancing the meshes are drawn in one call (the Particles/Standard shaders support it).",
                "С Enable GPU Instancing меши рисуются одним вызовом (шейдеры Particles/Standard это умеют).");
            Add("PART_MESH_NO_INSTANCING.do", "Turn on GPU Instancing", "Включить GPU Instancing");
            Add("PART_SOFT_PARTICLES.title", "Soft Particles on", "Включены Soft Particles");
            Add("PART_SOFT_PARTICLES", "Soft Particles are on in the quality level; the scenes have {0} particle systems.", "На уровне качества включены Soft Particles; систем частиц в сценах: {0}.");
            Add("PART_SOFT_PARTICLES.fix", "Soft particles need the depth texture of the camera, which in Forward can take an extra pass: turn Soft Particles off in Quality unless particles must fade into walls.",
                "Мягким частицам нужна текстура глубины камеры, а в Forward она может стоить отдельного прохода: выключите Soft Particles в Quality, если частицам не нужно мягко уходить в стены.");
            Add("PART_NOISE_HIGH.title", "Noise of High quality", "Noise качества High");
            Add("PART_NOISE_HIGH", "The Noise module is of High quality.", "Модуль Noise качества High.");
            Add("PART_NOISE_HIGH.fix", "Medium or Low is cheaper on the CPU and looks almost the same.", "Medium или Low дешевле на CPU и выглядят почти так же.");
        }
    }
}
