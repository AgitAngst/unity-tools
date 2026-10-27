# Spec Delta

## MODIFIED Requirements

### Requirement: Tour metrics
Per direction the tour SHALL average over the measured frames the frame time (from the unscaled delta time) with its
peak, the CPU main thread time, the GPU time (from the XR runtime when a headset is active, otherwise from frame
timing when the editor provides it, −1 when unknown), the dropped frames reported by the XR runtime, draw calls,
batches, SetPass calls and triangles. With the headset the frame is paced to the refresh rate of the headset, so the result of a viewpoint SHALL
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
summary with the tour date, whether the headset was active and how many viewpoints are slower than the Critical frame time (11.1 ms at 90 Hz) (by the
larger of CPU and GPU time with the headset) or dropped frames. The selected viewpoint SHALL
show its estimated and measured draw calls and triangles side by side. The tab SHALL say that the editor measurement
is a guide and the final numbers come from the profiler on a Development Build.

#### Scenario: Slow place
- **WHEN** a viewpoint measured 12.4 ms
- **THEN** its frame time is shown in the Critical colour and it counts in "slower than 11.1 ms"
