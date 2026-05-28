# Migration Analysis

## Extracted From Legacy

- EtherCAT connection/disconnection concept
- IEG3268 vendor API names and call order
- Servo ON/OFF, Z homing, Theta homing
- Z/Theta absolute move behavior
- Digital output and input concepts
- Cylinder, vacuum, chamber door, chamber lamp, tower lamp control
- FOUP A -> PM A -> PM B -> PM C -> FOUP B transfer flow
- Recipe/process countdown concepts

## Preserved

- DO0-DO15 canonical output map
- DI0-DI5 and DI12-DI13 canonical input map
- Home, FOUP A/B, Chamber A/B/C robot poses
- FOUP slot Z safe/work values
- Motion, stabilization, door, cylinder, vacuum, status, and auto tick timing values
- Scheduler priority:
  1. PM C -> FOUP B
  2. PM B -> PM C
  3. PM A -> PM B
  4. FOUP A -> PM A
  5. Process countdown tick

## Improved

- New WPF/MVVM architecture instead of WinForms event handlers
- Simulator-first startup with no automatic hardware connection
- Real hardware adapter isolated behind `IEthercatController`
- No vendor DLL reference in the public build
- Named `IoPoint` usage instead of application-level raw DO/DI numbers
- Async sequence services with cancellation and timeout handling
- Alarm/event logging and explicit safety state
- Unit tests for preserved values and scheduler behavior

## Known Legacy Risks

The legacy project had direct `DigitalOutput` calls with comments that conflicted with the canonical map. For example, some direct DO7/DO8 calls were commented as lamps, but the canonical map defines:

```text
DO7 = Chamber B Door Close
DO8 = Chamber B Door Open
```

The WPF project treats those direct calls as legacy defects. Canonical profile values and named `IoPoint` mappings win.
