namespace SFROofsSafetyMonitor.Models;

public class RoofConfig
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class LocationInfo
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Timezone { get; set; } = string.Empty;
}

public class RoofsConfiguration
{
    public string Version { get; set; } = string.Empty;
    public LocationInfo Location { get; set; } = new();
    public List<RoofConfig> Roofs { get; set; } = new();
}

public class SafetyMonitorSettings
{
    public string SelectedRoofName { get; set; } = string.Empty;
    public bool ManualOverrideEnabled { get; set; } = false;
    public bool ManualOverrideValue { get; set; } = false; // true = safe, false = unsafe
    public bool SolarLockoutEnabled { get; set; } = true;
    public double MaxSolarAltitude { get; set; } = 0.0; // Maximum safe solar altitude - observatory is UNSAFE when sun is above this threshold
    public double ObservatoryLatitude { get; set; } = 0.0; // degrees
    public double ObservatoryLongitude { get; set; } = 0.0; // degrees
    public string ObservatoryTimezone { get; set; } = string.Empty;
}
