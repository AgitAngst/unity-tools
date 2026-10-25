# Spec Delta

## ADDED Requirements

### Requirement: Audit history
Every audit run from the window SHALL be recorded as a point of the history of the audited scene set in
`SceneAuditHistory/<scene>-<hash>.json` next to `Assets/`. A point SHALL hold the time, the tool version, an
optional note, the counts of Critical, Warning, Info and acceptable problems, the scene triangles, the draw call
estimate, the texture memory, the count of problems per check, the list of problems (check and location) and,
when a profiler snapshot newer than the previous point exists, its FPS, CPU and GPU times. Runs of the same day
SHALL replace the last point of that day unless it has a note. A history SHALL keep at most 200 points.

#### Scenario: A week of work
- **WHEN** the user runs the audit on Monday, fixes problems and runs it again on Monday and on the next Monday
- **THEN** the history has two points: the last run of each day

#### Scenario: Noted point
- **WHEN** the user writes a note to today's point and runs the audit again the same day
- **THEN** the noted point stays and the run adds a new point

### Requirement: History tab
The window SHALL have a History tab with charts of problem counts and of budgets in percent of their warning
limits over time, a table of points with changes against the previous point, a comparison of two picked
points (changes of totals and budgets, checks whose count changed, lists of fixed and new problems), editing
the note of a point, removing a point and choosing another recorded scene.

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
