# unity-tools — замысел и этапы

## SceneOptimizationAuditor

Задание для сессий. Промпт одной сессии = **«Общая часть»** + раздел **одного** этапа. Этапы идут по
порядку; каждый — своё изменение OpenSpec (`openspec/changes/<имя>`) и +1 ко второй цифре версии
(сейчас 0.1.0). Версию поднимает ведущая сессия, не исполнитель.

| Этап | Что | Версия |
|---|---|---|
| 1 | Окно, сбор данных, все статические проверки, бюджеты | 0.2.0 |
| 2 | Экспорт отчёта JSON / CSV | 0.3.0 |
| 3 | Исправления: пакетные безопасные и по одной записи | 0.4.0 |
| 4 | Снимок профайлера в Play Mode и сравнение | 0.5.0 |
| 5 | Развёртка под свет: галочка Generate Lightmap UVs, UV1, 0–1, наложения (владелец, 22.10.2026) | 0.6.0 |

---

### Общая часть

**Что делаем.** Editor-утилиту для Unity 2019.4 LTS: окно `SceneOptimizationAuditor` сканирует
загруженные сцены под PC VR (HTC Vive Pro, SteamVR) и выводит проблемы по уровням
Critical / Warning / Info с подсказкой, как исправить.

**Ограничения**
- Unity 2019.4 LTS, Built-in Render Pipeline, C# 7.3: без switch-выражений, `??=`, `using var`,
  индексов `^` и диапазонов, records, `is not`, `new()` без типа, nullable-ссылок; без API новее 2019.4.
  Сомнительный API сверять с документацией 2019.4, а не по памяти.
- Только встроенные модули Unity и .NET (`System.IO`, `System.Reflection`, `System.Linq` и т. п.).
  Без пакетов, плагинов и загрузок. Типы из пакетов и плагинов (Post Processing v2, SteamVR Plugin,
  XR Plugin Management, OpenVR XR Plugin) — только через reflection по имени типа; типа нет — проверка
  пропускается, и это видно в шапке отчёта.
- `UnityEditorInternal` и закрытые члены — только где без них нельзя: reflection, `try/catch`, запасной
  путь; сбой не роняет аудит.
- Устаревший API для legacy VR — под `#pragma warning disable 0618` в узком месте: утилита не добавляет
  предупреждений в консоль.
- Комментарии в коде — на английском. Всё, что пишется в консоль и лог, — на английском.
- Интерфейс — RU/EN: переключатель в окне, выбор хранится в `EditorPrefs`; все строки — в одном классе
  `Strings`. Тексты проблем (message, suggestion) хранятся ключом и аргументами и собираются при
  отрисовке на текущем языке.
- Утилита ничего не меняет в проекте и сценах без явного действия пользователя.

**Где лежит**
- Папка в корне репозитория: `SceneOptimizationAuditor/Editor/`, сборка
  `UnityTools.SceneOptimizationAuditor.Editor.asmdef` (только платформа Editor), пространство имён
  `UnityTools.SceneOptimization`.
- Подключение — копией папки `SceneOptimizationAuditor` в `Assets/` проекта. Код не зависит от того,
  где лежит папка (никаких путей `Assets/...`).
- Файлы по смыслу, каждый до ~600 строк: `AuditorWindow.cs`, `AuditIssue.cs`, `AuditContext.cs`,
  `VRBudgets.cs`, `XrInfo.cs`, `Strings.cs`, `Checks/<Категория>Checks.cs`; в следующих этапах —
  `ReportExporter.cs`, `AutoFixer.cs`, `ProfilerCapture.cs`.
- `.meta` создаёт Unity при импорте; коммитятся вместе с кодом.
- Документация — `docs/scene-optimization-auditor.md` (подключение, окно, список проверок с уровнями
  и порогами, что именно оценивается) и строка в оглавлении `docs/README.md`; правится в том же шаге,
  что и код.

