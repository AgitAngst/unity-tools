# Proposal

## Why

Сцены под PC VR (HTC Vive Pro, 90 Гц) теряют кадры из-за типовых ошибок — несжатых текстур, лишних
источников теней, Canvas поверх экрана, неверного режима stereo. Нужна редакторская утилита для
Unity 2019.4, которая находит их за один прогон и объясняет, как исправить. Это этап 1 замысла
`SPEC.md` → «SceneOptimizationAuditor».

## What Changes

- Новая утилита `SceneOptimizationAuditor/` (Editor-сборка): окно `Tools/VR Scene Optimization Audit`.
- Сбор данных по всем загруженным сценам: объекты, ассеты, уровень качества Standalone, XR-система
  (legacy VR и XR Plug-in Management).
- Статические проверки девяти категорий с уровнями Critical / Warning / Info и подсказками.
- Обзор бюджетов сцены (треугольники, оценка draw calls, оценка памяти текстур) с настраиваемыми
  порогами в `ProjectSettings/SceneOptimizationAuditor.json`.
- Интерфейс на русском и английском.

## Capabilities

### New Capabilities
- `scene-audit`: прогон аудита загруженных сцен под PC VR, проверки, бюджеты, окно отчёта.

### Modified Capabilities
- нет

## Impact

- Новый код: `SceneOptimizationAuditor/Editor/` (asmdef только Editor); страницы `docs/`.
- Зависимостей нет: пакеты (PPv2, SteamVR, XR Management) — только через reflection.
- Проверочный проект `unity-tools-scratch\TestProject2019` — вне репозитория.
