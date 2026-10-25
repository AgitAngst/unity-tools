# Spec Delta

## Purpose

Вкладка «Тяжёлые»: какие исходники уровня — префабы, модели, меши — дают больше всего треугольников, draw calls и
памяти текстур.

## ADDED Requirements

### Requirement: Heaviest sources tab
The auditor window SHALL have a "Heaviest" tab after "Lighting". "Collect" SHALL list the sources of the active
renderers of the loaded scenes without changing them: a prefab (the asset of the nearest prefab instance root), else a
model or mesh asset, else the object. Each row SHALL show the source, its kind, copies, triangles per copy and in total
(LOD0 only, as every number of the row), draw calls (one per material, without shadows and lights), distinct materials, the memory of the distinct
textures of its materials and its share of the scene by the chosen measure, and SHALL mark "no LOD" when a copy has more
triangles than the object threshold and a renderer outside any LODGroup. Segments "Triangles | Draw calls | Textures" SHALL sort the
table; "Only without LOD" SHALL keep the rows marked "no LOD"; clicking a row SHALL select the copies; "Save CSV" SHALL
write every row.

#### Scenario: Forty houses
- **WHEN** 40 copies of a house prefab of 12 000 triangles without LODGroup stand in the scene
- **THEN** the house is a row with 40 copies, 12 000 per copy, 480 000 in total and "no LOD"
