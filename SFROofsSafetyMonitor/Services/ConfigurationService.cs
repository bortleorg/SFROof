using SFROofsSafetyMonitor.Models;
using Newtonsoft.Json;

namespace SFROofsSafetyMonitor.Services;

public class ConfigurationService
{
    private readonly string _roofsPath;
    private readonly string _settingsPath;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        _roofsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "roofs.json");
        _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    }

    public async Task<List<RoofConfig>> GetAvailableRoofsAsync()
    {
        try
        {
            if (!File.Exists(_roofsPath))
            {
                _logger.LogWarning("roofs.json not found at {Path}", _roofsPath);
                return new List<RoofConfig>();
            }

            var json = await File.ReadAllTextAsync(_roofsPath);
            var roofs = JsonConvert.DeserializeObject<List<RoofConfig>>(json) ?? new List<RoofConfig>();
            return roofs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roofs configuration");
            return new List<RoofConfig>();
        }
    }

    public async Task<SafetyMonitorSettings> GetSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                // Return default settings
                return new SafetyMonitorSettings();
            }

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonConvert.DeserializeObject<SafetyMonitorSettings>(json) ?? new SafetyMonitorSettings();
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
            return new SafetyMonitorSettings();
        }
    }

    public async Task SaveSettingsAsync(SafetyMonitorSettings settings)
    {
        try
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(_settingsPath, json);
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            throw;
        }
    }

    public async Task<RoofConfig?> GetSelectedRoofAsync()
    {
        var settings = await GetSettingsAsync();
        if (string.IsNullOrEmpty(settings.SelectedRoofName))
            return null;

        var roofs = await GetAvailableRoofsAsync();
        return roofs.FirstOrDefault(r => r.Name == settings.SelectedRoofName);
    }

    public async Task SaveSelectedRoofAsync(string roofName)
    {
        var settings = await GetSettingsAsync();
        settings.SelectedRoofName = roofName;
        await SaveSettingsAsync(settings);
        _logger.LogInformation("Selected roof updated to: {RoofName}", roofName);
    }
}
