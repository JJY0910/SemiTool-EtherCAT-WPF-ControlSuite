using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using SemiTool.Application;
using SemiTool.Domain;
using SemiTool.Infrastructure;

namespace SemiTool.Hmi.Wpf.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtime;
    private readonly DispatcherTimer _timer;
    private bool _isRefreshing;

    public MainViewModel(RuntimeCoordinator runtime, string profilePath, string settingsPath)
    {
        _runtime = runtime;
        MachineTwin = new MachineTwinViewModel(runtime);
        Dashboard = new DashboardViewModel(runtime);
        Manual = new ManualControlViewModel(runtime);
        IoMonitor = new IoMonitorViewModel(runtime);
        AutoSequence = new AutoSequenceViewModel(runtime);
        WaferRecipeFlow = new WaferRecipeFlowViewModel(runtime);
        AlarmEventLog = new AlarmEventLogViewModel(runtime);
        Settings = new SettingsViewModel(runtime, profilePath, settingsPath);

        _runtime.Updated += (_, _) => App.Current.Dispatcher.InvokeAsync(RefreshAsync);
        _runtime.Alarms.AlarmsChanged += (_, _) => App.Current.Dispatcher.InvokeAsync(RefreshAsync);
        _runtime.Events.EntriesChanged += (_, _) => App.Current.Dispatcher.InvokeAsync(RefreshAsync);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_runtime.Profile.Communication.StatusPushIntervalMs)
        };
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();
    }

    public DashboardViewModel Dashboard { get; }
    public MachineTwinViewModel MachineTwin { get; }
    public ManualControlViewModel Manual { get; }
    public IoMonitorViewModel IoMonitor { get; }
    public AutoSequenceViewModel AutoSequence { get; }
    public WaferRecipeFlowViewModel WaferRecipeFlow { get; }
    public AlarmEventLogViewModel AlarmEventLog { get; }
    public SettingsViewModel Settings { get; }

    public async Task RefreshAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        try
        {
            _isRefreshing = true;
            var status = await _runtime.BuildStatusAsync();
            MachineTwin.Refresh(status);
            Dashboard.Refresh(status);
            Manual.Refresh();
            IoMonitor.Refresh(status);
            AutoSequence.Refresh();
            WaferRecipeFlow.Refresh();
            AlarmEventLog.Refresh();
            Settings.Refresh();
        }
        catch (Exception ex)
        {
            _runtime.Events.Error(nameof(MainViewModel), $"Refresh failed: {ex.Message}");
        }
        finally
        {
            _isRefreshing = false;
        }
    }
}

public sealed class DashboardViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtime;
    private string _connectionStatus = "Disconnected";
    private string _mode = "Simulator";
    private string _machineState = "Offline";
    private string _currentStep = "Ready";
    private string _selectedRecipe = string.Empty;
    private string _waferTransferSummary = string.Empty;
    private string _alarmSummary = string.Empty;
    private string _axisSummary = "Z 0 / Theta 0";

    public DashboardViewModel(RuntimeCoordinator runtime)
    {
        _runtime = runtime;
        ConnectCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.ConnectAsync));
        DisconnectCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.DisconnectAsync));
        StartCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.StartAutoAsync));
        StopCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.StopAutoAsync));
        PauseCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.PauseAutoAsync));
        ResetCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.ResetAsync));
        EmergencyStopCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.EmergencyStopAsync));
    }

    public string ConnectionStatus { get => _connectionStatus; private set => SetProperty(ref _connectionStatus, value); }
    public string Mode { get => _mode; private set => SetProperty(ref _mode, value); }
    public string MachineState { get => _machineState; private set => SetProperty(ref _machineState, value); }
    public string CurrentStep { get => _currentStep; private set => SetProperty(ref _currentStep, value); }
    public string SelectedRecipe { get => _selectedRecipe; private set => SetProperty(ref _selectedRecipe, value); }
    public string WaferTransferSummary { get => _waferTransferSummary; private set => SetProperty(ref _waferTransferSummary, value); }
    public string AlarmSummary { get => _alarmSummary; private set => SetProperty(ref _alarmSummary, value); }
    public string AxisSummary { get => _axisSummary; private set => SetProperty(ref _axisSummary, value); }

    public AsyncRelayCommand ConnectCommand { get; }
    public AsyncRelayCommand DisconnectCommand { get; }
    public AsyncRelayCommand StartCommand { get; }
    public AsyncRelayCommand StopCommand { get; }
    public AsyncRelayCommand PauseCommand { get; }
    public AsyncRelayCommand ResetCommand { get; }
    public AsyncRelayCommand EmergencyStopCommand { get; }

    public void Refresh(EquipmentStatus status)
    {
        ConnectionStatus = status.IsConnected ? "Connected" : "Disconnected";
        Mode = status.Mode.ToString();
        MachineState = status.MachineState.ToString();
        CurrentStep = $"{status.StepNumber}: {status.CurrentStep}";
        SelectedRecipe = status.SelectedRecipe;
        WaferTransferSummary = status.WaferTransferSummary;
        AlarmSummary = status.AlarmSummary;
        AxisSummary = $"Z {status.ZPosition} / Theta {status.ThetaPosition}";
    }

    private Task RunAsync(Func<CancellationToken, Task> action) =>
        ViewModelErrorHandler.RunAsync(_runtime, nameof(DashboardViewModel), action);
}

