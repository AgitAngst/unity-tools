# Spec Delta

## MODIFIED Requirements

### Requirement: Target GPU
The Budgets tab SHALL offer a "Target GPU" choice of RTX 2060 (6 GB), RTX 2070 (8 GB), RTX 2080 (8 GB), RTX 3060
(12 GB) and RTX 4060 (8 GB), RTX 2070 being the default when the project has no budget file. Each GPU SHALL have its own starting
thresholds of scene triangles and texture memory (Warning / Critical):

| GPU | Triangles | Texture memory, MB |
|---|---|---|
| RTX 2060 | 2 500 000 / 5 000 000 | 1152 / 1536 |
| RTX 2070 | 3 000 000 / 6 000 000 | 1536 / 2048 |
| RTX 2080 | 3 500 000 / 7 000 000 | 1536 / 2048 |
| RTX 3060 | 3 000 000 / 6 000 000 | 2304 / 3072 |
| RTX 4060 | 3 500 000 / 7 000 000 | 1536 / 2048 |

The other thresholds SHALL be the same for every GPU. Choosing a GPU SHALL fill the threshold table with its
values; "Default" SHALL restore the values of the chosen GPU; "Save" SHALL store the GPU as `targetGpu` in
`ProjectSettings/SceneOptimizationAuditor.json`. When the thresholds differ from the chosen GPU set, the tab SHALL
mark them as custom. A saved budget file without `targetGpu` SHALL keep its values and show them as custom. The
chosen GPU SHALL be shown in the context line of the Problems tab.

#### Scenario: New project
- **WHEN** a project without a budget file runs the audit and its scene has 2 500 000 triangles
- **THEN** the target GPU is RTX 2070 and there is no triangle budget warning

#### Scenario: Weaker GPU
- **WHEN** the user chooses RTX 2060 and saves
- **THEN** the texture memory Warning threshold is 1152 MB and the budget file has `targetGpu` RTX 2060

#### Scenario: Budget file from an older version
- **WHEN** a project saved a texture memory Warning of 1536 MB before this version
- **THEN** the audit still warns above 1536 MB and the Budgets tab shows the thresholds as custom
