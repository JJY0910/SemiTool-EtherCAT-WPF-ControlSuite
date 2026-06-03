using System.IO;
using IoPath = System.IO.Path;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using SemiTool.Application;
using SemiTool.Domain;
using SemiTool.Hmi.Wpf.ViewModels;
using SemiTool.Hmi.Wpf.Views;

namespace SemiTool.Hmi.Wpf;

internal static class SequenceAssetCapture
{
    private const int CaptureWidth = 1536;
    private const int CaptureHeight = 864;
    private const int CapturePixelWidth = 1920;
    private const int CapturePixelHeight = 1080;
    private const double Dpi = 120;

    public static async Task CaptureAsync(RuntimeCoordinator runtime, MainViewModel viewModel)
    {
        var outputDirectory = IoPath.Combine(FindRepositoryRoot(), "docs", "images");
        Directory.CreateDirectory(outputDirectory);

        await PrepareSimulatorStateAsync(runtime);
        await viewModel.RefreshAsync();
        await RenderMainWindowAsync(viewModel, IoPath.Combine(outputDirectory, "machine-twin-runtime.png"));
        await RenderAsync(new DashboardView { DataContext = viewModel.Dashboard }, "Dashboard", "Simulator mode overview", IoPath.Combine(outputDirectory, "dashboard.png"));
        await RenderAsync(new ManualControlView { DataContext = viewModel.Manual }, "Manual Control", "Simulator-only manual operations", IoPath.Combine(outputDirectory, "manual-control.png"));
        await RenderAsync(new IoMonitorView { DataContext = viewModel.IoMonitor }, "I/O Monitor", "Named DO/DI points from EquipmentProfile", IoPath.Combine(outputDirectory, "io-monitor.png"));
        await RenderAsync(new AutoSequenceView { DataContext = viewModel.AutoSequence }, "Auto Sequence", "Scheduler and sequence status", IoPath.Combine(outputDirectory, "auto-sequence.png"));
        await RenderAsync(new WaferRecipeFlowView { DataContext = viewModel.WaferRecipeFlow }, "Wafer / Recipe Flow", "FOUP and PM simulator state", IoPath.Combine(outputDirectory, "wafer-flow.png"));
        await RenderAsync(new AlarmEventLogView { DataContext = viewModel.AlarmEventLog }, "Alarm & Event Log", "Simulator alarm and event history", IoPath.Combine(outputDirectory, "alarm-log.png"));
        await RenderAsync(new SettingsView { DataContext = viewModel.Settings }, "Settings", "Simulator-first configuration", IoPath.Combine(outputDirectory, "settings.png"));

        // Generated assets render the actual runtime MachineTwinView instead of a separate drawing path.
        await CaptureMachineTwinPortfolioFramesAsync(viewModel.MachineTwin, outputDirectory);
    }

