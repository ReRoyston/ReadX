using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReadX.Models;

namespace ReadX.Services;

public sealed class JsonSettingsStore(IAppDataPathProvider pathProvider) : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string settingsPath = Path.Combine(pathProvider.ReadXDirectory, "settings.json");

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(settingsPath))
        {
            return AppSettings.CreateDefault();
        }

        try
        {
            await using var stream = File.OpenRead(settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, Options);
            return (settings ?? AppSettings.CreateDefault()).Normalized();
        }
        catch
        {
            return AppSettings.CreateDefault();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        await using var stream = File.Create(settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings.Normalized(), Options);
    }
}
