using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace SemiTool.EtherCAT.ControlSuite.Controls;

public sealed class MachineTwin3DView : Viewport3D
{
    public static readonly DependencyProperty RobotAngleProperty =
        DependencyProperty.Register(
            nameof(RobotAngle),
            typeof(double),
            typeof(MachineTwin3DView),
            new PropertyMetadata(0d, OnRobotMotionChanged));

    public static readonly DependencyProperty BladeLengthProperty =
        DependencyProperty.Register(
            nameof(BladeLength),
            typeof(double),
            typeof(MachineTwin3DView),
            new PropertyMetadata(86d, OnRobotMotionChanged));

    public static readonly DependencyProperty VacuumOnProperty =
        DependencyProperty.Register(
            nameof(VacuumOn),
            typeof(bool),
            typeof(MachineTwin3DView),
            new PropertyMetadata(false, OnRobotMotionChanged));

    public static readonly DependencyProperty WaferOnBladeProperty =
        DependencyProperty.Register(
            nameof(WaferOnBlade),
            typeof(bool),
            typeof(MachineTwin3DView),
            new PropertyMetadata(false, OnRobotMotionChanged));

    public static readonly DependencyProperty ChamberDoorsClosedProperty =
        DependencyProperty.Register(
            nameof(ChamberDoorsClosed),
            typeof(bool),
            typeof(MachineTwin3DView),
            new PropertyMetadata(true, OnRobotMotionChanged));

    private readonly Model3DGroup _scene = new();
    private readonly Model3DGroup _robotGroup = new();
    private readonly Model3DGroup _waferGroup = new();
    private readonly AxisAngleRotation3D _robotRotation = new(new Vector3D(0, 1, 0), 0);
    private readonly ScaleTransform3D _bladeScale = new(1, 1, 1);
    private readonly TranslateTransform3D _waferOffset = new();
    private GeometryModel3D? _suctionPad;
    private GeometryModel3D? _wafer;
    private GeometryModel3D? _chamberDoorA;
    private GeometryModel3D? _chamberDoorB;
    private GeometryModel3D? _chamberDoorC;

    public MachineTwin3DView()
    {
        ClipToBounds = true;
        Camera = new PerspectiveCamera(
            new Point3D(7.6, 5.4, 8.4),
            new Vector3D(-7.6, -4.6, -8.4),
            new Vector3D(0, 1, 0),
            38);

        Children.Add(new ModelVisual3D { Content = _scene });
        BuildScene();
        UpdateRobotMotion(animated: false);
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

    public bool ChamberDoorsClosed
    {
        get => (bool)GetValue(ChamberDoorsClosedProperty);
        set => SetValue(ChamberDoorsClosedProperty, value);
    }

    private static void OnRobotMotionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((MachineTwin3DView)dependencyObject).UpdateRobotMotion(animated: true);
    }

    private void BuildScene()
    {
        _scene.Children.Clear();

        _scene.Children.Add(new AmbientLight(Color.FromRgb(38, 54, 65)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(245, 250, 255), new Vector3D(-0.7, -1.1, -0.55)));
        _scene.Children.Add(new DirectionalLight(Color.FromRgb(87, 210, 230), new Vector3D(0.5, -0.4, 0.35)));

        AddDeck();
        AddFrame();
        AddStations();
        AddCableCarrier();
        AddRobot();
    }

