using System.Reflection;
using SemiTool.Domain;

namespace SemiTool.Hardware;

public sealed class Ieg3268EthercatController : IEthercatController
{
    private readonly EquipmentProfile _profile;
    private readonly string _dllPath;
    private string? _resolvedDllPath;
    private object? _driver;
    private Type? _driverType;

    public Ieg3268EthercatController(EquipmentProfile profile, string dllPath)
    {
        _profile = profile;
        _dllPath = dllPath;
    }

    public bool IsConnected { get; private set; }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolution = VendorDllResolver.Resolve(_dllPath);
        if (!resolution.Success || string.IsNullOrWhiteSpace(resolution.ResolvedPath))
        {
            throw new InvalidOperationException(resolution.ErrorMessage);
        }

        _resolvedDllPath = resolution.ResolvedPath;
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(_resolvedDllPath);
        }
        catch (BadImageFormatException ex)
        {
            throw new InvalidOperationException(
                "Failed to load IEG3268_Dll.dll because of a process/DLL architecture mismatch. " +
                "The vendor DLL may be 32-bit. Run Real Hardware mode using x86, or provide a matching x64 vendor DLL. " +
                "Simulator mode is still available.",
                ex);
        }

        _driverType = assembly.GetType(_profile.Hardware.Adapter, throwOnError: false)
            ?? throw new InvalidOperationException(
                $"Vendor adapter type '{_profile.Hardware.Adapter}' was not found in DLL '{_resolvedDllPath}'.");
        _driver = Activator.CreateInstance(_driverType!)
            ?? throw new InvalidOperationException($"Could not create vendor driver '{_profile.Hardware.Adapter}'.");

        var connected = Invoke<bool>(_profile.Hardware.ConnectionMethod);
        if (!connected)
        {
            throw new InvalidOperationException("Vendor EtherCAT connection method returned false.");
        }

        TryInvoke("ReadData_Send_Start", _profile.Communication.ReadDataPeriodMs);
        TryInvoke("ReadData_Timer_Start");
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsConnected)
        {
            TryInvoke("CIFX_50RE_Disconnect");
            IsConnected = false;
        }

        return Task.CompletedTask;
    }

    public Task<bool> ReadDigitalInputAsync(IoPoint point, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        var channel = _profile.GetInputChannel(point);
        return Task.FromResult(Invoke<bool>("Digital_Input", channel));
    }

    public Task WriteDigitalOutputAsync(IoPoint point, bool value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        var channel = _profile.GetOutputChannel(point);
        Invoke("Digital_Output", channel, value);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyDictionary<IoPoint, bool>> ReadAllInputsAsync(CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<IoPoint, bool>();
        foreach (var channel in _profile.GetInputChannels())
        {
            values[channel.Point] = await ReadDigitalInputAsync(channel.Point, cancellationToken).ConfigureAwait(false);
        }

        return values;
    }

    public Task<IReadOnlyDictionary<IoPoint, bool>> ReadAllOutputsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyDictionary<IoPoint, bool>>(
            _profile.GetOutputChannels().ToDictionary(channel => channel.Point, _ => false));
    }

    public Task ServoOnAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        TryInvoke("Axis1_OFF");
        TryInvoke("Axis2_OFF");
        TryInvoke("Axis1_UD_Config_Update", _profile.Motion.Velocity, _profile.Motion.MaxVelocity, _profile.Motion.Deceleration, _profile.Motion.Acceleration);
        TryInvoke("Axis2_LR_Config_Update", _profile.Motion.Velocity, _profile.Motion.MaxVelocity, _profile.Motion.Deceleration, _profile.Motion.Acceleration);
        Invoke("Axis1_ON");
        Invoke("Axis2_ON");
        return Task.CompletedTask;
    }

    public Task ServoOffAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        TryInvoke("Axis1_OFF");
        TryInvoke("Axis2_OFF");
        return Task.CompletedTask;
    }

    public Task HomeAxisAsync(AxisId axis, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        Invoke(axis == AxisId.Z ? "Axis1_UD_Homming" : "Axis2_LR_Homming");
        return Task.CompletedTask;
    }

    public Task MoveAxisAbsoluteAsync(AxisId axis, long targetPosition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        if (axis == AxisId.Z)
        {
            Invoke("Axis1_UD_POS_Update", targetPosition);
            Invoke("Axis1_UD_Move_Send");
        }
        else
        {
            Invoke("Axis2_LR_POS_Update", targetPosition);
            Invoke("Axis2_LR_Move_Send");
        }

        return Task.CompletedTask;
    }

    public Task<long> ReadAxisPositionAsync(AxisId axis, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        var methodName = axis == AxisId.Z ? "Axis1_is_PosData" : "Axis2_is_PosData";
        return Task.FromResult(Convert.ToInt64(Invoke<object>(methodName)));
    }

    public Task StopMotionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TryInvoke("Axis1_Stop");
        TryInvoke("Axis2_Stop");
        return Task.CompletedTask;
    }

    public async Task EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        await StopMotionAsync(cancellationToken).ConfigureAwait(false);
        foreach (var output in RiskyOutputs)
        {
            await WriteDigitalOutputAsync(output, false, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task ResetAlarmAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TryInvoke("Alarm_Reset");
        return Task.CompletedTask;
    }

    private static readonly IoPoint[] RiskyOutputs =
    [
        IoPoint.CylinderForward,
        IoPoint.CylinderBackward,
        IoPoint.VacuumSuction,
        IoPoint.VacuumExhaust,
        IoPoint.ChamberADoorOpen,
        IoPoint.ChamberADoorClose,
        IoPoint.ChamberBDoorOpen,
        IoPoint.ChamberBDoorClose,
        IoPoint.ChamberCDoorOpen,
        IoPoint.ChamberCDoorClose
    ];

    private void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("The real EtherCAT controller is not connected.");
        }
    }

    private void Invoke(string methodName, params object[] arguments)
    {
        _ = Invoke<object?>(methodName, arguments);
    }

    private T Invoke<T>(string methodName, params object[] arguments)
    {
        var method = FindMethod(methodName, throwOnError: true)!;
        var result = method.Invoke(_driver, arguments);
        if (result is null)
        {
            return default!;
        }

        return (T)Convert.ChangeType(result, typeof(T));
    }

    private bool TryInvoke(string methodName, params object[] arguments)
    {
        var method = FindMethod(methodName, throwOnError: false);
        if (method is null)
        {
            return false;
        }

        method.Invoke(_driver, arguments);
        return true;
    }

    private MethodInfo? FindMethod(string methodName, bool throwOnError)
    {
        if (_driver is null || _driverType is null)
        {
            throw new InvalidOperationException("The vendor driver has not been loaded.");
        }

        var method = _driverType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        if (method is null && throwOnError)
        {
            throw new InvalidOperationException(
                $"Vendor method '{methodName}' was not found on adapter type '{_driverType.FullName}' from DLL '{_resolvedDllPath}'.",
                new MissingMethodException(_driverType.FullName, methodName));
        }

        return method;
    }
}
