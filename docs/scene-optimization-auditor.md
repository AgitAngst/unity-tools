# SceneOptimizationAuditor

Редакторская утилита для Unity 2019.4 (Built-in RP): сканирует загруженные сцены под PC VR
(HTC Vive Pro, SteamVR, 90 Гц) и показывает проблемы по уровням Critical / Warning / Info с подсказкой,
как исправить. Замысел и этапы — [SPEC.md](../SPEC.md); вид окна — [ui-guideline.md](ui-guideline.md).

**Состояние:** код всех четырёх этапов написан. Автотесты в Unity 2019.4.41f2 зелёные (22.10.2026,
`scripts\run-audit-tests.ps1`, 25 шагов, из них 4 — XR Plug-in Management): проверки, бюджеты, экспорт, исправления и Undo. Окно,
исправления, снимок профайлера и прогон на настоящей сцене владелец проверил вживую 22.10.2026. Не проверен
настоящий OpenVR XR Plugin (в тестах — заглушки) — см. [TESTPLAN.md](../TESTPLAN.md).

## Подключение

1. Скопировать папку `SceneOptimizationAuditor` в `Assets/` проекта (любое место внутри `Assets/`).
2. Дождаться компиляции; меню — `Tools/VR Scene Optimization Audit`.

Зависимостей нет. Post Processing v2, SteamVR Plugin, XR Plug-in Management и OpenVR XR Plugin
читаются через reflection: нет пакета — его проверки пропускаются.

## Окно

Вид — по [ui-guideline.md](ui-guideline.md); снимки — `scripts\shoot-audit-window.ps1`.

- **Тулбар:** «Запустить аудит» («Запустить снова» после прогона), «Исправить безопасное (N)»,
  «Сохранить отчёт ▾» (JSON или CSV), справа версия утилиты `v0.7.0.12` (X.Y.Z и номер сборки — число коммитов
  репозитория) и `EN | RU` (хранится в `EditorPrefs`). Версия видна и на экране до первого прогона.
- **Вкладки:** «Проблемы (N)», «Развёртка под свет», «Бюджеты», «Профайлер».
- **До первого прогона** — что проверит, что ничего не меняет, крупная кнопка «Запустить аудит» и
  найденная XR-система.

### Вкладка «Проблемы»

- **Вывод одной строкой:** «Критичных проблем: N — исправьте их в первую очередь», иначе «Критичных
  проблем нет. Предупреждений: N — стоит просмотреть», иначе «Нет ни критичных проблем, ни
  предупреждений».
- **Карточки уровней** «Критично / Предупреждение / Сведения» с числами — они же фильтры: клик скрывает
  или показывает уровень. Сведения по умолчанию скрыты; выбор запоминается.
- **Контекст:** XR-система, режим stereo, проверенный уровень качества, сцены, время прогона.
- **Полосы бюджетов:** треугольники (LOD0), draw calls, память текстур — «значение из порога
  предупреждения», цвет по порогам, полоса до критичного порога.
- **Панель списка:** поиск (по названию проблемы, коду проверки, объекту, пути ассета и тексту) с крестиком,
  «По категориям», «Развернуть все», «Свернуть все».
- **Список групп.** Группа — одна проверка одного уровня: значок, простое название («Текстура без
  mipmaps»), число объектов, справа «Исправить» / «Исправить все (N)», «Настройки» и категория.
  Порядок — критичные, затем предупреждения, затем сведения; внутри уровня — по числу объектов.
  Критичные группы после прогона развёрнуты. В развёрнутой группе — «Как исправить: …» и объекты:
  ссылка (клик выделяет и подсвечивает объект или ассет, у настройки — открывает Project Settings),
  под ней текст записи, справа «Исправить» у исправимых. В группе — первые 100 объектов и «Показать ещё».
- **По категориям** — те же группы под заголовками категорий (сворачиваются).

После перезагрузки домена (компиляция, вход в Play Mode) результат сбрасывается — снова пустое состояние.

### Вкладка «Развёртка под свет»

Проверка **всех** моделей загруженных сцен — и у объектов без отметки под lightmap, и у неактивных.

- «Проверить модели» — сканирует сцены (ничего не меняет). Сводка: «Моделей · без UV1 · галочка стоит ·
  с проблемами».