    private void AddDeck()
    {
        _scene.Children.Add(CreateBox(new Point3D(0, -0.08, 0), new Size3D(8.9, 0.16, 6.1), Material("#253846"), Material("#4D7182")));
        _scene.Children.Add(CreateBox(new Point3D(0, 0.03, 0), new Size3D(8.35, 0.08, 5.55), Material("#0E2028"), Material("#395B68")));

        // 장비 원점 기준 십자선입니다. 실제 티칭 좌표가 아니라 화면용 HOME 기준 표시입니다.
        _scene.Children.Add(CreateBox(new Point3D(0, 0.09, 0), new Size3D(0.04, 0.03, 4.55), Material("#842D35"), null));
        _scene.Children.Add(CreateBox(new Point3D(0, 0.1, 0), new Size3D(4.55, 0.03, 0.04), Material("#842D35"), null));

        var routeRing = Material("#215866", 0.34);
        for (var i = 0; i < 36; i += 2)
        {
            var angle = Math.PI * 2 * i / 36d;
            var x = Math.Cos(angle) * 2.38;
            var z = Math.Sin(angle) * 2.08;
            var segment = CreateBox(new Point3D(x, 0.12, z), new Size3D(0.3, 0.04, 0.08), routeRing, null);
            segment.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -i * 10), new Point3D(x, 0.12, z));
            _scene.Children.Add(segment);
        }
    }

    private void AddFrame()
    {
        var rail = Material("#A6B4BD");
        var glass = Material("#8FD7F0", 0.18);
        var corner = Material("#303940");

        _scene.Children.Add(CreateBox(new Point3D(0, 0.22, -2.92), new Size3D(8.75, 0.18, 0.12), rail, null));
        _scene.Children.Add(CreateBox(new Point3D(0, 0.22, 2.92), new Size3D(8.75, 0.18, 0.12), rail, null));
        _scene.Children.Add(CreateBox(new Point3D(-4.42, 0.22, 0), new Size3D(0.12, 0.18, 5.85), rail, null));
        _scene.Children.Add(CreateBox(new Point3D(4.42, 0.22, 0), new Size3D(0.12, 0.18, 5.85), rail, null));

        foreach (var x in new[] { -4.25, 4.25 })
        {
            foreach (var z in new[] { -2.75, 2.75 })
            {
                _scene.Children.Add(CreateBox(new Point3D(x, 0.9, z), new Size3D(0.16, 1.7, 0.16), rail, null));
                _scene.Children.Add(CreateBox(new Point3D(x, 1.82, z), new Size3D(0.36, 0.24, 0.36), corner, null));
            }
        }

        _scene.Children.Add(CreateBox(new Point3D(0, 1.2, -2.86), new Size3D(7.9, 1.45, 0.05), glass, null));
        _scene.Children.Add(CreateBox(new Point3D(-4.12, 1.2, 0), new Size3D(0.05, 1.45, 5.1), glass, null));
        _scene.Children.Add(CreateBox(new Point3D(4.12, 1.2, 0), new Size3D(0.05, 1.45, 5.1), glass, null));
    }

    private void AddStations()
    {
        AddChamber("A", new Point3D(0, 0.55, -2.08), 0);
        AddChamber("B", new Point3D(-2.95, 0.55, -0.78), -52);
        AddChamber("C", new Point3D(2.95, 0.55, -0.78), 52);
        AddFoup("A", new Point3D(-2.75, 0.52, 2.05), "#3EF08B");
        AddFoup("B", new Point3D(2.75, 0.52, 2.05), "#F6C453");

        _scene.Children.Add(CreateTowerLight(new Point3D(-3.85, 1.05, -2.15)));
        _scene.Children.Add(CreateTowerLight(new Point3D(3.85, 1.05, -2.15)));
    }

    private void AddChamber(string name, Point3D center, double angle)
    {
        var chamber = new Model3DGroup();
        chamber.Children.Add(CreateBox(new Point3D(0, 0, 0), new Size3D(1.28, 0.95, 1.05), Material("#E8EEF2"), Material("#FFFFFF")));
        chamber.Children.Add(CreateBox(new Point3D(0, -0.05, 0.56), new Size3D(0.98, 0.42, 0.08), Material("#1F2E36"), null));
        chamber.Children.Add(CreateBox(new Point3D(0, -0.02, 0.62), new Size3D(0.8, 0.06, 0.12), Material("#BFCBD2"), null));
        chamber.Children.Add(CreateCylinder(new Point3D(0, -0.18, 0.05), 0.32, 0.09, 36, Material("#B8C7CF"), Material("#E7F0F5")));
        chamber.Children.Add(CreateBox(new Point3D(0.55, 0.05, 0.42), new Size3D(0.12, 0.12, 0.05), Material("#2BD96B"), null));

        var door = CreateBox(new Point3D(0, 0.03, 0.67), new Size3D(0.86, 0.34, 0.04), Material("#C8D8E0", 0.75), Material("#FFFFFF", 0.8));
        chamber.Children.Add(door);
        switch (name)
        {
            case "A":
                _chamberDoorA = door;
                break;
            case "B":
                _chamberDoorB = door;
                break;
            case "C":
                _chamberDoorC = door;
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

    private void AddFoup(string name, Point3D center, string accent)
    {
        var foup = new Model3DGroup();
        foup.Children.Add(CreateBox(new Point3D(0, 0, 0), new Size3D(0.95, 1.08, 0.78), Material("#060A0F"), Material("#26323A")));
        foup.Children.Add(CreateBox(new Point3D(0, -0.48, 0), new Size3D(1.18, 0.12, 0.95), Material("#B8C1C7"), null));

        for (var i = 0; i < 5; i++)
        {
            var y = -0.3 + i * 0.16;
            foup.Children.Add(CreateBox(new Point3D(0, y, 0.42), new Size3D(0.82, 0.05, 0.08), Material(accent), null));
            if (name == "A")
            {
                foup.Children.Add(CreateBox(new Point3D(0, y + 0.04, 0.18), new Size3D(0.72, 0.025, 0.38), Material("#98F5D2", 0.65), null));
            }
        }

        foup.Children.Add(CreateBox(new Point3D(0, 0.66, 0.41), new Size3D(0.78, 0.08, 0.1), Material(accent), null));
        foup.Transform = new TranslateTransform3D(center.X, center.Y, center.Z);
        _scene.Children.Add(foup);
    }

    private void AddCableCarrier()
    {
        var carrierMaterial = Material("#0B0C10");
        var tieMaterial = Material("#D94B4B");

        for (var i = 0; i < 18; i++)
        {
            var t = Math.PI * (0.15 + i / 22d);
            var x = Math.Cos(t) * 2.0;
            var z = Math.Sin(t) * 1.72;
            var link = CreateBox(new Point3D(x, 0.55, z), new Size3D(0.14, 0.16, 0.28), carrierMaterial, null);
            link.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -t * 180 / Math.PI), new Point3D(x, 0.55, z));
            _scene.Children.Add(link);

            if (i % 3 == 0)
            {
                _scene.Children.Add(CreateBox(new Point3D(x, 0.68, z), new Size3D(0.12, 0.04, 0.28), tieMaterial, null));
            }
        }
    }

    private void AddRobot()
    {
        _scene.Children.Add(CreateCylinder(new Point3D(0, 0.22, 0), 0.58, 0.28, 56, Material("#A8B8C2"), Material("#E8F2F7")));
        _scene.Children.Add(CreateCylinder(new Point3D(0, 0.45, 0), 0.38, 0.3, 56, Material("#607985"), Material("#C9D7DF")));

        var robotTransform = new Transform3DGroup
        {
            Children =
            {
                new RotateTransform3D(_robotRotation, new Point3D(0, 0.55, 0))
            }
        };
        _robotGroup.Transform = robotTransform;

        _robotGroup.Children.Add(CreateBox(new Point3D(0.42, 0.72, 0), new Size3D(0.95, 0.22, 0.54), Material("#DDE8EE"), Material("#FFFFFF")));
        _robotGroup.Children.Add(CreateBox(new Point3D(0.7, 0.9, 0), new Size3D(0.48, 0.16, 0.48), Material("#A8B7BF"), null));

        var bladeGroup = new Model3DGroup
        {
            Transform = _bladeScale
        };
        bladeGroup.Children.Add(CreateBox(new Point3D(0.92, 0.62, 0), new Size3D(1.15, 0.08, 0.34), Material("#EDF6FA"), Material("#FFFFFF")));
        bladeGroup.Children.Add(CreateBox(new Point3D(0.92, 0.66, -0.18), new Size3D(1.05, 0.05, 0.06), Material("#94A7B1"), null));
        bladeGroup.Children.Add(CreateBox(new Point3D(0.92, 0.66, 0.18), new Size3D(1.05, 0.05, 0.06), Material("#94A7B1"), null));
        _robotGroup.Children.Add(bladeGroup);

        _suctionPad = CreateCylinder(new Point3D(1.56, 0.61, 0), 0.18, 0.055, 36, Material("#5FF0C9"), Material("#D7FFF4"));
        _robotGroup.Children.Add(_suctionPad);

        _wafer = CreateCylinder(new Point3D(0, 0.67, 0), 0.31, 0.035, 56, Material("#B8F2FF", 0.82), Material("#FFFFFF", 0.88));
        _wafer.Transform = _waferOffset;
        _waferGroup.Children.Add(_wafer);
        _robotGroup.Children.Add(_waferGroup);

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

    private void UpdateRobotMotion(bool animated)
    {
        // WPF 3D 좌표계에서는 화면용 각도를 반전해야 FOUP A/B와 CHAMBER 방향이 장비 배치와 맞습니다.
        Animate(_robotRotation, AxisAngleRotation3D.AngleProperty, -RobotAngle, animated ? 420 : 0);

        var extensionScale = Math.Clamp(BladeLength / 86d, 1d, 2.65d);
        Animate(_bladeScale, ScaleTransform3D.ScaleXProperty, extensionScale, animated ? 360 : 0);
        Animate(_waferOffset, TranslateTransform3D.OffsetXProperty, 1.52 * extensionScale, animated ? 360 : 0);

        if (_suctionPad is not null)
        {
            _suctionPad.Material = VacuumOn ? Material("#4CFFD0") : Material("#607985");
            _suctionPad.BackMaterial = _suctionPad.Material;
        }

        if (_wafer is not null)
        {
            var waferMaterial = WaferOnBlade ? Material("#B8F2FF", 0.86) : Material("#B8F2FF", 0);
            _wafer.Material = waferMaterial;
            _wafer.BackMaterial = waferMaterial;
        }

        var doorMaterial = ChamberDoorsClosed ? Material("#C8D8E0", 0.72) : Material("#F04444", 0.62);
        if (_chamberDoorA is not null)
        {
            _chamberDoorA.Material = doorMaterial;
        }

        if (_chamberDoorB is not null)
        {
            _chamberDoorB.Material = doorMaterial;
        }

        if (_chamberDoorC is not null)
        {
            _chamberDoorC.Material = doorMaterial;
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

        var animation = new DoubleAnimation
        {
            To = to,
            Duration = TimeSpan.FromMilliseconds(milliseconds),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.HoldEnd
        };
        target.BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private static GeometryModel3D CreateBox(Point3D center, Size3D size, Material material, Material? backMaterial)
    {
        var x = size.X / 2d;
        var y = size.Y / 2d;
        var z = size.Z / 2d;
        var points = new[]
        {
            new Point3D(center.X - x, center.Y - y, center.Z - z),
            new Point3D(center.X + x, center.Y - y, center.Z - z),
            new Point3D(center.X + x, center.Y + y, center.Z - z),
            new Point3D(center.X - x, center.Y + y, center.Z - z),
            new Point3D(center.X - x, center.Y - y, center.Z + z),
            new Point3D(center.X + x, center.Y - y, center.Z + z),
            new Point3D(center.X + x, center.Y + y, center.Z + z),
            new Point3D(center.X - x, center.Y + y, center.Z + z)
        };

        var mesh = new MeshGeometry3D();
        foreach (var point in points)
        {
            mesh.Positions.Add(point);
        }

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
            var x = center.X + Math.Cos(angle) * radius;
            var z = center.Z + Math.Sin(angle) * radius;
            mesh.Positions.Add(new Point3D(x, topCenter.Y, z));
            mesh.Positions.Add(new Point3D(x, bottomCenter.Y, z));
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
}
