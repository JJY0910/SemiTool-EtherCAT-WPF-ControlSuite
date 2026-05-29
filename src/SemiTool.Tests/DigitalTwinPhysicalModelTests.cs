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
            ["FOUP A", "Chamber A", "Chamber B (CMP)", "Chamber C", "FOUP B"],
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

        Assert.All(stations, station => Assert.InRange(station.VisualArcPositionDegrees, -150, 150));
        Assert.DoesNotContain(stations, station => station.VisualArcPositionDegrees == station.ThetaEncoderPosition);
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
    public void ChamberRecipeMetadata_MatchesTeachingScenario()
    {
        var recipes = TestProfile.Load().Recipes;

        Assert.Equal("PreClean_Default", recipes["A"].RecipeName);
        Assert.Equal("CMP_Main", recipes["B"].RecipeName);
        Assert.Contains(recipes["B"].Steps, step => step.StepName == "Bulk Polish" && step.SlurryFlow == 80);
        Assert.Contains(recipes["C"].Steps, step => step.StepName == "Spin Dry");
    }
}
