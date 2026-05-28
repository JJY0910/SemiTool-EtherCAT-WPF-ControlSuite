using System.Globalization;

namespace SemiTool.Domain;

public sealed class EquipmentProfile
{
    public string ProfileName { get; init; } = string.Empty;
    public string SourceProject { get; init; } = string.Empty;
    public HardwareInfo Hardware { get; init; } = new();
    public CommunicationProfile Communication { get; init; } = new();
    public MotionProfile Motion { get; init; } = new();
    public TimingProfile Timing { get; init; } = new();
    public IoProfile Io { get; init; } = new();
    public Dictionary<string, RobotPose> Poses { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, FoupSlotPose> FoupSlots { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Recipe> Recipes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public RobotPose GetPose(string key)
    {
        if (Poses.TryGetValue(key, out var pose))
        {
            return pose;
        }

        throw new KeyNotFoundException($"Robot pose '{key}' was not found in the equipment profile.");
    }

    public RobotPose GetChamberPose(ChamberId chamber) => GetPose($"Chamber{chamber}");

    public FoupSlotPose GetFoupSlotPose(int slot)
    {
        var key = string.Create(CultureInfo.InvariantCulture, $"Slot{slot}");
        if (FoupSlots.TryGetValue(key, out var pose))
        {
            return pose;
        }

        throw new ArgumentOutOfRangeException(nameof(slot), slot, "FOUP slot is not defined in the equipment profile.");
    }

    public int GetOutputChannel(IoPoint point) => GetChannel(Io.DigitalOutputs, point, "digital output");

    public int GetInputChannel(IoPoint point) => GetChannel(Io.DigitalInputs, point, "digital input");

    public IReadOnlyList<IoChannel> GetOutputChannels() =>
        Io.DigitalOutputs
            .OrderBy(item => item.Key)
            .Select(item => new IoChannel(item.Value, item.Key, item.Value.GetDisplayName()))
            .ToArray();

    public IReadOnlyList<IoChannel> GetInputChannels() =>
        Io.DigitalInputs
            .OrderBy(item => item.Key)
            .Select(item => new IoChannel(item.Value, item.Key, item.Value.GetDisplayName()))
            .ToArray();

    private static int GetChannel(IReadOnlyDictionary<int, IoPoint> channels, IoPoint point, string kind)
    {
        foreach (var item in channels)
        {
            if (item.Value == point)
            {
                return item.Key;
            }
        }

        throw new KeyNotFoundException($"{point} is not mapped as a {kind}.");
    }
}

public sealed class HardwareInfo
{
    public string Adapter { get; init; } = string.Empty;
    public string ConnectionMethod { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}

public sealed class CommunicationProfile
{
    public int ReadDataPeriodMs { get; init; } = 300;
    public int StatusPushIntervalMs { get; init; } = 300;
}

public sealed class MotionProfile
{
    public string Axis1Name { get; init; } = "Z";
    public string Axis2Name { get; init; } = "Theta";
    public long Velocity { get; init; } = 1_000_000;
    public long MaxVelocity { get; init; } = 1_000_000;
    public long Acceleration { get; init; } = 1_000_000;
    public long Deceleration { get; init; } = 100_000_000;
}

public sealed class TimingProfile
{
    public int MotionWaitMs { get; init; } = 900;
    public int ExtraIntervalMs { get; init; } = 1000;
    public int DoorWaitMs { get; init; } = 2000;
    public int CylinderWaitTimeoutMs { get; init; } = 1000;
    public int VacuumSuctionMs { get; init; } = 1200;
    public int VacuumExhaustMs { get; init; } = 1200;
    public int AutoRealTickMs { get; init; } = 1000;
    public int AutoSimTickMs { get; init; } = 3000;
}

public sealed class IoProfile
{
    public Dictionary<int, IoPoint> DigitalOutputs { get; init; } = new();
    public Dictionary<int, IoPoint> DigitalInputs { get; init; } = new();
}

public sealed record IoChannel(IoPoint Point, int Channel, string DisplayName);

public sealed class RobotPose
{
    public long ZSafe { get; init; }
    public long ZWork { get; init; }
    public long Theta { get; init; }
}

public sealed class FoupSlotPose
{
    public long ZSafe { get; init; }
    public long ZWork { get; init; }
}

public sealed class Recipe
{
    public string RecipeName { get; init; } = string.Empty;
    public IReadOnlyList<RecipeStep> Steps { get; init; } = Array.Empty<RecipeStep>();
}

public sealed class RecipeStep
{
    public string StepName { get; init; } = string.Empty;
    public int DurationSec { get; init; }
    public double Pressure { get; init; }
    public double SlurryFlow { get; init; }
}
