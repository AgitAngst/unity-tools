# Spec Delta

## ADDED Requirements

### Requirement: Heaviest sources in the JSON report
The JSON report SHALL contain `heavy`: the 20 sources with the most triangles, each with its name, kind, asset path,
copies, total triangles, draw calls and texture memory.

#### Scenario: Report with houses
- **WHEN** 40 copies of a house prefab are the heaviest source
- **THEN** `heavy[0]` names the house prefab with 40 copies
