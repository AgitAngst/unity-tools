# Spec Delta

## ADDED Requirements

### Requirement: Terrain and grass
For every active terrain the audit SHALL estimate the grass and detail instances drawn around the player (the detail
maps of all layers × Detail Density × the share of the terrain inside the circle of Detail Distance) with their triangles
and report more than 100 000 as Warning and more than 300 000 as Critical; it SHALL report a Grass Billboard detail layer
as Warning with a reviewed fix to Grass in the terrain data asset, a Detail Distance above 60 m as Info with a reviewed fix to 60 m, Draw Instanced
off as Info with a reviewed fix, and a Pixel Error below 5 as Info.

#### Scenario: Dense grass
- **WHEN** a 100 × 100 m terrain has two grass layers of 8 per cell on a 128 × 128 detail map, Detail Density 1 and Detail Distance 80 m
- **THEN** about 262 000 instances are reported as Warning with about 524 000 triangles

#### Scenario: Very dense grass
- **WHEN** the same terrain has 16 per cell in both layers
- **THEN** about 524 000 instances are reported as Critical

#### Scenario: Details not drawn
- **WHEN** Draw of trees and details is off or Detail Density is 0
- **THEN** there are no rows of grass, Detail Distance or Grass Billboard for that terrain

#### Scenario: Billboard grass
- **WHEN** a detail layer is Grass Billboard
- **THEN** a Warning names the layer and its fix sets Grass in the terrain data
