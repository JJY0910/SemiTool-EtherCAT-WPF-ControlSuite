using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace SemiTool.Hmi.Wpf.Controls;

/// <summary>
/// Native WPF 3D machine twin for the wafer transfer trainer.
/// </summary>
/// <remarks>
/// This control intentionally uses display-only geometry. It never stores,
/// edits, or derives protected teaching encoder values; runtime bindings supply
/// only simulator visual angle, blade extension, vacuum, wafer, and door state.
/// </remarks>
public sealed class MachineTwin3DView : Viewport3D
{
    public static readonly DependencyProperty RobotAngleProperty =
        DependencyProperty.Register(nameof(RobotAngle), typeof(double), typeof(MachineTwin3DView), new PropertyMetadata(0d, OnMotionChanged));

    public static readonly DependencyProperty BladeLengthProperty =
        DependencyProperty.Register(nameof(BladeLength), typeof(double), typeof(MachineTwin3DView), new PropertyMetadata(92d, OnMotionChanged));

    public static readonly DependencyProperty VacuumOnProperty =
        DependencyProperty.Register(nameof(VacuumOn), typeof(bool), typeof(MachineTwin3DView), new PropertyMetadata(false, OnMotionChanged));

    public static readonly DependencyProperty WaferOnBladeProperty =
        DependencyProperty.Register(nameof(WaferOnBlade), typeof(bool), typeof(MachineTwin3DView), new PropertyMetadata(false, OnMotionChanged));

    public static readonly DependencyProperty ChamberADoorOpenProperty =
        DependencyProperty.Register(nameof(ChamberADoorOpen), typeof(bool), typeof(MachineTwin3DView), new PropertyMetadata(false, OnMotionChanged));

    public static readonly DependencyProperty ChamberBDoorOpenProperty =
        DependencyProperty.Register(nameof(ChamberBDoorOpen), typeof(bool), typeof(MachineTwin3DView), new PropertyMetadata(false, OnMotionChanged));

    public static readonly DependencyProperty ChamberCDoorOpenProperty =
        DependencyProperty.Register(nameof(ChamberCDoorOpen), typeof(bool), typeof(MachineTwin3DView), new PropertyMetadata(false, OnMotionChanged));

    public static readonly DependencyProperty FoupACountProperty =
        DependencyProperty.Register(nameof(FoupACount), typeof(int), typeof(MachineTwin3DView), new PropertyMetadata(5, OnMotionChanged));

    public static readonly DependencyProperty FoupBCountProperty =
        DependencyProperty.Register(nameof(FoupBCount), typeof(int), typeof(MachineTwin3DView), new PropertyMetadata(0, OnMotionChanged));

    private readonly Model3DGroup _scene = new();
    private readonly Model3DGroup _robotGroup = new();
    private readonly AxisAngleRotation3D _robotRotation = new(new Vector3D(0, 1, 0), 0);
    private readonly ScaleTransform3D _bladeScale = new(1, 1, 1);
    private readonly TranslateTransform3D _waferOffset = new();
    private readonly List<GeometryModel3D> _foupAWafers = [];
    private readonly List<GeometryModel3D> _foupBWafers = [];
    private GeometryModel3D? _suctionPad;
    private GeometryModel3D? _bladeWafer;
    private GeometryModel3D? _chamberADoor;
    private GeometryModel3D? _chamberBDoor;
    private GeometryModel3D? _chamberCDoor;

    public MachineTwin3DView()
    {
        ClipToBounds = true;
        Camera = new PerspectiveCamera(
            new Point3D(0, 8.7, 11.2),
            new Vector3D(0, -8.0, -11.2),
            new Vector3D(0, 1, 0),
            48);

        Children.Add(new ModelVisual3D { Content = _scene });
        BuildScene();
        UpdateMotion(false);
    }

    public double RobotAngle
    {
        get => (double)GetValue(RobotAngleProperty);
        set => SetValue(RobotAngleProperty, value);
    }

