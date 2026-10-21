# SceneOptimizationAuditor

Редакторская утилита для Unity 2019.4 (Built-in RP): сканирует загруженные сцены под PC VR
(HTC Vive Pro, SteamVR, 90 Гц) и показывает проблемы по уровням Critical / Warning / Info с подсказкой,
как исправить. Замысел и этапы — [SPEC.md](../SPEC.md); вид окна — [ui-guideline.md](ui-guideline.md).

**Состояние:** код всех четырёх этапов написан. Автотесты в Unity 2019.4.41f2 зелёные (22.10.2026,
`scripts
un-audit-tests.ps1`, 21 шаг): проверки, бюджеты, экспорт, исправления и Undo. Вживую не
проверены окно, снимок профайлера в Play Mode и ветка XR Plug-in Management — см.
[TESTPLAN.md](../TESTPLAN.md).

## Подключение

1. Скопировать папку `SceneOptimizationAuditor` в `Assets/` проекта (любое место внутри `Assets/`).
2. Дождаться компиляции; меню — `Tools/VR Scene Optimization Audit`.

Зависимостей нет. Post Processing v2, SteamVR Plugin, XR Plug-in Management и OpenVR XR Plugin
читаются через reflection: нет пакета — его проверки пропускаются.

## Окно

- **Тулбар:** «Run Audit / Запустить аудит», справа переключатель `EN | RU` (хранится в `EditorPrefs`).
- **Шапка:** цель (HTC Vive Pro, 90 FPS); найденная XR-система — «Legacy VR (OpenVR)»,
  «XR Plug-in Management (OpenVRLoader)» или «VR не включён»; режим stereo; проверенный уровень
  качества (Standalone по умолчанию или текущий); сцены и время прогона.
- **Бюджеты сцены:** треугольники (LOD0), draw calls, память текстур — полоса до порога Critical,
  цвет по порогам, метка порога Warning; всё помечено «оценка».
- **Фильтры:** три переключателя уровней со счётчиками и поиск по имени объекта, пути ассета и тексту.
- **Категории:** Draw Calls, Meshes, Textures, Lighting, Physics, Animators, Audio, VR-Specific,
  General; в заголовке — число проблем каждого уровня. В категории — первые 200 строк и «Показать ещё».
- **Строка проблемы:** иконка и полоска уровня, текст, путь (сцена: путь в иерархии, путь ассета или
  страница Project Settings), число использований ассета, подсказка. Клик по тексту или пути выделяет
  объект или ассет и подсвечивает его; для настроек открывается Project Settings. Справа —
  «Исправить» (если есть исправление) и «Настройки» (если проблема в настройках проекта).
- **Бюджеты** (свёрнутый блок): пороги Warning / Critical, «Сохранить», «По умолчанию».
- **Снимок профайлера** (свёрнутый блок) — см. ниже.
- **Полоса итогов:** число проблем по уровням, «Исправить безопасное (N)», «Сохранить отчёт».

После перезагрузки домена (компиляция, вход в Play Mode) список пуст — прогон нужно повторить.

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
iterations больше 6, far clip больше 1000, near clip меньше 0.01, MSAA меньше 4x, occlusion — от 100
статичных рендереров, глубина иерархии больше 10, FPS: зелёный от 89, жёлтый от 80.

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
| `LIGHT_NO_UV1` | W | Contribute GI + Receive GI Lightmaps, у меша нет UV1 | — |
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
| `AUDIO_SPEAKER_MODE` | W | режим динамиков не Mono / Stereo | — |
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
| `VR_NEAR_CLIP` | W | near меньше 0.01 — на деле не срабатывает: Unity не даёт поставить меньше 0.01 (решение за владельцем) | — |
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
- **«Исправить» у строки** — любое доступное исправление этой записи.
- Перед применением — окно подтверждения: список с галочками в группах «Сцена — откатывается Undo» и
  «Ассеты — меняются во всём проекте, Undo их не откатывает, ассет переимпортируется»; объекты в
  экземплярах префабов помечены «станет override префаба».
- Сцена: одна группа Undo «Scene Audit Fix», сцены помечаются изменёнными. Ассеты: импорт одним пакетом
  (`StartAssetEditing`), материалы сохраняются; ассеты вне `Assets/` пропускаются с предупреждением в
  консоли. Каждое изменение — строка `[SceneAudit] Fixed <checkId>: <путь>`. После применения аудит
  запускается снова.
- Не исправляются: Canvas Screen Space Overlay и все настройки проекта — только подсказка и кнопка
  «Настройки».

## Отчёт

«Сохранить отчёт» → меню JSON или CSV → диалог сохранения; формат файла определяется расширением.

- **JSON** (UTF-8 без BOM): `scenes`, `timestamp` (ISO 8601), `platform`, `targetFPS`, `xr` (`system`:
  `Legacy` / `XRManagement` / `Legacy+XRManagement` / `None`, `loader`, `stereoMode`), `qualityLevel`,
  `summary`, `sceneBudgets` (`isEstimate`, значения и пороги Warning), `hasProfilerSnapshot`,
  `profilerSnapshot` (последний снимок активной сцены или пустой при `false`), `issues` (`checkId`,
  `category`, `severity`, `scene`, `objectPath`, `assetPath` — путь ассета или страница настроек,
  `message`, `suggestion`, `autoFixable`).
- **CSV** (UTF-8 с BOM, RFC 4180): `CheckId, Category, Severity, Scene, ObjectPath, AssetPath, Message,
  Suggestion, AutoFixable`.
- `checkId`, `category`, `severity`, `xr` — всегда по-английски; `message`, `suggestion` — на языке окна.

Из командной строки (код выхода 0 / 1):

```
Unity.exe -batchmode -quit -projectPath <проект> -executeMethod UnityTools.SceneOptimization.ReportExporter.RunFromCommandLine -auditScene Assets/Scenes/Main.unity -auditReport E:/reports/main.json -auditLang en
```

## Снимок профайлера

- Параметры: «Кадров» (300, от 30 до 5000) и «Кадров разогрева» (60) — хранятся в `EditorPrefs`.
- «Снять данные»: не в Play Mode — входит, пропускает разогрев, мерит N кадров, выходит; в Play Mode —
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
| `ReflectionUtil.cs` | доступ к типам пакетов по имени |

Новая проверка = блок в методе категории (`ctx.Report(...)`), тексты `<checkId>`, `<checkId>.fix` и
для исправимой `<checkId>.do` в `Strings.cs`, строка в таблице выше и в `TESTPLAN.md`.
