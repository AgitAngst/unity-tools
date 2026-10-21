# Proposal

## Why

Статический аудит не показывает реальную цену кадра. Нужен быстрый ориентир прямо в редакторе —
FPS, время кадра, draw calls, память — с цветом по бюджету 90 Гц и сравнением с прошлым снимком.
Это этап 4 замысла `SPEC.md` → «SceneOptimizationAuditor».

## What Changes

- Блок «Profiler Snapshot» в окне: число кадров, разогрев, захват, сравнение с прошлым снимком.
- Захват в Play Mode с переживанием перезагрузки домена; выход из Play Mode, если входили сами.
- Метрики из публичного API (`UnityStats`, `Profiler`, `Texture`, `FrameTimingManager`); недоступное
  показывается как «n/a».
- Последний и предыдущий снимки — в `EditorPrefs` по проекту и сцене; снимок попадает в JSON-отчёт.

## Capabilities

### New Capabilities
- `scene-audit-profiler`: снимок метрик кадра в Play Mode редактора и сравнение снимков.

### Modified Capabilities
- нет

## Impact

- Новый код: `ProfilerCapture.cs`, блок в окне, поле снимка в отчёте.
- Входит и выходит из Play Mode по кнопке пользователя.
- Зависит от `add-scene-audit-checks` и `add-scene-audit-export`.
