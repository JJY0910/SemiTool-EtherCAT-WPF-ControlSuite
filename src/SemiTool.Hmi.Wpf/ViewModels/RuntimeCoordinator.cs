using SemiTool.Application;
using SemiTool.Domain;
using SemiTool.Hardware;

namespace SemiTool.Hmi.Wpf.ViewModels;

public sealed class RuntimeCoordinator
{
    private CancellationTokenSource? _autoCts;

    public RuntimeCoordinator(
        EquipmentProfile profile,
        SelectableEthercatController controller,
        EquipmentSequenceService sequence,
        TransferScheduler scheduler,
        SafetyInterlockService safety,
        AlarmService alarms,
        EventLogService events,
        RecipeService recipes)
    {
        Profile = profile;
        Controller = controller;
        Sequence = sequence;
        Scheduler = scheduler;
        Safety = safety;
        Alarms = alarms;
        Events = events;
        Recipes = recipes;
    }

    public EquipmentProfile Profile { get; }
    public SelectableEthercatController Controller { get; }
    public EquipmentSequenceService Sequence { get; }
    public TransferScheduler Scheduler { get; }
    public SafetyInterlockService Safety { get; }
    public AlarmService Alarms { get; }
    public EventLogService Events { get; }
    public RecipeService Recipes { get; }

    public event EventHandler? Updated;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await Controller.ConnectAsync(cancellationToken).ConfigureAwait(false);
        Safety.MarkConnected();
        Events.Info(nameof(RuntimeCoordinator), $"{Controller.Mode} controller connected.");
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await StopAutoAsync(cancellationToken).ConfigureAwait(false);
        await Controller.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        Safety.MarkDisconnected();
        Events.Warn(nameof(RuntimeCoordinator), "Controller disconnected. Outputs were commanded OFF by the controller.");
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public Task StartAutoAsync(CancellationToken cancellationToken = default)
    {
        Safety.BeginAuto(Controller);
        _autoCts?.Cancel();
        _autoCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = RunAutoLoopAsync(_autoCts.Token);
        Updated?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task StopAutoAsync(CancellationToken cancellationToken = default)
    {
        _autoCts?.Cancel();
        if (Controller.IsConnected)
        {
            await Controller.StopMotionAsync(cancellationToken).ConfigureAwait(false);
        }

        if (Safety.IsAutoRunning)
        {
            Safety.StopAuto();
        }

        Updated?.Invoke(this, EventArgs.Empty);
    }

    public Task PauseAutoAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Safety.IsPaused)
        {
            Safety.ResumeAuto();
        }
        else
        {
            Safety.PauseAuto();
        }

        Updated?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await Sequence.ResetAsync(cancellationToken).ConfigureAwait(false);
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public async Task EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        _autoCts?.Cancel();
        await Sequence.EmergencyStopAsync(cancellationToken).ConfigureAwait(false);
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public async Task<EquipmentStatus> BuildStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new EquipmentStatus
        {
            Mode = Controller.Mode,
            MachineState = Safety.MachineState,
            IsConnected = Controller.IsConnected,
            IsHardwareUnlocked = Controller.HardwareUnlocked,
            IsAutoRunning = Safety.IsAutoRunning,
            IsPaused = Safety.IsPaused,
            IsHomedZ = Safety.IsHomedZ,
            IsHomedTheta = Safety.IsHomedTheta,
            CurrentStep = Sequence.StepDescription,
            StepNumber = Sequence.StepNumber,
            SelectedRecipe = Recipes.SelectedRecipe?.RecipeName ?? string.Empty,
            WaferTransferSummary = Scheduler.EvaluateNextTransfer().Description,
            AlarmSummary = Alarms.ActiveAlarms.Count == 0
                ? "No active alarms"
                : string.Join("; ", Alarms.ActiveAlarms.Select(alarm => $"{alarm.Code}: {alarm.Name}"))
        };

        if (Controller.IsConnected)
        {
            status.Inputs = new Dictionary<IoPoint, bool>(
                await Controller.ReadAllInputsAsync(cancellationToken).ConfigureAwait(false));
            status.Outputs = new Dictionary<IoPoint, bool>(
                await Controller.ReadAllOutputsAsync(cancellationToken).ConfigureAwait(false));
            status.ZPosition = await Controller.ReadAxisPositionAsync(AxisId.Z, cancellationToken).ConfigureAwait(false);
            status.ThetaPosition = await Controller.ReadAxisPositionAsync(AxisId.Theta, cancellationToken).ConfigureAwait(false);
        }

        return status;
    }

    private async Task RunAutoLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (Safety.IsAutoRunning && !cancellationToken.IsCancellationRequested)
            {
                if (!Safety.IsPaused)
                {
                    var decision = await Scheduler.ExecuteNextAsync(Sequence, cancellationToken).ConfigureAwait(false);
                    Events.Info(nameof(RuntimeCoordinator), $"Auto tick: {decision.Description}");
                    Updated?.Invoke(this, EventArgs.Empty);
                }

                var delay = Controller.Mode == OperatingMode.Simulator
                    ? Profile.Timing.AutoSimTickMs
                    : Profile.Timing.AutoRealTickMs;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Events.Info(nameof(RuntimeCoordinator), "Auto loop canceled.");
        }
        catch (Exception ex)
        {
            Safety.SetAlarmState();
            Alarms.Raise(
                AlarmCode.SequenceFailed,
                "Auto loop failed",
                ex.Message,
                "Review event log, recover equipment, reset alarms, and restart Auto.");
            Events.Error(nameof(RuntimeCoordinator), $"Auto loop failed: {ex.Message}");
        }
        finally
        {
            if (Safety.IsAutoRunning)
            {
                Safety.StopAuto();
            }

            Updated?.Invoke(this, EventArgs.Empty);
        }
    }
}