    public static async Task CaptureUiDebugReportAsync(RuntimeCoordinator runtime, MainViewModel viewModel)
    {
        var repositoryRoot = FindRepositoryRoot();
        var debugDirectory = IoPath.Combine(repositoryRoot, "docs", "debug", "latest");
        var screenshotDirectory = IoPath.Combine(debugDirectory, "screenshots");
        if (Directory.Exists(screenshotDirectory))
        {
            Directory.Delete(screenshotDirectory, recursive: true);
        }

        Directory.CreateDirectory(debugDirectory);
        foreach (var fileName in new[]
        {
            "ui-runtime-verification.md",
            "machine-twin-state-trace.json",
            "machine-twin-state-trace.csv",
            "event-log.txt"
        })
        {
            var filePath = IoPath.Combine(debugDirectory, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        Directory.CreateDirectory(screenshotDirectory);
        await viewModel.RefreshAsync();

        var trace = new List<MachineTwinStateTraceEntry>();
        await viewModel.MachineTwin.RunTransferSequenceForCaptureAsync(async step =>
        {
            if (!ShouldCaptureDebugStep(step))
            {
                return;
            }

            var fileName = step.ScreenshotName;
            var path = IoPath.Combine(screenshotDirectory, fileName);
            if (step.StepIndex == 0)
            {
                await RenderMainWindowAsync(viewModel, path);
            }
            else
            {
                await RenderMachineTwinAsync(viewModel.MachineTwin, "UI Debug", step.StepName, path);
            }

            var relativePath = $"docs/debug/latest/screenshots/{fileName}";
            trace.Add(viewModel.MachineTwin.CreateTraceEntry(step, relativePath));
        });

        await WriteDebugReportAsync(debugDirectory, trace);
    }

    // 전체 5장 파이프라인 캡처는 실제 EtherCAT 장비를 연결하지 않는 시뮬레이터 검증 전용 경로다.
    public static async Task CaptureFullPipelineQaAsync(RuntimeCoordinator runtime, MainViewModel viewModel)
    {
        var repositoryRoot = FindRepositoryRoot();
        var fullPipelineDirectory = IoPath.Combine(repositoryRoot, "docs", "debug", "latest", "full-pipeline");
        var screenshotDirectory = IoPath.Combine(fullPipelineDirectory, "screenshots");
        if (Directory.Exists(screenshotDirectory))
        {
            Directory.Delete(screenshotDirectory, recursive: true);
        }

        Directory.CreateDirectory(screenshotDirectory);
        foreach (var fileName in new[]
        {
            "full-machine-twin-state-trace.json",
            "full-pipeline-qa-summary.md"
        })
        {
            var filePath = IoPath.Combine(fullPipelineDirectory, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        await viewModel.RefreshAsync();

        var trace = new List<MachineTwinStateTraceEntry>();
        await viewModel.MachineTwin.RunTransferSequenceForCaptureAsync(async step =>
        {
            if (!ShouldCaptureFullPipelineStep(step))
            {
                return;
            }

            var fileName = BuildFullPipelineScreenshotName(step);
            var path = IoPath.Combine(screenshotDirectory, fileName);
            if (step.StepIndex == 0)
            {
                await RenderMainWindowAsync(viewModel, path);
            }
            else
            {
                await RenderMachineTwinAsync(viewModel.MachineTwin, "Full Pipeline QA", step.StepName, path);
            }

            var relativePath = $"docs/debug/latest/full-pipeline/screenshots/{fileName}";
            trace.Add(viewModel.MachineTwin.CreateTraceEntry(step, relativePath));
        });

        var json = JsonSerializer.Serialize(trace, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(IoPath.Combine(fullPipelineDirectory, "full-machine-twin-state-trace.json"), json, Encoding.UTF8);
        await File.WriteAllTextAsync(IoPath.Combine(fullPipelineDirectory, "full-pipeline-qa-summary.md"), BuildFullPipelineQaSummary(trace), Encoding.UTF8);
    }

    private static async Task CaptureMachineTwinPortfolioFramesAsync(MachineTwinViewModel machineTwin, string outputDirectory)
    {
        await machineTwin.RunTransferSequenceForCaptureAsync(async step =>
        {
            var fileName = step.ScreenshotName switch
            {
                "00-startup-simulator.png" => "digital-twin-limited-theta-swing.png",
                "03-chamber-a-door-opening.png" => "digital-twin-wafer-transfer-robot.png",
                "02-blade-holding-wafer-after-pickup.png" => "digital-twin-blade-mechanism.png",
                "01-foup-a-before-pickup.png" => "simulator-demo-frame-01.png",
                "04-blade-entering-chamber-a-door-open.png" => "simulator-demo-frame-02.png",
                "07-chamber-a-processing-door-closed.png" => "simulator-demo-frame-03.png",
                "09-final-foup-b-5-completed.png" => "simulator-demo-frame-04.png",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                await RenderMachineTwinAsync(machineTwin, "Runtime Machine Twin", step.StepName, IoPath.Combine(outputDirectory, fileName));
            }
        });
    }

    private static bool ShouldCaptureDebugStep(MachineTwinSequenceStep step) =>
        step.StepIndex == 0 ||
        step.ScreenshotName is
            "01-foup-a-before-pickup.png" or
            "02-z-work-foup-a-slot-a1.png" or
            "02-blade-holding-wafer-after-pickup.png" or
            "03-chamber-a-door-opening.png" or
            "04-blade-entering-chamber-a-door-open.png" or
            "05-wafer-placed-chamber-a-stage.png" or
            "06-blade-retracted-before-chamber-a-door-closes.png" or
            "07-chamber-a-processing-door-closed.png" or
            "08-chamber-a-unload-after-process-complete.png" or
            "09-final-foup-b-5-completed.png" or
            "10-reset-safe-state.png";

    private static bool ShouldCaptureFullPipelineStep(MachineTwinSequenceStep step)
    {
        var name = step.StepName;
        return step.StepIndex == 0 ||
            string.Equals(name, MachineTwinSequencePlan.ResetStepName, StringComparison.Ordinal) ||
            string.Equals(name, MachineTwinSequencePlan.CompletedStepName, StringComparison.Ordinal) ||
            name.StartsWith("Move To FOUP A Slot", StringComparison.Ordinal) ||
            name.Contains("On Blade From FOUP A Slot", StringComparison.Ordinal) ||
            name.Contains("Blade Entering Chamber", StringComparison.Ordinal) ||
            name.Contains("Placed At Chamber", StringComparison.Ordinal) ||
            name.Contains("Processing W", StringComparison.Ordinal) ||
            name.Contains("Placed At FOUP B Slot", StringComparison.Ordinal);
    }

    private static string BuildFullPipelineScreenshotName(MachineTwinSequenceStep step) =>
        $"{step.StepIndex:000}-{SlugForFileName(step.StepName)}.png";

    private static string BuildFullPipelineQaSummary(IReadOnlyList<MachineTwinStateTraceEntry> trace)
    {
        var builder = new StringBuilder();
        var final = trace.LastOrDefault(item => string.Equals(item.StepName, MachineTwinSequencePlan.CompletedStepName, StringComparison.Ordinal));
        builder.AppendLine("# Full Pipeline QA Summary");
        builder.AppendLine();
        builder.AppendLine("- Capture command: `dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-full-pipeline-qa`");
        builder.AppendLine("- Verification boundary: Simulator-mode WPF render capture only. No real EtherCAT hardware connection is attempted.");
        builder.AppendLine($"- Total sequence steps checked: {trace.Count}");
        builder.AppendLine($"- Screenshots captured: {trace.Count}");
        builder.AppendLine($"- Final FOUP A count: {final?.FoupACount ?? 0}/5");
        builder.AppendLine($"- Final FOUP B count: {final?.FoupBCount ?? 0}/5");
        builder.AppendLine($"- Final completed count: {final?.CompletedCount ?? 0}/5");
        builder.AppendLine();
        builder.AppendLine("## Pass Criteria");
        builder.AppendLine();
        builder.AppendLine("- FOUP A starts at 5/5 and drains to 0/5.");
        builder.AppendLine("- FOUP B starts at 0/5 and fills to 5/5.");
        builder.AppendLine("- W01-W05 each pass FOUP A, Chamber A, Chamber B, Chamber C, and FOUP B in order.");
        builder.AppendLine("- Home / Start captures remain blade-retracted; extension captures occur after station targeting.");
        builder.AppendLine("- Chamber captures include placed and processing frames for A/B/C.");
        builder.AppendLine();
        builder.AppendLine("## Wafer Movement Evidence");
        builder.AppendLine();
        builder.AppendLine("| Wafer | FOUP A Pick | Chamber A | Chamber B | Chamber C | FOUP B Place |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        for (var wafer = 1; wafer <= 5; wafer++)
        {
            var waferId = $"W{wafer:00}";
            builder.AppendLine(string.Join(" | ", new[]
            {
                $"| {waferId}",
                LinkFor(trace, $"{waferId} On Blade From FOUP A Slot"),
                LinkFor(trace, $"{waferId} Placed At Chamber A"),
                LinkFor(trace, $"{waferId} Placed At Chamber B"),
                LinkFor(trace, $"{waferId} Placed At Chamber C"),
                LinkFor(trace, $"{waferId} Placed At FOUP B Slot")
            }) + " |");
        }

        builder.AppendLine();
        builder.AppendLine("## Captured Screenshot Files");
        builder.AppendLine();
        foreach (var item in trace)
        {
            builder.AppendLine($"- `{item.ScreenshotPath}`");
        }

        return builder.ToString();
    }

    private static string LinkFor(IReadOnlyList<MachineTwinStateTraceEntry> trace, string stepNamePart)
    {
        var match = trace.FirstOrDefault(item => item.StepName.Contains(stepNamePart, StringComparison.Ordinal));
        if (match is null)
        {
            return "-";
        }

        return $"[{IoPath.GetFileName(match.ScreenshotPath)}]({match.ScreenshotPath.Replace("docs/debug/latest/full-pipeline/", string.Empty, StringComparison.Ordinal)})";
    }

    private static string SlugForFileName(string value)
    {
        var builder = new StringBuilder();
        var previousWasDash = false;
        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasDash = false;
                continue;
            }

            if (!previousWasDash)
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static Task RenderMachineTwinAsync(MachineTwinViewModel machineTwin, string title, string subtitle, string path) =>
        RenderAsync(new MachineTwinView { DataContext = machineTwin }, title, subtitle, path, motionSettleDelayMs: 1700);

    private static async Task RenderMainWindowAsync(MainViewModel viewModel, string path)
    {
        var window = new MainWindow
        {
            DataContext = viewModel,
            Width = CaptureWidth,
            Height = CaptureHeight,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -32000,
            Top = -32000,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            WindowState = WindowState.Normal,
            ResizeMode = ResizeMode.NoResize
        };

        try
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Width = CaptureWidth;
            window.Height = CaptureHeight;
            window.Measure(new Size(CaptureWidth, CaptureHeight));
            window.Arrange(new Rect(0, 0, CaptureWidth, CaptureHeight));
            window.UpdateLayout();
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

            var bitmap = new RenderTargetBitmap(CapturePixelWidth, CapturePixelHeight, Dpi, Dpi, PixelFormats.Pbgra32);
            bitmap.Render(window);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            await using var stream = File.Create(path);
            encoder.Save(stream);
        }
        finally
        {
            window.Close();
        }
    }

    private static async Task WriteDebugReportAsync(string debugDirectory, IReadOnlyList<MachineTwinStateTraceEntry> trace)
    {
        var json = JsonSerializer.Serialize(trace, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(IoPath.Combine(debugDirectory, "machine-twin-state-trace.json"), json, Encoding.UTF8);
        await File.WriteAllTextAsync(IoPath.Combine(debugDirectory, "machine-twin-state-trace.csv"), BuildCsv(trace), Encoding.UTF8);
        await File.WriteAllLinesAsync(
            IoPath.Combine(debugDirectory, "event-log.txt"),
            trace.Select(item => $"{item.Timestamp:O} | {item.EventLogMessage}"),
            Encoding.UTF8);
        await File.WriteAllTextAsync(IoPath.Combine(debugDirectory, "ui-runtime-verification.md"), BuildDebugReport(trace), Encoding.UTF8);
    }

    private static string BuildCsv(IReadOnlyList<MachineTwinStateTraceEntry> trace)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", new[]
        {
            "StepIndex",
            "StepName",
            "Timestamp",
            "IsSimulatorMode",
            "IsRealHardwareMode",
            "IsConnected",
            "MachineState",
            "CurrentStation",
            "PreviousStation",
            "NextStation",
            "CurrentStepName",
            "CurrentAction",
            "RobotState",
            "BladeState",
            "VacuumDisplayState",
            "ChamberADoorState",
            "ChamberBDoorState",
            "ChamberCDoorState",
            "ThetaTargetName",
            "VisualThetaAngle",
            "PreservedThetaEncoderValue",
            "ZState",
            "IsBladeExtended",
            "IsCylinderForward",
            "IsCylinderBackward",
            "IsVacuumOn",
            "IsWaferOnBlade",
            "IsWaferInFoupA1",
            "IsWaferInChamberA",
            "IsWaferInChamberB",
            "IsWaferInChamberC",
            "IsWaferInFoupB1",
            "ChamberADoorOpen",
            "ChamberBDoorOpen",
            "ChamberCDoorOpen",
            "TowerRed",
            "TowerYellow",
            "TowerGreen",
            "AlarmSummary",
            "EventLogMessage",
            "PipelineState",
            "FoupACount",
            "FoupBCount",
            "CompletedCount",
            "TotalWafers",
            "CurrentTransferDescription",
            "ActiveWaferId",
            "WaferIdOnBlade",
            "VacuumState",
            "WaferIds",
            "TimingProfileName",
            "FoupASlotStates",
            "FoupBSlotStates",
            "ChamberAState",
            "ChamberBState",
            "ChamberCState",
            "ScreenshotPath"
        }));

        foreach (var item in trace)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                item.StepIndex.ToString(),
                Csv(item.StepName),
                Csv(item.Timestamp.ToString("O")),
                item.IsSimulatorMode.ToString(),
                item.IsRealHardwareMode.ToString(),
                item.IsConnected.ToString(),
                Csv(item.MachineState),
                Csv(item.CurrentStation),
                Csv(item.PreviousStation),
                Csv(item.NextStation),
                Csv(item.CurrentStepName),
                Csv(item.CurrentAction),
                Csv(item.RobotState),
                Csv(item.BladeState),
                Csv(item.VacuumDisplayState),
                Csv(item.ChamberADoorState),
                Csv(item.ChamberBDoorState),
                Csv(item.ChamberCDoorState),
                Csv(item.ThetaTargetName),
                item.VisualThetaAngle.ToString("F0"),
                item.PreservedThetaEncoderValue.ToString(),
                Csv(item.ZState),
                item.IsBladeExtended.ToString(),
                item.IsCylinderForward.ToString(),
                item.IsCylinderBackward.ToString(),
                item.IsVacuumOn.ToString(),
                item.IsWaferOnBlade.ToString(),
                item.IsWaferInFoupA1.ToString(),
                item.IsWaferInChamberA.ToString(),
                item.IsWaferInChamberB.ToString(),
                item.IsWaferInChamberC.ToString(),
                item.IsWaferInFoupB1.ToString(),
                item.ChamberADoorOpen.ToString(),
                item.ChamberBDoorOpen.ToString(),
                item.ChamberCDoorOpen.ToString(),
                item.TowerRed.ToString(),
                item.TowerYellow.ToString(),
                item.TowerGreen.ToString(),
                Csv(item.AlarmSummary),
                Csv(item.EventLogMessage),
                Csv(item.PipelineState),
                item.FoupACount.ToString(),
                item.FoupBCount.ToString(),
                item.CompletedCount.ToString(),
                item.TotalWafers.ToString(),
                Csv(item.CurrentTransferDescription),
                Csv(item.ActiveWaferId),
                Csv(item.WaferIdOnBlade),
                Csv(item.VacuumState),
                Csv(item.WaferIds),
                Csv(item.TimingProfileName),
                Csv(item.FoupASlotStates),
                Csv(item.FoupBSlotStates),
                Csv(item.ChamberAState),
                Csv(item.ChamberBState),
                Csv(item.ChamberCState),
                Csv(item.ScreenshotPath)
            }));
        }

