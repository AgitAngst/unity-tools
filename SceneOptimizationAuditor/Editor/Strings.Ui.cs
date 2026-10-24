namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        static void AddUi()
        {
            Add("ui.title", "VR Scene Audit", "VR-аудит сцены");
            Add("ui.run", "Run Audit", "Запустить аудит");
            Add("ui.rerun", "Run Again", "Запустить снова");
            Add("ui.run.tip", "Checks every loaded scene. Nothing is changed.", "Проверяет все загруженные сцены. Ничего не меняет.");
            Add("ui.tab.problems", "Problems", "Проблемы");
            Add("ui.tab.problems.n", "Problems ({0})", "Проблемы ({0})");
            Add("ui.tab.budgets", "Budgets", "Бюджеты");
            Add("ui.tab.lightmap", "Lightmap UVs", "Развёртка под свет");
            Add("ui.lm.intro", "Checks every model in the loaded scenes, including objects not marked for lightmaps: is Generate Lightmap UVs on, is there a lightmap UV channel (UV1), does it stay inside 0–1 and does it overlap.",
                "Проверяет все модели загруженных сцен, в том числе у объектов без отметки под lightmap: стоит ли галочка Generate Lightmap UVs, есть ли развёртка под свет (UV1), не выходит ли она за 0–1 и нет ли наложений.");
            Add("ui.lm.scan", "Scan Models", "Проверить модели");
            Add("ui.lm.scan.tip", "Scans the loaded scenes, inactive objects too. Nothing is changed.", "Сканирует загруженные сцены, и неактивные объекты тоже. Ничего не меняет.");
            Add("ui.lm.enableAll", "Turn on for all without UV1 ({0})", "Включить у всех без UV1 ({0})");
            Add("ui.lm.enable", "Turn on", "Включить галочку");
            Add("ui.lm.csv", "Save CSV", "Сохранить CSV");
            Add("ui.lm.sel.count", "Selected models: {0}", "Выбрано моделей: {0}");
            Add("ui.lm.sel.enable", "Turn on for selected ({0})", "Включить у выбранных ({0})");
            Add("ui.lm.sel.assets", "Select in Project ({0})", "Выделить в Project ({0})");
            Add("ui.lm.sel.assets.tip", "Selects the model files, so their import settings can be changed together in the Inspector.",
                "Выделяет файлы моделей, чтобы поменять их импорт вместе в Inspector.");
            Add("ui.lm.sel.objects", "Select objects in scene ({0})", "Выделить объекты в сцене ({0})");
            Add("ui.lm.sel.objects.tip", "Selects every scene object with these models, for example to set Contribute GI on all of them.",
                "Выделяет все объекты сцены с этими моделями — например, чтобы поставить им Contribute GI.");
            Add("ui.lm.sel.all.tip", "Pick every model in the list.", "Выбрать все модели списка.");
            Add("ui.sel.count", "Selected: {0}", "Выбрано: {0}");
            Add("ui.sel.hidden", "{0} hidden by the filter", "скрыто фильтром: {0}");
            Add("ui.sel.fix", "Fix selected ({0})", "Исправить выбранные ({0})");
            Add("ui.sel.select", "Select in Unity ({0})", "Выделить в Unity ({0})");
            Add("ui.sel.select.tip", "Selects the objects and assets in the Hierarchy and Project, so you can edit them together in the Inspector.",
                "Выделяет объекты и ассеты в Hierarchy и Project, чтобы править их вместе в Inspector.");
            Add("ui.sel.clear", "Clear selection", "Снять выбор");
            Add("ui.acc.tag", "acceptable", "допустимо");
            Add("ui.acc.show", "Acceptable ({0})", "Допустимые ({0})");
            Add("ui.acc.show.tip", "Show problems marked as acceptable; they are not counted.", "Показать проблемы, помеченные допустимыми; они не считаются.");
            Add("ui.acc.hiddenNote", "{0} acceptable problems are hidden — turn on \"Acceptable\" to see them.",
                "Ещё {0} допустимых проблем скрыто — включите «Допустимые», чтобы увидеть.");
            Add("ui.acc.mark", "Mark as acceptable ({0})", "Пометить допустимыми ({0})");
            Add("ui.acc.mark.tip", "The problem stays where it is but is not counted any more, for example large houses on purpose. Saved for the project.",
                "Проблема остаётся, но больше не считается — например, большие дома так и задуманы. Сохраняется для проекта.");
            Add("ui.acc.release", "Remove the mark ({0})", "Снять пометку ({0})");
            Add("ui.acc.release.tip", "Count these problems again.", "Снова считать эти проблемы.");
            Add("ui.acc.disable", "Turn off the check ({0})", "Отключить проверку ({0})");
            Add("ui.acc.disable.tip", "Every problem of these checks becomes acceptable in the whole project.", "Все проблемы этих проверок становятся допустимыми во всём проекте.");
            Add("ui.acc.disable.title", "Turn off checks", "Отключить проверки");
            Add("ui.acc.disable.ask", "Turn off these checks for the whole project?\n\n{0}\n\nThey can be turned on again on the Budgets tab.",
                "Отключить эти проверки во всём проекте?\n\n{0}\n\nВключить их снова можно на вкладке «Бюджеты».");
            Add("ui.acc.disable.yes", "Turn off", "Отключить");
            Add("ui.acc.section", "Acceptable problems and turned-off checks", "Допустимые проблемы и отключённые проверки");
            Add("ui.acc.section.note", "Marks are kept per object or asset and survive renames; problems without an object (budgets, settings) are marked for the whole project.",
                "Пометки хранятся по объекту или ассету и переживают переименование; проблемы без объекта (бюджеты, настройки) помечаются для всего проекта.");
            Add("ui.acc.marked", "Marked as acceptable: {0}", "Помечено допустимыми: {0}");
            Add("ui.acc.clearAll", "Remove all marks", "Снять все пометки");
            Add("ui.acc.clearAll.ask", "Remove every acceptable mark and turn every check on again?", "Снять все пометки и снова включить все проверки?");
            Add("ui.acc.noDisabled", "No checks are turned off.", "Отключённых проверок нет.");
            Add("ui.acc.enable", "Turn on", "Включить");
            Add("ui.acc.file", "Saved in {0} — commit it to share with the team.", "Хранится в {0} — закоммитьте, чтобы команда видела то же.");
            Add("ui.sel.all", "Select all", "Выбрать все");
            Add("ui.sel.all.tip", "Picks every shown problem that has an object or a fix.", "Выбирает все показанные проблемы с объектом или исправлением.");
            Add("ui.sel.group.tip", "Pick every object of this problem.", "Выбрать все объекты этой проблемы.");
            Add("ui.sel.pick.tip", "Add to the selection.", "Добавить в выбор.");
            Add("ui.lm.onlyProblems", "Problems only", "Только проблемные");
            Add("ui.lm.notScanned", "Press Scan Models to list the models of the loaded scenes.", "Нажмите «Проверить модели», чтобы увидеть модели загруженных сцен.");
            Add("ui.lm.summary", "Models: {0} · without UV1: {1} · option on: {2} · with problems: {3}", "Моделей: {0} · без UV1: {1} · галочка стоит: {2} · с проблемами: {3}");
            Add("ui.lm.none", "No meshes in the loaded scenes.", "В загруженных сценах нет мешей.");
            Add("ui.lm.allGood", "No problems: every model has clean lightmap UVs.", "Проблем нет: у всех моделей чистая развёртка под свет.");
            Add("ui.lm.builtIn", "Built-in or scene mesh without an asset", "Встроенный меш или меш сцены без ассета");
            Add("ui.lm.on", "on", "стоит");
            Add("ui.lm.off", "off", "нет");
            Add("ui.lm.yes", "yes", "есть");
            Add("ui.lm.no", "no", "нет");
            Add("ui.lm.objects", "{0} (baked {1})", "{0} (в lightmap {1})");
            Add("ui.lm.col.model", "Model", "Модель");
            Add("ui.lm.col.model.tip", "Click to select it in the Project window.", "Клик — выделить в окне Project.");
            Add("ui.lm.col.flag", "Generate UVs", "Галочка");
            Add("ui.lm.col.flag.tip", "Generate Lightmap UVs in the model import settings; — for meshes without a model file.", "Generate Lightmap UVs в импорте модели; «—» — у мешей без файла модели.");
            Add("ui.lm.col.uv1", "UV1", "UV1");
            Add("ui.lm.col.uv1.tip", "Lightmap UV channel present in every mesh of the model.", "Развёртка под свет есть у всех мешей модели.");
            Add("ui.lm.col.outside", "Out of 0–1", "За 0–1");
            Add("ui.lm.col.outside.tip", "Share of UV1 vertices outside 0–1.", "Доля вершин UV1 за пределами 0–1.");
            Add("ui.lm.col.overlap", "Overlaps", "Наложения");
            Add("ui.lm.col.overlap.tip", "Share of lightmap texels shared by several UV1 triangles; above 1% is a problem.", "Доля текселей lightmap, общих у нескольких треугольников UV1; больше 1 % — проблема.");
            Add("ui.lm.col.objects", "Objects", "Объекты");
            Add("ui.lm.col.objects.tip", "Objects that use the model, and how many of them are baked into lightmaps.", "Объекты с этой моделью и сколько из них запекаются в lightmap.");
            Add("ui.tab.profiler", "Profiler", "Профайлер");
            Add("ui.tab.history", "History", "История");
            Add("ui.h.title", "Progress over time", "Прогресс во времени");
            Add("ui.h.intro", "Every audit run is a point of the scene history. Runs of one day make one point (the last one); a point with a note stays, and the next run adds a new one. Pick points A and B in the table to compare them.",
                "Каждый прогон аудита — точка истории сцены. Прогоны одного дня — одна точка (последний); точка с заметкой остаётся, и следующий прогон добавит новую. Отметьте в таблице точки A и B, чтобы сравнить их.");
            Add("ui.h.scene", "Scenes", "Сцены");
            Add("ui.h.scene.tip", "History of the audited scenes or of another scene set recorded in this project.", "История проверенных сцен или другого набора сцен этого проекта.");
            Add("ui.h.scene.current", "{0} (current)", "{0} (текущие)");
            Add("ui.h.none", "No history for these scenes yet. Run the audit: the run becomes the first point.", "У этих сцен истории пока нет. Запустите аудит — прогон станет первой точкой.");
            Add("ui.h.trend", "Trend", "Динамика");
            Add("ui.h.trend.note", "Grey lines — points A and B; dashed — the Warning budget. The number on the right is at the later point, below it the change since the earlier one.",
                "Серые линии — точки A и B, пунктир — порог Warning. Справа — значение в поздней точке, под ним — изменение с ранней.");
            Add("ui.h.sinceA", "since earlier: {0}", "с ранней: {0}");
            Add("ui.h.sinceA.tip", "Change between the compared points.", "Изменение между сравниваемыми точками.");
            Add("ui.h.critical", "Critical", "Критичные");
            Add("ui.h.warning", "Warnings", "Предупреждения");
            Add("ui.h.info", "Info", "Сведения");
            Add("ui.h.accepted", "Acceptable", "Допустимые");
            Add("ui.h.cpu", "CPU frame, ms", "Кадр CPU, мс");
            Add("ui.h.gpu", "GPU frame, ms", "Кадр GPU, мс");
            Add("ui.h.points", "Points ({0})", "Точки ({0})");
            Add("ui.h.points.note", "Newest at the top. A and B are the compared points; the order does not matter.",
                "Новые сверху. A и B — сравниваемые точки, порядок не важен.");
            Add("ui.h.col.date", "Date", "Дата");
            Add("ui.h.col.critical", "Crit.", "Крит.");
            Add("ui.h.col.warning", "Warn.", "Пред.");
            Add("ui.h.col.info", "Info", "Свед.");
            Add("ui.h.col.tris", "Triangles", "Треуг.");
            Add("ui.h.col.drawCalls", "Draw calls", "Draw calls");
            Add("ui.h.col.texMem", "Tex., MB", "Текст., МБ");
            Add("ui.h.col.note", "Note", "Заметка");
            Add("ui.h.col.a.tip", "Point A of the comparison.", "Точка A для сравнения.");
            Add("ui.h.col.b.tip", "Point B of the comparison; its note can be edited below.", "Точка B для сравнения; её заметку можно изменить ниже.");
            Add("ui.h.noteOf", "Note to point B ({0})", "Заметка к точке B ({0})");
            Add("ui.h.note.hint", "What was done before this run, e.g. \"LODs on the houses, atlas for props\". A noted point is not replaced by later runs of the same day.",
                "Что сделано перед этим прогоном, например «LOD у домов, атлас для мелочи». Точку с заметкой поздние прогоны того же дня не заменят.");
            Add("ui.h.note.save", "Save note", "Сохранить заметку");
            Add("ui.h.version", "Audited by v{0}", "Проверено v{0}");
            Add("ui.h.delete", "Remove point", "Удалить точку");
            Add("ui.h.delete.ask", "Remove the point of {0} from the history? This cannot be undone.", "Удалить точку {0} из истории? Отменить нельзя.");
            Add("ui.h.compare", "Comparison: {0} → {1}", "Сравнение: {0} → {1}");
            Add("ui.h.checks", "Checks with a changed count ({0})", "Проверки, где число изменилось ({0})");
            Add("ui.h.checks.none", "No check changed its count.", "Ни у одной проверки число не изменилось.");
            Add("ui.h.fixed", "Fixed ({0})", "Исправлено ({0})");
            Add("ui.h.new", "New ({0})", "Появилось ({0})");
            Add("ui.h.more", "…and {0} more", "…и ещё {0}");
            Add("ui.h.acceptedSince", "Marked acceptable in between: {0} (not counted as fixed).", "Помечено допустимыми за это время: {0} (не считаются исправленными).");
            Add("ui.h.problems.old", "Old points keep only the numbers, not the list of problems: fixed and new problems are shown for the last 50 points.",
                "У старых точек хранятся только числа, без списка проблем: исправленные и новые видны для последних 50 точек.");
            Add("ui.h.file", "Saved in {0} — commit it to share the history with the team.", "Хранится в {0} — закоммитьте, чтобы история была у всей команды.");
            Add("ui.h.since", "Since the last point ({0}): Critical {1} · Warnings {2}", "С прошлой точки ({0}): критичных {1} · предупреждений {2}");
            Add("ui.h.since.tip", "Compared with the previous point of the History tab.", "Сравнение с прошлой точкой вкладки «История».");
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
            Add("ui.export.tip", "Saves the problem list as JSON or CSV.", "Сохраняет список проблем в JSON или CSV.");
            Add("ui.lang.tip", "Window language", "Язык окна");
            Add("ui.version", "v{0}", "v{0}");
            Add("ui.version.tip", "Utility version; the last number is the build (commit count).", "Версия утилиты; последнее число — номер сборки (число коммитов).");
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
            Add("cat.Materials", "Materials", "Материалы");
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
    }
}
