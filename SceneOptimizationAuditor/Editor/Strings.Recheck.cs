namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        // The run bar at the bottom, the run kept across script reloads and the quick re-check (2.7.0).
        static void AddRecheck()
        {
            Add("ui.bar.none", "No runs yet", "Прогонов ещё не было");
            Add("ui.bar.run", "Run at {0} · {1} s", "Прогон {0} · {1} с");
            Add("ui.bar.runAt", "Run at {0}", "Прогон {0}");
            Add("ui.bar.afterReload", "after the script reload", "после перезагрузки скриптов");
            Add("ui.bar.rechecked", "{0} re-checked at {1}", "перепроверено: {0} в {1}");
            Add("ui.bar.waiting", "The run of {0} is checked again once Play Mode ends and scripts are compiled",
                "Прогон {0} проверится снова после Play Mode и компиляции");
            Add("ui.bar.dropped", "The run of {0} was dropped by the script reload", "Прогон {0} сброшен перезагрузкой скриптов");

            Add("ui.kept.reloaded", "The list was refreshed after the script reload", "Список обновлён после перезагрузки скриптов");
            Add("ui.kept.dropped.title", "The script reload dropped the run", "Перезагрузка скриптов сбросила прогон");
            Add("ui.kept.dropped.scenesTitle", "The script reload dropped the check of the build scenes", "Перезагрузка скриптов сбросила проверку сцен сборки");
            Add("ui.kept.dropped.scenes", "Run at {0}, scenes: {1}. It does not start by itself: it opens the scenes one by one and asks to save.",
                "Прогон {0}, сцен: {1}. Сам не запускается: он открывает сцены по очереди и спрашивает о сохранении.");
            Add("ui.kept.dropped.cancelled", "Run at {0}. Checking it again was cancelled.", "Прогон {0}. Повторную проверку отменили.");
            Add("ui.kept.runScenes", "Run Again ({0})", "Запустить снова ({0})");

            Add("ui.rc.items", "{0} item|{0} items", "{0} элемент|{0} элемента|{0} элементов");
            Add("ui.rc.objects", "{0} object|{0} objects", "{0} объект|{0} объекта|{0} объектов");
            Add("ui.rc.rows", "{0} row|{0} rows", "{0} запись|{0} записи|{0} записей");
            Add("ui.rc.scenes", "{0} scene|{0} scenes", "{0} сцена|{0} сцены|{0} сцен");
            Add("ui.rc.tip", "Re-check {0}: the object with its children and its assets, without a full run",
                "Перепроверить {0}: объект с детьми и его ассеты, без полного прогона");
            Add("ui.rc.tip.asset", "Re-check {0}: the asset as the open scenes use it, without a full run",
                "Перепроверить {0}: ассет — как его используют открытые сцены, без полного прогона");
            Add("ui.rc.tip.setting", "Re-check this project setting", "Перепроверить эту настройку проекта");
            Add("ui.rc.tip.scene", "A total or a count of the whole scene — Run Again updates it", "Итог или счёт по всей сцене — его обновит «Запустить снова»");
            Add("ui.rc.tip.folder", "A row of the folder check — Run Again updates it", "Запись проверки папки — её обновит «Запустить снова»");
            Add("ui.rc.tip.closed", "The scene is closed — open it to re-check the row", "Сцена закрыта — откройте её, чтобы перепроверить запись");
            Add("ui.rc.tip.group", "Re-check the objects of the group ({0}) without a full run", "Перепроверить объекты группы ({0}) без полного прогона");
            Add("ui.rc.tip.copies", "Re-check the copies ({0}) without a full run", "Перепроверить копии ({0}) без полного прогона");
            Add("ui.rc.picked", "Re-check Picked ({0})", "Перепроверить выбранное ({0})");
            Add("ui.rc.picked.tip", "Checks the picked rows again — their objects with children, assets and settings — without a full run. Budget bars and scene totals stay from the full run.",
                "Проверяет выбранные записи заново — их объекты с детьми, ассеты и настройки — без полного прогона. Полосы бюджета и итоги сцены остаются с полного прогона.");
            Add("ui.rc.mark", "re-checked at {0}", "перепроверено {0}");
            Add("ui.rc.done", "Re-checked {0} in {1} s: {2} went, {3} left", "Перепроверено: {0} за {1} с — ушло: {2}, осталось: {3}");
            Add("ui.rc.done.new", ", {0} new", ", новых: {0}");
            Add("ui.rc.nothing", "Nothing here can be re-checked without a full run", "Здесь нечего перепроверять без полного прогона");
            Add("ui.rc.progress", "Re-checking {0}…", "Перепроверка: {0}…");
        }
    }
}
