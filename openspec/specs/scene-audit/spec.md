# scene-audit Specification

## Purpose
Аудит загруженных сцен Unity 2019.4 под PC VR (шлем на SteamVR на выбор, по умолчанию HTC Vive Pro 2 Wireless, 90 Гц): найти проблемы
производительности и VR-настройки, показать их по уровням с подсказками и сравнить сцену с бюджетами.

## Requirements

### Requirement: Audit run over loaded scenes
The tool SHALL scan every loaded scene, including inactive objects, and SHALL collect the assets
referenced by those scenes. Budget sums and "active" counters SHALL include only active and enabled
objects. The run SHALL show a cancelable progress bar and SHALL NOT modify scenes, assets or settings.

#### Scenario: Two scenes loaded
- **WHEN** two scenes are loaded and the user runs the audit
- **THEN** issues from both scenes are listed, each with its scene name

#### Scenario: Run canceled
- **WHEN** the user cancels the progress bar
- **THEN** the run stops, the progress bar is cleared and no partial result replaces the previous one

### Requirement: Issue model
Each issue SHALL have a stable check id, a category, a severity (Critical, Warning or Info), a
message and a suggestion, and SHALL point to a scene object, an asset or a project setting. An asset
problem SHALL produce one issue per asset with the number of its users.

#### Scenario: Texture used by many objects
- **WHEN** one uncompressed texture is used by 50 renderers
- **THEN** a single issue is shown for that texture, mentioning 50 users

### Requirement: XR detection
The tool SHALL detect legacy built-in VR and XR Plug-in Management (without depending on the package)
for Standalone, SHALL report the loader or SDK and the stereo rendering mode in the header, and SHALL
raise an issue when VR is off, when OpenVR is absent, or when both systems are enabled.

#### Scenario: Legacy VR with OpenVR, multi-pass
- **WHEN** legacy VR is enabled for Standalone with OpenVR and stereo mode is multi-pass
- **THEN** the header shows "Legacy VR (OpenVR)" and a Warning recommends Single Pass Instanced

#### Scenario: VR not enabled
- **WHEN** neither legacy VR nor an XR loader is enabled for Standalone
- **THEN** a Critical issue reports that VR is not enabled

### Requirement: Static checks by category
The tool SHALL run the checks listed in `SPEC.md` (stage 1) for Draw Calls, Meshes, Textures,
Lighting, Physics, Animators, Audio, VR-Specific and General, with the severities and thresholds given
there. Checks that depend on a missing package or plugin SHALL be skipped without errors.

#### Scenario: Screen Space Overlay canvas
- **WHEN** a root Canvas uses Screen Space Overlay
- **THEN** a Critical VR-Specific issue states it is not visible in the headset

#### Scenario: Non-convex collider on a dynamic body
- **WHEN** a non-convex MeshCollider sits under a non-kinematic Rigidbody
- **THEN** a Critical Physics issue is reported

#### Scenario: Post Processing v2 not installed
- **WHEN** the project has no Post Processing v2 types
- **THEN** the post-processing check is skipped and the audit finishes without errors

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

### Requirement: Report window
The window SHALL open from `Tools/VR Scene Optimization Audit` and SHALL have three tabs: Problems,
Budgets and Profiler. Before the first run the Problems tab SHALL explain what the audit checks and
offer one large Run button. After a run it SHALL show a one-line verdict (critical first, then warnings),
three severity cards that toggle the severity filters, the XR state, quality level and scenes, compact
budget bars, a search field and the list. The list SHALL group issues of the same check and severity
into one row with a plain title, the number of objects, a "fix all" action when fixes exist and, when
expanded, how to fix it and the affected objects. Groups SHALL be ordered by severity; the list SHALL
be switchable to grouping by category. Info issues SHALL be hidden by default and the filter state
SHALL persist. Clicking an object SHALL select and ping it or its asset, or open the related Project
Settings page. All window texts SHALL be available in Russian and English; console output SHALL be in
English.

#### Scenario: Verdict
- **WHEN** the run finds critical issues
- **THEN** the verdict names their number and says to fix them first, and critical groups come first

#### Scenario: Grouped list
- **WHEN** fifty objects use one texture without mipmaps and three meshes have no LOD
- **THEN** the list shows one group for the texture and one group of three meshes, each with its count

