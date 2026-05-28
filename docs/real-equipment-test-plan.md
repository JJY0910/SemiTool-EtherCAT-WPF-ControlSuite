# Real Equipment Test Plan

Run these checks only with the correct machine, safety approval, and E-stop supervision.

1. Run simulator.
2. Verify Simulator mode starts disconnected and all outputs are off.
3. Select Real Hardware mode in Settings.
4. Set `libs/IEG3268_Dll.dll` path or another local vendor DLL path.
5. Check hardware unlock and click Apply.
6. Click Connect.
7. Servo ON.
8. Home Z.
9. Home Theta.
10. Move Z absolute with a safe small target.
11. Move Theta absolute with a safe small target.
12. Toggle tower lamp DO ON/OFF.
13. Verify DI monitor against physical sensor changes.
14. Cylinder forward and verify DI13.
15. Cylinder backward and verify DI12.
16. Vacuum suction and exhaust checks.
17. Chamber A/B/C door open/close checks and sensor feedback.
18. Run a short auto sequence with one wafer path.
19. Trigger and clear an alarm/reset scenario.
20. Disconnect and verify risky outputs are off.
