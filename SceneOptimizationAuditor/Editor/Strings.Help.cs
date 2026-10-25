namespace UnityTools.SceneOptimization
{
    public static partial class Strings
    {
        // Texts of the Help menu, "Getting started" and the card "More about the check".
        static void AddHelp()
        {
            Add("ui.help", "Help", "Справка");
            Add("ui.help.tip", "Getting started, help on this tab, the guide and the check reference.",
                "С чего начать, справка по вкладке, руководство и справочник проверок.");
            Add("ui.help.start", "Getting started", "С чего начать");
            Add("ui.help.tab", "Help on the {0} tab (in the browser, Russian)", "Справка по вкладке «{0}» (в браузере)");
            Add("ui.help.guide", "Guide (in the browser, Russian)", "Руководство (в браузере)");
            Add("ui.help.reference", "Check reference (in the browser, Russian)", "Справочник проверок (в браузере)");

            Add("ui.start.back", "Back to the Result", "Вернуться к результату");
            Add("ui.start.gpu", "GPU", "Видеокарта");
            Add("ui.start.gpu.text", "Budget thresholds: {0} · {1} triangles · {2} MB of textures.",
                "Пороги бюджетов: {0} · {1} треугольников · {2} МБ текстур.");
            Add("ui.start.gpu.custom", "your thresholds", "свои пороги");
            Add("ui.start.gpu.change", "Change…", "Изменить…");
            Add("ui.start.gpu.change.tip", "Opens the Budgets tab: the target GPU and the thresholds.",
                "Открывает вкладку «Бюджеты»: целевая видеокарта и пороги.");
            Add("ui.start.scope", "Scope", "Охват");
            Add("ui.start.scope.text", "What to check: the open scenes, the selection, the build scenes or a folder.",
                "Что проверять: открытые сцены, выделенное, сцены сборки или папку.");
            Add("ui.start.run", "Run", "Запуск");
            Add("ui.start.run.text", "A few seconds, nothing is changed. Then: Auto-Fix Safe Issues and By gain.",
                "Несколько секунд, ничего не меняет. Потом — «Исправить безопасное» и «По выигрышу».");
            Add("ui.start.tasks", "Or pick a task", "Или выберите задачу");
            Add("ui.start.first", "First pass over a level", "Первый проход по уровню");
            Add("ui.start.first.text", "Turns on By gain and runs the audit: what frees the most comes first.",
                "Включит «По выигрышу» и запустит аудит: сверху — что даст больше всего.");
            Add("ui.start.fps", "The level misses 90 FPS", "Уровень не держит 90 FPS");
            Add("ui.start.fps.text", "Opens Viewpoints: where on the level it is heavy to look; then a tour in Play Mode.",
                "Откроет «Точки обзора»: где на уровне тяжело смотреть; потом — замер в Play Mode.");
            Add("ui.start.memory", "Out of GPU memory", "Не хватает памяти видеокарты");
            Add("ui.start.memory.text", "Opens Heaviest on Textures; Collect shows which prefabs and models take the texture memory.",
                "Откроет «Тяжёлые» на «Текстурах»; «Собрать» покажет, какие префабы и модели занимают память текстур.");
            Add("ui.start.build", "Before a build", "Перед сборкой");
            Add("ui.start.build.text", "Checks the build scenes one by one: the table of scenes shows where it is worst.",
                "Проверит сцены сборки по очереди: таблица сцен покажет, где хуже всего.");
            Add("ui.start.build.none", "No enabled scenes with files in Build Settings.", "В Build Settings нет включённых сцен с файлами.");
            Add("ui.start.guide", "The whole guide (in the browser, Russian)", "Руководство целиком (в браузере)");

            Add("ui.help.more", "More about the check", "Подробнее о проверке");
            Add("ui.help.less", "Hide details", "Скрыть подробности");
            Add("ui.help.more.tip", "What is checked, why it matters in VR, how to fix it by hand and when it may stay.",
                "Что проверяется, почему это важно в VR, как исправить вручную и когда можно оставить.");
            Add("ui.help.what", "What is checked", "Что проверяется");
            Add("ui.help.why", "Why it matters in VR", "Почему это важно в VR");
            Add("ui.help.how", "How to fix by hand", "Как исправить вручную");
            Add("ui.help.gain", "Gain", "Выигрыш");
            Add("ui.help.keep", "When it may stay", "Когда можно оставить");
        }

        // The card of a check: what, why, how, gain (empty when the check has no gain estimate) and keep, each in English and Russian.
        static void Help(string id, string whatEn, string whatRu, string whyEn, string whyRu, string howEn, string howRu,
            string gainEn, string gainRu, string keepEn, string keepRu)
        {
            Add(id + ".help.what", whatEn, whatRu);
            Add(id + ".help.why", whyEn, whyRu);
            Add(id + ".help.how", howEn, howRu);
            if (gainEn.Length > 0) Add(id + ".help.gain", gainEn, gainRu);
            Add(id + ".help.keep", keepEn, keepRu);
        }
    }
}
