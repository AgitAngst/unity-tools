# Proposal

## Why

Reflection probe — кубическая текстура: большое разрешение съедает память, незапечённая проба и объекты вне проб
отражают небо, проба комнаты без box projection даёт «плывущее» отражение, перекрытые пробы — лишняя память. Сейчас
аудитор видит только realtime-пробы (`VR_PROBE_REALTIME`, `VR_PROBE_EVERY_FRAME`). Владелец, 25.10.2026: «делай частицы и
шейдер и reflection probe».

## What Changes

- Проверки в «Освещении»: `REFL_RESOLUTION` (W / C), `REFL_MEMORY` (W), `REFL_NOT_BAKED` (W), `REFL_NO_BOX` (I,
  исправление «включить Box Projection»), `REFL_DUPLICATE` (W), `REFL_OUTSIDE` (I).
- Вкладка «Свет»: пятый сегмент «Отражения» — сводка, таблица проб с метками и «Включить box», объекты вне проб.
- JSON — в `lighting`: `reflectionProbes`, `reflectionMB`, `outsideReflection`.
- Пороги — константы `VRBudgets` (256 / 1024, 64 МБ, 30 м, 80 %).
- Этап плана: версия 1.8.0.

## Capabilities

### Modified Capabilities
- `scene-audit`: проверки reflection probes.
- `scene-audit-lighting`: раздел «Отражения».
- `scene-audit-export`: поля reflection probes в `lighting`.

## Impact

- Код (новое): `LightReport.Reflections.cs` (пробы, память, дубликаты, объекты вне проб, исправление «Включить box»),
  `Checks/ReflectionChecks.cs`, `Strings.Reflections.cs`. Правка: `LightReport.cs` (partial, сбор),
  `Checks/LightingChecks.cs`, `AuditorWindow.Light.cs` (сегмент), `VRBudgets.cs`, `ReportExporter.cs`, `Strings.cs`.
