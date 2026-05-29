using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SemiTool.Application;
using SemiTool.Domain;
using SemiTool.Hmi.Wpf.ViewModels;
using SemiTool.Hmi.Wpf.Views;

namespace SemiTool.Hmi.Wpf;

internal static class DemoAssetCapture
{
    private const int CaptureWidth = 1280;
    private const int CaptureHeight = 820;
    private const double Dpi = 96;

    public static async Task CaptureAsync(RuntimeCoordinator runtime, MainViewModel viewModel)
    {
        var outputDirectory = Path.Combine(FindRepositoryRoot(), "docs", "images");
        Directory.CreateDirectory(outputDirectory);

        await PrepareSimulatorStateAsync(runtime);
        await viewModel.RefreshAsync();
        await RenderAsync(new DashboardView { DataContext = viewModel.Dashboard }, "Dashboard", "Simulator mode overview", Path.Combine(outputDirectory, "dashboard.png"));
        await RenderAsync(new ManualControlView { DataContext = viewModel.Manual }, "Manual Control", "Simulator-only manual operations", Path.Combine(outputDirectory, "manual-control.png"));
        await RenderAsync(new IoMonitorView { DataContext = viewModel.IoMonitor }, "I/O Monitor", "Named DO/DI points from EquipmentProfile", Path.Combine(outputDirectory, "io-monitor.png"));
        await RenderAsync(new AutoSequenceView { DataContext = viewModel.AutoSequence }, "Auto Sequence", "Scheduler and sequence status", Path.Combine(outputDirectory, "auto-sequence.png"));
        await RenderAsync(new WaferRecipeFlowView { DataContext = viewModel.WaferRecipeFlow }, "Wafer / Recipe Flow", "FOUP and PM simulator state", Path.Combine(outputDirectory, "wafer-flow.png"));
        await RenderAsync(new AlarmEventLogView { DataContext = viewModel.AlarmEventLog }, "Alarm & Event Log", "Simulator alarm and event history", Path.Combine(outputDirectory, "alarm-log.png"));
        await RenderAsync(new SettingsView { DataContext = viewModel.Settings }, "Settings", "Simulator-first configuration", Path.Combine(outputDirectory, "settings.png"));

        await RenderAsync(new DashboardView { DataContext = viewModel.Dashboard }, "Simulator Demo", "Frame 01 - Dashboard", Path.Combine(outputDirectory, "simulator-demo-frame-01.png"));
        await RenderAsync(new IoMonitorView { DataContext = viewModel.IoMonitor }, "Simulator Demo", "Frame 02 - I/O Monitor", Path.Combine(outputDirectory, "simulator-demo-frame-02.png"));
        await RenderAsync(new AutoSequenceView { DataContext = viewModel.AutoSequence }, "Simulator Demo", "Frame 03 - Auto Sequence", Path.Combine(outputDirectory, "simulator-demo-frame-03.png"));
        await RenderAsync(new AlarmEventLogView { DataContext = viewModel.AlarmEventLog }, "Simulator Demo", "Frame 04 - Alarm Log", Path.Combine(outputDirectory, "simulator-demo-frame-04.png"));
    }

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

        runtime.Alarms.Raise(
            AlarmCode.Timeout,
            "Simulator demo alarm",
            "Generated by capture mode to demonstrate alarm display.",
            "Reset in simulator mode after review.");
        runtime.Events.Info(nameof(DemoAssetCapture), "Simulator capture mode started.");
        runtime.Events.Warn(nameof(DemoAssetCapture), "Demo alarm is simulator-only.");
        runtime.Events.Info(nameof(DemoAssetCapture), "Real hardware mode was not selected or connected.");
    }

    private static async Task RenderAsync(FrameworkElement content, string title, string subtitle, string path)
    {
        var surface = CreateSurface(content, title, subtitle);
        surface.Measure(new Size(CaptureWidth, CaptureHeight));
        surface.Arrange(new Rect(0, 0, CaptureWidth, CaptureHeight));
        surface.UpdateLayout();
        await surface.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

        var bitmap = new RenderTargetBitmap(CaptureWidth, CaptureHeight, Dpi, Dpi, PixelFormats.Pbgra32);
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SemiTool.EtherCAT.WPF.ControlSuite.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root for demo asset output.");
    }
}
