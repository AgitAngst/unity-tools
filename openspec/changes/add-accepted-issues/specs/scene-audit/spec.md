# Spec Delta

## ADDED Requirements

### Requirement: Acceptable problems
The user SHALL be able to mark picked problems as acceptable and remove the mark. A mark SHALL be stored per
project in `ProjectSettings/SceneOptimizationAuditorAccepted.json`, keyed by the check and the GlobalObjectId of
the object (a component counts as its GameObject) or asset, or by the check alone for problems without an
object, so it survives renames, moves and new runs. Acceptable problems SHALL NOT be counted in the verdict,
the severity cards, the tab count, the safe batch fix or the report summary, SHALL be hidden by default and
shown, tagged as acceptable, with an "Acceptable (N)" toggle. The file SHALL be removed when nothing is marked.

#### Scenario: Large houses
- **WHEN** the user marks the extreme-scale problem of a house as acceptable and runs the audit again
- **THEN** the problem is tagged acceptable, hidden by default and not counted, and other objects of the same check still count

#### Scenario: Moved object
- **WHEN** a marked object is moved in the scene
- **THEN** its problem stays acceptable

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
