# Spec Delta

## Purpose

Замер в Play Mode по точкам обзора: в каждой точке и направлении — настоящее время кадра и счётчики отрисовки,
чтобы найти места уровня, где кадр не укладывается в 11,1 мс, и сравнить их с оценкой в редакторе.

## ADDED Requirements

### Requirement: Tour run
"Measure in Play Mode" SHALL measure the viewpoints the user picks — the worst by estimate (30 by default, 1 to 500),
the selected ones or all — with frames per direction (30), warm-up frames (15) and directions (8 or 4) kept in
`EditorPrefs`. Outside Play Mode the tour SHALL enter Play Mode, measure and leave it; inside Play Mode it SHALL
measure and stay. The tour SHALL survive the domain reload of entering Play Mode. "Cancel", leaving Play Mode or a
script reload SHALL stop the tour; measured viewpoints SHALL be kept and the tour marked as interrupted. Before the
start and while running the tab SHALL show the remaining time estimated as viewpoints × directions × (warm-up +
frames) / 90.

#### Scenario: Start outside Play Mode
- **WHEN** the user starts a tour of 10 viewpoints in Edit Mode
- **THEN** the editor enters Play Mode, measures 10 viewpoints and returns to Edit Mode

#### Scenario: Interrupted tour
- **WHEN** the user leaves Play Mode after 4 of 10 viewpoints
- **THEN** the 4 measured viewpoints are kept and the tour is marked interrupted

### Requirement: Moving the viewer
The tour SHALL move, as the user chooses, the root of the VR camera rig (default; the VR camera is an enabled camera
with Target Eye other than None), an object the user picks or the camera itself. For the rig the tour SHALL move and
turn the root around the head so that the head stands at the viewpoint and looks in the measured direction. While
measuring, a Rigidbody on the moved object SHALL be kinematic and a CharacterController on it disabled; these are
Play Mode changes and SHALL NOT reach the scene on disk.

#### Scenario: Rig with a tracked head
- **WHEN** the head camera is 0.3 m off the rig root and turned 20° by tracking
- **THEN** after the move the head camera is at the viewpoint and faces the measured direction

### Requirement: Tour metrics
Per direction the tour SHALL average over the measured frames the frame time (from the unscaled delta time) with its
peak, the CPU main thread time, the GPU time (from the XR runtime when a headset is active, otherwise from frame
timing when the editor provides it, −1 when unknown), the dropped frames reported by the XR runtime, draw calls,
batches, SetPass calls and triangles. With the headset the frame is paced to 90 Hz, so the result of a viewpoint SHALL
be its worst direction by dropped frames, then by the larger of CPU and GPU time; without the headset — by frame time.
The tour SHALL record whether an XR device was active; without one it SHALL mark the tour "without headset".

#### Scenario: No headset
- **WHEN** the tour runs while no XR device is active
- **THEN** the results are kept and marked "without headset" in the table summary and the report

#### Scenario: Headroom under 90 Hz
- **WHEN** with the headset two directions both run at 11.1 ms per frame, one with GPU 6 ms and one with GPU 10.5 ms
- **THEN** the direction with GPU 10.5 ms is the worst

### Requirement: Tour results on the Viewpoints tab
The Viewpoints tab SHALL switch between "Estimate" and "Measured" with segments. "Measured" SHALL show the columns
frame time, peak, CPU, GPU, dropped frames, draw calls and SetPass, times coloured by the frame time thresholds, and a
summary with the tour date, whether the headset was active and how many viewpoints are slower than 11.1 ms (by the
larger of CPU and GPU time with the headset) or dropped frames. The selected viewpoint SHALL
show its estimated and measured draw calls and triangles side by side. The tab SHALL say that the editor measurement
is a guide and the final numbers come from the profiler on a Development Build.

#### Scenario: Slow place
- **WHEN** a viewpoint measured 12.4 ms
- **THEN** its frame time is shown in the Critical colour and it counts in "slower than 11.1 ms"
