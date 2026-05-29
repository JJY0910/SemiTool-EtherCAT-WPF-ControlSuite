# Quality Gates

## Build Gate

The solution must build on Windows with WPF enabled.

```powershell
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-restore
```

## Test Gate

Unit tests must pass without real hardware.

```powershell
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-build --no-restore
```

## Safety Audit Gate

No generated binaries, private machine files, vendor DLLs, or legacy binary inputs should be tracked.

```powershell
git ls-files | Select-String -Pattern "IEG3268_Dll.dll|\.dll$|\.exe$|\.pdb$|/bin/|/obj/|\.vs/"
git ls-files | Select-String -Pattern "2504110108_FinalTest.zip|_extracted_legacy_readonly|migration_inputs/original"
```

## No Vendor DLL Gate

The public repository must not include `IEG3268_Dll.dll` or other vendor DLLs. Local vendor files belong under `libs/` and are ignored by git except for `libs/README.md`.

## No Raw Magic-Number DO/DI Usage Gate

HMI and application logic should call named `IoPoint` values through services and `IEthercatController`, not raw channel integers.

```powershell
rg -n "WriteDigitalOutputAsync\s*\(\s*\d|ReadDigitalInputAsync\s*\(\s*\d|DigitalOutput\s*\(\s*\d" src\SemiTool.Application src\SemiTool.Hmi.Wpf
```

## No Thread.Sleep Gate

WPF, HMI, and application service code must use async delays with cancellation instead of `Thread.Sleep`.

```powershell
rg -n "Thread\.Sleep|DigitalOutput\(7|DigitalOutput\(8" src
```

## GitHub Actions Gate

The `.NET CI` workflow must run on `main` and pass on Windows.

```powershell
gh run list --repo JJY0910/SemiTool-EtherCAT-WPF-ControlSuite --workflow ".NET CI" --limit 5
```

## Real Hardware Commissioning Gate

Real hardware verification is separate from simulator verification. Use the real hardware commissioning issue template before connecting to the physical tool.

- Confirm E-stop path.
- Confirm vendor DLL path.
- Confirm no motion or output auto-start behavior.
- Connect manually.
- Verify motion, I/O, actuators, short auto sequence, and alarm/reset recovery under supervision.
