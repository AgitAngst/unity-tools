using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public enum Lang { En = 0, Ru = 1 }

    // Every window text in English and Russian. Console and log output stays English and is not here.
    public static class Strings
    {
        const string LangPref = "UnityTools.SceneAudit.Lang";

        static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>();
        static bool langLoaded;
        static Lang lang;

        public static Lang Current
        {
            get
            {
                if (!langLoaded)
                {
                    var fallback = Application.systemLanguage == SystemLanguage.Russian ? Lang.Ru : Lang.En;
                    lang = (Lang)EditorPrefs.GetInt(LangPref, (int)fallback);
                    langLoaded = true;
                }
                return lang;
            }
            set
            {
                lang = value;
                langLoaded = true;
                EditorPrefs.SetInt(LangPref, (int)value);
            }
        }

        public static bool Has(string key) { return table.ContainsKey(key); }

        public static IEnumerable<string> Keys { get { return table.Keys; } }

        public static string Get(string key)
        {
            string[] texts;
            if (key != null && table.TryGetValue(key, out texts)) return texts[(int)Current];
            return key ?? "";
        }

        public static string Format(string key, params object[] args)
        {
            var text = Get(key);
            if (args == null || args.Length == 0) return text;
            try
            {
                return string.Format(CultureInfo.InvariantCulture, text, args);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        public static string CategoryName(Category category) { return Get("cat." + category); }

        public static string SeverityName(Severity severity) { return Get("sev." + severity); }

        static void Add(string key, string en, string ru) { table[key] = new[] { en, ru }; }

        static Strings()
        {
            AddUi();
            AddDrawCalls();
            AddMeshes();
            AddTextures();
            AddLighting();
            AddPhysics();
            AddAnimators();
            AddAudio();
            AddVr();
            AddGeneral();
            AddTitles();
        }

        // Plain names of the checks: titles of the grouped list.
        static void AddTitles()
        {
            var titles = new[]
            {
                "DC_BUDGET", "Draw call budget exceeded", "Превышен бюджет draw calls",
                "DC_SHADOW_CASTERS", "Shadow casters", "Объекты с тенями",
                "DC_MANY_MATERIALS", "Too many materials on a renderer", "Слишком много материалов на рендерере",
                "DC_MULTI_MATERIAL", "Several materials on a renderer", "Несколько материалов на рендерере",
                "DC_EXTRA_MATERIALS", "More materials than submeshes", "Материалов больше, чем submesh",
                "DC_STATIC_CANDIDATE", "Could be Batching Static", "Можно сделать Batching Static",
                "DC_INSTANCING_OFF", "GPU Instancing is off", "Выключен GPU Instancing",
                "DC_INSTANCING_UNKNOWN", "GPU Instancing is off (shader not checked)", "Выключен GPU Instancing (шейдер не проверен)",
                "MESH_SCENE_TRIS", "Triangle budget exceeded", "Превышен бюджет треугольников",
                "MESH_NO_LOD", "Heavy mesh without LOD", "Тяжёлый меш без LOD",
                "MESH_READ_WRITE", "Model with Read/Write", "Модель с Read/Write",
                "MESH_READ_WRITE_NEEDED", "Model with Read/Write (used by colliders or particles)", "Модель с Read/Write (нужен коллайдерам или частицам)",
                "MESH_NOT_OPTIMIZED", "Mesh not fully optimized", "Меш оптимизирован не полностью",
                "MESH_BONES_PER_VERTEX", "More than 4 bones per vertex", "Больше 4 костей на вершину",
                "MESH_QUALITY_SKIN_WEIGHTS", "Skin Weights above 4 bones", "Skin Weights больше 4 костей",
                "MESH_GPU_SKINNING_OFF", "GPU Skinning is off", "Выключен GPU Skinning",
                "TEX_SIZE", "Texture too large", "Слишком большая текстура",
                "TEX_NOT_NORMAL_MAP", "Normal map of the wrong type", "Карта нормалей не того типа",
                "TEX_NO_MIPMAPS", "Texture without mipmaps", "Текстура без mipmaps",
                "TEX_UNCOMPRESSED", "Uncompressed texture", "Несжатая текстура",
                "TEX_READ_WRITE", "Texture with Read/Write", "Текстура с Read/Write",
                "TEX_MEMORY", "Texture memory budget exceeded", "Превышен бюджет памяти текстур",
                "TEX_ANISO_DISABLED", "Anisotropic filtering off", "Анизотропная фильтрация выключена",
                "TEX_NOT_TRILINEAR", "Textures without trilinear filtering", "Текстуры без трилинейной фильтрации",
                "LIGHT_REALTIME", "Realtime point or spot light", "Realtime point или spot",
                "LIGHT_SHADOWED", "Too many shadowed lights", "Слишком много источников с тенями",
                "LIGHT_NO_UV1", "Lightmapped mesh without UV1", "Меш под lightmap без UV1",
                "LIGHT_SHADOW_RESOLUTION", "High shadow resolution", "Высокое разрешение теней",
                "LIGHT_SHADOW_DISTANCE", "Long shadow distance", "Большая дальность теней",
                "LIGHT_PIXEL_LIGHTS", "More than one pixel light", "Больше одного pixel light",
                "LIGHT_REALTIME_GI", "Realtime GI is on", "Включён Realtime GI",
                "PHYS_CONCAVE_DYNAMIC", "Non-convex collider on a dynamic body", "Невыпуклый коллайдер на динамическом теле",
                "PHYS_CONCAVE_KINEMATIC", "Non-convex collider on a kinematic body", "Невыпуклый коллайдер на kinematic теле",
                "PHYS_RENDER_MESH_COLLIDER", "Collider on a heavy render mesh", "Коллайдер на тяжёлом рендер-меше",
                "PHYS_SOLVER_ITERATIONS", "Many solver iterations", "Много итераций решателя",
                "PHYS_FIXED_STEP_SLOW", "Physics slower than 90 Hz", "Физика реже 90 Гц",
                "PHYS_FIXED_STEP_FAST", "Physics faster than 90 Hz", "Физика чаще 90 Гц",
                "ANIM_ALWAYS_ANIMATE", "Animator always animates", "Animator анимирует всегда",
                "ANIM_NO_CONTROLLER", "Animator without controller", "Animator без контроллера",
                "ANIM_ROOT_MOTION", "Root motion without a handler", "Root motion без обработчика",
                "ANIM_COUNT", "Too many active Animators", "Слишком много активных Animator",
                "ANIM_UPDATE_OFFSCREEN", "Skinned mesh updates off-screen", "SkinnedMesh обновляется за кадром",
                "ANIM_HEAVY_SKIN", "Heavy skinned mesh", "Тяжёлый SkinnedMesh",
                "ANIM_HEAVY_SKIN_CPU", "Heavy skinned mesh on the CPU", "Тяжёлый SkinnedMesh на CPU",
                "AUDIO_DECOMPRESS_LONG", "Long clip unpacked on load", "Длинный клип распаковывается при загрузке",
                "AUDIO_PCM_LARGE", "Large PCM clip", "Большой PCM-клип",
                "AUDIO_PRELOAD_LONG", "Long clip preloaded", "Длинный клип с предзагрузкой",
                "AUDIO_NO_CLIP", "AudioSource without a clip", "AudioSource без клипа",
                "AUDIO_2D_SOURCE", "2D sound", "2D-звук",
                "AUDIO_SOURCE_COUNT", "Too many AudioSources", "Слишком много AudioSource",
                "AUDIO_PRIORITY_ZERO", "Many sources with top priority", "Много источников с высшим приоритетом",
                "AUDIO_LISTENERS_MANY", "Several AudioListeners", "Несколько AudioListener",
                "AUDIO_NO_LISTENER", "No AudioListener", "Нет AudioListener",
                "AUDIO_LISTENER_NO_CAMERA", "AudioListener not on a camera", "AudioListener не на камере",
                "AUDIO_DSP_BUFFER", "DSP buffer not tuned for VR", "DSP-буфер не настроен под VR",
                "AUDIO_SPEAKER_MODE", "Surround speaker mode", "Объёмный режим динамиков",
                "AUDIO_NO_SPATIALIZER", "No spatializer", "Нет spatializer",
                "VR_NOT_ENABLED", "VR is not enabled", "VR не включён",
                "VR_NO_OPENVR", "VR without OpenVR", "VR без OpenVR",
                "VR_BOTH_SYSTEMS", "Two VR systems enabled", "Включены две VR-системы",
                "VR_STEREO_MODE", "Stereo mode is not Single Pass Instanced", "Режим stereo не Single Pass Instanced",
                "VR_DEFERRED_TIER", "Deferred rendering for Standalone", "Deferred для Standalone",
                "VR_DEFERRED_CAMERA", "VR camera in Deferred", "VR-камера в Deferred",
                "VR_MSAA", "MSAA below 4x", "MSAA ниже 4x",
                "VR_NO_CAMERA", "No VR camera", "Нет VR-камеры",
                "VR_CAMERA_NO_MSAA", "VR camera without MSAA", "VR-камера без MSAA",
                "VR_CAMERA_HDR", "HDR on a VR camera", "HDR на VR-камере",
                "VR_FAR_CLIP", "Far clip plane too far", "Дальняя плоскость слишком далеко",
                "VR_NEAR_CLIP", "Near clip plane too close", "Ближняя плоскость слишком близко",
                "VR_IMAGE_EFFECT", "Full-screen effect on a VR camera", "Полноэкранный эффект на VR-камере",
                "VR_CANVAS_OVERLAY", "Canvas not visible in the headset", "Canvas не виден в шлеме",
                "VR_CANVAS_CAMERA", "Canvas stuck to the head", "Canvas прилипает к голове",
                "VR_PP_LAYER", "Post-processing on a camera", "Постобработка на камере",
                "VR_PP_EFFECTS", "Post-processing effects on", "Включены эффекты постобработки",
                "VR_STEAMVR_MISSING", "SteamVR Plugin not found", "SteamVR Plugin не найден",
                "VR_STEAMVR_NOT_IN_SCENE", "No SteamVR rig in the scenes", "В сценах нет SteamVR-rig",
                "VR_PROBE_EVERY_FRAME", "Reflection probe updated every frame", "Reflection Probe каждый кадр",
                "VR_PROBE_REALTIME", "Realtime reflection probe", "Realtime Reflection Probe",
                "VR_NO_OCCLUSION", "Occlusion culling not baked", "Occlusion culling не запечён",
                "GEN_EMPTY_LEAF", "Empty object", "Пустой объект",
                "GEN_NEGATIVE_SCALE", "Negative scale", "Отрицательный масштаб",
                "GEN_DEEP_HIERARCHY", "Deep hierarchy", "Глубокая иерархия",
                "GEN_MISSING_SCRIPTS", "Missing scripts", "Пропавшие скрипты",
                "GEN_INACTIVE_HEAVY_MESH", "Inactive object with a heavy mesh", "Неактивный объект с тяжёлым мешем",
                "GEN_INACTIVE_HEAVY_TEXTURE", "Inactive object with a large texture", "Неактивный объект с большой текстурой",
            };
            for (int i = 0; i + 2 < titles.Length; i += 3) Add(titles[i] + ".title", titles[i + 1], titles[i + 2]);
        }

        static void AddUi()
        {
            Add("ui.title", "VR Scene Audit", "VR-аудит сцены");
            Add("ui.run", "Run Audit", "Запустить аудит");
            Add("ui.rerun", "Run Again", "Запустить снова");
            Add("ui.run.tip", "Checks every loaded scene. Nothing is changed.", "Проверяет все загруженные сцены. Ничего не меняет.");
            Add("ui.tab.problems", "Problems", "Проблемы");
            Add("ui.tab.problems.n", "Problems ({0})", "Проблемы ({0})");
            Add("ui.tab.budgets", "Budgets", "Бюджеты");
            Add("ui.tab.profiler", "Profiler", "Профайлер");
            Add("ui.empty.title", "VR scene audit", "Аудит VR-сцены");
            Add("ui.empty.text", "Checks the open scenes for HTC Vive Pro at 90 FPS: draw calls, meshes, textures, lighting, physics, animation, audio and VR settings. Nothing is changed: fixes are applied only when you press Fix and confirm.",
                "Проверит открытые сцены под HTC Vive Pro и 90 FPS: draw calls, меши, текстуры, свет, физику, анимацию, звук и настройки VR. Ничего не меняет: исправления — только по кнопке «Исправить» и после подтверждения.");
            Add("ui.verdict.critical", "{0} critical problems: fix them first.", "Критичных проблем: {0} — исправьте их в первую очередь.");
            Add("ui.verdict.warning", "No critical problems. {0} warnings are worth a look.", "Критичных проблем нет. Предупреждений: {0} — стоит просмотреть.");
            Add("ui.verdict.clean", "No critical problems and no warnings.", "Нет ни критичных проблем, ни предупреждений.");
            Add("ui.card.hidden", "hidden", "скрыто");
            Add("ui.card.hide.tip", "Click to hide these problems.", "Нажмите, чтобы скрыть эти проблемы.");
            Add("ui.card.show.tip", "Click to show these problems.", "Нажмите, чтобы показать эти проблемы.");
            Add("ui.search.tip", "Filters by problem name, object, asset path or text.", "Фильтр по названию проблемы, объекту, пути ассета или тексту.");
            Add("ui.byCategory", "By category", "По категориям");
            Add("ui.byCategory.tip", "Group the list by category instead of by importance.", "Сгруппировать список по категориям, а не по важности.");
            Add("ui.expandAll", "Expand all", "Развернуть все");
            Add("ui.collapseAll", "Collapse all", "Свернуть все");
            Add("ui.nothingMatches", "Nothing matches the filters.", "Под фильтры ничего не подходит.");
            Add("ui.hiddenByFilter", "{0} problems are hidden by the importance filter above.", "Ещё {0} проблем скрыто фильтром важности выше.");
            Add("ui.fixAll", "Fix all ({0})", "Исправить все ({0})");
            Add("ui.fix.tip", "Shows the list of changes first; nothing changes without confirmation.", "Сначала покажет список изменений; без подтверждения ничего не меняется.");
            Add("ui.howToFix", "How to fix: {0}", "Как исправить: {0}");
            Add("ui.select.tip", "Select and highlight it.", "Выделить и подсветить.");
            Add("ui.settings.tip", "Open the Project Settings page.", "Открыть страницу Project Settings.");
            Add("ui.autoFix.tip", "Enables mipmaps and GPU Instancing where it is safe; shows the list first.", "Включит mipmaps и GPU Instancing там, где это безопасно; сначала покажет список.");
            Add("ui.budgets.rerun", "Thresholds apply on the next run.", "Пороги применяются при следующем запуске аудита.");
            Add("ui.prof.intro", "Plays the scene for a few seconds and measures frame time, draw calls and memory.", "Запускает сцену на несколько секунд и меряет время кадра, draw calls и память.");
            Add("ui.xr", "XR: {0}", "XR: {0}");
            Add("ui.xr.legacy", "Legacy VR ({0})", "Legacy VR ({0})");
            Add("ui.xr.management", "XR Plug-in Management ({0})", "XR Plug-in Management ({0})");
            Add("ui.xr.both", "Legacy VR and XR Plug-in Management ({0})", "Legacy VR и XR Plug-in Management ({0})");
            Add("ui.xr.none", "VR not enabled", "VR не включён");
            Add("ui.stereo", "Stereo: {0}", "Stereo: {0}");
            Add("ui.stereo.unknown", "unknown", "неизвестно");
            Add("ui.quality.default", "Quality level: {0} (Standalone default)", "Уровень качества: {0} (по умолчанию для Standalone)");
            Add("ui.quality.current", "Quality level: {0} (current; Standalone default not found)", "Уровень качества: {0} (текущий; уровень Standalone не найден)");
            Add("ui.scenes", "Scenes: {0} · audited {1}", "Сцены: {0} · аудит {1}");
            Add("ui.budgetOverview", "Scene Budget Overview", "Бюджеты сцены");
            Add("ui.budget.tris", "Triangles (LOD0)", "Треугольники (LOD0)");
            Add("ui.budget.drawCalls", "Draw calls", "Draw calls");
            Add("ui.budget.texMem", "Texture memory", "Память текстур");
            Add("ui.budget.value", "{0} of {1}", "{0} из {1}");
            Add("ui.budget.tip", "Estimate against the Warning threshold; the bar ends at the Critical threshold.",
                "Оценка против порога предупреждения; полоса заканчивается на критичном пороге.");
            Add("ui.budget.note", "Estimates, not measurements: triangles count LOD0 without culling; draw calls count material slots and shadow passes without batching, doubled unless Single Pass Instanced; texture memory comes from format, size and mip levels.",
                "Это оценки, а не замеры: треугольники — LOD0 без отсечения; draw calls — слоты материалов и проходы теней без батчинга, ×2, если не Single Pass Instanced; память текстур — по формату, размеру и mip-уровням.");
            Add("ui.search", "Search", "Поиск");
            Add("ui.showMore", "Show {0} more", "Показать ещё {0}");
            Add("ui.noIssues", "No issues.", "Проблем нет.");
            Add("ui.autoFix", "Auto-Fix Safe Issues ({0})", "Исправить безопасное ({0})");
            Add("ui.export", "Export Report", "Сохранить отчёт");
            Add("ui.fix", "Fix", "Исправить");
            Add("ui.openSettings", "Settings", "Настройки");
            Add("ui.users", "used {0}×", "использований: {0}");
            Add("ui.progress.collect", "Collecting scene data…", "Сбор данных сцены…");
            Add("ui.progress.category", "Checking: {0}", "Проверка: {0}");

            Add("ui.budgets", "Budgets", "Бюджеты");
            Add("ui.budgets.file", "Saved in {0}", "Хранятся в {0}");
            Add("ui.budgets.defaults.note", "Defaults (the file is created on Save)", "По умолчанию (файл появится после «Сохранить»)");
            Add("ui.budgets.save", "Save", "Сохранить");
            Add("ui.budgets.reset", "Defaults", "По умолчанию");
            Add("ui.budgets.warning", "Warning", "Предупреждение");
            Add("ui.budgets.critical", "Critical", "Критично");
            Add("ui.b.drawCalls", "Draw calls per frame", "Draw calls за кадр");
            Add("ui.b.sceneTris", "Scene triangles", "Треугольники сцены");
            Add("ui.b.objectTris", "Object triangles without LOD", "Треугольники объекта без LOD");
            Add("ui.b.texMem", "Texture memory, MB", "Память текстур, МБ");
            Add("ui.b.texSize", "Texture side, px", "Сторона текстуры, px");
            Add("ui.b.shadowLights", "Shadowed lights", "Источники с тенями");
            Add("ui.b.animators", "Active Animators", "Активные Animator");
            Add("ui.b.skinnedTris", "Skinned mesh triangles", "Треугольники SkinnedMesh");
            Add("ui.b.audioSources", "Active AudioSources", "Активные AudioSource");
            Add("ui.b.colliderTris", "MeshCollider triangles (Info)", "MeshCollider: треугольники");
            Add("ui.mb", " MB", " МБ");
            Add("ui.b.frameTime", "Frame time, ms", "Время кадра, мс");

            Add("ui.export.title", "Export Report", "Сохранить отчёт");
            Add("ui.export.done", "Report saved: {0}", "Отчёт сохранён: {0}");
            Add("ui.export.error", "Could not save the report: {0}", "Не удалось сохранить отчёт: {0}");

            Add("ui.fix.title", "Confirm Fixes", "Подтверждение исправлений");
            Add("ui.fix.intro", "Review the changes. Unchecked items are skipped.", "Проверьте изменения. Пункты без галочки не применяются.");
            Add("ui.fix.groupScene", "Scene — can be undone with Undo", "Сцена — откатывается Undo");
            Add("ui.fix.groupAssets", "Assets — change the asset in the whole project, Undo does not revert them, the asset is reimported",
                "Ассеты — меняются во всём проекте, Undo их не откатывает, ассет переимпортируется");
            Add("ui.fix.override", "becomes a prefab override", "станет override префаба");
            Add("ui.fix.apply", "Apply ({0})", "Применить ({0})");
            Add("ui.fix.cancel", "Cancel", "Отмена");
            Add("ui.fix.none", "Nothing to fix.", "Исправлять нечего.");
            Add("ui.fix.ok", "OK", "ОК");

            Add("ui.prof.title", "Profiler Snapshot", "Снимок профайлера");
            Add("ui.prof.frames", "Frames", "Кадров");
            Add("ui.prof.warmup", "Warm-up frames", "Кадров разогрева");
            Add("ui.prof.capture", "Capture Profiler Data", "Снять данные");
            Add("ui.prof.cancel", "Cancel", "Отмена");
            Add("ui.prof.compare", "Compare with Previous", "Сравнить с прошлым");
            Add("ui.prof.entering", "Entering Play Mode…", "Вход в Play Mode…");
            Add("ui.prof.warming", "Warming up…", "Разогрев…");
            Add("ui.prof.running", "Capturing: {0} / {1} frames", "Снимаю: {0} из {1} кадров");
            Add("ui.prof.none", "No snapshot for this scene yet.", "Для этой сцены снимков ещё нет.");
            Add("ui.prof.noPrevious", "No previous snapshot to compare with.", "Нет прошлого снимка для сравнения.");
            Add("ui.prof.taken", "Captured {0}, {1} frames", "Снято {0}, кадров: {1}");
            Add("ui.prof.note", "Editor measurement is indicative only; final numbers come from the Profiler attached to a Development Build.",
                "Замер в редакторе — ориентир; итоговые цифры — профайлер на Development Build.");
            Add("ui.prof.cpuNote", "CPU is the main thread time from the Stats window; it may include waiting for the GPU or the headset.",
                "CPU — время главного потока из окна Stats; в него может входить ожидание GPU и шлема.");
            Add("ui.m.fps", "FPS (avg / min / max)", "FPS (сред. / мин. / макс.)");
            Add("ui.m.cpu", "CPU frame, ms (avg / peak)", "Кадр CPU, мс (сред. / пик)");
            Add("ui.m.gpu", "GPU frame, ms (avg / peak)", "Кадр GPU, мс (сред. / пик)");
            Add("ui.m.drawCalls", "Draw calls", "Draw calls");
            Add("ui.m.setPass", "SetPass calls", "SetPass calls");
            Add("ui.m.batches", "Batches", "Batches");
            Add("ui.m.tris", "Triangles per frame", "Треугольников за кадр");
            Add("ui.m.texMem", "Texture memory, MB", "Память текстур, МБ");
            Add("ui.m.meshMem", "Mesh memory, MB", "Память мешей, МБ");
            Add("ui.m.alloc", "Total allocated, MB", "Всего выделено, МБ");
            Add("ui.na", "n/a", "н/д");

            Add("cat.DrawCalls", "Draw Calls", "Draw calls и батчинг");
            Add("cat.Meshes", "Meshes", "Меши");
            Add("cat.Textures", "Textures", "Текстуры");
            Add("cat.Lighting", "Lighting", "Освещение");
            Add("cat.Physics", "Physics", "Физика");
            Add("cat.Animators", "Animators", "Аниматоры");
            Add("cat.Audio", "Audio", "Аудио");
            Add("cat.VRSpecific", "VR-Specific", "VR");
            Add("cat.General", "General", "Общее");

            Add("sev.Critical", "Critical", "Критично");
            Add("sev.Warning", "Warning", "Предупреждение");
            Add("sev.Info", "Info", "Сведения");
        }

        static void AddDrawCalls()
        {
            Add("DC_BUDGET", "Estimated draw calls per frame: {0} (budget {1}).", "Оценка draw calls за кадр: {0} (бюджет {1}).");
            Add("DC_BUDGET.fix", "Upper estimate without batching and culling. Merge materials, use static batching and GPU Instancing, cut shadow casters.",
                "Верхняя оценка без батчинга и отсечения. Объедините материалы, включите static batching и GPU Instancing, сократите тени.");
            Add("DC_SHADOW_CASTERS", "{0} renderers cast shadows; each is drawn again in {1} shadow passes.", "{0} рендереров отбрасывают тени; каждый рисуется ещё в {1} проходах теней.");
            Add("DC_SHADOW_CASTERS.fix", "Turn Cast Shadows off on small and distant objects.", "Выключите Cast Shadows у мелких и дальних объектов.");
            Add("DC_MANY_MATERIALS", "{0} materials on one renderer: each is a separate draw call.", "{0} материалов на одном рендерере: каждый — отдельный draw call.");
            Add("DC_MANY_MATERIALS.fix", "Merge materials into an atlas or split the mesh.", "Объедините материалы в атлас или разделите меш.");
            Add("DC_MULTI_MATERIAL", "{0} materials on the renderer.", "{0} материала на рендерере.");
            Add("DC_MULTI_MATERIAL.fix", "Each material adds a draw call; merge them where possible.", "Каждый материал добавляет draw call; объедините, где можно.");
            Add("DC_EXTRA_MATERIALS", "{0} materials for {1} submeshes: extra materials redraw the last submesh.", "{0} материалов на {1} submesh: лишние перерисовывают последний submesh.");
            Add("DC_EXTRA_MATERIALS.fix", "Remove the extra material slots.", "Уберите лишние слоты материалов.");
            Add("DC_STATIC_CANDIDATE", "Not Batching Static, and nothing on it or its parents moves it.", "Не Batching Static, и ничто на нём и его родителях его не двигает.");
            Add("DC_STATIC_CANDIDATE.fix", "Mark it Batching Static if it never moves. Static batching uses extra memory.", "Отметьте Batching Static, если объект не двигается. Static batching расходует память.");
            Add("DC_STATIC_CANDIDATE.do", "Add the Batching Static flag", "Добавить флаг Batching Static");
            Add("DC_INSTANCING_OFF", "GPU Instancing is off, the same mesh is drawn {0} times with this material.", "GPU Instancing выключен, один меш рисуется {0} раз с этим материалом.");
            Add("DC_INSTANCING_OFF.fix", "Enable GPU Instancing on the material.", "Включите GPU Instancing у материала.");
            Add("DC_INSTANCING_OFF.do", "Enable GPU Instancing on the material", "Включить GPU Instancing у материала");
            Add("DC_INSTANCING_UNKNOWN", "GPU Instancing is off, the same mesh is drawn {0} times with this material.", "GPU Instancing выключен, один меш рисуется {0} раз с этим материалом.");
            Add("DC_INSTANCING_UNKNOWN.fix", "Check that the shader supports instancing (#pragma multi_compile_instancing), then enable it.",
                "Проверьте, что шейдер поддерживает инстансинг (#pragma multi_compile_instancing), и включите его.");
        }

        static void AddMeshes()
        {
            Add("MESH_SCENE_TRIS", "Scene triangles (LOD0): {0} (budget {1}).", "Треугольников в сцене (LOD0): {0} (бюджет {1}).");
            Add("MESH_SCENE_TRIS.fix", "Upper estimate without culling. Add LODs, simplify meshes, bake details into normal maps.",
                "Верхняя оценка без отсечения. Добавьте LOD, упростите меши, перенесите детали в карты нормалей.");
            Add("MESH_NO_LOD", "{0} triangles without a LODGroup (limit {1}).", "{0} треугольников без LODGroup (порог {1}).");
            Add("MESH_NO_LOD.fix", "Add a LODGroup with simplified levels or reduce the mesh.", "Добавьте LODGroup с упрощёнными уровнями или упростите меш.");
            Add("MESH_READ_WRITE", "Read/Write is enabled: the mesh is kept twice in memory.", "Включён Read/Write: меш хранится в памяти дважды.");
            Add("MESH_READ_WRITE.fix", "Disable Read/Write unless scripts read the mesh at runtime.", "Выключите Read/Write, если скрипты не читают меш в рантайме.");
            Add("MESH_READ_WRITE.do", "Disable Read/Write in the model import settings", "Выключить Read/Write в импорте модели");
            Add("MESH_READ_WRITE_NEEDED", "Read/Write is enabled; the mesh is used by a MeshCollider or ParticleSystem.", "Включён Read/Write; меш используется MeshCollider или ParticleSystem.");
            Add("MESH_READ_WRITE_NEEDED.fix", "It may be needed (scaled colliders, particle meshes); disable only after checking.",
                "Возможно, нужен (масштабированные коллайдеры, меши частиц); выключайте только после проверки.");
            Add("MESH_NOT_OPTIMIZED", "Mesh optimization is not Everything ({0}).", "Оптимизация меша — не Everything ({0}).");
            Add("MESH_NOT_OPTIMIZED.fix", "Set Optimize Mesh to Everything in the model import settings.", "Поставьте Optimize Mesh = Everything в импорте модели.");
            Add("MESH_BONES_PER_VERTEX", "Up to {0} bones per vertex: skinning costs more.", "До {0} костей на вершину: скиннинг дороже.");
            Add("MESH_BONES_PER_VERTEX.fix", "Limit skin weights to 4 bones in the model import settings.", "Ограничьте веса 4 костями в импорте модели.");
            Add("MESH_QUALITY_SKIN_WEIGHTS", "Quality Skin Weights is {0} (more than four bones).", "Skin Weights уровня качества — {0} (больше четырёх костей).");
            Add("MESH_QUALITY_SKIN_WEIGHTS.fix", "Use Four Bones or fewer.", "Используйте Four Bones или меньше.");
            Add("MESH_GPU_SKINNING_OFF", "GPU Skinning is off: skinned meshes are processed on the CPU.", "GPU Skinning выключен: скиннинг идёт на CPU.");
            Add("MESH_GPU_SKINNING_OFF.fix", "Enable GPU Skinning in Player Settings.", "Включите GPU Skinning в Player Settings.");
        }

        static void AddTextures()
        {
            Add("TEX_SIZE", "Texture is {0} px (limit {1}).", "Текстура {0} px (порог {1}).");
            Add("TEX_SIZE.fix", "Lower Max Size for Standalone or split the texture.", "Уменьшите Max Size для Standalone или разделите текстуру.");
            Add("TEX_NOT_NORMAL_MAP", "Used as a normal map but imported as another texture type.", "Используется как карта нормалей, но импортирована другим типом.");
            Add("TEX_NOT_NORMAL_MAP.fix", "Set Texture Type to Normal map.", "Поставьте Texture Type = Normal map.");
            Add("TEX_NOT_NORMAL_MAP.do", "Set Texture Type to Normal map", "Поставить Texture Type = Normal map");
            Add("TEX_NO_MIPMAPS", "No mipmaps: aliasing and shimmering in VR.", "Нет mipmaps: лесенка и мерцание в VR.");
            Add("TEX_NO_MIPMAPS.fix", "Enable Generate Mip Maps.", "Включите Generate Mip Maps.");
            Add("TEX_NO_MIPMAPS.do", "Enable Generate Mip Maps", "Включить Generate Mip Maps");
            Add("TEX_UNCOMPRESSED", "Uncompressed format ({0}).", "Несжатый формат ({0}).");
            Add("TEX_UNCOMPRESSED.fix", "Use {1}: DXT1 without alpha, BC7 or DXT5 with alpha, BC6H for HDR, BC5 for normal maps.",
                "Используйте {1}: DXT1 без альфы, BC7 или DXT5 с альфой, BC6H для HDR, BC5 для нормалей.");
            Add("TEX_UNCOMPRESSED.do", "Compress the texture ({1} when the format is overridden)", "Сжать текстуру ({1}, если формат задан для Standalone)");
            Add("TEX_READ_WRITE", "Read/Write is enabled: the texture is kept twice in memory.", "Включён Read/Write: текстура хранится в памяти дважды.");
            Add("TEX_READ_WRITE.fix", "Disable Read/Write unless scripts read the pixels.", "Выключите Read/Write, если скрипты не читают пиксели.");
            Add("TEX_READ_WRITE.do", "Disable Read/Write in the texture import settings", "Выключить Read/Write в импорте текстуры");
            Add("TEX_MEMORY", "Estimated texture memory: {0} MB (budget {1} MB).", "Оценка памяти текстур: {0} МБ (бюджет {1} МБ).");
            Add("TEX_MEMORY.fix", "Compress textures, lower Max Size, remove unused ones.", "Сожмите текстуры, уменьшите Max Size, уберите лишние.");
            Add("TEX_ANISO_DISABLED", "Anisotropic filtering is disabled for the quality level.", "Анизотропная фильтрация выключена на уровне качества.");
            Add("TEX_ANISO_DISABLED.fix", "Floors and walls shimmer at grazing angles in VR; use Per Texture or Forced On.",
                "Полы и стены мерцают под углом в VR; поставьте Per Texture или Forced On.");
            Add("TEX_NOT_TRILINEAR", "{0} textures with mipmaps use Point or Bilinear filtering.", "{0} текстур с mipmaps используют фильтрацию Point или Bilinear.");
            Add("TEX_NOT_TRILINEAR.fix", "Trilinear blends mip levels smoothly and reduces shimmering in VR.", "Trilinear плавно смешивает уровни mip и снижает мерцание в VR.");
        }

        static void AddLighting()
        {
            Add("LIGHT_REALTIME", "Realtime {0} light.", "Realtime-источник {0}.");
            Add("LIGHT_REALTIME.fix", "Bake it or set it to Mixed; every realtime light adds a pass per lit object.",
                "Запеките его или поставьте Mixed; каждый realtime-источник добавляет проход по освещённым объектам.");
            Add("LIGHT_SHADOWED", "{0} realtime or mixed lights cast shadows (limit {1}): {2}.", "{0} realtime/mixed источников с тенями (порог {1}): {2}.");
            Add("LIGHT_SHADOWED.fix", "Shadow maps are expensive at the Vive Pro resolution; keep one shadowed light.",
                "Карты теней дороги при разрешении Vive Pro; оставьте один источник с тенями.");
            Add("LIGHT_NO_UV1", "Contributes to GI with lightmaps, but mesh {0} has no UV1.", "Участвует в GI с lightmap, но у меша {0} нет UV1.");
            Add("LIGHT_NO_UV1.fix", "Enable Generate Lightmap UVs in the model import settings.", "Включите Generate Lightmap UVs в импорте модели.");
            Add("LIGHT_SHADOW_RESOLUTION", "Shadow resolution is {0}.", "Разрешение теней — {0}.");
            Add("LIGHT_SHADOW_RESOLUTION.fix", "Lower it to save GPU time and memory.", "Понизьте, чтобы сберечь время GPU и память.");
            Add("LIGHT_SHADOW_DISTANCE", "Shadow distance is {0} m (more than {1} m).", "Дальность теней {0} м (больше {1} м).");
            Add("LIGHT_SHADOW_DISTANCE.fix", "A shorter distance gives sharper and cheaper shadows.", "Меньшая дальность — чётче и дешевле тени.");
            Add("LIGHT_PIXEL_LIGHTS", "Pixel Light Count is {0}.", "Pixel Light Count = {0}.");
            Add("LIGHT_PIXEL_LIGHTS.fix", "In Forward each extra pixel light is another pass over every lit object; use 1.",
                "В Forward каждый лишний pixel light — ещё проход по освещённым объектам; поставьте 1.");
            Add("LIGHT_REALTIME_GI", "Realtime Global Illumination is on.", "Включён Realtime Global Illumination.");
            Add("LIGHT_REALTIME_GI.fix", "It costs CPU every frame; use baked GI if lighting does not change.", "Он тратит CPU каждый кадр; используйте запечённый GI, если свет не меняется.");
        }

        static void AddPhysics()
        {
            Add("PHYS_CONCAVE_DYNAMIC", "Non-convex MeshCollider under a non-kinematic Rigidbody: not supported.", "Невыпуклый MeshCollider под не-kinematic Rigidbody: не поддерживается.");
            Add("PHYS_CONCAVE_DYNAMIC.fix", "Enable Convex or use primitive colliders.", "Включите Convex или используйте примитивные коллайдеры.");
            Add("PHYS_CONCAVE_KINEMATIC", "Non-convex MeshCollider under a kinematic Rigidbody.", "Невыпуклый MeshCollider под kinematic Rigidbody.");
            Add("PHYS_CONCAVE_KINEMATIC.fix", "Allowed, but it breaks if the body becomes dynamic.", "Допустимо, но сломается, если тело станет динамическим.");
            Add("PHYS_RENDER_MESH_COLLIDER", "MeshCollider uses the render mesh: {0} triangles (more than {1}).", "MeshCollider на рендер-меше: {0} треугольников (больше {1}).");
            Add("PHYS_RENDER_MESH_COLLIDER.fix", "Use a simplified collision mesh or primitive colliders.", "Используйте упрощённый меш коллизии или примитивы.");
            Add("PHYS_SOLVER_ITERATIONS", "Default Solver Iterations is {0} (more than {1}).", "Default Solver Iterations = {0} (больше {1}).");
            Add("PHYS_SOLVER_ITERATIONS.fix", "Lower it if the scene has no complex joints.", "Понизьте, если в сцене нет сложных сочленений.");
            Add("PHYS_FIXED_STEP_SLOW", "Fixed Timestep is {0} s ({1} Hz), slower than the {2} Hz display.", "Fixed Timestep {0} с ({1} Гц) — реже, чем экран {2} Гц.");
            Add("PHYS_FIXED_STEP_SLOW.fix", "Held objects jitter; set Fixed Timestep to 1/90 (0.0111) or interpolate rigidbodies.",
                "Предметы в руках дрожат; поставьте Fixed Timestep 1/90 (0.0111) или интерполяцию Rigidbody.");
            Add("PHYS_FIXED_STEP_FAST", "Fixed Timestep is {0} s ({1} Hz), faster than the {2} Hz display.", "Fixed Timestep {0} с ({1} Гц) — чаще, чем экран {2} Гц.");
            Add("PHYS_FIXED_STEP_FAST.fix", "Extra physics steps cost CPU; 1/90 is enough for VR.", "Лишние шаги физики тратят CPU; для VR хватает 1/90.");
        }

        static void AddAnimators()
        {
            Add("ANIM_ALWAYS_ANIMATE", "Culling Mode is Always Animate.", "Culling Mode = Always Animate.");
            Add("ANIM_ALWAYS_ANIMATE.fix", "Use Cull Update Transforms. Cull Completely stops off-screen doors and platforms with colliders, Animation Events and IK.",
                "Поставьте Cull Update Transforms. Cull Completely останавливает за кадром двери и платформы с коллайдерами, Animation Events и IK.");
            Add("ANIM_ALWAYS_ANIMATE.do", "Set Culling Mode to Cull Update Transforms", "Поставить Culling Mode = Cull Update Transforms");
            Add("ANIM_NO_CONTROLLER", "Animator without a controller.", "Animator без контроллера.");
            Add("ANIM_NO_CONTROLLER.fix", "Remove it unless a script assigns the controller.", "Уберите, если контроллер не назначает скрипт.");
            Add("ANIM_ROOT_MOTION", "Apply Root Motion without NavMeshAgent, CharacterController, Rigidbody or OnAnimatorMove.",
                "Apply Root Motion без NavMeshAgent, CharacterController, Rigidbody и OnAnimatorMove.");
            Add("ANIM_ROOT_MOTION.fix", "Turn it off if the object is moved by other means.", "Выключите, если объект двигают иначе.");
            Add("ANIM_COUNT", "{0} active Animators (limit {1}).", "{0} активных Animator (порог {1}).");
            Add("ANIM_COUNT.fix", "Disable animators of distant or idle objects.", "Выключайте аниматоры дальних и неподвижных объектов.");
            Add("ANIM_UPDATE_OFFSCREEN", "Update When Offscreen is on.", "Включён Update When Offscreen.");
            Add("ANIM_UPDATE_OFFSCREEN.fix", "Turn it off; fix the bounds instead if the mesh disappears at the screen edge.",
                "Выключите; если меш пропадает у края экрана — поправьте bounds.");
            Add("ANIM_UPDATE_OFFSCREEN.do", "Turn Update When Offscreen off", "Выключить Update When Offscreen");
            Add("ANIM_HEAVY_SKIN", "Skinned mesh of {0} triangles under an Animator (limit {1}).", "SkinnedMesh на {0} треугольников под Animator (порог {1}).");
            Add("ANIM_HEAVY_SKIN.fix", "Add LODs or reduce the mesh.", "Добавьте LOD или упростите меш.");
            Add("ANIM_HEAVY_SKIN_CPU", "Skinned mesh of {0} triangles under an Animator (limit {1}), skinned on the CPU.",
                "SkinnedMesh на {0} треугольников под Animator (порог {1}), скиннинг на CPU.");
            Add("ANIM_HEAVY_SKIN_CPU.fix", "Enable GPU Skinning, add LODs or reduce the mesh.", "Включите GPU Skinning, добавьте LOD или упростите меш.");
        }

        static void AddAudio()
        {
            Add("AUDIO_DECOMPRESS_LONG", "Decompress On Load for a {0} s clip: it is unpacked into RAM.", "Decompress On Load у клипа {0} с: распаковывается в память.");
            Add("AUDIO_DECOMPRESS_LONG.fix", "Use Streaming for music and ambience, Compressed In Memory for medium sounds.",
                "Для музыки и эмбиента — Streaming, для средних звуков — Compressed In Memory.");
            Add("AUDIO_DECOMPRESS_LONG.do", "Set Load Type to Streaming (60 s and longer) or Compressed In Memory",
                "Поставить Load Type = Streaming (от 60 с) или Compressed In Memory");
            Add("AUDIO_PCM_LARGE", "PCM clip of {0} MB.", "PCM-клип на {0} МБ.");
            Add("AUDIO_PCM_LARGE.fix", "Use Vorbis (quality about 70%) or ADPCM for short effects.", "Используйте Vorbis (качество около 70 %) или ADPCM для коротких эффектов.");
            Add("AUDIO_PCM_LARGE.do", "Set Compression Format to Vorbis, quality 70%", "Поставить Compression Format = Vorbis, качество 70 %");
            Add("AUDIO_PRELOAD_LONG", "Preload Audio Data for a {0} s clip.", "Preload Audio Data у клипа {0} с.");
            Add("AUDIO_PRELOAD_LONG.fix", "Turn preloading off for long clips: they slow down scene loading.", "Выключите предзагрузку длинных клипов: они замедляют загрузку сцены.");
            Add("AUDIO_PRELOAD_LONG.do", "Turn Preload Audio Data off", "Выключить Preload Audio Data");
            Add("AUDIO_NO_CLIP", "AudioSource without a clip.", "AudioSource без клипа.");
            Add("AUDIO_NO_CLIP.fix", "Fine if a script assigns the clip; otherwise remove it.", "Нормально, если клип назначает скрипт; иначе уберите.");
            Add("AUDIO_2D_SOURCE", "2D sound (Spatial Blend 0).", "2D-звук (Spatial Blend = 0).");
            Add("AUDIO_2D_SOURCE.fix", "If it is a sound of the world, make it 3D for immersion; music, UI and voice-over are fine as 2D.",
                "Если это звук мира — сделайте 3D для погружения; музыка, UI и озвучка — нормально в 2D.");
            Add("AUDIO_2D_SOURCE.do", "Set Spatial Blend to 1 (3D)", "Поставить Spatial Blend = 1 (3D)");
            Add("AUDIO_SOURCE_COUNT", "{0} active AudioSources (limit {1}; real voices {2}).", "{0} активных AudioSource (порог {1}; реальных голосов {2}).");
            Add("AUDIO_SOURCE_COUNT.fix", "Mixing costs CPU at 90 FPS; pool sources and disable silent ones.", "Микширование тратит CPU при 90 FPS; переиспользуйте источники и выключайте молчащие.");
            Add("AUDIO_PRIORITY_ZERO", "{0} AudioSources have priority 0 (more than {1}).", "У {0} AudioSource приоритет 0 (больше {1}).");
            Add("AUDIO_PRIORITY_ZERO.fix", "Keep the highest priority for a few key sounds.", "Оставьте высший приоритет нескольким ключевым звукам.");
            Add("AUDIO_LISTENERS_MANY", "{0} active AudioListeners in the loaded scenes.", "{0} активных AudioListener в загруженных сценах.");
            Add("AUDIO_LISTENERS_MANY.fix", "Keep exactly one, on the VR camera.", "Оставьте ровно один — на VR-камере.");
            Add("AUDIO_NO_LISTENER", "No active AudioListener in the loaded scenes.", "В загруженных сценах нет активного AudioListener.");
            Add("AUDIO_NO_LISTENER.fix", "Fine if the VR rig is created at runtime; otherwise add one to the VR camera.",
                "Нормально, если VR-rig создаётся в рантайме; иначе добавьте его на VR-камеру.");
            Add("AUDIO_LISTENER_NO_CAMERA", "AudioListener is not on a camera object.", "AudioListener не на объекте камеры.");
            Add("AUDIO_LISTENER_NO_CAMERA.fix", "Put it on the VR camera so sound follows the head.", "Перенесите на VR-камеру, чтобы звук следовал за головой.");
            Add("AUDIO_DSP_BUFFER", "DSP Buffer Size is {0}.", "DSP Buffer Size = {0}.");
            Add("AUDIO_DSP_BUFFER.fix", "Good Latency is the balance for VR; Best Latency costs CPU.", "Для VR баланс — Good Latency; Best Latency тратит CPU.");
            Add("AUDIO_SPEAKER_MODE", "Default Speaker Mode is {0}.", "Default Speaker Mode = {0}.");
            Add("AUDIO_SPEAKER_MODE.fix", "Vive Pro headphones are stereo; use Stereo.", "Наушники Vive Pro — стерео; поставьте Stereo.");
            Add("AUDIO_NO_SPATIALIZER", "No spatializer plugin is selected.", "Не выбран spatializer.");
            Add("AUDIO_NO_SPATIALIZER.fix", "An HRTF spatializer improves sound localization in VR.", "HRTF-spatializer улучшает локализацию звука в VR.");
        }

        static void AddVr()
        {
            Add("VR_NOT_ENABLED", "VR is not enabled for Standalone.", "VR не включён для Standalone.");
            Add("VR_NOT_ENABLED.fix", "Enable Virtual Reality Supported with OpenVR, or an OpenVR loader in XR Plug-in Management.",
                "Включите Virtual Reality Supported с OpenVR или загрузчик OpenVR в XR Plug-in Management.");
            Add("VR_NO_OPENVR", "VR is enabled without OpenVR ({0}).", "VR включён без OpenVR ({0}).");
            Add("VR_NO_OPENVR.fix", "Add OpenVR: the Vive Pro runs through SteamVR.", "Добавьте OpenVR: Vive Pro работает через SteamVR.");
            Add("VR_BOTH_SYSTEMS", "Legacy VR and XR Plug-in Management are both enabled.", "Включены и legacy VR, и XR Plug-in Management.");
            Add("VR_BOTH_SYSTEMS.fix", "Keep only one of them.", "Оставьте только одну систему.");
            Add("VR_STEREO_MODE", "Stereo rendering mode is {0}.", "Режим stereo — {0}.");
            Add("VR_STEREO_MODE.fix", "Single Pass Instanced is recommended at the Vive Pro resolution; custom shaders need stereo instancing macros (UNITY_VERTEX_INPUT_INSTANCE_ID, UNITY_VERTEX_OUTPUT_STEREO).",
                "При разрешении Vive Pro рекомендуется Single Pass Instanced; свои шейдеры должны поддерживать stereo instancing (UNITY_VERTEX_INPUT_INSTANCE_ID, UNITY_VERTEX_OUTPUT_STEREO).");
            Add("VR_DEFERRED_TIER", "Standalone graphics tier uses the Deferred rendering path.", "Графический уровень Standalone использует Deferred.");
            Add("VR_DEFERRED_TIER.fix", "Use Forward: MSAA does not work in Deferred and stereo Deferred is expensive.", "Используйте Forward: в Deferred не работает MSAA, а stereo в Deferred дорого.");
            Add("VR_DEFERRED_CAMERA", "VR camera uses the Deferred rendering path.", "VR-камера использует Deferred.");
            Add("VR_DEFERRED_CAMERA.fix", "Use Forward: MSAA does not work in Deferred.", "Используйте Forward: в Deferred не работает MSAA.");
            Add("VR_MSAA", "Anti Aliasing is {0}x.", "Anti Aliasing = {0}x.");
            Add("VR_MSAA.fix", "Use 4x MSAA or more: 2x is not enough at the Vive Pro resolution.", "Поставьте 4x MSAA и выше: 2x мало для разрешения Vive Pro.");
            Add("VR_NO_CAMERA", "No active VR camera (Target Eye set, no Target Texture).", "Нет активной VR-камеры (Target Eye задан, без Target Texture).");
            Add("VR_NO_CAMERA.fix", "Fine if the VR rig is created at runtime.", "Нормально, если VR-rig создаётся в рантайме.");
            Add("VR_CAMERA_NO_MSAA", "MSAA is off on the VR camera.", "У VR-камеры выключен MSAA.");
            Add("VR_CAMERA_NO_MSAA.fix", "Enable Allow MSAA.", "Включите Allow MSAA.");
            Add("VR_CAMERA_HDR", "HDR is on for the VR camera.", "У VR-камеры включён HDR.");
            Add("VR_CAMERA_HDR.fix", "HDR doubles render target memory and bandwidth for both eyes; turn it off unless post-processing needs it.",
                "HDR удваивает память и пропускную способность целей рендера на оба глаза; выключите, если он не нужен постобработке.");
            Add("VR_FAR_CLIP", "Far clip plane is {0} (more than {1}).", "Дальняя плоскость отсечения {0} (больше {1}).");
            Add("VR_FAR_CLIP.fix", "Use 500 or less for better depth precision.", "Поставьте 500 и меньше для точности глубины.");
            Add("VR_NEAR_CLIP", "Near clip plane is {0} (less than {1}).", "Ближняя плоскость отсечения {0} (меньше {1}).");
            Add("VR_NEAR_CLIP.fix", "Z-fighting is very visible in VR; use 0.01 or more.", "Z-fighting очень заметен в VR; поставьте 0.01 и больше.");
            Add("VR_IMAGE_EFFECT", "Image effect script {0} on the VR camera (OnRenderImage).", "Скрипт эффекта {0} на VR-камере (OnRenderImage).");
            Add("VR_IMAGE_EFFECT.fix", "Full-screen effects run for both eyes at a high resolution; remove or simplify.",
                "Полноэкранные эффекты идут на оба глаза в высоком разрешении; уберите или упростите.");
            Add("VR_CANVAS_OVERLAY", "Canvas in Screen Space Overlay: not visible in the headset.", "Canvas в Screen Space Overlay: не виден в шлеме.");
            Add("VR_CANVAS_OVERLAY.fix", "Switch to World Space, scale about 0.001, set the event camera and use a VR pointer.",
                "Переведите в World Space, масштаб около 0.001, назначьте камеру событий и VR-указатель.");
            Add("VR_CANVAS_CAMERA", "Canvas in Screen Space Camera: sticks to the head.", "Canvas в Screen Space Camera: прилипает к голове.");
            Add("VR_CANVAS_CAMERA.fix", "Use World Space for comfort.", "Для комфорта используйте World Space.");
            Add("VR_PP_LAYER", "Post Process Layer on a camera.", "Post Process Layer на камере.");
            Add("VR_PP_LAYER.fix", "Post-processing runs for both eyes at a high resolution; keep only cheap effects.",
                "Постобработка идёт на оба глаза в высоком разрешении; оставьте только дешёвые эффекты.");
            Add("VR_PP_EFFECTS", "Post Process Volume with effects: {0}. Expensive in VR: {1}.", "Post Process Volume с эффектами: {0}. Дорогие в VR: {1}.");
            Add("VR_PP_EFFECTS.fix", "Turn off Motion Blur, Depth of Field, Chromatic Aberration, SSR and Ambient Occlusion.",
                "Выключите Motion Blur, Depth of Field, Chromatic Aberration, SSR и Ambient Occlusion.");
            Add("VR_STEAMVR_MISSING", "SteamVR Plugin not found in the project.", "SteamVR Plugin не найден в проекте.");
            Add("VR_STEAMVR_MISSING.fix", "Fine if the project uses another rig (for example XR Interaction Toolkit).", "Нормально, если в проекте другой rig (например, XR Interaction Toolkit).");
            Add("VR_STEAMVR_NOT_IN_SCENE", "SteamVR Plugin is in the project, but the scenes have no SteamVR rig components.", "SteamVR Plugin есть в проекте, но в сценах нет компонентов его rig.");
            Add("VR_STEAMVR_NOT_IN_SCENE.fix", "Fine if the rig is created at runtime or lives in another scene.", "Нормально, если rig создаётся в рантайме или лежит в другой сцене.");
            Add("VR_PROBE_EVERY_FRAME", "Realtime Reflection Probe refreshed every frame.", "Realtime Reflection Probe обновляется каждый кадр.");
            Add("VR_PROBE_EVERY_FRAME.fix", "Bake it, or refresh it on awake or via scripting.", "Запеките или обновляйте при старте или из скрипта.");
            Add("VR_PROBE_REALTIME", "Realtime Reflection Probe.", "Realtime Reflection Probe.");
            Add("VR_PROBE_REALTIME.fix", "Baked probes are preferred in VR.", "В VR лучше запечённые пробы.");
            Add("VR_NO_OCCLUSION", "{0} static renderers, but no occlusion culling data is baked.", "{0} статичных рендереров, но occlusion culling не запечён.");
            Add("VR_NO_OCCLUSION.fix", "Bake occlusion culling (Window > Rendering > Occlusion Culling).", "Запеките occlusion culling (Window > Rendering > Occlusion Culling).");
        }

        static void AddGeneral()
        {
            Add("GEN_EMPTY_LEAF", "Empty object without components or children.", "Пустой объект без компонентов и детей.");
            Add("GEN_EMPTY_LEAF.fix", "Remove it unless it is a marker used by scripts.", "Уберите, если это не метка для скриптов.");
            Add("GEN_NEGATIVE_SCALE", "Negative scale.", "Отрицательный масштаб.");
            Add("GEN_NEGATIVE_SCALE.fix", "It breaks dynamic batching and flips the face winding; mirror the mesh in the model instead.",
                "Ломает dynamic batching и переворачивает обход граней; отзеркальте меш в модели.");
            Add("GEN_DEEP_HIERARCHY", "Hierarchy deeper than {0} levels.", "Иерархия глубже {0} уровней.");
            Add("GEN_DEEP_HIERARCHY.fix", "Deep hierarchies make transform updates more expensive for moving objects.", "Глубокая иерархия удорожает пересчёт Transform у движущихся объектов.");
            Add("GEN_MISSING_SCRIPTS", "{0} missing scripts.", "Пропавших скриптов: {0}.");
            Add("GEN_MISSING_SCRIPTS.fix", "Remove the missing script components.", "Удалите компоненты пропавших скриптов.");
            Add("GEN_INACTIVE_HEAVY_MESH", "Inactive object with a mesh of {0} triangles.", "Неактивный объект с мешем на {0} треугольников.");
            Add("GEN_INACTIVE_HEAVY_MESH.fix", "It is still loaded with the scene; load it on demand if it is rarely used.",
                "Он всё равно грузится со сценой; если нужен редко — загружайте по требованию.");
            Add("GEN_INACTIVE_HEAVY_TEXTURE", "Inactive object with a {0} px texture.", "Неактивный объект с текстурой {0} px.");
            Add("GEN_INACTIVE_HEAVY_TEXTURE.fix", "It is still loaded with the scene; load it on demand if it is rarely used.",
                "Она всё равно грузится со сценой; если нужна редко — загружайте по требованию.");
        }
    }
}
