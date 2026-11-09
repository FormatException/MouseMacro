using System.Diagnostics;
using System.IO;
using System.Text.Json;
using MouseMacro.Models;

namespace MouseMacro.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _filePath;

    public SettingsService(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    public string FilePath => _filePath;

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_filePath));
                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, SerializerOptions));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}
