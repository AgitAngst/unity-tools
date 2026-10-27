# Proposal

## Why

Владелец 27.10.2026: «Делай этапом: новые проверки Linear, Soft Shadows, Real-time Reflection Probes в Quality, Soft Particles, VSync и targetFrameRate; пороги MSAA, дальности теней и Skin Weights — по советам для RTX 2070». Часть советов разбора
настроек под Vive Pro 2 на RTX 2070 аудитор не проверял, а пороги MSAA, дальности теней и Skin Weights были строже
советов.

## What Changes

- Новые проверки: Color Space Gamma, мягкие тени realtime-источников, Realtime Reflection Probes и Soft Particles уровня
  качества, VSync Count, скрипты с `Application.targetFrameRate` и `QualitySettings.vSyncCount`.
- Пороги: MSAA — предупреждение только без MSAA, 8x — сведения; дальность теней — больше 40 м; Skin Weights — больше
  Two Bones.
- Этап плана: версия 2.2.0.

## Capabilities

### Modified Capabilities
- `scene-audit`: проверки настроек качества и частоты кадров.

## Impact

- Код: `QualitySnapshot.cs` (Soft Particles, Realtime Reflection Probes, VSync Count), `Checks/VRChecks.cs`,
  `Checks/LightingChecks.cs`, `Checks/ReflectionChecks.cs`, `Checks/ParticleChecks.cs`, `Checks/MeshChecks.cs`,
  `FrameRateScripts.cs` (новый: разбор скриптов), `VRBudgets.cs`, тексты `Strings.Checks.cs`, `Strings.Particles.cs`,
  `Strings.Reflections.cs`, карточка `VR_MSAA`.
- Тесты: `AuditTestQuality.cs` (шаги `quality-checks`, `frame-rate-script-make` и `frame-rate-script-check`),
  `AuditTestSettings.cs`, `AuditTests.cs` (варианты `quality` и `project`), `scripts/run-audit-tests.ps1`.
