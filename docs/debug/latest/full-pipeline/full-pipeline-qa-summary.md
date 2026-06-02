# Full Pipeline QA Summary

- Capture command: `dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-full-pipeline-qa`
- Verification boundary: Simulator-mode WPF render capture only. No real EtherCAT hardware connection is attempted.
- Total sequence steps checked: 63
- Screenshots captured: 63
- Final FOUP A count: 0/5
- Final FOUP B count: 5/5
- Final completed count: 5/5

## Pass Criteria

- FOUP A starts at 5/5 and drains to 0/5.
- FOUP B starts at 0/5 and fills to 5/5.
- W01-W05 each pass FOUP A, Chamber A, Chamber B, Chamber C, and FOUP B in order.
- Home / Start captures remain blade-retracted; extension captures occur after station targeting.
- Chamber captures include placed and processing frames for A/B/C.

## Wafer Movement Evidence

| Wafer | FOUP A Pick | Chamber A | Chamber B | Chamber C | FOUP B Place |
| --- | --- | --- | --- | --- | --- |
| W01 | [005-w01-on-blade-from-foup-a-slot-a1.png](screenshots/005-w01-on-blade-from-foup-a-slot-a1.png) | [014-w01-placed-at-chamber-a.png](screenshots/014-w01-placed-at-chamber-a.png) | [038-w01-placed-at-chamber-b.png](screenshots/038-w01-placed-at-chamber-b.png) | [081-w01-placed-at-chamber-c.png](screenshots/081-w01-placed-at-chamber-c.png) | [103-w01-placed-at-foup-b-slot-b1.png](screenshots/103-w01-placed-at-foup-b-slot-b1.png) |
| W02 | [048-w02-on-blade-from-foup-a-slot-a2.png](screenshots/048-w02-on-blade-from-foup-a-slot-a2.png) | [057-w02-placed-at-chamber-a.png](screenshots/057-w02-placed-at-chamber-a.png) | [124-w02-placed-at-chamber-b.png](screenshots/124-w02-placed-at-chamber-b.png) | [167-w02-placed-at-chamber-c.png](screenshots/167-w02-placed-at-chamber-c.png) | [189-w02-placed-at-foup-b-slot-b2.png](screenshots/189-w02-placed-at-foup-b-slot-b2.png) |
| W03 | [134-w03-on-blade-from-foup-a-slot-a3.png](screenshots/134-w03-on-blade-from-foup-a-slot-a3.png) | [143-w03-placed-at-chamber-a.png](screenshots/143-w03-placed-at-chamber-a.png) | [210-w03-placed-at-chamber-b.png](screenshots/210-w03-placed-at-chamber-b.png) | [253-w03-placed-at-chamber-c.png](screenshots/253-w03-placed-at-chamber-c.png) | [275-w03-placed-at-foup-b-slot-b3.png](screenshots/275-w03-placed-at-foup-b-slot-b3.png) |
| W04 | [220-w04-on-blade-from-foup-a-slot-a4.png](screenshots/220-w04-on-blade-from-foup-a-slot-a4.png) | [229-w04-placed-at-chamber-a.png](screenshots/229-w04-placed-at-chamber-a.png) | [296-w04-placed-at-chamber-b.png](screenshots/296-w04-placed-at-chamber-b.png) | [339-w04-placed-at-chamber-c.png](screenshots/339-w04-placed-at-chamber-c.png) | [361-w04-placed-at-foup-b-slot-b4.png](screenshots/361-w04-placed-at-foup-b-slot-b4.png) |
| W05 | [306-w05-on-blade-from-foup-a-slot-a5.png](screenshots/306-w05-on-blade-from-foup-a-slot-a5.png) | [315-w05-placed-at-chamber-a.png](screenshots/315-w05-placed-at-chamber-a.png) | [382-w05-placed-at-chamber-b.png](screenshots/382-w05-placed-at-chamber-b.png) | [406-w05-placed-at-chamber-c.png](screenshots/406-w05-placed-at-chamber-c.png) | [428-w05-placed-at-foup-b-slot-b5.png](screenshots/428-w05-placed-at-foup-b-slot-b5.png) |

## Captured Screenshot Files

