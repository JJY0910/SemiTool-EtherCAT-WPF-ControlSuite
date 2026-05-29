using SemiTool.Domain;

namespace SemiTool.Tests;

public sealed class MachineTwinTeachingSequenceTests
{
    [Fact]
    public void RelayCommands_MarshalCanExecuteChangedThroughWpfDispatcher()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "ViewModels", "RelayCommand.cs");

        Assert.Contains("System.Windows.Application.Current?.Dispatcher", source);
        Assert.Contains("dispatcher.CheckAccess()", source);
        Assert.Contains("dispatcher.BeginInvoke", source);
        Assert.DoesNotContain("ConfigureAwait(false)", source);
    }

    [Fact]
    public void TeachingTimeline_UsesNonInstantMechanicalDurations()
    {
        var steps = CreateTeachingSteps();

        Assert.All(steps, step => Assert.True(step.DelayMs >= 300, $"{step.StepName} had a fast/zero delay."));
        Assert.Contains(steps, step => step.StepName.Contains("Door Opening", StringComparison.Ordinal) && step.DelayMs >= 1000);
        Assert.Contains(steps, step => step.StepName.Contains("Blade Entering", StringComparison.Ordinal) && step.DelayMs >= 900);
        Assert.Contains(steps, step => step.StepName.Contains("Processing", StringComparison.Ordinal) && step.DelayMs >= 2500);
    }

    [Fact]
    public void BladeCanEnterChamberOnlyWhenDoorIsOpen()
    {
        var steps = CreateTeachingSteps();
        var chamberBladeSteps = steps.Where(step =>
            IsChamberStation(step.StationKey) &&
            step.BladeState is nameof(BladeTeachingState.Extending) or nameof(BladeTeachingState.Extended));

        Assert.NotEmpty(chamberBladeSteps);
        Assert.All(chamberBladeSteps, step => Assert.Equal(nameof(ChamberDoorTeachingState.Open), DoorStateForStation(step)));
    }

    [Fact]
    public void ChamberDoorDoesNotCloseWhileBladeIsExtended()
    {
        var steps = CreateTeachingSteps();
        var closingSteps = steps.Where(step =>
            step.ChamberADoorState == nameof(ChamberDoorTeachingState.Closing) ||
            step.ChamberBDoorState == nameof(ChamberDoorTeachingState.Closing) ||
            step.ChamberCDoorState == nameof(ChamberDoorTeachingState.Closing));

        Assert.NotEmpty(closingSteps);
        Assert.All(closingSteps, step =>
        {
            Assert.NotEqual(nameof(BladeTeachingState.Extending), step.BladeState);
            Assert.NotEqual(nameof(BladeTeachingState.Extended), step.BladeState);
        });
    }

    [Fact]
    public void ChamberProcessRunsOnlyWithDoorClosed()
    {
        var steps = CreateTeachingSteps();

        Assert.All(steps.Where(step => step.ChamberA.ProcessState == "Processing"),
            step => Assert.Equal(nameof(ChamberDoorTeachingState.Closed), step.ChamberADoorState));
        Assert.All(steps.Where(step => step.ChamberB.ProcessState == "Processing"),
            step => Assert.Equal(nameof(ChamberDoorTeachingState.Closed), step.ChamberBDoorState));
        Assert.All(steps.Where(step => step.ChamberC.ProcessState == "Processing"),
            step => Assert.Equal(nameof(ChamberDoorTeachingState.Closed), step.ChamberCDoorState));
    }

    [Fact]
    public void ChamberUnloadHappensOnlyAfterProcessComplete()
    {
        var steps = CreateTeachingSteps().Where(step =>
            step.CurrentAction.Contains("for unloading", StringComparison.OrdinalIgnoreCase));

        Assert.NotEmpty(steps);
        Assert.All(steps, step =>
        {
            if (step.StationKey == "ChamberA")
            {
                Assert.Equal("Completed", step.ChamberA.ProcessState);
            }
            else if (step.StationKey == "ChamberB")
            {
                Assert.Equal("Completed", step.ChamberB.ProcessState);
            }
            else if (step.StationKey == "ChamberC")
            {
                Assert.Equal("Completed", step.ChamberC.ProcessState);
            }
        });
    }

    [Fact]
    public void WaferPickupRequiresVacuumSuction()
    {
        var pickingSteps = CreateTeachingSteps().Where(step => step.RobotState == nameof(RobotTeachingState.Picking));

        Assert.NotEmpty(pickingSteps);
        Assert.All(pickingSteps, step => Assert.Equal(nameof(VacuumTeachingState.SuctionOn), step.VacuumTeachingState));
    }

    [Fact]
    public void WaferPlacementRequiresVacuumReleaseOrExhaust()
    {
        var placingSteps = CreateTeachingSteps().Where(step => step.RobotState == nameof(RobotTeachingState.Placing));

        Assert.NotEmpty(placingSteps);
        Assert.All(placingSteps, step => Assert.Equal(nameof(VacuumTeachingState.ExhaustOrRelease), step.VacuumTeachingState));
    }

    [Fact]
    public void TeachingZWorkSubstepsCommandWorkPositionThroughTypedFlag()
    {
        var steps = CreateTeachingSteps();
        var zWorkSubsteps = steps.Where(step => step.ZState.StartsWith("Z Work /", StringComparison.Ordinal)).ToArray();

        Assert.NotEmpty(zWorkSubsteps);
        Assert.All(zWorkSubsteps, step => Assert.True(step.IsZWorkPosition, $"{step.StepName} should command Z Work."));
        Assert.False(steps.Single(step => step.StepName == "Reset Safe State").IsZWorkPosition);

        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "ViewModels", "MachineTwinViewModel.cs");
        Assert.Contains("step.IsZWorkPosition ? pose.ZWork : pose.ZSafe", source);
        Assert.DoesNotContain("string.Equals(step.ZState, \"Z Work\"", source);
    }

    [Fact]
    public void TeachingVacuumOutputsFollowExplicitVacuumTeachingState()
    {
        var steps = CreateTeachingSteps();
        var off = steps.First(step => step.VacuumTeachingState == nameof(VacuumTeachingState.Off));
        var suction = steps.First(step => step.VacuumTeachingState == nameof(VacuumTeachingState.SuctionOn));
        var exhaust = steps.First(step => step.VacuumTeachingState == nameof(VacuumTeachingState.ExhaustOrRelease));

        Assert.False(off.IsVacuumSuctionOutputOn);
        Assert.False(off.IsVacuumExhaustOutputOn);
        Assert.True(suction.IsVacuumSuctionOutputOn);
        Assert.False(suction.IsVacuumExhaustOutputOn);
        Assert.False(exhaust.IsVacuumSuctionOutputOn);
        Assert.True(exhaust.IsVacuumExhaustOutputOn);

        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "ViewModels", "MachineTwinViewModel.cs");
        Assert.Contains("step.IsVacuumSuctionOutputOn", source);
        Assert.Contains("step.IsVacuumExhaustOutputOn", source);
        Assert.DoesNotContain("!step.IsVacuumOn && !step.IsWaferOnBlade", source);
    }

    [Fact]
    public void EveryTeachingSnapshotContainsExactlyFiveUniqueWaferIds()
    {
        var expected = new[] { "W01", "W02", "W03", "W04", "W05" };

        Assert.All(CreateTeachingSteps(), step =>
        {
            var actual = WaferIdsInStep(step).OrderBy(wafer => wafer).ToArray();
            Assert.Equal(expected, actual);
        });
    }

    [Fact]
    public void FinalTeachingStateCompletesAllFiveWafersInFoupB()
    {
        var complete = CreateTeachingSteps().Single(step => step.PipelineState == PipelineStateKind.Completed.ToString());

        Assert.Equal(0, complete.FoupACount);
        Assert.Equal(5, complete.FoupBCount);
        Assert.False(complete.ChamberA.HasWafer);
        Assert.False(complete.ChamberB.HasWafer);
        Assert.False(complete.ChamberC.HasWafer);
        Assert.False(complete.IsWaferOnBlade);
        Assert.Equal(nameof(VacuumTeachingState.Off), complete.VacuumTeachingState);
        Assert.Equal(nameof(ChamberDoorTeachingState.Closed), complete.ChamberADoorState);
        Assert.Equal(nameof(ChamberDoorTeachingState.Closed), complete.ChamberBDoorState);
        Assert.Equal(nameof(ChamberDoorTeachingState.Closed), complete.ChamberCDoorState);
    }

    [Fact]
    public void ResetTeachingStateRestoresFoupAAndClearsMachine()
    {
        var reset = CreateTeachingSteps().Last();

        Assert.Equal("Reset Safe State", reset.StepName);
        Assert.Equal(5, reset.FoupACount);
        Assert.Equal(0, reset.FoupBCount);
        Assert.False(reset.ChamberA.HasWafer);
        Assert.False(reset.ChamberB.HasWafer);
        Assert.False(reset.ChamberC.HasWafer);
        Assert.False(reset.IsWaferOnBlade);
        Assert.Equal(nameof(VacuumTeachingState.Off), reset.VacuumTeachingState);
        Assert.All(reset.FoupASlots, slot => Assert.Equal("Waiting", slot.State));
        Assert.All(reset.FoupBSlots, slot => Assert.Equal("Empty", slot.State));
    }

    [Fact]
    public void MachineTwinView_UsesTeachingDemoControls()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "Views", "MachineTwinView.xaml");

        Assert.Contains("Run Teaching Demo", source);
        Assert.Contains("PauseCommand", source);
        Assert.Contains("ResumeCommand", source);
        Assert.Contains("StepOnceCommand", source);
        Assert.Contains("CurrentAction", source);
        Assert.DoesNotContain("Run Simulator Demo", source);
    }

    [Fact]
    public void SchedulerPriority_RemainsDownstreamFirst()
    {
        var state = WaferPipelineSimulator.CreateInitial(SimulatorTimingProfile.Teaching) with
        {
            ChamberA = CompletedChamber("Chamber A", "Pre-Clean", "PreClean_Default", "W03"),
            ChamberB = CompletedChamber("Chamber B", "CMP Main", "CMP_Main", "W02"),
            ChamberC = CompletedChamber("Chamber C", "Post-Clean & Dry", "PostClean_Dry", "W01")
        };

        Assert.Equal(WaferTransferPriority.ChamberCToFoupB, WaferPipelineSimulator.ChooseNextTransfer(state));

        state = state with { ChamberC = EmptyChamber("Chamber C", "Post-Clean & Dry", "PostClean_Dry") };
        Assert.Equal(WaferTransferPriority.ChamberBToChamberC, WaferPipelineSimulator.ChooseNextTransfer(state));

        state = state with { ChamberB = EmptyChamber("Chamber B", "CMP Main", "CMP_Main") };
        Assert.Equal(WaferTransferPriority.ChamberAToChamberB, WaferPipelineSimulator.ChooseNextTransfer(state));
    }

    private static IReadOnlyList<MachineTwinDemoStep> CreateTeachingSteps() =>
        MachineTwinDemoPlan.Create(
            DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load()),
            SimulatorTimingProfile.Teaching);

    private static bool IsChamberStation(string stationKey) =>
        stationKey is "ChamberA" or "ChamberB" or "ChamberC";

    private static string DoorStateForStation(MachineTwinDemoStep step) => step.StationKey switch
    {
        "ChamberA" => step.ChamberADoorState,
        "ChamberB" => step.ChamberBDoorState,
        "ChamberC" => step.ChamberCDoorState,
        _ => string.Empty
    };

    private static IEnumerable<string> WaferIdsInStep(MachineTwinDemoStep step) =>
        step.FoupASlots
            .Concat(step.FoupBSlots)
            .Where(slot => slot.HasWafer)
            .Select(slot => slot.WaferId)
            .Concat(new[] { step.ChamberA, step.ChamberB, step.ChamberC }
                .Where(chamber => chamber.HasWafer)
                .Select(chamber => chamber.WaferId))
            .Concat(step.IsWaferOnBlade ? [step.WaferIdOnBlade] : [])
            .Where(wafer => !string.IsNullOrWhiteSpace(wafer));

    private static ChamberPipelineSnapshot CompletedChamber(string name, string role, string recipe, string waferId) =>
        new(name, role, true, waferId, "Completed", recipe, "Complete", 0, 100, false);

    private static ChamberPipelineSnapshot EmptyChamber(string name, string role, string recipe) =>
        new(name, role, false, string.Empty, "Empty", recipe, "-", 0, 0, false);

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

        throw new DirectoryNotFoundException("Could not locate repository root for Machine Twin teaching tests.");
    }
}
