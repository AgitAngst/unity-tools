# scene-audit-viewpoints Specification

## Purpose
Оценка того, что видит игрок из мест, где он бывает: точки обзора уровня, треугольники, draw calls и прозрачное
в кадре из каждой точки и худшие места уровня — вместо одной суммы по всей сцене.

## Requirements

### Requirement: Viewpoint placement
The tool SHALL place viewpoints of the loaded scenes from the sources the user ticks: a grid over the baked NavMesh
(step 4 m by default, 1 to 20 m, eye height 1.7 m above the NavMesh), SteamVR teleport points and teleport areas
when the SteamVR types exist (found by type name), enabled scene cameras other than the VR camera, and, when there
is neither a NavMesh nor teleports, a downward ray grid over the renderer bounds that keeps surfaces tilted up to
45°. The user SHALL be able to add a manual viewpoint at the Scene view camera position and direction, move a
viewpoint with a handle in the Scene view and remove viewpoints. Placing again SHALL replace only automatic
viewpoints and SHALL clear every estimate; a moved automatic viewpoint SHALL become manual. There SHALL be at most 500
automatic viewpoints (manual ones do not count); when the grid would give more, the step SHALL grow and the summary
SHALL say so. Viewpoints, placement settings and the last
estimate SHALL be stored in `SceneAuditPoints/<scene>-<hash>.json` next to `Assets/`, keyed by the set of loaded
scenes like the audit history. Placing, moving and estimating SHALL NOT change the scenes.

#### Scenario: NavMesh grid
- **WHEN** a scene has a baked NavMesh floor of 20 × 20 m and the user places viewpoints from the NavMesh with a step of 4 m
- **THEN** about 25 viewpoints appear 1.7 m above the floor and the scene is not marked dirty

#### Scenario: Manual viewpoint survives placing again
- **WHEN** the user adds a viewpoint from the Scene view camera and then places NavMesh viewpoints again
- **THEN** the manual viewpoint stays and the automatic ones are replaced

#### Scenario: No NavMesh and no teleports
- **WHEN** the scene has no NavMesh and no SteamVR teleports
- **THEN** viewpoints are placed by downward rays on upward-facing surfaces

### Requirement: Viewpoint estimate
For each viewpoint the tool SHALL evaluate 8 directions 45° apart at zero pitch (plus the own direction of manual
and camera viewpoints) with one view frustum for both eyes (110° vertical field of view, aspect 0.9, near plane
0.05, far plane of the scene VR camera or 1000) and, when a VR camera exists, its culling mask and per-layer cull
distances. A renderer SHALL count when it is active and its bounds intersect the frustum. A LOD group SHALL show the
level Unity would pick from the group size and scale, the distance to its reference point, the field of view,
`QualitySettings.lodBias` and `maximumLODLevel`, and nothing below its last transition. Per direction the tool SHALL
compute triangles of the visible meshes, draw calls (material slots, one per particle, line and sprite renderer,
extra pixel light passes for Realtime and Mixed point and spot lights whose range touches the renderer, up to the
Pixel Light Count, shadow passes of shadow casters within the shadow distance for directional lights and within the
range for point and spot lights, doubled with VR in Multi Pass or Single Pass stereo) and the share of the
screen covered by the bounds of transparent renderers (render queue 2501 and above) and particles, in percent. An
optional "Account for walls" mode, off by default, SHALL hide a renderer when rays from the eye to its bounds centre
and four top corners hit colliders of other objects first. The result of a viewpoint SHALL be its worst direction by draw
calls, then by triangles; all directions SHALL be kept. Terrain SHALL NOT be counted. The estimate SHALL run from its
own button with a cancelable progress bar and SHALL NOT be part of the audit run.

#### Scenario: LOD level by distance
- **WHEN** a LOD group with transitions 0.5 / 0.2 / 0.05 stands so that its screen height is 0.3 of the view
- **THEN** only the LOD1 renderers of that group are counted

#### Scenario: Object behind the viewer
- **WHEN** an object lies outside the view frustum of every direction of a viewpoint
- **THEN** its triangles and draw calls are not counted for that viewpoint

