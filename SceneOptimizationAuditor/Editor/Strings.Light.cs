namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        static void AddLight()
        {
            // Lighting checks: titles, messages, how to fix.
            Add("LIGHT_LIGHTMAP_MEMORY.title", "Lightmaps take much memory", "Lightmaps занимают много памяти");
            Add("LIGHT_LIGHTMAP_MEMORY", "Lightmaps take {0} MB (more than {1} MB).", "Lightmaps занимают {0} МБ (больше {1} МБ).");
            Add("LIGHT_LIGHTMAP_MEMORY.fix",
                "Lower Lightmap Resolution or Lightmap Size, turn off the directional mode, move small objects to light probes (Lighting tab).",
                "Снизьте Lightmap Resolution или Lightmap Size, выключите направленный режим, переведите мелкие объекты на пробы (вкладка «Свет»).");
            Add("LIGHT_LIGHTMAP_UNCOMPRESSED.title", "Lightmaps not compressed", "Lightmaps без сжатия");
            Add("LIGHT_LIGHTMAP_UNCOMPRESSED", "Lightmaps are not compressed: {0} MB.", "Lightmaps без сжатия: {0} МБ.");
            Add("LIGHT_LIGHTMAP_UNCOMPRESSED.fix", "Turn on Compress Lightmaps in the Lighting window and rebake: several times less memory.",
                "Включите Compress Lightmaps в окне Lighting и перезапеките свет: памяти в несколько раз меньше.");
            Add("LIGHT_LIGHTMAP_DIRECTIONAL.title", "Directional lightmaps", "Направленные lightmaps");
            Add("LIGHT_LIGHTMAP_DIRECTIONAL", "Directional lightmaps: {0} MB more for the direction textures.", "Направленные lightmaps: ещё {0} МБ на текстуры направления.");
            Add("LIGHT_LIGHTMAP_DIRECTIONAL.fix", "Non-Directional halves the memory; Directional is needed only when baked light must show normal maps.",
                "Non-Directional вдвое экономит память; Directional нужен, только если запечённый свет должен показывать normal map.");
            Add("LIGHT_NOT_BAKED.title", "Lighting not baked", "Свет не запечён");
            Add("LIGHT_NOT_BAKED", "Lighting is not baked: {0} lightmapped objects without a lightmap, {1} Baked or Mixed lights without a bake.",
                "Свет не запечён: объектов для lightmap без lightmap — {0}, источников Baked или Mixed без запекания — {1}.");
            Add("LIGHT_NOT_BAKED.fix", "Bake the lighting in the Lighting window (Generate Lighting).", "Запеките свет в окне Lighting (Generate Lighting).");
            Add("LIGHT_SMALL_IN_LIGHTMAP.title", "Small object in a lightmap", "Мелкий объект в lightmap");
            Add("LIGHT_SMALL_IN_LIGHTMAP", "A {0} m object is baked into a lightmap.", "Объект {0} м запекается в lightmap.");
            Add("LIGHT_SMALL_IN_LIGHTMAP.fix", "Light from probes is cheaper and has no seams on small objects: set Receive GI to Light Probes and rebake.",
                "Мелким объектам свет из проб дешевле и без швов: Receive GI = Light Probes, затем перезапеките свет.");
            Add("LIGHT_SMALL_IN_LIGHTMAP.do", "Receive GI = Light Probes (rebake the lighting afterwards)", "Receive GI = Light Probes (затем перезапеките свет)");
            Add("LIGHT_LIGHTMAP_SHARE.title", "Object takes much of a lightmap", "Объект занимает много атласа");
            Add("LIGHT_LIGHTMAP_SHARE", "The object takes {0}% of lightmap #{1}.", "Объект занимает {0}% атласа #{1}.");
            Add("LIGHT_LIGHTMAP_SHARE.fix", "Lower Scale in Lightmap on the renderer (now {2}) or the Lightmap Resolution.",
                "Уменьшите Scale in Lightmap у рендерера (сейчас {2}) или Lightmap Resolution.");
            Add("LIGHT_NO_PROBES.title", "No light probes", "Нет Light Probe Group");
            Add("LIGHT_NO_PROBES", "No Light Probe Group: {0} objects lit by probes get only the ambient light.",
                "Нет Light Probe Group: {0} объектов на пробах получают только ambient.");
            Add("LIGHT_NO_PROBES.fix", "Add a Light Probe Group where the player walks and moving props are, then rebake.",
                "Добавьте Light Probe Group там, где ходит игрок и есть подвижные предметы, затем перезапеките свет.");
            Add("LIGHT_OUTSIDE_PROBES.title", "Object outside the light probes", "Объект вне области проб");
            Add("LIGHT_OUTSIDE_PROBES", "The object is outside the probe area: {0} m to the nearest light probe.", "Объект вне области проб: до ближайшей пробы {0} м.");
            Add("LIGHT_OUTSIDE_PROBES.fix", "Extend the Light Probe Group to where the object is.", "Расширьте Light Probe Group туда, где стоит объект.");
            Add("LIGHT_DISTANCE_SHADOWMASK.title", "Distance Shadowmask", "Distance Shadowmask");
            Add("LIGHT_DISTANCE_SHADOWMASK", "Distance Shadowmask with Mixed lights: static objects cast realtime shadows up to the Shadow Distance.",
                "Distance Shadowmask при Mixed-источниках: статика отбрасывает тени в реальном времени до Shadow Distance.");
            Add("LIGHT_DISTANCE_SHADOWMASK.fix", "Use Shadowmask in the Quality settings: baked shadows for static objects, realtime ones only for moving objects.",
                "Поставьте Shadowmask в Quality: тени статики — запечённые, в реальном времени — только у подвижных.");
            Add("LIGHT_SHADOW_CASCADES.title", "Four shadow cascades", "Четыре каскада теней");
            Add("LIGHT_SHADOW_CASCADES", "4 shadow cascades with a realtime directional shadow.", "4 каскада теней при directional-источнике с тенями в реальном времени.");
            Add("LIGHT_SHADOW_CASCADES.fix", "2 cascades are enough in VR at a short Shadow Distance; each cascade draws the shadow casters again.",
                "В VR при короткой Shadow Distance хватает 2 каскадов; каждый каскад заново рисует отбрасывающих тень.");
            Add("LIGHT_WIDE_REALTIME.title", "Realtime light reaching many objects", "Реалтайм-источник на много объектов");
            Add("LIGHT_WIDE_REALTIME", "Realtime {0} light reaches {1} objects (more than {2}).", "Источник {0} в реальном времени достаёт до {1} объектов (больше {2}).");
            Add("LIGHT_WIDE_REALTIME.fix", "Bake it or shorten its range: every lit object gets an extra pass.", "Запеките его или уменьшите дальность: каждый освещённый объект получает лишний проход.");
            Add("LIGHT_IMPORTANT.title", "Light with Render Mode Important", "Источник с Render Mode Important");
            Add("LIGHT_IMPORTANT", "{0} light with Render Mode Important: per-pixel on every object in range, beyond the Pixel Light Count.",
                "Источник {0} с Render Mode Important: per-pixel на каждом объекте в зоне, сверх Pixel Light Count.");
            Add("LIGHT_IMPORTANT.fix", "Set Render Mode to Auto or bake the light.", "Поставьте Render Mode = Auto или запеките источник.");

            // Budgets and History tabs.
            Add("ui.b.lightmapMem", "Lightmap memory, MB", "Память lightmaps, МБ");
            Add("ui.h.lightmapMem", "Lightmap memory, MB", "Память lightmaps, МБ");

            // Lighting tab.
            Add("ui.tab.light", "Lighting", "Свет");
            Add("ui.lt.intro",
                "How much memory lightmaps take and who takes it, what is not baked, where objects lack light probes, which realtime lights reach many objects, what the reflection probes take. The scene is not changed.",
                "Сколько памяти занимают lightmaps и кто их занимает, что не запечено, где объектам не хватает проб, какие источники в реальном времени светят на много объектов, сколько берут reflection probes. Сцена не меняется.");
            Add("ui.lt.check", "Check Lighting", "Проверить свет");
            Add("ui.lt.check.tip", "Collects the lightmaps, the objects in them, the lights, the light and reflection probes of the loaded scenes. Nothing is changed.",
                "Собирает атласы lightmap, объекты в них, источники, пробы света и reflection probes загруженных сцен. Ничего не меняет.");
            Add("ui.lt.toProbesAll", "Small Ones to Probes ({0})", "Перевести мелкие на пробы ({0})");
            Add("ui.lt.openLighting", "Open Lighting", "Открыть Lighting");
            Add("ui.lighting.tip", "Open the Lighting Settings window", "Открыть окно Lighting Settings");
            Add("ui.lt.openLighting.tip", "The Lighting Settings window: bake settings and Generate Lighting.", "Окно Lighting Settings: настройки и запекание (Generate Lighting).");
            Add("ui.lt.onlyMarked", "Only marked", "Только с пометками");
            Add("ui.lt.emptyTitle", "Lighting not checked yet", "Свет ещё не проверен");
            Add("ui.lt.emptyText", "The tool collects the lightmap atlases, the objects in them, the lights, the light and reflection probes of the loaded scenes. Nothing is changed.",
                "Утилита соберёт атласы lightmap, объекты в них, источники, пробы света и reflection probes загруженных сцен. Ничего не меняет.");
            Add("ui.lt.seg.atlases", "Atlases", "Атласы");
            Add("ui.lt.seg.objects", "Objects", "Объекты");
            Add("ui.lt.seg.lights", "Lights", "Источники");
            Add("ui.lt.seg.probes", "Probes", "Пробы");
            Add("ui.lt.seg.tip", "What the tab shows.", "Что показывает вкладка.");

            // Atlases.
            Add("ui.lt.atlases.summary", "Lightmaps: {0} atlases · {1} of {2} MB", "Lightmaps: {0} атласов · {1} из {2} МБ");
            Add("ui.lt.atlases.none", "Lightmaps: none", "Lightmaps: нет");
            Add("ui.lt.atlases.empty", "No lightmaps: lighting is not baked or nothing in the scene is lightmapped. Bake it in the Lighting window.",
                "Lightmaps нет: свет не запечён или в сцене нет объектов для lightmap. Запеките свет в окне Lighting.");
            Add("ui.lt.set.size", "Lightmap Size {0}", "Lightmap Size {0}");
            Add("ui.lt.set.resolution", "{0} texels per m", "{0} текселей на м");
            Add("ui.lt.set.directional", "directional (memory ×2)", "направленные (память ×2)");
            Add("ui.lt.set.nonDirectional", "non-directional", "ненаправленные");
            Add("ui.lt.set.compressed", "compressed", "сжатие включено");
            Add("ui.lt.set.uncompressed", "not compressed", "без сжатия");
            Add("ui.lt.set.mixed", "Mixed: {0}", "Mixed: {0}");
            Add("ui.lt.set.shadowmask", "Quality: {0}", "Quality: {0}");
            Add("ui.lt.set.noBakedGI", "Baked Global Illumination is off", "Baked Global Illumination выключен");
            Add("ui.lt.memory", "Lightmap memory", "Память lightmaps");
            Add("ui.lt.col.atlas", "Atlas", "Атлас");
            Add("ui.lt.col.atlas.tip", "Click to select the objects of the atlas.", "Клик — выделить объекты атласа.");
            Add("ui.lt.col.size", "Size", "Размер");
            Add("ui.lt.col.format", "Format", "Формат");
            Add("ui.lt.col.mb", "MB", "МБ");
            Add("ui.lt.col.mb.tip", "Color + direction + shadowmask, estimated like textures on the Budgets tab.", "Цвет + направление + shadowmask, оценка как у текстур на «Бюджетах».");
            Add("ui.lt.col.count", "Objects", "Объектов");
            Add("ui.lt.col.filled", "Filled", "Заполнено");
            Add("ui.lt.col.filled.short", "Filled", "Заполн.");
            Add("ui.lt.col.filled.tip", "Sum of the shares of its objects.", "Сумма долей его объектов.");
            Add("ui.lt.atlases.footer",
                "MB is estimated as for textures on the Budgets tab: color + direction + shadowmask. Click an atlas to select its objects. Lighting is baked in the Lighting window.",
                "МБ — оценка как у текстур на «Бюджетах»: цвет + направление + shadowmask. Клик по атласу — выделить его объекты. Свет запекается в окне Lighting.");

            // Objects.
            Add("ui.lt.objects.summary", "In lightmaps: {0} objects · small: {1} · much of an atlas: {2} · no lightmap: {3}",
                "В lightmap: {0} объектов · мелких: {1} · много атласа: {2} · без lightmap: {3}");
            Add("ui.lt.objects.sub", "Share — the part of the atlas the object's UVs take. Small — under {0} m on its largest side: light from probes is cheaper and has no seams.",
                "Доля — часть атласа под развёртку объекта. Мелкий — меньше {0} м по большей стороне: свет из проб дешевле и без швов.");
            Add("ui.lt.objects.none", "Nothing in the loaded scenes is baked into lightmaps (Contribute GI with Receive GI = Lightmaps).",
                "В загруженных сценах ничего не запекается в lightmap (Contribute GI и Receive GI = Lightmaps).");
            Add("ui.lt.objects.allGood", "No objects with marks.", "Объектов с пометками нет.");
            Add("ui.lt.col.object", "Object", "Объект");
            Add("ui.lt.col.share", "Share", "Доля");
            Add("ui.lt.col.share.tip", "Part of the atlas the object's lightmap UVs take (lightmapScaleOffset x × y).", "Часть атласа под развёртку объекта (lightmapScaleOffset x × y).");
            Add("ui.lt.col.scale", "Scale", "Scale");
            Add("ui.lt.col.scale.tip", "Scale in Lightmap of the renderer.", "Scale in Lightmap у рендерера.");
            Add("ui.lt.col.meters", "Size, m", "Размер, м");
            Add("ui.lt.col.meters.short", "Size", "Разм.");
            Add("ui.lt.col.meters.tip", "Largest side of the bounds.", "Большая сторона bounds.");
            Add("ui.lt.mark.small", "small — use probes", "мелкий — на пробы");
            Add("ui.lt.mark.much", "much of the atlas", "много атласа");
            Add("ui.lt.mark.noLightmap", "no lightmap", "без lightmap");
            Add("ui.lt.toProbes", "To Probes", "На пробы");
            Add("ui.lt.toProbes.tip", "Receive GI = Light Probes; Contribute GI stays. Rebake the lighting afterwards.", "Receive GI = Light Probes; Contribute GI остаётся. Затем перезапеките свет.");
            Add("ui.lt.sel.count", "Objects selected: {0}", "Выбрано объектов: {0}");
            Add("ui.lt.sel.toProbes", "To Probes ({0})", "Перевести на пробы ({0})");
            Add("ui.lt.sel.select", "Select in Unity ({0})", "Выделить в Unity ({0})");
            Add("ui.lt.objects.footer", "\"To Probes\" sets Receive GI to Light Probes, Contribute GI stays; Ctrl+Z brings it back. Rebake the lighting afterwards.",
                "«На пробы» — Receive GI = Light Probes, Contribute GI остаётся; Ctrl+Z возвращает. После перевода свет надо перезапечь.");

            // Lights.
            Add("ui.lt.lights.summary", "Lights: {0} · realtime: {1} · with shadows: {2} · wide: {3}", "Источников: {0} · в реальном времени: {1} · с тенями: {2} · широких: {3}");
            Add("ui.lt.lights.sub", "Pixel Light Count: {0} · Important: {1} · Baked and Mixed not baked: {2} · in range — objects whose bounds meet the range sphere",
                "Pixel Light Count: {0} · Important: {1} · Baked и Mixed без запекания: {2} · в зоне — объекты, чьи bounds пересекают сферу дальности");
            Add("ui.lt.lights.none", "No active lights in the loaded scenes.", "В загруженных сценах нет включённых источников.");
            Add("ui.lt.lights.allGood", "No lights with marks.", "Источников с пометками нет.");
            Add("ui.lt.col.light", "Light", "Источник");
            Add("ui.lt.col.type", "Type", "Тип");
            Add("ui.lt.col.mode", "Mode", "Режим");
            Add("ui.lt.col.shadows", "Shadows", "Тени");
            Add("ui.lt.col.range", "Range, m", "Дальн., м");
            Add("ui.lt.col.inRange", "In range", "В зоне");
            Add("ui.lt.col.inRange.tip", "Objects whose bounds meet the range sphere (a spot light is counted by the same sphere); all for a directional light.",
                "Объекты, чьи bounds пересекают сферу дальности (spot — по той же сфере); у directional — все.");
            Add("ui.lt.col.baked", "Baked", "Запечён");
            Add("ui.lt.col.baked.short", "Baked", "Запеч.");
            Add("ui.lt.yes", "yes", "да");
            Add("ui.lt.no", "no", "нет");
            Add("ui.lt.shadows.None", "none", "нет");
            Add("ui.lt.shadows.Hard", "hard", "жёсткие");
            Add("ui.lt.shadows.Soft", "soft", "мягкие");
            Add("ui.lt.mark.wide", "wide", "широкий");
            Add("ui.lt.mark.important", "Important", "Important");
            Add("ui.lt.mark.notBaked", "not baked", "не запечён");
            Add("ui.lt.lights.footer",
                "Wide — a realtime point or spot reaching more than {0} objects: each one is an extra pass. Important lights ignore the Pixel Light Count.",
                "Широкий — point или spot в реальном времени, в зоне больше {0} объектов: каждый — лишний проход. Important не считается в Pixel Light Count.");

            // Probes.
            Add("ui.lt.probes.summary", "Probes: {0} in {1} groups · objects on probes: {2} · outside the probes: {3}",
                "Проб: {0} в {1} группах · объектов на пробах: {2} · вне проб: {3}");
            Add("ui.lt.probes.sub", "Probe area — the box of all probes + {0} m; an object outside it takes the light of the edge probes. On probes — moving objects and Receive GI = Light Probes.",
                "Область проб — коробка всех проб + {0} м; объект вне неё берёт свет крайних проб. На пробах — подвижные объекты и Receive GI = Light Probes.");
            Add("ui.lt.probes.noGroups", "No Light Probe Groups: objects on probes get only the ambient light.", "Light Probe Group нет: объекты на пробах получают только ambient.");
            Add("ui.lt.groups", "Light Probe Groups", "Light Probe Groups");
            Add("ui.lt.col.group", "Group", "Группа");
            Add("ui.lt.col.probes", "Probes", "Проб");
            Add("ui.lt.col.box", "Size, m", "Размер, м");
            Add("ui.lt.outside", "Objects on probes outside the probe area", "Объекты на пробах вне области проб");
            Add("ui.lt.outside.none", "Every object on probes is inside the probe area.", "Все объекты на пробах — внутри области проб.");
            Add("ui.lt.col.usage", "Light Probes", "Light Probes");
            Add("ui.lt.col.nearest", "To a probe, m", "До пробы, м");
            Add("ui.lt.probes.footer", "Without probes an object on probes gets only the ambient light. Put a Light Probe Group where the player walks and loose props lie.",
                "Без проб объект на пробах освещён только ambient. Поставьте Light Probe Group там, где ходит игрок и лежат подвижные предметы.");
        }
    }
}
