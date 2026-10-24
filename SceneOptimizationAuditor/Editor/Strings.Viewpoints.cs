namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        static void AddViewpoints()
        {
            // Target GPU on the Budgets tab and in the context line.
            Add("ui.gpu", "Target GPU", "Целевая видеокарта");
            Add("ui.gpu.tip", "Picking a GPU fills triangles and texture memory with its starting values; the other thresholds are the same for every GPU.",
                "Выбор подставляет треугольники и память текстур под карту; остальные пороги у всех карт одинаковы.");
            Add("ui.gpu.item", "{0} ({1} GB)", "{0} ({1} ГБ)");
            Add("ui.gpu.custom", "custom thresholds", "свои пороги");
            Add("ui.gpu.custom.tip", "Triangles or texture memory differ from the set of the GPU. \"Defaults\" brings its values back.",
                "Треугольники или память текстур отличаются от набора карты. «По умолчанию» вернёт её значения.");
            Add("ui.gpu.note", "Triangles and texture memory follow the GPU; the other thresholds are the same for every GPU. Starting values — refine them with the profiler on the target PC.",
                "Под карту подстраиваются треугольники и память текстур, остальные пороги у всех карт одинаковы. Значения стартовые — уточните профайлером на целевом ПК.");
            Add("ui.gpu.context", "GPU: {0}", "Видеокарта: {0}");
            Add("ui.gpu.context.edited", "GPU: {0}, custom thresholds", "Видеокарта: {0}, свои пороги");
            Add("ui.gpu.context.custom", "Custom thresholds", "Пороги: свои");
            Add("ui.b.transparent", "Transparent in frame, % of screen", "Прозрачное в кадре, % экрана");
            Add("ui.budgets.reset.tip", "Thresholds of the chosen GPU, the others back to defaults.", "Пороги выбранной видеокарты, остальные — по умолчанию.");

            // Viewpoints tab.
            Add("ui.tab.viewpoints", "Viewpoints", "Точки обзора");
            Add("ui.vp.intro",
                "What the player sees from where they stand: 8 directions 45° apart, the LOD level Unity would pick, triangles, draw calls and the share of the screen under transparent objects. An upper estimate — no batching, occlusion culling or shader cost. The scene is not changed.",
                "Что видно из мест, где бывает игрок: 8 направлений через 45°, уровень LOD — как выбирает Unity, треугольники, draw calls и доля экрана под прозрачным. Это оценка сверху — без батчинга, occlusion culling и стоимости шейдеров. Сцена не меняется.");
            Add("ui.vp.estimate", "Estimate", "Оценить");
            Add("ui.vp.estimate.tip", "Estimates every viewpoint. Nothing in the scene is changed.", "Оценивает все точки. Сцена не меняется.");
            Add("ui.vp.place", "Place Viewpoints", "Расставить точки");
            Add("ui.vp.place.tip", "Places viewpoints by the NavMesh, SteamVR teleports, scene cameras or rays down. Manual viewpoints stay.",
                "Расставляет точки по NavMesh, телепортам SteamVR, камерам сцены или лучом вниз. Ручные точки остаются.");
            Add("ui.vp.addHere", "Add From Here", "Добавить отсюда");
            Add("ui.vp.addHere.tip", "Adds a viewpoint at the Scene view camera, looking where it looks.", "Добавляет точку в камеру Scene view, со взглядом туда же.");
            Add("ui.vp.walls", "Count walls", "Учитывать стены");
            Add("ui.vp.walls.tip", "Hides objects behind colliders of other objects (rays from the eye). Without colliders on walls nothing is hidden. Works on the next estimate.",
                "Скрывает объекты за коллайдерами других объектов (лучи от глаза). Без коллайдеров на стенах ничего не скрывается. Действует со следующей оценки.");
            Add("ui.vp.onlyOver", "Over budget only", "Только сверх бюджета");
            Add("ui.vp.allGood", "Every estimated viewpoint is within budget.", "Все оценённые точки в бюджете.");
            Add("ui.vp.summary", "Viewpoints: {0} · over budget: {1} · worst: {2} — {3} draw calls", "Точек: {0} · сверх бюджета: {1} · худшая: {2} — {3} draw calls");
            Add("ui.vp.summary.notEstimated", "Viewpoints: {0} · not estimated yet — press \"Estimate\"", "Точек: {0} · оценки ещё нет — нажмите «Оценить»");
            Add("ui.vp.sources.navmesh", "NavMesh {0}", "NavMesh {0}");
            Add("ui.vp.sources.teleport", "teleports {0}", "телепорты {0}");
            Add("ui.vp.sources.camera", "cameras {0}", "камеры {0}");
            Add("ui.vp.sources.manual", "manual {0}", "вручную {0}");
            Add("ui.vp.sources.ray", "rays {0}", "лучи {0}");
            Add("ui.vp.estimated", "estimated {0} in {1} s", "оценено {0} за {1} с");
            Add("ui.vp.walls.on", "walls counted", "стены учтены");
            Add("ui.vp.walls.off", "walls not counted", "стены не учтены");
            Add("ui.vp.stepGrown", "grid step grew to {0} m", "шаг сетки вырос до {0} м");
            Add("ui.vp.sel.count", "Selected viewpoints: {0}", "Выбрано точек: {0}");
            Add("ui.vp.sel.estimate", "Estimate selected ({0})", "Оценить выбранные ({0})");
            Add("ui.vp.sel.remove", "Remove selected ({0})", "Удалить выбранные ({0})");
            Add("ui.vp.sel.remove.tip", "Ctrl+Z brings them back.", "Ctrl+Z вернёт их.");
            Add("ui.vp.col.point", "Viewpoint", "Точка");
            Add("ui.vp.col.point.tip", "Click — the Scene view camera stands here and looks in the worst direction.", "Клик — камера Scene view встаёт сюда и смотрит в худшем направлении.");
            Add("ui.vp.col.kind", "Kind", "Вид");
            Add("ui.vp.col.kind.tip", "Where the viewpoint comes from.", "Откуда точка.");
            Add("ui.vp.col.tris", "Tris", "Треуг.");
            Add("ui.vp.col.tris.tip", "Triangles in the worst direction, at the LOD level Unity would pick.", "Треугольники в худшем направлении, на уровне LOD, который выберет Unity.");
            Add("ui.vp.col.dc", "Draw calls", "Draw calls");
            Add("ui.vp.col.dc.tip", "Material slots of what is seen, extra pixel light passes and shadow passes; ×2 with VR in Multi Pass or Single Pass stereo.",
                "Слоты материалов видимого, доп. проходы pixel lights и проходы теней; ×2 при VR в stereo Multi Pass или Single Pass.");
            Add("ui.vp.col.transparent", "Transparent", "Прозрачное");
            Add("ui.vp.col.transparent.tip", "Share of the screen under transparent objects and particles; above 100 % means layers of overdraw.",
                "Доля экрана под прозрачными объектами и частицами; больше 100 % — слои overdraw.");
            Add("ui.vp.col.dir", "Direction", "Направление");
            Add("ui.vp.col.dir.tip", "The worst direction: 0° looks along +Z, 90° along +X.", "Худшее направление: 0° — вдоль +Z, 90° — вдоль +X.");
            Add("ui.vp.kind.Manual", "manual", "вручную");
            Add("ui.vp.kind.NavMesh", "NavMesh", "NavMesh");
            Add("ui.vp.kind.Teleport", "teleport", "телепорт");
            Add("ui.vp.kind.Camera", "camera", "камера");
            Add("ui.vp.kind.Ray", "ray", "луч");
            Add("ui.vp.manualName", "Point {0}", "Точка {0}");
            Add("ui.vp.rayName", "Ray {0}", "Луч {0}");
            Add("ui.vp.meta", "{0} · x {1} · y {2} · z {3}", "{0} · x {1} · y {2} · z {3}");
            Add("ui.vp.remove", "Remove Viewpoint", "Удалить точку");
            Add("ui.vp.dirs", "Directions", "Направления");
            Add("ui.vp.col.az", "Azimuth", "Азимут");
            Add("ui.vp.dirs.tip", "Click — the Scene view camera looks this way.", "Клик — камера Scene view смотрит сюда.");
            Add("ui.vp.worst", "worst", "худшее");
            Add("ui.vp.notEstimated", "Not estimated yet: press \"Estimate\".", "Оценки ещё нет: нажмите «Оценить».");
            Add("ui.vp.heavy", "What is heavy here · {0}", "Что здесь тяжёлое · {0}");
            Add("ui.vp.heavy.tip", "Click — select its copies in the scene.", "Клик — выделить копии в сцене.");
            Add("ui.vp.sort.tris", "Triangles", "Треугольники");
            Add("ui.vp.sort.dc", "Draw calls", "Draw calls");
            Add("ui.vp.heavy.tris", "{0} tris", "{0} треуг.");
            Add("ui.vp.heavy.dc", "{0} draw calls", "{0} draw calls");
            Add("ui.vp.heavy.screen", "{0} of screen", "{0} экрана");
            Add("ui.vp.heavy.none", "Nothing is seen in this direction.", "В этом направлении ничего не видно.");
            Add("ui.vp.mark.far", "far but LOD0", "далеко, а LOD0");
            Add("ui.vp.mark.far.tip", "Smaller than 5 % of the screen height and still drawn in full detail.", "Меньше 5 % высоты экрана, а рисуется в полной детализации.");
            Add("ui.vp.mark.nolod", "no LOD group", "нет LODGroup");
            Add("ui.vp.mark.nolod.tip", "More triangles than the object threshold and no LOD group.", "Треугольников больше порога объекта, LODGroup нет.");
            Add("ui.vp.mark.shadow", "small but casts shadows", "мелкий, но с тенью");
            Add("ui.vp.mark.shadow.tip", "Smaller than 30 cm and drawn again in every shadow pass.", "Меньше 30 см, а рисуется ещё раз в каждом проходе теней.");
            Add("ui.vp.mark.transparent", "large transparent", "крупный прозрачный");
            Add("ui.vp.mark.transparent.tip", "Transparent and covers more than a quarter of the screen: overdraw.", "Прозрачный и закрывает больше четверти экрана: overdraw.");
            Add("ui.vp.footer",
                "Click a viewpoint to put the Scene view camera there, facing its worst direction; click a direction to look there; click a row of \"What is heavy here\" to select its copies in the scene. Not counted: batching and instancing, Unity occlusion culling, objects spawned at runtime, shader and GPU cost.",
                "Клик по точке — камера Scene view встаёт в неё и смотрит в худшем направлении; клик по направлению — смотреть туда; клик по строке «Что здесь тяжёлое» — выделить её копии в сцене. Не учитываются: батчинг и instancing, occlusion culling Unity, объекты, которые появятся в игре, стоимость шейдеров и GPU.");
            Add("ui.vp.empty.title", "No viewpoints yet", "Точек обзора пока нет");
            Add("ui.vp.empty.text",
                "A viewpoint is a place where the player stands. The tool places viewpoints along the NavMesh, SteamVR teleports or scene cameras and estimates what each one sees. The scene is not changed: viewpoints live in SceneAuditPoints/ next to Assets/, and the folder can be committed.",
                "Точка обзора — место, где бывает игрок. Утилита расставит точки по NavMesh, телепортам SteamVR или камерам сцены и оценит, что видно из каждой. Сцена не меняется: точки хранятся в SceneAuditPoints/ рядом с Assets/, папку можно закоммитить.");
            Add("ui.vp.empty.button", "Place Viewpoints…", "Расставить точки…");
            Add("ui.vp.found", "Found: NavMesh — {0} · SteamVR teleports — {1} · scene cameras — {2}", "Найдено: NavMesh — {0} · телепорты SteamVR — {1} · камеры сцены — {2}");
            Add("ui.vp.yes", "yes", "есть");
            Add("ui.vp.no", "no", "нет");
            Add("ui.vp.empty.manual", "Or by hand: \"Add From Here\" puts a viewpoint at the Scene view camera.", "Или вручную: «Добавить отсюда» ставит точку в камеру Scene view.");
            Add("ui.vp.noSceneView", "Open a Scene view first.", "Сначала откройте Scene view.");
            Add("ui.vp.progress", "Estimating viewpoints", "Оценка точек обзора");
            Add("ui.vp.placed", "Viewpoints placed: {0}", "Расставлено точек: {0}");
            Add("ui.vp.label", "{0} · {1} draw calls · {2}", "{0} · {1} draw calls · {2}");
            Add("ui.vp.legend", "Viewpoints", "Точки обзора");
            Add("ui.vp.legend.ok", "within budget", "в бюджете");
            Add("ui.vp.legend.warning", "warning", "предупреждение");
            Add("ui.vp.legend.critical", "critical", "критично");
            Add("ui.vp.legend.none", "not estimated", "не оценена");
            Add("ui.vp.legend.note", "Colour — the worst of triangles, draw calls and transparent. Click a sphere to select the viewpoint.",
                "Цвет — по худшему из треугольников, draw calls и прозрачного. Клик по шарику — выбрать точку.");

            // Placement panel under "Place Viewpoints".
            Add("ui.vp.panel.title", "Where to take viewpoints from", "Откуда брать точки");
            Add("ui.vp.panel.navmesh", "NavMesh", "NavMesh");
            Add("ui.vp.panel.teleports", "SteamVR teleports", "Телепорты SteamVR");
            Add("ui.vp.panel.cameras", "Scene cameras", "Камеры сцены");
            Add("ui.vp.panel.rays", "Downward rays", "Лучом вниз");
            Add("ui.vp.panel.found", "found", "найден");
            Add("ui.vp.panel.missing", "not found", "не найден");
            Add("ui.vp.panel.teleports.found", "{0} points, {1} areas", "{0} точек, {1} областей");
            Add("ui.vp.panel.cameras.found", "{0}", "{0}");
            Add("ui.vp.panel.rays.info", "only without NavMesh and teleports", "только без NavMesh и телепортов");
            Add("ui.vp.panel.step", "Grid step, m", "Шаг сетки, м");
            Add("ui.vp.panel.eye", "Eye height, m", "Высота глаз, м");
            Add("ui.vp.panel.note",
                "Manual viewpoints stay: only automatic ones are replaced. At most 500 viewpoints — otherwise the step grows by itself. Ctrl+Z brings the previous viewpoints back.",
                "Ручные точки останутся: заменятся только авто-точки. Не больше 500 точек — иначе шаг вырастет сам. Ctrl+Z возвращает прежние точки.");
            Add("ui.vp.panel.place", "Place (≈ {0})", "Расставить (≈ {0})");

            // History of the worst viewpoint.
            Add("ui.h.vp.dc", "Worst viewpoint: draw calls", "Худшая точка: draw calls");
            Add("ui.h.vp.tris", "Worst viewpoint: triangles", "Худшая точка: треугольники");
            Add("ui.h.vp.transparent", "Worst viewpoint: transparent, %", "Худшая точка: прозрачное, %");
            Add("ui.h.vp.over", "Viewpoints over budget", "Точек сверх бюджета");
        }
    }
}
