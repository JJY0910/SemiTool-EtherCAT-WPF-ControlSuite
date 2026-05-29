using SemiTool.Application;
using SemiTool.Domain;

namespace SemiTool.Tests;

public sealed class UiCaptureStartupTests
{
    [Fact]
    public async Task SimulatorStartup_DoesNotInvokeRealHardwareFactory()
    {
        var profile = TestProfile.Load();
        var realFactoryInvoked = false;
        var controller = new SelectableEthercatController(
            profile,
            (_, _) =>
            {
                realFactoryInvoked = true;
                throw new InvalidOperationException("Real hardware factory must not run in simulator startup.");
            });

        await controller.ConnectAsync();

        Assert.Equal(OperatingMode.Simulator, controller.Mode);
        Assert.True(controller.IsConnected);
        Assert.False(realFactoryInvoked);
    }

    [Fact]
    public void CaptureModeRuntime_CanConstructSimulatorServicesWithoutRealHardwareFactory()
    {
        var profile = TestProfile.Load();
        var realFactoryInvoked = false;
        var controller = new SelectableEthercatController(
            profile,
            (_, _) =>
            {
                realFactoryInvoked = true;
                throw new InvalidOperationException("Capture mode must not construct real hardware.");
            });
        var runtime = CreateRuntime(profile, controller);
        var mainViewModelSource = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "ViewModels", "MainViewModel.cs");

