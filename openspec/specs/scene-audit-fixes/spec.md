# scene-audit-fixes Specification

## Purpose
Исправление проблем, найденных аудитом сцены: безопасные — пакетом, остальные — по одной после
просмотра, всегда с подтверждением и с понятной границей между откатываемыми и неоткатываемыми правками.

## Requirements

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

### Requirement: Copies together
With "Copies together" on (the default), an open group of the Problems list SHALL show the issues of copies of one
prefab or model object (the same corresponding source object) as one row with the source, its path in the prefab and
the number of copies; the row SHALL select all copies, expand to single rows and pick all its issues with its box.

#### Scenario: Animator in a prefab
- **WHEN** 30 copies of a prefab each report `ANIM_ALWAYS_ANIMATE`
- **THEN** the group shows one row "copies: 30" instead of 30 rows

### Requirement: Fix in the prefab
A copies row with scene fixes SHALL offer "Fix in Prefab" when it holds every issue of its check on copies of the source
in the run, none of them acceptable, and the source is an editable prefab in Assets/ (not a model, not immutable): the fix SHALL run on one copy, the properties it changed SHALL be applied to the prefab asset with
`ApplyPropertyOverride`, and copies that override any of those properties SHALL be fixed on themselves. The
confirmation window SHALL list it under assets with the prefab path and the number of copies. "Fix All" of a group and
"Fix Selected" SHALL use it for such rows while "Copies together" is on.

#### Scenario: One fix instead of thirty overrides
- **WHEN** the user fixes the 30 copies with "Fix in Prefab"
- **THEN** the prefab asset has Cull Update Transforms, the copies without their own value have no override of the culling
  mode, and a copy that had its own value is fixed on itself

#### Scenario: Part of the copies
- **WHEN** the user picks 5 of the 30 copies and fixes the selection
- **THEN** the 5 copies are fixed one by one and the prefab is not changed
