using SemiTool.Domain;

namespace SemiTool.Hardware;

/// <summary>
/// Chooses between the simulator and the real IEG3268 adapter while exposing one IEthercatController to the HMI.
/// </summary>
/// <remarks>
/// Startup stays in Simulator mode so a developer PC cannot accidentally connect to equipment. Real Hardware mode
/// requires both an explicit mode change and a hardware unlock before Connect is allowed.
/// </remarks>
public sealed class SelectableEthercatController : IEthercatController
{
    private readonly EquipmentProfile _profile;
    private readonly SimulatedEthercatController _simulator;
    private Ieg3268EthercatController? _realController;

    public SelectableEthercatController(EquipmentProfile profile)
    {
        _profile = profile;
        _simulator = new SimulatedEthercatController(profile);
    }

    public OperatingMode Mode { get; private set; } = OperatingMode.Simulator;
    public string VendorDllPath { get; private set; } = Path.Combine("libs", "IEG3268_" + "Dll.dll");
    public bool HardwareUnlocked { get; private set; }
    public bool IsConnected => ActiveController.IsConnected;

    public SimulatedEthercatController Simulator => _simulator;

    /// <summary>
    /// Stores Real Hardware settings from the Settings screen without loading the vendor DLL.
    /// </summary>
    public void ConfigureRealHardware(string vendorDllPath, bool hardwareUnlocked)
    {
        VendorDllPath = vendorDllPath;
        HardwareUnlocked = hardwareUnlocked;
        // Recreate the real adapter on next Connect so path/unlock changes are applied deterministically.
        _realController = null;
    }

    /// <summary>
    /// Switches simulator/real mode only while disconnected.
    /// </summary>
    public void SetMode(OperatingMode mode)
    {
        if (IsConnected)
        {
            throw new InvalidOperationException("Disconnect before switching simulator/real hardware mode.");
        }

        Mode = mode;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (Mode == OperatingMode.RealHardware && !HardwareUnlocked)
        {
            // Real hardware must never auto-connect from persisted settings alone.
            throw new InvalidOperationException("Real hardware control must be explicitly unlocked before connection.");
        }

        await ActiveController.ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        ActiveController.DisconnectAsync(cancellationToken);

    public Task<bool> ReadDigitalInputAsync(IoPoint point, CancellationToken cancellationToken = default) =>
        ActiveController.ReadDigitalInputAsync(point, cancellationToken);

    public Task WriteDigitalOutputAsync(IoPoint point, bool value, CancellationToken cancellationToken = default) =>
        ActiveController.WriteDigitalOutputAsync(point, value, cancellationToken);

    public Task<IReadOnlyDictionary<IoPoint, bool>> ReadAllInputsAsync(CancellationToken cancellationToken = default) =>
        ActiveController.ReadAllInputsAsync(cancellationToken);

    public Task<IReadOnlyDictionary<IoPoint, bool>> ReadAllOutputsAsync(CancellationToken cancellationToken = default) =>
        ActiveController.ReadAllOutputsAsync(cancellationToken);

    public Task ServoOnAsync(CancellationToken cancellationToken = default) =>
        ActiveController.ServoOnAsync(cancellationToken);

    public Task ServoOffAsync(CancellationToken cancellationToken = default) =>
        ActiveController.ServoOffAsync(cancellationToken);

    public Task HomeAxisAsync(AxisId axis, CancellationToken cancellationToken = default) =>
        ActiveController.HomeAxisAsync(axis, cancellationToken);

    public Task MoveAxisAbsoluteAsync(AxisId axis, long targetPosition, CancellationToken cancellationToken = default) =>
        ActiveController.MoveAxisAbsoluteAsync(axis, targetPosition, cancellationToken);

    public Task<long> ReadAxisPositionAsync(AxisId axis, CancellationToken cancellationToken = default) =>
        ActiveController.ReadAxisPositionAsync(axis, cancellationToken);

    public Task StopMotionAsync(CancellationToken cancellationToken = default) =>
        ActiveController.StopMotionAsync(cancellationToken);

    public Task EmergencyStopAsync(CancellationToken cancellationToken = default) =>
        ActiveController.EmergencyStopAsync(cancellationToken);

    public Task ResetAlarmAsync(CancellationToken cancellationToken = default) =>
        ActiveController.ResetAlarmAsync(cancellationToken);

    public Task SetSimulatorInputAsync(IoPoint point, bool value, CancellationToken cancellationToken = default)
    {
        if (Mode != OperatingMode.Simulator)
        {
            throw new InvalidOperationException("Input toggling is only available in Simulator mode.");
        }

        return _simulator.SetInputAsync(point, value, cancellationToken);
    }

    private IEthercatController ActiveController =>
        Mode == OperatingMode.Simulator
            ? _simulator
            // The DLL-backed adapter is lazy-created so Simulator mode remains available when IEG3268_Dll.dll is absent.
            : _realController ??= new Ieg3268EthercatController(_profile, VendorDllPath);
}
