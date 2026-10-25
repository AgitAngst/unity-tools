# Spec Delta

## ADDED Requirements

### Requirement: Particle checks
The audit SHALL check every active particle system of the loaded scenes in a "Particles" category. The particles alive at
once SHALL be estimated as Max Particles, but not more than Rate over Time × Start Lifetime (not longer than Duration
for a system that does not loop) plus the Bursts; a system with Rate over Distance, with Emission off or emitted as a
sub-emitter SHALL count Max Particles. The size SHALL be the largest one: Start Size × Size over Lifetime × the scale
of the transform (by Scaling Mode). It SHALL report: `PART_MAX_PARTICLES` when that estimate is above 1000 (Warning) or
5000 (Critical); `PART_TOTAL` (Warning) when the estimates of all systems add up to more than 20 000;
`PART_LARGE_TRANSPARENT` (Warning) for a billboard, stretched, horizontal or vertical system with an enabled renderer, a transparent material (render
queue 2501 or more), a size of 1 m or more and 20 or more particles at once; `PART_LIGHTS` (Warning) for the Lights module with a light set,
fixed by turning the module off; `PART_COLLISION_WORLD` (Warning) for world collision of High quality, fixed by Medium (which collides with static colliders only, as the fix says);
`PART_SHADOWS` (Warning) for an enabled particle renderer casting shadows, fixed by Cast Shadows Off; `PART_MESH_NO_INSTANCING`
(Info) for an enabled renderer of mesh particles without GPU Instancing, fixed by turning it on; `PART_NOISE_HIGH` (Info) for the Noise
module of High quality. The fixes SHALL run one row at a time after review and SHALL be undoable.

#### Scenario: Large smoke
- **WHEN** an active smoke system emits 40 billboards of 2 m with a transparent material
- **THEN** `PART_LARGE_TRANSPARENT` is a Warning naming the size and the number of particles

#### Scenario: Sparks with lights
- **WHEN** a system has the Lights module on and the user fixes the issue
- **THEN** the module is off, and Undo turns it back on

#### Scenario: Large cap, low rate
- **WHEN** a system has Max Particles 10 000 but emits 10 particles a second for 5 s
- **THEN** no `PART_MAX_PARTICLES` is reported
