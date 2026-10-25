# Spec Delta

## ADDED Requirements

### Requirement: Lighting checks
The audit SHALL report: `LIGHT_LIGHTMAP_MEMORY` when the lightmaps take more than the "Lightmap memory, MB" budget
(Warning 128, Critical 256 by default, editable on the Budgets tab); `LIGHT_LIGHTMAP_UNCOMPRESSED` (Warning) when
lightmaps exist and compression is off; `LIGHT_LIGHTMAP_DIRECTIONAL` (Info) for directional lightmaps;
`LIGHT_NOT_BAKED` (Warning) when lightmapped objects have no lightmap or Baked / Mixed lights are not baked;
`LIGHT_SMALL_IN_LIGHTMAP` (Info, fix "Light Probes") for lightmapped objects under 0.5 m; `LIGHT_LIGHTMAP_SHARE`
(Warning) for objects over 20 % of an atlas; `LIGHT_NO_PROBES` (Warning) when baked lighting and objects lit by probes exist
without a Light Probe Group; `LIGHT_OUTSIDE_PROBES` (Info) for objects lit by probes outside the probe area; `LIGHT_DISTANCE_SHADOWMASK`
(Warning) for Distance Shadowmask with Mixed lights; `LIGHT_SHADOW_CASCADES` (Warning) for 4 cascades with a realtime
shadowed directional light; `LIGHT_WIDE_REALTIME` (Warning) for point / spot Realtime or Mixed lights reaching more than
50 renderers; `LIGHT_IMPORTANT` (Warning) for point / spot lights with Render Mode Important. Checks on lighting
settings SHALL open the Lighting Settings window from "Settings".

#### Scenario: Lightmaps over budget
- **WHEN** the lightmaps take 300 MB with the default budget
- **THEN** `LIGHT_LIGHTMAP_MEMORY` is Critical with 300 of 256 MB

### Requirement: Lightmap memory in the history
A history point SHALL store the lightmap memory, and the History tab SHALL chart and compare it.

#### Scenario: Smaller lightmaps
- **WHEN** the lightmaps took 180 MB at one point and 96 MB at the next
- **THEN** the History tab shows the drop of 84 MB in green
