# Proposal

## Why

Владелец 29.10.2026: «что делать с Many shader variants? у меня шейдер с 75444 вариантами» и на предложение показать
виновников — «Да, сделай»; про простые материалы Standard — «нет возможности не только поменять, но и не показывает
конкретики»; про трилинейную — «Говорит что 371 текстуре надо включить, но никакой возможности включить нету». Записи
`MAT_SIMPLE_STANDARD` и `TEX_NOT_TRILINEAR` были одной строкой с числом, без списка и исправления; `SHADER_VARIANTS` не
говорил, что множит варианты и сколько их нужно на деле.

## What Changes

- `SHADER_VARIANTS`: сколько вариантов нужно материалам и какие строки `#pragma` множат больше всего; совет «можно не
  трогать», когда нужно мало (`SHADER_VARIANTS_FEW`).
- `MAT_SIMPLE_STANDARD`: запись на материал, подходящий шейдер (Mobile/Diffuse, Legacy Diffuse, Legacy Cutout Diffuse),
  Metallic и Smoothness, исправление с просмотром.
- `TEX_NOT_TRILINEAR`: запись на текстуру с текущей фильтрацией и безопасным исправлением.
- Этап плана: версия 2.9.0.

## Capabilities

### Modified Capabilities
- `scene-audit`: проверки текстур, материалов и шейдеров.

## Impact

- Код: `ShaderSource.cs` (строки ключевых слов), `Checks/ShaderChecks.cs`, `Checks/MaterialChecks.cs`,
  `Checks/TextureChecks.cs`, `Recheck.cs` (две проверки уходят из «по всей сцене»), `AuditScope.cs` (одна запись
  вариантов на шейдер у нескольких сцен), `AuditorWindow.Help.cs` (общая подсказка группы с записями разного вида),
  `Strings.Shaders.cs`, `Strings.Checks.cs`, `Strings.Ui.cs`.
- Тесты: новый шаг `specific-advice`, `shaders`, `base`, `fixes`, `recheck`; снимки `advice-*`.