public sealed class ManualControlViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtime;
    private long _targetZ;
    private long _targetTheta;
    private bool _isManualEnabled = true;

    public ManualControlViewModel(RuntimeCoordinator runtime)
    {
        _runtime = runtime;
        ServoOnCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.Sequence.ServoOnAsync));
        ServoOffCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.Sequence.ServoOffAsync));
        HomeZCommand = new AsyncRelayCommand(_ => RunAsync(ct => _runtime.Sequence.HomeAxisAsync(AxisId.Z, ct)));
        HomeThetaCommand = new AsyncRelayCommand(_ => RunAsync(ct => _runtime.Sequence.HomeAxisAsync(AxisId.Theta, ct)));
        MoveZCommand = new AsyncRelayCommand(_ => RunAsync(ct => _runtime.Sequence.MoveAxisAbsoluteAsync(AxisId.Z, TargetZ, ct)));
        MoveThetaCommand = new AsyncRelayCommand(_ => RunAsync(ct => _runtime.Sequence.MoveAxisAbsoluteAsync(AxisId.Theta, TargetTheta, ct)));
        MovePoseCommand = new AsyncRelayCommand(p => RunAsync(ct => _runtime.Sequence.MoveToNamedPoseAsync(p?.ToString() ?? "Home", ct)));
        CylinderForwardCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.Sequence.CylinderForwardAsync));
        CylinderBackwardCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.Sequence.CylinderBackwardAsync));
        VacuumSuctionCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.Sequence.VacuumSuctionAsync));
        VacuumExhaustCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.Sequence.VacuumExhaustAsync));
        SetOutputCommand = new AsyncRelayCommand(p => RunAsync(ct => SetOutputAsync(p, ct)));
        DoorCommand = new AsyncRelayCommand(p => RunAsync(ct => DoorAsync(p, ct)));
    }

    public long TargetZ { get => _targetZ; set => SetProperty(ref _targetZ, value); }
    public long TargetTheta { get => _targetTheta; set => SetProperty(ref _targetTheta, value); }
    public bool IsManualEnabled { get => _isManualEnabled; private set => SetProperty(ref _isManualEnabled, value); }

    public AsyncRelayCommand ServoOnCommand { get; }
    public AsyncRelayCommand ServoOffCommand { get; }
    public AsyncRelayCommand HomeZCommand { get; }
    public AsyncRelayCommand HomeThetaCommand { get; }
    public AsyncRelayCommand MoveZCommand { get; }
    public AsyncRelayCommand MoveThetaCommand { get; }
    public AsyncRelayCommand MovePoseCommand { get; }
    public AsyncRelayCommand CylinderForwardCommand { get; }
    public AsyncRelayCommand CylinderBackwardCommand { get; }
    public AsyncRelayCommand VacuumSuctionCommand { get; }
    public AsyncRelayCommand VacuumExhaustCommand { get; }
    public AsyncRelayCommand SetOutputCommand { get; }
    public AsyncRelayCommand DoorCommand { get; }

    public void Refresh()
    {
        IsManualEnabled = !_runtime.Safety.IsAutoRunning;
    }

    private async Task SetOutputAsync(object? parameter, CancellationToken cancellationToken)
    {
        var parts = (parameter?.ToString() ?? string.Empty).Split('|');
        var point = Enum.Parse<IoPoint>(parts[0]);
        var value = bool.Parse(parts[1]);
        await _runtime.Sequence.SetOutputAsync(point, value, cancellationToken);
    }

    private async Task DoorAsync(object? parameter, CancellationToken cancellationToken)
    {
        var parts = (parameter?.ToString() ?? string.Empty).Split('|');
        var chamber = Enum.Parse<ChamberId>(parts[0]);
        if (string.Equals(parts[1], "Open", StringComparison.OrdinalIgnoreCase))
        {
            await _runtime.Sequence.OpenChamberDoorAsync(chamber, cancellationToken);
        }
        else
        {
            await _runtime.Sequence.CloseChamberDoorAsync(chamber, cancellationToken);
        }
    }

    private Task RunAsync(Func<CancellationToken, Task> action) =>
        ViewModelErrorHandler.RunAsync(_runtime, nameof(ManualControlViewModel), action);
}

