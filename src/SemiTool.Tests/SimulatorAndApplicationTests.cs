using System.Text.RegularExpressions;
using SemiTool.Application;
using SemiTool.Domain;
using SemiTool.Hardware;

namespace SemiTool.Tests;

public sealed class SimulatorAndApplicationTests
{
    [Fact]
    public async Task Simulator_CanConnectAndDisconnect()
    {
        var controller = new SimulatedEthercatController(TestProfile.Load());

        await controller.ConnectAsync();
        Assert.True(controller.IsConnected);

        await controller.DisconnectAsync();
        Assert.False(controller.IsConnected);
    }

    [Fact]
    public async Task Simulator_CanSetAndReadOutputs()
    {
        var controller = new SimulatedEthercatController(TestProfile.Load());
        await controller.ConnectAsync();

        await controller.WriteDigitalOutputAsync(IoPoint.TowerGreen, true);
        var outputs = await controller.ReadAllOutputsAsync();

        Assert.True(outputs[IoPoint.TowerGreen]);
    }

    [Fact]
    public async Task Simulator_CanToggleAndReadInputs()
    {
        var controller = new SimulatedEthercatController(TestProfile.Load());
        await controller.ConnectAsync();

        await controller.SetInputAsync(IoPoint.ChamberADoorOpenSensor, true);

        Assert.True(await controller.ReadDigitalInputAsync(IoPoint.ChamberADoorOpenSensor));
    }

    [Fact]
    public async Task ManualCommand_IsBlockedWhileAutoSequenceIsRunning()
    {
        var services = TestServices.Create();
        await services.Controller.ConnectAsync();
        services.Safety.MarkConnected();
        await services.Controller.ServoOnAsync();
        await services.Controller.HomeAxisAsync(AxisId.Z);
        await services.Controller.HomeAxisAsync(AxisId.Theta);
        services.Safety.MarkHomed(AxisId.Z);
        services.Safety.MarkHomed(AxisId.Theta);
        services.Safety.BeginAuto(services.Controller);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => services.Sequence.SetOutputAsync(IoPoint.TowerRed, true));
    }

    [Fact]
    public async Task Timeout_CreatesAlarm()
    {
        var baseProfile = TestProfile.Load();
        var fastProfile = TestProfile.WithCylinderTimeout(baseProfile, 50);
        var controller = new SimulatedEthercatController(fastProfile, autoCompleteActuators: false);
        var alarms = new AlarmService();
        var events = new EventLogService();
        var safety = new SafetyInterlockService(alarms, events);
        var sequence = new EquipmentSequenceService(controller, fastProfile, safety, alarms, events);

        await controller.ConnectAsync();

        await Assert.ThrowsAsync<TimeoutException>(() => sequence.CylinderForwardAsync());
        Assert.Contains(alarms.ActiveAlarms, alarm => alarm.Code == AlarmCode.CylinderTimeout);
    }

    [Fact]
    public void ApplicationServices_DoNotUseRawDoDiMagicNumberCalls()
    {
        var repoRoot = TestProfile.FindRepositoryRoot();
        var applicationFiles = Directory.GetFiles(
            Path.Combine(repoRoot, "src", "SemiTool.Application"),
            "*.cs",
            SearchOption.AllDirectories);

        var source = string.Join(Environment.NewLine, applicationFiles.Select(File.ReadAllText));
        Assert.DoesNotMatch(new Regex(@"WriteDigitalOutputAsync\s*\(\s*\d", RegexOptions.Multiline), source);
        Assert.DoesNotMatch(new Regex(@"ReadDigitalInputAsync\s*\(\s*\d", RegexOptions.Multiline), source);
        Assert.DoesNotContain("DigitalOutput(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(string.Concat("Thread", ".", "Sleep"), source, StringComparison.Ordinal);
    }
}