    public double BladeLength
    {
        get => (double)GetValue(BladeLengthProperty);
        set => SetValue(BladeLengthProperty, value);
    }

    public bool VacuumOn
    {
        get => (bool)GetValue(VacuumOnProperty);
        set => SetValue(VacuumOnProperty, value);
    }

    public bool WaferOnBlade
    {
        get => (bool)GetValue(WaferOnBladeProperty);
        set => SetValue(WaferOnBladeProperty, value);
    }

    public bool ChamberADoorOpen
    {
        get => (bool)GetValue(ChamberADoorOpenProperty);
        set => SetValue(ChamberADoorOpenProperty, value);
    }

    public bool ChamberBDoorOpen
    {
        get => (bool)GetValue(ChamberBDoorOpenProperty);
        set => SetValue(ChamberBDoorOpenProperty, value);
    }

    public bool ChamberCDoorOpen
    {
        get => (bool)GetValue(ChamberCDoorOpenProperty);
        set => SetValue(ChamberCDoorOpenProperty, value);
    }

    public int FoupACount
    {
        get => (int)GetValue(FoupACountProperty);
        set => SetValue(FoupACountProperty, value);
    }

    public int FoupBCount
    {
        get => (int)GetValue(FoupBCountProperty);
        set => SetValue(FoupBCountProperty, value);
    }

    private static void OnMotionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((MachineTwin3DView)dependencyObject).UpdateMotion(true);

    private void BuildScene()
    {
        _scene.Children.Clear();
        _foupAWafers.Clear();
        _foupBWafers.Clear();

        _scene.Children.Add(new AmbientLight(Color.FromRgb(33, 47, 57)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(245, 251, 255), new Vector3D(-0.65, -1.1, -0.5)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(91, 218, 238), new Vector3D(0.45, -0.35, 0.35)));

        AddDeck();
        AddSafetyFrame();
        AddStations();
        AddCableCarrier();
        AddRobot();
    }

