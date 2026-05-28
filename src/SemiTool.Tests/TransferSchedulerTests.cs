using SemiTool.Application;
using SemiTool.Domain;

namespace SemiTool.Tests;

public sealed class TransferSchedulerTests
{
    [Fact]
    public void AutoSchedulerPriority_IsPmcToFoupB_First()
    {
        var scheduler = CreateScheduler();
        scheduler.State.PmC.HasWafer = true;
        scheduler.State.PmC.ProcessComplete = true;

        Assert.Equal(TransferActionKind.PmCToFoupB, scheduler.EvaluateNextTransfer().Kind);
    }

    [Fact]
    public void AutoSchedulerPriority_IsPmbToPmc_Second()
    {
        var scheduler = CreateScheduler();
        scheduler.State.PmB.HasWafer = true;
        scheduler.State.PmB.ProcessComplete = true;

        Assert.Equal(TransferActionKind.PmBToPmC, scheduler.EvaluateNextTransfer().Kind);
    }

    [Fact]
    public void AutoSchedulerPriority_IsPmaToPmb_Third()
    {
        var scheduler = CreateScheduler();
        scheduler.State.PmA.HasWafer = true;
        scheduler.State.PmA.ProcessComplete = true;

        Assert.Equal(TransferActionKind.PmAToPmB, scheduler.EvaluateNextTransfer().Kind);
    }

    [Fact]
    public void AutoSchedulerPriority_IsFoupAToPma_Fourth()
    {
        var scheduler = CreateScheduler();

        Assert.Equal(TransferActionKind.FoupAToPmA, scheduler.EvaluateNextTransfer().Kind);
    }

    private static TransferScheduler CreateScheduler()
    {
        var recipes = new RecipeService(TestProfile.Load());
        return new TransferScheduler(recipes);
    }
}
