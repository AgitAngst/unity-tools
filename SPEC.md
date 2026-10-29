# unity-tools — замысел и этапы

## SceneOptimizationAuditor

Задание для сессий. Промпт одной сессии = **«Общая часть»** + раздел **одного** этапа. Этапы идут по
порядку; каждый — своё изменение OpenSpec (`openspec/changes/<имя>`) и +1 ко второй цифре версии
(текущая — в `HANDOFF.md`); этап без нового поведения — третья цифра. Версию поднимает ведущая сессия, не исполнитель.

| Этап | Что | Версия |
|---|---|---|
| 1 | Окно, сбор данных, все статические проверки, бюджеты | 0.2.0 |
| 2 | Экспорт отчёта JSON / CSV | 0.3.0 |
| 3 | Исправления: пакетные безопасные и по одной записи | 0.4.0 |
| 4 | Снимок профайлера в Play Mode и сравнение | 0.5.0 |
| 5 | Развёртка под свет: галочка Generate Lightmap UVs, UV1, 0–1, наложения (владелец, 22.10.2026) | 0.6.0 |
| 6 | Вкладка «Развёртка под свет»: все модели сцены, «включить у всех без UV1», CSV (владелец, 22.10.2026) | 0.7.0 |
| 7 | Проверки 3D-ассетов: модели, меши, текстуры, материалы (владелец, 22.10.2026) | 0.8.0 |
| 8 | Выбор нескольких записей и действия над выбранным (владелец, 23.10.2026) | 0.9.0 |
| 9 | Допустимые проблемы и отключённые проверки (владелец, 24.10.2026) | 1.0.0 |
| 10 | История прогонов и сравнение (владелец, 24.10.2026) | 1.1.0 |
| 11 | Окно по файлам: разрезать `AuditorWindow.cs` и `Strings.cs`, без нового поведения (владелец, 24.10.2026) — `split-auditor-window` | 1.1.1 |
| 12 | Точки обзора: оценка в редакторе того, что видно из мест игрока; выбор целевой видеокарты RTX 2060 / 2070 / 2080 / 3060, по умолчанию 3060 (владелец, 24.10.2026) — `add-viewpoints` | 1.2.0 |
| 13 | Точки обзора: замер по точкам в Play Mode (владелец, 24.10.2026) — `add-viewpoint-tour` | 1.3.0 |
| 14 | Вкладка «Свет»: атласы lightmap, объекты в них, источники, пробы; 12 проверок света (владелец, 24.10.2026: «Вкладка «Свет»») — `add-light-tab` | 1.4.0 |
| 15 | Исходники: копии префаба в списке вместе, «Исправить в префабе», вкладка «Тяжёлые» (владелец, 24.10.2026: «Префабы и «Тяжёлые»») — `add-heavy-sources` | 1.5.0 |
| 16 | Частицы: лимиты, крупные прозрачные, модули Lights и Collision, тени, instancing мешей (владелец, 25.10.2026: «делай частицы и шейдер и reflection probe») — `add-particle-checks` | 1.6.0 |
| 17 | Шейдеры: ошибки, GrabPass, лишние проходы, stereo instancing при Single Pass Instanced, варианты, alpha-test без alpha-to-coverage (там же) — `add-shader-checks` | 1.7.0 |
| 18 | Reflection probes: разрешение, память, не запечены, без box projection, дубликаты, объекты вне проб; раздел «Отражения» на вкладке «Свет» (там же) — `add-reflection-probes` | 1.8.0 |
| 19 | Охват проверки: выделенное, сцены сборки, папка ассетов, несколько сцен в командной строке (владелец, 25.10.2026) — `add-audit-scope` | 1.9.0 |
| 20 | Оценка выигрыша у записей (МБ, draw calls, треугольники) и сортировка «что даст больше всего» (владелец, 25.10.2026: «делай оценку выйгрыша») — `add-fix-gain` | 2.0.0 |
| 21 | Справка в окне: кнопка «Справка» с меню, «С чего начать», «Подробнее о проверке» у 30 проверок (владелец, 25.10.2026) — `add-window-help` | 2.1.0 |
| 22 | Настройки качества под VR: Linear, мягкие тени, realtime-пробы и Soft Particles в Quality, VSync и targetFrameRate; пороги MSAA, дальности теней и Skin Weights по советам для RTX 2070 (владелец, 27.10.2026) — `add-quality-checks` | 2.2.0 |
| 23 | Целевой шлем: Vive Pro, Vive Pro 2, Vive Pro 2 Wireless, Index — частота и разрешение SteamVR; под шлем — время кадра, цвета FPS, шаг физики, поле зрения точек, порог прозрачного (владелец, 27.10.2026) — `add-target-headset` | 2.3.0 |
| 24 | Трава и террейн: трава вокруг игрока, Grass Billboard, Detail Distance, Draw Instanced, Pixel Error (владелец, 27.10.2026) — `add-terrain-grass` | 2.4.0 |
| 25 | Допустимыми — любые записи: галочка у всех строк (владелец, 27.10.2026); пометка записи сцены — для её сцены (решение сессии) — `accept-any-issue` | 2.5.0 |
| 26 | Тени по удалённости от точки: сегмент «Тени» на «Свете», точка и радиус, снять Cast Shadows, Receive Shadows, Contribute GI у дальних, поиск по родителям; история — каждый прогон точка (владелец, 27.10.2026) — `add-shadow-zone` | 2.6.0 |
| 27 | Запуск внизу окна, прогон не пропадает при перезагрузке скриптов, быстрая перепроверка записи (владелец, 27.10.2026) — `add-quick-recheck` | 2.7.0 |
| 28 | Бюджет кадра по точкам обзора: полосы и проверки треугольников и draw calls — по худшей точке, итог всей сцены справочно (владелец, 28.10.2026) — `update-frame-budget` | 2.8.0 |
| 29 | Конкретика и исправления: виновники вариантов шейдера, замена простых Standard на мобильный шейдер, трилинейная фильтрация по текстурам с кнопкой (владелец, 29.10.2026) — `update-specific-advice` | 2.9.0 |

Terrain не проверялся по слову владельца 24.10.2026; 27.10.2026 он попросил проверку травы на террейне — этап 24.

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
- Видеокарта — на выбор RTX 2060, 2070, 2080, 3060 и 4060, по умолчанию RTX 2070 (8 ГБ) — с 2.1.2 (владелец,
  27.10.2026: «Добавь в пресеты видео карты 2070 и 4060, 2070 сделай по дефолту»); с этапа 12 до 2.1.2 — по умолчанию
  RTX 3060, до этапа 12 — пороги под GTX 1070 (8 ГБ).

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
  уровня выше FourBones → Info (с 2.2.0 — выше TwoBones, этап 22).
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
- `shadowResolution` уровня VeryHigh → Warning, High → Info; `shadowDistance` > 50 м → Info (с 2.2.0 — > 40 м, этап 22).
- `pixelLightCount` > 1 → Warning: в Forward каждый дополнительный источник — ещё проход по объектам.
- Realtime GI (`Lightmapping.realtimeGI`) включён → Info.

**5. Физика**
- Невыпуклый MeshCollider при Rigidbody на себе или у предка: не kinematic → Critical (не
  поддерживается), kinematic → Info.
- MeshCollider на рендер-меше выше порога → Info: упрощённый меш коллизии или примитивы.
- `Physics.defaultSolverIterations` > 6 → Info.
- `Time.fixedDeltaTime` > 1/90 (с допуском) → Warning: предметы в руках дрожат относительно 90 Гц (с 2.3.0 — 1 / частота шлема, этап 23);
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
- `antiAliasing` уровня < 4 → Warning (с 2.2.0 — только выключен, 8x — Info, этап 22); у VR-камеры выключен `allowMSAA` → Warning.
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

---

### Этап 11 — окно по файлам (1.1.1)

Изменение OpenSpec `split-auditor-window`. `AuditorWindow.cs` — 1635 строк, `Strings.cs` — 818 при правиле
«файл до ~600»; вкладка этапа 12 без разреза ляжет туда же. Поведение и вид не меняются, поэтому версия —
третья цифра (1.1.1), а этапы с точками обзора остаются 1.2.0 и 1.3.0, как назвал владелец.

- `AuditorWindow` — `partial class` по файлам, код переносится как есть (без переименований и правки логики):
  - `AuditorWindow.cs` — поля, стили, `Open`, `OnEnable`/`OnDisable`, `OnGUI`, тулбар, вкладки, `Run`, экспорт
    и общие помощники (цвета, `Tint`, `Padded`, `FormatNumber`, `XrLine`);
  - `AuditorWindow.Problems.cs` — вкладка «Проблемы»: пустое состояние, вывод, карточки, контекст, панель списка,
    полоса выбора, группы, строки, `Select`;
  - `AuditorWindow.Lightmap.cs`, `AuditorWindow.Budgets.cs` (полосы бюджетов, вкладка, допустимые),
    `AuditorWindow.History.cs`, `AuditorWindow.Profiler.cs`;
  - `IssueGroup` — свой файл `IssueGroup.cs`.
- `Strings` — `partial class` по разделам: `Strings.cs` (язык, `Get`, `Format`), `Strings.Ui.cs` (тексты окна),
  `Strings.Checks.cs` (тексты проверок); таблица, ключи, `Get` и `Format` не меняются.
- Каждый файл — до ~600 строк. `.meta` новых файлов создаёт импорт в проверочном проекте (junction), они
  коммитятся.

**Готово, когда:** `compile-check` без ошибок и предупреждений; `run-audit-tests.ps1` — 29 из 29; снимки окна
(`shoot-audit-window.ps1`) до и после совпадают попиксельно; раздел «Устройство» страницы утилиты и HANDOFF
называют новые файлы, ссылки колонки «Где» в `docs/ui-guideline.md` остаются верными.

Сделано 24.10.2026, вместе с тестовым сервером (`scripts\audit-server.ps1`; владелец: «Один раз открой и тестируй
там постоянно»): тесты и снимки — командами в одном открытом редакторе, `ReportExporter.RunWithArgs` — экспорт
командной строки без выхода из редактора.

---

### Этап 12 — точки обзора: оценка в редакторе (1.2.0)

Изменение OpenSpec `add-viewpoints`. Бюджеты сейчас — сумма по всей сцене (LOD0, без отсечения): на большом
уровне число не говорит, где тяжело. Пороги draw calls и треугольников задуманы на кадр — на то, что видно из
одного места. Этап считает их из мест, где бывает игрок, и показывает худшие места уровня.

**Целевая видеокарта** (владелец, 24.10.2026: «по умолчанию целевая видеокарта 3060», «Добавь 2060, 2070, 2080
и 3060»).
- На вкладке «Бюджеты» — выбор «Целевая видеокарта»: RTX 2060 (6 ГБ), RTX 2070 (8 ГБ), RTX 2080 (8 ГБ), RTX 3060
  (12 ГБ). Без файла бюджетов — RTX 3060.
