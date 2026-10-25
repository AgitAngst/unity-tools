# Spec Delta

## ADDED Requirements

### Requirement: Shader checks
The audit SHALL check the shaders of the materials of active renderers in a "Shaders" category, one issue per shader
with the number of objects that use it. The source SHALL be read for `.shader` files under `Assets/` and their
`#include` files under `Assets/`. It SHALL report: `SHADER_ERROR` (Critical) for a shader with errors Unity knows of or an
unsupported shader, and on the material for a missing one; `SHADER_GRABPASS` for a GrabPass in the first SubShader,
Warning without a texture name (a screen copy per object) and Info with one (a copy per frame); `SHADER_MULTIPASS`
(Warning) for more than one drawing pass in the first SubShader (passes without LightMode or with Always, the ForwardBase
passes — or the most of Vertex, VertexLM and VertexLMRGBM when there is no ForwardBase — and one for a surface shader; ForwardAdd, ShadowCaster, Meta,
Deferred and the like do not count); `SHADER_NO_STEREO` (Critical) when the stereo mode is Single Pass Instanced and a
vertex function defined in the source lacks `UNITY_VERTEX_OUTPUT_STEREO` or `UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO`;
`SHADER_VARIANTS` (Info) for a project shader with more than 512 variants; `SHADER_ALPHA_TEST_MSAA` (Info) when MSAA is
on, a material is alpha-tested (RenderType TransparentCutout) and the shader has no `AlphaToMask On`.

#### Scenario: Unlit template in Single Pass Instanced
- **WHEN** the stereo mode is Single Pass Instanced and a material uses an unlit shader made from the 2019.4 template
- **THEN** `SHADER_NO_STEREO` is Critical with the shader and the number of objects

#### Scenario: Outline pass
- **WHEN** a shader draws an outline in a second untagged pass
- **THEN** `SHADER_MULTIPASS` is a Warning naming 2 passes

#### Scenario: Standard shader
- **WHEN** the objects use only the Standard shader in Opaque mode
- **THEN** no shader issue is reported
