# Spec Delta

## ADDED Requirements

### Requirement: Audit scope
The auditor window SHALL offer a scope next to "Run": the open scenes (as before), the selection, the build scenes and
a folder, remembered between sessions. The selection scope SHALL list only the problems of the selected scene objects
(with their children) and of the assets they use, without project settings and whole-scene problems. The build scenes
scope SHALL offer to save modified scenes, check every enabled scene of the build settings that is not open on its own,
open the scenes back as they were and check the open build scenes last; problems of closed scenes SHALL offer "Open
Scene" instead of a fix; the same asset or project setting problem from several scenes SHALL be one row with the users
added up; a table SHALL list every scene with its counts and budgets. The folder scope SHALL check the prefabs, models,
materials, textures and audio clips of a folder under `Assets/` in a preview scene and list the problems of these
assets and of the objects inside their prefabs and models only.

#### Scenario: A part of the level
- **WHEN** the user selects one building and runs with the selection scope
- **THEN** the list holds the problems of the building's objects and of the assets they use, and no project settings

#### Scenario: Whole game
- **WHEN** the build settings list three scenes, one of them open, and the user runs with the build scenes scope
- **THEN** every scene is checked, the open scenes are the same afterwards, the problems of the open scene can be fixed
  and those of the closed ones offer "Open Scene"

#### Scenario: New assets
- **WHEN** a folder holds a prefab, a model, a loose material, a texture and an audio clip
- **THEN** their problems are listed with the prefab objects as "<file> › <path>", and nothing outside the folder
