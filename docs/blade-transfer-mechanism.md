# Blade and Wafer Transfer Mechanism

## Components

- central theta-axis swing base
- Z axis
- blade/end-effector
- cylinder forward/backward
- vacuum suction/exhaust
- wafer on blade
- FOUP slots
- chamber positions

## Pick Sequence

1. Swing theta toward the source station.
2. Move Z to Safe.
3. Move Z to Work.
4. Cylinder Forward extends the blade.
5. Vacuum Suction turns ON.
6. The wafer is held on the blade/end-effector.
7. Move Z back to Safe.
8. Cylinder Backward retracts the blade.

## Place Sequence

1. Swing theta toward the target station.
2. Open the chamber door if the target is a chamber.
3. Move Z to Work.
4. Cylinder Forward extends the blade.
5. Vacuum Exhaust releases the wafer.
6. The wafer moves from the blade to the chamber or FOUP slot.
7. Cylinder Backward retracts the blade.
8. Move Z to Safe.
9. Close the chamber door if needed.

## Safety Notes

- never auto-connect real hardware
- never auto-move axes on startup
- hardware unlock is required before Real Hardware Connect
- simulator mode is safe without the vendor DLL
- real hardware mode requires E-stop, wiring checks, and operator supervision

## Digital Twin Notes

The blade is represented as a two-stage/telescopic assembly because the visible hand has a fixed lower/base section and an extending front blade section.

The blade orientation follows limited theta station targets:

```text
FOUP A -> Chamber A -> Chamber B -> Chamber C -> FOUP B
```

The visual is simulator-mode explanatory material and does not claim that the new WPF app has already been verified on the physical machine.
