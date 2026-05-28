using System.Text.Json;
using System.Text.Json.Serialization;
using SemiTool.Domain;

namespace SemiTool.Infrastructure;

public sealed class EquipmentProfileLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<EquipmentProfile> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var profile = await JsonSerializer.DeserializeAsync<EquipmentProfile>(stream, Options, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            throw new InvalidDataException($"Equipment profile '{path}' could not be deserialized.");
        }

        Validate(profile);
        return profile;
    }

    public EquipmentProfile Load(string path)
    {
        var json = File.ReadAllText(path);
        var profile = JsonSerializer.Deserialize<EquipmentProfile>(json, Options);
        if (profile is null)
        {
            throw new InvalidDataException($"Equipment profile '{path}' could not be deserialized.");
        }

        Validate(profile);
        return profile;
    }

    public static JsonSerializerOptions SerializerOptions => Options;

    private static void Validate(EquipmentProfile profile)
    {
        if (profile.Io.DigitalOutputs.Count == 0)
        {
            throw new InvalidDataException("Equipment profile has no digital output map.");
        }

        if (profile.Io.DigitalInputs.Count == 0)
        {
            throw new InvalidDataException("Equipment profile has no digital input map.");
        }

        _ = profile.GetPose("Home");
        _ = profile.GetPose("FoupA");
        _ = profile.GetPose("FoupB");
        _ = profile.GetChamberPose(ChamberId.A);
        _ = profile.GetChamberPose(ChamberId.B);
        _ = profile.GetChamberPose(ChamberId.C);
        _ = profile.GetFoupSlotPose(1);
        _ = profile.GetFoupSlotPose(5);
    }
}
