# Spec Delta

## MODIFIED Requirements

### Requirement: Viewpoint estimate
For each viewpoint the tool SHALL evaluate 8 directions 45° apart at zero pitch (plus the own direction of manual
and camera viewpoints) with one view frustum for both eyes (the field of view of the target headset — 110° vertical and aspect 0.9 for
the Vive Pro and the Index, 116° and 1 for the Vive Pro 2 and the Vive Pro 2 Wireless —, near plane 0.05, far plane of the scene VR camera or 1000) and, when a VR camera exists, its culling mask and per-layer cull
distances. A renderer SHALL count when it is active and its bounds intersect the frustum. A LOD group SHALL show the
level Unity would pick from the group size and scale, the distance to its reference point, the field of view,
`QualitySettings.lodBias` and `maximumLODLevel`, and nothing below its last transition. Per direction the tool SHALL
compute triangles of the visible meshes, draw calls (material slots, one per particle, line and sprite renderer,
extra pixel light passes for Realtime and Mixed point and spot lights whose range touches the renderer, up to the
Pixel Light Count, shadow passes of shadow casters within the shadow distance for directional lights and within the
range for point and spot lights, doubled with VR in Multi Pass or Single Pass stereo) and the share of the
screen covered by the bounds of transparent renderers (render queue 2501 and above) and particles, in percent. An
optional "Account for walls" mode, off by default, SHALL hide a renderer when rays from the eye to its bounds centre
and four top corners hit colliders of other objects first. The result of a viewpoint SHALL be its worst direction by draw
calls, then by triangles; all directions SHALL be kept. Terrain SHALL NOT be counted. The estimate SHALL run from its
own button with a cancelable progress bar and SHALL NOT be part of the audit run.

#### Scenario: LOD level by distance
- **WHEN** a LOD group with transitions 0.5 / 0.2 / 0.05 stands so that its screen height is 0.3 of the view
- **THEN** only the LOD1 renderers of that group are counted

#### Scenario: Object behind the viewer
- **WHEN** an object lies outside the view frustum of every direction of a viewpoint
- **THEN** its triangles and draw calls are not counted for that viewpoint

#### Scenario: Shadow passes
- **WHEN** a visible shadow caster with one material is lit by a Realtime directional light with 2 cascades and the stereo mode is Single Pass Instanced
- **THEN** it adds 1 draw call for itself and 2 for shadows

#### Scenario: Cancel
- **WHEN** the user cancels the progress bar during the estimate
- **THEN** the previous estimate stays and nothing is written
