# Theta Limited Swing Model

## Summary

The wafer transfer robot is not modeled as a free 360-degree continuously rotating robot.

The physical understanding is a central theta-axis base with a limited swing motion across station targets. The useful visual range is treated as roughly 300 degrees for HMI explanation.

## Station Targets

The Digital Twin station order is:

```text
FOUP A -> Chamber A -> Chamber B -> Chamber C -> FOUP B
```

These are detents on the visual swing arc, not arbitrary positions on a full circular dial.

## Preserved Theta Encoder Values

The preserved theta values remain unchanged:

```text
FOUP A   14140
Chamber A -59064
Chamber B -190823
Chamber C -322000
FOUP B   -394293
```

These numbers are encoder/position values from the equipment profile. They are not literal UI degree values.

## Digital Twin Mapping

The Digital Twin maps the preserved encoder targets to a readable limited visual arc.

The visual arc is display metadata only. It does not change motion commands, recipes, I/O mappings, timing constants, or `config/EquipmentProfile.finaltest.json`.

## Mechanical Boundary

The central theta base aims the telescopic blade/end-effector toward each station.

The cable/blade/mechanical structure should be treated as limited rotation, not infinite rotation.

The simulator visuals explain the intended station-to-station behavior. The new WPF app has not yet been verified on the real physical equipment.
