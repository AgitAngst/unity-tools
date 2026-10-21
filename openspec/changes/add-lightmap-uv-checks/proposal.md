# Proposal

## Why

Владелец (22.10.2026): «у меня всегда проблемы с lighting UV». Сейчас аудит ловит только отсутствие UV1 у
объекта под lightmap. Нужно видеть, стоит ли у модели галочка Generate Lightmap UVs, есть ли второй
UV-канал и годится ли он для запекания: внутри 0–1 и без наложений.

## What Changes

- Объекты, которые запекаются в lightmap (Contribute GI и Receive GI = Lightmaps), проверяются по мешу:
  - нет UV1 — запись `LIGHT_NO_UV1`; если меш из модели с выключенной галочкой — исправление «Включить
    Generate Lightmap UVs» (по одной записи, переимпорт модели);
  - UV1 выходит за 0–1 — `LIGHT_UV1_OUT_OF_RANGE`;
  - треугольники UV1 накладываются друг на друга — `LIGHT_UV1_OVERLAP` с долей наложения.
- Модель, у которой UV1 только из файла (галочка выключена), проходит, если развёртка в порядке.
- Тексты RU/EN, документация, тесты на сцене `AuditSeed`. Это этап плана: версия 0.6.0.

## Capabilities

### New Capabilities
- нет

### Modified Capabilities
- `scene-audit`: проверки lightmap UV для запекаемых объектов.

## Impact

- Код: `Checks/LightingChecks.cs`, `Strings.cs`, `VRBudgets.cs` (пределы).
- Документация: `docs/scene-optimization-auditor.md`; тесты: `tests/AuditTests`.
- Исправление меняет импорт модели (без Undo, переимпорт) — через окно подтверждения, как остальные.