**Проверка**
- Проверочный проект — `E:\dev_personal\unity-tools-scratch\TestProject2019` на Unity 2019.4.41f2
  (`C:\Program Files\Unity\Hub\Editor\2019.4.41f2\Editor\Unity.exe`). Папка утилиты подключена в его
  `Assets/` junction-ом на репозиторий, чтобы `.meta` ложились в репозиторий. Заводится в этапе 1.
- Компиляция — batchmode: `Unity.exe -batchmode -nographics -quit -projectPath <проект> -logFile <файл>`;
  судить по коду выхода и `error CS` в журнале, в разговор — хвост. UnityMCP — только если подключится
  к 2019.4.
- Не писать «проверено в редакторе» без слов владельца.

**Целевая платформа**
- HTC Vive Pro: панели 1440×1600 на глаз, но SteamVR при 100 % рендерит ~2016×2240 на глаз — fill rate
  критичен.
- 90 Гц: 11.1 мс на кадр; дольше — пропущенный кадр и репроекция. Цель — не больше ~10 мс CPU и GPU
  (запас под композитор SteamVR).
- Базовая видеокарта — GTX 1070 (8 ГБ).

**Бюджеты и пороги**
- Значения по умолчанию — в `VRBudgets`; переопределения проекта — `ProjectSettings/SceneOptimizationAuditor.json`
  (`JsonUtility`), правятся в окне (свёрнутый блок «Бюджеты», кнопка «По умолчанию»). Файл создаётся
  только при сохранении.
- Значения ниже — стартовые; калибровать профайлером на целевом ПК.

| Порог | Warning | Critical |
|---|---|---|
| Оценка draw calls за кадр (уже ×2 при multi-pass) | > 1000 | > 2000 |
| Треугольники сцены (LOD0, верхняя оценка) | > 2 000 000 | > 4 000 000 |
| Треугольники объекта без LODGroup | > 10 000 | > 25 000 |
| Память текстур (оценка) | > 1536 МБ | > 2048 МБ |
| Сторона текстуры | > 2048 | > 4096 |
| Realtime/Mixed источники с тенями | > 1 | > 2 |
| Активные Animator | > 20 | — |
| Треугольники SkinnedMesh под Animator | > 15 000 | — |
| Активные AudioSource | > 32 | — |
| Треугольники рендер-меша в MeshCollider | > 5 000 (Info) | — |
| Время кадра CPU / GPU (этап 4) | > 10 мс | > 11.1 мс |

**Модель данных**
- `AuditIssue`: `checkId` (стабильный, например `TEX_NO_MIPMAPS`), `category`, `severity` (enum),
  `target` (`UnityEngine.Object`: GameObject, ассет или `null` для настроек), `scene`, `objectPath`
  (`Root/Child/Name`) или `assetPath`, `settingsPath` (страница Project Settings), ключи и аргументы
  `message` / `suggestion`, `fixKind` (None / Safe / Review — заполняется в этапе 3), `fixAction` (`Action`).
- Проблема ассета — одна запись на ассет с числом использований (первые объекты — в подсказке), а не
  запись на каждый объект.
- Результаты живут в памяти окна; после перезагрузки домена список пуст с подсказкой «запустите аудит».

**Что сканируется** (`AuditContext`, собирается один раз за прогон)
- Все загруженные сцены (`SceneManager`); у проблемы — имя сцены. Объекты — включая неактивные
  (`GetComponentsInChildren(true)`); бюджеты и счётчики «активных» — только `activeInHierarchy` и
  `enabled`.
- Ассеты сцены — `EditorUtility.CollectDependencies` по корням сцен (попадают и ссылки из полей
  скриптов), плюс lightmaps, reflection probes и skybox.
- Настройки качества — уровень, который Standalone использует по умолчанию (`m_PerPlatformDefaultQuality`
  из `ProjectSettings/QualitySettings.asset` через `SerializedObject`); не вышло — текущий уровень.
  Какой проверен — в шапке.
