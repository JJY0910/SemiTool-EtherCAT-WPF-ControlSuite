using SemiTool.Domain;

namespace SemiTool.Application;

public sealed class TransferScheduler
{
    private readonly RecipeService _recipes;

    public TransferScheduler(RecipeService recipes)
    {
        _recipes = recipes;
    }

    public WaferFlowState State { get; } = new();

    public TransferDecision EvaluateNextTransfer()
    {
        var foupBSlot = State.FoupB.FirstOrDefault(slot => !slot.HasWafer);
        if (State.PmC.HasWafer && State.PmC.ProcessComplete && foupBSlot is not null)
        {
            return new TransferDecision(
                TransferActionKind.PmCToFoupB,
                $"PM C -> FOUP B Slot {foupBSlot.Slot}",
                DestinationSlot: foupBSlot.Slot,
                SourceChamber: ChamberId.C);
        }

        if (State.PmB.HasWafer && State.PmB.ProcessComplete && !State.PmC.HasWafer)
        {
            return new TransferDecision(
                TransferActionKind.PmBToPmC,
                "PM B -> PM C",
                SourceChamber: ChamberId.B,
                DestinationChamber: ChamberId.C);
        }

        if (State.PmA.HasWafer && State.PmA.ProcessComplete && !State.PmB.HasWafer)
        {
            return new TransferDecision(
                TransferActionKind.PmAToPmB,
                "PM A -> PM B",
                SourceChamber: ChamberId.A,
                DestinationChamber: ChamberId.B);
        }

        var foupASlot = State.FoupA.FirstOrDefault(slot => slot.HasWafer);
        if (foupASlot is not null && !State.PmA.HasWafer)
        {
            return new TransferDecision(
                TransferActionKind.FoupAToPmA,
                $"FOUP A Slot {foupASlot.Slot} -> PM A",
                SourceSlot: foupASlot.Slot,
                DestinationChamber: ChamberId.A);
        }

        return new TransferDecision(TransferActionKind.ProcessTick, "Advance process countdown");
    }

    public IReadOnlyList<string> BuildTransferQueueSnapshot()
    {
        var queue = new List<string>();
        var decision = EvaluateNextTransfer();
        queue.Add(decision.Description);
        queue.Add(State.PmA.ToString());
        queue.Add(State.PmB.ToString());
        queue.Add(State.PmC.ToString());
        return queue;
    }

    public async Task<TransferDecision> ExecuteNextAsync(EquipmentSequenceService sequence, CancellationToken cancellationToken = default)
    {
        var decision = EvaluateNextTransfer();
        switch (decision.Kind)
        {
            case TransferActionKind.PmCToFoupB:
                await sequence.PickFromChamber(ChamberId.C, cancellationToken).ConfigureAwait(false);
                await sequence.PlaceToFoupB(decision.DestinationSlot!.Value, cancellationToken).ConfigureAwait(false);
                MoveChamberToFoupB(State.PmC, decision.DestinationSlot.Value);
                break;
            case TransferActionKind.PmBToPmC:
                await sequence.PickFromChamber(ChamberId.B, cancellationToken).ConfigureAwait(false);
                await sequence.PlaceToChamber(ChamberId.C, cancellationToken).ConfigureAwait(false);
                MoveChamberToChamber(State.PmB, State.PmC, _recipes.Recipes["C"].RecipeName);
                break;
            case TransferActionKind.PmAToPmB:
                await sequence.PickFromChamber(ChamberId.A, cancellationToken).ConfigureAwait(false);
                await sequence.PlaceToChamber(ChamberId.B, cancellationToken).ConfigureAwait(false);
                MoveChamberToChamber(State.PmA, State.PmB, _recipes.Recipes["B"].RecipeName);
                break;
            case TransferActionKind.FoupAToPmA:
                await sequence.PickFromFoupA(decision.SourceSlot!.Value, cancellationToken).ConfigureAwait(false);
                await sequence.PlaceToChamber(ChamberId.A, cancellationToken).ConfigureAwait(false);
                MoveFoupAToPmA(decision.SourceSlot.Value);
                break;
            case TransferActionKind.ProcessTick:
                AdvanceProcessCountdown();
                break;
        }

        return decision;
    }

    public void AdvanceProcessCountdown()
    {
        foreach (var chamber in new[] { State.PmA, State.PmB, State.PmC })
        {
            if (!chamber.HasWafer || chamber.ProcessComplete)
            {
                continue;
            }

            chamber.RemainingSeconds = Math.Max(0, chamber.RemainingSeconds - 1);
            chamber.ProcessComplete = chamber.RemainingSeconds == 0;
        }
    }

    private void MoveFoupAToPmA(int slot)
    {
        var source = State.FoupA.First(item => item.Slot == slot);
        State.PmA.HasWafer = true;
        State.PmA.WaferId = source.WaferId;
        State.PmA.RecipeName = _recipes.Recipes["A"].RecipeName;
        State.PmA.RemainingSeconds = _recipes.Recipes["A"].Steps.Sum(step => step.DurationSec);
        State.PmA.ProcessComplete = false;
        source.HasWafer = false;
        source.WaferId = string.Empty;
    }

    private static void MoveChamberToChamber(ChamberProcessState source, ChamberProcessState destination, string recipeName)
    {
        destination.HasWafer = true;
        destination.WaferId = source.WaferId;
        destination.RecipeName = recipeName;
        destination.RemainingSeconds = 30;
        destination.ProcessComplete = false;
        ClearChamber(source);
    }

    private void MoveChamberToFoupB(ChamberProcessState source, int slot)
    {
        var destination = State.FoupB.First(item => item.Slot == slot);
        destination.HasWafer = true;
        destination.WaferId = source.WaferId;
        ClearChamber(source);
    }

    private static void ClearChamber(ChamberProcessState chamber)
    {
        chamber.HasWafer = false;
        chamber.WaferId = string.Empty;
        chamber.ProcessComplete = false;
        chamber.RemainingSeconds = 0;
        chamber.RecipeName = string.Empty;
    }
}