- Таблица, строка на файл модели (или на меш-ассет без модели): «Модель» (клик — выделить в Project),
  «Галочка» (Generate Lightmap UVs: «стоит» / «нет», «—» у мешей без модели), «UV1» («есть» / «нет» — у всех
  мешей модели), «За 0–1» (доля вершин UV1), «Наложения» (доля общих текселей; больше 1 % — проблема),
  «Объекты» (сколько объектов используют модель и сколько из них запекаются в lightmap). Проблемные строки —
  сверху, с цветной полосой слева.
- «Только проблемные» (по умолчанию включено) — без UV1, за 0–1 или с наложениями.
- «Включить галочку» у строки модели без UV1 с выключенной галочкой и «Включить у всех без UV1 (N)» — через
  окно подтверждения, затем модели сканируются снова.
- «Сохранить CSV» — `Model, AssetPath, GenerateLightmapUVs, HasUV1, OutsidePercent, OverlapPercent, Objects,
  BakedObjects` (UTF-8 с BOM).

### Вкладка «Бюджеты»

Полосы бюджетов с объяснением, что это оценки, и таблица порогов Warning / Critical: «Сохранить»,
«По умолчанию». Пороги действуют со следующего прогона.

### Вкладка «Профайлер»

Снимок профайлера — см. раздел ниже.

## Что сканируется

- Все загруженные сцены; объекты — с неактивными. Бюджеты и счётчики «активных» — только активные и
  включённые компоненты.
- Ассеты: материалы и текстуры рендереров, графика UI (через свойство `mainTexture`), skybox,
  lightmaps, reflection probes, клипы AudioSource, плюс всё из `EditorUtility.CollectDependencies` по
  корням сцен. Проблема ассета — одна запись с числом использований.
- Уровень качества — тот, что Standalone использует по умолчанию (`m_PerPlatformDefaultQuality`); на
  время чтения уровень переключается и сразу возвращается.
- XR: legacy — `PlayerSettings.GetVirtualRealitySDKs(Standalone)`, флаг `m_BuildTargetVRSettings`,
  `PlayerSettings.stereoRenderingPath`; XR Plug-in Management — `XRGeneralSettingsPerBuildTarget` для
  Standalone и его загрузчики; режим stereo OpenVR — из ассета `OpenVRSettings` (только читается).
- Ассеты вне `Assets/` (пакеты, встроенные) и меши без импортёра — без проверок импорта и без
  исправлений.

## Оценки

- **Треугольники:** сумма по активным рендерерам; в LODGroup — только LOD0. Без отсечения.
- **Draw calls:** материальные слоты активных рендереров (+1 на частицы, линии, спрайты) плюс проход
  теней: слоты shadow casters × число проходов (у directional — каскады, у point — 6, у spot — 1) по
  всем Realtime/Mixed источникам с тенями; ×2, если режим stereo не Single Pass Instanced. Без батчинга,
  отсечения и доп. проходов pixel lights.
- **Память текстур:** формат × размер × все уровни mip × грани cubemap; Read/Write удваивает. Каждый
  ассет — один раз.

## Бюджеты

По умолчанию — стартовые значения для GTX 1070; переопределения проекта —
`ProjectSettings/SceneOptimizationAuditor.json` (создаётся кнопкой «Сохранить»).

| Порог | Warning | Critical |
|---|---|---|
| Draw calls за кадр | > 1000 | > 2000 |
| Треугольники сцены | > 2 000 000 | > 4 000 000 |
| Треугольники объекта без LODGroup | > 10 000 | > 25 000 |
| Память текстур, МБ | > 1536 | > 2048 |
| Сторона текстуры, px | > 2048 | > 4096 |
| Realtime/Mixed источники с тенями | > 1 | > 2 |
| Время кадра, мс (снимок профайлера) | > 10 | > 11.1 |
| Активные Animator | > 20 | — |
| Треугольники SkinnedMesh под Animator | > 15 000 | — |
| Активные AudioSource | > 32 | — |
| Треугольники рендер-меша в MeshCollider | > 5 000 (Info) | — |

