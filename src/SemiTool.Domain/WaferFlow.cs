namespace SemiTool.Domain;

public sealed class WaferSlotState
{
    public int Slot { get; init; }
    public bool HasWafer { get; set; }
    public string WaferId { get; set; } = string.Empty;

    public override string ToString() => HasWafer ? $"Slot {Slot}: {WaferId}" : $"Slot {Slot}: Empty";
}

public sealed class ChamberProcessState
{
    public ChamberId Chamber { get; init; }
    public bool HasWafer { get; set; }
    public string WaferId { get; set; } = string.Empty;
    public bool ProcessComplete { get; set; }
    public int RemainingSeconds { get; set; }
    public string RecipeName { get; set; } = string.Empty;

    public override string ToString()
    {
        if (!HasWafer)
        {
            return $"PM {Chamber}: Empty";
        }

        return ProcessComplete
            ? $"PM {Chamber}: {WaferId} complete"
            : $"PM {Chamber}: {WaferId} {RemainingSeconds}s";
    }
}

public sealed class WaferFlowState
{
    public List<WaferSlotState> FoupA { get; } = Enumerable.Range(1, 5)
        .Select(slot => new WaferSlotState { Slot = slot, HasWafer = true, WaferId = $"A{slot:00}" })
        .ToList();

    public List<WaferSlotState> FoupB { get; } = Enumerable.Range(1, 5)
        .Select(slot => new WaferSlotState { Slot = slot })
        .ToList();

    public ChamberProcessState PmA { get; } = new() { Chamber = ChamberId.A };
    public ChamberProcessState PmB { get; } = new() { Chamber = ChamberId.B };
    public ChamberProcessState PmC { get; } = new() { Chamber = ChamberId.C };

    public ChamberProcessState GetChamber(ChamberId chamber) => chamber switch
    {
        ChamberId.A => PmA,
        ChamberId.B => PmB,
        ChamberId.C => PmC,
        _ => throw new ArgumentOutOfRangeException(nameof(chamber), chamber, null)
    };
}

public sealed record TransferDecision(
    TransferActionKind Kind,
    string Description,
    int? SourceSlot = null,
    int? DestinationSlot = null,
    ChamberId? SourceChamber = null,
    ChamberId? DestinationChamber = null);
