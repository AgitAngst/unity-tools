# Spec Delta

## ADDED Requirements

### Requirement: Reflection probes in the JSON report
The `lighting` block of the JSON report SHALL contain `reflectionProbes` (the number of active probes), `reflectionMB`
(their memory, rounded to 0.1 MB) and `outsideReflection` (the number of objects outside the probes).

#### Scenario: Report of a scene with probes
- **WHEN** the scene has 7 probes and 2 objects outside them
- **THEN** `lighting.reflectionProbes` is 7 and `lighting.outsideReflection` is 2
