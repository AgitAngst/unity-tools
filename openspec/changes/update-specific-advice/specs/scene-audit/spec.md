# Spec Delta

## MODIFIED Requirements

### Requirement: Material checks
The tool SHALL have a Materials category and SHALL report: materials with the same shader and the same
property values (with a scene fix that makes the renderers use one of them, undoable); transparent
materials, as a Warning when a user is larger than 2 m; shader keywords without their texture (with a fix
that disables the keyword); every Standard material under `Assets/` that uses only an albedo map, one issue per material with the
number of objects, the shader that fits it and its Metallic and Smoothness; objects that render with the built-in
Default-Material. The shader that fits SHALL be Legacy Shaders/Transparent/Cutout/Diffuse for a Cutout material (`_Mode`
1 or `_ALPHATEST_ON`), Legacy Shaders/Diffuse when `_Color` is not white, otherwise Mobile/Diffuse. For a material asset
under `Assets/` the issue SHALL offer a reviewed fix that sets that shader and clears the Standard keywords, keeping the
texture, tiling, color and cutoff; materials inside models SHALL get no fix, a text that names the material and the
advice to extract them.

#### Scenario: Duplicate materials
- **WHEN** two materials have the same shader, textures and values
- **THEN** one issue lists both and offers to use the first one on every scene renderer

#### Scenario: Normal map keyword without a texture
- **WHEN** a material has `_NORMALMAP` enabled and no normal map
- **THEN** an Info issue offers to disable the keyword

#### Scenario: Simple Standard material
- **WHEN** an opaque Standard material asset with a white color uses only an albedo map
- **THEN** an Info issue on that material names Mobile/Diffuse and its fix switches the material to it

#### Scenario: Tinted Standard material
- **WHEN** a simple Standard material has a red `_Color`
- **THEN** the issue names Legacy Shaders/Diffuse, which keeps the color

### Requirement: Shader checks
The audit SHALL check the shaders of the materials of active renderers in a "Shaders" category, one issue per shader
with the number of objects that use it. The source SHALL be read for `.shader` files under `Assets/` and their
`#include` files under `Assets/`. It SHALL report: `SHADER_ERROR` (Critical) for a shader with errors Unity knows of or an
unsupported shader; `SHADER_MISSING` (Critical) on a material whose shader is missing; `SHADER_GRABPASS` for a GrabPass in the first SubShader,
Warning without a texture name (a screen copy per object) and Info with one (a copy per frame); `SHADER_MULTIPASS`
(Warning) for more than one drawing pass in the first SubShader (passes without LightMode or with Always, the ForwardBase
passes — or the most of Vertex, VertexLM and VertexLMRGBM when there is no ForwardBase — and one for a surface shader; ForwardAdd, ShadowCaster, Meta,
Deferred and the like do not count); `SHADER_NO_STEREO` (Critical) when the stereo mode is Single Pass Instanced and a
vertex function defined in the source lacks the calls `UNITY_SETUP_INSTANCE_ID` or `UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO`;
`SHADER_VARIANTS` (Info) for a project shader with more than 512 variants; `SHADER_ALPHA_TEST_MSAA` (Info) when MSAA is
on, a material is alpha-tested (RenderType TransparentCutout) and the shader (its SubShader or Category) has no `AlphaToMask On`.
`SHADER_VARIANTS` SHALL name the number of variants the scene needs (Unity's count without the `shader_feature` variants no
material uses and the fog and instancing variants the scene does not need) and up to four keyword lines that multiply the most: `multi_compile` and `shader_feature` lines of the source
with their number of values, the built-in sets (`multi_compile_fog` 4, `multi_compile_instancing` and
`multi_compile_particles` 2, other `multi_compile_*` and `#pragma surface` without a number), and how many more there are.
Own `multi_compile` lines SHALL come first, then `shader_feature`, then the built-in sets. When the scene needs 512
variants or fewer, the advice SHALL say the shader can stay as it is. With several scenes there SHALL be one row per
shader with the most variants a scene needs.

#### Scenario: Unlit template in Single Pass Instanced
- **WHEN** the stereo mode is Single Pass Instanced and a material uses an unlit shader made from the 2019.4 template
- **THEN** `SHADER_NO_STEREO` is Critical with the shader and the number of objects

#### Scenario: Outline pass
- **WHEN** a shader draws an outline in a second untagged pass
- **THEN** `SHADER_MULTIPASS` is a Warning naming 2 passes

#### Scenario: Standard shader
- **WHEN** the objects use only the Standard shader in Opaque mode
- **THEN** no shader issue is reported

#### Scenario: Keyword lines that multiply
- **WHEN** a project shader has ten `multi_compile _ KEYWORD` lines and one `multi_compile _ A B C` line
- **THEN** `SHADER_VARIANTS` names `multi_compile _ A B C ×4` first

## ADDED Requirements

### Requirement: Trilinear filtering per texture
For every texture of the project with mipmaps that is used on 3D objects and filtered Point or Bilinear, the tool SHALL
report `TEX_NOT_TRILINEAR` (Info) on that texture with its filter mode and the number of objects, with a fix that sets
Trilinear in the importer: a safe fix for Bilinear, a reviewed one for Point (sharp pixels may be meant). The issue SHALL be re-checked with its texture, not as a check over the whole scene.

#### Scenario: Bilinear texture
- **WHEN** a 3D object uses a texture with mipmaps and Bilinear filtering
- **THEN** an Info issue on that texture names Bilinear and its fix sets Trilinear
