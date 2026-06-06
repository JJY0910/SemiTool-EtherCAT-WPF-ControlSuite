# Threat Model

## Scope

This document covers risks in the WPF HMI, simulator, real EtherCAT adapter boundary, configuration profile, and public repository workflow.

## Assets

- operator safety
- preserved equipment teaching values
- EtherCAT output control
- real hardware unlock and connection path
- vendor DLL loading boundary
- simulator verification evidence
- repository integrity and CI signal

## Primary Threats

| Threat | Risk | Current control |
| --- | --- | --- |
| Startup actuates hardware | Unexpected motion or output activation | Simulator default, no auto-connect, no auto-run, startup safety tests |
| Raw DO/DI channel use | Wrong output or input mapped in code | Named `IoPoint` usage and tests scanning application logic |
| Vendor DLL loaded in the wrong path | Public build or simulator path depends on machine-local DLL | DLL usage isolated inside `Ieg3268EthercatController` |
| Preserved values edited casually | UI no longer matches approved equipment profile | Profile preservation tests and contributor rules |
| Simulator evidence overstated | Reviewers believe real hardware was commissioned | README, security, and QA docs state simulator-only boundary |
| Unsafe PR merges | Safety checks skipped | CI, PR template, commissioning issue template |
| Secrets or local DLLs committed | Private machine data or vendor assets exposed | `.gitignore`, contribution rules, manual staging review |

## Review Triggers

Require extra review when a change touches:

- `config/EquipmentProfile.finaltest.json`
- `Ieg3268EthercatController`
- `SelectableEthercatController`
- `EquipmentSequenceService`
- `RuntimeCoordinator`
- output writes, door/cylinder/vacuum logic, servo or motion commands
- startup code in `App.xaml.cs`

## Residual Risks

- Simulator tests cannot prove physical wiring, sensor polarity, or pneumatic behavior.
- Public CI cannot load a local vendor DLL or connect to school equipment.
- Visual screenshots can prove WPF state rendering, not real EtherCAT synchronization.

Real hardware verification should be recorded through the real-hardware commissioning checklist with operator, equipment state, date/time, and deviations.