- XR (`XrInfo`): legacy — `PlayerSettings.virtualRealitySupported`,
  `PlayerSettings.GetVirtualRealitySDKs(BuildTargetGroup.Standalone)`, `PlayerSettings.stereoRenderingPath`;
  XR Plug-in Management — через reflection: настройки Standalone, активные loader-ы (ищется OpenVR),
  режим stereo из настроек OpenVR XR Plugin. Итог в шапке: «Legacy VR (OpenVR)» /
  «XR Plug-in Management (OpenVR Loader)» / «VR не включён» и режим stereo.
- Меши без импортёра (процедурные, ProBuilder, встроенные примитивы) и ассеты из `Packages/` — проверки
  импортёра пропускаются.
- Прогон — с `EditorUtility.DisplayCancelableProgressBar`, сброс в `finally`.

**Архитектура**
- Категория — свой метод: `AuditDrawCalls(ctx)`, `AuditMeshes`, `AuditTextures`, `AuditLighting`,
  `AuditPhysics`, `AuditAnimators`, `AuditAudio`, `AuditVRSpecific`, `AuditGeneral`; все вызываются из
  `RunFullAudit()`. Новая проверка = блок в методе категории (или новый метод) + вызов + строка в docs.

---

### Этап 1 — окно и статические проверки (0.2.0)

Сделать: проверочный проект, `AuditContext`, все проверки ниже, окно. Экспорта, исправлений и
профайлера нет (и кнопок для них пока нет).

**1. Draw Calls и батчинг**
- Оценка draw calls: материальные слоты активных Renderer (+1 на ParticleSystemRenderer) + проход теней
  (слоты shadow casters × число Realtime/Mixed источников с тенями, у directional — × каскады); ×2 при
  multi-pass. Подпись «верхняя оценка: без батчинга и отсечения, без доп. проходов pixel lights» →
  бюджет.
- MeshRenderer с >1 материалом — Info с числом, > 4 — Warning. Материалов больше, чем submesh, —
  Warning (лишние проходы по последнему submesh).
- Кандидаты в Batching Static: MeshRenderer без флага `BatchingStatic`, у которых ни на себе, ни у
  предков нет Animator, Animation, Rigidbody и MonoBehaviour — Info; в подсказке — static batching
  добавляет память.
- GPU Instancing: больше 3 активных нестатичных объектов с одной парой меш + материал, у материала
  выключен `enableInstancing`, шейдер поддерживает инстансинг (способ проверки найти в 2019.4; не
  определить — Info «проверьте шейдер») — Warning. SkinnedMesh не учитывается.
- Shadow casters (`shadowCastingMode != Off`) — Info с общим числом (вклад уже в оценке).

**2. Меши**
- Треугольники сцены: LOD0 у объектов в LODGroup, остальные целиком, только активные → бюджет.
- Меш без LODGroup (на себе или у предка) выше порога объекта → Warning / Critical.
- Read/Write у модели (`ModelImporter.isReadable`) → Warning: память меша ×2. Меш у MeshCollider или
  ParticleSystem → Info: может быть нужен.
- `ModelImporter.meshOptimizationFlags` не `Everything` → Info.
- Больше 4 костей на вершину (`Mesh.GetBonesPerVertex`) → Warning: дороже скиннинг. `QualitySettings.skinWeights`
  уровня выше FourBones → Info.
- `PlayerSettings.gpuSkinning` выключен → Warning: на ПК скиннинг на GPU снимает нагрузку с CPU.

**3. Текстуры** (из материалов сцены по всем текстурным свойствам, UI, lightmaps, probes, skybox;
настройки — override Standalone, иначе по умолчанию)
- Сторона после импорта выше порога → Warning / Critical. Lightmaps и reflection probes — без проверки
  размера (только в памяти); cubemap и skybox — не выше Warning.
- Текстура в свойстве нормалей (`_BumpMap`, `_DetailNormalMap`; свойства с флагом Normal, если API
  есть) не типа Normal map → Critical.
- Без mipmaps → Warning: aliasing и мерцание в VR. Исключение — текстуры только у Screen Space
  канвасов.