    private void AddDeck()
    {
        _scene.Children.Add(CreateBox(new Point3D(0, -0.08, 0), new Size3D(9.1, 0.16, 6.2), Material("#2D3F49"), Material("#6F8996")));
        _scene.Children.Add(CreateBox(new Point3D(0, 0.03, 0), new Size3D(8.45, 0.08, 5.55), Material("#10232B"), Material("#4E7180")));

        // UI용 HOME 기준선입니다. 실제 엔코더 원점값이 아니라 화면 방향 확인용입니다.
        _scene.Children.Add(CreateBox(new Point3D(0, 0.1, 0), new Size3D(0.04, 0.03, 4.6), Material("#8D2E35"), null));
        _scene.Children.Add(CreateBox(new Point3D(0, 0.11, 0), new Size3D(4.6, 0.03, 0.04), Material("#8D2E35"), null));

        var arcMaterial = Material("#34D7F0", 0.42);
        for (var index = 0; index < 28; index++)
        {
            var degrees = -150 + index * (300d / 27d);
            var radians = DegreesToRadians(degrees);
            var x = Math.Sin(radians) * 2.25;
            var z = -Math.Cos(radians) * 2.05;
            var segment = CreateBox(new Point3D(x, 0.13, z), new Size3D(0.26, 0.04, 0.08), arcMaterial, null);
            segment.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), degrees), new Point3D(x, 0.13, z));
            _scene.Children.Add(segment);
        }
    }

    private void AddSafetyFrame()
    {
        var rail = Material("#A9B8C0");
        var glass = Material("#8FD7F0", 0.16);
        var corner = Material("#2F3840");

        _scene.Children.Add(CreateBox(new Point3D(0, 0.22, -2.92), new Size3D(8.8, 0.18, 0.12), rail, null));
        _scene.Children.Add(CreateBox(new Point3D(0, 0.22, 2.92), new Size3D(8.8, 0.18, 0.12), rail, null));
        _scene.Children.Add(CreateBox(new Point3D(-4.42, 0.22, 0), new Size3D(0.12, 0.18, 5.85), rail, null));
        _scene.Children.Add(CreateBox(new Point3D(4.42, 0.22, 0), new Size3D(0.12, 0.18, 5.85), rail, null));

        foreach (var x in new[] { -4.25, 4.25 })
        {
            foreach (var z in new[] { -2.75, 2.75 })
            {
                _scene.Children.Add(CreateBox(new Point3D(x, 0.95, z), new Size3D(0.16, 1.75, 0.16), rail, null));
                _scene.Children.Add(CreateBox(new Point3D(x, 1.86, z), new Size3D(0.36, 0.22, 0.36), corner, null));
            }
        }

        _scene.Children.Add(CreateBox(new Point3D(0, 1.17, -2.86), new Size3D(7.9, 1.42, 0.05), glass, null));
        _scene.Children.Add(CreateBox(new Point3D(-4.12, 1.17, 0), new Size3D(0.05, 1.42, 5.1), glass, null));
        _scene.Children.Add(CreateBox(new Point3D(4.12, 1.17, 0), new Size3D(0.05, 1.42, 5.1), glass, null));
    }

    private void AddStations()
    {
        AddChamber("A", new Point3D(-2.9, 0.56, -0.75), -55);
        AddChamber("B", new Point3D(0, 0.56, -2.15), 0);
        AddChamber("C", new Point3D(2.9, 0.56, -0.75), 55);
        AddFoup("A", new Point3D(-3.18, 0.52, 1.78), 27, "#44F091", _foupAWafers);
        AddFoup("B", new Point3D(3.18, 0.52, 1.78), -27, "#F6C453", _foupBWafers);

        _scene.Children.Add(CreateTowerLight(new Point3D(-3.9, 1.05, -2.0)));
        _scene.Children.Add(CreateTowerLight(new Point3D(3.9, 1.05, -2.0)));
    }

    private void AddChamber(string name, Point3D center, double angle)
    {
        var chamber = new Model3DGroup();
        chamber.Children.Add(CreateBox(new Point3D(0, 0, 0), new Size3D(1.25, 0.96, 1.05), Material("#E9EEF2"), Material("#FFFFFF")));
        chamber.Children.Add(CreateBox(new Point3D(0, -0.05, 0.58), new Size3D(0.96, 0.43, 0.08), Material("#1E2D35"), null));
        chamber.Children.Add(CreateCylinder(new Point3D(0, -0.2, 0.04), 0.32, 0.09, 36, Material("#B9C8D0"), Material("#EDF6FA")));
        chamber.Children.Add(CreateBox(new Point3D(0.54, 0.05, 0.43), new Size3D(0.12, 0.12, 0.05), Material("#2FDB72"), null));
        chamber.Children.Add(CreateBox(new Point3D(-0.45, 0.36, 0.57), new Size3D(0.28, 0.06, 0.04), Material("#D7E5EB", 0.85), null));
        chamber.Children.Add(CreateBox(new Point3D(0.45, 0.36, 0.57), new Size3D(0.28, 0.06, 0.04), Material("#D7E5EB", 0.85), null));

        var door = CreateBox(new Point3D(0, 0.03, 0.67), new Size3D(0.84, 0.33, 0.04), Material("#C9DAE2", 0.76), Material("#FFFFFF", 0.82));
        chamber.Children.Add(door);
        switch (name)
        {
            case "A":
                _chamberADoor = door;
                break;
            case "B":
                _chamberBDoor = door;
                break;
            case "C":
                _chamberCDoor = door;
                break;
        }

        chamber.Transform = new Transform3DGroup
        {
            Children =
            {
                new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), angle)),
                new TranslateTransform3D(center.X, center.Y, center.Z)
            }
        };
        _scene.Children.Add(chamber);
    }

    private void AddFoup(string name, Point3D center, double angle, string accent, ICollection<GeometryModel3D> waferSlots)
    {
        var foup = new Model3DGroup();
        foup.Children.Add(CreateBox(new Point3D(0, 0, 0), new Size3D(1.02, 1.18, 0.86), Material("#070B10"), Material("#2A353D")));
        foup.Children.Add(CreateBox(new Point3D(0, -0.5, 0), new Size3D(1.2, 0.12, 0.96), Material("#BBC4CA"), null));
        foup.Children.Add(CreateBox(new Point3D(-0.6, -0.03, 0.02), new Size3D(0.08, 1.0, 0.8), Material("#111820"), null));
        foup.Children.Add(CreateBox(new Point3D(0.6, -0.03, 0.02), new Size3D(0.08, 1.0, 0.8), Material("#111820"), null));

        for (var index = 0; index < 5; index++)
        {
            var y = -0.32 + index * 0.16;
            foup.Children.Add(CreateBox(new Point3D(0, y, 0.43), new Size3D(0.82, 0.05, 0.08), Material(accent), null));
            var wafer = CreateCylinder(new Point3D(0, y + 0.04, 0.12), 0.34, 0.026, 48, Material("#A4F5D8", 0.74), Material("#E9FFF8", 0.78));
            waferSlots.Add(wafer);
            foup.Children.Add(wafer);
        }

        foup.Children.Add(CreateBox(new Point3D(0, 0.67, 0.42), new Size3D(0.78, 0.08, 0.1), Material(accent), null));
        foup.Transform = new Transform3DGroup
        {
            Children =
            {
                new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), angle)),
                new TranslateTransform3D(center.X, center.Y, center.Z)
            }
        };
        _scene.Children.Add(foup);
    }

    private void AddCableCarrier()
    {
        var carrier = Material("#090B0F");
        var tie = Material("#D84C4C");

        for (var i = 0; i < 19; i++)
        {
            var t = Math.PI * (0.12 + i / 23d);
            var x = Math.Cos(t) * 2.05;
            var z = Math.Sin(t) * 1.72;
            var link = CreateBox(new Point3D(x, 0.55, z), new Size3D(0.14, 0.16, 0.28), carrier, null);
            link.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -t * 180 / Math.PI), new Point3D(x, 0.55, z));
            _scene.Children.Add(link);

            if (i % 3 == 0)
            {
                _scene.Children.Add(CreateBox(new Point3D(x, 0.68, z), new Size3D(0.12, 0.04, 0.28), tie, null));
            }
        }
    }

    private void AddRobot()
    {
        _scene.Children.Add(CreateCylinder(new Point3D(0, 0.22, 0), 0.58, 0.28, 56, Material("#A8B8C2"), Material("#E8F2F7")));
        _scene.Children.Add(CreateCylinder(new Point3D(0, 0.45, 0), 0.38, 0.3, 56, Material("#607985"), Material("#C9D7DF")));

        _robotGroup.Transform = new RotateTransform3D(_robotRotation, new Point3D(0, 0.55, 0));
        _robotGroup.Children.Add(CreateBox(new Point3D(0.42, 0.72, 0), new Size3D(0.95, 0.22, 0.54), Material("#DDE8EE"), Material("#FFFFFF")));
        _robotGroup.Children.Add(CreateBox(new Point3D(0.7, 0.9, 0), new Size3D(0.48, 0.16, 0.48), Material("#A8B7BF"), null));

        var bladeGroup = new Model3DGroup { Transform = _bladeScale };
        bladeGroup.Children.Add(CreateBox(new Point3D(0.92, 0.62, 0), new Size3D(1.15, 0.08, 0.34), Material("#EDF6FA"), Material("#FFFFFF")));
        bladeGroup.Children.Add(CreateBox(new Point3D(0.92, 0.66, -0.18), new Size3D(1.05, 0.05, 0.06), Material("#94A7B1"), null));
        bladeGroup.Children.Add(CreateBox(new Point3D(0.92, 0.66, 0.18), new Size3D(1.05, 0.05, 0.06), Material("#94A7B1"), null));
        _robotGroup.Children.Add(bladeGroup);

        _suctionPad = CreateCylinder(new Point3D(1.56, 0.61, 0), 0.18, 0.055, 36, Material("#5FF0C9"), Material("#D7FFF4"));
        _robotGroup.Children.Add(_suctionPad);

        _bladeWafer = CreateCylinder(new Point3D(0, 0.67, 0), 0.31, 0.035, 56, Material("#B8F2FF", 0), Material("#FFFFFF", 0));
        _bladeWafer.Transform = _waferOffset;
        _robotGroup.Children.Add(_bladeWafer);

        _scene.Children.Add(_robotGroup);
    }

    private Model3DGroup CreateTowerLight(Point3D center)
    {
        var tower = new Model3DGroup();
        tower.Children.Add(CreateCylinder(new Point3D(center.X, center.Y - 0.45, center.Z), 0.12, 0.22, 28, Material("#D4DCE2"), null));
        tower.Children.Add(CreateCylinder(new Point3D(center.X, center.Y - 0.2, center.Z), 0.1, 0.22, 28, Material("#2BDB68"), null));
        tower.Children.Add(CreateCylinder(new Point3D(center.X, center.Y + 0.05, center.Z), 0.1, 0.22, 28, Material("#F6C453"), null));
        tower.Children.Add(CreateCylinder(new Point3D(center.X, center.Y + 0.3, center.Z), 0.1, 0.22, 28, Material("#EF4444"), null));
        return tower;
    }

    private void UpdateMotion(bool animated)
    {
        Animate(_robotRotation, AxisAngleRotation3D.AngleProperty, RobotAngle, animated ? 420 : 0);

        var extensionScale = Math.Clamp(BladeLength / 92d, 1d, 2.65d);
        Animate(_bladeScale, ScaleTransform3D.ScaleXProperty, extensionScale, animated ? 360 : 0);
        Animate(_waferOffset, TranslateTransform3D.OffsetXProperty, 1.52 * extensionScale, animated ? 360 : 0);

        UpdatePadAndWafer();
        UpdateDoor(_chamberADoor, ChamberADoorOpen);
        UpdateDoor(_chamberBDoor, ChamberBDoorOpen);
        UpdateDoor(_chamberCDoor, ChamberCDoorOpen);
        UpdateFoupWafers(_foupAWafers, FoupACount);
        UpdateFoupWafers(_foupBWafers, FoupBCount);
    }

    private void UpdatePadAndWafer()
    {
        if (_suctionPad is not null)
        {
            _suctionPad.Material = VacuumOn ? Material("#4CFFD0") : Material("#607985");
            _suctionPad.BackMaterial = _suctionPad.Material;
        }

        if (_bladeWafer is not null)
        {
            var material = WaferOnBlade ? Material("#B8F2FF", 0.86) : Material("#B8F2FF", 0);
            _bladeWafer.Material = material;
            _bladeWafer.BackMaterial = material;
        }
    }

    private static void UpdateDoor(GeometryModel3D? door, bool isOpen)
    {
        if (door is null)
        {
            return;
        }

        door.Material = isOpen ? Material("#F04444", 0.74) : Material("#C9DAE2", 0.76);
        door.BackMaterial = door.Material;
        door.Transform = new TranslateTransform3D(0, isOpen ? 0.24 : 0, isOpen ? 0.05 : 0);
    }

    private static void UpdateFoupWafers(IReadOnlyList<GeometryModel3D> wafers, int visibleCount)
    {
        for (var index = 0; index < wafers.Count; index++)
        {
            var material = index < Math.Clamp(visibleCount, 0, 5)
                ? Material("#A4F5D8", 0.72)
                : Material("#A4F5D8", 0);
            wafers[index].Material = material;
            wafers[index].BackMaterial = material;
        }
    }

    private static void Animate(Animatable target, DependencyProperty property, double to, int milliseconds)
    {
        if (milliseconds == 0)
        {
            target.BeginAnimation(property, null);
            target.SetValue(property, to);
            return;
        }

        target.BeginAnimation(
            property,
            new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.HoldEnd
            },
            HandoffBehavior.SnapshotAndReplace);
    }

    private static GeometryModel3D CreateBox(Point3D center, Size3D size, Material material, Material? backMaterial)
    {
        var halfX = size.X / 2d;
        var halfY = size.Y / 2d;
        var halfZ = size.Z / 2d;
        var mesh = new MeshGeometry3D
        {
            Positions =
            {
                new Point3D(center.X - halfX, center.Y - halfY, center.Z - halfZ),
                new Point3D(center.X + halfX, center.Y - halfY, center.Z - halfZ),
                new Point3D(center.X + halfX, center.Y + halfY, center.Z - halfZ),
                new Point3D(center.X - halfX, center.Y + halfY, center.Z - halfZ),
                new Point3D(center.X - halfX, center.Y - halfY, center.Z + halfZ),
                new Point3D(center.X + halfX, center.Y - halfY, center.Z + halfZ),
                new Point3D(center.X + halfX, center.Y + halfY, center.Z + halfZ),
                new Point3D(center.X - halfX, center.Y + halfY, center.Z + halfZ)
            }
        };

        AddFace(mesh, 0, 1, 2, 3);
        AddFace(mesh, 5, 4, 7, 6);
        AddFace(mesh, 4, 0, 3, 7);
        AddFace(mesh, 1, 5, 6, 2);
        AddFace(mesh, 3, 2, 6, 7);
        AddFace(mesh, 4, 5, 1, 0);

        return new GeometryModel3D(mesh, material) { BackMaterial = backMaterial ?? material };
    }

    private static GeometryModel3D CreateCylinder(Point3D center, double radius, double height, int segments, Material material, Material? backMaterial)
    {
        var mesh = new MeshGeometry3D();
        var topCenter = new Point3D(center.X, center.Y + height / 2d, center.Z);
        var bottomCenter = new Point3D(center.X, center.Y - height / 2d, center.Z);
        mesh.Positions.Add(topCenter);
        mesh.Positions.Add(bottomCenter);

        for (var i = 0; i < segments; i++)
        {
            var angle = Math.PI * 2 * i / segments;
            mesh.Positions.Add(new Point3D(center.X + Math.Cos(angle) * radius, topCenter.Y, center.Z + Math.Sin(angle) * radius));
            mesh.Positions.Add(new Point3D(center.X + Math.Cos(angle) * radius, bottomCenter.Y, center.Z + Math.Sin(angle) * radius));
        }

        for (var i = 0; i < segments; i++)
        {
            var next = (i + 1) % segments;
            var top = 2 + i * 2;
            var bottom = top + 1;
            var nextTop = 2 + next * 2;
            var nextBottom = nextTop + 1;

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(top);
            mesh.TriangleIndices.Add(nextTop);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(nextBottom);
            mesh.TriangleIndices.Add(bottom);
            mesh.TriangleIndices.Add(top);
            mesh.TriangleIndices.Add(bottom);
            mesh.TriangleIndices.Add(nextBottom);
            mesh.TriangleIndices.Add(top);
            mesh.TriangleIndices.Add(nextBottom);
            mesh.TriangleIndices.Add(nextTop);
        }

        return new GeometryModel3D(mesh, material) { BackMaterial = backMaterial ?? material };
    }

    private static void AddFace(MeshGeometry3D mesh, int a, int b, int c, int d)
    {
        mesh.TriangleIndices.Add(a);
        mesh.TriangleIndices.Add(b);
        mesh.TriangleIndices.Add(c);
        mesh.TriangleIndices.Add(a);
        mesh.TriangleIndices.Add(c);
        mesh.TriangleIndices.Add(d);
    }

    private static Material Material(string color, double opacity = 1)
    {
        var parsed = (Color)ColorConverter.ConvertFromString(color);
        parsed.A = (byte)Math.Round(255 * Math.Clamp(opacity, 0, 1));
        return new DiffuseMaterial(new SolidColorBrush(parsed));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
