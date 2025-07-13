namespace SFROofsSafetyMonitor.Models;

public class RoofConfig
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class SafetyMonitorSettings
{
    public string SelectedRoofName { get; set; } = string.Empty;
}
