using SemiTool.Domain;

namespace SemiTool.Tests;

public sealed class EquipmentProfileTests
{
    [Fact]
    public void EquipmentProfile_LoadsSuccessfully()
    {
        var profile = TestProfile.Load();

        Assert.Equal("FinalTest_2504110108_ActualEtherCATProfile", profile.ProfileName);
        Assert.NotEmpty(profile.Io.DigitalOutputs);
        Assert.NotEmpty(profile.Poses);
    }

    [Fact]
    public void DigitalOutputMappingValues_ArePreservedExactly()
    {
        var profile = TestProfile.Load();

        Assert.Equal(0, profile.GetOutputChannel(IoPoint.TowerRed));
        Assert.Equal(1, profile.GetOutputChannel(IoPoint.TowerYellow));
        Assert.Equal(2, profile.GetOutputChannel(IoPoint.TowerGreen));
        Assert.Equal(3, profile.GetOutputChannel(IoPoint.ChamberALamp));
        Assert.Equal(4, profile.GetOutputChannel(IoPoint.ChamberADoorClose));
        Assert.Equal(5, profile.GetOutputChannel(IoPoint.ChamberADoorOpen));
        Assert.Equal(6, profile.GetOutputChannel(IoPoint.ChamberBLamp));
        Assert.Equal(7, profile.GetOutputChannel(IoPoint.ChamberBDoorClose));
        Assert.Equal(8, profile.GetOutputChannel(IoPoint.ChamberBDoorOpen));
        Assert.Equal(9, profile.GetOutputChannel(IoPoint.ChamberCLamp));
        Assert.Equal(10, profile.GetOutputChannel(IoPoint.ChamberCDoorClose));
        Assert.Equal(11, profile.GetOutputChannel(IoPoint.ChamberCDoorOpen));
        Assert.Equal(12, profile.GetOutputChannel(IoPoint.CylinderForward));
        Assert.Equal(13, profile.GetOutputChannel(IoPoint.CylinderBackward));
        Assert.Equal(14, profile.GetOutputChannel(IoPoint.VacuumSuction));
        Assert.Equal(15, profile.GetOutputChannel(IoPoint.VacuumExhaust));
    }

    [Fact]
    public void DigitalInputMappingValues_ArePreservedExactly()
    {
        var profile = TestProfile.Load();

        Assert.Equal(0, profile.GetInputChannel(IoPoint.ChamberADoorOpenSensor));
        Assert.Equal(1, profile.GetInputChannel(IoPoint.ChamberADoorCloseSensor));
        Assert.Equal(2, profile.GetInputChannel(IoPoint.ChamberBDoorOpenSensor));
        Assert.Equal(3, profile.GetInputChannel(IoPoint.ChamberBDoorCloseSensor));
        Assert.Equal(4, profile.GetInputChannel(IoPoint.ChamberCDoorOpenSensor));
        Assert.Equal(5, profile.GetInputChannel(IoPoint.ChamberCDoorCloseSensor));
        Assert.Equal(12, profile.GetInputChannel(IoPoint.CylinderRearSensor));
        Assert.Equal(13, profile.GetInputChannel(IoPoint.CylinderFrontSensor));
    }

    [Fact]
    public void RobotPoseValues_ArePreservedExactly()
    {
        var profile = TestProfile.Load();

        AssertPose(profile.GetPose("Home"), 0, 0, 0);
        AssertPose(profile.GetPose("FoupA"), 3018457, 2818463, 14140);
        AssertPose(profile.GetPose("FoupB"), 3018457, 2818463, -394293);
        AssertPose(profile.GetPose("ChamberA"), 1156931, 1153931, -59064);
        AssertPose(profile.GetPose("ChamberB"), 1156931, 1153931, -190823);
        AssertPose(profile.GetPose("ChamberC"), 1156931, 1153931, -322000);
    }

    [Fact]
    public void FoupSlotValues_ArePreservedExactly()
    {
        var profile = TestProfile.Load();

        AssertSlot(profile.GetFoupSlotPose(1), 302380, 102379);
        AssertSlot(profile.GetFoupSlotPose(2), 982378, 782378);
        AssertSlot(profile.GetFoupSlotPose(3), 1627604, 1432388);
        AssertSlot(profile.GetFoupSlotPose(4), 2332102, 2119399);
        AssertSlot(profile.GetFoupSlotPose(5), 3018457, 2818463);
    }

    [Fact]
    public void TimingValues_ArePreservedExactly()
    {
        var timing = TestProfile.Load().Timing;

        Assert.Equal(900, timing.MotionWaitMs);
        Assert.Equal(1000, timing.ExtraIntervalMs);
        Assert.Equal(2000, timing.DoorWaitMs);
        Assert.Equal(1000, timing.CylinderWaitTimeoutMs);
        Assert.Equal(1200, timing.VacuumSuctionMs);
        Assert.Equal(1200, timing.VacuumExhaustMs);
        Assert.Equal(1000, timing.AutoRealTickMs);
        Assert.Equal(3000, timing.AutoSimTickMs);
    }

    private static void AssertPose(RobotPose pose, long zSafe, long zWork, long theta)
    {
        Assert.Equal(zSafe, pose.ZSafe);
        Assert.Equal(zWork, pose.ZWork);
        Assert.Equal(theta, pose.Theta);
    }

    private static void AssertSlot(FoupSlotPose pose, long zSafe, long zWork)
    {
        Assert.Equal(zSafe, pose.ZSafe);
        Assert.Equal(zWork, pose.ZWork);
    }
}