#### Scenario: Info hidden by default
- **WHEN** the window opens for the first time
- **THEN** the Info card is off, its count is still shown, and clicking it shows Info groups

#### Scenario: Language switch
- **WHEN** the user switches the window language from EN to RU after a run
- **THEN** all labels and issue texts are shown in Russian without running the audit again

#### Scenario: Asset issue click
- **WHEN** the user clicks an issue that points to a texture asset
- **THEN** the texture is selected and pinged in the Project window

### Requirement: Help in the window
The window SHALL have a Help button in the toolbar whose menu offers "Getting started", help on the open tab, the user
guide and the check reference (pages of the repository opened in the browser). The Problems tab without a result SHALL
show a getting-started screen: the target GPU with a link to the budgets, the scope menu, the run button and task cards
that each take the first step of the task. A group of a check that has help SHALL offer "More about the check": a card
with what is checked, why it matters in VR, how to fix it by hand, the gain (only for checks with a gain estimate) and
when it may stay, with a button that selects the group's objects. Help texts SHALL exist in English and Russian for at
least 30 common checks.

#### Scenario: Getting started
- **WHEN** the Problems tab has no result
- **THEN** it shows the three steps and the task cards; "Before a build" sets the scope to the build scenes and runs the
  audit, and is unavailable when Build Settings has no enabled scenes

#### Scenario: Getting started over a result
- **WHEN** the user picks "Getting started" in the Help menu while a result is shown
- **THEN** the Problems tab shows the getting-started screen with "Back to the Result", and a new run hides it

#### Scenario: More about a check
- **WHEN** a group of "Texture too large" is open and the user clicks "More about the check"
- **THEN** a card shows the sections of the check in the window language, with "Gain", and the link becomes "Hide
  details"

#### Scenario: Help on the tab
- **WHEN** the Lighting tab is open and the user picks "Help on the Lighting tab (in the browser, Russian)"
- **THEN** the guide opens in the browser at its section about the Lighting tab

### Requirement: Lightmap UV checks
For every active MeshRenderer that contributes to global illumination and receives GI from lightmaps,
the tool SHALL check the second UV channel (UV1) of its mesh. A mesh without UV1 SHALL produce a Warning;
when the mesh comes from a model whose Generate Lightmap UVs option is off, the issue SHALL offer a fix
that turns the option on and reimports the model. UV1 coordinates outside the 0–1 range (with a small
tolerance) SHALL produce a Warning. UV1 triangles that overlap each other SHALL produce a Warning that
states the overlapping share; the share SHALL be estimated on a texel grid and small shares SHALL be
ignored. Each mesh SHALL be checked once, with the number of objects that use it.

#### Scenario: Model without lightmap UVs
- **WHEN** a lightmapped object uses a model mesh without UV1 and Generate Lightmap UVs is off
- **THEN** a Warning names the mesh and offers to turn Generate Lightmap UVs on

#### Scenario: Generated lightmap UVs
- **WHEN** the model has Generate Lightmap UVs on
- **THEN** no lightmap UV issue is reported for that mesh

#### Scenario: Overlapping UV1
- **WHEN** UV1 of a lightmapped mesh is a copy of overlapping UV0 islands
- **THEN** a Warning reports the overlapping share of the lightmap texels

#### Scenario: Not lightmapped
- **WHEN** the object receives GI from light probes
- **THEN** its UV1 is not checked

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

### Requirement: Mesh and model asset checks
For meshes and model files used in the loaded scenes the tool SHALL report, one issue per asset with the
number of users: vertex channels that the Standard-family shaders of its materials do not use (colors,
tangents without a normal map, UV2 and higher); split meshes (more than 1.5 vertices per triangle on meshes
of at least 500 triangles); 32-bit indices with fewer than 65 535 vertices; Mesh Compression off on large
models; an import Scale Factor other than 1; zero-area triangles; identical meshes stored as different
assets; a pivot far from the mesh. Per object it SHALL report an extreme scale and a small object with a
dense mesh. Per LOD group it SHALL report a LOD1 that keeps more than 70 % of LOD0 triangles, a last level
that is never culled, a LOD0 transition below 15 % of the screen and different materials between levels.
Per model it SHALL report animation import on models without animation, cameras or lights inside the
model and materials embedded in the model. Fixes SHALL be offered for index format, mesh compression,
animation, camera and light import, as asset fixes through the confirmation window.