Неизменяемые пределы (`VRBudgets`, константы): больше 4 материалов на рендерере, больше 3 одинаковых
объектов для instancing, больше 4 костей на вершину, Decompress On Load дольше 10 с, PCM больше 1 МБ,
Preload дольше 30 с, priority 0 больше чем у 5 источников, shadow distance больше 50 м, solver
iterations больше 6, far clip больше 1000, near clip меньше 0.05, MSAA меньше 4x, occlusion — больше 100
статичных рендереров, глубина иерархии больше 10, FPS: зелёный от 89, жёлтый от 80; допуск Fixed Timestep
0.0002 с; развёртка под свет — допуск 0–1 0.001, наложения от 1 % на сетке 256 × 256; исправление Load Type — Streaming для клипов от 60 с.

## Проверки

Исправление: **S** — безопасное (пакетом и по строке), **R** — по строке после просмотра, — — нет.

| checkId | Уровень | Когда | Испр. |
|---|---|---|---|
| **Draw Calls** | | | |
| `DC_BUDGET` | W / C | оценка draw calls выше порога | — |
| `DC_SHADOW_CASTERS` | I | есть shadow casters (число и проходы) | — |
| `DC_MANY_MATERIALS` | W | больше 4 материалов на MeshRenderer | — |
| `DC_MULTI_MATERIAL` | I | 2–4 материала | — |
| `DC_EXTRA_MATERIALS` | W | материалов больше, чем submesh | — |
| `DC_STATIC_CANDIDATE` | I | не Batching Static; ни на нём, ни у предков нет Animator, Animation, Rigidbody, NavMeshAgent, MonoBehaviour | R |
| `DC_INSTANCING_OFF` | W | больше 3 нестатичных объектов с одной парой меш + материал, instancing выключен, шейдер поддерживает | S |
| `DC_INSTANCING_UNKNOWN` | I | то же, но поддержку шейдером не определить | — |
| **Meshes** | | | |
| `MESH_SCENE_TRIS` | W / C | треугольники сцены выше порога | — |
| `MESH_NO_LOD` | W / C | объект без LODGroup выше порога | — |
| `MESH_READ_WRITE` | W | Read/Write у модели | R |
| `MESH_READ_WRITE_NEEDED` | I | Read/Write, меш у MeshCollider или ParticleSystem | — |
| `MESH_NOT_OPTIMIZED` | I | Optimize Mesh не Everything | — |
| `MESH_BONES_PER_VERTEX` | W | больше 4 костей на вершину | — |
| `MESH_QUALITY_SKIN_WEIGHTS` | I | Skin Weights уровня больше Four Bones (есть skinned) | — |
| `MESH_GPU_SKINNING_OFF` | W | GPU Skinning выключен (есть skinned) | — |
| **Textures** | | | |
| `TEX_SIZE` | W / C | сторона выше порога (cubemap и skybox — не выше W; lightmaps и probes не проверяются) | — |
| `TEX_NOT_NORMAL_MAP` | C | в свойстве нормалей, тип не Normal map | R |
| `TEX_NO_MIPMAPS` | W | без mipmaps в 3D или world-space UI | S |
| `TEX_UNCOMPRESSED` | W | несжатый формат (подсказка DXT1 / BC7 / BC6H / BC5) | R |
| `TEX_READ_WRITE` | W | Read/Write у текстуры | R |
| `TEX_MEMORY` | W / C | оценка памяти выше порога | — |
| `TEX_ANISO_DISABLED` | I | анизотропная фильтрация выключена на уровне | — |
| `TEX_NOT_TRILINEAR` | I | число 3D-текстур с mipmaps на Point/Bilinear (одна запись) | — |
| **Lighting** | | | |
| `LIGHT_REALTIME` | W | Point/Spot в Realtime | — |
| `LIGHT_SHADOWED` | W / C | Realtime/Mixed источников с тенями больше порога | — |
| `LIGHT_NO_UV1` | W | меш объекта под lightmap (Contribute GI + Receive GI Lightmaps) без UV1; одна запись на меш. Исправление — только у модели с выключенной галочкой Generate Lightmap UVs: включить её | R |
| `LIGHT_UV1_OUT_OF_RANGE` | W | UV1 меша под lightmap выходит за 0–1 (допуск 0.001), доля вершин | — |
| `LIGHT_UV1_OVERLAP` | W | треугольники UV1 накладываются: больше 1 % занятых текселей на сетке 256 × 256 (общие рёбра не в счёт) | — |
| `LIGHT_SHADOW_RESOLUTION` | W / I | VeryHigh / High | — |
| `LIGHT_SHADOW_DISTANCE` | I | дальше 50 м | — |
| `LIGHT_PIXEL_LIGHTS` | W | Pixel Light Count больше 1 | — |
| `LIGHT_REALTIME_GI` | I | Realtime GI включён | — |
| **Physics** | | | |
| `PHYS_CONCAVE_DYNAMIC` | C | невыпуклый MeshCollider под не-kinematic Rigidbody (и у предков) | — |
| `PHYS_CONCAVE_KINEMATIC` | I | то же, Rigidbody kinematic | — |
| `PHYS_RENDER_MESH_COLLIDER` | I | MeshCollider на рендер-меше больше 5 000 треугольников | — |
| `PHYS_SOLVER_ITERATIONS` | I | больше 6 | — |
| `PHYS_FIXED_STEP_SLOW` | W | Fixed Timestep больше 1/90 | — |
| `PHYS_FIXED_STEP_FAST` | I | Fixed Timestep меньше 1/90 | — |
| **Animators** | | | |
| `ANIM_ALWAYS_ANIMATE` | W | Culling Mode = Always Animate (исправление — Cull Update Transforms) | R |
| `ANIM_NO_CONTROLLER` | I | нет контроллера | — |
| `ANIM_ROOT_MOTION` | I | Apply Root Motion без NavMeshAgent, CharacterController, Rigidbody, OnAnimatorMove | — |
| `ANIM_COUNT` | W | активных Animator больше порога | — |
| `ANIM_UPDATE_OFFSCREEN` | W | Update When Offscreen | R |
| `ANIM_HEAVY_SKIN` / `ANIM_HEAVY_SKIN_CPU` | W | SkinnedMesh под Animator больше порога (CPU — при выключенном GPU Skinning) | — |
| **Audio** | | | |
| `AUDIO_DECOMPRESS_LONG` | W | Decompress On Load дольше 10 с (исправление — Streaming от 60 с, иначе Compressed In Memory) | R |
| `AUDIO_PCM_LARGE` | W | PCM больше 1 МБ (исправление — Vorbis 70 %) | R |
| `AUDIO_PRELOAD_LONG` | W | Preload Audio Data дольше 30 с | R |
| `AUDIO_NO_CLIP` | I | AudioSource без клипа | — |
| `AUDIO_2D_SOURCE` | I | Spatial Blend = 0 (исправление — 1) | R |
| `AUDIO_SOURCE_COUNT` | W | активных AudioSource больше порога | — |
| `AUDIO_PRIORITY_ZERO` | W | priority 0 больше чем у 5 | — |
| `AUDIO_LISTENERS_MANY` | C | больше одного активного AudioListener (запись на каждый) | — |
| `AUDIO_NO_LISTENER` | W | ни одного | — |
| `AUDIO_LISTENER_NO_CAMERA` | I | AudioListener не на объекте с Camera | — |
| `AUDIO_DSP_BUFFER` | I | DSP Buffer Size = Default или Best Latency | — |
| `AUDIO_SPEAKER_MODE` | W | режим динамиков не Mono / Stereo / Raw | — |
| `AUDIO_NO_SPATIALIZER` | I | spatializer не выбран | — |
| **VR-Specific** | | | |
| `VR_NOT_ENABLED` | C | VR не включён для Standalone | — |
| `VR_NO_OPENVR` | W | VR без OpenVR | — |
| `VR_BOTH_SYSTEMS` | W | включены legacy и XR Plug-in Management | — |
| `VR_STEREO_MODE` | W | режим stereo известен и не Single Pass Instanced | — |
| `VR_DEFERRED_TIER` | W | Tier 3 Standalone — Deferred | — |
| `VR_DEFERRED_CAMERA` | W | VR-камера явно Deferred | — |
| `VR_MSAA` | W | Anti Aliasing уровня меньше 4x | — |
| `VR_NO_CAMERA` | I | нет VR-камеры (Target Eye ≠ None, без Target Texture) | — |
| `VR_CAMERA_NO_MSAA` | W | у VR-камеры выключен Allow MSAA | — |
| `VR_CAMERA_HDR` | W | у VR-камеры включён HDR | — |
| `VR_FAR_CLIP` | I | far больше 1000 | — |
| `VR_NEAR_CLIP` | I | near меньше 0.05 (Unity не даёт меньше 0.01, порог поднят по решению владельца 22.10.2026) | — |
| `VR_IMAGE_EFFECT` | W | скрипт с `OnRenderImage` на VR-камере | — |
| `VR_CANVAS_OVERLAY` | C | корневой Canvas в Screen Space Overlay | — |
| `VR_CANVAS_CAMERA` | W | корневой Canvas в Screen Space Camera | — |
| `VR_PP_LAYER` | W | Post Process Layer (PPv2) | — |
| `VR_PP_EFFECTS` | W | Post Process Volume с включёнными эффектами (дорогие перечислены) | — |
| `VR_STEAMVR_MISSING` | I | SteamVR Plugin не найден | — |
| `VR_STEAMVR_NOT_IN_SCENE` | I | плагин есть, компонентов его rig в сценах нет | — |
| `VR_PROBE_REALTIME` | W | Realtime Reflection Probe | — |
| `VR_PROBE_EVERY_FRAME` | C | то же, обновление каждый кадр | — |
| `VR_NO_OCCLUSION` | I | больше 100 статичных рендереров без запечённого occlusion culling | — |
| **General** | | | |
| `GEN_EMPTY_LEAF` | I | объект только с Transform и без детей | — |
| `GEN_NEGATIVE_SCALE` | I | отрицательный scale | — |
| `GEN_DEEP_HIERARCHY` | I | первый объект глубже 10 уровней | — |
| `GEN_MISSING_SCRIPTS` | W | пропавшие скрипты | — |
| `GEN_INACTIVE_HEAVY_MESH` | I | неактивный объект с мешем выше порога объекта | — |
| `GEN_INACTIVE_HEAVY_TEXTURE` | I | неактивный объект с текстурой больше порога Warning | — |

