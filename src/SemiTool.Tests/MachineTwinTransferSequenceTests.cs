using SemiTool.Domain;

namespace SemiTool.Tests;

public sealed class MachineTwinTransferSequenceTests
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
    public void TransferTimeline_UsesNonInstantMechanicalDurations()
    {
        var steps = CreateTransferSteps();

        Assert.All(steps, step => Assert.True(step.DelayMs >= 300, $"{step.StepName} had a fast/zero delay."));
        Assert.Contains(steps, step => step.StepName.Contains("Door Opening", StringComparison.Ordinal) && step.DelayMs >= 1000);
        Assert.Contains(steps, step => step.StepName.Contains("Blade Entering", StringComparison.Ordinal) && step.DelayMs >= 900);
        Assert.Contains(steps, step => step.StepName.Contains("Processing", StringComparison.Ordinal) && step.DelayMs >= 2500);
    }

    [Fact]
    public void TransferSequence_StartsHomeThenMovesZToFoupSlotBeforeBladeExtends()
    {
        var steps = CreateTransferSteps();

        Assert.Equal("Home / Start", steps[0].CurrentStation);
        Assert.Equal(-130, steps[0].VisualThetaAngle);

        for (var slot = 1; slot <= 5; slot++)
        {
            var moveToFoup = steps.First(step => step.StepName == $"Move To FOUP A Slot A{slot}");
            var zWorkAtSlot = steps.First(step => step.StepName == $"Z Work At FOUP A Slot A{slot}");
            var bladeEnteringSlot = steps.First(step => step.StepName == $"Blade Extending Into FOUP A Slot A{slot}");

            Assert.Equal("Z Safe", moveToFoup.ZState);
            Assert.Equal(nameof(BladeSequenceState.Retracted), moveToFoup.BladeState);
            Assert.Equal($"Z Work / FOUP A Slot A{slot}", zWorkAtSlot.ZState);
            Assert.Equal(nameof(BladeSequenceState.Retracted), zWorkAtSlot.BladeState);
            Assert.True(zWorkAtSlot.StepIndex < bladeEnteringSlot.StepIndex);
            Assert.Equal(nameof(BladeSequenceState.Extending), bladeEnteringSlot.BladeState);
        }
    }

    [Fact]
    public void BladeCanEnterChamberOnlyWhenDoorIsOpen()
    {
        var steps = CreateTransferSteps();
        var chamberBladeSteps = steps.Where(step =>
            IsChamberStation(step.StationKey) &&
            step.BladeState is nameof(BladeSequenceState.Extending) or nameof(BladeSequenceState.Extended));

        Assert.NotEmpty(chamberBladeSteps);
        Assert.All(chamberBladeSteps, step => Assert.Equal(nameof(ChamberDoorSequenceState.Open), DoorStateForStation(step)));
    }

    [Fact]
    public void ChamberDoorDoesNotCloseWhileBladeIsExtended()
    {
        var steps = CreateTransferSteps();
        var closingSteps = steps.Where(step =>
            step.ChamberADoorState == nameof(ChamberDoorSequenceState.Closing) ||
            step.ChamberBDoorState == nameof(ChamberDoorSequenceState.Closing) ||
            step.ChamberCDoorState == nameof(ChamberDoorSequenceState.Closing));

        Assert.NotEmpty(closingSteps);
        Assert.All(closingSteps, step =>
        {
            Assert.NotEqual(nameof(BladeSequenceState.Extending), step.BladeState);
            Assert.NotEqual(nameof(BladeSequenceState.Extended), step.BladeState);
        });
    }

    [Fact]
    public void ChamberProcessRunsOnlyWithDoorClosed()
    {
        var steps = CreateTransferSteps();

        Assert.All(steps.Where(step => step.ChamberA.ProcessState == "Processing"),
            step => Assert.Equal(nameof(ChamberDoorSequenceState.Closed), step.ChamberADoorState));
        Assert.All(steps.Where(step => step.ChamberB.ProcessState == "Processing"),
            step => Assert.Equal(nameof(ChamberDoorSequenceState.Closed), step.ChamberBDoorState));
        Assert.All(steps.Where(step => step.ChamberC.ProcessState == "Processing"),
            step => Assert.Equal(nameof(ChamberDoorSequenceState.Closed), step.ChamberCDoorState));
    }

    [Fact]
    public void ChamberUnloadHappensOnlyAfterProcessComplete()
    {
        var steps = CreateTransferSteps().Where(step =>
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
        var pickingSteps = CreateTransferSteps().Where(step => step.RobotState == nameof(RobotSequenceState.Picking));

        Assert.NotEmpty(pickingSteps);
        Assert.All(pickingSteps, step => Assert.Equal(nameof(VacuumSequenceState.SuctionOn), step.VacuumSequenceState));
    }

    [Fact]
    public void WaferPlacementRequiresVacuumReleaseOrExhaust()
    {
        var placingSteps = CreateTransferSteps().Where(step => step.RobotState == nameof(RobotSequenceState.Placing));

        Assert.NotEmpty(placingSteps);
        Assert.All(placingSteps, step => Assert.Equal(nameof(VacuumSequenceState.ExhaustOrRelease), step.VacuumSequenceState));
    }

    [Fact]
    public void TransferZWorkSubstepsCommandWorkPositionThroughTypedFlag()
    {
        var steps = CreateTransferSteps();
        var zWorkSubsteps = steps.Where(step => step.ZState.StartsWith("Z Work /", StringComparison.Ordinal)).ToArray();

        Assert.NotEmpty(zWorkSubsteps);
        Assert.All(zWorkSubsteps, step => Assert.True(step.IsZWorkPosition, $"{step.StepName} should command Z Work."));
        Assert.False(CreateResetStep().IsZWorkPosition);

        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "ViewModels", "MachineTwinViewModel.cs");
        Assert.Contains("step.IsZWorkPosition ? pose.ZWork : pose.ZSafe", source);
        Assert.DoesNotContain("string.Equals(step.ZState, \"Z Work\"", source);
    }

    [Fact]
    public void TransferVacuumOutputsFollowExplicitVacuumSequenceState()
    {
        var steps = CreateTransferSteps();
        var off = steps.First(step => step.VacuumSequenceState == nameof(VacuumSequenceState.Off));
        var suction = steps.First(step => step.VacuumSequenceState == nameof(VacuumSequenceState.SuctionOn));
        var exhaust = steps.First(step => step.VacuumSequenceState == nameof(VacuumSequenceState.ExhaustOrRelease));

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
    public void EveryTransferSnapshotContainsExactlyFiveUniqueWaferIds()
    {
        var expected = new[] { "W01", "W02", "W03", "W04", "W05" };

        Assert.All(CreateTransferSteps(), step =>
        {
            var actual = WaferIdsInStep(step).OrderBy(wafer => wafer).ToArray();
            Assert.Equal(expected, actual);
        });
    }

    [Fact]
    public void FinalTransferStateCompletesAllFiveWafersInFoupB()
    {
        var complete = CreateTransferSteps().Single(step => step.PipelineState == PipelineStateKind.Completed.ToString());

        Assert.Equal(MachineTwinSequencePlan.CompletedStepName, complete.StepName);
        Assert.Equal(0, complete.FoupACount);
        Assert.Equal(5, complete.FoupBCount);
        Assert.False(complete.ChamberA.HasWafer);
        Assert.False(complete.ChamberB.HasWafer);
        Assert.False(complete.ChamberC.HasWafer);
        Assert.False(complete.IsWaferOnBlade);
        Assert.Equal(nameof(VacuumSequenceState.Off), complete.VacuumSequenceState);
        Assert.Equal(nameof(ChamberDoorSequenceState.Closed), complete.ChamberADoorState);
        Assert.Equal(nameof(ChamberDoorSequenceState.Closed), complete.ChamberBDoorState);
        Assert.Equal(nameof(ChamberDoorSequenceState.Closed), complete.ChamberCDoorState);
    }

    [Fact]
    public void RuntimeTransferPlaybackStopsAtCompletedState()
    {
        var steps = CreateTransferSteps();
        var final = steps[^1];

        Assert.Equal(MachineTwinSequencePlan.CompletedStepName, final.StepName);
        Assert.Equal(PipelineStateKind.Completed.ToString(), final.PipelineState);
        Assert.DoesNotContain(steps, step => step.StepName == MachineTwinSequencePlan.ResetStepName);
    }

    [Fact]
    public void CompletedTransferPlaybackHoldsFoupBFullUntilManualReset()
    {
        var final = CreateTransferSteps()[^1];

        Assert.Equal(MachineTwinSequencePlan.CompletedStepName, final.StepName);
        Assert.Equal(0, final.FoupACount);
        Assert.Equal(5, final.FoupBCount);
        Assert.All(final.FoupASlots, slot => Assert.Equal("Empty", slot.State));
        Assert.All(final.FoupBSlots, slot =>
        {
            Assert.True(slot.HasWafer);
            Assert.Equal("Completed", slot.State);
        });
        Assert.False(final.ChamberA.HasWafer);
        Assert.False(final.ChamberB.HasWafer);
        Assert.False(final.ChamberC.HasWafer);
        Assert.False(final.IsWaferOnBlade);
        Assert.Equal(nameof(VacuumSequenceState.Off), final.VacuumSequenceState);
        Assert.True(final.TowerGreen);
        Assert.Equal(nameof(ChamberDoorSequenceState.Closed), final.ChamberADoorState);
        Assert.Equal(nameof(ChamberDoorSequenceState.Closed), final.ChamberBDoorState);
        Assert.Equal(nameof(ChamberDoorSequenceState.Closed), final.ChamberCDoorState);
    }

    [Fact]
    public void ResetTransferStateRestoresFoupAAndClearsMachine()
    {
        var reset = CreateResetStep();

        Assert.Equal(MachineTwinSequencePlan.ResetStepName, reset.StepName);
        Assert.Equal(5, reset.FoupACount);
        Assert.Equal(0, reset.FoupBCount);
        Assert.False(reset.ChamberA.HasWafer);
        Assert.False(reset.ChamberB.HasWafer);
        Assert.False(reset.ChamberC.HasWafer);
        Assert.False(reset.IsWaferOnBlade);
        Assert.Equal(nameof(VacuumSequenceState.Off), reset.VacuumSequenceState);
        Assert.All(reset.FoupASlots, slot => Assert.Equal("Waiting", slot.State));
        Assert.All(reset.FoupBSlots, slot => Assert.Equal("Empty", slot.State));
    }

    [Fact]
    public void MachineTwinView_UsesFieldHmiSequenceControls()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "Views", "MachineTwinView.xaml");

        Assert.Contains("Run Transfer Sequence", source);
        Assert.Contains("Wafer Transfer 3D Machine Twin", source);
        Assert.Contains("MachineTwin3DView", source);
        Assert.Contains("FoupACount=\"{Binding FoupACount}\"", source);
        Assert.Contains("FoupBCount=\"{Binding FoupBCount}\"", source);
        Assert.Contains("FoupASlotMask=\"{Binding FoupASlotMask}\"", source);
        Assert.Contains("FoupBSlotMask=\"{Binding FoupBSlotMask}\"", source);
        Assert.Contains("ActiveStationKey=\"{Binding ActiveStationKey}\"", source);
        Assert.Contains("ActiveSlotLevel=\"{Binding ActiveSlotLevel}\"", source);
        Assert.Contains("WaferOnBlade=\"{Binding IsWaferOnBlade}\"", source);
        Assert.Contains("WaferInChamberA=\"{Binding IsWaferInChamberA}\"", source);
        Assert.Contains("WaferInChamberB=\"{Binding IsWaferInChamberB}\"", source);
        Assert.Contains("WaferInChamberC=\"{Binding IsWaferInChamberC}\"", source);
        Assert.Contains("Current Sequence Step", source);
        Assert.Contains("Sequence Speed", source);
        Assert.Contains("OperationWafer", source);
        Assert.Contains("OperationSource", source);
        Assert.Contains("OperationDestination", source);
        Assert.Contains("PauseCommand", source);
        Assert.Contains("ResumeCommand", source);
        Assert.Contains("StepOnceCommand", source);
        Assert.Contains("CurrentAction", source);
        Assert.DoesNotContain("Run " + "Teach" + "ing " + "Demo", source);
        Assert.DoesNotContain("Run Simulator " + "Demo", source);
    }

    [Fact]
    public void MachineTwin3DView_AlignsStationDirectionAndFoupSlotHeight()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "Controls", "MachineTwin3DView.cs");

        Assert.Contains("90 - RobotAngle", source);
        Assert.Contains("AngleFacingOrigin(chamberA)", source);
        Assert.Contains("AngleFacingOrigin(foupA)", source);
        Assert.Contains("0.32 - index * 0.16", source);
        Assert.Contains("SlotLiftOffset(ActiveSlotLevel)", source);
        Assert.Contains("ResolveBladeExtensionScale()", source);
        Assert.Contains("CanVisuallyExtendIntoStation()", source);
        Assert.Contains("_motionRevision", source);
        Assert.Contains("isStationTurn", source);
        Assert.Contains("liftDelay = isStationTurn ? 520 : 0", source);
        Assert.Contains("extensionDelay = liftDelay + (needsSlotLift ? 380 : 0)", source);
        Assert.Contains("RunMotionStage(motionRevision, animated ? liftDelay : 0", source);
        Assert.Contains("RunMotionStage(motionRevision, animated ? extensionDelay : 0", source);
        Assert.Contains("if (motionRevision != _motionRevision)", source);
        Assert.Contains("AngleDelta(", source);
        Assert.Contains("string.Equals(ActiveStationKey, \"Home\"", source);
        Assert.Contains("\"FoupA\" or \"FoupB\" => 2.08", source);
        Assert.Contains("\"ChamberA\" or \"ChamberC\" => 1.66", source);
        Assert.Contains("\"ChamberB\" => 1.14", source);
        Assert.Contains("UpdateFoupWafers(_foupAWafers, FoupASlotMask, FoupACount)", source);
        Assert.Contains("UpdateChamberButton(_chamberAButton, ChamberADoorOpen)", source);
        Assert.Contains("WaferInChamberAProperty", source);
        Assert.Contains("_chamberAWafer", source);
        Assert.Contains("UpdateChamberWafer(_chamberAWafer, WaferInChamberA, ChamberADoorOpen)", source);
    }

    [Fact]
    public void MachineTwinViewModel_HoldsManualStepLongEnoughToShowMotionOrder()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "ViewModels", "MachineTwinViewModel.cs");

        Assert.Contains("var step = _sequenceSteps[index]", source);
        Assert.Contains("Task.Delay(GetManualStepVisualDelay(step)", source);
        Assert.Contains("Math.Clamp(GetRuntimeDelayForSelectedSpeed(step), 650, 1100)", source);
    }

    [Fact]
    public void SequenceStepsPopulateCurrentActionAndTransferFields()
    {
        var representativeSteps = CreateTransferSteps()
            .Where(step => !string.IsNullOrWhiteSpace(step.ActiveWaferId))
            .Take(10)
            .ToArray();

        Assert.NotEmpty(representativeSteps);
        Assert.All(representativeSteps, step =>
        {
            Assert.False(string.IsNullOrWhiteSpace(step.CurrentAction));
            Assert.False(string.IsNullOrWhiteSpace(step.ActiveWaferId));
            Assert.True(
                step.CurrentTransferDescription.Contains("->", StringComparison.Ordinal) ||
                step.CurrentTransferDescription.Contains("process", StringComparison.OrdinalIgnoreCase) ||
                step.CurrentTransferDescription.Contains("Complete", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void CompletedStateUsesSequenceCompleteTerminology()
    {
        var complete = CreateTransferSteps().Single(step => step.PipelineState == PipelineStateKind.Completed.ToString());

        Assert.Contains("Sequence Complete", complete.StepName, StringComparison.Ordinal);
        Assert.Equal(5, complete.FoupBCount);
        Assert.Contains("FOUP B", complete.StepName, StringComparison.Ordinal);
    }

    [Fact]
    public void SchedulerPriority_RemainsDownstreamFirst()
    {
        var state = WaferPipelineSimulator.CreateInitial(SimulatorTimingProfile.Normal) with
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

    private static IReadOnlyList<MachineTwinSequenceStep> CreateTransferSteps() =>
        MachineTwinSequencePlan.Create(
            DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load()),
            SimulatorTimingProfile.Normal);

    private static MachineTwinSequenceStep CreateResetStep() =>
        MachineTwinSequencePlan.CreateResetStep(
            DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load()),
            SimulatorTimingProfile.Normal);

    private static bool IsChamberStation(string stationKey) =>
        stationKey is "ChamberA" or "ChamberB" or "ChamberC";

    private static string DoorStateForStation(MachineTwinSequenceStep step) => step.StationKey switch
    {
        "ChamberA" => step.ChamberADoorState,
        "ChamberB" => step.ChamberBDoorState,
        "ChamberC" => step.ChamberCDoorState,
        _ => string.Empty
    };

    private static IEnumerable<string> WaferIdsInStep(MachineTwinSequenceStep step) =>
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
