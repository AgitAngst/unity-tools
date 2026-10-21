# scene-audit-profiler Specification

## Purpose
Снимок метрик кадра (FPS, время CPU и GPU, draw calls, память) в Play Mode редактора с цветом по
бюджету 90 Гц и сравнением с предыдущим снимком — как ориентир до профилирования билда.

## Requirements

### Requirement: Capture
On "Capture Profiler Data" the tool SHALL enter Play Mode if needed, skip the warm-up frames, collect
the configured number of frames and leave Play Mode only if it entered it. The capture state SHALL
survive the domain reload caused by entering Play Mode.

#### Scenario: Capture from Edit Mode
- **WHEN** the user starts a capture of 300 frames with 60 warm-up frames in Edit Mode
- **THEN** Play Mode starts, 300 frames after the first 60 are measured, and Play Mode stops

#### Scenario: Capture in Play Mode
- **WHEN** the user starts a capture while already in Play Mode
- **THEN** the frames are measured and Play Mode continues

### Requirement: Metrics
The snapshot SHALL contain average, minimum and maximum FPS, average and peak CPU and GPU frame time,
draw calls, SetPass calls, batches, triangles per frame, texture memory, mesh memory and total allocated
memory. A metric that is not available SHALL be shown as "n/a" and stored as -1, never as 0. Frame
times SHALL be colored green up to the Warning threshold, yellow up to the Critical threshold and red
above it. The block SHALL state that editor numbers are indicative only.

#### Scenario: GPU time unavailable
- **WHEN** the GPU frame time cannot be read
- **THEN** GPU values show "n/a" and are exported as -1

### Requirement: Snapshot history
The last and the previous snapshots SHALL be stored per project and scene and SHALL survive an editor
restart. "Compare with Previous" SHALL show the delta of each metric with an up or down arrow colored
as better or worse.

#### Scenario: Compare
- **WHEN** two captures were made for the same scene
- **THEN** each metric shows the delta between them
