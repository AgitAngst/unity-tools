# Spec Delta

## MODIFIED Requirements

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
