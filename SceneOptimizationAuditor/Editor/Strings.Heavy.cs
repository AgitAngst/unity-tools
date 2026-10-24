namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        static void AddHeavy()
        {
            // Copies together on the Problems tab.
            Add("ui.copies", "Copies together", "Копии вместе");
            Add("ui.copies.tip", "Copies of one prefab or model object as one row; a prefab is fixed once instead of every copy.",
                "Копии одного объекта префаба или модели — одной строкой; префаб исправляется один раз, а не каждая копия.");
            Add("ui.copies.count", "copies: {0}", "копий: {0}");
            Add("ui.copies.pick.tip", "Pick every copy of the row.", "Выбрать все копии строки.");
            Add("ui.copies.select.tip", "Click to select every copy in the Hierarchy.", "Клик — выделить все копии в Hierarchy.");
            Add("ui.fix.inPrefab", "Fix in Prefab", "Исправить в префабе");
            Add("ui.fix.inPrefab.scope", "in prefab {0}: every instance in the project changes; copies in the scenes: {1}", "в префабе {0}: меняются все его экземпляры в проекте; копий в сценах: {1}");
            Add("ui.fix.copies", "Fix Copies ({0})", "Исправить копии ({0})");
            Add("ui.fix.copies.tip",
                "Fixed copy by copy (an override on each): the source is a model or a prefab outside Assets/, some copies are acceptable, hidden by the search or not picked, or a fix changes an asset.",
                "Исправляется каждая копия (override у каждой): источник — модель или префаб вне Assets/, часть копий допустимы, скрыты поиском или не выбраны, либо исправление меняет ассет.");
            Add("ui.fix.plan.tip",
                "With \"Copies together\" copies of one prefab object are fixed once in the prefab; the rest one by one. The confirmation window lists every change.",
                "При «Копии вместе» копии одного объекта префаба исправляются один раз в префабе, остальное — по одной. Окно подтверждения покажет каждое изменение.");

            // Heaviest tab.
            Add("ui.tab.heavy", "Heaviest", "Тяжёлые");
            Add("ui.hv.intro", "Which sources of the level — prefabs, models, meshes — give the most triangles, draw calls and texture memory. The scene is not changed.",
                "Какие исходники уровня — префабы, модели, меши — дают больше всего треугольников, draw calls и памяти текстур. Сцена не меняется.");
            Add("ui.hv.collect", "Collect", "Собрать");
            Add("ui.hv.collect.tip", "Groups the active renderers of the loaded scenes by prefab, model and mesh. Nothing is changed.",
                "Группирует включённые рендереры загруженных сцен по префабам, моделям и мешам. Ничего не меняет.");
            Add("ui.hv.onlyNoLod", "Only without LOD", "Только без LOD");
            Add("ui.hv.emptyTitle", "Sources not collected yet", "Исходники ещё не собраны");
            Add("ui.hv.emptyText", "The tool groups the renderers of the loaded scenes by prefab, model and mesh and counts triangles, draw calls and textures. Nothing is changed.",
                "Утилита сгруппирует рендереры загруженных сцен по префабам, моделям и мешам и посчитает треугольники, draw calls и текстуры. Ничего не меняет.");
            Add("ui.hv.seg.tris", "Triangles", "Треугольники");
            Add("ui.hv.seg.dc", "Draw calls", "Draw calls");
            Add("ui.hv.seg.tex", "Textures", "Текстуры");
            Add("ui.hv.seg.tip", "What the table is sorted by and the share counted of.", "По чему сортировать таблицу и считать долю.");
            Add("ui.hv.summary.0", "Sources: {0} · the first {1} give {2}% of the scene triangles", "Исходников: {0} · первые {1} — {2}% треугольников сцены");
            Add("ui.hv.summary.1", "Sources: {0} · the first {1} give {2}% of the draw calls", "Исходников: {0} · первые {1} — {2}% draw calls");
            Add("ui.hv.summary.2", "Sources: {0} · the first {1} give {2}% of the texture memory", "Исходников: {0} · первые {1} — {2}% памяти текстур");
            Add("ui.hv.none", "No active renderers with meshes in the loaded scenes.", "В загруженных сценах нет включённых рендереров с мешами.");
            Add("ui.hv.allLod", "No source has a copy over the object threshold without a LODGroup.", "Нет исходников, у которых копия сверх порога объекта без LODGroup.");
            Add("ui.hv.sub",
                "LOD0 only (triangles, draw calls, materials, textures); draw calls — one per material, without shadows and lights; MB — the textures of its materials (shared ones count for each).",
                "Только LOD0 (треугольники, draw calls, материалы, текстуры); draw calls — по материалам, без теней и света; МБ — текстуры его материалов (общие считаются у каждого).");
            Add("ui.hv.footer", "Click a row to select the copies in the scene, the name to show the source in Project. An estimate: batching, culling and shadows are not counted.",
                "Клик по строке — выделить копии в сцене, по имени — исходник в Project. Это оценка: батчинг, отсечение и тени не учтены.");
            Add("ui.hv.col.source", "Source", "Исходник");
            Add("ui.hv.col.source.tip", "Prefab (the asset of the nearest prefab copy), else model or mesh, else the object.",
                "Префаб (ассет ближайшей копии префаба), иначе модель или меш, иначе объект.");
            Add("ui.hv.col.kind", "Kind", "Вид");
            Add("ui.hv.col.copies", "Copies", "Копий");
            Add("ui.hv.col.perCopy", "Per copy", "На копию");
            Add("ui.hv.col.perCopy.tip", "Triangles per copy on average, LOD0.", "Треугольники на копию в среднем, LOD0.");
            Add("ui.hv.col.trisTotal", "Tris total", "Треуг. всего");
            Add("ui.hv.col.trisTotal.short", "Tris", "Треуг.");
            Add("ui.hv.col.trisTotal.tip", "Triangles of every copy, LOD0.", "Треугольники всех копий, LOD0.");
            Add("ui.hv.col.dc", "Draw calls", "Draw calls");
            Add("ui.hv.col.dc.short", "DC", "DC");
            Add("ui.hv.col.dc.tip", "One per material of every copy, without shadows and lights.", "По одному на материал каждой копии, без теней и света.");
            Add("ui.hv.col.materials", "Mater.", "Матер.");
            Add("ui.hv.col.materials.tip", "Distinct materials.", "Разных материалов.");
            Add("ui.hv.col.mb", "Tex., MB", "Текст., МБ");
            Add("ui.hv.col.mb.short", "MB", "МБ");
            Add("ui.hv.col.mb.tip", "Distinct textures of its materials, estimated like on the Budgets tab.", "Разные текстуры его материалов, оценка как на «Бюджетах».");
            Add("ui.hv.col.share", "Share", "Доля");
            Add("ui.hv.col.share.tip", "Share of the scene by the chosen measure.", "Доля сцены по выбранной мере.");
            Add("ui.hv.kind.Prefab", "prefab", "префаб");
            Add("ui.hv.kind.Model", "model", "модель");
            Add("ui.hv.kind.Mesh", "mesh", "меш");
            Add("ui.hv.kind.Object", "object", "объект");
            Add("ui.hv.mark.noLod", "no LOD", "нет LOD");
            Add("ui.hv.mark.noLod.tip", "A copy has more triangles than the object threshold and a renderer outside any LODGroup.", "У копии треугольников больше порога объекта и рендерер вне LODGroup.");
        }
    }
}