- Несжатый формат (`RGBA32`, `RGB24`, `ARGB32` и т. п., `textureCompression == Uncompressed`) → Warning;
  подсказка по содержимому: DXT1 (BC1) без альфы, DXT5 (BC3) или BC7 с альфой и для качества, BC6H для
  HDR; нормали — тип Normal map со сжатием.
- Read/Write (`TextureImporter.isReadable`) → Warning: память ×2.
- `QualitySettings.anisotropicFiltering == Disable` → Info: полы и стены мерцают под углом. Текстуры с
  Point / Bilinear при включённых mipmaps — одна сводная запись Info с числом (Trilinear мягче
  переходит между mip).
- Память текстур — оценка по формату, размеру, mip (≈ ×1.33) и граням cubemap, каждый ассет один раз →
  бюджет.

**4. Освещение**
- Point / Spot с `lightmapBakeType == Realtime` → Warning.
- Realtime / Mixed источники с тенями — по порогу.
- Объекты с `ContributeGI` и `receiveGI == Lightmaps` без UV1 (`mesh.uv2` пуст и `generateSecondaryUV`
  выключен) → Warning.
- `shadowResolution` уровня VeryHigh → Warning, High → Info; `shadowDistance` > 50 м → Info.
- `pixelLightCount` > 1 → Warning: в Forward каждый дополнительный источник — ещё проход по объектам.
- Realtime GI (`Lightmapping.realtimeGI`) включён → Info.

**5. Физика**
- Невыпуклый MeshCollider при Rigidbody на себе или у предка: не kinematic → Critical (не
  поддерживается), kinematic → Info.
- MeshCollider на рендер-меше выше порога → Info: упрощённый меш коллизии или примитивы.
- `Physics.defaultSolverIterations` > 6 → Info.
- `Time.fixedDeltaTime` > 1/90 (с допуском) → Warning: предметы в руках дрожат относительно 90 Гц;
  < 1/90 → Info: лишние шаги физики.

**6. Аниматоры**
- `cullingMode == AlwaysAnimate` → Warning; в подсказке — риск CullCompletely (двери и платформы с
  коллайдерами, Animation Events, IK).
- Без `RuntimeAnimatorController` → Info (может назначаться скриптом).
- `applyRootMotion` без NavMeshAgent, CharacterController, Rigidbody и без скрипта с `OnAnimatorMove` →
  Info.
- SkinnedMeshRenderer с `updateWhenOffscreen` → Warning.
- Активные Animator — по порогу.
- SkinnedMeshRenderer под Animator выше порога треугольников → Warning (при выключенном GPU skinning —
  упомянуть CPU).

**7. Аудио** (клипы из AudioSource и зависимостей сцены; настройки — override Standalone, иначе по
умолчанию)
- `DecompressOnLoad` и длина > 10 с → Warning: Streaming для музыки и эмбиента, CompressedInMemory для
  средних звуков.
- PCM и размер > 1 МБ → Warning: Vorbis (~70 %) или ADPCM для коротких SFX.
- `preloadAudioData` и длина > 30 с → Warning.
- `spatialBlend == 0` → Info: если это звук мира — сделать 3D; музыка, UI и озвучка — норма.
- AudioSource без клипа → Info (может назначаться скриптом).
- Активные AudioSource — по порогу; в подсказке — сравнение с `AudioSettings.GetConfiguration().numRealVoices`.
- `priority == 0` больше чем у 5 источников → Warning.
- DSP Buffer Size проекта Default или Best Latency → Info: для VR — Good Latency.
- AudioListener (включённые, на активных объектах): > 1 → Critical; 0 → Warning (rig может создаваться
  в рантайме); не на объекте с камерой → Info.
- Режим динамиков проекта 5.1 / 7.1 и прочий Surround → Warning: в Vive Pro стерео.
- Spatializer не выбран (`AudioSettings.GetSpatializerPluginName()` пуст) → Info: HRTF улучшает
  локализацию звука в VR.

