# Design

## Context

Unity 2019.4, Built-in RP. Настройки запекания — статические `LightmapEditorSettings` и `Lightmapping` (ассета
`LightingSettings` ещё нет). Атласы — `LightmapSettings.lightmaps` (`lightmapColor`, `lightmapDir`, `shadowMask`),
у рендерера — `lightmapIndex`, `lightmapScaleOffset`, `receiveGI`, `lightProbeUsage`, Scale in Lightmap — через
`SerializedObject` (`m_ScaleInLightmap`). Пробы — `LightProbeGroup.probePositions` (запечённые `LightmapSettings.lightProbes` не нужны: область — по группам).
Источники — `Light.lightmapBakeType`, `bakingOutput`, `renderMode`, `range`. Всё собрано пробной компиляцией против DLL
2019.4.41f2 24.10.2026 (`unity-tools-scratch\probe\light`). Образец вкладки-таблицы — «Развёртка под свет»
(`LightmapUvReport`, `AuditorWindow.Lightmap.cs`).

## Goals / Non-Goals

**Goals:**
- Одной кнопкой увидеть память lightmaps и кто её занимает, незапечённое, дыры в пробах и широкие реалтайм-источники.
- Мелкие объекты — на пробы одним действием, с Undo.

**Non-Goals:**
- Запуск запекания и правка настроек Lighting из утилиты — только кнопка «Открыть Lighting».
- Reflection probes (отдельное предложение), Enlighten Realtime GI, Terrain.
- Предпросмотр плотности текселей в Scene view.

## Decisions

- **Свой сборщик `LightReport`**, как `LightmapUvReport`: строит атласы, объекты, источники, пробы за один проход по
  загруженным сценам; проверки на «Проблемах» берут числа из него же, чтобы вкладка и проверки не расходились.
- **Доля атласа** — `lightmapScaleOffset.x × .y`: часть атласа под развёртку объекта (с отступами). «Заполнено» атласа —
  сумма долей его объектов.
- **Память** — `TextureMemory.EstimateBytes` у цвета, направления и shadowmask: то же, что у текстур на «Бюджетах».
- **«Мелкий»** — большая сторона bounds меньше 0,5 м: свет из проб дешевле и без швов. Исправление — `receiveGI =
  LightProbes` через `FixKind.Review` и окно подтверждения; Contribute GI остаётся (объект отражает свет). Нужна
  перезапечка — пишется в окне подтверждения и в подсказке.
- **«Без lightmap»** — объект для lightmap с `lightmapIndex` вне атласов (не запекался после добавления или свет не
  запечён); источник Baked / Mixed с `bakingOutput.isBaked == false` — тоже не запечён. Одна проверка с двумя числами.
- **Объекты в зоне источника** — bounds рендерера пересекают сферу дальности (у spot — та же сфера, без конуса:
  дёшево и сверху). Directional — все активные рендереры в его culling mask.
- **Объекты на пробах** — рендереры с Light Probes = Blend Probes не в lightmap: подвижные и Receive GI = Light Probes
  (после «На пробы» объект тоже должен быть внутри проб). **Область проб** — коробка всех позиций проб из Light Probe
  Groups + 1 м. «Вне проб» — центр bounds такого объекта вне неё; расстояние до ближайшей пробы — перебором.
- **Окно Lighting** — `EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting Settings")`; путь настройки
  проверок «Lighting» открывает его, остальные — `SettingsService.OpenProjectSettings`.
- **Тесты без запекания:** `LightmapSettings.lightmaps`, `Renderer.lightmapIndex` и `lightmapScaleOffset`,
  `LightProbeGroup.probePositions`, `Light.bakingOutput` выставляются в редакторе — атласы подставляются текстурами
  известного размера и формата.

## Risks / Trade-offs

- [Данные вкладки живут в памяти окна и пропадают при перезагрузке домена] → «Проверить свет» заново; новый прогон
  аудита обновляет уже собранные данные.
- [Сфера вместо конуса у spot] → число «в зоне» сверху; подсказка колонки об этом говорит.
- [Пороги 0,5 м, 20 %, 50 объектов, 128 / 256 МБ — предложены сессией] → константы и бюджет на «Бюджетах»,
  калибровать на уровнях владельца.
