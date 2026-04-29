using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
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
    private readonly SemaphoreSlim writeLock = new(1, 1);

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
        await writeLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(settingsPath)!;
            var tempPath = Path.Combine(directory, $"settings.json.{Guid.NewGuid():N}.tmp");
            var legacyTempPath = Path.Combine(directory, "settings.json.tmp");

            Directory.CreateDirectory(directory);

            try
            {
                await using (var stream = File.Create(tempPath))
                {
                    await JsonSerializer.SerializeAsync(stream, settings.Normalized(), Options);
                }

                File.Move(tempPath, settingsPath, overwrite: true);
                if (File.Exists(legacyTempPath))
                {
                    File.Delete(legacyTempPath);
                }
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }
        }
        finally
        {
            writeLock.Release();
        }
    }
}