- `docs/debug/latest/full-pipeline/screenshots/000-startup-simulator.png`
- `docs/debug/latest/full-pipeline/screenshots/001-move-to-foup-a-slot-a1.png`
- `docs/debug/latest/full-pipeline/screenshots/005-w01-on-blade-from-foup-a-slot-a1.png`
- `docs/debug/latest/full-pipeline/screenshots/012-blade-entering-chamber-a.png`
- `docs/debug/latest/full-pipeline/screenshots/014-w01-placed-at-chamber-a.png`
- `docs/debug/latest/full-pipeline/screenshots/019-chamber-a-processing-w01.png`
- `docs/debug/latest/full-pipeline/screenshots/036-blade-entering-chamber-b.png`
- `docs/debug/latest/full-pipeline/screenshots/038-w01-placed-at-chamber-b.png`
- `docs/debug/latest/full-pipeline/screenshots/043-chamber-b-processing-w01.png`
- `docs/debug/latest/full-pipeline/screenshots/044-move-to-foup-a-slot-a2.png`
- `docs/debug/latest/full-pipeline/screenshots/048-w02-on-blade-from-foup-a-slot-a2.png`
- `docs/debug/latest/full-pipeline/screenshots/055-blade-entering-chamber-a.png`
- `docs/debug/latest/full-pipeline/screenshots/057-w02-placed-at-chamber-a.png`
- `docs/debug/latest/full-pipeline/screenshots/062-chamber-a-processing-w02.png`
- `docs/debug/latest/full-pipeline/screenshots/079-blade-entering-chamber-c.png`
- `docs/debug/latest/full-pipeline/screenshots/081-w01-placed-at-chamber-c.png`
- `docs/debug/latest/full-pipeline/screenshots/086-chamber-c-processing-w01.png`
- `docs/debug/latest/full-pipeline/screenshots/103-w01-placed-at-foup-b-slot-b1.png`
- `docs/debug/latest/full-pipeline/screenshots/122-blade-entering-chamber-b.png`
- `docs/debug/latest/full-pipeline/screenshots/124-w02-placed-at-chamber-b.png`
- `docs/debug/latest/full-pipeline/screenshots/129-chamber-b-processing-w02.png`
- `docs/debug/latest/full-pipeline/screenshots/130-move-to-foup-a-slot-a3.png`
- `docs/debug/latest/full-pipeline/screenshots/134-w03-on-blade-from-foup-a-slot-a3.png`
- `docs/debug/latest/full-pipeline/screenshots/141-blade-entering-chamber-a.png`
- `docs/debug/latest/full-pipeline/screenshots/143-w03-placed-at-chamber-a.png`
- `docs/debug/latest/full-pipeline/screenshots/148-chamber-a-processing-w03.png`
- `docs/debug/latest/full-pipeline/screenshots/165-blade-entering-chamber-c.png`
- `docs/debug/latest/full-pipeline/screenshots/167-w02-placed-at-chamber-c.png`
- `docs/debug/latest/full-pipeline/screenshots/172-chamber-c-processing-w02.png`
- `docs/debug/latest/full-pipeline/screenshots/189-w02-placed-at-foup-b-slot-b2.png`
- `docs/debug/latest/full-pipeline/screenshots/208-blade-entering-chamber-b.png`
- `docs/debug/latest/full-pipeline/screenshots/210-w03-placed-at-chamber-b.png`
- `docs/debug/latest/full-pipeline/screenshots/215-chamber-b-processing-w03.png`
- `docs/debug/latest/full-pipeline/screenshots/216-move-to-foup-a-slot-a4.png`
- `docs/debug/latest/full-pipeline/screenshots/220-w04-on-blade-from-foup-a-slot-a4.png`
- `docs/debug/latest/full-pipeline/screenshots/227-blade-entering-chamber-a.png`
- `docs/debug/latest/full-pipeline/screenshots/229-w04-placed-at-chamber-a.png`
- `docs/debug/latest/full-pipeline/screenshots/234-chamber-a-processing-w04.png`
- `docs/debug/latest/full-pipeline/screenshots/251-blade-entering-chamber-c.png`
- `docs/debug/latest/full-pipeline/screenshots/253-w03-placed-at-chamber-c.png`
- `docs/debug/latest/full-pipeline/screenshots/258-chamber-c-processing-w03.png`
- `docs/debug/latest/full-pipeline/screenshots/275-w03-placed-at-foup-b-slot-b3.png`
- `docs/debug/latest/full-pipeline/screenshots/294-blade-entering-chamber-b.png`
- `docs/debug/latest/full-pipeline/screenshots/296-w04-placed-at-chamber-b.png`
- `docs/debug/latest/full-pipeline/screenshots/301-chamber-b-processing-w04.png`
- `docs/debug/latest/full-pipeline/screenshots/302-move-to-foup-a-slot-a5.png`
- `docs/debug/latest/full-pipeline/screenshots/306-w05-on-blade-from-foup-a-slot-a5.png`
- `docs/debug/latest/full-pipeline/screenshots/313-blade-entering-chamber-a.png`
- `docs/debug/latest/full-pipeline/screenshots/315-w05-placed-at-chamber-a.png`
- `docs/debug/latest/full-pipeline/screenshots/320-chamber-a-processing-w05.png`
- `docs/debug/latest/full-pipeline/screenshots/337-blade-entering-chamber-c.png`
- `docs/debug/latest/full-pipeline/screenshots/339-w04-placed-at-chamber-c.png`
- `docs/debug/latest/full-pipeline/screenshots/344-chamber-c-processing-w04.png`
- `docs/debug/latest/full-pipeline/screenshots/361-w04-placed-at-foup-b-slot-b4.png`
- `docs/debug/latest/full-pipeline/screenshots/380-blade-entering-chamber-b.png`
- `docs/debug/latest/full-pipeline/screenshots/382-w05-placed-at-chamber-b.png`
- `docs/debug/latest/full-pipeline/screenshots/387-chamber-b-processing-w05.png`
- `docs/debug/latest/full-pipeline/screenshots/404-blade-entering-chamber-c.png`
- `docs/debug/latest/full-pipeline/screenshots/406-w05-placed-at-chamber-c.png`
- `docs/debug/latest/full-pipeline/screenshots/411-chamber-c-processing-w05.png`
- `docs/debug/latest/full-pipeline/screenshots/428-w05-placed-at-foup-b-slot-b5.png`
- `docs/debug/latest/full-pipeline/screenshots/431-sequence-complete-foup-b-5-5.png`
- `docs/debug/latest/full-pipeline/screenshots/432-reset-safe-state.png`