**8. VR**
- VR не включён ни в legacy, ни в XR Plug-in Management → Critical; включён без OpenVR → Warning;
  включены обе системы → Warning.
- Режим stereo не Single Pass Instanced → Warning: рекомендуется при разрешении Vive Pro; свои шейдеры
  должны поддерживать stereo instancing (`UNITY_VERTEX_INPUT_INSTANCE_ID`, `UNITY_VERTEX_OUTPUT_STEREO`).
- Rendering path Deferred (у камер или в tier settings Standalone) → Warning: MSAA не работает, stereo
  дорого.
- `antiAliasing` уровня < 4 → Warning; у VR-камеры выключен `allowMSAA` → Warning.
- VR-камеры — включённые Camera с `stereoTargetEye != None` и без `targetTexture`. Ни одной → Info
  (rig может создаваться в рантайме).
- `allowHDR` → Warning: память и пропускная способность на два глаза (нужен ли PP с HDR);
  `farClipPlane` > 1000 → Info (≤ 500, точность depth); `nearClipPlane` < 0.05 → Info (Unity не даёт меньше 0.01; порог поднят владельцем 22.10.2026).
- Canvas `ScreenSpaceOverlay` → Critical: не виден в шлеме; подсказка — World Space, масштаб ~0.001,
  `worldCamera`, VR-указатель. `ScreenSpaceCamera` → Warning: прилипает к голове.
- Post Processing v2 через reflection (`PostProcessLayer`, `PostProcessVolume` и включённые эффекты
  профиля) → Warning со списком; Motion Blur, Depth of Field, Chromatic Aberration, Screen Space
  Reflections, Ambient Occlusion отмечены как дорогие в VR. Скрипты на VR-камерах с `OnRenderImage` →
  Warning.
- SteamVR Plugin: типы есть в проекте, а в сценах нет `SteamVR_PlayArea`, `SteamVR_Behaviour_Pose`,
  `SteamVR_TrackedObject` → Info; типов нет → Info «SteamVR Plugin не найден».
- Reflection Probe Realtime → Warning; с `refreshMode == EveryFrame` → Critical.
- Нет запечённых данных occlusion culling при > 100 статичных рендерерах → Info.

**9. Общее**
- Пустые GameObject-листья (только Transform, без детей) → Info.
- Отрицательный scale → Info: ломает dynamic batching, переворачивает обход граней.
- Неактивные объекты с мешами или текстурами выше порогов → Info: всё равно грузятся со сценой.
- Глубина иерархии > 10 → Info.
- Пропавшие скрипты (`GameObjectUtility.GetMonoBehavioursWithMissingScriptCount`) → Warning.

**Окно**
- Меню `Tools/VR Scene Optimization Audit`.
- Шапка: «Target: HTC Vive Pro (PC VR, 90 FPS)», найденная XR-система и режим stereo, проверенный
  уровень качества, переключатель RU/EN.
- Кнопка «Run Audit» / «Запустить аудит».
- «Scene Budget Overview»: треугольники, draw calls, память текстур — полоса с цветом и подписью
  «оценка».
- Фильтр уровней (три переключателя) и поиск по имени объекта или пути ассета.
- Свёрнутые блоки категорий: Draw Calls, Meshes, Textures, Lighting, Physics, Animators, Audio,
  VR-Specific, General; в заголовке — счётчики со встроенными иконками `console.erroricon.sml`,
  `console.warnicon.sml`, `console.infoicon.sml` (не символы ⚠ ✕).
- Строка: иконка, текст, объект или ассет, подсказка под строкой. Клик: GameObject →
  `Selection.activeGameObject` + `EditorGUIUtility.PingObject`; ассет → `Selection.activeObject` + Ping;
  настройка → `SettingsService.OpenProjectSettings(...)`.
- В категории — первые 200 строк и «показать ещё».
- Внизу — сводка по уровням; блок «Бюджеты».