## Исправления

- **«Исправить безопасное (N)»** — пакетом только безопасные: GPU Instancing у материала (только
  материалы-ассеты в `Assets/`, не вложенные в модели) и mipmaps у текстуры.
- **«Исправить все (N)» у группы** — все доступные исправления группы (у группы из одной записи —
  «Исправить»); **«Исправить» у объекта** внутри группы — только эта запись.
- Перед применением — окно подтверждения: список с галочками в группах «Сцена — откатывается Undo» и
  «Ассеты — меняются во всём проекте, Undo их не откатывает, ассет переимпортируется»; объекты в
  экземплярах префабов помечены «станет override префаба».
- Сцена: одна группа Undo «Scene Audit Fix», сцены помечаются изменёнными. Ассеты: импорт одним пакетом
  (`StartAssetEditing`), материалы сохраняются; ассеты вне `Assets/` пропускаются с предупреждением в
  консоли. Каждое изменение — строка `[SceneAudit] Fixed <checkId>: <путь>`; каждый прогон — строка
  `[SceneAudit] Audit <версия>: N issues in M scene(s).` После применения аудит
  запускается снова.
- Не исправляются: Canvas Screen Space Overlay и все настройки проекта — только подсказка и кнопка
  «Настройки».

## Отчёт

«Сохранить отчёт» → меню «JSON…» или «CSV…» → диалог сохранения (имя по умолчанию —
`<первая сцена>-audit-<yyyyMMdd-HHmm>`); формат файла определяется расширением.

