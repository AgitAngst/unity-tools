# Proposal

## Why

Владелец 27.10.2026: «Ещё с учетом всего проверку травы на террейне добавь, если нет». Террейн и его трава не
проверялись, а густая трава на большой дистанции и Grass Billboard в VR — частая причина просадок и неприятной картинки.

## What Changes

- Категория «Террейн»: трава и детали вокруг игрока (оценка числа и треугольников), Grass Billboard, Detail Distance больше
  60 м, Draw Instanced выключен, Pixel Error меньше 5; исправления по просмотру — Grass вместо Billboard, Detail Distance 60 м,
  Draw Instanced.
- Этап плана: версия 2.4.0.

## Capabilities

### Modified Capabilities
- `scene-audit`: проверки террейна и травы.

## Impact

- Код: `AuditIssue.cs` (категория `Terrain`), `Checks/TerrainChecks.cs` (новый), `SceneAuditor.cs` (шаг), `VRBudgets.cs`
  (пределы — предложение сессии), `Strings.Terrain.cs` (новый: тексты и название категории), `Strings.cs`, `AutoFixer.cs`
  (данные террейна сохраняются после исправления).
- Тесты: `AuditTestTerrain.cs`, шаг `terrain` в `scripts/run-audit-tests.ps1`.
