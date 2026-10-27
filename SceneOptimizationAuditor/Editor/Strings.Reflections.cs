namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        static void AddReflections()
        {
            // Reflection probe checks: titles, messages, how to fix.
            Add("REFL_QUALITY_REALTIME.title", "Realtime reflection probes allowed", "Realtime-пробы разрешены в Quality");
            Add("REFL_QUALITY_REALTIME", "Realtime Reflection Probes are on in the quality level.", "На уровне качества включены Realtime Reflection Probes.");
            Add("REFL_QUALITY_REALTIME.fix", "Bake the probes and turn Realtime Reflection Probes off in Quality: a realtime probe that updates every frame or from a script renders its six faces again while the game runs.",
                "Запеките пробы и выключите Realtime Reflection Probes в Quality: realtime-проба с обновлением каждый кадр или из скрипта снова рисует свои шесть граней во время игры.");
            Add("REFL_RESOLUTION.title", "High reflection probe resolution", "Большое разрешение reflection probe");
            Add("REFL_RESOLUTION", "Reflection probe of {0} × {0} (more than {1}): {2} MB.", "Reflection probe {0} × {0} (больше {1}): {2} МБ.");
            Add("REFL_RESOLUTION.fix", "128 or 256 is enough for most reflections: the cubemap is blurred by roughness anyway. Lower Resolution (and rebake a baked probe).",
                "Большинству отражений хватает 128 или 256: кубическая текстура всё равно размывается шероховатостью. Снизьте Resolution (запечённую пробу — перезапеките).");
            Add("REFL_MEMORY.title", "Reflection probes take much memory", "Reflection probes занимают много памяти");
            Add("REFL_MEMORY", "Reflection probes take {0} MB (more than {1} MB) in {2} probes.", "Reflection probes занимают {0} МБ (больше {1} МБ), проб: {2}.");
            Add("REFL_MEMORY.fix", "Lower the resolution of the large probes and remove duplicates (Lighting tab, Reflections).",
                "Снизьте разрешение больших проб и уберите дубликаты (вкладка «Свет», «Отражения»).");
            Add("REFL_NOT_BAKED.title", "Reflection probe not baked", "Reflection probe не запечена");
            Add("REFL_NOT_BAKED", "{0} reflection probe without its cubemap: the objects reflect only the environment (the sky by default).",
                "Reflection probe {0} без кубической текстуры: объекты отражают только окружение (по умолчанию — небо).");
            Add("REFL_NOT_BAKED.fix", "Bake it: Bake on the probe or Generate Lighting in the Lighting window; a Custom probe needs its cubemap.",
                "Запеките: Bake у пробы или Generate Lighting в окне Lighting; пробе Custom нужна своя кубическая текстура.");
            Add("REFL_NO_BOX.title", "Reflection probe without box projection", "Reflection probe без box projection");
            Add("REFL_NO_BOX", "A {0} m reflection probe without Box Projection: indoors the reflection swims as the head moves.",
                "Reflection probe {0} м без Box Projection: в помещении отражение плывёт при движении головы.");
            Add("REFL_NO_BOX.fix", "Turn on Box Projection and fit the box to the room's walls; outdoors it is not needed.",
                "Включите Box Projection и подгоните коробку пробы к стенам комнаты; на улице он не нужен.");
            Add("REFL_NO_BOX.do", "Turn on Box Projection", "Включить Box Projection");
            Add("REFL_DUPLICATE.title", "Reflection probes overlap", "Reflection probes перекрываются");
            Add("REFL_DUPLICATE", "The probe nearly repeats {0} ({1}% of it is shared): one more cubemap for the same place.",
                "Проба почти повторяет {0} (общая часть — {1}%): ещё одна кубическая текстура для того же места.");
            Add("REFL_DUPLICATE.fix", "Remove one of them. A small probe inside a large one (a room in the street) is fine and is not reported.",
                "Уберите одну из них. Малая проба внутри большой (комната на улице) — нормально, о ней не сообщается.");
            Add("REFL_OUTSIDE.title", "Objects outside the reflection probes", "Объекты вне reflection probes");
            Add("REFL_OUTSIDE", "{0} objects with Reflection Probes on stand outside all {1} probes: they reflect only the environment (the sky by default).",
                "{0} объектов с Reflection Probes стоят вне всех проб (всего проб: {1}): они отражают только окружение (по умолчанию — небо).");
            Add("REFL_OUTSIDE.fix", "Outdoors that is right. Indoors, grow the room's probe to cover them (Lighting tab, Reflections lists them, nearest first).",
                "На улице так и надо. В помещении расширьте пробу комнаты, чтобы она их накрыла (вкладка «Свет», «Отражения» — список, ближние первыми).");

            // Lighting tab, Reflections.
            Add("ui.lt.seg.reflections", "Reflections", "Отражения");
            Add("ui.lt.refl.summary", "Probes: {0}  ·  {1} MB  ·  no box projection: {2}  ·  not baked: {3}  ·  objects outside: {4}",
                "Проб: {0}  ·  {1} МБ  ·  без box projection: {2}  ·  не запечено: {3}  ·  объектов вне проб: {4}");
            Add("ui.lt.refl.none", "Reflection probes: none", "Reflection probes: нет");
            Add("ui.lt.refl.sub",
                "MB — the cubemap: 6 faces with mips, estimated from the resolution when not baked or realtime (≈). No box — a probe up to {0} m on its largest side without Box Projection. Objects — mesh renderers with the center of their bounds inside.",
                "МБ — кубическая текстура: 6 граней с mip, у незапечённой и realtime — оценка по разрешению (≈). Без box — проба до {0} м по большей стороне без Box Projection. Объектов — меш-рендереры, у которых центр границ внутри.");
            Add("ui.lt.refl.footer",
                "An object reflects the probes it stands in; outside them, the environment (the sky by default). In a room without Box Projection the reflection swims as the head moves. Click a probe to select it.",
                "Объект отражает пробы, внутри которых стоит; вне проб — окружение (по умолчанию — небо). В комнате без Box Projection отражение плывёт при движении головы. Клик по пробе — выделить её.");
            Add("ui.lt.refl.empty", "No reflection probes: objects reflect only the environment (the sky by default). Indoors, put a Reflection Probe in each room and bake it.",
                "Reflection probes нет: объекты отражают только окружение (по умолчанию — небо). В помещениях поставьте Reflection Probe на комнату и запеките.");
            Add("ui.lt.refl.allGood", "No marked probes.", "Проб с пометками нет.");
            Add("ui.lt.col.probe", "Probe", "Проба");
            Add("ui.lt.col.resolution", "Resolution", "Разрешение");
            Add("ui.lt.col.resolution.short", "Res.", "Разр.");
            Add("ui.lt.col.resolution.tip", "Resolution of each face of the cubemap.", "Разрешение каждой грани кубической текстуры.");
            Add("ui.lt.col.hdr", "HDR", "HDR");
            Add("ui.lt.col.reflMb.tip", "Memory of the cubemap with its mips; ≈ — estimated from the resolution.", "Память кубической текстуры с mip; ≈ — оценка по разрешению.");
            Add("ui.lt.col.reflSize.tip", "Size of the probe's box in the world.", "Размер коробки пробы в мире.");
            Add("ui.lt.col.boxProjection", "Box", "Box");
            Add("ui.lt.col.boxProjection.tip", "Box Projection: the reflection is projected onto the probe's box, as on the walls of a room.",
                "Box Projection: отражение проецируется на коробку пробы, как на стены комнаты.");
            Add("ui.lt.col.objects", "Objects", "Объектов");
            Add("ui.lt.col.objects.short", "Obj.", "Объект.");
            Add("ui.lt.col.objects.tip", "Mesh and skinned mesh renderers with Reflection Probes on whose bounds center is inside the probe.",
                "MeshRenderer и SkinnedMeshRenderer с Reflection Probes, у которых центр границ внутри пробы.");
            Add("ui.lt.col.reflUsage", "Reflection Probes", "Reflection Probes");
            Add("ui.lt.reflOutside", "Objects outside the reflection probes", "Объекты вне reflection probes");
            Add("ui.lt.reflOutside.none", "Every object with Reflection Probes on stands in a probe.", "Все объекты с Reflection Probes стоят в пробах.");
            Add("ui.lt.mark.duplicate", "duplicate", "дубликат");
            Add("ui.lt.mark.noBox", "no box", "без box");
            Add("ui.lt.mark.probeNotBaked", "not baked", "не запечена");
            Add("ui.lt.boxOn", "Box On", "Включить box");
            Add("ui.lt.boxOn.tip", "Turns on Box Projection after the confirmation; Ctrl+Z takes it back. Fit the box to the room's walls afterwards.",
                "Включает Box Projection после подтверждения; Ctrl+Z возвращает. Затем подгоните коробку к стенам комнаты.");
        }
    }
}