public sealed class IoMonitorViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtime;
    private bool _canToggleInputs = true;

    public IoMonitorViewModel(RuntimeCoordinator runtime)
    {
        _runtime = runtime;
        Inputs = new ObservableCollection<IoSignalViewModel>(
            runtime.Profile.GetInputChannels().Select(channel => new IoSignalViewModel(channel, ToggleInputAsync)));
        Outputs = new ObservableCollection<IoSignalViewModel>(
            runtime.Profile.GetOutputChannels().Select(channel => new IoSignalViewModel(channel, null)));
    }

    public ObservableCollection<IoSignalViewModel> Inputs { get; }
    public ObservableCollection<IoSignalViewModel> Outputs { get; }
    public bool CanToggleInputs { get => _canToggleInputs; private set => SetProperty(ref _canToggleInputs, value); }

    public void Refresh(EquipmentStatus status)
    {
        CanToggleInputs = status.Mode == OperatingMode.Simulator;
        foreach (var input in Inputs)
        {
            input.IsOn = status.Inputs.TryGetValue(input.Point, out var value) && value;
            input.CanToggle = CanToggleInputs;
        }

        foreach (var output in Outputs)
        {
            output.IsOn = status.Outputs.TryGetValue(output.Point, out var value) && value;
        }
    }

    private Task ToggleInputAsync(IoSignalViewModel signal, CancellationToken cancellationToken) =>
        ViewModelErrorHandler.RunAsync(
            _runtime,
            nameof(IoMonitorViewModel),
            ct => _runtime.Controller.SetSimulatorInputAsync(signal.Point, !signal.IsOn, ct),
            cancellationToken);
}

public sealed class IoSignalViewModel : ObservableObject
{
    private readonly Func<IoSignalViewModel, CancellationToken, Task>? _toggle;
    private bool _isOn;
    private bool _canToggle;

    public IoSignalViewModel(IoChannel channel, Func<IoSignalViewModel, CancellationToken, Task>? toggle)
    {
        Point = channel.Point;
        Channel = channel.Channel;
        Name = channel.DisplayName;
        _toggle = toggle;
        ToggleCommand = new AsyncRelayCommand(_ => _toggle?.Invoke(this, CancellationToken.None) ?? Task.CompletedTask, _ => CanToggle);
    }

    public IoPoint Point { get; }
    public int Channel { get; }
    public string Name { get; }
    public bool IsOn { get => _isOn; set => SetProperty(ref _isOn, value); }
    public bool CanToggle
    {
        get => _canToggle;
        set
        {
            if (SetProperty(ref _canToggle, value))
            {
                ToggleCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand ToggleCommand { get; }
}

public sealed class AutoSequenceViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtime;
    private string _sequenceName = "Idle";
    private int _stepNumber;
    private string _stepDescription = "Ready";
    private string _elapsed = "00:00:00";
    private string _timeout = "00:00:00";

    public AutoSequenceViewModel(RuntimeCoordinator runtime)
    {
        _runtime = runtime;
        StartCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.StartAutoAsync));
        StopCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.StopAutoAsync));
        PauseCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.PauseAutoAsync));
        ResetCommand = new AsyncRelayCommand(_ => RunAsync(_runtime.ResetAsync));
        TransferQueue = new ObservableCollection<string>();
    }

    public string SequenceName { get => _sequenceName; private set => SetProperty(ref _sequenceName, value); }
    public int StepNumber { get => _stepNumber; private set => SetProperty(ref _stepNumber, value); }
    public string StepDescription { get => _stepDescription; private set => SetProperty(ref _stepDescription, value); }
    public string Elapsed { get => _elapsed; private set => SetProperty(ref _elapsed, value); }
    public string Timeout { get => _timeout; private set => SetProperty(ref _timeout, value); }
    public ObservableCollection<string> TransferQueue { get; }
    public AsyncRelayCommand StartCommand { get; }
    public AsyncRelayCommand StopCommand { get; }
    public AsyncRelayCommand PauseCommand { get; }
    public AsyncRelayCommand ResetCommand { get; }

    public void Refresh()
    {
        SequenceName = _runtime.Sequence.CurrentSequenceName;
        StepNumber = _runtime.Sequence.StepNumber;
        StepDescription = _runtime.Sequence.StepDescription;
        Elapsed = _runtime.Sequence.Elapsed.ToString(@"hh\:mm\:ss");
        Timeout = _runtime.Sequence.Timeout.ToString(@"hh\:mm\:ss");
        TransferQueue.Clear();
        foreach (var item in _runtime.Scheduler.BuildTransferQueueSnapshot())
        {
            TransferQueue.Add(item);
        }
    }

    private Task RunAsync(Func<CancellationToken, Task> action) =>
        ViewModelErrorHandler.RunAsync(_runtime, nameof(AutoSequenceViewModel), action);
}

