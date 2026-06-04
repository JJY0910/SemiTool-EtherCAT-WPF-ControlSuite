# Visual QA - Wafer Transfer Sequence Monitor

## Scope

This report reviews the latest simulator-only Machine Twin / Wafer Transfer Sequence Monitor captures from `main`.

- Repository HEAD reviewed: `33eda3ee44d5f8217554728ba668d0d7810e4556`
- Real hardware validation: not performed and not claimed
- Hardware behavior scope: RealHardware adapter, vendor DLL handling, preserved theta/detent/axis values, and I/O mapping semantics were not changed
- Capture commands:
  - `dotnet run --project src\SemiTool.Hmi.Wpf\SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-sequence-assets`
  - `dotnet run --project src\SemiTool.Hmi.Wpf\SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report`

## Validation Summary

- Build result: succeeded with 0 errors
- Test result: passed, 99 passed / 0 failed
- Capture result: both capture commands succeeded
- Terminology result: no `Teaching`, `Teaching Demo`, `정비교육`, or `교육용` matches
- Safety result: no Thread.Sleep, raw HMI/Application DO/DI magic-number regression, unsafe tracked DLL/EXE/PDB/bin/obj/.vs artifact, legacy ZIP, Excel, or DOCX reference file was found
- Hardware boundary: this QA pass did not perform or claim new real-hardware validation

## Summary Result

No visual QA blocker was found. The generated screenshots show a field-HMI style Wafer Transfer Sequence Monitor with readable sequence terminology, visible operation fields, visible actuator/chamber states, FOUP A/B 5-slot cassettes, and a clear final FOUP B 5/5 completed state.

The trace was also checked for the five-wafer invariant. For every reviewed runtime screenshot, W01-W05 appear exactly once across FOUP A, FOUP B, Chamber A/B/C, and the blade.

## Required State Coverage

| Required state | Capture evidence | Result |
|---|---|---|
| Initial FOUP A 5/5 waiting | `docs/debug/latest/screenshots/00-startup-simulator.png`, `01-foup-a-before-pickup.png` | Pass |
| W01 picked from FOUP A | `02-blade-holding-wafer-after-pickup.png` | Pass |
| Blade holding W01 | `02-blade-holding-wafer-after-pickup.png`, `04-blade-entering-chamber-a-door-open.png` | Pass |
| Chamber A door opening/open | `03-chamber-a-door-opening.png`, `04-blade-entering-chamber-a-door-open.png` | Pass |
| Blade entering Chamber A only after door open | `04-blade-entering-chamber-a-door-open.png` | Pass |
| W01 placed on Chamber A stage | `05-wafer-placed-chamber-a-stage.png` | Pass |
| Blade retracted before Chamber A door close | `06-blade-retracted-before-chamber-a-door-closes.png` | Pass |
| Chamber A processing with door closed | `07-chamber-a-processing-door-closed.png` | Pass |
| Chamber A unload after process complete | `08-chamber-a-unload-after-process-complete.png` | Pass |
| Final FOUP B 5/5 completed and held | `09-final-foup-b-5-completed.png` | Pass |

## Runtime Debug Screenshot QA

