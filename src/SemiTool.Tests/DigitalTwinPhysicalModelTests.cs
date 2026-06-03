using SemiTool.Domain;

namespace SemiTool.Tests;

public sealed class DigitalTwinPhysicalModelTests
{
    [Fact]
    public void ThetaSwingModel_IsLimitedAndNotContinuous()
    {
        var model = DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load());

        Assert.False(model.ThetaSwing.IsContinuousRotation);
        Assert.Equal(300, model.ThetaSwing.VisualSweepApproxDegrees);
    }

    [Fact]
    public void ThetaSwingModel_UsesStationOrderFromFoupAToFoupB()
    {
        var stations = DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load()).ThetaSwing.Stations;

        Assert.Equal(
            ["Home / Start", "FOUP A", "Chamber A", "Chamber B (CMP)", "Chamber C", "FOUP B"],
            stations.OrderBy(station => station.Order).Select(station => station.DisplayName).ToArray());
    }

    [Fact]
    public void ThetaSwingModel_PreservesEncoderTargets()
    {
        var stations = DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load()).ThetaSwing.Stations;

        Assert.Equal(14140, stations.Single(station => station.PoseKey == "FoupA").ThetaEncoderPosition);
        Assert.Equal(-59064, stations.Single(station => station.PoseKey == "ChamberA").ThetaEncoderPosition);
        Assert.Equal(-190823, stations.Single(station => station.PoseKey == "ChamberB").ThetaEncoderPosition);
        Assert.Equal(-322000, stations.Single(station => station.PoseKey == "ChamberC").ThetaEncoderPosition);
        Assert.Equal(-394293, stations.Single(station => station.PoseKey == "FoupB").ThetaEncoderPosition);
    }

    [Fact]
    public void ThetaSwingModel_DoesNotTreatEncoderValuesAsVisualDegrees()
    {
        var stations = DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load()).ThetaSwing.Stations;

        var transferStations = stations.Where(station => station.PoseKey != "Home").ToArray();

        Assert.All(transferStations, station => Assert.InRange(station.VisualArcPositionDegrees, -150, 150));
        Assert.DoesNotContain(transferStations, station => station.VisualArcPositionDegrees == station.ThetaEncoderPosition);
    }

    [Fact]
    public void ThetaSwingModel_UsesOperatorViewAnglesForActualStationLayout()
    {
        var stations = DigitalTwinPhysicalModel.CreateDefault(CreateVisualLayoutProfile()).ThetaSwing.Stations;

        Assert.Equal(-180, stations.Single(station => station.PoseKey == "Home").VisualArcPositionDegrees);
        Assert.Equal(-120, stations.Single(station => station.PoseKey == "FoupA").VisualArcPositionDegrees);
        Assert.Equal(-75, stations.Single(station => station.PoseKey == "ChamberA").VisualArcPositionDegrees);
        Assert.Equal(0, stations.Single(station => station.PoseKey == "ChamberB").VisualArcPositionDegrees);
        Assert.Equal(75, stations.Single(station => station.PoseKey == "ChamberC").VisualArcPositionDegrees);
        Assert.Equal(120, stations.Single(station => station.PoseKey == "FoupB").VisualArcPositionDegrees);
    }

    [Fact]
    public void BladeMechanism_MapsCylinderAndVacuumToEndEffectorBehavior()
    {
        var mechanism = DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load()).BladeMechanism;

        Assert.True(mechanism.IsTelescopic);
        Assert.Equal("CylinderForward", mechanism.ExtendCommand);
        Assert.Equal("CylinderBackward", mechanism.RetractCommand);
        Assert.Equal("VacuumSuction", mechanism.HoldCommand);
        Assert.Equal("VacuumExhaust", mechanism.ReleaseCommand);
    }

    [Fact]
    public void ChamberRecipeMetadata_MatchesTransferScenario()
    {
        var recipes = TestProfile.Load().Recipes;

        Assert.Equal("PreClean_Default", recipes["A"].RecipeName);
        Assert.Equal("CMP_Main", recipes["B"].RecipeName);
        Assert.Contains(recipes["B"].Steps, step => step.StepName == "Bulk Polish" && step.SlurryFlow == 80);
        Assert.Contains(recipes["C"].Steps, step => step.StepName == "Spin Dry");
    }

    [Fact]
    public void MachineTwinSequencePlan_MovesFromFoupAToFoupB()
    {
        var model = DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load());
        var steps = MachineTwinSequencePlan.CreateDefault(model);

        Assert.Equal("Home / Start", steps[0].CurrentStation);
        Assert.Contains(steps, step => step.CurrentStation == "FOUP A");
        Assert.Contains(steps, step => step.CurrentStation == "Chamber A");
        Assert.Contains(steps, step => step.CurrentStation == "Chamber B (CMP)");
        Assert.Contains(steps, step => step.CurrentStation == "Chamber C");
        Assert.Equal("FOUP B", steps[^1].CurrentStation);
        Assert.Equal(MachineTwinSequencePlan.CompletedStepName, steps[^1].StepName);
    }

    [Fact]
    public void MachineTwinSequencePlan_DoesNotRequireRealHardware()
    {
        var model = DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load());
        var steps = MachineTwinSequencePlan.CreateDefault(model);

        Assert.All(steps, step => Assert.DoesNotContain("RealHardware", step.EventLogMessage, StringComparison.OrdinalIgnoreCase));
        Assert.All(steps, step => Assert.DoesNotContain("IEG3268", step.EventLogMessage, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MachineTwinSequencePlan_VisualAnglesStaySeparateFromEncoderValues()
    {
        var model = DigitalTwinPhysicalModel.CreateDefault(TestProfile.Load());
        var steps = MachineTwinSequencePlan.CreateDefault(model);

        var transferSteps = steps.Where(step => step.StationKey != "Home").ToArray();

        Assert.All(transferSteps, step => Assert.InRange(step.VisualThetaAngle, -150, 150));
        Assert.DoesNotContain(transferSteps, step => step.VisualThetaAngle == step.PreservedThetaEncoderValue);
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

        throw new DirectoryNotFoundException("Could not locate repository root for test asset check.");
    }

    private static EquipmentProfile CreateVisualLayoutProfile() => new()
    {
        Poses = new Dictionary<string, RobotPose>(StringComparer.OrdinalIgnoreCase)
        {
            ["Home"] = new() { Theta = 0 },
            ["FoupA"] = new() { Theta = 14140 },
            ["ChamberA"] = new() { Theta = -59064 },
            ["ChamberB"] = new() { Theta = -190823 },
            ["ChamberC"] = new() { Theta = -322000 },
            ["FoupB"] = new() { Theta = -394293 }
        }
    };
}
