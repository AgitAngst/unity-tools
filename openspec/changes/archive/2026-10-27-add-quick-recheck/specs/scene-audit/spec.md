# Spec Delta

## ADDED Requirements

### Requirement: Run bar at the bottom
The window SHALL have a bar at the bottom on every tab with the scope button on the left, a short line about the last
run next to it (time and duration; after a re-check also how many objects were re-checked and when) and the Run Audit /
Run Again button with the Play icon on the right. The top toolbar SHALL no longer hold the run and scope buttons.

#### Scenario: Run from the bottom
- **WHEN** the user presses Run Audit in the bottom bar with the scope Open Scenes
- **THEN** the open scenes are checked and the bar shows the time and the duration of the run

#### Scenario: Narrow window
- **WHEN** the window is 560 px wide
- **THEN** the bottom bar keeps the scope button and the run button on one row and the top toolbar fits on one row

### Requirement: Kept result
The last run SHALL survive the window being opened again or re-created by the editor. After a script reload the window
SHALL check the same scope again by itself once the editor is out of Play Mode and not compiling, keeping the open
groups, the tab and the scroll, and say so with a notification, without writing a history point. A run of the build scenes or of several scenes SHALL
not start by itself; the Problems tab SHALL instead show a bar saying the script reload dropped that check, with a
button to run it again.

#### Scenario: Window opened again
- **WHEN** the user closes the window after a run and opens it again
- **THEN** the same list is shown without a new run

#### Scenario: Script reload
- **WHEN** scripts are recompiled after a run of the open scenes with two groups open
- **THEN** the open scenes are checked again by themselves, the two groups are still open and a notification says the
  list was refreshed after the script reload

#### Scenario: Build scenes after a script reload
- **WHEN** scripts are recompiled after a run of six build scenes
- **THEN** no scene is opened by itself and a bar above the list offers to run the six scenes again

### Requirement: Quick re-check
A row of the Problems list, a copies row, a group and the selection bar SHALL offer to re-check only their objects (with
their children and the assets they use), assets or project settings without a full run. The re-check SHALL replace the
rows of those objects and assets from the per-object checks with new ones — fixed rows go, changed rows are updated, new
rows appear — and update the verdict, the severity cards and the tab count. Budget bars, scene totals and checks over the
whole scene SHALL keep the values of the full run; their rows SHALL show the re-check action disabled with a tooltip. A
re-check SHALL not record a history point. The result SHALL be told by a notification, and the remaining rows of
re-checked objects SHALL be marked with the time of the re-check until the next full run.

#### Scenario: Fixed by hand
- **WHEN** a mesh row says the mesh has no LOD, the user adds a LOD Group to the object and re-checks the row
- **THEN** the row goes, the group count drops by one and the notification says one row went

#### Scenario: Still there
- **WHEN** a mesh row says 12000 triangles without LOD, the user swaps the mesh for one of 30000 triangles and re-checks
  the row
- **THEN** the row stays in its place with the new count and is marked as re-checked

#### Scenario: Scene totals untouched
- **WHEN** the user re-checks every row of a group after hiding half of the meshes
- **THEN** the triangle budget row and the budget bars keep the values of the full run

#### Scenario: Picked rows
- **WHEN** three rows are picked and the user presses Re-check Picked (3)
- **THEN** only the objects of those three rows are checked again

## MODIFIED Requirements

### Requirement: Readable window in both skins and at minimum width
No tab label SHALL be cut, down to the minimum window width of 560: tabs SHALL be of equal width while the longest
label fits and as wide as their labels otherwise. When the toolbar buttons do not fit on one row, measured by their
labels, the help, the version and the language switch SHALL move to a second toolbar row; at 560 the toolbar without
Run and scope SHALL fit on one row.
Text coloured by severity SHALL use a shade per editor skin with a contrast of at least 4.5:1 against the background
(dark skin: Critical `#FF8A8A`, Warning `#F2B833`, good `#5CC968`; light skin: `#9E1C1C`, `#6B4700`, `#185A22`);
stripes, dots, backgrounds and icons SHALL keep the severity colours. A table row picked by a click SHALL have the
editor selection colour as its background.

#### Scenario: Narrow window
- **WHEN** the window is 560 wide and has six tabs
- **THEN** every tab label is shown in full

#### Scenario: Light skin
- **WHEN** the editor uses the light skin and a value is over the Critical threshold
- **THEN** the value is shown in `#9E1C1C`, not in the Critical stripe colour

### Requirement: Audit scope
The auditor window SHALL offer a scope on the left of the run bar, with Run on its right: the open scenes (as before),
the selection, the build scenes and a folder, remembered between sessions. The selection scope SHALL list only the
problems of the selected scene objects (with their children) and of the assets they use, without project settings and
whole-scene problems. The build scenes scope SHALL offer to save modified scenes, check every enabled scene of the build
settings that is not open on its own, open the scenes back as they were and check the open build scenes last; problems
of closed scenes SHALL offer "Open Scene" instead of a fix; the same asset or project setting problem from several
scenes SHALL be one row with the users added up; a table SHALL list every scene with its counts and budgets. The folder
scope SHALL check the prefabs, models, materials, textures and audio clips of a folder under `Assets/` in a preview
scene and list the problems of these assets and of the objects inside their prefabs and models only.

#### Scenario: A part of the level
- **WHEN** the user selects one building and runs with the selection scope
- **THEN** the list holds the problems of the building's objects and of the assets they use, and no project settings

#### Scenario: Whole game
- **WHEN** the build settings list three scenes, one of them open, and the user runs with the build scenes scope
- **THEN** every scene is checked, the open scenes are the same afterwards, the problems of the open scene can be fixed
  and those of the closed ones offer "Open Scene"

#### Scenario: New assets
- **WHEN** a folder holds a prefab, a model, a loose material, a texture and an audio clip
- **THEN** their problems are listed with the prefab objects as "<file> › <path>", and nothing outside the folder