**Готово, когда**
- Проверочный проект на 2019.4.41f2 собирается в batchmode без ошибок и без предупреждений от утилиты.
- В проверочном проекте сцена `AuditSeed` создаётся кодом с заранее посеянными проблемами (хотя бы по
  одной на категорию, в том числе два AudioListener, Canvas ScreenSpaceOverlay, невыпуклый MeshCollider
  на Rigidbody, текстура без mipmaps). Метод проверочного проекта (не утилиты) через `-executeMethod`
  прогоняет аудит и пишет в журнал на английском число проблем по `checkId`; ожидаемый список — в
  отчёте этапа и совпадает с журналом.
- `docs/scene-optimization-auditor.md` и оглавление `docs/README.md` обновлены.

---

### Этап 2 — экспорт (0.3.0)

- Кнопка «Export Report» → `EditorUtility.SaveFilePanel`, формат по расширению: `.json` или `.csv`.
- JSON — `JsonUtility` и `[Serializable]` DTO (Newtonsoft в 2019.4 — пакет, нельзя):

```json
{
  "scenes": ["..."],
  "timestamp": "2026-10-21T14:05:00.0000000+03:00",
  "platform": "PC VR - HTC Vive Pro",
  "targetFPS": 90,
  "xr": { "system": "Legacy | XRManagement | None", "loader": "OpenVR", "stereoMode": "SinglePassInstanced" },
  "qualityLevel": "...",
  "summary": { "critical": 0, "warning": 0, "info": 0 },
  "sceneBudgets": {
    "isEstimate": true,
    "totalTris": 0,
    "trisLimit": 2000000,
    "estimatedDrawCalls": 0,
    "drawCallLimit": 1000,
    "textureMemoryMB": 0,
    "textureMemoryLimitMB": 1536
  },
  "hasProfilerSnapshot": false,
  "profilerSnapshot": { },
  "issues": [
    {
      "checkId": "AUDIO_DECOMPRESS_LONG",
      "category": "Audio",
      "severity": "Warning",
      "scene": "...",
      "objectPath": "Root/Child/ObjectName",
      "assetPath": "",
      "message": "...",
      "suggestion": "...",
      "autoFixable": false
    }
  ]
}
```

- Лимиты в `sceneBudgets` — пороги Warning из текущих бюджетов.
- `profilerSnapshot` до этапа 4 — пустой, `hasProfilerSnapshot: false` (`JsonUtility` не пишет `null`
  для вложенного класса).
- `checkId`, `category`, `severity`, `xr` — всегда по-английски; `message`, `suggestion` — на текущем
  языке окна. `timestamp` — `DateTime.Now.ToString("o")`.
- CSV: `CheckId, Category, Severity, Scene, ObjectPath, AssetPath, Message, Suggestion, AutoFixable`;
  экранирование по RFC 4180, UTF-8 с BOM (Excel и кириллица). Бюджеты и профайлер — только в JSON.
- Метод для batchmode `UnityTools.SceneOptimization.ReportExporter.RunFromCommandLine`: аргументы
  `-auditScene <путь>` и `-auditReport <файл.json|.csv>`, код выхода 0 / 1.
- В консоль — одна строка на английском: `Scene audit report saved: <path>`.

**Готово, когда:** отчёт по `AuditSeed` в обоих форматах снят в batchmode через `RunFromCommandLine`;
JSON читается обратно, в CSV столько строк, сколько проблем; docs обновлены.

---

### Этап 3 — исправления (0.4.0)

- Два вида:
  - **Safe** — кнопка «Auto-Fix Safe Issues» внизу окна: включить GPU Instancing у материала (шейдер
    поддерживает, пользователи нестатичны, ассет не из `Packages/` и доступен для записи); включить
    mipmaps у текстуры (кроме текстур только Screen Space канвасов).
  - **Review** — кнопка «Fix» у строки: выключить Read/Write (меш, текстура; не предлагается, если меш у
    MeshCollider или ParticleSystem); тип Normal map; сжатие по подсказке; `updateWhenOffscreen = false`;
    `cullingMode = CullUpdateTransforms`; `spatialBlend = 1`; Batching Static у кандидатов; загрузка и
    сжатие аудио по подсказке.
  - Без исправления: Canvas `ScreenSpaceOverlay` и настройки проекта (stereo, XR, качество, физика,
    аудио) — только подсказка и переход в Project Settings.
