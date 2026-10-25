# Proposal

## Why

Список проблем говорит, что не так, но не говорит, что даст исправление: сотня записей «Сведения» и одна текстура 8К
выглядят одинаково важными. Владелец, 25.10.2026: «делай оценку выйгрыша» — пункт анализа «оценка выигрыша у каждой
проблемы («−34 МБ», «−120 draw calls») и сортировка «что даст больше всего»».

## What Changes

- У записей проверок с измеримым результатом — оценка выигрыша в МБ, draw calls или треугольниках (грубые — «≈»).
- «Проблемы»: выигрыш у записи, строки копий и группы; переключатель «По выигрышу» — группы по доле бюджета; строка
  «Если исправить всё: …» под полосами бюджета.
- Отчёт: `gainMB`, `gainDrawCalls`, `gainTris`, `gainApprox` у записи, суммы в `summary`, столбцы CSV.
- Этап плана: версия 2.0.0 (после 1.9.x).

## Capabilities

### Modified Capabilities
- `scene-audit`: оценка выигрыша и сортировка по нему.
- `scene-audit-export`: поля выигрыша в отчёте.

## Impact

- Код: `AuditIssue.cs` (поля выигрыша), `FixGain.cs` (новый: память меша, текстуры при другом размере и формате, доля
  бюджета, суммы), проверки (`TextureChecks`, `TextureAssetChecks`, `MeshChecks`, `MeshAssetChecks`, `AudioChecks`,
  `DrawCallChecks`, `LightingChecks`, `ShaderChecks`, `ReflectionChecks`), `TextureMemory.cs`, окно
  (`AuditorWindow.Problems.cs`, `IssueGroup.cs`), `ReportExporter.cs`, `Strings.Gain.cs` (новый).