| Capture | Dimensions | Sequence state shown | Current action readable | Source / Wafer / Destination readable | Chamber door state visible | Blade state visible | Vacuum state visible | FOUP A/B 5-slot state visible | W01-W05 duplicated | Final FOUP B 5/5 clear |
|---|---:|---|---|---|---|---|---|---|---|---|
| `docs/debug/latest/screenshots/00-startup-simulator.png` | 1280x820 | Startup simulator / FOUP A loaded | Yes | Yes | Yes, all closed | Yes, retracted | Yes, off | Yes, FOUP A 5/5 and FOUP B 0/5 | No | N/A |
| `docs/debug/latest/screenshots/01-foup-a-before-pickup.png` | 1280x820 | Move to FOUP A Slot A1 | Yes | Yes, W01 / FOUP A Slot A1 / Chamber A | Yes, all closed | Yes, retracted | Yes, off | Yes, A1-A5 loaded and B1-B5 empty | No | N/A |
| `docs/debug/latest/screenshots/02-blade-holding-wafer-after-pickup.png` | 1280x820 | W01 picked to blade | Yes | Yes, W01 / FOUP A Slot A1 / Chamber A | Yes, all closed | Yes, retracted with wafer visual | Yes, suction on | Yes, FOUP A decremented and FOUP B empty | No | N/A |
| `docs/debug/latest/screenshots/03-chamber-a-door-opening.png` | 1280x820 | Chamber A door opening | Yes | Yes, W01 / FOUP A Slot A1 / Chamber A | Yes, Chamber A opening is called out | Yes | Yes, suction on | Yes | No | N/A |
| `docs/debug/latest/screenshots/04-blade-entering-chamber-a-door-open.png` | 1280x820 | Blade entering Chamber A after door open | Yes | Yes, W01 / FOUP A Slot A1 / Chamber A | Yes, Chamber A open | Yes, extending/extended state is visible | Yes, suction on | Yes | No | N/A |
| `docs/debug/latest/screenshots/05-wafer-placed-chamber-a-stage.png` | 1280x820 | W01 placed on Chamber A stage | Yes | Yes, W01 / FOUP A Slot A1 / Chamber A | Yes, Chamber A open | Yes, blade extended | Yes, release/exhaust | Yes | No | N/A |
| `docs/debug/latest/screenshots/06-blade-retracted-before-chamber-a-door-closes.png` | 1280x820 | Blade retracted before door close | Yes | Yes, W01 / Chamber A / Chamber B | Yes, Chamber A open before close | Yes, retracting/retracted state visible | Yes, off | Yes | No | N/A |
| `docs/debug/latest/screenshots/07-chamber-a-processing-door-closed.png` | 1280x820 | Chamber A processing W01 | Yes | Yes, W01 / Chamber A / Chamber B | Yes, Chamber A closed | Yes, retracted | Yes, off | Yes | No | N/A |
| `docs/debug/latest/screenshots/08-chamber-a-unload-after-process-complete.png` | 1280x820 | Chamber A unload after process complete | Yes | Yes, W01 / Chamber A / Chamber B | Yes, Chamber A open | Yes, blade entering chamber | Yes, suction on | Yes | No | N/A |
| `docs/debug/latest/screenshots/09-final-foup-b-5-completed.png` | 1280x820 | Sequence Complete - FOUP B 5/5 | Yes | Yes, W05 / FOUP B / complete | Yes, all closed | Yes, retracted | Yes, off | Yes, FOUP A 0/5 and FOUP B 5/5 | No | Yes |
| `docs/debug/latest/screenshots/10-reset-safe-state.png` | 1280x820 | Explicit reset safe state | Yes | Yes | Yes, all closed | Yes, retracted | Yes, off | Yes, FOUP A restored and FOUP B empty | No | N/A |

## Portfolio Asset QA

| Capture | Dimensions | Visual QA note |
|---|---:|---|
| `docs/images/machine-twin-runtime.png` | 1280x820 | Runtime Machine Twin shell is visible with the sequence monitor, operation strip, FOUP slots, actuator cards, and simulator boundary labels. |
| `docs/images/digital-twin-limited-theta-swing.png` | 1280x820 | Limited theta station arc and station markers are visible; it does not read as a free 360-degree spinner. |
| `docs/images/digital-twin-wafer-transfer-robot.png` | 1280x820 | Wafer transfer robot arrangement is visible with Chamber A/B/C, FOUP A/B, central theta base, and blade. |
| `docs/images/digital-twin-blade-mechanism.png` | 1280x820 | Blade/end-effector state is visible and paired with actuator/vacuum state. |
| `docs/images/dashboard.png` | 1280x820 | Dashboard remains a secondary overview; the Machine Twin runtime screen is the main sequence monitor. |
| `docs/images/auto-sequence.png` | 1280x820 | Auto sequence view remains readable as a supporting HMI page. |
| `docs/images/wafer-flow.png` | 1280x820 | Wafer/recipe flow remains readable as a supporting process view. |
| `docs/images/alarm-log.png` | 1280x820 | Alarm/event page remains readable; no visual blocker found. |

## Trace Invariant Check

`docs/debug/latest/machine-twin-state-trace.json` was checked against the runtime screenshot milestones. Each reviewed entry contained exactly W01-W05 once across:

- FOUP A slots
- FOUP B slots
- Chamber A/B/C
- Robot blade

No duplicate wafer ID was found in the reviewed capture set.

## Visual Defects

No clear UI defect requiring a code change was found in this QA pass.

Minor observation: some dense labels in the right-side Motion / Actuator State panel are compact, but they remain readable at the generated 1280x820 capture resolution and do not block the required sequence understanding.

## Decision

No visual-polish code change was needed. This is a documentation/evidence-only QA baseline for the latest Wafer Transfer Sequence Monitor captures.