- Выбор подставляет пороги набора в таблицу (дальше их можно править); «По умолчанию» возвращает пороги выбранной
  карты; «Сохранить» пишет и карту (`targetGpu`) в `ProjectSettings/SceneOptimizationAuditor.json`. Пороги правлены
  руками — рядом с картой «свои пороги».
- Карта — в строке контекста вкладки «Проблемы» и в отчёте JSON (`targetGpu`).
- Проект с сохранённым файлом без `targetGpu` остаётся на своих порогах, карта у него — «свои пороги».
- Наборы — стартовые, предложены сессией, калибровать профайлером на целевом ПК:

| Порог (Warning / Critical) | RTX 2060 | RTX 2070 | RTX 2080 | RTX 3060 |
|---|---|---|---|---|
| Треугольники сцены и кадра | 2 500 000 / 5 000 000 | 3 000 000 / 6 000 000 | 3 500 000 / 7 000 000 | 3 000 000 / 6 000 000 |
| Память текстур, МБ | 1152 / 1536 | 1536 / 2048 | 1536 / 2048 | 2304 / 3072 |
| Прозрачное в кадре, % экрана (новый) | 100 / 200 | 100 / 200 | 100 / 200 | 100 / 200 |

С 2.1.2 (владелец, 27.10.2026) — ещё RTX 4060 (8 ГБ): треугольники 3 500 000 / 7 000 000 (по скорости — как RTX 2080),
память текстур 1536 / 2048 МБ; без файла бюджетов — RTX 2070. Изменение OpenSpec `update-gpu-presets`.

Память текстур — та же доля памяти карты, что у прежних порогов под GTX 1070 (19 % / 25 % от 8 ГБ); треугольники —
по скорости карты относительно GTX 1070 (2 000 000 / 4 000 000). Draw calls, тени, время кадра и остальные пороги у
всех карт прежние: draw calls упираются в CPU, время кадра — в 90 Гц. GTX 1070 в выборе нет.

**Точки**
- «Расставить точки ▾» — панель под кнопкой (`PopupWindow`): источники галочками (у каждого — что найдено), шаг
  сетки, высота глаз, пояснение, «Отмена» и «Расставить (≈ N)». Источники:
  - NavMesh — узлы сетки с шагом 4 м (1–20), попавшие на треугольники `NavMesh.CalculateTriangulation` (по одному на
    этаж), плюс высота глаз 1,7 м;
  - телепорты SteamVR (reflection по имени типа): `TeleportPoint` — точка, `TeleportArea` — сетка тем же шагом
    по её коллайдеру;
  - камеры сцены — включённые Camera, кроме VR-камеры: позиция и направление;
  - нет NavMesh и телепортов — сетка лучом вниз (`Physics.Raycast`) по границам рендереров, поверхности с
    наклоном до 45°.
- Вручную: «Добавить отсюда» — позиция и направление камеры Scene view; перетаскивание ручкой в Scene view;
  «Удалить». Перетащенная авто-точка становится ручной; повторная расстановка заменяет только авто-точки и сбрасывает
  оценку у всех.
- Не больше 500 авто-точек (ручные не в счёт): больше — шаг сетки растёт, сводка об этом пишет.
- Хранение — `SceneAuditPoints/<сцена>-<хэш>.json` в корне проекта рядом с `Assets/` (набор сцен и хэш — как у
  истории), `JsonUtility`; сцена не меняется, папку можно коммитить. В файле: настройки расстановки, точки (id,
  имя, вид, позиция, направление у ручных и камер), последняя оценка (время, версия утилиты, числа по точкам).

**Оценка точки**
- 8 направлений по 45° (у ручных точек и камер — ещё своё), наклон 0. Одна пирамида на оба глаза: по вертикали
  110°, соотношение сторон 0,9 (с 2.3.0 — поле зрения целевого шлема, этап 23), near 0,05, far — у VR-камеры сцены, иначе 1000; маска
  слоёв и `layerCullDistances` VR-камеры, если она есть.
- Видимые — активные рендереры, чьи `bounds` пересекают пирамиду (`GeometryUtility.TestPlanesAABB`), ближе
  расстояния отсечения своего слоя. Terrain не учитывается.
- LOD — уровень LODGroup выбирается как в Unity: размер группы × наибольший масштаб, расстояние до опорной
  точки, поле зрения, `QualitySettings.lodBias` и `maximumLODLevel`; ниже последнего перехода группа не
  рисуется. Рендереры других уровней не считаются.
- Треугольники — сумма по видимым мешам.
- Draw calls — слоты материалов видимых (+1 у частиц, линий, спрайтов); доп. проходы pixel lights — у объекта
  столько Realtime/Mixed point и spot, чьи range задевают его `bounds`, но не больше Pixel Light Count; проходы
  теней — directional: shadow casters ближе shadow distance к точке × каскады, point (×6) и spot (×1): casters в
  range, если range задевает пирамиду; ×2 при VR в stereo Multi Pass или Single Pass.
- Прозрачное — сумма долей экрана, которые закрывают `bounds` видимых прозрачных (очередь от 2501) и частиц
  (проекция 8 углов, обрезка по экрану), в %; больше 100 % — слои overdraw.
- «Учитывать стены» (по умолчанию выключено): объект скрыт, если лучи от глаза к центру и углам его `bounds`
  упираются в коллайдеры других объектов (`Physics.Raycast`, без триггеров). Без коллайдеров на стенах
  оценка завышена — подпись об этом.
- Итог точки — худшее направление по draw calls (при равенстве — по треугольникам); числа всех направлений
  хранятся.
- Данные рендереров собираются один раз; 500 точек × 5 000 рендереров — до ~10 с, полоса хода с отменой.
- Не учитываются (подпись во вкладке): батчинг и instancing, occlusion culling Unity, объекты, которые появятся
  в игре, стоимость шейдеров и GPU — это верхняя оценка геометрии и прозрачности.
- Прогон аудита оценку не запускает — у неё своя кнопка.

**Вкладка «Точки обзора»** — вторая, после «Проблем»; вид — вкладка-таблица по `docs/ui-guideline.md` и доске
https://claude.ai/artifact/5rYUsiTnTkFuqLNtSC6Bui (владелец принял 24.10.2026: «Доска подходит, новые правила вида
принимаю»).
- Пояснение мелко; тулбар: «Оценить» (значок Play), «Расставить точки ▾», «Добавить отсюда», «Сохранить CSV»,
  справа «Учитывать стены» и «Только сверх бюджета»; на узком окне — в две строки.
- До первой расстановки — пустое состояние: «Точек обзора пока нет», что сделает утилита и что сцена не меняется,
  крупная кнопка «Расставить точки…», что найдено (NavMesh, телепорты, камеры) и подсказка про «Добавить отсюда».
- Сводка: «Точек N · сверх бюджета K · худшая: <имя> — 1 450 draw calls».
- Таблица, худшие сверху, первые 20 строк и «Показать ещё 20»: «Точка» (ссылка), «Вид», «Треуг.», «Draw calls»,
  «Прозрачное», «Направление»; число —
  цветом по порогам, у строк сверх бюджета полоса слева; на ширине 560 ничего не обрезано (имя многоточием,
  полное — в подсказке).
- Клик по строке — Scene view встаёт в точку и смотрит в худшем направлении (`SceneView.LookAt`); настройки
  Scene view (поле зрения) не меняются.
- У выбранной точки под таблицей: «Направления» — 8 строк (азимут, треугольники, draw calls, прозрачное; клик —
  смотреть туда) и «Что здесь тяжёлое» — первые 10 по треугольникам и по draw calls в показанном направлении
  (худшем, если не выбрано другое; без проходов теней),
  сгруппированные по исходному префабу или модели (число копий), с метками «далеко, а LOD0», «нет LODGroup»,
  «мелкий, но с тенью», «крупный прозрачный»; клик — выделить объекты.
- Scene view (пока открыта вкладка, `SceneView.duringSceneGui`): точки шариками — зелёный / жёлтый / красный по
  худшему значению относительно порогов, серый — не оценена; у выбранной — стрелка показанного направления, контур
  пирамиды и ручка перемещения.
- Галочки, как в других таблицах: «Оценить выбранные», «Удалить выбранные».

**Правила вида для всего окна** (владелец, 24.10.2026, `docs/ui-guideline.md`) — входят в код этим этапом:
- подписи вкладок не обрезаются: равные, пока самая длинная помещается, иначе каждая по ширине подписи
  (`GUI.ToolbarButtonSize.FitToContents`); на окне уже 640 версия и `EN | RU` — во второй строке тулбара;
- текст цветом уровня — свой оттенок на скин (контраст 4,5:1) на всех вкладках; константы уровней остаются для
  полос, точек, подложек и значков;
- выбранная строка таблицы — подложка цвета выделения Unity.

**Отчёт, история, командная строка**
- JSON: `hasViewpoints` и `viewpoints` — время оценки, пороги, точки (имя, вид, позиция, худшее направление,
  треугольники, draw calls, прозрачное, сверх бюджета). CSV вкладки: `Point, Kind, X, Y, Z, Yaw, Tris, DrawCalls,
  TransparentPercent, OverBudget`.
- История: оценка новее прошлой точки — худшие значения и число точек сверх бюджета; строки-графики на вкладке
  «История».
- `-auditViewpoints` — оценка по сохранённым точкам (нет файла — авто-расстановка по NavMesh и телепортам) в отчёт,
  с `-auditHistory` — и в историю.

**Готово, когда:** тесты на посеянной сцене — уровень LOD на известных расстояниях, видимость за пирамидой,
draw calls с тенями и pixel lights, доля прозрачного, авто-сетка по NavMesh, запечённому в тесте, телепорты
(заглушки SteamVR), файл точек туда и обратно, `-auditViewpoints`, наборы видеокарт и старый файл бюджетов;
`run-audit-tests.ps1`
зелёный; снимки вкладки RU/EN; страница утилиты, гайдлайн, TESTPLAN, HANDOFF обновлены; версия 1.2.0. Живая
проверка на настоящем уровне — за владельцем.

Сделано 24.10.2026: тесты 32 из 32, снимки вкладки (пустая, RU, EN, ширина 560) и «Бюджетов» RU/EN. Не проверено
вживую: панель «Расставить точки» (снимок окна её не захватывает), отметки и ручка в Scene view, светлый скин.

---

### Этап 13 — точки обзора: замер в Play Mode (1.3.0)

Изменение OpenSpec `add-viewpoint-tour`. Оценка этапа 12 — без батчинга, occlusion culling и шейдеров. Замер
проходит по тем же точкам в Play Mode и даёт настоящее время кадра: где кадр дольше 11,1 мс.

- «Замерить в Play Mode» на вкладке «Точки обзора»; какие точки — «Худшие по оценке» (30, от 1 до 500),
  «Выбранные», «Все». Параметры в `EditorPrefs`: кадров на направление (30), разогрев (15), направлений (8 или 4).
