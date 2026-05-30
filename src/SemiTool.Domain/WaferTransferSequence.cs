namespace SemiTool.Domain;

/// <summary>
/// Builds the slow, readable Machine Twin transfer sequence used by the WPF runtime.
/// </summary>
/// <remarks>
/// This is a simulator presentation model only. It preserves the five wafer
/// invariant while making the door, blade, vacuum, and process-gating rules
/// visible to operators and reviewers. It does not touch EtherCAT
/// axis targets, I/O mapping, vendor DLL loading, or real-hardware adapter
/// behavior.
/// </remarks>
internal static class WaferTransferSequence
{
    private const int TotalWafers = WaferPipelineSimulator.TotalWafers;

    public static IReadOnlyList<WaferPipelineSnapshot> Create(SimulatorTimingProfile timing)
    {
        var builder = new Builder(timing);
        return builder.Build();
    }

    private sealed class Builder
    {
        private readonly SimulatorTimingProfile _timing;
        private readonly List<WaferPipelineSnapshot> _snapshots = [];
        private readonly string[] _foupA = ["W01", "W02", "W03", "W04", "W05"];
        private readonly string[] _foupB = ["", "", "", "", ""];
        private readonly ChamberState _chamberA = new("Chamber A", "ChamberA", "Pre-Clean", "PreClean_Default", "Chem Clean");
        private readonly ChamberState _chamberB = new("Chamber B", "ChamberB", "CMP Main", "CMP_Main", "Bulk Polish");
        private readonly ChamberState _chamberC = new("Chamber C", "ChamberC", "Post-Clean & Dry", "PostClean_Dry", "Spin Dry");
        private string _bladeWaferId = string.Empty;
        private string _stationKey = "Home";
        private string _previousStation = "-";
        private string _nextStation = "FOUP A";
        private string _zState = "Z Safe";
        private BladeSequenceState _bladeState = BladeSequenceState.Retracted;
        private VacuumSequenceState _vacuumState = VacuumSequenceState.Off;
        private RobotSequenceState _robotState = RobotSequenceState.Idle;
        private bool _towerGreen;
        private int _stepIndex;

        public Builder(SimulatorTimingProfile timing)
        {
            _timing = timing;
        }

        public IReadOnlyList<WaferPipelineSnapshot> Build()
        {
            Add(
                "Startup Simulator",
                "00-startup-simulator.png",
                PipelineStateKind.Ready,
                "Pipeline ready: FOUP A 5 wafers, FOUP B empty",
                "Home/start position. FOUP A starts with W01-W05 waiting. Blade retracted, vacuum off, all chamber doors closed.",
                "Ready",
                delayMs: _timing.ThetaSwingMs);

            var guard = 0;
            while (!IsComplete())
            {
                guard++;
                if (guard > 100)
                {
                    throw new InvalidOperationException("Transfer sequence guard exceeded before all wafers reached FOUP B.");
                }

                var transfer = ChooseNextTransfer();
                if (transfer is null)
                {
                    AdvanceProcessing();
                    continue;
                }

                ExecuteTransfer(transfer.Value);
            }

            _stationKey = "FoupB";
            _previousStation = "Chamber C";
            _nextStation = "-";
            _zState = "Z Safe";
            _bladeState = BladeSequenceState.Retracted;
            _vacuumState = VacuumSequenceState.Off;
            _robotState = RobotSequenceState.Idle;
            _towerGreen = true;
            Add(
                MachineTwinSequencePlan.CompletedStepName,
                "09-final-foup-b-5-completed.png",
                PipelineStateKind.Completed,
                "All 5 wafers complete in FOUP B",
                "All 5 wafers completed in FOUP B. Tower green blink enabled.",
                "Complete",
                activeWafer: "W05",
                delayMs: _timing.TowerBlinkMs * 2);

            ResetMutableState();
            Add(
                "Reset Safe State",
                "10-reset-safe-state.png",
                PipelineStateKind.Ready,
                "Reset to safe simulator state",
                "Reset returns FOUP A to W01-W05 waiting, FOUP B empty, blade retracted, vacuum off, all doors closed.",
                "Reset",
                delayMs: _timing.ThetaSwingMs);

            return _snapshots;
        }

