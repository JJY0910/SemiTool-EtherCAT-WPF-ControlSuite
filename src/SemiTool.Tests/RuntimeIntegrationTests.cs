namespace SemiTool.Tests;

public sealed class RuntimeIntegrationTests
{
    [Fact]
    public void MainWindow_PutsMachineTwinBeforeDashboard()
    {
        var xaml = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "MainWindow.xaml");

        var machineTwinIndex = xaml.IndexOf("Header=\"Machine Twin\"", StringComparison.Ordinal);
        var dashboardIndex = xaml.IndexOf("Header=\"Dashboard\"", StringComparison.Ordinal);

        Assert.True(machineTwinIndex >= 0, "MainWindow must expose the Machine Twin tab.");
        Assert.True(dashboardIndex >= 0, "MainWindow must keep the Dashboard tab.");
        Assert.True(machineTwinIndex < dashboardIndex, "Machine Twin should be the first/default tab before Dashboard.");
    }

    [Fact]
    public void MainWindow_BindsMachineTwinViewToMainViewModelProperty()
    {
        var xaml = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "MainWindow.xaml");

        Assert.Contains("<views:MachineTwinView DataContext=\"{Binding MachineTwin}\" />", xaml);
        Assert.Contains("SelectedIndex=\"0\"", xaml);
    }

    [Fact]
    public void MainViewModel_ExposesMachineTwinUsingSharedRuntime()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "ViewModels", "MainViewModel.cs");

        Assert.Contains("public MachineTwinViewModel MachineTwin { get; }", source);
        Assert.Contains("MachineTwin = new MachineTwinViewModel(runtime);", source);
        Assert.Contains("MachineTwin.Refresh(status);", source);
    }

    [Fact]
    public void RuntimeDebugEvidence_DocumentsMainWindowIntegration()
    {
        var captureSource = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "DemoAssetCapture.cs");

        Assert.Contains("## Runtime Integration Check", captureSource);
        Assert.Contains("00-startup-simulator.png", captureSource);
        Assert.Contains("RenderMainWindowAsync", captureSource);
        Assert.Contains("MainWindow first tab is `Machine Twin`", captureSource);
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

        throw new DirectoryNotFoundException("Could not locate repository root for runtime integration tests.");
    }
}
