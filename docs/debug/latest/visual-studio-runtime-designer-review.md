# Visual Studio Runtime and Designer Review

## Scope

This review records a final local verification pass for the current stable
Machine Twin / Wafer Transfer Sequence Monitor state.

- Repository: `JJY0910/SemiTool-EtherCAT-WPF-ControlSuite`
- Base branch: `main`
- Current main commit: `0acbdb3918dbc56fded8ca34a41dc3531180083b`
- Review date: 2026-05-30
- Review type: runtime, capture, and Visual Studio designer-readability check

No application source code, runtime sequence logic, RealHardware adapter logic,
vendor DLL handling, preserved theta/detent/axis values, or real I/O mapping
semantics were changed for this review.

## Build and Test Result

The Windows-native Release build and tests were re-run from the current main
state.

```powershell
dotnet restore SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-restore
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-build --no-restore
```

Result:

- Restore: success
- Build: success, 0 warnings, 0 errors
- Tests: success, 99 passed / 0 failed / 0 skipped

## Capture Result

The simulator-only capture commands were re-run successfully.

```powershell
dotnet run --project src\SemiTool.Hmi.Wpf\SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-sequence-assets
dotnet run --project src\SemiTool.Hmi.Wpf\SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report
```

Result:

- Demo asset capture: success
- UI debug report capture: success
- Runtime screenshot dimensions checked at 1280x820 for representative
  Machine Twin captures.
- The debug report still documents the simulator-only boundary.
- The final captured sequence state remains FOUP B 5/5 completed and held
  until Reset.

Representative evidence paths:

- `docs/images/machine-twin-runtime.png`
- `docs/debug/latest/ui-runtime-verification.md`
- `docs/debug/latest/machine-twin-state-trace.json`
- `docs/debug/latest/screenshots/00-startup-simulator.png`
- `docs/debug/latest/screenshots/09-final-foup-b-5-completed.png`

## MainWindow Designer Preview Check

`src/SemiTool.Hmi.Wpf/MainWindow.xaml` was inspected for Visual Studio designer
support and runtime tab wiring.

Confirmed:

- `xmlns:d` is present.
- `xmlns:mc` is present.
- `mc:Ignorable="d"` is present.
- `d:DataContext` points to `DesignMainViewModel`.
- The first/default `TabControl` item is `Machine Twin`.
- The `Machine Twin` tab instantiates `views:MachineTwinView`.
- Existing tabs remain after the Machine Twin tab.

Expected designer preview:

- The shell should open directly to the Machine Twin tab.
- The Machine Twin surface should be visible without starting the runtime app.
- The preview uses design-time sample data only.

## MachineTwinView Designer Preview Check

`src/SemiTool.Hmi.Wpf/Views/MachineTwinView.xaml` and
`src/SemiTool.Hmi.Wpf/DesignTime/DesignMachineTwinViewModel.cs` were inspected
for design-time support.

Confirmed:

- `xmlns:d` is present.
- `xmlns:mc` is present.
- `mc:Ignorable="d"` is present.
- `d:DataContext` points to `DesignMachineTwinViewModel`.
- The former real-equipment context photo panel was removed from the runtime Machine Twin surface.
- FOUP A and FOUP B are bound to 5-slot cassette collections.
- Chamber A/B/C sample state is exposed through design-time chamber view
  models.
- The limited theta swing, blade, vacuum, Z state, tower lamp, operation strip,
  and event log bindings are present.
- Design-time sample data does not require the vendor DLL.
- Design-time sample data does not require a real hardware connection.
- Design-time sample data does not connect to RuntimeCoordinator hardware mode.

Expected MachineTwinView designer preview:

- Real equipment context photo panel.
- FOUP A 5-slot cassette with W05 waiting in the sample state.
- FOUP B 5-slot cassette with W01 completed in B1.
- Chamber A/B/C sample states with W04, W03, and W02 respectively.
- No wafer duplicated on the robot blade.
- Exactly five unique sample wafers across FOUP A, FOUP B, Chamber A/B/C, and
  the blade: W01, W02, W03, W04, W05.
- Limited theta swing arc.
- Telescopic blade.
- Z Safe/Work, cylinder, vacuum, tower lamp, and sample event log rows.

## Runtime Machine Twin Behavior Check

The current runtime binding and capture evidence show the field-HMI Wafer
Transfer Sequence Monitor behavior expected for the stable state.

Confirmed:

- Machine Twin is the first/default tab.
- Runtime screen includes the real-equipment context photo panel.
- Runtime screen includes the vector Machine Twin surface.
- FOUP A and FOUP B are represented as 5-slot cassette stacks.
- Chamber A/B/C cards expose process and door state.
- The robot uses limited theta swing visualization, not full 360-degree
  continuous rotation.
- The operation strip exposes Source / Wafer / Destination.
- Current action and event log bindings are present.
- The five-wafer pipeline starts with FOUP A 5/5 and FOUP B 0/5.
- W01-W05 move through Chamber A/B/C without duplication in reviewed evidence.
- Final state holds at FOUP B 5/5 completed.
- Normal runtime does not auto-close after sequence completion.

## Terminology and Safety Check

The requested terminology and safety commands were re-run.

```powershell
rg -n "Thread\.Sleep|DigitalOutput\(7|DigitalOutput\(8" src
rg -n "WriteDigitalOutputAsync\s*\(\s*\d|ReadDigitalInputAsync\s*\(\s*\d|DigitalOutput\s*\(\s*\d" src\SemiTool.Application src\SemiTool.Hmi.Wpf
rg -n "Application\.Current\.Shutdown|Environment\.Exit|Close\(" src\SemiTool.Hmi.Wpf
git ls-files | Select-String -Pattern "IEG3268_Dll.dll|\.dll$|\.exe$|\.pdb$|/bin/|/obj/|\.vs/"
git ls-files | Select-String -Pattern "2504110108_FinalTest.zip|_extracted_legacy_readonly|migration_inputs/original"
```

Result:

- No forbidden visible terminology was found.
- No `Thread.Sleep` regression was found.
- No raw DO/DI magic-number regression was found in HMI/Application paths.
- `window.Close()` appears only in the explicit capture helper path:
  `src\SemiTool.Hmi.Wpf\SequenceAssetCapture.cs`.
- No tracked vendor DLL, legacy ZIP, Excel/docx reference file, bin, obj, `.vs`,
  exe, or pdb artifact was found.

## Current Limitation

The actual physical equipment has not yet been re-verified with this new WPF
application. This review confirms simulator-mode runtime, capture evidence, and
designer-readability structure only. It does not claim new real equipment
validation.

## UI Polish Decision

No clear Visual Studio runtime, capture, or designer-readability defect was
found during this pass.

No UI polish PR is needed from this review unless a future manual review of the
actual app window at operator monitor scale finds a concrete readability issue.
