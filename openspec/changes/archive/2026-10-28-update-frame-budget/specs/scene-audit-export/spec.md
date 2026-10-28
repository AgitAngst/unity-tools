# Spec Delta

## ADDED Requirements

### Requirement: Frame budget in the JSON report
The JSON report SHALL carry `hasFrame` and `frame` with the number of estimated viewpoints and the worst viewpoints of the
run: `tris` with `trisViewpoint` and `drawCalls` with `drawCallsViewpoint`; without viewpoints `hasFrame` SHALL be false.
`sceneBudgets` SHALL keep the whole-scene figures. With several scenes `frame` SHALL hold the worst scene with viewpoints,
its name before the viewpoint.

#### Scenario: Scene with a viewpoint
- **WHEN** the scene has one viewpoint and the report is exported
- **THEN** `hasFrame` is true and `frame.drawCallsViewpoint` names that viewpoint
