# Spec Delta

## Purpose

Аудит загруженных сцен Unity 2019.4 под PC VR (HTC Vive Pro, 90 Гц): найти проблемы
производительности и VR-настройки, показать их по уровням с подсказками и сравнить сцену с бюджетами.

## ADDED Requirements

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
The tool SHALL estimate scene triangles (LOD0 only), draw calls (material slots plus shadow passes,
doubled when stereo mode is not Single Pass Instanced) and texture memory, SHALL compare them with
configurable Warning and Critical thresholds, and SHALL label the values as estimates. Threshold
overrides SHALL be stored per project in `ProjectSettings/SceneOptimizationAuditor.json` and the file
SHALL be written only when the user saves budgets.

#### Scenario: Budget exceeded
- **WHEN** the triangle estimate is above the Warning threshold
- **THEN** the budget bar turns yellow and a Meshes Warning names the estimate and the threshold

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