public sealed class WaferRecipeFlowViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtime;
    private string _selectedRecipeKey = string.Empty;

    public WaferRecipeFlowViewModel(RuntimeCoordinator runtime)
    {
        _runtime = runtime;
        FoupA = new ObservableCollection<SlotStateViewModel>();
        FoupB = new ObservableCollection<SlotStateViewModel>();
        Chambers = new ObservableCollection<ChamberStateViewModel>();
        RecipeKeys = new ObservableCollection<string>(runtime.Recipes.Recipes.Keys.OrderBy(key => key));
        _selectedRecipeKey = runtime.Recipes.SelectedRecipeKey;
    }

    public ObservableCollection<SlotStateViewModel> FoupA { get; }
    public ObservableCollection<SlotStateViewModel> FoupB { get; }
    public ObservableCollection<ChamberStateViewModel> Chambers { get; }
    public ObservableCollection<string> RecipeKeys { get; }
    public string SelectedRecipeKey
    {
        get => _selectedRecipeKey;
        set
        {
            if (SetProperty(ref _selectedRecipeKey, value) && !string.IsNullOrWhiteSpace(value))
            {
                _runtime.Recipes.SelectRecipe(value);
            }
        }
    }

    public void Refresh()
    {
        Replace(FoupA, _runtime.Scheduler.State.FoupA.Select(SlotStateViewModel.From));
        Replace(FoupB, _runtime.Scheduler.State.FoupB.Select(SlotStateViewModel.From));
        Replace(Chambers, new[]
        {
            ChamberStateViewModel.From(_runtime.Scheduler.State.PmA),
            ChamberStateViewModel.From(_runtime.Scheduler.State.PmB),
            ChamberStateViewModel.From(_runtime.Scheduler.State.PmC)
        });
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}

public sealed record SlotStateViewModel(int Slot, string WaferId, string State)
{
    public static SlotStateViewModel From(WaferSlotState source) =>
        new(source.Slot, source.WaferId, source.HasWafer ? "Loaded" : "Empty");
}

public sealed record ChamberStateViewModel(string Chamber, string WaferId, string Recipe, string State, int RemainingSeconds)
{
    public static ChamberStateViewModel From(ChamberProcessState source)
    {
        var state = !source.HasWafer
            ? "Empty"
            : source.ProcessComplete ? "Complete" : "Processing";
        return new($"PM {source.Chamber}", source.WaferId, source.RecipeName, state, source.RemainingSeconds);
    }
}

public sealed class AlarmEventLogViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtime;

    public AlarmEventLogViewModel(RuntimeCoordinator runtime)
    {
        _runtime = runtime;
        Alarms = new ObservableCollection<AlarmRecord>();
        Events = new ObservableCollection<EventLogEntry>();
        ExportCommand = new AsyncRelayCommand(_ => ViewModelErrorHandler.RunAsync(
            _runtime,
            nameof(AlarmEventLogViewModel),
            ct => _runtime.Events.ExportCsvAsync(Path.Combine("logs", "event-log.csv"), ct)));
    }

    public ObservableCollection<AlarmRecord> Alarms { get; }
    public ObservableCollection<EventLogEntry> Events { get; }
    public AsyncRelayCommand ExportCommand { get; }

    public void Refresh()
    {
        Alarms.Clear();
        foreach (var alarm in _runtime.Alarms.Alarms.OrderByDescending(alarm => alarm.OccurredTime))
        {
            Alarms.Add(alarm);
        }

        Events.Clear();
        foreach (var entry in _runtime.Events.Entries.OrderByDescending(entry => entry.Timestamp).Take(200))
        {
            Events.Add(entry);
        }
    }
}