- Ход — как у снимка профайлера: статический класс с `[InitializeOnLoad]`, шаг и номер точки в `SessionState`,
  `EditorApplication.update`. Не в Play Mode — войти, замерить, выйти; в Play Mode — остаться. Кого двигать, ищется
  после 30 кадров успокоения: игра успевает создать rig и игрока. «Отмена»; выход из Play Mode или перекомпиляция
  прерывают замер, измеренные точки сохраняются с пометкой «прерван».
- Что двигать — выбор:
  - «VR rig (авто)» — корень rig VR-камеры (включённая Camera с Target Eye ≠ None): rig сдвигается и
    поворачивается вокруг головы так, чтобы голова встала в точку и смотрела в нужную сторону — саму камеру
    перебивает трекинг шлема;
  - «Объект…» — указанный объект (путь в иерархии: объекты Play Mode — копии сцены, GlobalObjectId их не находит);
  - «Камера» — сама камера (сцены без VR).
- На время замера у двигаемого Rigidbody — kinematic, CharacterController выключен; это изменения Play Mode, они
  пропадают при выходе; подпись об этом.
- Метрики на направление, средние за N кадров: время кадра (`Time.unscaledDeltaTime`) — среднее и пик, CPU
  (`UnityStats.frameTime`), GPU (со шлемом — `XRStats.TryGetGPUTimeLastFrame`, без него или если рантайм не дал — `FrameTimingManager`; нет
  данных — −1), пропущенные кадры (`XRStats.TryGetDroppedFrameCount`), draw calls, batches, SetPass, треугольники
  (`UnityStats`).
- Со шлемом кадр привязан к 90 Гц: время кадра около 11,1 мс и при запасе, и без него. Поэтому итог точки со
  шлемом — худшее направление по пропущенным кадрам, затем по большему из CPU и GPU; без шлема — по времени кадра.
- Шлем: `XRSettings.enabled` и `XRSettings.isDeviceActive` хоть в одном кадре замера — замер в стерео; нет — моно в
  Game view, пометка «без шлема» у замера и в сводке.
- Время — до старта и по ходу («осталось ~2 мин»): (30 кадров успокоения + направления всех точек × (разогрев +
  кадры)) / 90.
- Вкладка: сегменты «Оценка | Замер» над таблицей; в «Замере» колонки «Кадр, мс», «Пик, мс», «CPU», «GPU», «Пропущено»,
  «Draw calls», «SetPass», цвет времени — по порогам времени кадра; сводка «Замер: дольше 11,1 мс или с пропусками —
  K из N», под ней мелко дата, «со шлемом» / «без шлема» и параметры; у выбранной точки — «Оценка ↔ замер» (draw calls,
  треугольники и SetPass; у оценки SetPass нет).
- Хранение: замер — в файле точек (время, версия утилиты, шлем, параметры, числа); JSON — `viewpointTour`;
  история — худшее время кадра и число точек дольше 11,1 мс.
- Надпись: «Замер в редакторе — ориентир; итоговые цифры — профайлер на Development Build».

**Готово, когда:** тесты на подставленных данных — сдвиг rig (голова в точке, направление), смена точек и
направлений по шагам, сохранение прерванного замера, отчёт и история; `run-audit-tests.ps1` зелёный; страница
утилиты, гайдлайн, TESTPLAN, HANDOFF обновлены; версия 1.3.0. Замер в Play Mode со шлемом и без — проверяет
владелец.

Сделано 24.10.2026: тесты замера на подставленных кадрах и короткий замер в Play Mode в тестовом редакторе (3 точки × 4
направления, без шлема, rig создаётся игрой на 5-м кадре), снимки «Замера» (со шлемом RU/EN, без шлема, ширина 560). Не проверено вживую: замер
со шлемом (GPU и пропущенные кадры от SteamVR), перенос настоящего rig, панель «Замер в Play Mode» и полоса хода (снимок
их не захватывает), светлый скин. Доски «Замер» владелец принял 24.10.2026 («Доски «Замер» подходят»).

---

### Этап 14 — вкладка «Свет» (1.4.0)

Изменение OpenSpec `add-light-tab`. Владелец 24.10.2026 выбрал следующим этапом «Вкладку «Свет»» из предложений
анализа. Свет сейчас — 6 проверок в `LightingChecks.cs` и проверки UV1: не видно, сколько памяти съели lightmaps, кто
их занимает, что не запечено, где движущимся объектам не хватает проб и какой источник светит на полсцены.

- Вкладка «Свет» — после «Развёртки под свет». «Проверить свет» (значок Play) — собирает данные, сцену не меняет.
  Сегменты «Атласы | Объекты | Источники | Пробы», у каждого своя сводка и таблица; справа в тулбаре — «Только с
  пометками»; «Открыть Lighting» — окно Lighting Settings.
- **Атласы:** `LightmapSettings.lightmaps` — номер, размер, формат, МБ (цвет + направление + shadowmask, оценка как у
  текстур), объектов, заполнено (сумма долей объектов). Над таблицей — настройки запекания 2019.4 (`LightmapEditorSettings`:
  lightmapper, Lightmap Size, разрешение, сжатие, Mixed Lighting; `LightmapSettings.lightmapsMode`;
  `QualitySettings.shadowmaskMode` уровня Standalone по умолчанию, как у проверок). Клик по атласу — выделить его объекты.
- **Объекты в lightmap** (Contribute GI и Receive GI = Lightmaps, активные): атлас, доля атласа (`lightmapScaleOffset`
  x·y), Scale in Lightmap, размер (большая сторона bounds), пометки «мелкий — на пробы» (меньше 0,5 м), «много атласа»
  (больше 20 %), «без lightmap» (индекс вне атласов). Исправление «На пробы» — Receive GI = Light Probes (Undo; свет надо
  перезапечь); выбор нескольких и «Перевести на пробы (N)», «Выделить в Unity (N)».
- **Источники:** тип, режим (Realtime / Mixed / Baked), тени, дальность, объектов в зоне (bounds пересекают сферу
  дальности; у directional — все), Important, запечён ли (`bakingOutput.isBaked`).
- **Пробы:** Light Probe Groups и число проб; объекты на пробах (Light Probes = Blend Probes, не в lightmap: подвижные и
  Receive GI = Light Probes) вне области проб (коробка всех проб + 1 м) — с расстоянием до ближайшей пробы.
- Проверки на «Проблемах», в отчёте и истории — 12 новых:
  - `LIGHT_LIGHTMAP_MEMORY` — память lightmaps больше порога (новый бюджет «Память lightmaps, МБ»: 128 / 256);
  - `LIGHT_LIGHTMAP_UNCOMPRESSED` — lightmaps без сжатия → Warning; `LIGHT_LIGHTMAP_DIRECTIONAL` — направленные
    lightmaps (у запечённых есть текстуры направления), память ×2 → Info (обе — настройка «Lighting»);
  - `LIGHT_NOT_BAKED` — объекты для lightmap без lightmap или Baked/Mixed источники без запекания (при включённом Baked
    GI) → Warning, настройка «Lighting»;
  - `LIGHT_SMALL_IN_LIGHTMAP` — мелкий объект в lightmap → Info, исправление «Light Probes»;
  - `LIGHT_LIGHTMAP_SHARE` — объект больше 20 % атласа → Warning;
  - `LIGHT_NO_PROBES` — есть запечённый свет (атласы или любой Baked / Mixed источник) и объекты на пробах, но нет
    Light Probe Group → Warning;
  - `LIGHT_OUTSIDE_PROBES` — объект на пробах вне области проб → Info;
  - `LIGHT_DISTANCE_SHADOWMASK` — Distance Shadowmask при Mixed-источниках: тени статики в реальном времени до
    Shadow Distance → Warning;
  - `LIGHT_SHADOW_CASCADES` — 4 каскада у directional-источника с тенями в реальном времени → Warning;
  - `LIGHT_WIDE_REALTIME` — point / spot Realtime или Mixed, в зоне больше 50 объектов → Warning;
  - `LIGHT_IMPORTANT` — point / spot с Render Mode Important не по Pixel Light Count → Warning.
- Отчёт JSON — `lighting` (атласов, МБ и порог, направленные, сжатие, объектов в lightmap, без lightmap, проб, групп,
  объектов на пробах вне проб); история — память lightmaps, график и сравнение.
- Кнопка «Настройки» у проверок «Lighting» открывает окно Lighting Settings.

**Готово, когда:** тесты на подставленных атласах (без запекания: `LightmapSettings.lightmaps`, `lightmapIndex`,
`lightmapScaleOffset`, пробы, источники) — память и доли, мелкие и «много атласа», «без lightmap», источники в зоне,
вне проб, все 12 проверок, исправление и Undo, отчёт и история; `run-audit-tests.ps1` зелёный; доска вкладки на
холсте, снимки RU/EN; страница утилиты, гайдлайн, TESTPLAN, HANDOFF; версия 1.4.0. Живая проверка на запечённом
уровне — за владельцем.

Сделано 24.10.2026: тест `light` на подставленных атласах (все 12 проверок на одной сцене, `LIGHT_NO_PROBES` — после
удаления группы, Critical памяти, «ничего не запечено» с Baked GI и без, тишина на чистой сцене, исправление и Undo,
отчёт, точка истории), снимки вкладки (до проверки, «Атласы», «Объекты», «Пробы» RU, «Источники» EN, ширина 560). Не проверено вживую: запечённый уровень,
окно Lighting по «Настройкам» и «Открыть Lighting», светлый скин. Доски «Свет» владелец принял 24.10.2026 («Доски «Свет» подходят»).

---

### Этап 15 — исходники: копии вместе, исправление в префабе, «Тяжёлые» (1.5.0)

Изменение OpenSpec `add-heavy-sources`. Владелец 24.10.2026 выбрал «Префабы и «Тяжёлые»» из предложений анализа.
Проблема привязана к объекту сцены: одна ошибка в префабе даёт 500 записей, а «Исправить» у копии делает override
вместо правки префаба. И не видно, какие исходники (префабы, модели, меши) дают больше всего треугольников, draw calls и
памяти текстур.

- **Копии вместе** (переключатель «Копии вместе» в панели списка «Проблем», по умолчанию включён, `EditorPrefs`): в
  раскрытой группе записи копий одного объекта префаба или модели (один объект-источник,
  `PrefabUtility.GetCorrespondingObjectFromSource`) — одной строкой «<файл префаба> › <путь внутри>» и мелко «копий: N»; ссылка
  выделяет все копии, стрелка раскрывает их по одной; галочка строки берёт все её записи.
- **«Исправить в префабе»** у такой строки, если в ней все записи этой проверки у копий источника (не урезаны поиском и
  выбором, ни одной допустимой, все копии на месте), исправления меняют объекты сцены и префаб можно править (в `Assets/`,
  не модель, `IsPartOfImmutablePrefab` = нет): исправление выполняется на одной копии, изменённые свойства (разница
  `GetPropertyModifications` до и после) уходят в префаб (`ApplyPropertyOverride`); копии, у которых это свойство
  своё (`prefabOverride`), исправляются на себе; не изменилось ни одно свойство префаба — исправляется каждая копия. Меняются
  все экземпляры префаба в проекте; окно подтверждения — группа «Ассеты», «… — в префабе <путь>: меняются все его экземпляры
  в проекте; копий в сценах: N». Ctrl+Z откатывает префаб и копии вместе.
  «Исправить все (N)» у группы и «Исправить выбранные (N)» при включённом «Копии вместе» тоже берут исправление в
  префабе для таких строк.
