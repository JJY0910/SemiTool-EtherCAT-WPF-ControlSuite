using SemiTool.Domain;

namespace SemiTool.Tests;

public sealed class WaferPipelineSimulatorTests
{
    [Fact]
    public void InitialPipeline_HasFiveWafersInFoupAAndNoneInFoupB()
    {
        var state = WaferPipelineSimulator.CreateInitial(SimulatorTimingProfile.Realistic);

        Assert.Equal(5, state.FoupACount);
        Assert.Equal(0, state.FoupBCount);
        Assert.Equal(["A1", "A2", "A3", "A4", "A5"], state.FoupASlots.Select(slot => slot.SlotName).ToArray());
        Assert.Equal(["B1", "B2", "B3", "B4", "B5"], state.FoupBSlots.Select(slot => slot.SlotName).ToArray());
        Assert.Equal(["W01", "W02", "W03", "W04", "W05"], state.FoupASlots.Select(slot => slot.WaferId).ToArray());
    }

    [Fact]
    public void PickFromFoupA_RemovesOneWaferFromFoupA()
    {
        var pick = WaferPipelineSimulator.CreateDebugTimeline(SimulatorTimingProfile.Realistic)
            .Single(step => step.StepName == "W01 Pick A1");

        Assert.Equal(4, pick.FoupACount);
        Assert.DoesNotContain(pick.FoupASlots, slot => slot.WaferId == "W01");
        Assert.False(pick.FoupASlots[0].HasWafer);
        Assert.Equal("A1", pick.FoupASlots[0].SlotName);
        Assert.Equal("W02", pick.FoupASlots[1].WaferId);
        Assert.Equal("W05", pick.FoupASlots[4].WaferId);
        Assert.Equal("W01", pick.ActiveWaferId);
    }

    [Fact]
    public void FoupSlotsPreservePhysicalSlotPositionsInsteadOfCompacting()
    {
        var pipelineLoaded = WaferPipelineSimulator.CreateDebugTimeline(SimulatorTimingProfile.Realistic)
            .Single(step => step.StepName == "Three Chambers Occupied");

        Assert.Equal("Empty", pipelineLoaded.FoupASlots[0].State);
        Assert.Equal("Empty", pipelineLoaded.FoupASlots[1].State);
        Assert.Equal("Empty", pipelineLoaded.FoupASlots[2].State);
        Assert.Equal("W04", pipelineLoaded.FoupASlots[3].WaferId);
        Assert.Equal("W05", pipelineLoaded.FoupASlots[4].WaferId);
    }

    [Fact]
    public void PlaceToFoupB_IncreasesFoupBCount()
    {
        var placed = WaferPipelineSimulator.CreateDebugTimeline(SimulatorTimingProfile.Realistic)
            .Single(step => step.StepName == "W01 Placed FOUP B B1");

        Assert.Equal(1, placed.FoupBCount);
        Assert.Equal("W01", placed.FoupBSlots[0].WaferId);
        Assert.Equal("Completed", placed.FoupBSlots[0].State);
    }

    [Fact]
    public void PipelineCompletesWithFoupAEmptyFoupBFullAndChambersEmpty()
    {
        var complete = WaferPipelineSimulator.CreateDebugTimeline(SimulatorTimingProfile.Realistic)
            .Single(step => step.PipelineState == PipelineStateKind.Completed);

        Assert.Equal(0, complete.FoupACount);
        Assert.Equal(5, complete.FoupBCount);
        Assert.False(complete.ChamberA.HasWafer);
        Assert.False(complete.ChamberB.HasWafer);
        Assert.False(complete.ChamberC.HasWafer);
    }

    [Fact]
    public void SchedulerPriority_PrefersChamberCToFoupB()
    {
        var state = WaferPipelineSimulator.CreateInitial(SimulatorTimingProfile.Realistic) with
        {
            ChamberC = CompletedChamber("Chamber C", "Post-Clean & Dry", "PostClean_Dry", "W01")
        };

        Assert.Equal(WaferTransferPriority.ChamberCToFoupB, WaferPipelineSimulator.ChooseNextTransfer(state));
    }

    [Fact]
    public void SchedulerPriority_UsesChamberBToChamberCBeforeUpstreamMoves()
    {
        var state = WaferPipelineSimulator.CreateInitial(SimulatorTimingProfile.Realistic) with
        {
            ChamberB = CompletedChamber("Chamber B", "CMP Main", "CMP_Main", "W01")
        };

        Assert.Equal(WaferTransferPriority.ChamberBToChamberC, WaferPipelineSimulator.ChooseNextTransfer(state));
    }

