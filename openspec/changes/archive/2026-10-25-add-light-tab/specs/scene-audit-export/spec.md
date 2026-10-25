# Spec Delta

## ADDED Requirements

### Requirement: Lighting in the JSON report
The JSON report SHALL contain `lighting`: the number of lightmaps, their memory and the budget limit, whether they
are directional and compressed, the number of lightmapped objects and of those without a lightmap, the number of light
probes and probe groups, and the number of objects lit by probes outside the probe area.

#### Scenario: Report of a baked scene
- **WHEN** the scene has 3 lightmaps of 32 MB and 120 lightmapped objects
- **THEN** `lighting.lightmaps` is 3, `lighting.lightmapMB` is 32 and `lighting.objectsInLightmaps` is 120
