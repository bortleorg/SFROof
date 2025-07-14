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
            var roofsConfig = JsonConvert.DeserializeObject<RoofsConfiguration>(json);
            return roofsConfig?.Roofs ?? new List<RoofConfig>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roofs configuration");
            return new List<RoofConfig>();
        }
    }

    public async Task<LocationInfo?> GetLocationInfoAsync()
    {
        try
        {
            if (!File.Exists(_roofsPath))
            {
                _logger.LogWarning("roofs.json not found at {Path}", _roofsPath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_roofsPath);
            var roofsConfig = JsonConvert.DeserializeObject<RoofsConfiguration>(json);
            return roofsConfig?.Location;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading location configuration");
            return null;
        }
    }

    public async Task<SafetyMonitorSettings> GetSettingsAsync()
    {
        try
        {
            SafetyMonitorSettings settings;
            
            if (!File.Exists(_settingsPath))
            {
                // Return default settings
                settings = new SafetyMonitorSettings();
            }
            else
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                settings = JsonConvert.DeserializeObject<SafetyMonitorSettings>(json) ?? new SafetyMonitorSettings();
            }

            // Auto-populate location from roofs.json if not set
            if (settings.ObservatoryLatitude == 0.0 && settings.ObservatoryLongitude == 0.0)
            {
                var locationInfo = await GetLocationInfoAsync();
                if (locationInfo != null)
                {
                    settings.ObservatoryLatitude = locationInfo.Latitude;
                    settings.ObservatoryLongitude = locationInfo.Longitude;
                    settings.ObservatoryTimezone = locationInfo.Timezone;
                }
            }

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
