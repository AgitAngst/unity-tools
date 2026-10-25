# Spec Delta

## ADDED Requirements

### Requirement: Lightmap UV checks
For every active MeshRenderer that contributes to global illumination and receives GI from lightmaps,
the tool SHALL check the second UV channel (UV1) of its mesh. A mesh without UV1 SHALL produce a Warning;
when the mesh comes from a model whose Generate Lightmap UVs option is off, the issue SHALL offer a fix
that turns the option on and reimports the model. UV1 coordinates outside the 0–1 range (with a small
tolerance) SHALL produce a Warning. UV1 triangles that overlap each other SHALL produce a Warning that
states the overlapping share; the share SHALL be estimated on a texel grid and small shares SHALL be
ignored. Each mesh SHALL be checked once, with the number of objects that use it.

#### Scenario: Model without lightmap UVs
- **WHEN** a lightmapped object uses a model mesh without UV1 and Generate Lightmap UVs is off
- **THEN** a Warning names the mesh and offers to turn Generate Lightmap UVs on

#### Scenario: Generated lightmap UVs
- **WHEN** the model has Generate Lightmap UVs on
- **THEN** no lightmap UV issue is reported for that mesh

#### Scenario: Overlapping UV1
- **WHEN** UV1 of a lightmapped mesh is a copy of overlapping UV0 islands
- **THEN** a Warning reports the overlapping share of the lightmap texels

#### Scenario: Not lightmapped
- **WHEN** the object receives GI from light probes
- **THEN** its UV1 is not checked
