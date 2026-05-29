using SemiTool.Hmi.Wpf.DesignTime;

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
    public void DesignMachineTwinViewModel_IsCreatableWithoutRuntimeCoordinator()
    {
        var viewModel = new DesignMachineTwinViewModel();

        Assert.Equal("SIMULATOR", viewModel.ModeLabel);
        Assert.Equal("Designer", viewModel.ConnectionLabel);
        Assert.NotNull(viewModel.RunSimulatorDemoCommand);
        Assert.NotNull(viewModel.EmergencyStopCommand);
    }

    [Fact]
    public void DesignMachineTwinViewModel_ExposesFiveFoupASlots()
    {
        var viewModel = new DesignMachineTwinViewModel();

        Assert.Equal(5, viewModel.FoupASlots.Count);
        Assert.Equal(["A1", "A2", "A3", "A4", "A5"], viewModel.FoupASlots.Select(slot => slot.Label).ToArray());
        Assert.Equal(["W01", "W02", "W03", "W04", "W05"], viewModel.FoupASlots.Select(slot => slot.WaferId).ToArray());
    }

    [Fact]
    public void DesignMachineTwinViewModel_ExposesFiveFoupBSlots()
    {
        var viewModel = new DesignMachineTwinViewModel();

        Assert.Equal(5, viewModel.FoupBSlots.Count);
        Assert.Equal(["B1", "B2", "B3", "B4", "B5"], viewModel.FoupBSlots.Select(slot => slot.Label).ToArray());
        Assert.Contains(viewModel.FoupBSlots, slot => slot.State == "Completed");
    }

    [Fact]
    public void DesignMachineTwinViewModel_ExposesChamberSampleStates()
    {
        var viewModel = new DesignMachineTwinViewModel();

        Assert.Equal("PreClean_Default", viewModel.ChamberA.RecipeName);
        Assert.Equal("CMP_Main", viewModel.ChamberB.RecipeName);
        Assert.Equal("PostClean_Dry", viewModel.ChamberC.RecipeName);
        Assert.True(viewModel.ChamberA.HasWafer);
        Assert.True(viewModel.ChamberA.ProgressPercent > 0);
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
