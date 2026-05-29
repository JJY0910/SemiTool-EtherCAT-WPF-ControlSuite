# Physical Equipment Model

## Summary

The actual school equipment should be described as a wafer transfer robot setup represented by a field-facing HMI sequence monitor.

`CMP Cluster` is kept as a previous-year HMI simulator scenario name. It is useful for recipe and flow explanation, but it is not used here as a claim that the physical wafer transfer setup is an official production CMP cluster tool.

## Visible Structure From Reference Understanding

The Digital Twin uses an abstract, sanitized model of the physical layout:

- transparent cover outline
- fixed aluminum base / lower frame
- central rotary theta-axis base
- blade/end-effector assembly mounted on the rotary base
- `CHAMBER_A` on the left side
- `CHAMBER_B` on the top side
- `CHAMBER_C` on the right side
- FOUP/source/destination cassette positions on the lower side
- tower lamp near the upper-right side

The repository includes one user-approved top-view context photo at `docs/images/real-equipment-context-top-view.jpg`.

The live Digital Twin is still an abstract HMI model, not a photo clone. It avoids using the photo as proof that the new WPF app has already completed real-hardware verification.

## Robot / Blade Mechanism

The transfer robot points the blade toward station targets using a central theta-axis swing.

The robot is not represented as an infinite or continuously spinning 360-degree robot. For the Digital Twin, FOUP A, Chamber A, Chamber B, Chamber C, and FOUP B are detents on a limited visual swing arc.

Z-axis movement handles Safe and Work height positions.

Cylinder forward/backward extends and retracts the blade or hand.

Vacuum suction holds the wafer on the blade/end-effector.

Vacuum exhaust releases the wafer into a chamber or FOUP slot.

The blade/end-effector is the wafer-carrying part. It is represented as a two-stage/telescopic slide-style mechanism mounted on the rotating theta base.

## Simulator Flow

The simulator/Digital Twin flow remains:

```text
FOUP A -> Chamber A -> Chamber B(CMP) -> Chamber C -> FOUP B
```

Chamber A is modeled as a pre-clean station.

Chamber B is modeled as the `CMP_Main` simulator station.

Chamber C is modeled as a post-clean and dry station.

## Boundary

This document describes simulator-mode Digital Twin understanding.

The original WinForms project controlled real EtherCAT hardware, but the new WPF app has not yet been verified on the physical equipment.

Actual real-hardware validation requires the local vendor DLL, EtherCAT connection, E-stop verification, wiring checks, and supervised commissioning.
