# Proposal

## Why

Результат аудита нужно сохранять и сравнивать между сборками и передавать без открытого редактора,
а для проверки утилиты — снимать отчёт в batchmode. Это этап 2 замысла `SPEC.md` →
«SceneOptimizationAuditor».

## What Changes

- Кнопка «Export Report» в окне: файл JSON или CSV по выбранному расширению.
- JSON с шапкой (сцены, время, XR, уровень качества), сводкой, бюджетами и списком проблем.
- CSV со списком проблем для Excel.
- Метод для batchmode: аудит указанной сцены и отчёт в файл, код выхода 0 / 1.

## Capabilities

### New Capabilities
- `scene-audit-export`: выгрузка отчёта аудита в JSON и CSV из окна и из командной строки.

### Modified Capabilities
- нет

## Impact

- Новый код: `SceneOptimizationAuditor/Editor/ReportExporter.cs`, кнопка в окне.
- Формат файлов отчёта — `docs/scene-optimization-auditor.md`.
- Зависит от `add-scene-audit-checks`.