#### Scenario: Shadow passes
- **WHEN** a visible shadow caster with one material is lit by a Realtime directional light with 2 cascades and the stereo mode is Single Pass Instanced
- **THEN** it adds 1 draw call for itself and 2 for shadows

#### Scenario: Cancel
- **WHEN** the user cancels the progress bar during the estimate
- **THEN** the previous estimate stays and nothing is written

### Requirement: Viewpoint budgets
The tool SHALL compare the worst direction of each viewpoint with the draw call and triangle thresholds of the
budgets and with a new "Transparent in frame, % of screen" threshold (Warning above 100, Critical above 200 by
default), editable on the Budgets tab and stored with the other budgets.

#### Scenario: Over budget
- **WHEN** a viewpoint sees 1 450 draw calls and the draw call Warning threshold is 1 000
- **THEN** the viewpoint is over budget and its draw call value is shown in the Warning colour

### Requirement: Viewpoints tab
The window SHALL have a Viewpoints tab second after Problems with a short explanation, a toolbar ("Estimate", "Place
viewpoints" opening a panel under the button with the source checkboxes and what each found, the grid step, the eye
height, "Cancel" and "Place (≈ N)", "Add from here", "Save CSV", and on the right "Account for walls" and "Only over
budget"; on a narrow window the toolbar SHALL wrap to two rows), an empty state before any viewpoint exists (what the
tool does, that the scene is not changed, one large "Place viewpoints…" button and what was found), a summary line
(number of viewpoints, how many are over budget, the worst viewpoint and its value) and a table, 20 rows at first with
"Show 20 more", sorted worst first
with the columns viewpoint, triangles, draw calls, transparent and direction, values coloured by the thresholds and a
side stripe on rows over budget. Clicking a row SHALL move the Scene view camera to the viewpoint looking in its
worst direction without changing Scene view settings. The selected viewpoint SHALL show its directions (azimuth,
triangles, draw calls, transparent; clicking one looks there) and "What is heavy here": the top 10 by triangles and
by draw calls in the shown direction (the worst one unless the user picked another; without shadow passes) grouped by
source prefab or model with the number of copies and the marks "far
but LOD0", "no LOD group", "small but casts shadows" and "large transparent"; clicking one selects its objects. Rows
SHALL have checkboxes with "Estimate selected" and "Remove selected". The tab SHALL list what the estimate does not
count: batching and instancing, Unity occlusion culling, objects spawned at runtime, shader and GPU cost.

#### Scenario: Jump to the worst place
- **WHEN** the user clicks the first row of the table
- **THEN** the Scene view camera stands at that viewpoint and looks in its worst direction

#### Scenario: Several sources at once
- **WHEN** the user opens "Place viewpoints", ticks NavMesh and teleports and changes the step to 6 m
- **THEN** the panel stays open until "Place (≈ N)" or "Cancel" and N follows the ticked sources and the step

#### Scenario: Heavy prefab
- **WHEN** forty copies of one prefab without a LOD group are visible in the worst direction
- **THEN** "What is heavy here" shows the prefab once with 40 copies and the mark "no LOD group"

### Requirement: Viewpoints in the Scene view
While the Viewpoints tab is open the Scene view SHALL show every viewpoint as a sphere coloured green, yellow or red
by its worst value against the thresholds, grey when not estimated, and for the selected viewpoint an arrow of the
shown direction (the worst one unless the user picked another), the outline of its view frustum and a move handle.

#### Scenario: Not estimated yet
- **WHEN** viewpoints were placed and not estimated
- **THEN** their spheres are grey

### Requirement: Viewpoints CSV
"Save CSV" SHALL write `Point, Kind, X, Y, Z, Yaw, Tris, DrawCalls, TransparentPercent, OverBudget`, one row per
viewpoint, escaped by RFC 4180 and written as UTF-8 with BOM.

#### Scenario: Export the table
- **WHEN** 12 viewpoints are estimated and the user saves CSV
- **THEN** the file has a header and 12 rows with the worst direction of each viewpoint