- **Вкладка «Тяжёлые»** (после «Света»): «Собрать» (значок Play, сцену не меняет) — исходники загруженных сцен:
  префаб (ближайший корень копии), иначе модель или меш, иначе объект; колонки «Исходник» (клик — выделить копии,
  ссылка — ассет), «Вид», «Копий», «На копию» и «Треуг. всего», «Draw calls» (по материалам, без теней и света),
  «Матер.», «Текст., МБ» (текстуры его материалов; общие считаются у каждого), «Доля» — доля сцены по выбранной мере; всё —
  только LOD0; метка «нет LOD» (у копии больше порога объекта и рендерер вне LODGroup). Сегменты «Треугольники | Draw calls |
  Текстуры» — сортировка; первые 20 и «Показать ещё»; «Сохранить CSV»; «Только без LOD».
- Отчёт JSON — `heavy`: 20 исходников с наибольшим числом треугольников (имя, вид, путь ассета, копий, треугольники,
  draw calls, МБ текстур).

**Готово, когда:** тесты на сцене с копиями префаба и модели — сворачивание по источнику, исправление в префабе
(префаб изменён, копии без своего override исправлены, со своим — исправлены на себе, модель — без «в префабе»),
Undo сцены; сбор «Тяжёлых» (копии, треугольники LOD0, draw calls, материалы, МБ, «нет LOD»), CSV и JSON;
`run-audit-tests.ps1` зелёный; доски на холсте, снимки RU/EN; страница утилиты, гайдлайн, TESTPLAN, HANDOFF; версия
1.5.0. Живая проверка на уровне владельца — за ним.

Сделано 24.10.2026: тест `heavy` (30 копий префаба — одна строка и одно исправление в префабе, копия со своим значением
исправлена на себе, Ctrl+Z откатывает префаб и копии; часть копий, допустимая или удалённая копия — по копиям; копии
модели без «в префабе»; «Тяжёлые» на 40 копиях, свободных мешах и LODGroup; CSV и JSON), снимки
(копии RU/EN, «Тяжёлые» до сбора, RU, EN, ширина 560). Не проверено вживую: уровень владельца, вложенные префабы и
варианты, светлый скин. Доски «Копии» и «Тяжёлые» владелец принял 24.10.2026 («Доски «Копии» и «Тяжёлые» подходят»).

---

### Этап 16 — частицы (1.6.0)

Изменение OpenSpec `add-particle-checks`. Владелец 25.10.2026: «делай частицы и шейдер и reflection probe». Частицы в VR —
перерисовка (overdraw) крупными прозрачными спрайтами на весь экран, реалтайм-свет модуля Lights, коллизии с миром и
тени; сейчас аудитор их не видит. Новая категория «Частицы».

- Включённые ParticleSystem загруженных сцен (неактивные — нет):
  - частиц одновременно — оценка: Max Particles, но не больше Rate over Time × Start Lifetime (у незацикленной — не
    дольше Duration) + Bursts; у систем с Rate over Distance, с выключенной Emission и у суб-эмиттеров — Max Particles;
    большой лимит при слабой эмиссии не шумит;
  - `PART_MAX_PARTICLES` — оценка больше 1000 → Warning, больше 5000 → Critical;
  - `PART_TOTAL` — сумма оценок всех систем больше 20 000 → Warning (на сцену);
  - `PART_LARGE_TRANSPARENT` — прозрачный материал (render queue от 2501), Billboard / Stretch / Horizontal / Vertical, наибольший размер (Start Size × Size over Lifetime × масштаб) от 1 м,
    частиц одновременно от 20 → Warning: перерисовка; в тексте — размер и Max Particle Size рендерера;
  - `PART_LIGHTS` — модуль Lights включён и в нём задан Light → Warning, исправление «выключить модуль Lights»;
  - `PART_COLLISION_WORLD` — модуль Collision с миром (World) и качеством High → Warning, исправление «качество
    Medium» (Medium и Low сталкиваются только со статичными коллайдерами — в тексте исправления);
  - `PART_SHADOWS` — рендерер частиц отбрасывает тени → Warning, исправление «Cast Shadows = Off»;
  - `PART_MESH_NO_INSTANCING` — частицы-меши без GPU Instancing → Info, исправление «включить GPU Instancing»;
  - `PART_NOISE_HIGH` — модуль Noise с качеством High → Info.
- Исправления — по строке после просмотра (Review), Undo; для копий префаба работают «Копии вместе» и «Исправить в
  префабе».
- Пороги — константы `VRBudgets` (1000 / 5000, 20 000, 1 м, 20).

**Готово, когда:** тест на сцене с системами частиц — каждая проверка срабатывает на своей системе и молчит на
обычной, исправления и Undo; `run-audit-tests.ps1` зелёный; страница утилиты, TESTPLAN, HANDOFF; версия 1.6.0.

Сделано 25.10.2026: тест `particles` (каждая проверка на своей системе, тишина на обычной и неактивной, оценка частиц по
эмиссии, Bursts, суб-эмиттерам и Duration, размер с Size over Lifetime и масштабом; четыре исправления и Undo; пять копий
префаба — одно исправление в префабе), тексты RU/EN. Не проверено вживую: эффекты уровня владельца.

---

### Этап 17 — шейдеры (1.7.0)

Изменение OpenSpec `add-shader-checks`. Шейдеры материалов включённых рендереров загруженных сцен; исходник читается у
`.shader` в `Assets/` (и их `#include` из `Assets/`), у встроенных и пакетных — только то, что даёт API. Новая категория
«Шейдеры» после «Материалов»; запись — на шейдер, с числом объектов. Исправлений нет — правится исходник.

- `SHADER_ERROR` — ошибки, которые Unity уже знает (`ShaderUtil.ShaderHasError`), или шейдер не поддерживается
  (`isSupported`) → Critical: розовые объекты; `SHADER_MISSING` — у материала нет шейдера (`Hidden/InternalErrorShader`,
  запись — на материал) → Critical.
- `SHADER_GRABPASS` — в первом SubShader GrabPass без имени текстуры → Warning: копия экрана на каждый объект, в VR — для
  двух глаз; с именем (`GrabPass { "_Name" }`) → Info: копия раз за кадр.
- `SHADER_MULTIPASS` — в первом SubShader больше одного прохода рисования → Warning: объект рисуется N раз. Считаются
  проходы без LightMode или с Always, проходы ForwardBase (нет их — больший из Vertex / VertexLM / VertexLMRGBM), UsePass (кроме теней по префиксу SHADOW
  и имён META, DEFERRED, PREPASS*, FORWARD_DELTA, FORWARDADD, MOTIONVECTORS) и один за surface-шейдер; ForwardAdd, ShadowCaster, Meta, Deferred, Prepass,
  MotionVectors — нет.
- `SHADER_NO_STEREO` — stereo-режим Single Pass Instanced, вершинная функция (`#pragma vertex`) определена в исходнике
  проекта, а в нём нет вызова `UNITY_SETUP_INSTANCE_ID` или `UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO` → Critical: объект
  виден одним глазом. Так выглядит шаблон Unlit Shader из 2019.4. Surface-шейдеры и функции из встроенных `.cginc`
  (`vert_img`) — не в счёт.
- `SHADER_VARIANTS` — у шейдера проекта вариантов больше 512 (`ShaderUtil.GetVariantCount` через reflection; нет
  метода — молчит) → Info: дольше сборка и загрузка.
- `SHADER_ALPHA_TEST_MSAA` — MSAA включён, материал alpha-test (RenderType = TransparentCutout), в шейдере нет
  `AlphaToMask On` (у встроенных — считается, что нет) → Info: края листвы мерцают; alpha-to-coverage сглаживает их
  при MSAA.

**Готово, когда:** тест на шейдерах, которые тест пишет в проект (GrabPass, контур вторым проходом, шаблон Unlit без
stereo, cutout без AlphaToMask и с ним, 1024 варианта, с ошибкой разбора), и на Standard — проверки срабатывают и
молчат; `run-audit-tests.ps1` зелёный; страница утилиты, TESTPLAN, HANDOFF; версия 1.7.0.

Сделано 25.10.2026: тест `shaders` (разбор исходника на строках; 11 шейдеров, которые тест пишет в проект, Standard
opaque и cutout, материал с удалённым шейдером; при Single Pass Instanced и MSAA 4x и без них), тексты RU/EN. Не
проверено вживую: шейдеры уровня владельца и Single Pass Instanced в шлеме.

Исправлено 1.7.1 (сверка судьёй): `SHADER_NO_STEREO` смотрит на вызовы макросов, а не на поля (своя функция в `v2f_img`
больше не ложный Critical); UsePass исключается по точному имени (`METALLIC_OUTLINE` рисует); AlphaToMask на `Category`
виден; пропавший шейдер — своя проверка `SHADER_MISSING`.

---

### Этап 18 — reflection probes (1.8.0)

Изменение OpenSpec `add-reflection-probes`. Reflection probes — кубические текстуры: память и швы отражений. Сейчас
есть только `VR_PROBE_REALTIME` и `VR_PROBE_EVERY_FRAME`.

- Проверки (категория «Освещение»):
  - `REFL_RESOLUTION` — разрешение больше 256 → Warning, от 1024 → Critical;
  - `REFL_MEMORY` — память всех проб больше 64 МБ → Warning;
  - `REFL_NOT_BAKED` — проба Baked без запечённой текстуры или Custom без своей → Warning;
  - `REFL_NO_BOX` — без box projection при размере пробы до 30 м (комната) → Info, исправление «включить Box
    Projection»;
  - `REFL_DUPLICATE` — две пробы почти совпадают: общая часть больше 80 % объёма каждой → Warning (на меньшую); малая
    проба внутри большой (комната на улице) — не дубликат (сверка судьёй 25.10.2026);
  - `REFL_OUTSIDE` — объекты с Reflection Probes ≠ Off вне всех проб, когда пробы есть → Info (на сцену): отражают
    окружение (по умолчанию — небо).
- **Раздел «Отражения»** на вкладке «Свет» (пятый сегмент): сводка «Проб: N · M МБ · без box projection: K · не
  запечено: L · объектов вне проб: P»; таблица «Проба» (ссылка), «Режим», «Разрешение», «HDR», «МБ», «Box», «Размер, м»,
  «Объектов» (внутри), «Включить box» у проб с меткой «без box»; метки «дубликат», «без box», «не запечена»; ниже —
  «Объекты вне reflection probes» с расстоянием до ближайшей пробы, ближние первыми.
- JSON — в `lighting`: `reflectionProbes`, `reflectionMB`, `outsideReflection`.

