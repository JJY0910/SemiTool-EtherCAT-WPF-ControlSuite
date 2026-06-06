# Roadmap

This roadmap is intentionally conservative because the project can connect to real EtherCAT hardware when unlocked by an operator.

## Completed

- WPF/MVVM rebuild separated from the legacy WinForms code path.
- Simulator-first startup with no automatic hardware connection.
- Named I/O points and adapter boundary for hardware access.
- Native WPF 3D Machine Twin for the wafer-transfer trainer layout.
- Five-wafer simulator pipeline from FOUP A through Chamber A/B/C to FOUP B.
- Preservation tests for approved equipment profile values.
- Runtime screenshot capture for Machine Twin and full-pipeline evidence.

## Current Focus

- Keep public documentation aligned with the current 3D runtime.
- Strengthen CI and safety regression tests.
- Maintain a clear boundary between simulator evidence and real-hardware commissioning.
- Improve GitHub issue and PR intake so future work can be reviewed safely.

## Next Candidates

- Add more Machine Twin visual assertions around tower lamp, chamber lamp, and wafer hiding states.
- Add a small operator status timeline export for sequence troubleshooting.
- Add explicit commissioning logs once supervised equipment access is available.
- Add optional 3D model asset import only if it does not weaken startup safety or profile preservation.

## Out Of Scope Without Equipment Access

- Claiming real-hardware validation.
- Changing preserved teaching values.
- Enabling any startup motion or automatic output activation.
- Treating simulator capture evidence as commissioning evidence.
