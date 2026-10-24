# Spec Delta

## Purpose

Вкладка «Свет»: сколько памяти занимают lightmaps и кто её занимает, что не запечено, где движущимся объектам не
хватает проб и какие источники в реальном времени светят на много объектов.

## ADDED Requirements

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