        private TransferIntent? ChooseNextTransfer()
        {
            if (_chamberC.ProcessState == "Completed" && NextEmptyFoupBSlotIndex() >= 0)
            {
                return new TransferIntent(TransferKind.ChamberCToFoupB, _chamberC, null, NextEmptyFoupBSlotIndex());
            }

            if (_chamberB.ProcessState == "Completed" && !_chamberC.HasWafer)
            {
                return new TransferIntent(TransferKind.ChamberBToChamberC, _chamberB, _chamberC, -1);
            }

            if (_chamberA.ProcessState == "Completed" && !_chamberB.HasWafer)
            {
                return new TransferIntent(TransferKind.ChamberAToChamberB, _chamberA, _chamberB, -1);
            }

            if (!_chamberA.HasWafer && NextWaitingFoupASlotIndex() >= 0)
            {
                return new TransferIntent(TransferKind.FoupAToChamberA, null, _chamberA, NextWaitingFoupASlotIndex());
            }

            return null;
        }

        private void ExecuteTransfer(TransferIntent transfer)
        {
            var waferId = transfer.Kind == TransferKind.FoupAToChamberA
                ? _foupA[transfer.SlotIndex]
                : transfer.SourceChamber!.WaferId;

            var sourceName = transfer.Kind == TransferKind.FoupAToChamberA
                ? $"FOUP A Slot A{transfer.SlotIndex + 1}"
                : transfer.SourceChamber!.Name;

            var targetName = transfer.Kind == TransferKind.ChamberCToFoupB
                ? $"FOUP B Slot B{transfer.SlotIndex + 1}"
                : transfer.TargetChamber!.Name;

            PickFromSource(transfer, waferId, sourceName, targetName);
            PlaceToTarget(transfer, waferId, sourceName, targetName);
        }

