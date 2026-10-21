# Spec Delta

## Purpose

Исправление проблем, найденных аудитом сцены: безопасные — пакетом, остальные — по одной после
просмотра, всегда с подтверждением и с понятной границей между откатываемыми и неоткатываемыми правками.

## ADDED Requirements

### Requirement: Safe and review fixes
Only enabling GPU Instancing on a material and enabling mipmaps on a texture SHALL be offered as safe
batch fixes. Other fixable issues SHALL be fixed one at a time from their row. Screen Space Overlay
canvases and project settings SHALL NOT be fixed automatically; their rows SHALL only explain the fix
and open the related settings.

#### Scenario: Batch fix
- **WHEN** the user presses "Auto-Fix Safe Issues"
- **THEN** the confirmation lists only instancing and mipmap changes

#### Scenario: Overlay canvas
- **WHEN** a Screen Space Overlay canvas issue is shown
- **THEN** the row has no Fix button

### Requirement: Confirmation before changes
Before any change the tool SHALL show a confirmation window listing every change with a checkbox,
grouped into scene changes (undoable) and asset changes (project-wide, not undoable by Undo, causes
reimport), and SHALL mark objects that are part of a prefab instance as becoming overrides. Nothing
SHALL change if the user cancels.

#### Scenario: Cancel
- **WHEN** the user closes the confirmation with Cancel
- **THEN** no scene or asset is modified

### Requirement: Applying changes
Scene changes SHALL be recorded in one Undo group and SHALL mark their scenes dirty. Asset changes SHALL
skip read-only assets and assets outside `Assets/`. Each applied change SHALL be logged to the console
in English. After applying, the audit SHALL run again.

#### Scenario: Undo
- **WHEN** the user applies scene fixes and then presses Undo once
- **THEN** all scene changes of that apply are reverted

#### Scenario: Package asset
- **WHEN** a fix targets a material inside `Packages/`
- **THEN** it is skipped and reported as read-only
