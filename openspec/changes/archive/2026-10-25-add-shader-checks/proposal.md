# Proposal

## Why

Шейдер решает, сколько раз и как рисуется объект: GrabPass копирует экран на каждый объект, лишние проходы рисуют объект
несколько раз, а вершинный шейдер без stereo-макросов при Single Pass Instanced виден одним глазом. Аудитор шейдеры
сейчас не проверяет. Владелец, 25.10.2026: «делай частицы и шейдер и reflection probe».

## What Changes

- Новая категория «Шейдеры» (`Shaders`) после «Материалов»; запись — на шейдер, с числом объектов.
- Шейдеры материалов включённых рендереров загруженных сцен. Исходник читается у `.shader` в `Assets/` (и их `#include`
  из `Assets/`); у встроенных и пакетных — только то, что даёт API.
- Проверки: `SHADER_ERROR` (C), `SHADER_GRABPASS` (W), `SHADER_MULTIPASS` (W), `SHADER_NO_STEREO` (C, только при
  Single Pass Instanced), `SHADER_VARIANTS` (I), `SHADER_ALPHA_TEST_MSAA` (I). Исправлений нет — правится исходник.
- Этап плана: версия 1.7.0.

## Capabilities

### Modified Capabilities
- `scene-audit`: проверки шейдеров.

## Impact

- Код: `Checks/ShaderChecks.cs`, `ShaderSource.cs` (разбор исходника: проходы первого SubShader, LightMode, GrabPass,
  AlphaToMask, программы и `#include`), `Strings.Shaders.cs` (новые); `AuditIssue.cs`, `SceneAuditor.cs`,
  `VRBudgets.cs`, `Strings.Ui.cs`, `Strings.cs`.
- `ShaderUtil.GetVariantCount` в 2019.4 внутренний — через reflection; нет метода — проверка молчит.