#### Scenario: Static model with an Animator
- **WHEN** a model has an animation type but no clips and no skinned meshes
- **THEN** an Info issue offers to set Animation Type to None

#### Scenario: Weak LOD
- **WHEN** LOD1 of a LOD group has 100 % of the LOD0 triangles
- **THEN** a Warning reports the share and suggests a real simplification

### Requirement: Texture asset checks
For textures used in the loaded scenes the tool SHALL report: a non-power-of-two size with NPOT scaling
off; identical files under different names; texel density far above or below the object size; an alpha
channel that is fully opaque while the chosen format keeps alpha; sRGB on data maps (metallic, occlusion,
height, detail mask) in the Linear color space; anisotropic level 0–1 on large flat surfaces when the
quality level filters per texture; Streaming Mip Maps off on large textures when streaming is active.
Fixes SHALL be offered for NPOT scaling, alpha source, sRGB, anisotropic level and streaming.

#### Scenario: Metallic map with sRGB
- **WHEN** the project is in Linear color space and a metallic map has sRGB on
- **THEN** a Warning offers to turn sRGB off

### Requirement: Material checks
The tool SHALL have a Materials category and SHALL report: materials with the same shader and the same
property values (with a scene fix that makes the renderers use one of them, undoable); transparent
materials, as a Warning when a user is larger than 2 m; shader keywords without their texture (with a fix
that disables the keyword); one summary of Standard materials that use only an albedo map; objects that
render with the built-in Default-Material.

#### Scenario: Duplicate materials
- **WHEN** two materials have the same shader, textures and values
- **THEN** one issue lists both and offers to use the first one on every scene renderer

#### Scenario: Normal map keyword without a texture
- **WHEN** a material has `_NORMALMAP` enabled and no normal map
- **THEN** an Info issue offers to disable the keyword

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

### Requirement: Turned-off checks
The user SHALL be able to turn off the checks of the picked rows for the whole project after a confirmation;
every problem of a turned-off check SHALL be acceptable. The Budgets tab SHALL list turned-off checks with a
button to turn each on again, show the number of marks and offer to remove every mark and turn every check on.

#### Scenario: Turn a check off and on
- **WHEN** the user turns off the 2D sound check and later turns it on on the Budgets tab
- **THEN** all 2D sound problems are acceptable while it is off and count again after it is on

### Requirement: Acceptable problems in reports
The JSON report SHALL contain `summary.accepted` and `accepted` per issue, and the CSV report an `Accepted`
column; severity counts in the summary SHALL leave acceptable problems out.

#### Scenario: Export with marks
- **WHEN** a report is exported while three problems are acceptable
- **THEN** `summary.accepted` is 3 and those issues have `accepted: true`

### Requirement: Audit history
Every audit run from the window SHALL be recorded as a point of the history of the audited scene set in
`SceneAuditHistory/<scene>-<hash>.json` next to `Assets/`. A point SHALL hold the time, the tool version, an
optional note, the counts of Critical, Warning, Info and acceptable problems, the scene triangles, the draw call
estimate, the texture memory, the count of problems per check, the list of problems (check and location) and,
when a profiler snapshot newer than the previous point exists, its FPS, CPU and GPU times. Every run SHALL be a
new point, runs of the same day too. A history SHALL keep at most 200 points.

#### Scenario: A week of work
- **WHEN** the user runs the audit on Monday, fixes problems and runs it again on Monday and on the next Monday
- **THEN** the history has three points, and the second Monday run shows the difference from the first

#### Scenario: Noted point
- **WHEN** the user writes a note to a point and runs the audit again
- **THEN** the noted point stays and the run adds a new point

### Requirement: History tab
The window SHALL have a History tab with charts of problem counts over time, of texture memory in percent of its
warning limit and of the whole-scene triangles and draw calls without a threshold (they are no frame), a table of points
with changes against the previous point, a comparison of two picked points (changes of totals and budgets, checks whose
count changed, lists of fixed and new problems), editing the note of a point, removing a point and choosing another
recorded scene.

