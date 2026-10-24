# Proposal

## Why

`AuditorWindow.cs` разросся до 1635 строк, `Strings.cs` — до 818, а правило SPEC — файл до ~600 строк. Следующий
этап (`add-viewpoints`) добавляет вкладку «Точки обзора»; без разреза она ляжет в тот же файл. Владелец
(24.10.2026): «перед ними этап — разрезать AuditorWindow.cs».

## What Changes

- `AuditorWindow` становится `partial class` по файлам: ядро окна и по файлу на вкладку («Проблемы», «Развёртка
  под свет», «Бюджеты», «Профайлер», «История»); `IssueGroup` — свой файл.
- `Strings` становится `partial class` по разделам; таблица текстов, ключи и методы те же.
- Код переносится как есть: поведение, вид окна, тексты, форматы файлов и командная строка не меняются.
- Версия 1.1.1: изменение без нового поведения — третья цифра; этапы точек обзора остаются 1.2.0 и 1.3.0.

## Capabilities

### New Capabilities
- нет

### Modified Capabilities
- нет: поведение не меняется (`skip_specs: true`).

## Impact

- Код: `SceneOptimizationAuditor/Editor/AuditorWindow*.cs`, `IssueGroup.cs`, `Strings*.cs` и их `.meta`.
- Тесты (`tests/AuditTests`) обращаются к членам окна (`PickForShot`, `ExpandForShot`) — `partial` их не меняет.
- Документация: раздел «Устройство» в `docs/scene-optimization-auditor.md`, колонка «Где» в `docs/ui-guideline.md`,
  HANDOFF.