**Готово, когда:** доска раздела «Отражения» на холсте; тест на пробах — проверки, исправление, раздел, JSON;
`run-audit-tests.ps1` зелёный; страница утилиты, гайдлайн, TESTPLAN, HANDOFF; версия 1.8.0.

Сделано 25.10.2026: тест `reflections` (восемь проб — данные, все шесть проверок, «Включить box» и Undo, тишина без проб и с
хорошей пробой, JSON), снимки раздела «Отражения» (RU, EN, ширина 560). Доски «Отражения» https://claude.ai/artifact/5rYUsiTnTkFuqLNtSC6Bui
владелец принял 25.10.2026 («Доски «Отражения» подходят»). Не проверено вживую: запечённый уровень владельца.

---

### Этап 19 — охват проверки (1.9.0)

Изменение OpenSpec `add-audit-scope`. Владелец 25.10.2026: «Делай охват проверки этапом 1.9.0: выделенный участок, все
сцены из Build Settings, папка ассетов, несколько сцен в командной строке». Сейчас окно проверяет загруженные сцены
целиком, командная строка — одну сцену.

- **Выбор охвата в окне** — кнопка «Охват: <что> ▾» справа от «Запустить» (меню, у текущего — галочка): «Открытые
  сцены» (как сейчас), «Выделенное (N)» (объекты сцены, выделенные в Hierarchy, с детьми; нет выделения — пункт
  недоступен), «Сцены сборки (N)» (включённые сцены Build Settings; нет — недоступен), «Папка…» (выбор папки внутри
  `Assets/`) и последняя папка. Выбор запоминается (`EditorPrefs`), «Запустить» проверяет выбранное. Строка под итогом
  называет охват. На окне уже 640 — «Исправить безопасное», «Сохранить отчёт», версия и язык во второй строке тулбара.
- **Выделенное** — обычный прогон по открытым сценам, в списке — только проблемы выделенных объектов (с детьми) и ассетов,
  которые они используют; проблемы настроек проекта и всей сцены (target нет) не показываются; полосы бюджета скрыты,
  вместо них — «Выделено объектов: N (с детьми — M)».
- **Сцены сборки** — изменённые сцены: предложение сохранить (отказ — проверки нет); безымянная сцена — сообщение и
  стоп. Каждая неоткрытая сцена сборки открывается одна, проверяется; затем открываются сцены, что были, и открытые
  сцены сборки проверяются последними, как «Открытые сцены» (их записи можно исправлять). Полоса хода «Сцена i из N:
  <имя>» с отменой (отмена — вернуть сцены, показать проверенное). Итог: таблица «Сцены» над списком — сцена,
  Critical / Warning / Info, треугольники, draw calls, МБ текстур, «Открыть» (у открытых — «открыта»); записи закрытой
  сцены без «Исправить» — «Открыть сцену» (предложение сохранить, затем выделить объект по пути); одна и та же проблема
  ассета из нескольких сцен — одна запись, использований — сумма; одинаковые проблемы настроек проекта — одна запись.
  Полосы бюджета — худшая сцена. Точка истории — у каждой сцены своя.
- **Папка** — проверка ассетов папки (с подпапками) без сцены: префабы и модели ставятся в скрытую сцену
  (`NewPreviewScene`), свободные материалы — на quad, свободные текстуры и клипы — как ассеты. В списке — проблемы
  ассетов папки и объектов её префабов и моделей («<файл> › <путь>», выделение — объект в префабе); исправления — только у
  ассетов (импорт, материал), у объектов префаба — подсказка; ассеты вне папки, настройки и счётчики сцены не
  показываются; полосы бюджета скрыты, вместо них — «Префабов N · моделей · материалов · текстур · клипов».
- **Командная строка:** `-auditScene` — несколько сцен через запятую или точку с запятой, или флаг несколько раз;
  `-auditBuildScenes` — все включённые сцены сборки; `-auditFolder Assets/…` — папка. Каждая сцена проверяется одна;
  `-auditHistory` — точка у каждой сцены, `-auditViewpoints` — у каждой сцены.
- **JSON:** `scope` (`loaded` | `selection` | `buildScenes` | `scenes` | `folder`), `folder`, `sceneDetails` (у каждой
  сцены: `path`, `critical`, `warning`, `info`, `tris`, `drawCalls`, `textureMB`; поле `scenes` — имена — остаётся); у
  записи — `scenePath`; `sceneBudgets` при нескольких сценах — худшая. CSV — столбец `ScenePath` после `Accepted` (с 2.0.0 за ним — столбцы выигрыша).
- Вкладки «Точки обзора», «Развёртка под свет», «Свет», «Тяжёлые» по-прежнему смотрят открытые сцены.

**Готово, когда:** доски «Охват» на холсте; тесты: выделенное (только свои объекты и их ассеты, без настроек),
сцены сборки из трёх сцен (одна открыта — её записи исправляются, у закрытых — нет; сцены возвращены; ассет из двух
сцен — одна запись; таблица сцен; история по сценам), папка (префаб, модель, свободные материал, текстура, клип;
исправление импорта; ничего вне папки), командная строка с двумя сценами, сборкой и папкой, JSON и CSV;
`run-audit-tests.ps1` зелёный; страница утилиты, гайдлайн, TESTPLAN, HANDOFF; версия 1.9.0.

Сделано 25.10.2026: тест `scope` (выделенное; три сцены сборки — возврат сцен, открытая последней и исправляется,
слияние ассетов и настроек, обновление после исправления; командная строка — две сцены, флаг дважды, CSV,
`-auditBuildScenes`, одна сцена, история по сценам, `-auditFolder`; папка — префаб, модель, свободные ассеты), снимки
(сцены сборки RU/EN, папка, выделенное, ширина 560). Доски «Охват» https://claude.ai/artifact/5rYUsiTnTkFuqLNtSC6Bui владелец принял
25.10.2026 («Доски «Охват» подходят»). Не проверено вживую: диалоги сохранения, отмена хода, свой проект владельца.

Исправлено по сверке судьёй (25.10.2026): после исправлений проверяется охват результата (те же выделенные объекты, та же
папка), а не кнопки; строки копий закрытой сцены не падают; исправление дубликатов материалов не сливается между
сценами и не работает у закрытых и в папке; ссылка записи закрытой сцены без объекта открывает сцену; Play Mode,
сохранение только при открытии других сцен, отметка прерванной проверки, порядок сборки в таблице, охват — на проект.

---

### Этап 20 — оценка выигрыша (2.0.0)

Изменение OpenSpec `add-fix-gain`. Владелец 25.10.2026: «делай оценку выйгрыша» — пункт анализа «оценка выигрыша у
каждой проблемы («−34 МБ», «−120 draw calls») и сортировка «что даст больше всего»». Версия: после 1.9.x — 2.0.0.

- **Выигрыш записи** — оценка того, что даст исправление, в одной или нескольких единицах: память (МБ), draw calls,
  треугольники. Грубые оценки помечены «≈». У проверок без измеримого выигрыша (Info про настройки, физика, аниматоры)
  выигрыша нет.
  - Память: `TEX_SIZE` — до порога Warning: память × (1 − (порог / размер)²); `TEX_UNCOMPRESSED` — память × (1 − бит на
    пиксель предложенного формата / текущего); `TEX_READ_WRITE` — копия в памяти CPU (= память текстуры);
    `TEX_ALPHA_UNUSED` — память × (1 − 4 / бит на пиксель); `TEX_DUPLICATE` — память всех копий, кроме первой; `TEX_DENSITY_HIGH` — до 1024 px на
    метр меньшего объекта (степень двойки) ≈; `MESH_READ_WRITE` — копия мешей модели в памяти CPU (вершины и индексы);
    `MESH_DUPLICATE` — память меша × (копий − 1); `AUDIO_DECOMPRESS_LONG` и `AUDIO_PCM_LARGE` — 85 % несжатого PCM ≈;
    `LIGHT_LIGHTMAP_UNCOMPRESSED` — 75 % памяти lightmaps ≈; `LIGHT_LIGHTMAP_DIRECTIONAL` — память текстур направления;
    `LIGHT_SMALL_IN_LIGHTMAP` — доля атласа × память атласа; `REFL_RESOLUTION` — память × (1 − (256 / разрешение)²);
    `REFL_DUPLICATE` — память пробы.
  - Draw calls: `DC_MANY_MATERIALS`, `DC_MULTI_MATERIAL` — материалов − 1 (атлас) ≈; `DC_EXTRA_MATERIALS` — лишние
    материалы; `DC_INSTANCING_OFF` — копий − 1; `LIGHT_REALTIME`, `LIGHT_WIDE_REALTIME`, `LIGHT_IMPORTANT` — объекты в
    зоне источника (проход на каждый) ≈; `SHADER_MULTIPASS` — (проходов − 1) × объекты.
  - Треугольники: `MESH_NO_LOD` — половина треугольников (LOD1 вдвое легче вдали) ≈.
- **«Проблемы»:** у записи справа — «−34 МБ» / «−120 DC» / «≈ −6 000 tris»; у строки копий — сумма; у группы в
  заголовке — сумма по единицам. Переключатель «По выигрышу» в панели списка (хранится в `EditorPrefs`): группы по доле
  бюджета, которую дают их исправления (МБ / порог памяти текстур + DC / порог draw calls + треугольники / порог
  треугольников), группы без выигрыша — после, как обычно. Под полосами бюджета — «Если исправить всё: ≈ −210 МБ ·
  −640 draw calls · −96 000 треугольников» (без допустимых; у нескольких записей одного ассета или объекта — наибольшая
  оценка, чтобы не считать одну память дважды).
- **Отчёт:** у записи — `gainMB`, `gainDrawCalls`, `gainTris`, `gainApprox`; в `summary` — `gainMB`, `gainDrawCalls`,
  `gainTris`; CSV — столбцы `GainMB`, `GainDrawCalls`, `GainTris` в конце.

**Готово, когда:** доски «Выигрыш» на холсте; тест: выигрыш проверок на известных ассетах (текстура 4096
несжатая, Read/Write, дубликаты, клип, материалы, instancing, LOD, пробы), сортировка «По выигрышу», сумма, JSON и CSV;
`run-audit-tests.ps1` зелёный; страница утилиты, гайдлайн, TESTPLAN, HANDOFF; версия 2.0.0.

Сделано 25.10.2026: тест `gain` (выигрыш на известных ассетах у 11 проверок, порядок «По выигрышу», итог без допустимых,
JSON и CSV), снимки (RU, EN, обычный порядок, ширина 560). Доски «Выигрыш» https://claude.ai/artifact/5rYUsiTnTkFuqLNtSC6Bui владелец принял
25.10.2026 («Доски «Выигрыш» подходят»). Не проверено вживую: свой уровень владельца.

Исправлено по сверке судьёй (25.10.2026): итог не складывает выигрыши записей одного ассета, объекта или настроек света
(берётся наибольший в каждой единице); `AUDIO_PCM_LARGE` без выигрыша памяти у клипа Decompress On Load;
`TEX_DENSITY_HIGH` — по самому большому объекту с текстурой.

