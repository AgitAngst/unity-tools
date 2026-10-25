# Spec Delta

## ADDED Requirements

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
