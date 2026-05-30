namespace SemiTool.Tests;

public sealed class DesignerPreviewTests
{
    [Fact]
    public void MainWindowXaml_UsesDesignTimeDataContext()
    {
        var xaml = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "MainWindow.xaml");

        Assert.Contains("xmlns:d=", xaml);
        Assert.Contains("xmlns:mc=", xaml);
        Assert.Contains("DesignMainViewModel", xaml);
        Assert.Contains("d:DataContext", xaml);
    }

    [Fact]
    public void MachineTwinViewXaml_UsesDesignTimeDataContext()
    {
        var xaml = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "Views", "MachineTwinView.xaml");

        Assert.Contains("DesignMachineTwinViewModel", xaml);
        Assert.Contains("d:DataContext", xaml);
        Assert.Contains("mc:Ignorable=\"d\"", xaml);
    }

    [Fact]
    public void MainWindowDesignerPreview_KeepsMachineTwinBeforeDashboard()
    {
        var xaml = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "MainWindow.xaml");

        var machineTwinIndex = xaml.IndexOf("Header=\"Machine Twin\"", StringComparison.Ordinal);
        var dashboardIndex = xaml.IndexOf("Header=\"Dashboard\"", StringComparison.Ordinal);

        Assert.True(machineTwinIndex >= 0);
        Assert.True(dashboardIndex > machineTwinIndex);
        Assert.Contains("<views:MachineTwinView DataContext=\"{Binding MachineTwin}\" />", xaml);
    }

    [Fact]
    public void DesignMachineTwinViewModel_IsStaticPreviewWithoutRuntimeCoordinator()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "DesignTime", "DesignMachineTwinViewModel.cs");

        Assert.Contains("public sealed class DesignMachineTwinViewModel", source);
        Assert.Contains("public string ModeLabel => \"SIMULATOR\";", source);
        Assert.Contains("public string ConnectionLabel => \"Designer\";", source);
        Assert.Contains("RunTransferSequenceCommand = CreateNoOpCommand();", source);
        Assert.Contains("EmergencyStopCommand = CreateNoOpCommand();", source);
        Assert.Contains("SelectedSequenceSpeed { get; set; } = \"Normal\";", source);
        Assert.Contains("OperationWafer", source);
        Assert.Contains("OperationSource", source);
        Assert.Contains("OperationDestination", source);
        Assert.DoesNotContain("new RuntimeCoordinator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Run " + "Teach" + "ing " + "Demo", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignMachineTwinViewModel_ExposesFiveFoupASlots()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "DesignTime", "DesignMachineTwinData.cs");

        Assert.Contains("new(\"A1\", false, string.Empty, \"Empty\", false)", source);
        Assert.Contains("new(\"A2\", false, string.Empty, \"Empty\", false)", source);
        Assert.Contains("new(\"A3\", false, string.Empty, \"Empty\", false)", source);
        Assert.Contains("new(\"A4\", false, string.Empty, \"Empty\", false)", source);
        Assert.Contains("new(\"A5\", true, \"W05\", \"Waiting\", true)", source);
    }

    [Fact]
    public void DesignMachineTwinViewModel_ExposesFiveFoupBSlots()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "DesignTime", "DesignMachineTwinData.cs");

        Assert.Contains("new(\"B1\", true, \"W01\", \"Completed\", false)", source);
        Assert.Contains("new(\"B2\", false, string.Empty, \"Empty\", false)", source);
        Assert.Contains("new(\"B3\", false, string.Empty, \"Empty\", false)", source);
        Assert.Contains("new(\"B4\", false, string.Empty, \"Empty\", false)", source);
        Assert.Contains("new(\"B5\", false, string.Empty, \"Empty\", false)", source);
    }

    [Fact]
    public void DesignMachineTwinViewModel_ExposesChamberSampleStates()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "DesignTime", "DesignMachineTwinViewModel.cs");

        Assert.Contains("\"PreClean_Default\"", source);
        Assert.Contains("\"CMP_Main\"", source);
        Assert.Contains("\"PostClean_Dry\"", source);
        Assert.Contains("\"W04\"", source);
        Assert.Contains("\"W03\"", source);
        Assert.Contains("\"W02\"", source);
    }

    [Fact]
    public void DesignMachineTwinViewModel_PreviewKeepsExactlyFiveUniqueWafers()
    {
        var dataSource = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "DesignTime", "DesignMachineTwinData.cs");
        var viewModelSource = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "DesignTime", "DesignMachineTwinViewModel.cs");

        Assert.Contains("new(\"B1\", true, \"W01\", \"Completed\", false)", dataSource);
        Assert.Contains("\"W02\"", viewModelSource);
        Assert.Contains("\"W03\"", viewModelSource);
        Assert.Contains("\"W04\"", viewModelSource);
        Assert.Contains("new(\"A5\", true, \"W05\", \"Waiting\", true)", dataSource);
        Assert.Contains("public bool IsWaferOnBlade => false;", viewModelSource);
        Assert.Contains("public string WaferIdOnBlade => string.Empty;", viewModelSource);
        Assert.Contains("Five unique wafers total", viewModelSource);
    }

    [Fact]
    public void DesignTimeFiles_DoNotRequireVendorDll()
    {
        var designDirectory = Path.Combine(FindRepositoryRoot(), "src", "SemiTool.Hmi.Wpf", "DesignTime");
        var combined = string.Join(
            Environment.NewLine,
            Directory.GetFiles(designDirectory, "*.cs").Select(File.ReadAllText));

        Assert.DoesNotContain("using SemiTool.Application", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("RuntimeCoordinator runtime", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Assembly.LoadFrom", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("VendorDllResolver", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfigureRealHardware", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectAsync", combined, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(params string[] parts)
    {
        var path = Path.Combine(new[] { FindRepositoryRoot() }.Concat(parts).ToArray());
        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SemiTool.EtherCAT.WPF.ControlSuite.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root for designer preview tests.");
    }
}
