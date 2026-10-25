# Spec Delta

## ADDED Requirements

### Requirement: Viewpoint tour in the JSON report
The JSON report SHALL contain `hasViewpointTour` and `viewpointTour`: the tour time, whether it was interrupted,
whether the headset was active, frames, warm-up and directions, and for every measured viewpoint its name, worst
direction, average and peak frame time, CPU and GPU times (−1 when unknown), dropped frames, draw calls, batches,
SetPass calls and triangles. Without a tour `hasViewpointTour` SHALL be false.

#### Scenario: Report after a tour
- **WHEN** the user measured 30 viewpoints and exports JSON
- **THEN** `hasViewpointTour` is true and `viewpointTour` lists 30 viewpoints with their frame times
