# Spec Delta

## ADDED Requirements

### Requirement: Player zone
The Viewpoints tab SHALL let the user mark where the player can be as one or more rectangles, each with a center, a width
and a length, a rotation around the vertical axis and a height (which floors belong to it), edited with handles in the
Scene view while the Player Zone button is on, added under the Scene view pivot and removed from a panel in the Scene
view, with Undo. The zone SHALL be stored in the viewpoint file of the loaded scenes. With "Only in the player zone"
ticked in the placement panel, every automatic source SHALL keep only the viewpoints inside the zone, and the downward
ray grid SHALL cover the zone instead of the whole level. Manual viewpoints SHALL stay and the ones outside the zone
SHALL be marked. The summary SHALL name the zone; placing from the command line SHALL use the stored zone.

#### Scenario: NavMesh larger than the play area
- **WHEN** the NavMesh covers the whole level, one rectangle covers a third of it and the user places viewpoints with "Only in the player zone"
- **THEN** every automatic viewpoint lies inside the rectangle

#### Scenario: Rotated rectangle on the second floor
- **WHEN** a rectangle turned by 45° spans only the height of the second floor
- **THEN** viewpoints are placed on the second floor inside the turned rectangle and none on the first floor below it

#### Scenario: Level without a NavMesh
- **WHEN** the level has neither a NavMesh nor teleports and a zone is set
- **THEN** the downward rays cover only the zone

#### Scenario: Manual viewpoint outside
- **WHEN** a manual viewpoint lies outside the zone and the user places again
- **THEN** it stays and is marked as outside the zone