        return builder.ToString();
    }

    private static string BuildDebugReport(IReadOnlyList<MachineTwinStateTraceEntry> trace)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Runtime UI Verification Report");
        builder.AppendLine();
        builder.AppendLine("## Purpose");
        builder.AppendLine();
        builder.AppendLine("This report proves how the actual WPF simulator UI moves during debug/capture mode. The screenshots are rendered from the same `MachineTwinView` and `MachineTwinViewModel` used by the running app.");
        builder.AppendLine();
        builder.AppendLine("## Execution Command");
        builder.AppendLine();
        builder.AppendLine("```powershell");
        builder.AppendLine("dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Verification Boundary");
        builder.AppendLine();
        builder.AppendLine("- Simulator mode only.");
        builder.AppendLine("- No vendor DLL is loaded.");
        builder.AppendLine("- No real hardware connection is attempted.");
        builder.AppendLine("- Visual theta angle is for HMI rendering only.");
        builder.AppendLine("- Preserved theta encoder values are machine position values, not literal UI degrees.");
        builder.AppendLine("- The robot is modeled as a limited station-to-station theta swing, not continuous 360-degree rotation.");
        builder.AppendLine("- Normal runtime `Run Transfer Sequence` holds at FOUP B 5/5 completed until the user presses Reset; only explicit capture modes call application shutdown.");
        builder.AppendLine();
        builder.AppendLine("## Runtime Integration Check");
        builder.AppendLine();
        builder.AppendLine("- MainWindow first tab is `Machine Twin`.");
        builder.AppendLine("- MainWindow uses `<views:MachineTwinView DataContext=\"{Binding MachineTwin}\" />`.");
        builder.AppendLine("- MainViewModel exposes `MachineTwinViewModel` through the `MachineTwin` property.");
        builder.AppendLine("- `Run Transfer Sequence` is a command on the actual `MachineTwinView` runtime screen.");
        builder.AppendLine("- `00-startup-simulator.png` is captured from the actual `MainWindow`, so it shows the selected `Machine Twin` tab.");
        builder.AppendLine("- The remaining screenshots are captured from the same `MachineTwinView` and `MachineTwinViewModel` used by the running app.");
        builder.AppendLine();
        builder.AppendLine("## Captured Steps");
        builder.AppendLine();
        builder.AppendLine("| Step | State | Action | Station | FOUP A | FOUP B | Chambers | Door/Blade/Vacuum | Screenshot |");
        builder.AppendLine("|---:|---|---|---|---:|---:|---|---|---|");

        foreach (var item in trace)
        {
            var chamberSummary = $"{item.ChamberAState}<br>{item.ChamberBState}<br>{item.ChamberCState}";
            var sequenceState = $"A:{item.ChamberADoorState} B:{item.ChamberBDoorState} C:{item.ChamberCDoorState}<br>{item.BladeState}<br>{item.VacuumDisplayState}";
            builder.AppendLine($"| {item.StepIndex} | {item.StepName} | {item.CurrentAction} | {item.CurrentStation} | {item.FoupACount}/5 | {item.FoupBCount}/5 | {chamberSummary} | {sequenceState} | [{IoPath.GetFileName(item.ScreenshotPath)}]({item.ScreenshotPath.Replace("docs/debug/latest/", string.Empty)}) |");
        }

        builder.AppendLine();
        builder.AppendLine("## Expected vs Actual Movement");
        builder.AppendLine();
        builder.AppendLine("| Expected simulator movement | Evidence in this report |");
        builder.AppendLine("|---|---|");
        builder.AppendLine("| Machine Twin starts in Simulator mode and does not connect to real hardware. | Step 0 shows `IsSimulatorMode=true` and `IsRealHardwareMode=false`; `IsConnected` refers to the simulator controller connection, not real equipment. |");
        builder.AppendLine("| FOUP A starts with five wafers. | Steps 0 and 1 show `FoupACount=5` and `FoupBCount=0`. |");
        builder.AppendLine("| Theta target follows the limited station arc instead of a 360-degree dial. | The trace records station-to-station `ThetaTargetName` changes plus preserved encoder values. |");
        builder.AppendLine("| Z moves from Safe to Work only during pick/place visualization. | Pick/place steps show `ZState=Z Work`; processing and reset states return to `Z Safe`. |");
        builder.AppendLine("| Chamber doors gate blade entry. | Chamber-target blade-extension steps include `DoorState=Open`; close steps occur only after the blade retracts. |");
        builder.AppendLine("| Cylinder forward extends the telescopic blade. | Steps with `BladeState=Extending/Extended` also show `IsCylinderForward=true`. |");
        builder.AppendLine("| Vacuum suction attaches the wafer to the blade. | Pickup steps show `VacuumDisplayState=SuctionOn` before the wafer appears on the blade. |");
        builder.AppendLine("| Vacuum exhaust/release places the wafer into the chamber or FOUP. | Placement steps show `VacuumDisplayState=ExhaustOrRelease` before the wafer moves to the target. |");
        builder.AppendLine("| Tower yellow indicates simulator sequence completion. | The final complete state shows `TowerYellow=true` with FOUP B 5/5 and the completion alarm text. |");
        builder.AppendLine("| Reset returns the visual to a safe simulator state. | Reset returns to FOUP A loaded, blade retracted, vacuum off, all chamber doors closed, and Z Safe. |");
        builder.AppendLine("| FOUP A count decreases from 5 to 0. | Captured states show FOUP A 5/5 at startup, 4/5 after W01 pick, and 0/5 while the pipeline drains. |");
        builder.AppendLine("| FOUP B count increases from 0 to 5. | Captured states show B1 filled after W01 and all B1-B5 filled at completion. |");
        builder.AppendLine("| Chambers are used as a pipeline. | The state trace records Chamber A/B/C wafer ownership and process state while the five-wafer scheduler drains downstream first. |");
        builder.AppendLine("| Scheduler drains downstream first. | The timeline only unloads completed chambers and uses the priority C -> FOUP B, B -> C, A -> B, FOUP A -> A. |");
        builder.AppendLine("| Runtime sequence does not auto-close or auto-reset. | The only shutdown calls live in explicit capture-mode startup paths; normal `Run Transfer Sequence` leaves the window open at FOUP B 5/5 completed until Reset is pressed. |");
        builder.AppendLine();
        builder.AppendLine("## Screenshot Timeline");
        builder.AppendLine();
        builder.AppendLine("| Screenshot | What to check visually |");
        builder.AppendLine("|---|---|");

        foreach (var item in trace)
        {
            var screenshotName = IoPath.GetFileName(item.ScreenshotPath);
            builder.AppendLine($"| [{screenshotName}]({item.ScreenshotPath.Replace("docs/debug/latest/", string.Empty)}) | {item.EventLogMessage} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Known Limitations");
        builder.AppendLine();
        builder.AppendLine("- This evidence pack is simulator-mode only.");
        builder.AppendLine("- It does not prove that the new WPF app has been verified on physical equipment.");
        builder.AppendLine("- Real hardware feedback depends on the local vendor DLL, EtherCAT wiring, E-stop path, and supervised commissioning.");
        builder.AppendLine("- If the real adapter exposes only commanded state, the UI must label it as commanded or last-known state.");
        builder.AppendLine();
        builder.AppendLine("## Generated Files");
        builder.AppendLine();
        builder.AppendLine("- `ui-runtime-verification.md`");
        builder.AppendLine("- `machine-twin-state-trace.json`");
        builder.AppendLine("- `machine-twin-state-trace.csv`");
        builder.AppendLine("- `event-log.txt`");

        foreach (var item in trace)
        {
            builder.AppendLine($"- `{item.ScreenshotPath}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Trace Files");
        builder.AppendLine();
        builder.AppendLine("- `machine-twin-state-trace.json`");
        builder.AppendLine("- `machine-twin-state-trace.csv`");
        builder.AppendLine("- `event-log.txt`");
        return builder.ToString();
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static async Task PrepareSimulatorStateAsync(RuntimeCoordinator runtime)
    {
        await runtime.Controller.ConnectAsync();
        runtime.Safety.MarkConnected();
        await runtime.Controller.ServoOnAsync();
        await runtime.Controller.HomeAxisAsync(AxisId.Z);
        await runtime.Controller.HomeAxisAsync(AxisId.Theta);
        runtime.Safety.MarkHomed(AxisId.Z);
        runtime.Safety.MarkHomed(AxisId.Theta);
        await runtime.Controller.MoveAxisAbsoluteAsync(AxisId.Z, runtime.Profile.GetPose("ChamberA").ZSafe);
        await runtime.Controller.MoveAxisAbsoluteAsync(AxisId.Theta, runtime.Profile.GetPose("ChamberA").Theta);
        await runtime.Controller.WriteDigitalOutputAsync(IoPoint.TowerGreen, true);
        await runtime.Controller.WriteDigitalOutputAsync(IoPoint.ChamberALamp, true);
        await runtime.Controller.WriteDigitalOutputAsync(IoPoint.VacuumSuction, true);
        await runtime.Controller.SetSimulatorInputAsync(IoPoint.ChamberADoorCloseSensor, true);
        await runtime.Controller.SetSimulatorInputAsync(IoPoint.CylinderRearSensor, true);

        runtime.Scheduler.State.PmA.HasWafer = true;
        runtime.Scheduler.State.PmA.WaferId = "A01";
        runtime.Scheduler.State.PmA.RecipeName = runtime.Recipes.Recipes["A"].RecipeName;
        runtime.Scheduler.State.PmA.RemainingSeconds = 24;
        runtime.Scheduler.State.PmA.ProcessComplete = false;
        runtime.Scheduler.State.PmB.HasWafer = true;
        runtime.Scheduler.State.PmB.WaferId = "A02";
        runtime.Scheduler.State.PmB.RecipeName = runtime.Recipes.Recipes["B"].RecipeName;
        runtime.Scheduler.State.PmB.RemainingSeconds = 0;
        runtime.Scheduler.State.PmB.ProcessComplete = true;
        runtime.Scheduler.State.PmC.HasWafer = true;
        runtime.Scheduler.State.PmC.WaferId = "A03";
        runtime.Scheduler.State.PmC.RecipeName = runtime.Recipes.Recipes["C"].RecipeName;
        runtime.Scheduler.State.PmC.RemainingSeconds = 0;
        runtime.Scheduler.State.PmC.ProcessComplete = true;

        runtime.Events.Info(nameof(SequenceAssetCapture), "Simulator capture mode started.");
        runtime.Events.Info(nameof(SequenceAssetCapture), "Real hardware mode was not selected or connected.");
    }

    private static async Task RenderAsync(FrameworkElement content, string title, string subtitle, string path, int motionSettleDelayMs = 0)
    {
        var surface = CreateSurface(content, title, subtitle);
        surface.Measure(new Size(CaptureWidth, CaptureHeight));
        surface.Arrange(new Rect(0, 0, CaptureWidth, CaptureHeight));
        surface.UpdateLayout();
        await surface.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
        if (motionSettleDelayMs > 0)
        {
            // 캡처용 새 Viewport3D도 회전/Z/블레이드 애니메이션을 끝낸 뒤 렌더링해야 실제 화면 순서와 맞습니다.
            await Task.Delay(motionSettleDelayMs);
            surface.UpdateLayout();
            await surface.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
        }

        var bitmap = new RenderTargetBitmap(CapturePixelWidth, CapturePixelHeight, Dpi, Dpi, PixelFormats.Pbgra32);
        bitmap.Render(surface);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        await using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static Grid CreateSurface(FrameworkElement content, string title, string subtitle)
    {
        content.Margin = new Thickness(18);
        content.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.VerticalAlignment = VerticalAlignment.Stretch;

        var surface = new Grid
        {
            Width = CaptureWidth,
            Height = CaptureHeight,
            Background = new SolidColorBrush(Color.FromRgb(245, 247, 249))
        };
        surface.RowDefinitions.Add(new RowDefinition { Height = new GridLength(74) });
        surface.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(37, 50, 56)),
            Padding = new Thickness(24, 10, 24, 10)
        };
        Grid.SetRow(header, 0);
        header.Child = new Grid
        {
            Children =
            {
                new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            Foreground = Brushes.White,
                            FontSize = 24,
                            FontWeight = FontWeights.SemiBold
                        },
                        new TextBlock
                        {
                            Text = subtitle,
                            Foreground = new SolidColorBrush(Color.FromRgb(217, 226, 236)),
                            FontSize = 14
                        }
                    }
                },
                new TextBlock
                {
                    Text = "Simulator Mode / No Real Hardware Connected",
                    Foreground = new SolidColorBrush(Color.FromRgb(217, 226, 236)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14
                }
            }
        };

        var body = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(245, 247, 249)),
            Child = content
        };
        Grid.SetRow(body, 1);

        surface.Children.Add(header);
        surface.Children.Add(body);
        return surface;
    }

    private static FrameworkElement CreateDigitalTwinLayout(DigitalTwinPhysicalModel model, DigitalTwinSequenceAssetState state)
    {
        var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(18, 26, 32)) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });

        var canvas = new Canvas
        {
            Width = 840,
            Height = 650,
            Margin = new Thickness(18),
            Background = new SolidColorBrush(Color.FromRgb(55, 64, 69))
        };
        Grid.SetColumn(canvas, 0);
        root.Children.Add(canvas);

        DrawEquipmentBase(canvas);

        var center = new Point(420, 340);
        var stationPoints = BuildStationPoints();
        DrawStationArc(canvas, model, stationPoints);
        DrawStations(canvas, model, stationPoints, state.CurrentTargetKey);
        DrawTowerLamp(canvas, state.TowerYellow);
        DrawThetaBaseAndBlade(canvas, center, stationPoints[state.CurrentTargetKey], state);

        var status = CreateStatusPanel(model, state);
        Grid.SetColumn(status, 1);
        root.Children.Add(status);
        return root;
    }

    private static FrameworkElement CreateBladeMechanismLayout(DigitalTwinPhysicalModel model)
    {
        var root = new Grid { Background = new SolidColorBrush(Color.FromRgb(20, 28, 34)) };
        var canvas = new Canvas { Width = 940, Height = 600, Margin = new Thickness(24) };
        root.Children.Add(canvas);

        AddText(canvas, "Two-stage telescopic blade / end-effector", 38, 30, 28, Brushes.White, FontWeights.SemiBold);
        AddText(canvas, "Display abstraction for simulator mode. Cylinder and vacuum commands remain named IoPoint operations.", 40, 70, 15, Brushes.LightSteelBlue);

        AddRectangle(canvas, 90, 250, 300, 72, Color.FromRgb(104, 115, 122), Color.FromRgb(189, 198, 204), 3);
        AddText(canvas, "Lower/base slide", 135, 272, 17, Brushes.White, FontWeights.SemiBold);
        AddText(canvas, model.BladeMechanism.BaseStage, 100, 330, 14, Brushes.LightSteelBlue);

        AddRectangle(canvas, 310, 232, 390, 38, Color.FromRgb(173, 184, 190), Color.FromRgb(224, 230, 234), 2);
        AddRectangle(canvas, 555, 220, 195, 62, Color.FromRgb(213, 218, 221), Color.FromRgb(247, 250, 252), 2);
        AddText(canvas, "Upper/front blade extends", 455, 186, 18, Brushes.White, FontWeights.SemiBold);
        AddText(canvas, "Front stage extends/retracts under cylinder command", 345, 345, 14, Brushes.LightSteelBlue);

        AddEllipse(canvas, 678, 232, 42, 42, Color.FromRgb(116, 191, 157), Color.FromRgb(213, 247, 230), 2);
        AddText(canvas, "Wafer held by vacuum", 760, 258, 16, Brushes.White, FontWeights.SemiBold);

        DrawArrow(canvas, new Point(140, 430), new Point(315, 430), "CylinderForward = extend");
        DrawArrow(canvas, new Point(690, 468), new Point(510, 468), "CylinderBackward = retract");
        DrawArrow(canvas, new Point(628, 115), new Point(698, 222), "VacuumSuction holds / VacuumExhaust releases");

        AddText(canvas, "Limited theta base aims this assembly at FOUP A, Chamber A, Chamber B, Chamber C, and FOUP B.", 80, 535, 16, Brushes.LightSteelBlue);
        return root;
    }

    private static void DrawEquipmentBase(Canvas canvas)
    {
        AddRectangle(canvas, 42, 34, 756, 558, Color.FromRgb(92, 102, 108), Color.FromRgb(171, 182, 188), 2);
        AddRectangle(canvas, 62, 54, 716, 518, Color.FromRgb(67, 75, 80), Color.FromRgb(221, 230, 236), 1);
        AddText(canvas, "Transparent cover outline / fixed aluminum base", 72, 62, 14, Brushes.LightSteelBlue);
        AddText(canvas, "Simulator Mode / Digital Twin / No Real Hardware Connected", 72, 532, 15, Brushes.LightGreen, FontWeights.SemiBold);
    }

    private static void DrawStationArc(Canvas canvas, DigitalTwinPhysicalModel model, IReadOnlyDictionary<string, Point> stationPoints)
    {
        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(115, 205, 255)),
            StrokeThickness = 4,
            StrokeDashArray = new DoubleCollection { 8, 5 },
            Points = new PointCollection(new[] { "FoupA", "ChamberA", "ChamberB", "ChamberC", "FoupB" }.Select(key => stationPoints[key]))
        };
        canvas.Children.Add(polyline);
        AddText(canvas, $"Limited Theta Swing ~{model.ThetaSwing.VisualSweepApproxDegrees} deg visual arc / not 360 deg", 250, 102, 17, Brushes.White, FontWeights.SemiBold);
    }

    private static void DrawStations(Canvas canvas, DigitalTwinPhysicalModel model, IReadOnlyDictionary<string, Point> stationPoints, string currentTargetKey)
    {
        foreach (var station in model.ThetaSwing.Stations.OrderBy(station => station.Order))
        {
            var point = stationPoints[station.PoseKey];
            var isCurrent = station.PoseKey == currentTargetKey;
            var fill = isCurrent ? Color.FromRgb(63, 171, 132) : Color.FromRgb(42, 52, 58);
            AddRectangle(canvas, point.X - 58, point.Y - 28, 116, 56, fill, Color.FromRgb(218, 230, 235), isCurrent ? 3 : 1.5);
            AddText(canvas, station.DisplayName, point.X - 45, point.Y - 17, 14, Brushes.White, FontWeights.SemiBold);
            AddText(canvas, $"Theta enc {station.ThetaEncoderPosition}", point.X - 45, point.Y + 3, 11, Brushes.LightSteelBlue);
        }
    }

    private static void DrawTowerLamp(Canvas canvas, bool yellowOn)
    {
        AddText(canvas, "Tower Lamp", 690, 70, 13, Brushes.White, FontWeights.SemiBold);
        AddEllipse(canvas, 720, 96, 24, 24, Color.FromRgb(130, 33, 31), Color.FromRgb(245, 100, 94), 1);
        AddEllipse(canvas, 720, 124, 24, 24, yellowOn ? Color.FromRgb(210, 150, 35) : Color.FromRgb(82, 64, 28), Color.FromRgb(255, 204, 83), 1);
        AddEllipse(canvas, 720, 152, 24, 24, Color.FromRgb(28, 73, 48), Color.FromRgb(160, 240, 190), 1);
    }

    private static void DrawThetaBaseAndBlade(Canvas canvas, Point center, Point target, DigitalTwinSequenceAssetState state)
    {
        AddEllipse(canvas, center.X - 66, center.Y - 66, 132, 132, Color.FromRgb(38, 47, 54), Color.FromRgb(207, 216, 222), 3);
        AddEllipse(canvas, center.X - 34, center.Y - 34, 68, 68, Color.FromRgb(85, 96, 104), Color.FromRgb(232, 238, 242), 2);
        AddText(canvas, "Theta base", center.X - 27, center.Y - 10, 14, Brushes.White, FontWeights.SemiBold);

        var direction = Normalize(new Vector(target.X - center.X, target.Y - center.Y));
        var baseEnd = center + direction * 118;
        var bladeEnd = center + direction * (state.BladeExtended ? 232 : 164);
        var waferPoint = center + direction * (state.BladeExtended ? 188 : 138);

        AddLine(canvas, center, baseEnd, Color.FromRgb(130, 140, 146), 28);
        AddLine(canvas, center + direction * 88, bladeEnd, Color.FromRgb(214, 221, 225), 18);
        AddLine(canvas, center + direction * 118, bladeEnd, Color.FromRgb(245, 248, 250), 5);
        AddText(canvas, state.BladeExtended ? "blade extended" : "blade retracted", center.X - 64, center.Y + 78, 14, Brushes.LightSteelBlue);

        if (state.WaferHeld)
        {
            AddEllipse(canvas, waferPoint.X - 19, waferPoint.Y - 19, 38, 38, Color.FromRgb(95, 181, 148), Color.FromRgb(216, 249, 233), 2);
        }
    }

    private static Border CreateStatusPanel(DigitalTwinPhysicalModel model, DigitalTwinSequenceAssetState state)
    {
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "Physical Model", Foreground = Brushes.White, FontSize = 22, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = model.EquipmentKind, Foreground = Brushes.LightSteelBlue, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 14) });
        panel.Children.Add(StatusLine("Scenario", "CMP Cluster = simulator/HMI reference"));
        panel.Children.Add(StatusLine("Theta Target", state.TargetLabel));
        panel.Children.Add(StatusLine("Theta Motion", "Limited station-to-station swing"));
        panel.Children.Add(StatusLine("Z", state.ZState));
        panel.Children.Add(StatusLine("Cylinder", state.CylinderState));
        panel.Children.Add(StatusLine("Vacuum", state.VacuumState));
        panel.Children.Add(StatusLine("Wafer", state.WaferLocation));
        panel.Children.Add(StatusLine("Step", state.CurrentStep));
        panel.Children.Add(new TextBlock
        {
            Text = "Encoder theta values are preserved profile positions, not literal UI degrees.",
            Foreground = Brushes.LightGoldenrodYellow,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 18, 0, 0)
        });

        return new Border
        {
            Margin = new Thickness(0, 18, 18, 18),
            Padding = new Thickness(18),
            Background = new SolidColorBrush(Color.FromRgb(34, 44, 52)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(94, 112, 124)),
            BorderThickness = new Thickness(1),
            Child = panel
        };
    }

    private static FrameworkElement StatusLine(string label, string value) =>
        new TextBlock
        {
            Text = $"{label}: {value}",
            Foreground = Brushes.WhiteSmoke,
            FontSize = 15,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };

    private static IReadOnlyDictionary<string, Point> BuildStationPoints() => new Dictionary<string, Point>
    {
        ["FoupA"] = new(210, 500),
        ["ChamberA"] = new(120, 320),
        ["ChamberB"] = new(420, 150),
        ["ChamberC"] = new(720, 320),
        ["FoupB"] = new(630, 500)
    };

    private static Vector Normalize(Vector vector)
    {
        vector.Normalize();
        return vector;
    }

    private static void AddText(Canvas canvas, string text, double x, double y, double fontSize, Brush brush, FontWeight? weight = null)
    {
        var block = new TextBlock
        {
            Text = text,
            Foreground = brush,
            FontSize = fontSize,
            FontWeight = weight ?? FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 760
        };
        Canvas.SetLeft(block, x);
        Canvas.SetTop(block, y);
        canvas.Children.Add(block);
    }

    private static void AddRectangle(Canvas canvas, double x, double y, double width, double height, Color fill, Color stroke, double strokeThickness)
    {
        var rectangle = new Rectangle
        {
            Width = width,
            Height = height,
            RadiusX = 6,
            RadiusY = 6,
            Fill = new SolidColorBrush(fill),
            Stroke = new SolidColorBrush(stroke),
            StrokeThickness = strokeThickness
        };
        Canvas.SetLeft(rectangle, x);
        Canvas.SetTop(rectangle, y);
        canvas.Children.Add(rectangle);
    }

    private static void AddEllipse(Canvas canvas, double x, double y, double width, double height, Color fill, Color stroke, double strokeThickness)
    {
        var ellipse = new Ellipse
        {
            Width = width,
            Height = height,
            Fill = new SolidColorBrush(fill),
            Stroke = new SolidColorBrush(stroke),
            StrokeThickness = strokeThickness
        };
        Canvas.SetLeft(ellipse, x);
        Canvas.SetTop(ellipse, y);
        canvas.Children.Add(ellipse);
    }

    private static void AddLine(Canvas canvas, Point start, Point end, Color color, double thickness)
    {
        canvas.Children.Add(new Line
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        });
    }

    private static void DrawArrow(Canvas canvas, Point start, Point end, string label)
    {
        AddLine(canvas, start, end, Color.FromRgb(102, 204, 255), 4);
        AddText(canvas, label, Math.Min(start.X, end.X), Math.Min(start.Y, end.Y) - 30, 15, Brushes.White, FontWeights.SemiBold);
    }

    private sealed record DigitalTwinSequenceAssetState(
        string CurrentTargetKey,
        string TargetLabel,
        bool BladeExtended,
        bool WaferHeld,
        string ZState,
        string CylinderState,
        string VacuumState,
        string WaferLocation,
        string CurrentStep,
        bool TowerYellow)
    {
        public static DigitalTwinSequenceAssetState LimitedSwingOverview { get; } = new("ChamberB", "Chamber B (CMP)", false, false, "Z Safe", "Cylinder Backward", "Vacuum OFF", "No wafer on blade", "Station arc overview", false);
        public static DigitalTwinSequenceAssetState TransferRobotWithWafer { get; } = new("ChamberA", "Chamber A", true, true, "Z Work", "Cylinder Forward", "Vacuum Suction ON", "Wafer held on blade", "Place wafer into Chamber A", false);
        public static DigitalTwinSequenceAssetState PickFromFoupA { get; } = new("FoupA", "FOUP A Slot 1", true, true, "Z Work", "Cylinder Forward", "Vacuum Suction ON", "Wafer picked from FOUP A", "Pick FOUP A Slot 1", false);
        public static DigitalTwinSequenceAssetState PlaceToChamberA { get; } = new("ChamberA", "Chamber A", true, false, "Z Work", "Cylinder Forward", "Vacuum Exhaust / release", "Wafer in Chamber A", "PreClean_Default starts", false);
        public static DigitalTwinSequenceAssetState TransferToChamberC { get; } = new("ChamberC", "Chamber C", false, true, "Z Safe", "Cylinder Backward", "Vacuum Suction ON", "Wafer carried from Chamber B", "CMP_Main complete, moving to PostClean_Dry", false);
        public static DigitalTwinSequenceAssetState PlaceToFoupB { get; } = new("FoupB", "FOUP B Slot 1", true, false, "Z Work -> Z Safe", "Cylinder Forward then Backward", "Vacuum Exhaust / release", "Wafer stored in FOUP B Slot 1", "Overall simulator flow complete", true);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null)
        {
            if (File.Exists(IoPath.Combine(directory.FullName, "SemiTool.EtherCAT.WPF.ControlSuite.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root for sequence asset output.");
    }
}
