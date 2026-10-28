# Spec Delta

## MODIFIED Requirements

### Requirement: Scene budgets
The tool SHALL estimate triangles and draw calls per frame from the viewpoints of the open scenes — every run SHALL
estimate them in memory as "Estimate" on the Viewpoints tab does (headset field of view, LOD, shadow passes, doubled
unless Single Pass Instanced, without batching) and SHALL NOT change the viewpoint file — and SHALL compare the worst
viewpoint, one for triangles and one for draw calls, with the Warning and Critical thresholds, which are per frame. The
whole scene (triangles of LOD0 and draw calls of every material slot plus shadow passes, without culling or batching)
SHALL be shown for reference only, without a threshold colour. Without viewpoints the frame budget SHALL be shown as
not estimated with a way to place viewpoints, and an Info row SHALL say so with the whole-scene figures. Texture memory
SHALL be estimated for the whole scene and compared with its thresholds. All values SHALL be labelled as estimates.
Threshold overrides SHALL be stored per project in `ProjectSettings/SceneOptimizationAuditor.json` and the file SHALL be
written only when the user saves budgets.

#### Scenario: Budget exceeded
- **WHEN** the worst viewpoint sees more triangles than the Warning threshold
- **THEN** the triangle bar turns yellow and a Meshes Warning names the viewpoint, the estimate and the threshold

#### Scenario: Whole scene over, every frame within
- **WHEN** the whole scene has 19,000 draw calls without culling and the worst viewpoint 800 with the threshold at 1,000
- **THEN** the draw call bar is green at 800, the line under it gives 19,000 for the whole scene without a colour and
  there is no draw call budget problem

#### Scenario: No viewpoints
- **WHEN** the open scenes have no viewpoints
- **THEN** the triangle and draw call bars say there are no viewpoints and offer to place them, the list has an Info row
  with the whole-scene figures and no budget problem for triangles or draw calls

### Requirement: History tab
The window SHALL have a History tab with charts of problem counts over time, of texture memory in percent of its
warning limit and of the whole-scene triangles and draw calls without a threshold (they are no frame), a table of points
with changes against the previous point, a comparison of two picked points (changes of totals and budgets, checks whose
count changed, lists of fixed and new problems), editing the note of a point, removing a point and choosing another
recorded scene.

#### Scenario: Compare two points
- **WHEN** the user picks the first and the last point
- **THEN** the tab shows how many Critical and Warning problems are gone, budget changes, and the fixed and new problems by check and location

