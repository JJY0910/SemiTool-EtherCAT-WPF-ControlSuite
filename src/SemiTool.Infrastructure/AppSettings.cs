using SemiTool.Domain;

namespace SemiTool.Infrastructure;

public sealed class AppSettings
{
    public OperatingMode Mode { get; set; } = OperatingMode.Simulator;
    public string VendorDllPath { get; set; } = Path.Combine("libs", "IEG3268_" + "Dll.dll");
    public string ProfileFilePath { get; set; } = Path.Combine("config", "EquipmentProfile.finaltest.json");
    public int PollingIntervalMs { get; set; } = 300;
    public bool RequireDoorSensorInterlock { get; set; } = true;
    public bool RequireCylinderSensorInterlock { get; set; } = true;
    public bool HardwareUnlocked { get; set; }
}
