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
groups, the tab and the scroll, and say so with a notification. A run of the build scenes or of several scenes SHALL
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
- **WHEN** the user lowers a texture from 8192 px to 4096 px and re-checks its row with the Warning threshold at 2048 px
- **THEN** the row stays with the new size and is marked as re-checked

#### Scenario: Scene totals untouched
- **WHEN** the user re-checks every row of a group after hiding half of the meshes
- **THEN** the triangle budget row and the budget bars keep the values of the full run

#### Scenario: Picked rows
- **WHEN** three rows are picked and the user presses Re-check Picked (3)
- **THEN** only the objects of those three rows are checked again