- **JSON** (UTF-8 без BOM): `scenes`, `toolVersion` (например `0.7.0.12`), `timestamp` (ISO 8601), `platform`, `targetFPS`, `xr` (`system`:
  `Legacy` / `XRManagement` / `Legacy+XRManagement` / `None`, `loader`, `stereoMode`), `qualityLevel`,
  `summary`, `sceneBudgets` (`isEstimate`, значения и пороги Warning), `hasProfilerSnapshot`,
  `profilerSnapshot` (последний снимок активной сцены или пустой при `false`), `issues` (`checkId`,
  `category`, `severity`, `scene`, `objectPath`, `assetPath` — путь ассета или страница настроек,
  `message`, `suggestion`, `autoFixable`). `summary` — `critical`, `warning`, `info`; `sceneBudgets` —
  `isEstimate`, `totalTris`, `trisLimit`, `estimatedDrawCalls`, `drawCallLimit`, `textureMemoryMB`,
  `textureMemoryLimitMB`; `category` — `DrawCalls`, `Meshes`, `Textures`, `Lighting`, `Physics`, `Animators`,
  `Audio`, `VRSpecific`, `General`; поля `profilerSnapshot` — в разделе о снимке ниже.
- **CSV** (UTF-8 с BOM, RFC 4180): `CheckId, Category, Severity, Scene, ObjectPath, AssetPath, Message,
  Suggestion, AutoFixable`.
