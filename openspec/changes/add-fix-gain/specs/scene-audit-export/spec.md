# Spec Delta

## ADDED Requirements

### Requirement: Gain in the report
Every issue of the JSON report SHALL carry `gainMB`, `gainDrawCalls`, `gainTris` and `gainApprox`, and `summary` SHALL
carry their totals over the issues that are not acceptable (several rows of one asset or object with their largest
estimate); the CSV report SHALL end with `GainMB`, `GainDrawCalls` and
`GainTris`.

#### Scenario: Report of a scene with a large texture
- **WHEN** the scene has a texture whose size issue frees 48 MB
- **THEN** that issue has `gainMB` 48 and `summary.gainMB` is at least 48
