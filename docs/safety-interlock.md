# Safety Interlock

## Startup Defaults

- Simulator mode
- Disconnected
- Auto stopped
- No motion
- No output activation

## Real Hardware Entry

Real Hardware mode requires:

- User selects `RealHardware`
- User sets vendor DLL path
- User checks hardware unlock
- User clicks Apply
- User clicks Connect

## Blocking Rules

- Manual commands are blocked while Auto is running.
- Auto Start is blocked if disconnected.
- Auto Start is blocked until Z and Theta homing are marked complete.
- Cylinder forward waits for `CylinderFrontSensor`.
- Cylinder backward waits for `CylinderRearSensor`.
- Door operations wait for the matching preserved door sensor.

## Fault Response

On emergency stop, fatal sequence failure, timeout, disconnect, or communication failure:

- Stop motion where possible.
- Turn off risky outputs where possible.
- Set machine state to Alarm or Emergency.
- Record alarm and event log entries.
- Require Reset before restart.