#### Scenario: Compare two points
- **WHEN** the user picks the first and the last point
- **THEN** the tab shows how many Critical and Warning problems are gone, budget changes, and the fixed and new problems by check and location

### Requirement: Progress on the Problems tab
The Problems tab SHALL show the change of Critical and Warning counts against the previous point of the
history with its date.

#### Scenario: After a fix
- **WHEN** the previous point had 5 Critical problems and the new run has 2
- **THEN** the line reads "Critical −3" with the date of the previous point

### Requirement: History from the command line
The command-line export SHALL record a history point when `-auditHistory` is given, with the note from
`-auditNote`.

#### Scenario: Nightly run
- **WHEN** the export runs with `-auditHistory -auditNote nightly`
- **THEN** the history of the scene gets a point with the note "nightly"

### Requirement: Target GPU
The Budgets tab SHALL offer a "Target GPU" choice of RTX 2060 (6 GB), RTX 2070 (8 GB), RTX 2080 (8 GB), RTX 3060
(12 GB) and RTX 4060 (8 GB), RTX 2070 being the default when the project has no budget file. Each GPU SHALL have its own starting
thresholds of scene triangles and texture memory (Warning / Critical):

| GPU | Triangles | Texture memory, MB |
|---|---|---|
| RTX 2060 | 2 500 000 / 5 000 000 | 1152 / 1536 |
| RTX 2070 | 3 000 000 / 6 000 000 | 1536 / 2048 |
| RTX 2080 | 3 500 000 / 7 000 000 | 1536 / 2048 |
| RTX 3060 | 3 000 000 / 6 000 000 | 2304 / 3072 |
| RTX 4060 | 3 500 000 / 7 000 000 | 1536 / 2048 |

The other thresholds SHALL be the same for every GPU. Choosing a GPU SHALL fill the threshold table with its
values; "Default" SHALL restore the values of the chosen GPU; "Save" SHALL store the GPU as `targetGpu` in
`ProjectSettings/SceneOptimizationAuditor.json`. When the thresholds differ from the chosen GPU set, the tab SHALL
mark them as custom. A saved budget file without `targetGpu` SHALL keep its values and show them as custom. The
chosen GPU SHALL be shown in the context line of the Problems tab.

#### Scenario: New project
- **WHEN** a project without a budget file runs the audit and its scene has 2 500 000 triangles
- **THEN** the target GPU is RTX 2070 and there is no triangle budget warning

#### Scenario: Weaker GPU
- **WHEN** the user chooses RTX 2060 and saves
- **THEN** the texture memory Warning threshold is 1152 MB and the budget file has `targetGpu` RTX 2060

#### Scenario: Budget file from an older version
- **WHEN** a project saved a texture memory Warning of 1536 MB before this version
- **THEN** the audit still warns above 1536 MB and the Budgets tab shows the thresholds as custom

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

### Requirement: Viewpoints in the history
When a viewpoint estimate newer than the previous history point exists, a new history point SHALL store, for the
viewpoint with the most draw calls, its draw calls, triangles and transparent share, and the number of viewpoints and
of viewpoints over budget. The History tab SHALL chart these values like the other budgets and SHALL compare them
when both points have them.

#### Scenario: Progress of the worst place
- **WHEN** the worst viewpoint had 1 450 draw calls at the first point and 900 at the next
- **THEN** the History tab shows the drop of 550 in green

### Requirement: Viewpoint tour in the history
When a viewpoint tour newer than the previous history point exists, a new history point SHALL store the worst
measured frame time, the number of viewpoints slower than 11.1 ms and whether the headset was active, and the History
tab SHALL chart the worst frame time.

#### Scenario: Faster after fixes
- **WHEN** the worst measured frame time was 12.4 ms at one point and 10.2 ms at the next
- **THEN** the History tab shows the drop of 2.2 ms in green

