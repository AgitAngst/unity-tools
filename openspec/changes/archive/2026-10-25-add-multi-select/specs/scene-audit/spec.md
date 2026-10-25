# Spec Delta

## ADDED Requirements

### Requirement: Picking several rows
The window SHALL let the user pick several rows with checkboxes. On the Problems tab a row SHALL be pickable
when it has a fix or points to an object or asset; a group checkbox SHALL pick or release all pickable rows
of the group and "Select all" SHALL pick every shown pickable row. While rows are picked, a bar SHALL show
their number, how many of them a severity filter hides, "Clear selection", "Fix selected" for the fixable
ones through the confirmation window, and "Select in Unity" that selects their objects and assets so they can
be edited together in the Inspector. On the Lightmap UVs tab every row SHALL have a checkbox and the header a
checkbox for all shown rows; the bar SHALL offer "Turn on for selected" (models without UV1 and with the option
off), "Select in Project" (the model files) and "Select objects in scene" (every scene object that uses them).
A new audit run or scan SHALL clear the picks.

#### Scenario: Turn on for some models
- **WHEN** the user picks two of ten models without UV1 and presses "Turn on for selected" and confirms
- **THEN** only those two models get Generate Lightmap UVs on

#### Scenario: Edit together in the Inspector
- **WHEN** the user picks three problem rows (two scene objects and a texture) and presses "Select in Unity"
- **THEN** Unity selects the two GameObjects and the texture

#### Scenario: Settings row
- **WHEN** a row is a project setting without a fix
- **THEN** it has no checkbox
