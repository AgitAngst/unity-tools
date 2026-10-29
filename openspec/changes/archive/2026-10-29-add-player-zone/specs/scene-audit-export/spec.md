# Spec Delta

## MODIFIED Requirements

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