### Requirement: Lighting checks
The audit SHALL report: `LIGHT_LIGHTMAP_MEMORY` when the lightmaps take more than the "Lightmap memory, MB" budget
(Warning 128, Critical 256 by default, editable on the Budgets tab); `LIGHT_LIGHTMAP_UNCOMPRESSED` (Warning) when
lightmaps exist and compression is off; `LIGHT_LIGHTMAP_DIRECTIONAL` (Info) for directional lightmaps;
`LIGHT_NOT_BAKED` (Warning) when lightmapped objects have no lightmap or Baked / Mixed lights are not baked;
`LIGHT_SMALL_IN_LIGHTMAP` (Info, fix "Light Probes") for lightmapped objects under 0.5 m; `LIGHT_LIGHTMAP_SHARE`
(Warning) for objects over 20 % of an atlas; `LIGHT_NO_PROBES` (Warning) when baked lighting and objects lit by probes exist
without a Light Probe Group; `LIGHT_OUTSIDE_PROBES` (Info) for objects lit by probes outside the probe area; `LIGHT_DISTANCE_SHADOWMASK`
(Warning) for Distance Shadowmask with Mixed lights; `LIGHT_SHADOW_CASCADES` (Warning) for 4 cascades with a realtime
shadowed directional light; `LIGHT_WIDE_REALTIME` (Warning) for point / spot Realtime or Mixed lights reaching more than
50 renderers; `LIGHT_IMPORTANT` (Warning) for point / spot lights with Render Mode Important. Checks on lighting
settings SHALL open the Lighting Settings window from "Settings".

#### Scenario: Lightmaps over budget
- **WHEN** the lightmaps take 300 MB with the default budget
- **THEN** `LIGHT_LIGHTMAP_MEMORY` is Critical with 300 of 256 MB

### Requirement: Lightmap memory in the history
A history point SHALL store the lightmap memory, and the History tab SHALL chart and compare it.

#### Scenario: Smaller lightmaps
- **WHEN** the lightmaps took 180 MB at one point and 96 MB at the next
- **THEN** the History tab shows the drop of 84 MB in green

### Requirement: Particle checks
The audit SHALL check every active particle system of the loaded scenes in a "Particles" category. The particles alive at
once SHALL be estimated as Max Particles, but not more than Rate over Time × Start Lifetime (not longer than Duration
for a system that does not loop) plus the Bursts; a system with Rate over Distance, with Emission off or emitted as a
sub-emitter SHALL count Max Particles. The size SHALL be the largest one: Start Size × Size over Lifetime × the scale
of the transform (by Scaling Mode). It SHALL report: `PART_MAX_PARTICLES` when that estimate is above 1000 (Warning) or
5000 (Critical); `PART_TOTAL` (Warning) when the estimates of all systems add up to more than 20 000;
`PART_LARGE_TRANSPARENT` (Warning) for a billboard, stretched, horizontal or vertical system with an enabled renderer, a transparent material (render
queue 2501 or more), a size of 1 m or more and 20 or more particles at once; `PART_LIGHTS` (Warning) for the Lights module with a light set,
fixed by turning the module off; `PART_COLLISION_WORLD` (Warning) for world collision of High quality, fixed by Medium (which collides with static colliders only, as the fix says);
`PART_SHADOWS` (Warning) for an enabled particle renderer casting shadows, fixed by Cast Shadows Off; `PART_MESH_NO_INSTANCING`
(Info) for an enabled renderer of mesh particles without GPU Instancing, fixed by turning it on; `PART_NOISE_HIGH` (Info) for the Noise
module of High quality. The fixes SHALL run one row at a time after review and SHALL be undoable.

#### Scenario: Large smoke
- **WHEN** an active smoke system emits 40 billboards of 2 m with a transparent material
- **THEN** `PART_LARGE_TRANSPARENT` is a Warning naming the size and the number of particles

#### Scenario: Sparks with lights
- **WHEN** a system has the Lights module on and the user fixes the issue
- **THEN** the module is off, and Undo turns it back on

#### Scenario: Large cap, low rate
- **WHEN** a system has Max Particles 10 000 but emits 10 particles a second for 5 s
- **THEN** no `PART_MAX_PARTICLES` is reported

