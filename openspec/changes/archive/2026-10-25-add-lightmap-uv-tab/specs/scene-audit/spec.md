# Spec Delta

## ADDED Requirements

### Requirement: Lightmap UV report for every model
The window SHALL have a "Lightmap UVs" tab that, on request, scans every MeshFilter and
SkinnedMeshRenderer of the loaded scenes, inactive objects included, and lists each model file (or
mesh asset without a model) once: whether Generate Lightmap UVs is on, whether every used mesh has UV1,
the share of UV1 outside 0–1, the overlapping share of UV1, the number of objects that use it and how
many of them are baked into lightmaps. The list SHALL be filterable to problem rows only (no UV1, UV1
outside 0–1 or overlapping). A row for a model without UV1 and with the option off SHALL offer to turn
the option on; a button SHALL offer it for all such models at once; both SHALL go through the fix
confirmation. The table SHALL be exportable as CSV. All texts SHALL be in Russian and English.

#### Scenario: Model not marked for lightmaps
- **WHEN** a model without UV1 is used only by objects without Contribute GI
- **THEN** the tab still lists it with the option off and no UV1, and offers to turn the option on

#### Scenario: Turn on for all
- **WHEN** the user presses "Turn on for all without UV1" and confirms
- **THEN** every listed model without UV1 gets Generate Lightmap UVs on and, after the rescan, has UV1

#### Scenario: Model with the option on
- **WHEN** a model has Generate Lightmap UVs on
- **THEN** its row shows the option on and UV1 present, and the problem filter hides it