        Assert.Equal(OperatingMode.Simulator, runtime.Controller.Mode);
        Assert.Contains("MachineTwin = new MachineTwinViewModel(runtime);", mainViewModelSource);
        Assert.False(realFactoryInvoked);
    }

    [Fact]
    public void RealHardware_IsConnected_DoesNotInvokeFactory()
    {
        var profile = TestProfile.Load();
        var realFactoryInvoked = false;
        var controller = new SelectableEthercatController(
            profile,
            (_, _) =>
            {
                realFactoryInvoked = true;
                throw new InvalidOperationException("Reading IsConnected must not construct real hardware.");
            });

        controller.SetMode(OperatingMode.RealHardware);

        Assert.False(controller.IsConnected);
        Assert.False(realFactoryInvoked);
    }

    [Fact]
    public async Task RealHardware_BuildStatus_DoesNotInvokeFactoryBeforeConnect()
    {
        var profile = TestProfile.Load();
        var realFactoryInvoked = false;
        var controller = new SelectableEthercatController(
            profile,
            (_, _) =>
            {
                realFactoryInvoked = true;
                throw new InvalidOperationException("Status reads must not construct real hardware.");
            });
        controller.ConfigureRealHardware("C:\\local\\IEG3268_Dll.dll", hardwareUnlocked: false);
        controller.SetMode(OperatingMode.RealHardware);
        var runtime = CreateRuntime(profile, controller);

        var status = await runtime.BuildStatusAsync();

        Assert.Equal(OperatingMode.RealHardware, status.Mode);
        Assert.False(status.IsConnected);
        Assert.False(realFactoryInvoked);
    }

    [Fact]
    public async Task RealHardware_ConnectWithoutUnlock_DoesNotInvokeFactory()
    {
        var profile = TestProfile.Load();
        var realFactoryInvoked = false;
        var controller = new SelectableEthercatController(
            profile,
            (_, _) =>
            {
                realFactoryInvoked = true;
                throw new InvalidOperationException("Connect without unlock must not construct real hardware.");
            });

        controller.ConfigureRealHardware("C:\\local\\IEG3268_Dll.dll", hardwareUnlocked: false);
        controller.SetMode(OperatingMode.RealHardware);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.ConnectAsync());
        Assert.False(realFactoryInvoked);
    }

    [Fact]
    public async Task RealHardware_DisconnectBeforeConnect_DoesNotInvokeFactory()
    {
        var profile = TestProfile.Load();
        var realFactoryInvoked = false;
        var controller = new SelectableEthercatController(
            profile,
            (_, _) =>
            {
                realFactoryInvoked = true;
                throw new InvalidOperationException("Disconnect before Connect must not construct real hardware.");
            });

        controller.SetMode(OperatingMode.RealHardware);

        await controller.DisconnectAsync();

        Assert.False(controller.IsConnected);
        Assert.False(realFactoryInvoked);
    }

    [Fact]
    public async Task RealHardware_CommandBeforeConnect_DoesNotInvokeFactoryAndThrows()
    {
        var profile = TestProfile.Load();
        var realFactoryInvoked = false;
        var controller = new SelectableEthercatController(
            profile,
            (_, _) =>
            {
                realFactoryInvoked = true;
                throw new InvalidOperationException("Commands before Connect must not construct real hardware.");
            });

        controller.SetMode(OperatingMode.RealHardware);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.ReadAllOutputsAsync());

        Assert.Contains("Click Connect before issuing commands", exception.Message, StringComparison.Ordinal);
        Assert.False(realFactoryInvoked);
    }

    [Fact]
    public async Task RealHardware_ConnectAfterUnlock_InvokesFactory()
    {
        var profile = TestProfile.Load();
        var realFactoryInvoked = false;
        var controller = new SelectableEthercatController(
            profile,
            (factoryProfile, vendorDllPath) =>
            {
                realFactoryInvoked = true;
                Assert.Same(profile, factoryProfile);
                Assert.Equal("C:\\local\\IEG3268_Dll.dll", vendorDllPath);
                return new SimulatedEthercatController(profile);
            });

        controller.ConfigureRealHardware("C:\\local\\IEG3268_Dll.dll", hardwareUnlocked: true);
        controller.SetMode(OperatingMode.RealHardware);
        await controller.ConnectAsync();

        Assert.True(realFactoryInvoked);
        Assert.True(controller.IsConnected);
    }

    [Fact]
    public void HmiCaptureStartupSource_DoesNotReferenceHardwareNamespace()
    {
        var appSource = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "App.xaml.cs");
        var runtimeSource = ReadRepositoryFile("src", "SemiTool.Application", "RuntimeCoordinator.cs");
        var applicationProject = ReadRepositoryFile("src", "SemiTool.Application", "SemiTool.Application.csproj");

        Assert.DoesNotContain("using SemiTool.Hardware", appSource, StringComparison.Ordinal);
        Assert.DoesNotContain("using SemiTool.Hardware", runtimeSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SemiTool.Hardware.csproj", applicationProject, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignerPreview_DoesNotRequireHardwareRuntimeTypes()
    {
        var designFiles = Directory.GetFiles(
            Path.Combine(TestProfile.FindRepositoryRoot(), "src", "SemiTool.Hmi.Wpf", "DesignTime"),
            "*.cs",
            SearchOption.AllDirectories);
        var combined = string.Join(Environment.NewLine, designFiles.Select(File.ReadAllText));

        Assert.DoesNotContain("SemiTool.Hardware", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectableEthercatController", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("new RuntimeCoordinator", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("RuntimeCoordinator runtime", combined, StringComparison.Ordinal);
    }

    private static RuntimeCoordinator CreateRuntime(EquipmentProfile profile, SelectableEthercatController controller)
    {
        var alarms = new AlarmService();
        var events = new EventLogService();
        var safety = new SafetyInterlockService(alarms, events);
        var recipes = new RecipeService(profile);
        var sequence = new EquipmentSequenceService(controller, profile, safety, alarms, events);
        var scheduler = new TransferScheduler(recipes);
        return new RuntimeCoordinator(profile, controller, sequence, scheduler, safety, alarms, events, recipes);
    }

    private static string ReadRepositoryFile(params string[] parts)
    {
        var path = Path.Combine(new[] { TestProfile.FindRepositoryRoot() }.Concat(parts).ToArray());
        return File.ReadAllText(path);
    }
}
