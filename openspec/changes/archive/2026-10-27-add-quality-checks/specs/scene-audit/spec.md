# Spec Delta

## ADDED Requirements

### Requirement: Quality and frame rate settings for VR
The audit SHALL report, as Info unless stated: the Gamma color space of the Player settings; active realtime or mixed
lights with soft shadows while the quality level allows soft shadows, with a reviewed fix that sets hard shadows; Realtime
Reflection Probes enabled on the quality level; Soft Particles enabled on the quality level while the scenes have active
particle systems; a VSync Count other than Don't Sync; and every script in `Assets/` that assigns
`Application.targetFrameRate` or `QualitySettings.vSyncCount`, comments not counted (with VR Unity ignores both; they cap
the Game view and the build without a headset). MSAA SHALL be a Warning
only when it is off and Info at 8x; the shadow distance SHALL be reported above 40 m; the quality Skin Weights SHALL be
reported above Two Bones when skinned meshes exist.

#### Scenario: MSAA 2x on the target GPU
- **WHEN** the quality level Standalone uses has Anti Aliasing 2x
- **THEN** no MSAA issue is reported

#### Scenario: A script caps the frame rate
- **WHEN** a script in `Assets/` contains `Application.targetFrameRate = 60;` outside a comment
- **THEN** an Info row names the script

#### Scenario: Soft shadows
- **WHEN** the quality level allows soft shadows and a realtime directional light has Soft Shadows
- **THEN** an Info row names the light and offers to set Hard Shadows
