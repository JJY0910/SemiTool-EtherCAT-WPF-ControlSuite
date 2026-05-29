using System.Reflection;
using SemiTool.Domain;

namespace SemiTool.Application;

/// <summary>
/// Selects the simulator controller by default and lazy-loads the real hardware adapter only for explicit RealHardware use.
/// </summary>
/// <remarks>
/// This type intentionally lives outside <c>SemiTool.Hardware</c> so normal WPF startup, Visual Studio Designer preview,
/// and screenshot/debug capture can run without loading the hardware assembly. Real hardware behavior remains delegated
/// to <c>SemiTool.Hardware.Ieg3268EthercatController</c>, which is created only after the operator selects RealHardware,
/// unlocks hardware, and clicks Connect.
/// </remarks>
public sealed class SelectableEthercatController : IEthercatController
{
    private const string HardwareAssemblyName = "SemiTool.Hardware";
    private const string Ieg3268ControllerTypeName = "SemiTool.Hardware.Ieg3268EthercatController";
    private readonly EquipmentProfile _profile;
    private readonly SimulatedEthercatController _simulator;
    private readonly Func<EquipmentProfile, string, IEthercatController> _realControllerFactory;
    private IEthercatController? _realController;

    public SelectableEthercatController(
        EquipmentProfile profile,
        Func<EquipmentProfile, string, IEthercatController>? realControllerFactory = null)
    {
        _profile = profile;
        _simulator = new SimulatedEthercatController(profile);
        _realControllerFactory = realControllerFactory ?? CreateRealHardwareController;
    }

    public OperatingMode Mode { get; private set; } = OperatingMode.Simulator;
    public string VendorDllPath { get; private set; } = Path.Combine("libs", "IEG3268_" + "Dll.dll");
    public bool HardwareUnlocked { get; private set; }
    public bool IsConnected =>
        Mode == OperatingMode.Simulator
            ? _simulator.IsConnected
            : _realController?.IsConnected ?? false;

    public SimulatedEthercatController Simulator => _simulator;

    /// <summary>
    /// Stores Real Hardware settings from the Settings screen without loading the hardware assembly or vendor DLL.
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

        var controller = Mode == OperatingMode.Simulator
            ? _simulator
            : GetRealControllerForConnect();

        await controller.ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (Mode == OperatingMode.Simulator)
        {
            return _simulator.DisconnectAsync(cancellationToken);
        }

        return _realController?.DisconnectAsync(cancellationToken) ?? Task.CompletedTask;
    }

    public Task<bool> ReadDigitalInputAsync(IoPoint point, CancellationToken cancellationToken = default) =>
        GetControllerForCommand().ReadDigitalInputAsync(point, cancellationToken);

    public Task WriteDigitalOutputAsync(IoPoint point, bool value, CancellationToken cancellationToken = default) =>
        GetControllerForCommand().WriteDigitalOutputAsync(point, value, cancellationToken);

    public Task<IReadOnlyDictionary<IoPoint, bool>> ReadAllInputsAsync(CancellationToken cancellationToken = default) =>
        GetControllerForCommand().ReadAllInputsAsync(cancellationToken);

    public Task<IReadOnlyDictionary<IoPoint, bool>> ReadAllOutputsAsync(CancellationToken cancellationToken = default) =>
        GetControllerForCommand().ReadAllOutputsAsync(cancellationToken);

    public Task ServoOnAsync(CancellationToken cancellationToken = default) =>
        GetControllerForCommand().ServoOnAsync(cancellationToken);

    public Task ServoOffAsync(CancellationToken cancellationToken = default) =>
        GetControllerForCommand().ServoOffAsync(cancellationToken);

    public Task HomeAxisAsync(AxisId axis, CancellationToken cancellationToken = default) =>
        GetControllerForCommand().HomeAxisAsync(axis, cancellationToken);

    public Task MoveAxisAbsoluteAsync(AxisId axis, long targetPosition, CancellationToken cancellationToken = default) =>
        GetControllerForCommand().MoveAxisAbsoluteAsync(axis, targetPosition, cancellationToken);

    public Task<long> ReadAxisPositionAsync(AxisId axis, CancellationToken cancellationToken = default) =>
        GetControllerForCommand().ReadAxisPositionAsync(axis, cancellationToken);

    public Task StopMotionAsync(CancellationToken cancellationToken = default) =>
        GetControllerForCommand().StopMotionAsync(cancellationToken);

    public Task EmergencyStopAsync(CancellationToken cancellationToken = default) =>
        GetControllerForCommand().EmergencyStopAsync(cancellationToken);

    public Task ResetAlarmAsync(CancellationToken cancellationToken = default) =>
        GetControllerForCommand().ResetAlarmAsync(cancellationToken);

    public Task SetSimulatorInputAsync(IoPoint point, bool value, CancellationToken cancellationToken = default)
    {
        if (Mode != OperatingMode.Simulator)
        {
            throw new InvalidOperationException("Input toggling is only available in Simulator mode.");
        }

        return _simulator.SetInputAsync(point, value, cancellationToken);
    }

    private IEthercatController GetControllerForCommand() =>
        Mode == OperatingMode.Simulator
            ? _simulator
            : _realController ?? throw new InvalidOperationException(
                "Real hardware controller is not connected. Click Connect before issuing commands.");

    private IEthercatController GetRealControllerForConnect() =>
        _realController ??= _realControllerFactory(_profile, VendorDllPath);

    private static IEthercatController CreateRealHardwareController(EquipmentProfile profile, string vendorDllPath)
    {
        // Loading by name keeps simulator/capture/designer paths free of a hard reference to SemiTool.Hardware.
        var assembly = Assembly.Load(HardwareAssemblyName);
        var adapterType = assembly.GetType(Ieg3268ControllerTypeName, throwOnError: true)
            ?? throw new InvalidOperationException($"Could not find {Ieg3268ControllerTypeName} in {HardwareAssemblyName}.");

        var instance = Activator.CreateInstance(adapterType, profile, vendorDllPath);
        return instance as IEthercatController
            ?? throw new InvalidOperationException($"{Ieg3268ControllerTypeName} does not implement {nameof(IEthercatController)}.");
    }
}
