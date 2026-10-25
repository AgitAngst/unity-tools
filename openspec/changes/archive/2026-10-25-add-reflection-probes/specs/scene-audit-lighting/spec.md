# Spec Delta

## ADDED Requirements

### Requirement: Reflections on the Lighting tab
The Lighting tab SHALL have a fifth segment "Reflections" with a summary (probes, MB, without box projection, not baked,
objects outside), a table of the probes — mode, resolution, HDR, MB (estimated ones marked "≈"), box projection, size,
objects inside — with the marks "duplicate", "no box" and "not baked", "Box On" for the probes marked "no box" through
the confirmation window, and a list of the objects outside the probes with the distance to the nearest one, nearest first.
"Only marked" SHALL apply to the probes.

#### Scenario: No probes
- **WHEN** the scene has no reflection probes
- **THEN** "Reflections" says that the objects reflect only the sky
