namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        // "Shadows" on the Lighting tab: the point, the radius, what to turn off beyond it.
        static void AddShadows()
        {
            Add("ui.lt.seg.shadows", "Shadows", "Тени");
            Add("ui.sh.summary", "Objects: {0}  ·  beyond {1} m: {2}  ·  to change: {3}", "Объектов: {0}  ·  дальше {1} м: {2}  ·  изменятся: {3}");
            Add("ui.sh.summary.noPoint", "Objects: {0}  ·  the point is not set", "Объектов: {0}  ·  точка не поставлена");
            Add("ui.sh.summary.search", "  ·  search: {0}", "  ·  поиск: «{0}»");
            Add("ui.sh.sub", "Meshes at any depth that cast or receive shadows or contribute to GI. Distance — from the point to the nearest point of the object’s bounds. The point and the radius are kept with the viewpoints of these scenes; the handle and the radius circle are in the Scene view.",
                "Меши на любой глубине, которые отбрасывают или получают тени или участвуют в GI. Расстояние — от точки до ближайшей точки границ объекта. Точка и радиус хранятся с точками обзора этих сцен; ручка точки и круг радиуса — в Scene view.");
            Add("ui.sh.footer", "\"Turn Off\" clears the ticked settings through the confirmation window; Ctrl+Z brings them back. Cast Shadows decides the baked shadow too: with the box on, baked objects (Contribute GI, lit from lightmaps) keep it. Without Contribute GI an object leaves the lightmaps and is lit from probes after a rebake. Shadows Only objects keep Cast Shadows. The search looks at the object and every parent.",
                "«Выключить» снимает отмеченное через окно подтверждения, Ctrl+Z возвращает. Cast Shadows действует и на запечённую тень: с флажком у запекаемых (Contribute GI, свет из lightmap) он остаётся. Без Contribute GI объект выпадает из lightmap и после перезапекания освещается пробами. У Shadows Only Cast Shadows остаётся. Поиск — по имени объекта и всех его родителей.");
            Add("ui.sh.point", "Point:", "Точка:");
            Add("ui.sh.point.value", "X {0}  ·  Y {1}  ·  Z {2}", "X {0}  ·  Y {1}  ·  Z {2}");
            Add("ui.sh.point.none", "not set — put it at the centre of the Scene view", "не поставлена — поставьте её в центр Scene view");
            Add("ui.sh.center", "Put at View Centre", "Поставить в центр вида");
            Add("ui.sh.center.tip", "The point goes to the centre of the last Scene view; then drag its handle there. Ctrl+Z moves it back.",
                "Точка встаёт в центр последнего Scene view; дальше её двигает ручка там. Ctrl+Z возвращает.");
            Add("ui.sh.show", "Show in Scene View", "Показать в Scene view");
            Add("ui.sh.what", "Turn off beyond the radius:", "Снимать у дальних:");
            Add("ui.sh.cast", "Cast Shadows", "Cast Shadows");
            Add("ui.sh.cast.tip", "Cast Shadows = Off: the object casts no shadow, realtime or baked.", "Cast Shadows = Off: объект не отбрасывает тень — ни в реальном времени, ни запечённую.");
            Add("ui.sh.receive", "Receive Shadows", "Receive Shadows");
            Add("ui.sh.receive.tip", "Receive Shadows off: realtime shadows do not fall on the object; baked ones stay in its lightmap.",
                "Receive Shadows снят: тени в реальном времени на объект не падают; запечённые остаются в его lightmap.");
            Add("ui.sh.gi", "Contribute GI", "Contribute GI");
            Add("ui.sh.gi.tip", "The static flag Contribute GI off: the object leaves the lightmaps and the bounce; after a rebake it is lit from light probes.",
                "Статичный флаг Contribute GI снят: объект выпадает из lightmap и отражённого света; после перезапекания освещается пробами.");
            Add("ui.sh.radius", "Radius, m", "Радиус, м");
            Add("ui.sh.keepBaked", "Keep Cast Shadows on baked", "Запекаемым оставить Cast Shadows");
            Add("ui.sh.keepBaked.tip", "Objects with Contribute GI lit from lightmaps keep Cast Shadows: without it their baked shadow goes too after a rebake.",
                "Объекты с Contribute GI и светом из lightmap оставляют Cast Shadows: без него после перезапекания пропадёт и их запечённая тень.");
            Add("ui.sh.offAll", "Turn Off Beyond {0} m ({1})", "Выключить дальше {0} м ({1})");
            Add("ui.sh.offAll.tip", "The ticked settings off on every found object beyond the radius, through the confirmation window. Ctrl+Z brings them back.",
                "Снять отмеченное у всех найденных объектов дальше радиуса — через окно подтверждения. Ctrl+Z возвращает.");
            Add("ui.sh.offOne", "Turn Off", "Выключить");
            Add("ui.sh.offOne.tip", "The ticked settings off on this object, wherever it is.", "Снять отмеченное у этого объекта, где бы он ни был.");
            Add("ui.sh.search.tip", "A part of the name of the object or of any of its parents: a group finds every mesh in it.",
                "Часть имени объекта или любого его родителя: группа находит все меши внутри.");
            Add("ui.sh.col.distance", "Distance, m", "Расстояние, м");
            Add("ui.sh.col.distance.short", "Dist., m", "Расст., м");
            Add("ui.sh.col.tris", "Triangles", "Треугольники");
            Add("ui.sh.col.mode", "Cast", "Отбрасывает");
            Add("ui.sh.col.receive", "Receives", "Получает");
            Add("ui.sh.col.receive.short", "Rec.", "Получ.");
            Add("ui.sh.col.gi", "GI", "GI");
            Add("ui.sh.mode.on", "On", "Вкл.");
            Add("ui.sh.mode.two", "Two Sided", "Двусторонние");
            Add("ui.sh.mode.only", "Shadows Only", "Только тень");
            Add("ui.sh.mode.off", "Off", "Выкл.");
            Add("ui.sh.mark.far", "beyond the radius", "дальше радиуса");
            Add("ui.sh.mark.baked", "baked", "запекается");
            Add("ui.sh.mark.only", "shadow only", "только тень");
            Add("ui.sh.none", "No enabled mesh in the loaded scenes casts or receives shadows or contributes to GI.",
                "В загруженных сценах ни один включённый меш не отбрасывает и не получает тени и не участвует в GI.");
            Add("ui.sh.noneFound", "Nothing found.", "Ничего не найдено.");
            Add("ui.sh.legend", "Shadows by distance", "Тени по удалённости");
            Add("ui.sh.legend.radius", "radius from the point", "радиус от точки");
            Add("ui.sh.legend.off", "the ticked settings turn off", "выключится отмеченное");
            Add("ui.sh.legend.baked", "baked — Cast Shadows kept", "запекается — Cast Shadows остаётся");
            Add("ui.sh.legend.note", "Shown while Shadows on the Lighting tab is open. The handle moves the point; Ctrl+Z moves it back.",
                "Видно, пока открыт сегмент «Тени» на «Свете». Ручка двигает точку, Ctrl+Z возвращает.");
            Add("ui.sh.scene.label", "Shadows: {0} m  ·  to change: {1}", "Тени: {0} м  ·  изменятся: {1}");
            Add("SHADOW_FAR.title", "Far from the point", "Далеко от точки");
            Add("SHADOW_FAR", "{1} off: {0} m from the point.", "Выключить {1}: {0} м от точки.");
            Add("SHADOW_FAR.do", "Turn off {1} ({0} m from the point)", "Снять {1} ({0} м от точки)");
        }
    }
}
