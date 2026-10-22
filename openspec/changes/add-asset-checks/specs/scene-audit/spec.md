# Spec Delta

## ADDED Requirements

### Requirement: Mesh and model asset checks
For meshes and model files used in the loaded scenes the tool SHALL report, one issue per asset with the
number of users: vertex channels that the Standard-family shaders of its materials do not use (colors,
tangents without a normal map, UV2 and higher); split meshes (more than 1.5 vertices per triangle on meshes
of at least 500 triangles); 32-bit indices with fewer than 65 535 vertices; Mesh Compression off on large
models; an import Scale Factor other than 1; zero-area triangles; identical meshes stored as different
assets; a pivot far from the mesh. Per object it SHALL report an extreme scale and a small object with a
dense mesh. Per LOD group it SHALL report a LOD1 that keeps more than 70 % of LOD0 triangles, a last level
that is never culled, a LOD0 transition below 15 % of the screen and different materials between levels.
Per model it SHALL report animation import on models without animation, cameras or lights inside the
model and materials embedded in the model. Fixes SHALL be offered for index format, mesh compression,
animation, camera and light import, as asset fixes through the confirmation window.

#### Scenario: Static model with an Animator
- **WHEN** a model has an animation type but no clips and no skinned meshes
- **THEN** an Info issue offers to set Animation Type to None

#### Scenario: Weak LOD
- **WHEN** LOD1 of a LOD group has 100 % of the LOD0 triangles
- **THEN** a Warning reports the share and suggests a real simplification

### Requirement: Texture asset checks
For textures used in the loaded scenes the tool SHALL report: a non-power-of-two size with NPOT scaling
off; identical files under different names; texel density far above or below the object size; an alpha
channel that is fully opaque while the chosen format keeps alpha; sRGB on data maps (metallic, occlusion,
height, detail mask) in the Linear color space; anisotropic level 0–1 on large flat surfaces when the
quality level filters per texture; Streaming Mip Maps off on large textures when streaming is active.
Fixes SHALL be offered for NPOT scaling, alpha source, sRGB, anisotropic level and streaming.

#### Scenario: Metallic map with sRGB
- **WHEN** the project is in Linear color space and a metallic map has sRGB on
- **THEN** a Warning offers to turn sRGB off

### Requirement: Material checks
The tool SHALL have a Materials category and SHALL report: materials with the same shader and the same
property values (with a scene fix that makes the renderers use one of them, undoable); transparent
materials, as a Warning when a user is larger than 2 m; shader keywords without their texture (with a fix
that disables the keyword); one summary of Standard materials that use only an albedo map; objects that
render with the built-in Default-Material.

#### Scenario: Duplicate materials
- **WHEN** two materials have the same shader, textures and values
- **THEN** one issue lists both and offers to use the first one on every scene renderer

#### Scenario: Normal map keyword without a texture
- **WHEN** a material has `_NORMALMAP` enabled and no normal map
- **THEN** an Info issue offers to disable the keyword
