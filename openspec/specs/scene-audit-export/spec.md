# scene-audit-export Specification

## Purpose
Выгрузка результата аудита сцены в JSON и CSV — из окна утилиты и из командной строки Unity, чтобы
хранить, сравнивать и проверять отчёты без открытого окна.

## Requirements

### Requirement: JSON report
The JSON report SHALL contain the scene list, an ISO 8601 timestamp, the platform, the target FPS,
the XR state, the checked quality level, the severity summary, the scene budgets with an estimate flag,
a `hasProfilerSnapshot` flag with the snapshot, and the issue list. Check ids, categories, severities
and XR values SHALL be in English; messages and suggestions SHALL be in the current window language.

#### Scenario: No profiler snapshot
- **WHEN** a report is exported before any profiler capture
- **THEN** `hasProfilerSnapshot` is false

#### Scenario: Russian window language
- **WHEN** the window language is RU and the user exports JSON
- **THEN** `severity` values are English and `message` values are Russian

### Requirement: CSV report
The CSV report SHALL have the columns `CheckId, Category, Severity, Scene, ObjectPath, AssetPath,
Message, Suggestion, AutoFixable`, one row per issue, SHALL escape fields by RFC 4180 and SHALL be
written as UTF-8 with BOM.

#### Scenario: Message with a comma and quotes
- **WHEN** an issue message contains a comma and double quotes
- **THEN** the field is quoted and inner quotes are doubled

### Requirement: Command-line export
The tool SHALL provide a static method for `-executeMethod` that opens the scene given by `-auditScene`,
runs the audit and writes the report to the path given by `-auditReport` (format by extension), and
SHALL exit with code 0 on success and 1 on any error.

#### Scenario: Missing scene argument
- **WHEN** the method runs without `-auditScene`
- **THEN** an English error is logged and Unity exits with code 1

### Requirement: Viewpoints in the JSON report
The JSON report SHALL contain `hasViewpoints` and `viewpoints`: the estimate time, the thresholds used and for every
viewpoint its name, kind, position, worst direction, triangles, draw calls, transparent share and whether it is over
budget. Without an estimate `hasViewpoints` SHALL be false. The report SHALL also contain `targetGpu` — the chosen
GPU, or `Custom` when the thresholds differ from its set.

#### Scenario: Report after an estimate
- **WHEN** the user estimated viewpoints and exports JSON
- **THEN** `hasViewpoints` is true and `viewpoints` lists every viewpoint with its worst direction

### Requirement: Viewpoints from the command line
With `-auditViewpoints` the command-line export SHALL estimate the saved viewpoints of the scene, or place them from
the NavMesh and teleports (downward rays when there are neither) when the viewpoint file has no viewpoints, inside the
player zone of the file when it has one and "Only in the player zone" is not unticked, save them in the viewpoint file,
put the estimate in the report and, with `-auditHistory`, in the history point.

#### Scenario: Nightly run with viewpoints
- **WHEN** the export runs with `-auditViewpoints -auditHistory` on a scene with saved viewpoints
- **THEN** the report has `hasViewpoints: true` and the history point stores the worst viewpoint values

#### Scenario: A zone and no viewpoints yet
- **WHEN** the viewpoint file has a player zone and no viewpoints and the export runs with `-auditViewpoints`
- **THEN** the viewpoints are placed inside the zone

### Requirement: Viewpoint tour in the JSON report
The JSON report SHALL contain `hasViewpointTour` and `viewpointTour`: the tour time, whether it was interrupted,
whether the headset was active, frames, warm-up and directions, and for every measured viewpoint its name, worst
direction, average and peak frame time, CPU and GPU times (−1 when unknown), dropped frames, draw calls, batches,
SetPass calls and triangles. Without a tour `hasViewpointTour` SHALL be false.

#### Scenario: Report after a tour
- **WHEN** the user measured 30 viewpoints and exports JSON
- **THEN** `hasViewpointTour` is true and `viewpointTour` lists 30 viewpoints with their frame times

### Requirement: Lighting in the JSON report
The JSON report SHALL contain `lighting`: the number of lightmaps, their memory and the budget limit, whether they
are directional and compressed, the number of lightmapped objects and of those without a lightmap, the number of light
probes and probe groups, and the number of objects lit by probes outside the probe area.

#### Scenario: Report of a baked scene
- **WHEN** the scene has 3 lightmaps of 32 MB and 120 lightmapped objects
- **THEN** `lighting.lightmaps` is 3, `lighting.lightmapMB` is 32 and `lighting.objectsInLightmaps` is 120

### Requirement: Heaviest sources in the JSON report
The JSON report SHALL contain `heavy`: the 20 sources with the most triangles, each with its name, kind, asset path,
copies, total triangles, draw calls and texture memory.

#### Scenario: Report with houses
- **WHEN** 40 copies of a house prefab are the heaviest source
- **THEN** `heavy[0]` names the house prefab with 40 copies

### Requirement: Reflection probes in the JSON report
The `lighting` block of the JSON report SHALL contain `reflectionProbes` (the number of active probes), `reflectionMB`
(their memory, rounded to 0.1 MB) and `outsideReflection` (the number of objects outside the probes).

#### Scenario: Report of a scene with probes
- **WHEN** the scene has 7 probes and 2 objects outside them
- **THEN** `lighting.reflectionProbes` is 7 and `lighting.outsideReflection` is 2

### Requirement: Several scenes and folders from the command line
The command line SHALL accept several scenes (`-auditScene` with a comma or semicolon separated list, or repeated),
`-auditBuildScenes` and `-auditFolder <Assets/...>`, check every scene on its own and write one report. The JSON report
SHALL contain `scope`, `folder`, `sceneDetails` (path, counts and budgets of each scene; `scenes` keeps the names) and
`scenePath` of every problem;
with several scenes `sceneBudgets` SHALL hold the worst scene. The CSV report SHALL contain `ScenePath`. With
`-auditHistory` every scene SHALL get its own history point.

#### Scenario: Two scenes
- **WHEN** the command line names two scenes
- **THEN** the report lists both in `sceneDetails` and every problem names its scene in `scenePath`

### Requirement: Gain in the report
Every issue of the JSON report SHALL carry `gainMB`, `gainDrawCalls`, `gainTris` and `gainApprox`, and `summary` SHALL
carry their totals over the issues that are not acceptable (several rows of one asset or object with their largest
estimate); the CSV report SHALL end with `GainMB`, `GainDrawCalls` and
`GainTris`.

#### Scenario: Report of a scene with a large texture
- **WHEN** the scene has a texture whose size issue frees 48 MB
- **THEN** that issue has `gainMB` 48 and `summary.gainMB` is at least 48

### Requirement: Frame budget in the JSON report
The JSON report SHALL carry `hasFrame` and `frame` with the number of estimated viewpoints and the worst viewpoints of the
run: `tris` with `trisViewpoint` and `drawCalls` with `drawCallsViewpoint`; without viewpoints `hasFrame` SHALL be false.
`sceneBudgets` SHALL keep the whole-scene figures. With several scenes `frame` SHALL hold the worst scene with viewpoints,
its name before the viewpoint.

#### Scenario: Scene with a viewpoint
- **WHEN** the scene has one viewpoint and the report is exported
- **THEN** `hasFrame` is true and `frame.drawCallsViewpoint` names that viewpoint