### Requirement: Shader checks
The audit SHALL check the shaders of the materials of active renderers in a "Shaders" category, one issue per shader
with the number of objects that use it. The source SHALL be read for `.shader` files under `Assets/` and their
`#include` files under `Assets/`. It SHALL report: `SHADER_ERROR` (Critical) for a shader with errors Unity knows of or an
unsupported shader; `SHADER_MISSING` (Critical) on a material whose shader is missing; `SHADER_GRABPASS` for a GrabPass in the first SubShader,
Warning without a texture name (a screen copy per object) and Info with one (a copy per frame); `SHADER_MULTIPASS`
(Warning) for more than one drawing pass in the first SubShader (passes without LightMode or with Always, the ForwardBase
passes — or the most of Vertex, VertexLM and VertexLMRGBM when there is no ForwardBase — and one for a surface shader; ForwardAdd, ShadowCaster, Meta,
Deferred and the like do not count); `SHADER_NO_STEREO` (Critical) when the stereo mode is Single Pass Instanced and a
vertex function defined in the source lacks the calls `UNITY_SETUP_INSTANCE_ID` or `UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO`;
`SHADER_VARIANTS` (Info) for a project shader with more than 512 variants; `SHADER_ALPHA_TEST_MSAA` (Info) when MSAA is
on, a material is alpha-tested (RenderType TransparentCutout) and the shader (its SubShader or Category) has no `AlphaToMask On`.

#### Scenario: Unlit template in Single Pass Instanced
- **WHEN** the stereo mode is Single Pass Instanced and a material uses an unlit shader made from the 2019.4 template
- **THEN** `SHADER_NO_STEREO` is Critical with the shader and the number of objects

#### Scenario: Outline pass
- **WHEN** a shader draws an outline in a second untagged pass
- **THEN** `SHADER_MULTIPASS` is a Warning naming 2 passes

#### Scenario: Standard shader
- **WHEN** the objects use only the Standard shader in Opaque mode
- **THEN** no shader issue is reported

### Requirement: Reflection probe checks
The audit SHALL check the active reflection probes of the loaded scenes in the Lighting category: `REFL_RESOLUTION` for
a resolution above 256 (Warning) or of 1024 and more (Critical); `REFL_MEMORY` (Warning) when the cubemaps of all probes
take more than 64 MB — a baked cubemap as imported, otherwise estimated from the resolution; `REFL_NOT_BAKED` (Warning)
for a Baked probe without its baked cubemap or a Custom one without its cubemap; `REFL_NO_BOX` (Info) for a probe of up
to 30 m on its largest side without Box Projection, fixed by turning it on; `REFL_DUPLICATE` (Warning) on the smaller of two
probes whose shared part is more than 80 % of the volume of each (a small probe nested in a large one is no duplicate); `REFL_OUTSIDE` (Info) when probes exist and renderers with
Reflection Probes on stand outside all of them.

#### Scenario: Room without box projection
- **WHEN** a baked probe of 10 × 4 × 8 m has Box Projection off and the user fixes the issue
- **THEN** Box Projection is on, and Undo turns it off

#### Scenario: Probe inside a probe
- **WHEN** a probe of 9.5 × 4 × 7.5 m lies inside one of 10 × 4 × 8 m
- **THEN** `REFL_DUPLICATE` is reported on the smaller probe naming the larger one and 100 %

#### Scenario: Room in the street
- **WHEN** a probe of 3 × 3 × 3 m lies inside one of 60 × 6 × 10 m
- **THEN** no `REFL_DUPLICATE` is reported

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

### Requirement: Fix gain
Issues whose fix has a measurable result SHALL carry an estimated gain in memory (MB), draw calls or triangles, rough
estimates marked as such: texture size, compression, Read/Write, unused alpha, duplicates and density; mesh Read/Write
and duplicates; audio decompressed on load and PCM; lightmap compression, direction textures and small objects in
lightmaps; reflection probe resolution and duplicates (memory); several or extra materials, instancing, realtime lights
and extra shader passes (draw calls); heavy meshes without LOD (triangles). The Problems tab SHALL show the gain of a
row, of a copies row and of a group, SHALL offer "By gain" to order the groups by the share of the budgets their fixes
free, and SHALL show the total of all problems that are not acceptable, counting several rows of one asset, object or
lighting setting with the largest of their estimates in each unit.

#### Scenario: Large uncompressed texture
- **WHEN** a 4096 × 4096 RGBA32 texture is reported as too large and uncompressed
- **THEN** the size row shows about −64 MB (to 2048), the compression row about −75 MB (to DXT1), and the total counts
  the texture once, with −75 MB

