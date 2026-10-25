namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        static void AddScope()
        {
            // Scope of a run: the button, its menu and messages.
            Add("ui.scope.button", "Scope: {0}", "Охват: {0}");
            Add("ui.scope.tip", "What Run checks: the open scenes, the objects selected in the Hierarchy, every scene of the build or the assets of a folder.",
                "Что проверяет «Запустить»: открытые сцены, выделенные в Hierarchy объекты, все сцены сборки или ассеты папки.");
            Add("ui.scope.loaded", "Open Scenes", "Открытые сцены");
            Add("ui.scope.selection", "Selection ({0})", "Выделенное ({0})");
            Add("ui.scope.build", "Build Scenes ({0})", "Сцены сборки ({0})");
            Add("ui.scope.folder", "Folder {0}", "Папка {0}");
            Add("ui.scope.pickFolder", "Folder…", "Папка…");
            Add("ui.scope.pickFolder.title", "Folder to check", "Папка для проверки");
            Add("ui.scope.outside", "Pick a folder inside Assets of this project.", "Выберите папку внутри Assets этого проекта.");
            Add("ui.scope.noSelection", "Select objects in the Hierarchy first: the selection is checked with its children.",
                "Сначала выделите объекты в Hierarchy: выделенное проверяется вместе с детьми.");
            Add("ui.scope.noBuildScenes", "Build Settings have no enabled scenes.", "В Build Settings нет включённых сцен.");
            Add("ui.scope.noFolder", "The folder to check is gone: pick it again in the scope menu.", "Папки для проверки больше нет: выберите её снова в меню охвата.");
            Add("ui.scope.untitled", "Save the open scenes first: they are opened back after the check, and an untitled scene cannot be.",
                "Сначала сохраните открытые сцены: после проверки они открываются снова, а безымянную сцену открыть нельзя.");
            Add("ui.scope.progress", "Scene {0} of {1}: {2}", "Сцена {0} из {1}: {2}");
            Add("ui.scope.playMode", "Leave Play Mode first: the build scenes are opened one by one in the editor.",
                "Сначала выйдите из Play Mode: сцены сборки открываются в редакторе по одной.");
            Add("ui.scope.incomplete", "stopped: {0} of {1} checked", "прервано: проверено {0} из {1}");

            // The line under the verdict.
            Add("ui.scope.ctx.loaded", "Scope: open scenes — {0} · audited {1}", "Охват: открытые сцены — {0} · аудит {1}");
            Add("ui.scope.ctx.selection", "Scope: selection in {0} · audited {1}", "Охват: выделенное в {0} · аудит {1}");
            Add("ui.scope.ctx.build", "Scope: build scenes — {0} scenes (open: {1}) · audited {2}", "Охват: сцены сборки — сцен: {0} (открыты: {1}) · аудит {2}");
            Add("ui.scope.ctx.scenes", "Scope: scenes one by one — {0} scenes (open: {1}) · audited {2}", "Охват: сцены по одной — сцен: {0} (открыты: {1}) · аудит {2}");
            Add("ui.scope.ctx.folder", "Scope: folder {0} · audited {1}", "Охват: папка {0} · аудит {1}");
            Add("ui.scope.selection.note", "Objects selected: {0} (with children — {1}). Project settings and whole-scene totals are not shown — they come with a scene check.",
                "Выделено объектов: {0} (с детьми — {1}). Настройки проекта и счётчики всей сцены не показываются — они при проверке сцен.");
            Add("ui.scope.folder.note",
                "Prefabs {0}  ·  models {1}  ·  materials {2}  ·  textures {3}  ·  clips {4}. Project settings, scene totals and assets outside the folder are not shown; objects of prefabs are fixed in the prefab by hand.",
                "Префабов {0}  ·  моделей {1}  ·  материалов {2}  ·  текстур {3}  ·  клипов {4}. Настройки проекта, счётчики сцены и ассеты вне папки не показываются; объекты префабов исправляются в префабе вручную.");
            Add("ui.scope.worst", "Budget bars — the worst scene of the check.", "Полосы бюджета — худшая сцена проверки.");

            // The table of scenes.
            Add("ui.scope.scenes", "Scenes", "Сцены");
            Add("ui.scope.col.scene", "Scene", "Сцена");
            Add("ui.scope.col.critical", "Critical", "Critical");
            Add("ui.scope.col.critical.short", "C", "C");
            Add("ui.scope.col.warning", "Warning", "Warning");
            Add("ui.scope.col.warning.short", "W", "W");
            Add("ui.scope.col.info", "Info", "Info");
            Add("ui.scope.col.tris", "Triangles", "Треугольники");
            Add("ui.scope.col.tris.short", "Tris", "Треуг.");
            Add("ui.scope.col.tris.tip", "Triangles of the scene, LOD0 (— for scenes checked together with other open scenes).",
                "Треугольники сцены, LOD0 («—» у сцен, проверенных вместе с другими открытыми).");
            Add("ui.scope.col.dc", "Draw calls", "Draw calls");
            Add("ui.scope.col.dc.short", "DC", "DC");
            Add("ui.scope.col.mb", "Textures, MB", "Текстуры, МБ");
            Add("ui.scope.millions", " M", " М");
            Add("ui.scope.open", "Open", "Открыть");
            Add("ui.scope.open.tip", "Opens the scene (offering to save the open ones) and checks it again, so its rows can be fixed.",
                "Открывает сцену (предложив сохранить открытые) и проверяет её снова, чтобы её записи можно было исправить.");
            Add("ui.scope.isOpen", "open", "открыта");
            Add("ui.scope.showAll", "Show all ({0})", "Показать все ({0})");
            Add("ui.scope.showFewer", "Show fewer", "Показать меньше");

            // Rows of the list.
            Add("ui.scope.openScene", "Open Scene", "Открыть сцену");
            Add("ui.scope.openScene.tip", "The scene is closed: opens it (offering to save the open ones), checks it again and selects the object.",
                "Сцена закрыта: открыть её (предложив сохранить открытые), проверить снова и выделить объект.");
            Add("ui.scope.closed", "scene closed", "сцена закрыта");
            Add("ui.scope.inScenes", "in {0} scenes", "сцен: {0}");
        }
    }
}