        private void PickFromSource(TransferIntent transfer, string waferId, string sourceName, string targetName)
        {
            _stationKey = transfer.Kind == TransferKind.FoupAToChamberA ? "FoupA" : transfer.SourceChamber!.StationKey;
            _previousStation = DisplayStation(_previousStation);
            _nextStation = targetName;
            _robotState = RobotSequenceState.MovingTheta;
            _zState = "Z Safe";
            _bladeState = BladeSequenceState.Retracted;
            _vacuumState = VacuumSequenceState.Off;
            Add(
                $"Move To {sourceName}",
                FirstMatchingScreenshot(sourceName, "01-foup-a-before-pickup.png"),
                PipelineStateKind.Running,
                $"Moving to {sourceName}",
                $"Theta target {sourceName}; preparing to pick {waferId}.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.ThetaSwingMs);

            if (transfer.SourceChamber is not null)
            {
                OpenDoor(transfer.SourceChamber, $"Open {sourceName} door for unloading {waferId}");
            }

            _robotState = RobotSequenceState.MovingZ;
            _zState = $"Z Work / {sourceName}";
            _bladeState = BladeSequenceState.Retracted;
            _vacuumState = VacuumSequenceState.Off;
            Add(
                $"Z Work At {sourceName}",
                FirstMatchingScreenshot(sourceName, "02-z-work-foup-a-slot-a1.png"),
                PipelineStateKind.Running,
                $"Z moving to {sourceName} height",
                $"Z reaches the selected slot/stage before blade extension.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.ZSafeToWorkMs);

            _bladeState = BladeSequenceState.Extending;
            _vacuumState = VacuumSequenceState.Off;
            Add(
                $"Blade Extending Into {sourceName}",
                transfer.SourceChamber == _chamberA ? Once("08-chamber-a-unload-after-process-complete.png") : null,
                PipelineStateKind.Running,
                $"Blade extending into {sourceName}",
                $"Blade enters {sourceName} only after the target path is safe.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.BladeExtendMs);

            _robotState = RobotSequenceState.Picking;
            _bladeState = BladeSequenceState.Extended;
            _vacuumState = VacuumSequenceState.SuctionOn;
            Add(
                $"Vacuum Suction {waferId} At {sourceName}",
                null,
                PipelineStateKind.Running,
                $"Vacuum suction ON before picking {waferId}",
                $"Vacuum confirms suction before {waferId} leaves {sourceName}.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.VacuumSuctionSettleMs);

            RemoveWaferFromSource(transfer);
            _bladeWaferId = waferId;
            Add(
                $"{waferId} On Blade From {sourceName}",
                FirstMatchingScreenshot(sourceName, "02-blade-holding-wafer-after-pickup.png"),
                PipelineStateKind.Running,
                $"{waferId} picked onto blade",
                $"{waferId} is now held on the blade; source slot/stage is empty.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.VacuumSuctionSettleMs);

            _robotState = RobotSequenceState.MovingZ;
            _bladeState = BladeSequenceState.Retracting;
            Add(
                $"Blade Retracting With {waferId}",
                null,
                PipelineStateKind.Running,
                $"Blade retracting with {waferId}",
                $"Blade retracts with {waferId} before any chamber door can close.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.BladeRetractMs);

            _bladeState = BladeSequenceState.Retracted;
            _zState = "Z Safe";
            Add(
                $"{waferId} Clear Of {sourceName}",
                null,
                PipelineStateKind.Running,
                $"{waferId} clear of {sourceName}",
                $"Blade is fully retracted with {waferId}; transfer can proceed.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.ZWorkToSafeMs);

            if (transfer.SourceChamber is not null)
            {
                CloseDoor(transfer.SourceChamber, $"Close {sourceName} door after unloading {waferId}");
            }
        }

        private void PlaceToTarget(TransferIntent transfer, string waferId, string sourceName, string targetName)
        {
            _stationKey = transfer.Kind == TransferKind.ChamberCToFoupB ? "FoupB" : transfer.TargetChamber!.StationKey;
            _previousStation = sourceName;
            _nextStation = targetName;
            _robotState = RobotSequenceState.MovingTheta;
            _zState = "Z Safe";
            _bladeState = BladeSequenceState.Retracted;
            _vacuumState = VacuumSequenceState.SuctionOn;
            Add(
                $"Move {waferId} To {targetName}",
                null,
                PipelineStateKind.Running,
                $"Moving {waferId} to {targetName}",
                $"Theta swings station-to-station toward {targetName}; this is not 360-degree continuous rotation.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.ThetaSwingMs);

            if (transfer.TargetChamber is not null)
            {
                OpenDoor(transfer.TargetChamber, $"Open {targetName} door before loading {waferId}");
            }

            _robotState = RobotSequenceState.MovingZ;
            _zState = $"Z Work / {targetName}";
            _bladeState = BladeSequenceState.Retracted;
            Add(
                $"Z Work At {targetName}",
                null,
                PipelineStateKind.Running,
                $"Z moving to {targetName} height",
                $"Z reaches the target slot/stage before blade extension.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.ZSafeToWorkMs);

            _bladeState = BladeSequenceState.Extending;
            Add(
                $"Blade Entering {targetName}",
                transfer.TargetChamber == _chamberA ? Once("04-blade-entering-chamber-a-door-open.png") : null,
                PipelineStateKind.Running,
                $"Blade extending into {targetName}",
                $"Blade enters {targetName} only while the chamber door is open.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.BladeExtendMs);

            _robotState = RobotSequenceState.Placing;
            _bladeState = BladeSequenceState.Extended;
            _vacuumState = VacuumSequenceState.ExhaustOrRelease;
            Add(
                $"Vacuum Release {waferId} At {targetName}",
                null,
                PipelineStateKind.Running,
                $"Vacuum release/exhaust before placing {waferId}",
                $"Vacuum exhaust releases {waferId} before it is marked on the target stage/slot.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.VacuumExhaustSettleMs);

            PlaceWaferAtTarget(transfer, waferId);
            _bladeWaferId = string.Empty;
            Add(
                $"{waferId} Placed At {targetName}",
                transfer.TargetChamber == _chamberA ? Once("05-wafer-placed-chamber-a-stage.png") : null,
                PipelineStateKind.Running,
                $"{waferId} placed at {targetName}",
                $"{waferId} moved from blade to {targetName}; blade is now empty.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.VacuumExhaustSettleMs);

            _robotState = RobotSequenceState.MovingZ;
            _vacuumState = VacuumSequenceState.Off;
            _bladeState = BladeSequenceState.Retracting;
            Add(
                $"Blade Retracting Empty From {targetName}",
                transfer.TargetChamber == _chamberA ? Once("06-blade-retracted-before-chamber-a-door-closes.png") : null,
                PipelineStateKind.Running,
                $"Blade retracting empty from {targetName}",
                "Door close is blocked until the blade is fully retracted.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.BladeRetractMs);

            _bladeState = BladeSequenceState.Retracted;
            _zState = "Z Safe";
            Add(
                $"Blade Clear Of {targetName}",
                null,
                PipelineStateKind.Running,
                $"Blade clear of {targetName}",
                "Blade is retracted and target stage/slot owns the wafer.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.ZWorkToSafeMs);

            if (transfer.TargetChamber is not null)
            {
                CloseDoor(transfer.TargetChamber, $"Close {targetName} door before process starts");
                StartProcess(transfer.TargetChamber, waferId);
            }
        }

        private void OpenDoor(ChamberState chamber, string action)
        {
            chamber.DoorState = ChamberDoorSequenceState.Opening;
            _robotState = RobotSequenceState.Idle;
            Add(
                $"{chamber.Name} Door Opening",
                chamber == _chamberA ? Once("03-chamber-a-door-opening.png") : null,
                PipelineStateKind.Running,
                action,
                $"{chamber.Name} door opening; blade remains retracted.",
                action,
                chamber.WaferId,
                _timing.DoorOpenCloseMs);

            chamber.DoorState = ChamberDoorSequenceState.Open;
            Add(
                $"{chamber.Name} Door Open",
                null,
                PipelineStateKind.Running,
                $"{chamber.Name} door open",
                $"{chamber.Name} is open and safe for blade entry.",
                action,
                chamber.WaferId,
                _timing.DoorOpenCloseMs);
        }

        private void CloseDoor(ChamberState chamber, string action)
        {
            chamber.DoorState = ChamberDoorSequenceState.Closing;
            _robotState = RobotSequenceState.Idle;
            Add(
                $"{chamber.Name} Door Closing",
                null,
                PipelineStateKind.Running,
                action,
                $"{chamber.Name} door closing after blade is retracted.",
                action,
                chamber.WaferId,
                _timing.DoorOpenCloseMs);

            chamber.DoorState = ChamberDoorSequenceState.Closed;
            Add(
                $"{chamber.Name} Door Closed",
                null,
                PipelineStateKind.Running,
                $"{chamber.Name} door closed",
                $"{chamber.Name} door is closed; process may run only in this state.",
                action,
                chamber.WaferId,
                _timing.DoorOpenCloseMs);
        }

        private void StartProcess(ChamberState chamber, string waferId)
        {
            chamber.ProcessState = "Processing";
            chamber.ProgressPercent = 10;
            chamber.RemainingSeconds = ProcessSeconds(chamber);
            _robotState = RobotSequenceState.Idle;
            Add(
                $"{chamber.Name} Processing {waferId}",
                chamber == _chamberA ? Once("07-chamber-a-processing-door-closed.png") : null,
                PipelineStateKind.Running,
                $"{chamber.Name} processing {waferId}",
                $"{chamber.Name} process starts only after wafer is on the stage and door is closed.",
                $"{chamber.Name} process",
                waferId,
                ProcessSeconds(chamber) * 1000);
        }

        private void AdvanceProcessing()
        {
            var chamber = new[] { _chamberC, _chamberB, _chamberA }
                .FirstOrDefault(item => item.ProcessState == "Processing");

            if (chamber is null)
            {
                throw new InvalidOperationException("No transfer or process countdown was available in the sequence run.");
            }

            chamber.ProgressPercent = 100;
            chamber.RemainingSeconds = 0;
            chamber.ProcessState = "Completed";
            _robotState = RobotSequenceState.Idle;
            _bladeState = BladeSequenceState.Retracted;
            _vacuumState = VacuumSequenceState.Off;
            Add(
                $"{chamber.Name} Process Complete {chamber.WaferId}",
                null,
                PipelineStateKind.Running,
                $"{chamber.Name} process complete for {chamber.WaferId}",
                $"{chamber.WaferId} is ready for downstream-first unload from {chamber.Name}.",
                $"{chamber.Name} complete",
                chamber.WaferId,
                _timing.TowerBlinkMs * 2);
        }

        private void RemoveWaferFromSource(TransferIntent transfer)
        {
            if (transfer.Kind == TransferKind.FoupAToChamberA)
            {
                _foupA[transfer.SlotIndex] = string.Empty;
                return;
            }

            transfer.SourceChamber!.Clear();
        }

        private void PlaceWaferAtTarget(TransferIntent transfer, string waferId)
        {
            if (transfer.Kind == TransferKind.ChamberCToFoupB)
            {
                _foupB[transfer.SlotIndex] = waferId;
                return;
            }

            transfer.TargetChamber!.Load(waferId);
        }

        private void Add(
            string name,
            string? screenshotName,
            PipelineStateKind pipelineState,
            string currentAction,
            string eventMessage,
            string transferDescription,
            string activeWafer = "",
            int delayMs = 800)
        {
            var screenshot = screenshotName ?? $"{_stepIndex:00}-{Slug(name)}.png";
            var snapshot = new WaferPipelineSnapshot(
                _stepIndex,
                name,
                screenshot,
                pipelineState,
                BuildSlots("A", _foupA, completed: false, activeWafer),
                BuildSlots("B", _foupB, completed: true, activeWafer),
                _chamberA.ToSnapshot(),
                _chamberB.ToSnapshot(),
                _chamberC.ToSnapshot(),
                _stationKey,
                _previousStation,
                _nextStation,
                currentAction,
                transferDescription,
                currentAction,
                activeWafer,
                _robotState,
                _bladeState,
                _vacuumState,
                _chamberA.DoorState,
                _chamberB.DoorState,
                _chamberC.DoorState,
                _robotState is RobotSequenceState.MovingTheta or RobotSequenceState.MovingZ,
                _zState,
                _bladeState is BladeSequenceState.Extending or BladeSequenceState.Extended,
                _bladeState is BladeSequenceState.Extending or BladeSequenceState.Extended,
                _bladeState is BladeSequenceState.Retracted or BladeSequenceState.Retracting,
                VacuumLabel(_vacuumState),
                !string.IsNullOrWhiteSpace(_bladeWaferId),
                _bladeWaferId,
                _towerGreen,
                Math.Max(300, delayMs),
                eventMessage);

            ValidateFiveWaferInvariant(snapshot);
            _snapshots.Add(snapshot);
            _stepIndex++;
        }

        private static IReadOnlyList<WaferPipelineSlot> BuildSlots(string prefix, IReadOnlyList<string> wafers, bool completed, string activeWafer)
        {
            var slots = new List<WaferPipelineSlot>(TotalWafers);
            for (var i = 1; i <= TotalWafers; i++)
            {
                var waferId = wafers[i - 1];
                var hasWafer = !string.IsNullOrWhiteSpace(waferId);
                var state = hasWafer ? completed ? "Completed" : "Waiting" : "Empty";
                slots.Add(new WaferPipelineSlot($"{prefix}{i}", hasWafer, waferId, state, waferId == activeWafer));
            }

            return slots;
        }

        private int NextWaitingFoupASlotIndex() => Array.FindIndex(_foupA, wafer => !string.IsNullOrWhiteSpace(wafer));

        private int NextEmptyFoupBSlotIndex() => Array.FindIndex(_foupB, string.IsNullOrWhiteSpace);

        private bool IsComplete() =>
            _foupA.All(string.IsNullOrWhiteSpace) &&
            _foupB.All(wafer => !string.IsNullOrWhiteSpace(wafer)) &&
            !_chamberA.HasWafer &&
            !_chamberB.HasWafer &&
            !_chamberC.HasWafer &&
            string.IsNullOrWhiteSpace(_bladeWaferId);

        private int ProcessSeconds(ChamberState chamber)
        {
            if (chamber == _chamberA)
            {
                return _timing.ChamberAProcessSeconds;
            }

            if (chamber == _chamberB)
            {
                return _timing.ChamberBProcessSeconds;
            }

            return _timing.ChamberCProcessSeconds;
        }

        private void ResetMutableState()
        {
            for (var i = 0; i < TotalWafers; i++)
            {
                _foupA[i] = $"W{i + 1:00}";
                _foupB[i] = string.Empty;
            }

            _chamberA.Clear();
            _chamberB.Clear();
            _chamberC.Clear();
            _chamberA.DoorState = ChamberDoorSequenceState.Closed;
            _chamberB.DoorState = ChamberDoorSequenceState.Closed;
            _chamberC.DoorState = ChamberDoorSequenceState.Closed;
            _bladeWaferId = string.Empty;
            _stationKey = "Home";
            _previousStation = "-";
            _nextStation = "FOUP A";
            _zState = "Z Safe";
            _bladeState = BladeSequenceState.Retracted;
            _vacuumState = VacuumSequenceState.Off;
            _robotState = RobotSequenceState.Idle;
            _towerGreen = false;
        }

        private string? Once(string screenshotName) =>
            _snapshots.Any(snapshot => string.Equals(snapshot.ScreenshotName, screenshotName, StringComparison.OrdinalIgnoreCase))
                ? null
                : screenshotName;

        private static string? FirstMatchingScreenshot(string sourceName, string screenshotName) =>
            sourceName.StartsWith("FOUP A", StringComparison.Ordinal) && sourceName.Contains("A1", StringComparison.Ordinal)
                ? screenshotName
                : null;

        private static string DisplayStation(string value) =>
            string.IsNullOrWhiteSpace(value) ? "-" : value;

        private static string VacuumLabel(VacuumSequenceState state) => state switch
        {
            VacuumSequenceState.SuctionOn => "Suction",
            VacuumSequenceState.ExhaustOrRelease => "Exhaust",
            _ => "Off"
        };

        private static string Slug(string value) =>
            value
                .ToLowerInvariant()
                .Replace(" / ", "-", StringComparison.Ordinal)
                .Replace(" ", "-", StringComparison.Ordinal)
                .Replace("->", "to", StringComparison.Ordinal)
                .Replace(":", string.Empty, StringComparison.Ordinal);

        private static void ValidateFiveWaferInvariant(WaferPipelineSnapshot snapshot)
        {
            var waferIds = snapshot.FoupASlots.Concat(snapshot.FoupBSlots)
                .Where(slot => slot.HasWafer)
                .Select(slot => slot.WaferId)
                .Concat(new[] { snapshot.ChamberA, snapshot.ChamberB, snapshot.ChamberC }
                    .Where(chamber => chamber.HasWafer)
                    .Select(chamber => chamber.WaferId))
                .Concat(snapshot.IsWaferOnBlade ? [snapshot.WaferIdOnBlade] : [])
                .Where(wafer => !string.IsNullOrWhiteSpace(wafer))
                .OrderBy(wafer => wafer)
                .ToArray();

            var expected = Enumerable.Range(1, TotalWafers).Select(index => $"W{index:00}").ToArray();
            if (!waferIds.SequenceEqual(expected))
            {
                throw new InvalidOperationException($"Sequence snapshot '{snapshot.StepName}' violated the five-wafer invariant: {string.Join(", ", waferIds)}");
            }
        }
    }

    private sealed class ChamberState
    {
        public ChamberState(string name, string stationKey, string role, string recipeName, string processStep)
        {
            Name = name;
            StationKey = stationKey;
            Role = role;
            RecipeName = recipeName;
            ProcessStep = processStep;
        }

        public string Name { get; }
        public string StationKey { get; }
        public string Role { get; }
        public string RecipeName { get; }
        public string ProcessStep { get; }
        public string WaferId { get; private set; } = string.Empty;
        public string ProcessState { get; set; } = "Empty";
        public int RemainingSeconds { get; set; }
        public double ProgressPercent { get; set; }
        public ChamberDoorSequenceState DoorState { get; set; } = ChamberDoorSequenceState.Closed;
        public bool HasWafer => !string.IsNullOrWhiteSpace(WaferId);

        public void Load(string waferId)
        {
            WaferId = waferId;
            ProcessState = "Loaded";
            RemainingSeconds = 0;
            ProgressPercent = 0;
        }

        public void Clear()
        {
            WaferId = string.Empty;
            ProcessState = "Empty";
            RemainingSeconds = 0;
            ProgressPercent = 0;
        }

        public ChamberPipelineSnapshot ToSnapshot() =>
            new(
                Name,
                Role,
                HasWafer,
                WaferId,
                ProcessState,
                RecipeName,
                HasWafer ? ProcessStep : "-",
                RemainingSeconds,
                ProgressPercent,
                DoorState == ChamberDoorSequenceState.Open);
    }

    private enum TransferKind
    {
        FoupAToChamberA,
        ChamberAToChamberB,
        ChamberBToChamberC,
        ChamberCToFoupB
    }

    private readonly record struct TransferIntent(
        TransferKind Kind,
        ChamberState? SourceChamber,
        ChamberState? TargetChamber,
        int SlotIndex);
}
