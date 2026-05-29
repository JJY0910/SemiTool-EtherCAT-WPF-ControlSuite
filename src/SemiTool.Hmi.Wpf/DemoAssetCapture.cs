using System.IO;
using IoPath = System.IO.Path;
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

internal static class DemoAssetCapture
{
    private const int CaptureWidth = 1280;
    private const int CaptureHeight = 820;
    private const double Dpi = 96;

    public static async Task CaptureAsync(RuntimeCoordinator runtime, MainViewModel viewModel)
    {
        var outputDirectory = IoPath.Combine(FindRepositoryRoot(), "docs", "images");
        Directory.CreateDirectory(outputDirectory);

        await PrepareSimulatorStateAsync(runtime);
        await viewModel.RefreshAsync();
        await RenderAsync(new DashboardView { DataContext = viewModel.Dashboard }, "Dashboard", "Simulator mode overview", IoPath.Combine(outputDirectory, "dashboard.png"));
        await RenderAsync(new ManualControlView { DataContext = viewModel.Manual }, "Manual Control", "Simulator-only manual operations", IoPath.Combine(outputDirectory, "manual-control.png"));
        await RenderAsync(new IoMonitorView { DataContext = viewModel.IoMonitor }, "I/O Monitor", "Named DO/DI points from EquipmentProfile", IoPath.Combine(outputDirectory, "io-monitor.png"));
        await RenderAsync(new AutoSequenceView { DataContext = viewModel.AutoSequence }, "Auto Sequence", "Scheduler and sequence status", IoPath.Combine(outputDirectory, "auto-sequence.png"));
        await RenderAsync(new WaferRecipeFlowView { DataContext = viewModel.WaferRecipeFlow }, "Wafer / Recipe Flow", "FOUP and PM simulator state", IoPath.Combine(outputDirectory, "wafer-flow.png"));
        await RenderAsync(new AlarmEventLogView { DataContext = viewModel.AlarmEventLog }, "Alarm & Event Log", "Simulator alarm and event history", IoPath.Combine(outputDirectory, "alarm-log.png"));
        await RenderAsync(new SettingsView { DataContext = viewModel.Settings }, "Settings", "Simulator-first configuration", IoPath.Combine(outputDirectory, "settings.png"));

        var physicalModel = DigitalTwinPhysicalModel.CreateDefault(runtime.Profile);
        await RenderAsync(
            CreateDigitalTwinLayout(physicalModel, DigitalTwinDemoState.LimitedSwingOverview),
            "Digital Twin - Limited Theta Swing",
            "Station-to-station simulator model / not 360-degree rotation",
            IoPath.Combine(outputDirectory, "digital-twin-limited-theta-swing.png"));
        await RenderAsync(
            CreateDigitalTwinLayout(physicalModel, DigitalTwinDemoState.TransferRobotWithWafer),
            "Digital Twin - Wafer Transfer Robot",
            "Central theta base, telescopic blade, Z Safe/Work, cylinder, and vacuum",
            IoPath.Combine(outputDirectory, "digital-twin-wafer-transfer-robot.png"));
        await RenderAsync(
            CreateBladeMechanismLayout(physicalModel),
            "Digital Twin - Blade Mechanism",
            "Two-stage/telescopic blade end-effector abstraction",
            IoPath.Combine(outputDirectory, "digital-twin-blade-mechanism.png"));

        // The demo frames visualize simulator-only station-to-station swing. They do not load the vendor DLL or
        // claim that the new WPF app has already completed physical equipment verification.
        await RenderAsync(CreateDigitalTwinLayout(physicalModel, DigitalTwinDemoState.PickFromFoupA), "Simulator Demo", "Frame 01 - Pick FOUP A Slot 1", IoPath.Combine(outputDirectory, "simulator-demo-frame-01.png"));
        await RenderAsync(CreateDigitalTwinLayout(physicalModel, DigitalTwinDemoState.PlaceToChamberA), "Simulator Demo", "Frame 02 - Place to Chamber A / PreClean", IoPath.Combine(outputDirectory, "simulator-demo-frame-02.png"));
        await RenderAsync(CreateDigitalTwinLayout(physicalModel, DigitalTwinDemoState.TransferToChamberC), "Simulator Demo", "Frame 03 - Chamber B CMP to Chamber C", IoPath.Combine(outputDirectory, "simulator-demo-frame-03.png"));
        await RenderAsync(CreateDigitalTwinLayout(physicalModel, DigitalTwinDemoState.PlaceToFoupB), "Simulator Demo", "Frame 04 - Place to FOUP B Slot 1 / Complete", IoPath.Combine(outputDirectory, "simulator-demo-frame-04.png"));
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

    private static FrameworkElement CreateDigitalTwinLayout(DigitalTwinPhysicalModel model, DigitalTwinDemoState state)
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
        DrawTowerLamp(canvas, state.TowerGreen);
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

    private static void DrawTowerLamp(Canvas canvas, bool greenOn)
    {
        AddText(canvas, "Tower Lamp", 690, 70, 13, Brushes.White, FontWeights.SemiBold);
        AddEllipse(canvas, 720, 96, 24, 24, Color.FromRgb(130, 33, 31), Color.FromRgb(245, 100, 94), 1);
        AddEllipse(canvas, 720, 124, 24, 24, Color.FromRgb(132, 95, 24), Color.FromRgb(255, 204, 83), 1);
        AddEllipse(canvas, 720, 152, 24, 24, greenOn ? Color.FromRgb(25, 155, 83) : Color.FromRgb(28, 73, 48), Color.FromRgb(160, 240, 190), 1);
    }

    private static void DrawThetaBaseAndBlade(Canvas canvas, Point center, Point target, DigitalTwinDemoState state)
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

    private static Border CreateStatusPanel(DigitalTwinPhysicalModel model, DigitalTwinDemoState state)
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

    private sealed record DigitalTwinDemoState(
        string CurrentTargetKey,
        string TargetLabel,
        bool BladeExtended,
        bool WaferHeld,
        string ZState,
        string CylinderState,
        string VacuumState,
        string WaferLocation,
        string CurrentStep,
        bool TowerGreen)
    {
        public static DigitalTwinDemoState LimitedSwingOverview { get; } = new("ChamberB", "Chamber B (CMP)", false, false, "Z Safe", "Cylinder Backward", "Vacuum OFF", "No wafer on blade", "Station arc overview", false);
        public static DigitalTwinDemoState TransferRobotWithWafer { get; } = new("ChamberA", "Chamber A", true, true, "Z Work", "Cylinder Forward", "Vacuum Suction ON", "Wafer held on blade", "Place wafer into Chamber A", false);
        public static DigitalTwinDemoState PickFromFoupA { get; } = new("FoupA", "FOUP A Slot 1", true, true, "Z Work", "Cylinder Forward", "Vacuum Suction ON", "Wafer picked from FOUP A", "Pick FOUP A Slot 1", false);
        public static DigitalTwinDemoState PlaceToChamberA { get; } = new("ChamberA", "Chamber A", true, false, "Z Work", "Cylinder Forward", "Vacuum Exhaust / release", "Wafer in Chamber A", "PreClean_Default starts", false);
        public static DigitalTwinDemoState TransferToChamberC { get; } = new("ChamberC", "Chamber C", false, true, "Z Safe", "Cylinder Backward", "Vacuum Suction ON", "Wafer carried from Chamber B", "CMP_Main complete, moving to PostClean_Dry", false);
        public static DigitalTwinDemoState PlaceToFoupB { get; } = new("FoupB", "FOUP B Slot 1", true, false, "Z Work -> Z Safe", "Cylinder Forward then Backward", "Vacuum Exhaust / release", "Wafer stored in FOUP B Slot 1", "Overall simulator flow complete", true);
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

        throw new DirectoryNotFoundException("Could not locate repository root for demo asset output.");
    }
}