### Этап 21 — справка в окне (2.1.0)

Изменение OpenSpec `add-window-help`. Владелец 25.10.2026: «Делай этапом справку в окне: кнопку «Справка» с меню,
«Подробнее о проверке» (сначала 30 частых проверок) и «С чего начать» на пустом экране». Идеи и доски «Справка» —
https://claude.ai/artifact/5rYUsiTnTkFuqLNtSC6Bui (идеи 1, 3 и 4); руководство — `docs/scene-optimization-auditor-guide.md`.

- **Кнопка «Справка»** в тулбаре справа, перед версией (значок «?» редактора и подпись; на узком окне — во второй строке
  тулбара). Меню: «С чего начать» (экран «С чего начать» на «Проблемах», и при готовом результате — с кнопкой
  «Вернуться к результату»); «Справка по вкладке «<вкладка>»» — раздел руководства об открытой вкладке в браузере;
  разделитель; «Руководство (в браузере)»; «Справочник проверок (в браузере)». Руководство и справочник — страницы
  `docs/` репозитория на GitHub, только на русском (в EN-окне так и подписано); разделы — по якорям `<a id="…">` в
  руководстве.
- **«С чего начать»** вместо пустого экрана «Проблем»: заголовок и текст, три шага карточками — «Видеокарта» (пороги
  целевой видеокарты, «Изменить…» → «Бюджеты»), «Охват» (кнопка-меню охвата, как в тулбаре), «Запуск» (большая кнопка
  «Запустить аудит»); «Или выберите задачу» — четыре карточки, каждая делает первый шаг: «Первый проход по уровню»
  (включает «По выигрышу» и запускает аудит), «Уровень не держит 90 FPS» (вкладка «Точки обзора»), «Не хватает памяти
  видеокарты» («Тяжёлые», сегмент «Текстуры»; список — после «Собрать»), «Перед сборкой» (охват «Сцены сборки» и запуск;
  без включённых сцен с файлами в Build Settings недоступна); ссылка «Руководство целиком (в браузере)» — начало
  руководства; внизу — XR, stereo и версия, как раньше. На ширине меньше 640 —
  карточки одна под другой; экран прокручивается.
- **«Подробнее о проверке»** — ссылка в блоке «Как исправить» у группы, у которой есть справка; раскрывает под ним
  карточку: «Что проверяется», «Почему это важно в VR», «Как исправить вручную», «Выигрыш» (только у проверок с оценкой
  выигрыша), «Когда можно оставить»; кнопки «Выделить в Unity (N)» (объекты и ассеты показанных записей группы) и «Справочник проверок (в
  браузере)». Ссылка меняется на «Скрыть подробности». Тексты RU/EN — у 30 частых проверок:
  `DC_MULTI_MATERIAL`, `DC_INSTANCING_OFF`, `DC_STATIC_CANDIDATE`, `DC_SHADOW_CASTERS`, `MESH_NO_LOD`, `MESH_READ_WRITE`,
  `MESH_DUPLICATE`, `TEX_SIZE`, `TEX_UNCOMPRESSED`, `TEX_NO_MIPMAPS`, `TEX_READ_WRITE`, `TEX_ALPHA_UNUSED`, `TEX_DUPLICATE`,
  `TEX_NPOT`, `LIGHT_REALTIME`, `LIGHT_SHADOWED`, `LIGHT_NO_UV1`, `LIGHT_SMALL_IN_LIGHTMAP`, `LIGHT_LIGHTMAP_UNCOMPRESSED`,
  `ANIM_ALWAYS_ANIMATE`, `AUDIO_DECOMPRESS_LONG`, `AUDIO_2D_SOURCE`, `VR_STEREO_MODE`, `VR_CANVAS_OVERLAY`, `VR_MSAA`,
  `VR_NO_OCCLUSION`, `MAT_TRANSPARENT`, `SHADER_NO_STEREO`, `PART_LARGE_TRANSPARENT`, `REFL_RESOLUTION` (выбраны по
  тому, что чаще всего встречается на VR-уровнях; остальные — следующими шагами).
- **Заодно:** тулбар уходит во вторую строку и на широком окне, если ряд не помещается (ширина по подписям);
  `DC_SHADOW_CASTERS` без проходов теней (Shadows выключены в Quality или нет realtime/mixed-источника с тенями) записи
  не даёт — раньше было «рисуется ещё в 0 проходах теней».

**Готово, когда:** доски «Справка» поправлены под код; тест: у 30 проверок все разделы на двух языках, «Выигрыш» — ровно у
проверок с оценкой, якоря вкладок есть в руководстве, действия экрана «С чего начать» и пунктов меню; снимки: «С чего
начать» RU/EN и ширина 560, карточка проверки RU/EN и ширина 560; `run-audit-tests.ps1` зелёный; страница утилиты,
руководство, гайдлайн, TESTPLAN, HANDOFF; версия 2.1.0.

Сделано 25.10.2026: тест `help` (30 карточек на двух языках, «Выигрыш» ровно у 15 проверок с оценкой, меню и якоря
руководства, действия «С чего начать», `DC_SHADOW_CASTERS` без проходов теней), снимки («С чего начать» RU, EN, ширина 560,
над результатом; карточка `TEX_SIZE` RU, EN, ширина 560), 43 шага из 43. Тексты карточек сверены судьёй с кодом проверок
(расхождений нет); по сверке поправлены формулировки (условия кнопки у `DC_INSTANCING_OFF`, формат у `TEX_ALPHA_UNUSED`,
«Собрать» у карточки памяти, ссылка «Руководство целиком» — на начало). Доски «Справка» https://claude.ai/artifact/5rYUsiTnTkFuqLNtSC6Bui поправлены под
код; владелец принял их 25.10.2026 («Доски «Справка» подходят»). Не проверено вживую: меню (снимком не снимается), светлый скин, названия настроек Unity в
карточках — TESTPLAN 6и.

Исправлено 27.10.2026 (2.1.1): у владельца список «Проблем» падал при отрисовке групп (`NullReferenceException` в
`DrawGroup`, `AuditorWindow.Problems.cs:351`, затем ошибки разметки при каждой перерисовке). Причина: приватные поля окна
переживают перезагрузку скриптов; после обновления утилиты с открытым окном старые стили вернулись, новые (выигрыш из
2.0.0) остались пустыми, а стили делались заново, только если пуст первый. Теперь стили и значки окон — `[NonSerialized]`,
создаются по флагу `stylesReady`; тесты — шаг `window-fields` и `scripts\run-window-runs.ps1`.

### Этап 22 — настройки качества под VR (2.2.0)

Изменение OpenSpec `add-quality-checks`. Владелец 27.10.2026: «Делай этапом: новые проверки Linear, Soft Shadows, Real-time Reflection Probes в Quality, Soft Particles, VSync и targetFrameRate; пороги MSAA, дальности теней и Skin Weights — по советам для RTX 2070». Советы — разбор
настроек под Vive Pro 2 на RTX 2070 (Built-in), который владелец прислал в разговоре.

- **Новые проверки** (все — «Сведения»):
  - `LIGHT_GAMMA_SPACE` — Color Space в Player Settings — Gamma («Настройки» → Player); исправления нет: смена пространства
    меняет вид света и требует перезапекания.
  - `LIGHT_SOFT_SHADOWS` — включённый realtime- или mixed-источник с Shadow Type = Soft Shadows, когда уровень качества
    разрешает мягкие тени (Shadows = Hard and Soft); исправление по просмотру — Shadow Type = Hard Shadows (объект сцены,
    Undo).
  - `REFL_QUALITY_REALTIME` — на уровне качества включены Realtime Reflection Probes («Настройки» → Quality).
  - `PART_SOFT_PARTICLES` — на уровне качества включены Soft Particles, а в сценах есть включённые системы частиц.
  - `VR_VSYNC` — VSync Count уровня качества не Don't Sync: в VR частоту кадров ведёт рантайм шлема.
  - `VR_FRAME_RATE_SCRIPT` — скрипт в `Assets/` присваивает `Application.targetFrameRate` или `QualitySettings.vSyncCount`;
    одна запись на скрипт, комментарии не считаются. По документации Unity 2019.4 с VR частоту задаёт SDK шлема, а эти
    значения игнорируются; действуют они без шлема и в Game view редактора, где искажают замер без шлема — поэтому
    «Сведения», а не предупреждение (по сверке судьёй, 27.10.2026).
- **Пороги по советам для RTX 2070:**
  - `VR_MSAA` — предупреждение, только когда MSAA выключен (было — ниже 4x): 2x — нормальная настройка, 4x — при запасе по
    времени кадра; новое `VR_MSAA_HIGH` (Info) — 8x: дороже, чем поднять разрешение рендера.
  - `LIGHT_SHADOW_DISTANCE` — дальше 40 м (было 50).
  - `MESH_QUALITY_SKIN_WEIGHTS` — больше Two Bones (было — больше Four Bones), когда есть skinned-меши.
- Карточка «Подробнее о проверке» у `VR_MSAA` — по новому порогу.

**Готово, когда:** тест `quality-checks` (каждая новая проверка и её отсутствие, пороги MSAA 0 / 2 / 4 / 8, дальность 40 / 41,
Skin Weights Two / Four Bones, разбор скриптов на строках и файлах), варианты `quality` и `project` под новые пороги;
`run-audit-tests.ps1` зелёный; страница утилиты, TESTPLAN, HANDOFF; версия 2.2.0.

Сделано 27.10.2026: тесты `quality-checks`, `frame-rate-script-make` / `frame-rate-script-check`, варианты `quality` и
`project` под новые пороги; 47 шагов из 47. По сверке судьёй: `VR_FRAME_RATE_SCRIPT` — «Сведения» (Unity в VR значения
игнорирует), подписи «2 Bones», мягче тексты Soft Particles и realtime-проб, старые пороги в справочнике и этапе 1 SPEC.
`REFL_QUALITY_REALTIME` срабатывает и без realtime-проб в сценах (флаг в Unity включён по умолчанию) — так попросил
владелец. Не проверено вживую: TESTPLAN 6к.

### Этап 23 — целевой шлем (2.3.0)

Изменение OpenSpec `add-target-headset`. Владелец 27.10.2026: «У нас в проекте Vive Pro 2 Wireless. Добавь ещё выбор и настройку шлема, чтобы под него подстраивались лимиты».

- **Выбор на «Бюджетах»** под «Целевой видеокартой»: «Шлем» — HTC Vive Pro, HTC Vive Pro 2, HTC Vive Pro 2 Wireless, Valve
  Index; «Частота» — частоты шлема (Vive Pro — 90 Гц; Vive Pro 2 — 90 / 120; Vive Pro 2 Wireless — 90: беспроводной адаптер;
  Index — 80 / 90 / 120 / 144); «Разрешение SteamVR, %» — 20–500, по площади, как в SteamVR. Ниже мелко: рендер на глаз
  (панель × 1,4 по стороне при 100 % — так SteamVR компенсирует искажение линз, × корень из процента), частота, время кадра
  и во сколько раз пикселей в секунду больше, чем у Vive Pro на 90 Гц и 100 %. Без файла бюджетов — Vive Pro 2 Wireless,
  90 Гц, 100 % (шлем проекта владельца); файл бюджетов без шлема (до 2.3.0) — HTC Vive Pro, 90 Гц, 100 %, как раньше.
