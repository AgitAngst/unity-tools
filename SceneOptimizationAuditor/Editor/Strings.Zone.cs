namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        // The player zone of the Viewpoints tab (3.0.0).
        static void AddZone()
        {
            Add("ui.vp.zone", "Player Zone", "Зона игрока");
            Add("ui.vp.zone.tip", "Where the player can be: rectangles in the Scene view. Automatic viewpoints are placed only there; manual ones outside stay.",
                "Где бывает игрок: прямоугольники в Scene view. Авто-точки ставятся только там; ручные вне зоны остаются.");
            Add("ui.vp.zone.rects", "{0} rectangle|{0} rectangles", "{0} прямоугольник|{0} прямоугольника|{0} прямоугольников");
            Add("ui.vp.panel.zone", "Only in the player zone", "Только в зоне игрока");
            Add("ui.vp.panel.zone.info", "{0} · {1} m²", "{0} · {1} м²");
            Add("ui.vp.panel.zone.none", "not set", "не задана");
            Add("ui.vp.panel.zone.edit", "Edit…", "Изменить…");
            Add("ui.vp.panel.zone.set", "Set…", "Задать…");
            Add("ui.vp.panel.zone.hint", "Every source only inside the zone; downward rays cover the zone, not the whole level.",
                "Все источники — только внутри зоны; лучи вниз идут по зоне, а не по всему уровню.");
            Add("ui.vp.panel.zone.hintNone", "Set… turns Player Zone on in the Scene view: rectangles where the player can be.",
                "«Задать…» включит «Зону игрока» в Scene view: прямоугольники там, где бывает игрок.");
            Add("ui.vp.sources.zone", "in the player zone: {0}, {1} m²", "в зоне игрока: {0}, {1} м²");
            Add("ui.vp.outside", "outside the zone", "вне зоны");
            Add("ui.vp.outside.tip", "The viewpoint is outside the player zone: a manual one stays when placing again, an automatic one goes.",
                "Точка вне зоны игрока: ручная останется при новой расстановке, автоматическая уйдёт.");
            Add("ui.vp.zone.title", "Player zone", "Зона игрока");
            Add("ui.vp.zone.count", "{0} · {1} m² · edit with the handles of the selected one", "{0} · {1} м² · правка — ручками на выбранном");
            Add("ui.vp.zone.empty", "No rectangles yet: add one under the Scene view camera, then drag its handles.",
                "Прямоугольников ещё нет: добавьте под камерой Scene view и тяните ручки.");
            Add("ui.vp.zone.selected", "Selected: zone {0}", "Выбрана: зона {0}");
            Add("ui.vp.zone.size", "Size, m", "Размер, м");
            Add("ui.vp.zone.turn", "Turn, °", "Поворот, °");
            Add("ui.vp.zone.height", "Height, m", "Высота, м");
            Add("ui.vp.zone.add", "+ Rectangle Under the Camera", "+ Прямоугольник под камерой");
            Add("ui.vp.zone.remove", "Remove", "Удалить");
            Add("ui.vp.zone.done", "Done", "Готово");
            Add("ui.vp.zone.label", "zone {0}", "зона {0}");
            Add("ui.vp.legend.zone", "player zone", "зона игрока");
            Add("ui.vp.legend.outside", "outside the zone", "вне зоны");
        }
    }
}
