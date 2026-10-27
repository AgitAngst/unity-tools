# Spec Delta

## MODIFIED Requirements

### Requirement: Picking several rows
The window SHALL let the user pick several rows with checkboxes. On the Problems tab every row SHALL be pickable
(since 2.5.0 project settings and problems of a whole scene without a fix too); a group checkbox SHALL pick or release
all shown rows of the group and "Select all" SHALL pick every shown row. While rows are picked, a bar SHALL show
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
- **THEN** it has a checkbox, and picking it adds nothing to "Fix selected" and "Select in Unity"

### Requirement: Acceptable problems
The user SHALL be able to mark picked problems as acceptable and remove the mark. A mark SHALL be stored per
project in `ProjectSettings/SceneOptimizationAuditorAccepted.json`, keyed by the check and the GlobalObjectId of
the object (a component counts as its GameObject) or asset, so it survives renames, moves and new runs. A problem
without an object SHALL be keyed by the guid of its scene when the run checked one saved scene (one open scene, or
scenes one by one), and by the check alone (`*`, for the whole project) for project settings, for several open scenes
checked together and for a scene never saved; a mark `*` SHALL hold for the problems of every scene, and releasing a
scene's row SHALL remove it. Acceptable problems SHALL NOT be counted in the verdict, the severity cards, the tab
count, the safe batch fix or the report summary, SHALL be hidden by default and shown, tagged as acceptable, with an
"Acceptable (N)" toggle. The file SHALL be removed when nothing is marked.

#### Scenario: Large houses
- **WHEN** the user marks the extreme-scale problem of a house as acceptable and runs the audit again
- **THEN** the problem is tagged acceptable, hidden by default and not counted, and other objects of the same check still count

#### Scenario: Moved object
- **WHEN** a marked object is moved in the scene
- **THEN** its problem stays acceptable

#### Scenario: A scene-wide row in one scene of two
- **WHEN** two empty scenes are checked one by one and the row "no VR camera" of the first is marked acceptable
- **THEN** after a new run it is acceptable in the first scene and counts in the second

#### Scenario: An old project-wide mark
- **WHEN** the accepted list holds a mark `*` of that check
- **THEN** the row is acceptable in both scenes, and releasing it in one scene removes the mark

#### Scenario: Open scenes
- **WHEN** one open scene is checked
- **THEN** its scene-wide rows get the same key as when the scene is checked alone; with two open scenes together or a scene never saved the key is `*`
