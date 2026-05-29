namespace SemiTool.Domain;

/// <summary>
/// Builds the slow, readable Machine Twin teaching sequence used by the WPF runtime demo.
/// </summary>
/// <remarks>
/// This is a simulator presentation model only. It preserves the five wafer
/// invariant while making the door, blade, vacuum, and process-gating rules
/// visible to students and interview reviewers. It does not touch EtherCAT
/// axis targets, I/O mapping, vendor DLL loading, or real-hardware adapter
/// behavior.
/// </remarks>
internal static class TeachingWaferPipelineSequence
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
        private string _stationKey = "FoupA";
        private string _previousStation = "-";
        private string _nextStation = "Chamber A";
        private string _zState = "Z Safe";
        private BladeTeachingState _bladeState = BladeTeachingState.Retracted;
        private VacuumTeachingState _vacuumState = VacuumTeachingState.Off;
        private RobotTeachingState _robotState = RobotTeachingState.Idle;
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
                "FOUP A starts with W01-W05 waiting. Blade retracted, vacuum off, all chamber doors closed.",
                "Ready",
                delayMs: _timing.ThetaSwingMs);

            var guard = 0;
            while (!IsComplete())
            {
                guard++;
                if (guard > 100)
                {
                    throw new InvalidOperationException("Teaching sequence guard exceeded before all wafers reached FOUP B.");
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
            _bladeState = BladeTeachingState.Retracted;
            _vacuumState = VacuumTeachingState.Off;
            _robotState = RobotTeachingState.Idle;
            _towerGreen = true;
            Add(
                "FOUP B 5 Wafers Complete",
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
            _robotState = RobotTeachingState.MovingTheta;
            _zState = transfer.Kind == TransferKind.FoupAToChamberA ? $"Z Work / {sourceName}" : "Z Safe";
            _bladeState = BladeTeachingState.Retracted;
            _vacuumState = VacuumTeachingState.Off;
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

            _robotState = RobotTeachingState.MovingZ;
            _bladeState = BladeTeachingState.Extending;
            _vacuumState = VacuumTeachingState.Off;
            Add(
                $"Blade Extending Into {sourceName}",
                transfer.SourceChamber == _chamberA ? Once("08-chamber-a-unload-after-process-complete.png") : null,
                PipelineStateKind.Running,
                $"Blade extending into {sourceName}",
                $"Blade enters {sourceName} only after the target path is safe.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.BladeExtendMs);

            _robotState = RobotTeachingState.Picking;
            _bladeState = BladeTeachingState.Extended;
            _vacuumState = VacuumTeachingState.SuctionOn;
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

            _robotState = RobotTeachingState.MovingZ;
            _bladeState = BladeTeachingState.Retracting;
            Add(
                $"Blade Retracting With {waferId}",
                null,
                PipelineStateKind.Running,
                $"Blade retracting with {waferId}",
                $"Blade retracts with {waferId} before any chamber door can close.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.BladeRetractMs);

            _bladeState = BladeTeachingState.Retracted;
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
            _robotState = RobotTeachingState.MovingTheta;
            _zState = transfer.Kind == TransferKind.ChamberCToFoupB ? $"Z Work / {targetName}" : "Z Safe";
            _bladeState = BladeTeachingState.Retracted;
            _vacuumState = VacuumTeachingState.SuctionOn;
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

            _robotState = RobotTeachingState.MovingZ;
            _bladeState = BladeTeachingState.Extending;
            Add(
                $"Blade Entering {targetName}",
                transfer.TargetChamber == _chamberA ? Once("04-blade-entering-chamber-a-door-open.png") : null,
                PipelineStateKind.Running,
                $"Blade extending into {targetName}",
                $"Blade enters {targetName} only while the chamber door is open.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.BladeExtendMs);

            _robotState = RobotTeachingState.Placing;
            _bladeState = BladeTeachingState.Extended;
            _vacuumState = VacuumTeachingState.ExhaustOrRelease;
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

            _robotState = RobotTeachingState.MovingZ;
            _vacuumState = VacuumTeachingState.Off;
            _bladeState = BladeTeachingState.Retracting;
            Add(
                $"Blade Retracting Empty From {targetName}",
                transfer.TargetChamber == _chamberA ? Once("06-blade-retracted-before-chamber-a-door-closes.png") : null,
                PipelineStateKind.Running,
                $"Blade retracting empty from {targetName}",
                "Door close is blocked until the blade is fully retracted.",
                $"{sourceName} -> {targetName}",
                waferId,
                _timing.BladeRetractMs);

            _bladeState = BladeTeachingState.Retracted;
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
            chamber.DoorState = ChamberDoorTeachingState.Opening;
            _robotState = RobotTeachingState.Idle;
            Add(
                $"{chamber.Name} Door Opening",
                chamber == _chamberA ? Once("03-chamber-a-door-opening.png") : null,
                PipelineStateKind.Running,
                action,
                $"{chamber.Name} door opening; blade remains retracted.",
                action,
                chamber.WaferId,
                _timing.DoorOpenCloseMs);

            chamber.DoorState = ChamberDoorTeachingState.Open;
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
            chamber.DoorState = ChamberDoorTeachingState.Closing;
            _robotState = RobotTeachingState.Idle;
            Add(
                $"{chamber.Name} Door Closing",
                null,
                PipelineStateKind.Running,
                action,
                $"{chamber.Name} door closing after blade is retracted.",
                action,
                chamber.WaferId,
                _timing.DoorOpenCloseMs);

            chamber.DoorState = ChamberDoorTeachingState.Closed;
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
            _robotState = RobotTeachingState.Idle;
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
                throw new InvalidOperationException("No transfer or process countdown was available in the teaching sequence.");
            }

            chamber.ProgressPercent = 100;
            chamber.RemainingSeconds = 0;
            chamber.ProcessState = "Completed";
            _robotState = RobotTeachingState.Idle;
            _bladeState = BladeTeachingState.Retracted;
            _vacuumState = VacuumTeachingState.Off;
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
                _robotState is RobotTeachingState.MovingTheta or RobotTeachingState.MovingZ,
                _zState,
                _bladeState is BladeTeachingState.Extending or BladeTeachingState.Extended,
                _bladeState is BladeTeachingState.Extending or BladeTeachingState.Extended,
                _bladeState is BladeTeachingState.Retracted or BladeTeachingState.Retracting,
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
            _chamberA.DoorState = ChamberDoorTeachingState.Closed;
            _chamberB.DoorState = ChamberDoorTeachingState.Closed;
            _chamberC.DoorState = ChamberDoorTeachingState.Closed;
            _bladeWaferId = string.Empty;
            _stationKey = "FoupA";
            _previousStation = "-";
            _nextStation = "FOUP A";
            _zState = "Z Safe";
            _bladeState = BladeTeachingState.Retracted;
            _vacuumState = VacuumTeachingState.Off;
            _robotState = RobotTeachingState.Idle;
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

        private static string VacuumLabel(VacuumTeachingState state) => state switch
        {
            VacuumTeachingState.SuctionOn => "Suction",
            VacuumTeachingState.ExhaustOrRelease => "Exhaust",
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
                throw new InvalidOperationException($"Teaching snapshot '{snapshot.StepName}' violated the five-wafer invariant: {string.Join(", ", waferIds)}");
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
        public ChamberDoorTeachingState DoorState { get; set; } = ChamberDoorTeachingState.Closed;
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
                DoorState == ChamberDoorTeachingState.Open);
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
