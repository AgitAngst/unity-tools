# План тестов — SceneOptimizationAuditor

## Итоги прогона 27.10.2026

Unity 2019.4.41f2, `scripts\run-audit-tests.ps1` в тестовом сервере (`scripts\audit-server.ps1`: один batch-редактор,
запущенный один раз, все шаги — командами в нём; шаги `cli-*` — через `ReportExporter.RunWithArgs`, без выхода из
редактора): **51 шаг из 51 зелёный**, версия 2.6.0. С 2.6.0 — шаг `shadow-zone` (кто в таблице: отбрасывает,
Shadows Only, получает — не неактивный и не меш без всего; путь вложенного Forest/Trees/Pine_01; расстояние 49,5 м до
границ; дальние первыми; что меняют Cast Shadows с флажком запекаемых и без, Receive Shadows, Contribute GI; поиск
«forest»; исправления и одна Undo вместе с флагом GI; без точки ничего не дальнее; точка, радиус и флажки в файле точек
обзора), шаг `history` — каждый прогон точка, и прогоны одного дня. С 2.5.0 — шаг `accept-any` (две пустые сцены по одной: запись
«нет VR-камеры» помечена в первой — допустима в первой и считается во второй; ключ `scene:<guid>`, у настройки проекта —
`*`; старая пометка `*` — в обеих сценах, снятие у записи сцены её убирает; одна открытая сцена — тот же ключ, две
открытые вместе и несохранённая — `*`), шаг `multi-select` — у настройки без
исправления нечего выделять и исправлять. С 2.4.0 — шаг `terrain` (террейн 100 × 100 м, два слоя травы по 8 на
клетку карты 128 × 128, Detail Density 1, Detail Distance 80 м: 262 144 травинки и 524 288 треугольников — Warning; слой Grass
Billboard, Draw Instanced выключен, Pixel Error 3; исправления 60 м и Draw Instanced с Undo, Grass в данных террейна и на
диске; 16 на клетку — Critical; «Draw» выключен — без записей травы; тихий террейн — ни одной записи). С 2.3.0 — шаг `headset` (наборы шлемов, умолчания и старый файл
бюджетов, время кадра и прозрачное по частоте и проценту, «свои пороги», шаг физики на 120 Гц, поле зрения точек обзора,
отчёт). С 2.2.0 — шаг `quality-checks` (пороги MSAA 0 / 2 / 4 / 8, дальность
теней 40 / 41 м, Skin Weights, мягкие тени с исправлением и Undo, Realtime Reflection Probes, Soft Particles, VSync, разбор
скриптов) и пара `frame-rate-script-make` / `frame-rate-script-check` (настоящий скрипт с `targetFrameRate` в проекте —
одна запись «Сведения», после проверки скрипт удаляется). С 2.1.2 шаг `gpu` проверяет RTX 4060 и RTX 2070 по умолчанию. С 2.1.1 — шаг `window-fields` (стили и значки окон не переживают
перезагрузку скриптов) и проверка окна в видимом редакторе `scripts\run-window-runs.ps1` (прогоны всех охватов на тяжёлых
сценах, все группы и карточки открыты, «По категориям», узкое окно, стили после перезагрузки — без исключений; со старой
логикой стилей она даёт ту же ошибку, что у владельца, `AuditorWindow.Problems.cs:351`). С 2.1.0 — шаг `help`: 30 карточек проверок на двух языках, «Выигрыш»
ровно у проверок с оценкой (и у тех, что дают выигрыш на сцене теста), меню «Справка» и якоря руководства и справочника
(`-helpDocs`), действия карточек «С чего начать», «С чего начать» из меню и его закрытие прогоном, `DC_SHADOW_CASTERS` без
проходов теней. С 2.0.0 — шаг `gain`: выигрыш на известных ассетах (текстура 4096 до 2048 — три
четверти памяти, несжатая RGB24 → DXT1, Read/Write — копия, 5 камней без instancing — 4 DC, 3 материала на 2 submesh —
≈ 2 и 1 DC, меш 30 000 без LOD — ≈ половина, проба 1024 — 15/16, realtime point над 3 объектами — ≈ 3 DC, контур вторым
проходом на 2 объектах — 2 DC, длинный клип — ≈ 85 % PCM, аниматор — без выигрыша), порядок «По выигрышу» и доля по
своему порогу, итог без допустимых, JSON и CSV. Вид — снимками («По выигрышу» RU/EN, обычный порядок, ширина 560). С
1.9.0 — шаг `scope`: выделенное (здание с детьми и его текстура — в списке, чужой
аниматор и чужая текстура — нет, записей без объекта нет), три сцены сборки (открыта A: после проверки снова A, A
проверена последней и исправляется, B и C — закрыты; текстура из двух сцен — одна запись, «в 2 сценах»; настройка
проекта — одна; «нет VR-камеры» — по сцене с её именем; исправление общей текстуры и обновление — строки текстуры
уходят из всех сцен, строки закрытых остаются), командная строка (две сцены через запятую, флаг дважды и CSV со
`ScenePath`, `-auditBuildScenes`, одна сцена — прежний отчёт, `-auditHistory` — точка у каждой сцены, `-auditFolder`),
папка (префаб, модель, свободные материал, текстура и клип; «Crate.prefab › Lid» без исправления, текстура вне папки не
в списке, исправление импорта, скрытая сцена не оставляет объектов). Кнопка охвата, таблица «Сцены» и «Открыть
сцену» — снимками; диалоги сохранения и отмена хода — вручную. С 1.8.0 — шаг `reflections`: восемь проб (комната без box и её почти копия, ларёк внутри уличной пробы — не дубликат,
таверна 1024 и зал 512 без запекания, два атриума 2048 на память, realtime-улица 60 м) — данные `LightReport` (МБ
запечённой кубической текстуры и оценки, объекты внутри, дубликат, вне проб — ближние первыми, Reflection Probes Off не в
счёт), все шесть проверок, «Включить box» и Undo, тишина без проб и с одной хорошей пробой, JSON, тексты RU/EN; раздел
«Отражения» (сводка, фильтр, кнопка, узкая таблица) — только снимками: IMGUI в batch-редакторе не рисуется. С 1.7.0 — шаг `shaders`: разбор исходника на строках (комментарии,
Vertex / VertexLM / VertexLMRGBM как один проход, surface, UsePass тени и контура, Category, GrabPass с именем и без,
AlphaToMask из свойства и на Category, шаблон Unlit, `vert_img`, своя функция в `v2f_img` с вызовами, поле без вызовов,
UsePass `METALLIC_OUTLINE`) и 11 шейдеров, которые тест пишет в проект (хороший, GrabPass с
именем и без, контур вторым проходом, ForwardBase + ForwardAdd + ShadowCaster + UsePass, шаблон Unlit, вершинная функция
из `.cginc` проекта, cutout с AlphaToMask и без, 1024 варианта, ошибка разбора), Standard opaque и cutout, материал с
удалённым шейдером; при Single Pass Instanced и MSAA 4x — и без них. С 1.6.0 — шаг `particles`: системы частиц на каждую проверку (оценка частиц
одновременно — Rate × Lifetime, Max Particles, Bursts и повторяющиеся Bursts, незацикленная, Rate over Distance,
выключенная Emission, суб-эмиттер, кривая; размер — 3D, Size over Lifetime, Scaling Mode Hierarchy / Local / Shape),
тишина на обычной и неактивной системе, на крупной непрозрачной и на малочисленной, на модуле Lights без Light, четыре исправления и одно Undo, пять копий префаба факела — «Исправить в
префабе» выключает модуль Lights в префабе без override у копий, тексты RU/EN. С 1.5.0 — шаг `heavy`: 30 копий префаба с `ANIM_ALWAYS_ANIMATE` —
одна строка копий, план «одно исправление», «Исправить в префабе» (префаб изменён, 29 копий без override, копия со своим
значением исправлена на себе, новый прогон чист, Ctrl+Z откатывает префаб и копии), часть копий / допустимая / удалённая
копия — исправления по копиям, копии модели (OBJ) — строка без «в префабе», «Тяжёлые» на 40 копиях префаба, свободных
мешах и LODGroup (копии без LOD1, LOD0, draw calls, «нет LOD» по копиям, доли, сумма долей текстур), CSV и `heavy` в JSON. С 1.4.0 — шаг `light`: вкладка «Свет» и 12 проверок света на
подставленных атласах без запекания (`LightmapSettings.lightmaps`, `lightmapIndex`, `lightmapScaleOffset`, пробы и
источники выставлены в редакторе) — данные `LightReport` (память и доли атласов, «мелкий», «много атласа», «без lightmap»,
источники в зоне, Important, незапечённый Baked, вне проб), все 12 проверок на одной сцене (`LIGHT_NO_PROBES` — после
удаления группы проб), Critical памяти, «ничего не запечено» с Baked GI и без, тишина на чистой сцене, исправление «На
пробы» и Undo, `lighting` в JSON, память lightmaps в точке истории. Рисование вкладки — только снимками; график и
сравнение памяти lightmaps в «Истории» тестом не покрыты. С 1.3.0 — шаги `viewpoints-tour` (сэмплер кадров и снимок
профайлера на подставленных кадрах, ход 10 точек × 4 направления и прерывание после 4-й, ход из JSON после перезагрузки
домена, «Headroom under 90 Hz», перенос rig с головой в 0,3 м и повёрнутой на 20°, камера, заморозка и возврат
физики, файл точек, JSON, история «Faster after fixes»), `viewpoints-tour-start` и `viewpoints-tour-check` (короткий
замер в Play Mode в тестовом редакторе: 3 точки × 4 направления, без шлема; камеры в сцене нет — rig с головой в 0,3 м
и повёрнутой на 20° создаётся на 5-м кадре и находится после успокоения, голова встаёт в каждую точку; вход, замер и
выход из Play Mode).
Снимки «Замера» — на подставленных числах. Со шлемом и на настоящем rig не проверено — за владельцем. С 1.2.0 — шаги `gpu` (наборы видеокарт, старый файл бюджетов),
`viewpoints` (LOD, расстановка по NavMesh и лучам, предел 500, файл точек, Undo, отмена, счёт с тенями, прозрачное,
стены, 40 копий префаба, CSV, JSON, `-auditViewpoints`, история) и `viewpoints-teleports` (заглушки
`TeleportPoint` / `TeleportArea`, между `steamvr-on` и `steamvr-off`). 1.1.1 — 29 из 29 (окно и тексты разрезаны по
файлам; снимки окна до и после совпали, кроме времени прогона). Тесты — в batch-редакторе, снимки — в видимом; смена режима закрывает один
редактор и открывает другой. До 24.10.2026 — по запуску Unity на шаг (22.10.2026 — 25 шагов, дальше шаги
добавлялись этапами) (с 0.6.0 — проверки развёртки под свет, с 0.7.0 — шаг `lightmap-report`, с 0.8.0 —
проверки 3D-ассетов, с 0.9.0 — шаг `multi-select`, с 1.0.0 — `accepted`, с 1.1.0 — `history`; `MESH_IMPORT_CAMERAS_LIGHTS` не покрыт: OBJ не хранит камер и света) (14 вариантов настроек, из них 4 — XR Plug-in Management;
экспорт — модуль и 5 запусков из командной строки, исправления, SteamVR-заглушка в двух вариантах). Утилита
импортируется без ошибок и предупреждений, утечек NativeArray нет. Тесты — `tests/AuditTests/`, отчёты —
`unity-tools-scratch\reports\audit-tests\`.

Найдено и исправлено в утилите:
- проблемы моделей (`.obj`, `.fbx`) указывали на корень модели как на объект сцены — теперь это ассет с путём;
- DSP Buffer Size читался из `m_DSPBufferSize` (итоговый размер) — настройка лежит в `m_RequestedDSPBufferSize`;
- режим динамиков читался из `m_DefaultSpeakerMode` — в 2019.4 поле называется `Default Speaker Mode`.

Unity не даёт выставить near clip меньше 0.01, поэтому `VR_NEAR_CLIP` (< 0.01) не срабатывал никогда. Владелец
22.10.2026: поднять порог — теперь < 0.05, уровень «Сведения» (0.01–0.03 в VR встречается часто).

22.10.2026, по слову владельца, в проверочный проект поставлен `com.unity.xr.management` 3.2.13 (из локального
кэша Unity). OpenVR XR Plugin не ставился: загрузчик и его настройки заменены заглушками с теми же именами
(`tests/AuditTests/XR/`). Добавлены варианты `xr-openvr-spi`, `xr-openvr-multipass`, `xr-mock`, `xr-both`.

Сверено: `ShaderUtil.HasInstancing` находится через reflection (`DC_INSTANCING_OFF` и безопасное исправление
работают); переключение уровня качества не меняет `QualitySettings.asset`.

Живую проверку (окно, снимок профайлера, настоящая сцена) владелец подтвердил 22.10.2026 («Сделал»).
Не проверено: настоящий OpenVR XR Plugin (в тестах — заглушки);
остальное — пункт 8 ниже.

Что проверить, когда дойдём до тестов. Код всех четырёх этапов написан и компилируется компилятором
Unity 2019.4.41f2 (`scripts\compile-check.ps1`: 0 ошибок, 0 предупреждений); с 22.10.2026 — и в Unity (итоги выше).
Отмечать `[x]` по мере прохождения; итог прогона — в отчёт `unity-tools-scratch\reports\`.

## 0. Подготовка

- [x] Решение владельца: ставить ли `com.unity.xr.management` (и OpenVR XR Plugin) в проверочный
      проект — только в scratch, не в утилиту. Без них ветка XR Plug-in Management проверяется лишь как
      «не найден».
- [x] Проверочный проект `E:\dev_personal\unity-tools-scratch\TestProject2019` на 2019.4.41f2
      (`Unity.exe -batchmode -quit -createProject <путь> -logFile <файл>`).
- [x] Папка `SceneOptimizationAuditor` подключена в `Assets/` junction-ом на репозиторий; Unity создала
      `.meta` в репозитории; предупреждение Unity о symlink допустимо.
- [x] Набор проверочных скриптов — в `TestProject2019/Assets/AuditTests/Editor/` (не в утилите):
      генератор сцены `AuditSeed`, генератор ассетов, заглушки пакетов, прогон с ожиданиями.
- [x] Прогон: `-executeMethod AuditTests.Run -auditVariant <имя>`; сверка — по парам
      (`checkId`, путь объекта или ассета): «должно быть» и «не должно быть»; лишние срабатывания —
      в отчёт, но не провал; код выхода 0 / 1.

## 1. Компиляция в самом Unity

- [x] Импорт в 2019.4.41f2 batchmode: код выхода 0, в журнале нет `error CS` и нет `warning CS` из
      файлов утилиты.
- [x] Нет предупреждений `A Native Collection has not been disposed` после прогона (`Mesh.GetBonesPerVertex`
      не освобождается — по документации не нужно, проверить).

## 2. Сцена AuditSeed — базовый вариант

Ассеты генерируются кодом: PNG (`Texture2D.EncodeToPNG`), WAV, меши `.obj` (идут через ModelImporter),
skinned-меши `.asset` (`Mesh.SetBoneWeights` с нужным числом костей). Базовые настройки проекта
задаются тестом явно: Anti Aliasing 4x, VR выключен, остальное по умолчанию.

| Объект / ассет | Должно быть | Не должно быть |
|---|---|---|
| **Draw Calls** | | |
| `DC/Five` — куб, 5 материалов | `DC_MANY_MATERIALS` W, `DC_EXTRA_MATERIALS` W | `DC_MULTI_MATERIAL` |
| `DC/Two` — меш с 2 submesh, 2 материала | `DC_MULTI_MATERIAL` I | `DC_EXTRA_MATERIALS` |
| `DC/Candidate` — куб без скриптов, не static | `DC_STATIC_CANDIDATE` I | |
| `DC/Static` — Batching Static | | `DC_STATIC_CANDIDATE` |
| `DC/Moved` — с Rigidbody; `DC/UnderScript` — родитель с MonoBehaviour | | `DC_STATIC_CANDIDATE` |
| `M_NoInst` на 4 нестатичных кубах | `DC_INSTANCING_OFF` W (или `_UNKNOWN` I — см. п. 8) | |
| `M_NoInst3` на 3 кубах | | `DC_INSTANCING_*` |
| сцена с тенями и realtime/mixed-источником с тенями | `DC_SHADOW_CASTERS` I | |
| **Meshes** | | |
| `Mesh/12k` без LOD | `MESH_NO_LOD` W | |
| `Mesh/30k` без LOD | `MESH_NO_LOD` C | |
| `Mesh/30kLod` в LOD0 у LODGroup | | `MESH_NO_LOD` |
| `ReadWrite.obj` (Read/Write) | `MESH_READ_WRITE` W, исправление R | |
| `ReadWriteCollider.obj` у MeshCollider | `MESH_READ_WRITE_NEEDED` I | `MESH_READ_WRITE` |
| `NotOptimized.obj` (Optimize Mesh = Polygon Order) | `MESH_NOT_OPTIMIZED` I | |
| `Skin8.asset` — до 8 костей на вершину | `MESH_BONES_PER_VERTEX` W | |
| **Textures** | | |
| `T_4096.png` (Max Size 4096) | `TEX_SIZE` W | |
| `T_8192.png` (Max Size 8192) | `TEX_SIZE` C | |
| `T_Cube8192` (cubemap) | `TEX_SIZE` W | `TEX_SIZE` C |
| `T_FakeNormal.png` (Default) в `_BumpMap` | `TEX_NOT_NORMAL_MAP` C | |
| `T_Normal.png` (Normal map) в `_BumpMap` | | `TEX_NOT_NORMAL_MAP` |
| `T_NoMips.png` на 3D-материале | `TEX_NO_MIPMAPS` W, исправление S | |
| `T_OverlayOnly.png` без mips, только RawImage на Overlay-канвасе | | `TEX_NO_MIPMAPS` |
| `T_Raw.png` (Compression = None) | `TEX_UNCOMPRESSED` W, формат `Uncompressed` | |
| `T_Rgba32.png` (override Standalone RGBA32, с альфой) | `TEX_UNCOMPRESSED` W, подсказка `BC7` | |
| `T_Readable.png` | `TEX_READ_WRITE` W | |
| любая 3D-текстура Bilinear с mips | `TEX_NOT_TRILINEAR` I — одна запись | |
| **Lighting** | | |
| `Light/PointRealtime` | `LIGHT_REALTIME` W | |
| `Light/SpotMixed` | | `LIGHT_REALTIME` |
| 3 источника с тенями (Directional, Point realtime, Spot mixed) | `LIGHT_SHADOWED` C | |
| `GI/NoUv1` — Contribute GI, Receive GI = Lightmaps, меш без UV1 | `LIGHT_NO_UV1` W | |
| `GI/Probes` — то же, Receive GI = Light Probes | | `LIGHT_NO_UV1` |
| **Physics** | | |
| `Phys/ConcaveDynamic` | `PHYS_CONCAVE_DYNAMIC` C | |
| `Phys/ConcaveKinematic` | `PHYS_CONCAVE_KINEMATIC` I | |
| `Phys/ChildOfBody` — Rigidbody у родителя | `PHYS_CONCAVE_DYNAMIC` C | |
| `Phys/RenderMesh12k` | `PHYS_RENDER_MESH_COLLIDER` I | |
| Fixed Timestep 0.02 (по умолчанию) | `PHYS_FIXED_STEP_SLOW` W | |
| **Animators** | | |
| `Anim/Default` — Animator по умолчанию, без контроллера | `ANIM_ALWAYS_ANIMATE` W, `ANIM_NO_CONTROLLER` I | |
| `Anim/RootMotion` | `ANIM_ROOT_MOTION` I | |
| `Anim/RootMotionAgent` — с NavMeshAgent | | `ANIM_ROOT_MOTION` |
| `Anim/RootMotionScript` — скрипт с `OnAnimatorMove` | | `ANIM_ROOT_MOTION` |
| `Anim/Offscreen` — SkinnedMeshRenderer, Update When Offscreen | `ANIM_UPDATE_OFFSCREEN` W | |
| `Anim/Heavy` — SkinnedMesh 16k под Animator | `ANIM_HEAVY_SKIN` W | |
| `Light/Directional` (realtime) и `Light/SpotMixed` с Soft Shadows; уровень качества — Hard and Soft | `LIGHT_SOFT_SHADOWS` I (у Directional — с исправлением) | `LIGHT_SOFT_SHADOWS` у `Light/PointRealtime` (Hard) |
| **Audio** | | |
| `A_35s.wav` (по умолчанию: Decompress On Load, Preload) | `AUDIO_DECOMPRESS_LONG` W, `AUDIO_PRELOAD_LONG` W | |
| `A_12s_pcm.wav` (PCM, > 1 МБ) | `AUDIO_DECOMPRESS_LONG` W, `AUDIO_PCM_LARGE` W | |
| `A_5s.wav` | | все `AUDIO_*_LONG`, `AUDIO_PCM_LARGE` |
| `Audio/NoClip` | `AUDIO_NO_CLIP` I | |
| AudioSource с Spatial Blend 0 / 1 | `AUDIO_2D_SOURCE` I / — | |
| 6 источников с priority 0 | `AUDIO_PRIORITY_ZERO` W | |
| два AudioListener, один не на камере | `AUDIO_LISTENERS_MANY` C ×2, `AUDIO_LISTENER_NO_CAMERA` I | |
| проект: DSP Default, без spatializer | `AUDIO_DSP_BUFFER` I, `AUDIO_NO_SPATIALIZER` I | `AUDIO_SPEAKER_MODE` |
| **VR** | | |
| в проекте нет скриптов, задающих частоту кадров | | `VR_FRAME_RATE_SCRIPT` |
| VR выключен | `VR_NOT_ENABLED` C | `VR_STEREO_MODE` |
| Main Camera (HDR по умолчанию) | `VR_CAMERA_HDR` W | |
| `Cam/Far` (far 2000), `Cam/Near` (near 0.001) | `VR_FAR_CLIP` I, `VR_NEAR_CLIP` I (Unity поднимает near до 0.01, порог 0.05) | |
| `Cam/Deferred`, `Cam/NoMsaa` | `VR_DEFERRED_CAMERA` W, `VR_CAMERA_NO_MSAA` W | |
| `Cam/ToTexture` (Target Texture) — те же «плохие» настройки | | все `VR_*` по камере |
| `Cam/Effect` — скрипт с `OnRenderImage` | `VR_IMAGE_EFFECT` W | |
| `UI/Overlay`, `UI/ScreenCamera`, `UI/World` | `VR_CANVAS_OVERLAY` C, `VR_CANVAS_CAMERA` W / — | |
| заглушка PPv2: Layer на камере, Volume с Bloom и MotionBlur | `VR_PP_LAYER` W, `VR_PP_EFFECTS` W («Expensive in VR: MotionBlur») | |
| SteamVR-типов нет | `VR_STEAMVR_MISSING` I | |
| `Probe/Realtime` (via scripting), `Probe/EveryFrame` | `VR_PROBE_REALTIME` W, `VR_PROBE_EVERY_FRAME` C | |
| `Probe/Baked` | | `VR_PROBE_*` |
| **General** | | |
| `Gen/Empty` | `GEN_EMPTY_LEAF` I | |
| `Gen/Mirrored` (scale x = −1) | `GEN_NEGATIVE_SCALE` I | |
| цепочка из 12 уровней | `GEN_DEEP_HIERARCHY` I — ровно одна запись | |
| `Gen/Missing` — пропавший скрипт (подменить guid в `.unity`) | `GEN_MISSING_SCRIPTS` W | |
| `Gen/InactiveHeavy` (меш 12k), `Gen/InactiveTex` (T_4096) | `GEN_INACTIVE_HEAVY_MESH` I, `GEN_INACTIVE_HEAVY_TEXTURE` I | |
| **Прочее** | | |
| [x] Один `T_NoMips.png` на 50 объектах — одна запись `TEX_NO_MIPMAPS` с `users = 50` | | |
| [x] Сцена `AuditSeedB` загружена вместе с `AuditSeed` — у записей обе сцены по имени | | |
| [x] Прогон не меняет сцену и ассеты: хэши `.unity`, `.meta` и `ProjectSettings/*.asset` до и после равны (уровень качества переключается на время чтения) | | |

## 3. Варианты настроек (`-auditVariant`)

| Вариант | Настройки | Ожидание |
|---|---|---|
| `legacy-multipass` | legacy VR, OpenVR, Multi Pass | `VR_STEREO_MODE` W (MultiPass); шапка «Legacy VR (OpenVR)»; оценка draw calls ×2 к варианту `legacy-spi` |
| `legacy-spi` | то же, Single Pass Instanced | нет `VR_STEREO_MODE`, `VR_NOT_ENABLED` |
| `legacy-no-openvr` | legacy VR, SDK `None` / `Mock HMD` | `VR_NO_OPENVR` W |
| `xr-openvr-spi` | XR Management, загрузчик OpenVR (заглушка), SPI | шапка «XR Plug-in Management (OpenVRLoader)», режим SPI из `OpenVRSettings`, нет `VR_NOT_ENABLED`, `VR_STEREO_MODE` |
| `xr-openvr-multipass` | то же, Multi Pass | `VR_STEREO_MODE` W, `DC_BUDGET` W (оценка ×2) |
| `xr-mock` | XR Management, загрузчик не OpenVR | `VR_NO_OPENVR` W, режим stereo «неизвестно» |
| `xr-both` | legacy OpenVR и XR Management вместе | `VR_BOTH_SYSTEMS` W |
| `quality` | Standalone по умолчанию — уровень, отличный от текущего; на нём: AA выключен, Pixel Lights 4, Shadow Resolution Very High, Shadow Distance 150, Anisotropic Disable, Skin Weights Unlimited, Soft Particles и Realtime Reflection Probes включены, VSync Every V Blank | `VR_MSAA`, `LIGHT_PIXEL_LIGHTS`, `LIGHT_SHADOW_RESOLUTION` W, `LIGHT_SHADOW_DISTANCE`, `TEX_ANISO_DISABLED`, `MESH_QUALITY_SKIN_WEIGHTS`, `REFL_QUALITY_REALTIME`, `VR_VSYNC`; без `VR_MSAA_HIGH` и `PART_SOFT_PARTICLES` (частиц в сцене нет); текущий уровень после прогона прежний |
| `project` | GPU Skinning off, Solver Iterations 10, Fixed Timestep 0.005, Realtime GI on, Deferred в Tier 3, Speaker Mode 5.1, DSP Best Latency, Color Space Gamma | `MESH_GPU_SKINNING_OFF`, `ANIM_HEAVY_SKIN_CPU`, `PHYS_SOLVER_ITERATIONS`, `PHYS_FIXED_STEP_FAST`, `LIGHT_REALTIME_GI`, `VR_DEFERRED_TIER`, `AUDIO_SPEAKER_MODE`, `AUDIO_DSP_BUFFER`, `LIGHT_GAMMA_SPACE`; без `TEX_SRGB_DATA` (только в Linear) |
| `fixed-step-90` | Fixed Timestep 0.0111 | нет `PHYS_FIXED_STEP_*` |
| `budgets-low` | файл бюджетов с порогами ниже значений сцены | `DC_BUDGET`, `MESH_SCENE_TRIS`, `TEX_MEMORY` — W и C на разных порогах |
| `counts` | 21 Animator, 33 AudioSource, 101 статичный рендерер без occlusion | `ANIM_COUNT`, `AUDIO_SOURCE_COUNT`, `VR_NO_OCCLUSION` |
| `steamvr-not-in-scene`, `steamvr-in-scene` (между `steamvr-on` и `steamvr-off`) | заглушка `Valve.VR.SteamVR_PlayArea`, не в сцене / в сцене | `VR_STEAMVR_NOT_IN_SCENE` I / ничего |
| `no-listener` | все AudioListener выключены | `AUDIO_NO_LISTENER` W |

## 4. Экспорт (этап 2)

- [x] `RunFromCommandLine` по `AuditSeed` в `.json` и `.csv`: код выхода 0, файлы есть.
- [x] JSON читается (`python -c "import json; json.load(open(...))"`); `summary` совпадает с числом
      записей по уровням; `hasProfilerSnapshot` = false до снимка; `timestamp` — ISO 8601.
- [x] CSV: UTF-8 с BOM; `csv.reader` даёт столько строк, сколько записей; текст с запятой и кавычками
      экранирован (добавить объект с именем `a, "b"`).
- [x] `-auditLang ru`: `severity` по-английски, `message` по-русски.
- [x] Без `-auditScene` — ошибка в журнале и код выхода 1; несуществующая сцена — код 1.

## 5. Исправления (этап 3)

- [x] На копии `AuditSeed` вызвать `AutoFixer.Apply` для всех Safe: пропали `TEX_NO_MIPMAPS` и
      `DC_INSTANCING_OFF`, остальные `checkId` на месте.
- [x] Каждое Review-исправление по одному: после повторного аудита его запись пропала; ничего лишнего
      не изменилось (сравнить `.meta` и сцену до и после).
- [x] `Undo.PerformUndo()` после сценовых исправлений возвращает Batching Static, Culling Mode,
      Update When Offscreen, Spatial Blend.
- [x] Исправление объекта в экземпляре префаба — появился override, префаб-ассет не изменился.
- [x] Материал из `Packages/` (или вложенный в модель) — исправление не предлагается.
- [x] Окно подтверждения (вручную, владелец): группы «Сцена» и «Ассеты», галочки, «Отмена» ничего не
      меняет, «Применить (N)» считает отмеченные.

## 6. Снимок профайлера (этап 4) — вручную, владелец

- [x] Из Edit Mode: вход в Play Mode, разогрев, N кадров, выход; снимок в блоке; в консоли строка
      `[SceneAudit] Profiler snapshot: …`.
- [x] Из Play Mode: Play Mode остаётся.
- [x] «Отмена» во время замера; Stop во время замера — предупреждение, состояние «Idle».
- [x] Перекомпиляция во время замера — предупреждение «interrupted by a domain reload».
- [x] С включённым Enter Play Mode Options (без перезагрузки домена) — замер проходит.
- [x] GPU: «н/д», если `FrameTimingManager` не отдаёт данные; в JSON −1.
- [x] Два снимка → «Сравнить с прошлым» показывает дельты; после перезапуска Unity снимки на месте.
- [x] Экспорт после снимка: `hasProfilerSnapshot` = true.
- [x] Кодом на подставленных данных: `ToJson` со снимком и без.

## 6а. Замер по точкам (этап 13, 1.3.0) — вручную, владелец

- [ ] Без шлема на настоящем уровне: из Edit Mode — вход в Play Mode, полоса хода («точка i из N · направление j из
      M», «Осталось ~…»), выход; сегмент «Замер» с числами, в консоли `[SceneAudit] Viewpoint tour done: …`.
- [ ] Со шлемом (SteamVR): «со шлемом» в сводке, GPU и «Пропущено» не «—»; rig переносится — голова в точке и
      смотрит в направление (по картинке в шлеме или Game view).
- [ ] «Отмена» и Stop во время замера — измеренные точки остались, «прерван»; из Play Mode — Play Mode остаётся.
- [ ] «Объект…» — свой rig или игрок: Rigidbody и CharacterController не мешают; после выхода из Play Mode — как были.
- [ ] Игра двигает игрока (телепорт на старте, скрипт движения) — метка «сдвинуто игрой».
- [ ] Панель «Замер в Play Mode»: числа и время сходятся с ходом; оба скина, RU/EN, ширина 560.
- [ ] «Оценка ↔ замер» правдоподобны: замер draw calls меньше оценки (батчинг, occlusion culling).

## 6б. Вкладка «Свет» (этап 14, 1.4.0) — вручную, владелец

- [ ] Запечённый уровень: «Проверить свет» — атласы, МБ и форматы сходятся с окном Lighting (Baked Lightmaps); доли
      объектов правдоподобны, клик по атласу выделяет его объекты.
- [ ] «Перевести мелкие на пробы» и «На пробы» у строки: окно подтверждения, Receive GI = Light Probes, Ctrl+Z
      возвращает; после перезапекания объект освещён пробами.
- [ ] «Источники»: число «В зоне» у point и spot правдоподобно, Important и широкие отмечены; «Запечён» — «нет» у
      незапечённых Baked/Mixed.
- [ ] «Пробы»: объект за краем Light Probe Group в списке с расстоянием; без групп — подпись.
- [ ] «Настройки» у `LIGHT_LIGHTMAP_UNCOMPRESSED` / `LIGHT_LIGHTMAP_DIRECTIONAL` / `LIGHT_NOT_BAKED` открывает окно
      Lighting Settings.
- [ ] График и сравнение «Память lightmaps, МБ» на «Истории» после двух прогонов с разной памятью.
- [ ] Оба скина, RU/EN, ширина 560 (вкладки в две строки), «Только с пометками».

## 6в. Копии префабов и «Тяжёлые» (этап 15, 1.5.0) — вручную, владелец

- [ ] Уровень с префабами: «Копии вместе» — копии одного объекта префаба одной строкой, «копий: N», стрелка раскрывает.
- [ ] «Исправить в префабе»: окно подтверждения с путём префаба и числом копий; после — префаб изменён, у копий нет
      override этого свойства; вложенный префаб и вариант — правится ближайший ассет.
- [ ] Модель (FBX) — «Исправить копии (N)», копии исправляются на себе.
- [ ] «Тяжёлые»: «Собрать» на настоящем уровне — числа правдоподобны, клик по строке выделяет копии, имя — ассет;
      «Только без LOD», три сегмента, CSV.
- [ ] Оба скина, RU/EN, ширина 560 (вкладки в две строки).

## 6г. Частицы (этап 16, 1.6.0) — вручную, владелец

- [ ] Уровень с эффектами: категория «Частицы» — дым, искры, дождь; числа «частиц одновременно» правдоподобны, большой
      Max Particles при слабой эмиссии не шумит.
- [ ] Исправления «Выключить модуль Lights», «Collision Quality = Medium», «Cast Shadows = Off», «Включить GPU
      Instancing»: окно подтверждения, после — эффект выглядит приемлемо, Ctrl+Z возвращает.
- [ ] Префаб эффекта в нескольких копиях — «Исправить в префабе».

## 6д. Шейдеры (этап 17, 1.7.0) — вручную, владелец

- [ ] Уровень со своими шейдерами: категория «Шейдеры» — GrabPass, проходы, варианты правдоподобны; клик выделяет шейдер.
- [ ] Single Pass Instanced в шлеме: шейдер с `SHADER_NO_STEREO` действительно виден одним глазом, после макросов — двумя.
- [ ] Листва с alpha-test: `SHADER_ALPHA_TEST_MSAA`; с `AlphaToMask On` края при MSAA мягче.

## 6е. Reflection probes (этап 18, 1.8.0) — вручную, владелец

- [x] Доски «Отражения» на холсте — владелец принял 25.10.2026 («Доски «Отражения» подходят»).
- [ ] Запечённый уровень: «Свет» → «Отражения» — МБ сходятся с размером кубических текстур в Project, дубликаты и «без
      box» правдоподобны; «Включить box» и Ctrl+Z.
- [ ] Объекты вне проб в помещении: после расширения пробы уходят из списка.
- [ ] Оба скина, RU/EN, ширина 560.

## 6ж. Охват проверки (этап 19, 1.9.0) — вручную, владелец

- [x] Доски «Охват» на холсте — владелец принял 25.10.2026 («Доски «Охват» подходят»).
- [ ] «Сцены сборки» на своём проекте: изменённая сцена — предложение сохранить; ход по сценам с отменой; после —
      открыты те же сцены; таблица «Сцены» правдоподобна; «Открыть сцену» у записи — сцена открывается, объект выделен,
      запись можно исправить.
- [ ] «Выделенное»: выделить участок уровня — в списке только он и его ассеты.
- [ ] «Папка…»: выбрать папку с новыми ассетами — записи префабов «<файл> › <путь>», исправления импорта работают.
- [ ] Командная строка: `-auditBuildScenes` на своём проекте в batch.

## 6з. Оценка выигрыша (этап 20, 2.0.0) — вручную, владелец

- [x] Доски «Выигрыш» на холсте — владелец принял 25.10.2026 («Доски «Выигрыш» подходят»).
- [ ] Свой уровень: «По выигрышу» — сверху то, что правда даёт больше всего; числа правдоподобны (после исправления
      «Память текстур» на «Бюджетах» падает примерно на обещанное).
- [ ] Итог «Если исправить всё» — порядок величин похож на правду.

## 6и. Справка в окне (этап 21, 2.1.0) — вручную, владелец

- [x] Доски «Справка» на холсте (меню, «Подробнее о проверке», «С чего начать») — владелец принял 25.10.2026 («Доски «Справка» подходят»).
- [ ] Меню «Справка» (снимком не снимается): пункты, «Справка по вкладке» открывает нужный раздел руководства в браузере.
- [ ] «С чего начать» на своём проекте: карточки задач делают свой шаг; светлый скин.
- [ ] «Подробнее о проверке» у своих проблем: тексты понятны и верны; светлый скин.
- [ ] Названия настроек Unity в карточках — как в Inspector 2019.4: override PC, Mac & Linux Standalone у текстуры, Non Power
      of 2 / To Nearest, Generate Lightmap UVs (Model), Read/Write Enabled, Compress Lightmaps и Generate Lighting (окно
      Lighting Settings), Max Particle Size, Receive Global Illumination / Contribute GI, Shadow Cascades, 4x / 8x Multi
      Sampling, Stereo Rendering Mode (Player и OpenVR), Occlusion Culling → Bake, Alpha Source, Compression, Culling Mode,
      Load Type, Spatial Blend, Event Camera, Rendering Mode.
- [ ] Подсказка у неактивной карточки «Перед сборкой»; перенос тулбара на ширинах 560–720 в RU и EN; ссылки с якорями
      открывают нужные разделы на GitHub (после пуша).

## 6к. Настройки качества под VR (этап 22, 2.2.0) — вручную, владелец

- [ ] Новые записи на своём проекте: Gamma, мягкие тени (и «Исправить» → Hard Shadows), Realtime Reflection Probes и Soft
      Particles в Quality, VSync, скрипты с `targetFrameRate`; MSAA 2x без предупреждения.
- [ ] Названия в Inspector 2019.4: Skin Weights «2 Bones», Shadows «Hard and Soft Shadows» / «Hard Shadows Only», VSync
      Count «Don't Sync», Anti Aliasing «2x Multi Sampling».

## 6л. Целевой шлем (этап 23, 2.3.0) — вручную, владелец

- [ ] Доски «Бюджеты: шлем» на холсте — слово владельца (задача 1.1 `add-target-headset`).
- [ ] Свой проект: Vive Pro 2 Wireless на 90 Гц и своём проценте SteamVR — замер со шлемом сходится с временем кадра 11,1 мс;
      строки шлема в светлом скине.
- [ ] Числа рендера на глаз совпадают с тем, что показывает SteamVR для приложения (панель × 1,4 — допущение).

## 6м. Трава и террейн (этап 24, 2.4.0) — вручную, владелец

- [ ] Свой проект с травой: число травинок в записи `TERRAIN_DETAIL_INSTANCES` на глаз похоже на то, что видно в шлеме
      (оценка — по средней плотности, кусты травы у игрока могут быть гуще); категория «Террейн» в окне.
- [ ] «Исправить» у `TERRAIN_GRASS_BILLBOARD` — Render Mode = Grass у слоя в Paint Details, данные террейна сохранены на диск.
- [ ] Пороги — предложение сессии: 100 000 / 300 000 травинок вокруг игрока, Detail Distance 60 м, Pixel Error 5 — сверить
      с профайлером в шлеме и поправить в `VRBudgets`.

## 6н. Допустимыми — любые записи (этап 25, 2.5.0) — вручную, владелец

- [ ] Галочка у настроек проекта и записей всей сцены (бюджеты, «Нет VR-камеры»); «Пометить допустимыми» — запись
      уходит в «Допустимые (N)»; в сценах сборки — только в своей сцене.
- [ ] Строки без объекта не съезжают: галочка в том же слоте, что у остальных.

## 6о. Тени по удалённости (этап 26, 2.6.0) — вручную, владелец

- [ ] Доски «Тени» на холсте — слово владельца (задача 1.1 `add-shadow-zone`).
- [ ] Свой уровень: «Поставить в центр вида», ручка в Scene view, круг радиуса, отметки дальних и легенда; Ctrl+Z
      возвращает перенос.
- [ ] «Выключить дальше N м (K)» с Cast Shadows, Receive Shadows и Contribute GI — в окне подтверждения список, после —
      Ctrl+Z возвращает всё; поиск по имени группы снимает только внутри неё.
- [ ] История: два прогона подряд — две точки, «С прошлой точки» под строкой контекста.

## 7. Окно — вручную, владелец

- [x] Оба скина (Personal и Pro) × EN / RU: тексты не обрезаются, иконки и полоски видны.
- [x] Смена языка после прогона — тексты записей сменились без нового прогона.
- [x] Клик по записи объекта — выделение и подсветка в Hierarchy; ассета — в Project; настройки —
      открывается нужная страница Project Settings.
- [x] Фильтры уровней, поиск по имени, пути ассета и тексту.
- [x] «Показать ещё» при > 200 записях в категории.
- [x] Бюджеты: правка, «Сохранить» создаёт `ProjectSettings/SceneOptimizationAuditor.json`,
      «По умолчанию», новый прогон учитывает пороги.
- [x] Отмена прогресса прогона — прежний результат остаётся.
- [x] После перекомпиляции — пустое состояние с кнопкой «Запустить аудит», окно без ошибок.
- [x] Прогон на настоящей VR-сцене владельца — время прогона и правдоподобие записей.

## 8. Что сверить в API 2019.4 при первом запуске

- [x] `ShaderUtil.HasInstancing` (непубличный) находится через reflection — иначе вместо
      `DC_INSTANCING_OFF` всегда `DC_INSTANCING_UNKNOWN` и безопасного исправления instancing нет.
- [x] `UnityStats.frameTime` — в секундах (код умножает на 1000); совпадает с окном Stats.
- [x] `QualitySettings.SetQualityLevel` туда и обратно не оставляет изменённым `QualitySettings.asset`.
- [x] `m_PerPlatformDefaultQuality`, `m_BuildTargetVRSettings`, `m_DSPBufferSize`,
      `m_DefaultSpeakerMode` читаются через `SerializedObject` (имена полей в 2019.4).
- [x] XR Management (если пакет поставлен): `XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget`,
      `Manager.activeLoaders` / `loaders`, поле `OpenVRSettings.StereoRenderingMode`.
- [x] `Application.onBeforeRender` и `EditorApplication.update` в Play Mode дают по одному замеру на кадр.
