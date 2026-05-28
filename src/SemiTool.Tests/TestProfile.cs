using SemiTool.Application;
using SemiTool.Domain;
using SemiTool.Hardware;
using SemiTool.Infrastructure;

namespace SemiTool.Tests;

internal static class TestProfile
{
    public static EquipmentProfile Load() =>
        new EquipmentProfileLoader().Load(Path.Combine(AppContext.BaseDirectory, "config", "EquipmentProfile.finaltest.json"));

    public static EquipmentProfile WithCylinderTimeout(EquipmentProfile source, int timeoutMs) => new()
    {
        ProfileName = source.ProfileName,
        SourceProject = source.SourceProject,
        Hardware = source.Hardware,
        Communication = source.Communication,
        Motion = source.Motion,
        Timing = new TimingProfile
        {
            MotionWaitMs = source.Timing.MotionWaitMs,
            ExtraIntervalMs = source.Timing.ExtraIntervalMs,
            DoorWaitMs = source.Timing.DoorWaitMs,
            CylinderWaitTimeoutMs = timeoutMs,
            VacuumSuctionMs = source.Timing.VacuumSuctionMs,
            VacuumExhaustMs = source.Timing.VacuumExhaustMs,
            AutoRealTickMs = source.Timing.AutoRealTickMs,
            AutoSimTickMs = source.Timing.AutoSimTickMs
        },
        Io = source.Io,
        Poses = source.Poses,
        FoupSlots = source.FoupSlots,
        Recipes = source.Recipes
    };

    public static string FindRepositoryRoot()
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

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}

internal sealed record TestServiceBundle(
    SimulatedEthercatController Controller,
    SafetyInterlockService Safety,
    EquipmentSequenceService Sequence);

internal static class TestServices
{
    public static TestServiceBundle Create()
    {
        var profile = TestProfile.Load();
        var controller = new SimulatedEthercatController(profile);
        var alarms = new AlarmService();
        var events = new EventLogService();
        var safety = new SafetyInterlockService(alarms, events);
        var sequence = new EquipmentSequenceService(controller, profile, safety, alarms, events);
        return new TestServiceBundle(controller, safety, sequence);
    }
}
