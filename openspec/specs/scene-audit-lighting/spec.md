# scene-audit-lighting Specification

## Purpose
Вкладка «Свет»: сколько памяти занимают lightmaps и кто её занимает, что не запечено, где движущимся объектам не
хватает проб и какие источники в реальном времени светят на много объектов.

## Requirements

### Requirement: Lighting tab
The auditor window SHALL have a "Lighting" tab after "Lightmap UVs". "Check lighting" SHALL collect the data of the
loaded scenes without changing them. The tab SHALL switch with segments between "Atlases", "Objects", "Lights" and
"Probes", each with its summary and table, and SHALL offer "Only marked" and "Open Lighting" (the Lighting Settings
window).

#### Scenario: Nothing baked
- **WHEN** the scene has no lightmaps and the user checks lighting
- **THEN** "Atlases" says that there are no lightmaps and "Objects" lists the lightmapped objects marked "no lightmap"

### Requirement: Lightmap atlases
"Atlases" SHALL list every lightmap with its number, size, format, memory (color, direction and shadowmask
estimated like textures), number of objects and filled share (the sum of its objects' shares), and above the table
the bake settings: lightmapper, Lightmap Size, resolution, compression, directional mode, Mixed Lighting mode and
shadowmask mode. Clicking an atlas SHALL select its objects.

#### Scenario: Directional atlas
- **WHEN** an atlas is 1024 × 1024 in DXT5 with a directional texture of the same size
- **THEN** its memory is the sum of both textures

### Requirement: Objects in lightmaps
"Objects" SHALL list the active renderers with Contribute GI and Receive GI = Lightmaps: atlas, share of the atlas
(`lightmapScaleOffset` x × y), Scale in Lightmap, size (the largest side of the bounds) and marks: "small — use probes"
under 0.5 m, "much of the atlas" over 20 %, "no lightmap" when the index is outside the atlases. "To probes" SHALL set
Receive GI to Light Probes through the confirmation window with Undo, one by one or for the selected objects.

#### Scenario: Small prop
- **WHEN** a 0.3 m cup is lightmapped
- **THEN** it is marked "small — use probes" and "To probes" switches it to Light Probes; Undo brings Lightmaps back

### Requirement: Lights and probes
"Lights" SHALL list the active lights with type, mode, shadows, range, the number of renderers whose bounds meet the
range sphere within its culling mask (all of them for directional), Important and whether the light is baked. "Probes" SHALL list the Light Probe
Groups with their probe counts and the renderers lit by probes (Blend Probes and not in lightmaps: moving ones and
Receive GI = Light Probes) whose bounds centre lies outside the box of all probes grown by 1 m, with the distance to the
nearest probe.

#### Scenario: Prop outside the probes
- **WHEN** probes cover a 20 m room and a moving barrel stands 6 m outside it
- **THEN** "Probes" lists the barrel with about 6 m to the nearest probe

### Requirement: Reflections on the Lighting tab
The Lighting tab SHALL have a fifth segment "Reflections" with a summary (probes, MB, without box projection, not baked,
objects outside), a table of the probes — mode, resolution, HDR, MB (estimated ones marked "≈"), box projection, size,
objects inside — with the marks "duplicate", "no box" and "not baked", "Box On" for the probes marked "no box" through
the confirmation window, and a list of the objects outside the probes with the distance to the nearest one, nearest first.
"Only marked" SHALL apply to the probes.

#### Scenario: No probes
- **WHEN** the scene has no reflection probes
- **THEN** "Reflections" says that the objects reflect only the sky

### Requirement: Shadows by distance from a point
The Lighting tab SHALL have a segment "Shadows" with a point and a radius set by the user (kept with the viewpoints of the
loaded scenes, moved with a handle in the Scene view or put at the centre of the Scene view, with Undo) and boxes for what
to turn off beyond the radius: Cast Shadows (on by default), Receive Shadows, Contribute GI, and "Keep Cast Shadows on
baked" (on by default). It SHALL list the enabled mesh and skinned mesh renderers of the loaded scenes at any depth that
cast shadows (On, Two Sided, Shadows Only), receive shadows or contribute to GI, with the distance from the point to the
nearest point of their bounds, their triangles and those three settings, farthest first, marking those beyond the radius,
the baked ones (Contribute GI with lightmaps) and Shadows Only ones. A search SHALL find a renderer by its name or the name
of any parent. "Turn Off Beyond N m" SHALL clear the ticked settings on the found renderers beyond the radius — Cast
Shadows except on Shadows Only ones and, while the box is on, baked ones — through the confirmation window with one Undo
step; a row SHALL offer the same for one renderer wherever it is.

#### Scenario: Far objects
- **WHEN** the point is at the origin, the radius is 30 m and a cube 50 m away casts shadows
- **THEN** the cube is 49.5 m away, marked beyond the radius, and the button turns its shadows off; Ctrl+Z turns them on

#### Scenario: Baked objects keep their shadow
- **WHEN** a far cube is static with Contribute GI and lit from lightmaps and "Keep Cast Shadows on baked" is on
- **THEN** it is marked baked and keeps Cast Shadows; with the box off it is turned off too

#### Scenario: Receive Shadows and GI
- **WHEN** Receive Shadows and Contribute GI are ticked
- **THEN** every far receiver loses Receive Shadows and the far cube with Contribute GI loses the flag; one Undo brings all back

#### Scenario: A parent's name
- **WHEN** the user searches "forest" and a mesh lies at Forest/Trees/Pine_01
- **THEN** that mesh is found

#### Scenario: The point is kept
- **WHEN** the user moves the point and opens the scene again
- **THEN** the point, the radius and the boxes are where they were
