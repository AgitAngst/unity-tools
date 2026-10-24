# Spec Delta

## ADDED Requirements

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
the NavMesh and teleports when no viewpoint file exists, put the estimate in the report and, with `-auditHistory`,
in the history point.

#### Scenario: Nightly run with viewpoints
- **WHEN** the export runs with `-auditViewpoints -auditHistory` on a scene with saved viewpoints
- **THEN** the report has `hasViewpoints: true` and the history point stores the worst viewpoint values
