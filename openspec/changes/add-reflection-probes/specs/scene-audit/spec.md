# Spec Delta

## ADDED Requirements

### Requirement: Reflection probe checks
The audit SHALL check the active reflection probes of the loaded scenes in the Lighting category: `REFL_RESOLUTION` for
a resolution above 256 (Warning) or of 1024 and more (Critical); `REFL_MEMORY` (Warning) when the cubemaps of all probes
take more than 64 MB — a baked cubemap as imported, otherwise estimated from the resolution; `REFL_NOT_BAKED` (Warning)
for a Baked probe without its baked cubemap or a Custom one without its cubemap; `REFL_NO_BOX` (Info) for a probe of up
to 30 m on its largest side without Box Projection, fixed by turning it on; `REFL_DUPLICATE` (Warning) on the smaller of two
probes whose shared part is more than 80 % of the volume of each (a small probe nested in a large one is no duplicate); `REFL_OUTSIDE` (Info) when probes exist and renderers with
Reflection Probes on stand outside all of them.

#### Scenario: Room without box projection
- **WHEN** a baked probe of 10 × 4 × 8 m has Box Projection off and the user fixes the issue
- **THEN** Box Projection is on, and Undo turns it off

#### Scenario: Probe inside a probe
- **WHEN** a probe of 9.5 × 4 × 7.5 m lies inside one of 10 × 4 × 8 m
- **THEN** `REFL_DUPLICATE` is reported on the smaller probe naming the larger one and 100 %

#### Scenario: Room in the street
- **WHEN** a probe of 3 × 3 × 3 m lies inside one of 60 × 6 × 10 m
- **THEN** no `REFL_DUPLICATE` is reported