- **Что подстраивается под шлем:**
  - время кадра: «Критично» — 1000 / частота мс, «Предупреждение» — 90 % от него (90 Гц — 10 / 11,1; 120 Гц — 7,5 / 8,3);
    замер по точкам, «Профайлер» и «История» берут их;
  - цвета FPS «Профайлера» и «Истории»: зелёный — от частоты − 1, жёлтый — от 89 % частоты (90 Гц — 89 / 80, как раньше);
  - шаг физики `PHYS_FIXED_STEP_*` — 1 / частота;
  - поле зрения точек обзора: Vive Pro и Index — 110° по вертикали, кадр 0,9 : 1; Vive Pro 2 и Vive Pro 2 Wireless — 116°, 1 : 1;
  - порог «Прозрачное в кадре»: 100 / 200 % у Vive Pro на 90 Гц и 100 %, у других — делится на отношение пикселей в
    секунду (Vive Pro 2 Wireless на 90 Гц и 100 % — 38 / 77 %): прозрачные слои стоят столько, сколько пикселей закрашено.
  Таблица порогов заполняется этими значениями при выборе шлема, частоты или процента; правка руками — «свои пороги» у шлема.
- **Строка контекста** «Проблем»: «Шлем: HTC Vive Pro 2 Wireless, 90 Гц». Отчёт: `platform` — «PC VR - <шлем>», `targetFPS` —
  частота, новые `headset`, `refreshRate`, `renderScale`.
- Тексты проверок и окна без «Vive Pro» — «шлем».

**Готово, когда:** доски «Бюджеты: шлем» (RU/EN, оба скина, ширина 560); тест: наборы шлемов, умолчания без файла и для
старого файла, время кадра и прозрачное по частоте и проценту, «свои пороги», шаг физики на 120 Гц, поле зрения точек
обзора, отчёт; снимки «Бюджетов» RU/EN и ширина 560; `run-audit-tests.ps1` зелёный; страница утилиты, руководство,
гайдлайн, TESTPLAN, HANDOFF; версия 2.3.0.

Решено по сверке судьёй (27.10.2026): названия рецепта и карточки «Уровень не держит 90 FPS» оставлены — это частый случай,
пороги в нём берутся по шлему; оценка длительности замера — по частоте шлема (`TourPlan.fps`); после смены шлема оценку
точек обзора нужно повторить (числа хранятся со старым полем зрения).

Сделано 27.10.2026: тест `headset`, снимки «Бюджетов» (RU, EN, ширина 560), доски «Бюджеты: шлем»
https://claude.ai/artifact/5rYUsiTnTkFuqLNtSC6Bui (владелец 27.10.2026: «всё отлично»), 48 шагов из 48. Числа рендера (панель × 1,4) и
порога прозрачного — допущения сессии, уточнить по SteamVR и профайлеру. Не проверено вживую: TESTPLAN 6л.

### Этап 24 — трава и террейн (2.4.0)

Изменение OpenSpec `add-terrain-grass`. Владелец 27.10.2026: «Ещё с учетом всего проверку травы на террейне добавь, если нет». Пороги
ниже — предложение сессии, не владельца; уточнить по профайлеру (TESTPLAN 6м). До этого Terrain не
проверялся (SPEC до 27.10.2026: «Terrain не проверяется и в планах его нет», владелец 24.10.2026). Новая категория «Террейн» (в конце списка категорий).

- `TERRAIN_DETAIL_INSTANCES` — ≈ сколько травинок и деталей рисуется вокруг игрока: сумма плотностей всех слоёв деталей ×
  Detail Density × доля террейна в круге Detail Distance (не больше всего террейна); больше 100 000 — Warning, больше 300 000 —
  Critical; в сообщении — и треугольники (трава и Grass Billboard — 2 на травинку, сетка детали — её треугольники). Только у
  включённого террейна с «Draw» деталей и Detail Density больше 0.
- `TERRAIN_DETAIL_DISTANCE` — Detail Distance больше 60 м (по умолчанию Unity — 80) → Info; исправление по просмотру —
  60 м (объект сцены, Undo).
- `TERRAIN_GRASS_BILLBOARD` — слой травы Grass Billboard → Warning: билборды поворачиваются вслед за головой и каждый глаз
  видит их по-своему; исправление по просмотру — Render Mode = Grass (в данных террейна — ассет, для всех сцен).
- `TERRAIN_DRAW_INSTANCED` — Draw Instanced выключен → Info: инстансинг террейна меньше грузит CPU; исправление по
  просмотру — включить (объект сцены, Undo).
- `TERRAIN_PIXEL_ERROR` — Pixel Error меньше 5 → Info: больше треугольников террейна.

**Готово, когда:** тест `terrain` (каждая проверка и её отсутствие, оценка травы на известной карте деталей, исправления и
Undo); `run-audit-tests.ps1` зелёный; страница утилиты, TESTPLAN, HANDOFF; версия 2.4.0.

### Этап 25 — допустимыми — любые записи (2.5.0)

Изменение OpenSpec `accept-any-issue`. Владелец 27.10.2026: «Потом добавь возможность игнорировать любые замечания. Сейчас можно только те, где можно поставить галочку».

- Галочка — у каждой записи, группы и строки копий: и у настроек проекта, и у записей всей сцены (бюджеты, счётчики) без
  исправления. «Выбрать все» выбирает все показанные записи. «Исправить выбранные (N)» и «Выделить в Unity (N)» считают
  только то, что можно исправить и выделить, как раньше.
- Пометка записи без объекта (решение сессии, не владельца): прогон проверял одну сохранённую сцену (одна открытая сцена или сцены по одной) — для этой
  сцены, по guid сцены (`scene:<guid>`, ключ ставит сам прогон); настройки проекта и несколько открытых сцен вместе — для
  всего проекта (`*`). Пометки `*` до 2.5.0 действуют
  во всех сценах; «Снять пометку» у записи сцены снимает и такую старую пометку (запись снова считается во всех сценах).
- Вид не меняется — только галочка у строк, где её не было; доски не делались (решение сессии).

**Готово, когда:** тест `accept-any` (ключ сцены и проекта, пометка в одной сцене из двух, старая пометка `*`, снятие,
одна и две открытые сцены);
`run-audit-tests.ps1` зелёный; страница утилиты, руководство, гайдлайн, TESTPLAN, HANDOFF; версия 2.5.0.

### Этап 26 — тени по удалённости от точки (2.6.0)

Изменение OpenSpec `add-shadow-zone`. Владелец 27.10.2026: «Добавь потом возможность отключать генерацию теней для мешей, в зависимости от удаленности от точки, которую я выставляю». Затем, и в тот же день: «Добавь возможность не только тени отключать, но и снимать галки с получения теней и глобальной иллюминации. Сделай чтобы умело искать во вложенных объектах». Вид — по
доскам «Тени» https://claude.ai/artifact/5rYUsiTnTkFuqLNtSC6Bui (владелец 27.10.2026: «всё отлично»); правила ниже, кроме слов
владельца, — предложение сессии. «Искать во вложенных объектах» понято так: в таблицу попадают меши на любой глубине
иерархии, а поиск находит меш и по имени любого родителя (группа — все меши внутри). Владелец 27.10.2026: «Поиск по
вложенным объектам понят верно, оставляй так».

- Вкладка «Свет», шестой сегмент «Тени». Сверху — панель: «Точка: X · Y · Z» (или «не поставлена»), «Поставить в центр
  вида» (центр Scene view), «Показать в Scene view»; «Снимать у дальних:» — флажки Cast Shadows (включён), Receive Shadows,
  Contribute GI; «Радиус, м» (`DelayedFloatField`, 1–1000, по умолчанию 30); «Запекаемым оставить Cast Shadows»
  (включён); кнопка «Выключить дальше N м (K)».
- Точка, радиус и флажки хранятся вместе с точками обзора тех же сцен (`SceneAuditPoints/<сцены>.json`, поле `shadows`),
  Undo откатывает их правку. В Scene view, пока открыт сегмент «Тени»: ручка точки, круг радиуса, подпись «Тени: N м ·
  изменятся: K», отметки дальних (точка — изменится, кружок — запекается и оставляет Cast Shadows), легенда.
- Таблица — включённые MeshRenderer и SkinnedMeshRenderer загруженных сцен на любой глубине, которые отбрасывают тень
  (On, Two Sided, Shadows Only), получают тени или участвуют в GI: «Объект» (подсказка — путь), «Расстояние, м» (от точки
  до ближайшей точки границ), «Треугольники», «Отбрасывает», «Получает», «GI»; дальние первыми; метки «дальше радиуса»,
  «запекается» (Contribute GI и Receive GI = Lightmaps), «только тень»; у строки — «Выключить» (`miniButton`). Поиск — по
  имени объекта и всех его родителей; сводка, таблица и кнопка считают найденное. «Только с пометками» — только дальше
  радиуса.
- «Выключить дальше N м (K)» — у найденных дальше радиуса снимает отмеченное: Cast Shadows = Off (кроме Shadows Only и,
  с флажком, запекаемых), Receive Shadows, статичный флаг Contribute GI; «Выключить» у строки — то же у одного объекта,
  где бы он ни был. Через окно подтверждения, одна группа Undo (и флаг на GameObject), копии префабов — override.

История (то же изменение): Владелец 27.10.2026: «В истории не отображается разница между снапшотами. Даже не появляется запись нового снапшота». Причина — прогоны одного дня заменяли
последнюю точку дня (решение этапа истории). Теперь каждый прогон — новая точка, «С прошлой точки» сравнивает с прошлым
прогоном.

**Готово, когда:** тест `shadow-zone` (кто в таблице, путь вложенного, расстояния, что меняет каждый флажок, поиск по
родителю, исправления и одна Undo, точка в файле точек обзора), тест `history` (каждый прогон — точка); снимки сегмента
RU/EN и ширина 560; доски; страница утилиты, руководство, гайдлайн, TESTPLAN, HANDOFF; версия 2.6.0.

### Этап 27 — запуск внизу, список не слетает, перепроверка (2.7.0)

Изменение OpenSpec `add-quick-recheck`. Владелец 27.10.2026: «кнопку начала аудита поставь вниз», «Иногда, когда окно
теряет фокус, текущий аудит слетает. Приходится заново делать», «Добавь возможность быстрой перепроверки отдельного
элемента, чтобы обновить то что поменял» и «Сначала сделай макет на всё, прежде чем кода делать». Вид — по доскам
«Запуск и перепроверка» https://claude.ai/artifact/5rYUsiTnTkFuqLNtSC6Bui (владелец 27.10.2026: «Доски «Запуск и перепроверка» подходят, делай код этапа 2.7.0»);
правила ниже, кроме слов владельца, — предложение сессии.

