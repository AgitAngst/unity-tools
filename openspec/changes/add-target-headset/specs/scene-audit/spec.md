# Spec Delta

## ADDED Requirements

### Requirement: Target headset
The Budgets tab SHALL offer a target headset (HTC Vive Pro, HTC Vive Pro 2, HTC Vive Pro 2 Wireless, Valve Index), its
refresh rate among the rates of the headset and the SteamVR render resolution in percent, and SHALL show the per-eye
render resolution, the frame time and the pixel rate against the Vive Pro. The frame time thresholds (Critical 1000 / rate
ms, Warning 90 % of it), the FPS colors, the physics step expected by the physics checks, the field of view of viewpoints
and the transparent share thresholds (scaled by the pixel rate) SHALL follow the headset; edited values SHALL be marked as
custom. A project without a budget file SHALL start with Vive Pro 2 Wireless at 90 Hz and 100 %; a budget file without a
headset SHALL keep HTC Vive Pro at 90 Hz. The context line and the report SHALL name the headset and its rate.

#### Scenario: 120 Hz
- **WHEN** the user picks HTC Vive Pro 2 at 120 Hz
- **THEN** the frame time thresholds are 7.5 / 8.3 ms and a fixed timestep of 1/90 s is reported as slower than the display

#### Scenario: Old budget file
- **WHEN** a budget file saved before 2.3.0 is loaded
- **THEN** the headset is HTC Vive Pro at 90 Hz and its thresholds are not marked as custom