- Перед применением — своё окно подтверждения (не `DisplayDialog`): список изменений с галочками в двух
  группах — «Сцена (откатывается Undo)» и «Ассеты (меняются во всём проекте, Undo не откатывает,
  будет переимпорт)»; объекты в экземплярах префабов помечены «станет override».
- Сцена: `Undo.RecordObject`, одна группа Undo «Scene Audit Fix», `EditorSceneManager.MarkSceneDirty`.
- Ассеты: импортёры — между `AssetDatabase.StartAssetEditing` и `StopAssetEditing` в `try/finally`;
  материалы — `EditorUtility.SetDirty` и `AssetDatabase.SaveAssets`. Ассеты только для чтения
  пропускаются с пометкой.
- Каждое изменение — строка в консоли на английском; после применения — повторный аудит.

**Готово, когда:** в batchmode на копии `AuditSeed` после Safe-исправлений их `checkId` исчезают, а
остальные на месте; `Undo.PerformUndo` откатывает изменения сцены; docs обновлены.

---

### Этап 4 — снимок профайлера (0.5.0)

- Блок «Profiler Snapshot»: число кадров N (300), разогрев (60 кадров, не учитываются), кнопки
  «Capture Profiler Data» и «Compare with Previous».
- Не в Play Mode → войти, разогрев, N кадров, выйти. Уже в Play Mode → N кадров, остаться.
- Вход в Play Mode перезагружает домен: состояние захвата (шаг, счётчики, накопители) — в `SessionState`;
  ход ведёт статический класс с `[InitializeOnLoad]` через `EditorApplication.playModeStateChanged` и
  `EditorApplication.update`; окно только показывает. Работает и без перезагрузки домена (Enter Play
  Mode Options).
- Источники — сначала публичные и простые:
  - FPS (среднее, минимум, максимум) — `Time.unscaledDeltaTime`;
  - draw calls, SetPass, batches, треугольники, время main и render thread — `UnityEditor.UnityStats`
    (как в окне Stats);
  - GPU-время — `FrameTimingManager`, если даёт данные, иначе «n/a» (не 0);
  - память — `Profiler.GetTotalAllocatedMemoryLong`, `Texture.currentTextureMemory`, меши — сумма
    `Profiler.GetRuntimeMemorySizeLong` по загруженным Mesh;
  - `ProfilerDriver` — только если без него нельзя.
- В VR кадр задаёт композитор: время CPU брать без ожидания GPU / vsync; если источник ожидание
  включает — пометить метрику.
- Цвета — по порогам времени кадра: зелёный ≤ 10 мс, жёлтый ≤ 11.1 мс, красный выше; FPS — зелёный
  от 89.
- Надпись в блоке: «Замер в редакторе — ориентир; итоговые цифры — профайлер на Development Build».
- Последний и предыдущий снимки — JSON в `EditorPrefs` под ключом с путём проекта и сцены (переживают
  перезапуск Unity); «Compare with Previous» — дельта ↑↓ с цветом «лучше / хуже».
- Экспорт: `hasProfilerSnapshot: true` и `profilerSnapshot` — `avgFPS`, `minFPS`, `maxFPS`, `avgCPUms`,
  `peakCPUms`, `avgGPUms`, `peakGPUms` (−1 при «n/a»), `drawCalls`, `setPassCalls`, `batches`,
  `trisPerFrame`, `textureMemoryMB`, `meshMemoryMB`, `totalAllocatedMB`.

**Готово, когда:** собирается в 2019.4 без ошибок и предупреждений от утилиты; экспорт со снимком
проверен кодом на подставленных данных; захват в Play Mode в редакторе проверяет владелец.
