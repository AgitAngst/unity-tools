# Spec Delta

## ADDED Requirements

### Requirement: Viewpoint tour in the history
When a viewpoint tour newer than the previous history point exists, a new history point SHALL store the worst
measured frame time, the number of viewpoints slower than 11.1 ms and whether the headset was active, and the History
tab SHALL chart the worst frame time.

#### Scenario: Faster after fixes
- **WHEN** the worst measured frame time was 12.4 ms at one point and 10.2 ms at the next
- **THEN** the History tab shows the drop of 2.2 ms in green