    [Fact]
    public void SchedulerPriority_UsesChamberAToChamberBBeforeNewFoupFeed()
    {
        var state = WaferPipelineSimulator.CreateInitial(SimulatorTimingProfile.Realistic) with
        {
            ChamberA = CompletedChamber("Chamber A", "Pre-Clean", "PreClean_Default", "W01")
        };

        Assert.Equal(WaferTransferPriority.ChamberAToChamberB, WaferPipelineSimulator.ChooseNextTransfer(state));
    }

    [Fact]
    public void SchedulerPriority_FeedsFoupAToChamberAWhenChamberAIsEmpty()
    {
        var state = WaferPipelineSimulator.CreateInitial(SimulatorTimingProfile.Realistic);

        Assert.Equal(WaferTransferPriority.FoupAToChamberA, WaferPipelineSimulator.ChooseNextTransfer(state));
    }

    [Fact]
    public void ChamberProcessMetadata_IsPresentForAAndBAndC()
    {
        var pipeline = WaferPipelineSimulator.CreateDebugTimeline(SimulatorTimingProfile.Realistic);

        Assert.Contains(pipeline, step => step.ChamberA.RecipeName == "PreClean_Default" && step.ChamberA.CurrentStep == "Chem Clean");
        Assert.Contains(pipeline, step => step.ChamberB.RecipeName == "CMP_Main" && step.ChamberB.CurrentStep == "Bulk Polish");
        Assert.Contains(pipeline, step => step.ChamberC.RecipeName == "PostClean_Dry" && step.ChamberC.CurrentStep == "Spin Dry");
    }

    [Fact]
    public void DefaultTimingProfile_IsNotInstant()
    {
        var timing = SimulatorTimingProfile.Realistic;

        Assert.False(timing.IsInstant);
        Assert.InRange(timing.ThetaSwingMs, 1500, 2500);
        Assert.True(timing.ChamberAProcessSeconds >= 8);
        Assert.True(timing.ChamberBProcessSeconds >= 10);
        Assert.True(timing.ChamberCProcessSeconds >= 8);
    }

    [Fact]
    public void NormalSequenceMode_DoesNotRequestApplicationShutdown()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "ViewModels", "MachineTwinViewModel.cs");

        Assert.DoesNotContain("Application.Current.Shutdown", source);
        Assert.DoesNotContain("Environment.Exit", source);
        Assert.DoesNotContain(".Close(", source);
    }

    [Fact]
    public void CaptureMode_IsOnlyCodePathThatCallsShutdown()
    {
        var source = ReadRepositoryFile("src", "SemiTool.Hmi.Wpf", "App.xaml.cs");

        Assert.Contains("--capture-demo-assets", source);
        Assert.Contains("--capture-ui-debug-report", source);
        Assert.Contains("--capture-full-pipeline-qa", source);
        Assert.Equal(3, CountOccurrences(source, "Shutdown();"));
    }

    [Fact]
    public void StateTraceTimeline_IncludesAllFiveWaferIds()
    {
        var steps = MachineTwinSequencePlan.Create(
            DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load()),
            SimulatorTimingProfile.Realistic);

        Assert.All(["W01", "W02", "W03", "W04", "W05"], waferId =>
            Assert.Contains(steps, step => step.WaferIds.Contains(waferId, StringComparison.Ordinal)));
    }

    [Fact]
    public void VisualThetaAnglesRemainSeparateFromPreservedEncoderValues()
    {
        var steps = MachineTwinSequencePlan.Create(
            DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load()),
            SimulatorTimingProfile.Realistic);

        var transferSteps = steps.Where(step => step.StationKey != "Home").ToArray();

        Assert.All(transferSteps, step => Assert.InRange(step.VisualThetaAngle, -150, 150));
        Assert.DoesNotContain(transferSteps, step => step.VisualThetaAngle == step.PreservedThetaEncoderValue);
    }

    [Fact]
    public void SimulatorPipelineTests_DoNotRequireVendorDll()
    {
        var steps = MachineTwinSequencePlan.Create(
            DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load()),
            SimulatorTimingProfile.Realistic);

        Assert.All(steps, step => Assert.DoesNotContain("IEG3268", step.EventLogMessage, StringComparison.OrdinalIgnoreCase));
        Assert.All(steps, step => Assert.DoesNotContain("RealHardware", step.EventLogMessage, StringComparison.OrdinalIgnoreCase));
    }

    private static ChamberPipelineSnapshot CompletedChamber(string name, string role, string recipe, string waferId) =>
        new(name, role, true, waferId, "Completed", recipe, "Complete", 0, 100, false);

    private static int CountOccurrences(string value, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
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

        throw new DirectoryNotFoundException("Could not locate repository root for pipeline simulator tests.");
    }
}