- **Запуск внизу.** Нижняя полоса окна на всех вкладках: слева «Охват: <что> ▾», за ним мелко ход прогонов («Прогон
  14:32 · 3,4 с», после перепроверки — «· перепроверено: 3 объекта в 14:41»); справа «Запустить аудит» / «Запустить
  снова» — кнопка 28 px со значком Play. Верхний тулбар — «Исправить безопасное (N)», «Сохранить отчёт ▾», справа
  «Справка ▾», версия, `EN | RU`; на ширине 560 он помещается в одну строку. «С чего начать» — как принято 25.10.2026.
- **Список не слетает.** Причина (по коду): результат прогона живёт только в памяти окна (`AuditResult` не сохраняется),
  поэтому его сбрасывает перезагрузка скриптов — Unity делает её, когда возвращаешься в редактор после правки скриптов,
  и при входе в Play Mode. Теперь: окно, открытое заново, сразу показывает последний прогон; после перезагрузки скриптов
  тот же охват проверяется снова сам (вне Play Mode, после компиляции), открытые группы, вкладка и прокрутка остаются, итог —
  «Список обновлён после перезагрузки скриптов». Сцены сборки и несколько сцен сами не перепроверяются (открывают сцены по
  очереди): над списком — полоса «Перезагрузка скриптов сбросила проверку …» и «Запустить снова (N сцен)».
- **Перепроверка.** Значок «Перепроверить» (встроенный `Refresh`) — последним у каждой записи, строки копий и группы; в
  полосе выбора — «Перепроверить выбранное (N)». Перепроверяются только эти объекты (с детьми и ассетами, которые они
  используют), ассет или настройка проекта — без полного прогона. Записи этих объектов заменяются новыми: исправленные
  уходят, изменённые обновляются, новые появляются; вывод, карточки и число на вкладке пересчитываются. Полосы бюджета,
  итоги сцены и проверки по всей сцене (бюджеты, одинаковые меш и материал, число источников и т. п.) остаются с полного
  прогона — у их записей значок неактивен с подсказкой. Точка истории — только от полного прогона. Итог —
  `ShowNotification`: «Перепроверено: 3 объекта за 0,4 с — ушло: 2 записи, осталось: 1 запись»; оставшиеся записи — с меткой
  «перепроверено 14:41» до следующего полного прогона.

**Готово, когда:** доски приняты владельцем; тесты `recheck` (запись уходит после правки объекта, изменённая обновляется,
записи по всей сцене не трогаются, ассет и настройка проекта, группа и выбор) и `keep-result` (окно открыто заново —
список тот же; после перезагрузки скриптов — тот же охват снова, группы открыты); снимки нижней полосы RU/EN и ширина 560;
страница утилиты, руководство, гайдлайн (главное действие — внизу справа), TESTPLAN, HANDOFF; версия 2.7.0.

Сделано 27.10.2026: тесты `recheck` (каждая из 258 перепроверяемых записей тестовой сцены по одной даёт тот же список, что
полный прогон; так найдены две настройки, зависящие от сцены) и `keep-result`, снимки `problems-narrow-ru-dark`,
`recheck-ru/en-dark`, `dropped-ru-dark`; 53 шага из 53. Деление проверок «по объекту / по всей сцене» — по разбору кода
агентом и тесту; записи других тестовых сцен (частицы, террейн, шейдеры) по одной не сверялись. Не проверено вживую:
TESTPLAN 6п.

Исправление 2.7.1 (этап 26, «Тени»), владелец 28.10.2026: «отключение теней и всего что с ними связано не работает на вложенных объектах», на уточнение — «Не меняются». У запекаемых с
флажком «Запекаемым оставить Cast Shadows» Cast Shadows оставался и при снятом Contribute GI; теперь снимается вместе с
ним (объект больше не запекается). Тест `shadow-nested`.

### Этап 28 — бюджет кадра по точкам обзора (2.8.0)

Изменение OpenSpec `update-frame-budget`. Владелец 28.10.2026: «А почему аудитор мне показывает 19к дроу коллов, как он
определяет? Почему во время запуска игры юнити показывает их меньше», «Т.е. он показывает все на сцену, а лимит в 1000
стоит на кадр?» и на предложение сравнивать полосы и проверку с худшей точкой обзора, а итог сцены показывать
справочно — «Да, сделай». Правила ниже, кроме слов владельца, — предложение сессии; доски «Бюджет кадра»
https://claude.ai/artifact/5rYUsiTnTkFuqLNtSC6Bui (владелец 28.10.2026, на вопрос «Доски «Бюджет кадра» подходят?» — «Подходят»).

- Пороги треугольников и draw calls на «Бюджетах» — на кадр. Полосы «Треугольники в кадре» и «Draw calls в кадре» на
  «Проблемах» и «Бюджетах» и проверки `MESH_SCENE_TRIS` и `DC_BUDGET` берут худшую точку обзора (своя для каждой полосы):
  прогон оценивает точки обзора открытых сцен в памяти, как «Оценить» на «Точках обзора» (поле зрения шлема, LOD, тени,
  удвоение без Single Pass Instanced; без батчинга), файл точек не меняется. Под полосами мелко — какие точки худшие,
  сколько оценено и вся сцена без отсечения и батчинга (справочно, без цвета).
- Точек обзора нет — полосы серые с подписью «нет точек обзора», строка под ними и «Расставить точки…» (вкладка «Точки
  обзора»); в списке — запись `FRAME_NO_VIEWPOINTS` (сведения) с итогом всей сцены. Память текстур — как раньше (вся
  сцена).
- Таблица «Сцены» и «История» показывают итоги сцены без цвета порога.

**Готово, когда:** тест `frame-budget` (точка обзора в тестовой сцене: полосы и проверки по худшей точке, пороги на
кадр, файл точек не меняется; без точек — запись сведений и серые полосы), тесты бюджетов на точке обзора; снимки
полос RU/EN, без точек, ширина 560; доски; страница утилиты, руководство, гайдлайн, TESTPLAN, HANDOFF; версия 2.8.0.

Сделано 28.10.2026: шаг `frame-budget`, точка обзора у AuditSeed, `budgets-low` от кадра, Multi Pass удваивает и кадр;
снимки `bars-*` с точкой, `frame-none-ru-dark`, `frame-none-en-dark`, `frame-none-narrow-ru-dark`; 55 шагов из 55. Кадр в оценке точки включает доп. проходы pixel lights,
которых нет в итоге сцены, поэтому на маленькой сцене кадр бывает больше «всей сцены». Не проверено вживую: TESTPLAN 6р.

### Этап 29 — конкретика и исправления: варианты шейдера, мобильный шейдер, трилинейная (2.9.0)

Изменение OpenSpec `update-specific-advice`. Владелец 29.10.2026: «что делать с Many shader variants? у меня шейдер с
75444 вариантами», на предложение показывать, какие `multi_compile` дают больше всего вариантов, — «Да, сделай»; «Потом ещё
добавь возможность менять шейдер на мобильный, для материалов где это поможет. Сейчас есть упоминание что надо заменить на
мобильный шейдер, оптому что только есть текстура альбедо, но нет возможности не только поменять, но и не показывает
конкретики»; «Для трилинейной фильтрации тоже. Говорит что 371 текстуре надо включить, но никакой возможности включить
нету». Правила ниже, кроме слов владельца, — предложение сессии.

- `SHADER_VARIANTS` называет, сколько вариантов нужно сцене (счёт Unity без `shader_feature`, не нужных ни одному материалу,
  и без лишних сцене вариантов тумана и instancing), и до
  четырёх строк, которые множат больше всего: `multi_compile` и `shader_feature` исходника и его `#include` из `Assets/` (×N
  значений), встроенные наборы (`multi_compile_fog` ×4, `multi_compile_instancing` и `multi_compile_particles` ×2, прочие
  `multi_compile_*` и `#pragma surface` — без числа), остальное — «+N». Порядок (поправка по судье): свои `multi_compile`,
  затем `shader_feature`, затем встроенные наборы (их лишнее сборка и так отбрасывает), наборы без числа — в конце; внутри
  — больше N выше. У нескольких сцен — одна запись на шейдер с наибольшим «нужно сцене». Порог записи прежний (больше 512 всего); нужно сцене не больше
  512 — совет «можно не трогать» (`SHADER_VARIANTS_FEW`), иначе — перевести ключевые слова материалов в `shader_feature`,
  урезать наборы.
- `MAT_SIMPLE_STANDARD` — запись на каждый материал Standard из `Assets/` без карт, кроме альбедо (раньше одна запись с числом): какой шейдер
  подойдёт, Metallic и Smoothness материала (блеск и отражения пропадут) и число объектов. Шейдер: Cutout (`_Mode` 1 или
  `_ALPHATEST_ON`) — Legacy Shaders/Transparent/Cutout/Diffuse, цвет `_Color` не белый — Legacy Shaders/Diffuse (у
  Mobile/Diffuse нет цвета), иначе — Mobile/Diffuse. Исправление (с просмотром, ассет): шейдер меняется, текстура, тайлинг,
  цвет и порог остаются, ключевые слова Standard снимаются. Материалы внутри моделей — без исправления, запись называет материал и советует извлечь; встроенные (Default-Material),
  пакетные и созданные в коде — без записи.
- `TEX_NOT_TRILINEAR` — запись на каждую текстуру (раньше одна с числом): какая фильтрация сейчас, число объектов;
  исправление — Trilinear в импортёре: у Bilinear безопасное, у Point — с просмотром (резкие пиксели бывают задуманы).
- Обе записи больше не «по всей сцене»: перепроверяются по своему ассету; пометки «допустимо» старых общих записей к новым
  не переходят.

**Готово, когда:** тесты — строки вариантов и счёт для материалов на тестовом шейдере, записи материалов с выбором
шейдера и исправлением, записи текстур с исправлением, перепроверка по одной; снимки записей RU/EN, ширина 560; доски;
страница утилиты, руководство, TESTPLAN, HANDOFF; версия 2.9.0.

Сделано 29.10.2026: шаг `specific-advice`, разбор `#pragma` и `SHADER_VARIANTS_FEW` в `shaders`, снимки `advice-ru-dark`,
`advice-en-dark`, `advice-narrow-ru-dark`, `advice-shader-ru-dark`, `advice-shader-en-dark`; 56 шагов из 56. Unity считает
«нужно сцене» без неиспользуемых `shader_feature` и без лишних вариантов тумана и instancing (видно в тесте: 768 из 24576).
По судье: порядок строк — свои `multi_compile` первыми, Point — исправление с просмотром, запись материала внутри модели
называет его, у нескольких сцен — одна запись вариантов на шейдер, подсказка группы с записями разного вида — общая. Не
проверено вживую: TESTPLAN 6с.
