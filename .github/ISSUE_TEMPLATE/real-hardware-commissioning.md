---
name: Real hardware commissioning
about: Checklist for controlled real equipment verification
title: "Real hardware commissioning checklist"
labels: ["hardware", "commissioning", "safety"]
assignees: ""
---

# Real Hardware Commissioning Checklist

> Real hardware mode can move axes and actuate outputs. Perform this checklist only with verified wiring, E-stop, and operator supervision.

## Pre-check

- [ ] Confirm E-stop path
- [ ] Confirm machine area is clear
- [ ] Confirm operator supervision
- [ ] Confirm vendor DLL path
- [ ] Confirm EtherCAT controller power
- [ ] Confirm no axis auto-motion on startup
- [ ] Confirm Simulator mode still works

## Connection

- [ ] Select Real Hardware mode manually
- [ ] Connect only
- [ ] Confirm connection status
- [ ] Confirm no unexpected output turns on

## Motion

- [ ] Servo ON
- [ ] Home Z
- [ ] Home Theta
- [ ] Move Z small test
- [ ] Move Theta small test

## I/O and actuator tests

- [ ] DO channel test
- [ ] DI sensor test
- [ ] Cylinder forward/backward test
- [ ] Vacuum suction/exhaust test
- [ ] Chamber door open/close test

## Sequence and recovery

- [ ] Short auto sequence
- [ ] Alarm/reset recovery
- [ ] Record test video
- [ ] Document failures and fixes

## Notes

Record machine state, operator, date/time, vendor DLL version, and any deviations from expected behavior.
