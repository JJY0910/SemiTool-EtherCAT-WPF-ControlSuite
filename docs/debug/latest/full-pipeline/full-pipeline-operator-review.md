# Full Pipeline Operator Review

This review summarizes the simulator-side 3D Machine Twin evidence captured under `docs/debug/latest/full-pipeline`. It does not claim real EtherCAT hardware commissioning.

## Result Summary

- PASS: Startup shows FOUP A 5/5 and FOUP B 0/5.
- PASS: W01-W05 each travel through `FOUP A -> Chamber A -> Chamber B -> Chamber C -> FOUP B`.
- PASS: Home / Start keeps the blade retracted; extension happens after station targeting and slot/work height selection.
- PASS: FOUP A slot state changes to Empty when a wafer is picked and the wafer appears on the blade.
- PASS: Chamber A/B/C captures include door-open, placement, hidden-in-chamber processing, and unload states.
- PASS: Final state shows W01-W05 placed in FOUP B B1-B5.
- LIMIT: The run is WPF simulator capture only. Real teaching values and physical EtherCAT synchronization were not changed or verified.

## Key Captures

- `full-pipeline-contact-sheet.png`: condensed overview of the full transfer.
- `screenshots/000-startup-simulator.png`: initial state, FOUP A full, FOUP B empty, blade retracted.
- `screenshots/001-move-to-foup-a-slot-a1.png`: robot targets FOUP A before blade extension.
- `screenshots/005-w01-on-blade-from-foup-a-slot-a1.png`: W01 picked from A1 and visible on the blade.
- `screenshots/014-w01-placed-at-chamber-a.png`: W01 placed into Chamber A.
- `screenshots/019-chamber-a-processing-w01.png`: Chamber A processing with the wafer hidden inside.
- `screenshots/038-w01-placed-at-chamber-b.png`: W01 placed into Chamber B.
- `screenshots/043-chamber-b-processing-w01.png`: Chamber B processing.
- `screenshots/081-w01-placed-at-chamber-c.png`: W01 placed into Chamber C.
- `screenshots/086-chamber-c-processing-w01.png`: Chamber C processing.
- `screenshots/103-w01-placed-at-foup-b-slot-b1.png`: W01 placed into FOUP B B1.
- `screenshots/306-w05-on-blade-from-foup-a-slot-a5.png`: W05 picked from FOUP A A5.
- `screenshots/428-w05-placed-at-foup-b-slot-b5.png`: W05 placed into FOUP B B5.
- `screenshots/431-sequence-complete-foup-b-5-5.png`: completed state, FOUP A 0/5 and FOUP B 5/5.
- `screenshots/432-reset-safe-state.png`: reset returns to safe simulator state.

## Wafer Movement Evidence

| Wafer | FOUP A Pick | Chamber A | Chamber B | Chamber C | FOUP B Place |
| --- | --- | --- | --- | --- | --- |
| W01 | `005-w01-on-blade-from-foup-a-slot-a1.png` | `014-w01-placed-at-chamber-a.png` | `038-w01-placed-at-chamber-b.png` | `081-w01-placed-at-chamber-c.png` | `103-w01-placed-at-foup-b-slot-b1.png` |
| W02 | `048-w02-on-blade-from-foup-a-slot-a2.png` | `057-w02-placed-at-chamber-a.png` | `124-w02-placed-at-chamber-b.png` | `167-w02-placed-at-chamber-c.png` | `189-w02-placed-at-foup-b-slot-b2.png` |
| W03 | `134-w03-on-blade-from-foup-a-slot-a3.png` | `143-w03-placed-at-chamber-a.png` | `210-w03-placed-at-chamber-b.png` | `253-w03-placed-at-chamber-c.png` | `275-w03-placed-at-foup-b-slot-b3.png` |
| W04 | `220-w04-on-blade-from-foup-a-slot-a4.png` | `229-w04-placed-at-chamber-a.png` | `296-w04-placed-at-chamber-b.png` | `339-w04-placed-at-chamber-c.png` | `361-w04-placed-at-foup-b-slot-b4.png` |
| W05 | `306-w05-on-blade-from-foup-a-slot-a5.png` | `315-w05-placed-at-chamber-a.png` | `382-w05-placed-at-chamber-b.png` | `406-w05-placed-at-chamber-c.png` | `428-w05-placed-at-foup-b-slot-b5.png` |

## Acceptance Criteria

- FOUP slots remain five levels and update one wafer at a time.
- Chamber doors face the robot blade direction and stay open during blade entry.
- Chamber wafers are tracked inside the chamber during processing without protruding outside the body.
- The blade does not extend from Home; it extends only after station angle and Z work height are selected.
- Preserved teaching values are not changed for visual verification.