#### Scenario: Ordered by gain
- **WHEN** "By gain" is on and one group frees 100 MB while another frees 10 draw calls
- **THEN** the group with the larger share of its budget comes first

### Requirement: Quality and frame rate settings for VR
The audit SHALL report, as Info unless stated: the Gamma color space of the Player settings; active realtime or mixed
lights with soft shadows while the quality level allows soft shadows, with a reviewed fix that sets hard shadows; Realtime
Reflection Probes enabled on the quality level; Soft Particles enabled on the quality level while the scenes have active
particle systems; a VSync Count other than Don't Sync; and every script in `Assets/` that assigns
`Application.targetFrameRate` or `QualitySettings.vSyncCount`, comments not counted (with VR Unity ignores both; they cap
the Game view and the build without a headset). MSAA SHALL be a Warning
only when it is off and Info at 8x; the shadow distance SHALL be reported above 40 m; the quality Skin Weights SHALL be
reported above Two Bones when skinned meshes exist.

#### Scenario: MSAA 2x on the target GPU
- **WHEN** the quality level Standalone uses has Anti Aliasing 2x
- **THEN** no MSAA issue is reported

#### Scenario: A script caps the frame rate
- **WHEN** a script in `Assets/` contains `Application.targetFrameRate = 60;` outside a comment
- **THEN** an Info row names the script

#### Scenario: Soft shadows
- **WHEN** the quality level allows soft shadows and a realtime directional light has Soft Shadows
- **THEN** an Info row names the light and offers to set Hard Shadows

### Requirement: Target headset
The Budgets tab SHALL offer a target headset (HTC Vive Pro, HTC Vive Pro 2, HTC Vive Pro 2 Wireless, Valve Index), its
refresh rate among the rates of the headset and the SteamVR render resolution in percent, and SHALL show the per-eye
render resolution, the frame time and the pixel rate against the Vive Pro. The frame time thresholds (Critical 1000 / rate
ms, Warning 90 % of it), the FPS colors, the physics step expected by the physics checks, the field of view of viewpoints
and the transparent share thresholds (scaled by the pixel rate) SHALL follow the headset; edited values SHALL be marked as
custom. A project without a budget file SHALL start with Vive Pro 2 Wireless at 90 Hz and 100 %; a budget file without a
headset SHALL keep HTC Vive Pro at 90 Hz. The context line and the report SHALL name the headset and its rate.

#### Scenario: 120 Hz
- **WHEN** the user picks HTC Vive Pro 2 at 120 Hz
- **THEN** the frame time thresholds are 7.5 / 8.3 ms and a fixed timestep of 1/90 s is reported as slower than the display

#### Scenario: Old budget file
- **WHEN** a budget file saved before 2.3.0 is loaded
- **THEN** the headset is HTC Vive Pro at 90 Hz and its thresholds are not marked as custom

### Requirement: Terrain and grass
For every active terrain the audit SHALL estimate the grass and detail instances drawn around the player (the detail
maps of all layers × Detail Density × the share of the terrain inside the circle of Detail Distance) with their triangles
and report more than 100 000 as Warning and more than 300 000 as Critical; it SHALL report a Grass Billboard detail layer
as Warning with a reviewed fix to Grass in the terrain data asset, a Detail Distance above 60 m as Info with a reviewed fix to 60 m, Draw Instanced
off as Info with a reviewed fix, and a Pixel Error below 5 as Info.

#### Scenario: Dense grass
- **WHEN** a 100 × 100 m terrain has two grass layers of 8 per cell on a 128 × 128 detail map, Detail Density 1 and Detail Distance 80 m
- **THEN** about 262 000 instances are reported as Warning with about 524 000 triangles

#### Scenario: Very dense grass
- **WHEN** the same terrain has 16 per cell in both layers
- **THEN** about 524 000 instances are reported as Critical

#### Scenario: Details not drawn
- **WHEN** Draw of trees and details is off or Detail Density is 0
- **THEN** there are no rows of grass, Detail Distance or Grass Billboard for that terrain

#### Scenario: Billboard grass
- **WHEN** a detail layer is Grass Billboard
- **THEN** a Warning names the layer and its fix sets Grass in the terrain data

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
