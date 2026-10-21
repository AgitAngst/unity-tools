# Proposal

## Why

Часть найденных проблем исправляется одной правкой свойства или импорта, но многие «простые»
исправления ломают сцену (Static на движимых объектах, Overlay → World Space, 2D-звук → 3D). Нужны
исправления в два вида — безопасные пакетом и остальные по одному после просмотра — с честным
подтверждением. Это этап 3 замысла `SPEC.md` → «SceneOptimizationAuditor».

## What Changes

- Кнопка «Auto-Fix Safe Issues»: GPU Instancing у материалов и mipmaps у текстур.
- Кнопка «Fix» у строк, которые можно исправить после просмотра (Read/Write, тип Normal map, сжатие,
  Animator, SkinnedMesh, AudioSource, импорт аудио, Batching Static).
- Окно подтверждения со списком изменений и галочками: сцена (Undo) и ассеты (без Undo, переимпорт).
- После исправлений — повторный аудит.

## Capabilities

### New Capabilities
- `scene-audit-fixes`: исправление найденных проблем — пакетом для безопасных, по одной для остальных.

### Modified Capabilities
- нет

## Impact

- Новый код: `AutoFixer.cs`, `FixConfirmWindow.cs`; действия исправления в проверках; кнопки в окне.
- Меняет сцены (с Undo) и ассеты проекта (без Undo) — только после подтверждения.
- Зависит от `add-scene-audit-checks`.