- `checkId`, `category`, `severity`, `xr` — всегда по-английски; `message`, `suggestion` — на языке окна.

Из командной строки: `-auditScene` (сцена в проекте) и `-auditReport` (файл; `.csv` — CSV, иначе JSON)
обязательны, `-auditLang en|ru` — язык текстов (по умолчанию — как в окне). Код выхода 0 — отчёт сохранён, 1 —
нет аргументов или ошибка (строка `[SceneAudit] …` в журнале).

```
Unity.exe -batchmode -quit -projectPath <проект> -executeMethod UnityTools.SceneOptimization.ReportExporter.RunFromCommandLine -auditScene Assets/Scenes/Main.unity -auditReport E:/reports/main.json -auditLang en
```

## Снимок профайлера

- Параметры: «Кадров» (300, от 30 до 5000) и «Кадров разогрева» (60, от 0 до 1000) — хранятся в `EditorPrefs`.
- «Снять данные»: не в Play Mode — входит, пропускает кадры разогрева, мерит N кадров, выходит; в Play Mode —
  мерит и остаётся. «Отмена» прерывает. Выход из Play Mode или перекомпиляция во время замера
  прерывают его с предупреждением в консоли.
- Метрики: FPS (сред. / мин. / макс.) по `Time.unscaledDeltaTime`; CPU — время главного потока
  `UnityStats.frameTime` (как в окне Stats; может включать ожидание GPU и шлема); GPU —
  `FrameTimingManager`, если даёт данные, иначе «н/д» (в отчёте −1); draw calls, SetPass, batches,
  треугольники — средние `UnityStats`; память текстур — `Texture.currentTextureMemory`, мешей —
  `Profiler.GetRuntimeMemorySizeLong` по загруженным мешам, всего — `Profiler.GetTotalAllocatedMemoryLong`.
- Цвет: время кадра по порогам Warning / Critical; FPS — зелёный от 89, жёлтый от 80; draw calls,
  треугольники и память текстур — по бюджетам; остальное без цвета.
- Последний и предыдущий снимки — JSON в `EditorPrefs` с ключом по пути проекта и сцены, переживают
  перезапуск Unity. «Сравнить с прошлым» — дельта ↑ / ↓, зелёная — лучше, красная — хуже.
- Замер в редакторе — ориентир; итоговые цифры — профайлер на Development Build.

## Устройство

`SceneOptimizationAuditor/Editor/` (сборка `UnityTools.SceneOptimizationAuditor.Editor`, только Editor,
пространство имён `UnityTools.SceneOptimization`):

| Файл | Что |
|---|---|
| `AuditorWindow.cs` | окно |
| `SceneAuditor.cs` | `RunFullAudit()`, `AuditResult` |
| `Checks/*Checks.cs` | проверки по категориям (`partial class SceneAuditor`) |
| `AuditContext.cs` | сбор сцен, объектов, ассетов и мест использования текстур |
| `AuditIssue.cs` | модель проблемы, уровни, категории, виды исправления |
| `VRBudgets.cs` | пороги и константы, файл бюджетов |
| `XrInfo.cs`, `QualitySnapshot.cs` | XR и уровень качества |
| `TextureMemory.cs` | оценка памяти текстуры |
| `Strings.cs` | все тексты RU/EN |
| `AutoFixer.cs`, `FixConfirmWindow.cs` | исправления и подтверждение |
| `ReportExporter.cs` | JSON, CSV, командная строка |
| `ProfilerCapture.cs` | снимок профайлера |
| `LightmapUvReport.cs` | вкладка «Развёртка под свет»: сканирование всех моделей, исправления, CSV |
| `ToolVersion.cs` | версия утилиты и номер сборки (`scripts\stamp-version.ps1`) |
| `ReflectionUtil.cs` | доступ к типам пакетов по имени |

Новая проверка = блок в методе категории (`ctx.Report(...)`), тексты `<checkId>`, `<checkId>.title`, `<checkId>.fix` и
для исправимой `<checkId>.do` в `Strings.cs`, строка в таблице выше и в `TESTPLAN.md`.
