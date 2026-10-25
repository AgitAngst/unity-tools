# Spec Delta

## ADDED Requirements

### Requirement: Fix gain
Issues whose fix has a measurable result SHALL carry an estimated gain in memory (MB), draw calls or triangles, rough
estimates marked as such: texture size, compression, Read/Write, unused alpha, duplicates and density; mesh Read/Write
and duplicates; audio decompressed on load and PCM; lightmap compression, direction textures and small objects in
lightmaps; reflection probe resolution and duplicates (memory); several or extra materials, instancing, realtime lights
and extra shader passes (draw calls); heavy meshes without LOD (triangles). The Problems tab SHALL show the gain of a
row, of a copies row and of a group, SHALL offer "By gain" to order the groups by the share of the budgets their fixes
free, and SHALL show the total of all problems that are not acceptable, counting several rows of one asset, object or
lighting setting with the largest of their estimates in each unit.

#### Scenario: Large uncompressed texture
- **WHEN** a 4096 × 4096 RGBA32 texture is reported as too large and uncompressed
- **THEN** the size row shows about −64 MB (to 2048), the compression row about −75 MB (to DXT1), and the total counts
  the texture once, with −75 MB

#### Scenario: Ordered by gain
- **WHEN** "By gain" is on and one group frees 100 MB while another frees 10 draw calls
- **THEN** the group with the larger share of its budget comes first