public sealed class SettingsViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtime;
    private readonly AppSettingsStore _store = new();
    private readonly string _settingsPath;
    private OperatingMode _selectedMode = OperatingMode.Simulator;
    private string _vendorDllPath = Path.Combine("libs", "IEG3268_" + "Dll.dll");
    private string _profileFilePath;
    private int _pollingIntervalMs = 300;
    private bool _requireDoorSensorInterlock = true;
    private bool _requireCylinderSensorInterlock = true;
    private bool _hardwareUnlocked;
    private string _status = "Simulator mode is the startup default.";

    public SettingsViewModel(RuntimeCoordinator runtime, string profilePath, string settingsPath)
    {
        _runtime = runtime;
        _profileFilePath = profilePath;
        _settingsPath = settingsPath;
        Modes = new ObservableCollection<OperatingMode>(Enum.GetValues<OperatingMode>());
        ApplyCommand = new AsyncRelayCommand(_ => ApplyAsync());
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
    }

    public ObservableCollection<OperatingMode> Modes { get; }
    public OperatingMode SelectedMode { get => _selectedMode; set => SetProperty(ref _selectedMode, value); }
    public string VendorDllPath { get => _vendorDllPath; set => SetProperty(ref _vendorDllPath, value); }
    public string ProfileFilePath { get => _profileFilePath; set => SetProperty(ref _profileFilePath, value); }
    public int PollingIntervalMs { get => _pollingIntervalMs; set => SetProperty(ref _pollingIntervalMs, value); }
    public bool RequireDoorSensorInterlock { get => _requireDoorSensorInterlock; set => SetProperty(ref _requireDoorSensorInterlock, value); }
    public bool RequireCylinderSensorInterlock { get => _requireCylinderSensorInterlock; set => SetProperty(ref _requireCylinderSensorInterlock, value); }
    public bool HardwareUnlocked { get => _hardwareUnlocked; set => SetProperty(ref _hardwareUnlocked, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public AsyncRelayCommand ApplyCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand LoadCommand { get; }

    public void Refresh()
    {
        SelectedMode = _runtime.Controller.Mode;
        HardwareUnlocked = _runtime.Controller.HardwareUnlocked;
    }

    private Task ApplyAsync() => ViewModelErrorHandler.RunAsync(_runtime, nameof(SettingsViewModel), ct =>
    {
        ct.ThrowIfCancellationRequested();
        if (_runtime.Controller.IsConnected)
        {
            throw new InvalidOperationException("Disconnect before applying hardware mode settings.");
        }

        _runtime.Controller.ConfigureRealHardware(VendorDllPath, HardwareUnlocked);
        _runtime.Controller.SetMode(SelectedMode);
        Status = $"{SelectedMode} mode applied. Connect is still manual.";
        return Task.CompletedTask;
    });

    private Task SaveAsync() => ViewModelErrorHandler.RunAsync(_runtime, nameof(SettingsViewModel), async ct =>
    {
        await _store.SaveAsync(_settingsPath, ToSettings(), ct);
        Status = $"Settings saved to {_settingsPath}.";
    });

    private Task LoadAsync() => ViewModelErrorHandler.RunAsync(_runtime, nameof(SettingsViewModel), ct =>
    {
        ct.ThrowIfCancellationRequested();
        var settings = _store.LoadOrDefault(_settingsPath);
        SelectedMode = settings.Mode;
        VendorDllPath = settings.VendorDllPath;
        ProfileFilePath = settings.ProfileFilePath;
        PollingIntervalMs = settings.PollingIntervalMs;
        RequireDoorSensorInterlock = settings.RequireDoorSensorInterlock;
        RequireCylinderSensorInterlock = settings.RequireCylinderSensorInterlock;
        HardwareUnlocked = settings.HardwareUnlocked;
        Status = $"Settings loaded from {_settingsPath}. Apply is still required.";
        return Task.CompletedTask;
    });

    private AppSettings ToSettings() => new()
    {
        Mode = SelectedMode,
        VendorDllPath = VendorDllPath,
        ProfileFilePath = ProfileFilePath,
        PollingIntervalMs = PollingIntervalMs,
        RequireDoorSensorInterlock = RequireDoorSensorInterlock,
        RequireCylinderSensorInterlock = RequireCylinderSensorInterlock,
        HardwareUnlocked = HardwareUnlocked
    };
}

internal static class ViewModelErrorHandler
{
    public static async Task RunAsync(
        RuntimeCoordinator runtime,
        string source,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await action(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            runtime.Events.Warn(source, "Operation canceled.");
        }
        catch (Exception ex)
        {
            runtime.Events.Error(source, ex.Message);
            runtime.Alarms.Raise(
                AlarmCode.SequenceFailed,
                "HMI command failed",
                ex.Message,
                "Review the command, equipment state, and safety interlocks.");
        }
    }
}
