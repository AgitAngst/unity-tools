# Spec Delta

## ADDED Requirements

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
