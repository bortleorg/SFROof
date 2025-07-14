using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SFROofsSafetyMonitor.Services;
using SFROofsSafetyMonitor.Models;

namespace SFROofsSafetyMonitor.Controllers;

[ApiController]
[Route("setup/v1/safetymonitor/{deviceNumber:int}")]
public class SetupController : ControllerBase
{
    private readonly ConfigurationService _configService;

    public SetupController(ConfigurationService configService)
    {
        _configService = configService;
    }

    [HttpGet("setup")]
    public async Task<IActionResult> GetSetupPage()
    {
        var roofs = await _configService.GetAvailableRoofsAsync();
        var settings = await _configService.GetSettingsAsync();
        var selectedRoof = await _configService.GetSelectedRoofAsync();
        var locationInfo = await _configService.GetLocationInfoAsync();

        var roofOptions = string.Join("", roofs.Select(r => 
            $"<option value='{r.Name}' {(r.Name == settings.SelectedRoofName ? "selected" : "")}>{r.Name}</option>"));

        var selectedRoofInfo = selectedRoof != null 
            ? $"<p><strong>Selected Roof:</strong> {selectedRoof.Name}</p><p><strong>URL:</strong> <a href='{selectedRoof.Url}' target='_blank'>{selectedRoof.Url}</a></p>"
            : "<p><strong>No roof selected!</strong> Please select a roof below.</p>";

        var locationSection = locationInfo != null 
            ? $@"
        <div class='config-section'>
            <h2>Observatory Location</h2>
            <p><strong>Latitude:</strong> {locationInfo.Latitude:F6}°</p>
            <p><strong>Longitude:</strong> {locationInfo.Longitude:F6}°</p>
            <p><strong>Timezone:</strong> {locationInfo.Timezone}</p>
            </div>"
            : "<div class='config-section'><p><strong>Warning:</strong> No location information found in roofs.json</p></div>";

        var manualOverrideSection = $@"
        <div class='roof-selection'>
            <h2>Manual Override</h2>
            <p>Manual override allows you to force the safety monitor to return a specific value, bypassing all other checks.</p>
            
            <div id='override-status' style='margin-top: 10px; padding: 10px; border-radius: 5px;'>
                <span id='overrideStatusIndicator' class='status-indicator'>Loading...</span>
            </div>
            
            <form id='overrideForm'>
                <label>
                    <input type='checkbox' id='overrideEnabled' {(settings.ManualOverrideEnabled ? "checked" : "")}> 
                    Enable Manual Override
                </label><br><br>
                
                <div id='overrideControls' style='display: {(settings.ManualOverrideEnabled ? "block" : "none")}'>
                    <label>Override Value:</label><br>
                    <label>
                        <input type='radio' name='overrideValue' value='true' {(settings.ManualOverrideValue ? "checked" : "")}> 
                        Force SAFE
                    </label><br>
                    <label>
                        <input type='radio' name='overrideValue' value='false' {(!settings.ManualOverrideValue ? "checked" : "")}> 
                        Force UNSAFE
                    </label><br><br>
                </div>
                
                <button type='submit'>Save Override Settings</button>
            </form>
            
            <div id='overrideMessage'></div>
        </div>

        <div class='roof-selection'>
            <h2>Solar Altitude Lockout</h2>
            <p>Prevents the system from reporting SAFE when the sun is above a specified altitude (to protect during daylight operations).</p>
            
            <div id='solar-status' style='margin-top: 10px; padding: 10px; border-radius: 5px;'>
                <span id='solarStatusIndicator' class='status-indicator'>Loading...</span><br />
                <span id='currentSolarInfo'>Loading...</span>
            </div>
            
            <form id='solarForm'>
                <label>
                    <input type='checkbox' id='solarEnabled' {(settings.SolarLockoutEnabled ? "checked" : "")}> 
                    Enable Solar Lockout
                </label><br><br>
                
                <label for='maxAltitude'>Maximum Safe Solar Altitude (degrees):</label><br>
                <input type='number' id='maxAltitude' step='0.1' value='{settings.MaxSolarAltitude}' style='width: 100px;'><br>
                <small>Observatory is UNSAFE when sun is above this altitude. Negative values = below horizon (e.g., -5 = 5 degrees below horizon)</small><br><br>
                
                <button type='submit'>Save Solar Settings</button>
            </form>
            
            <div id='solarMessage'></div>
        </div>";

        var html = $@"<!DOCTYPE html>
<html>
<head>
    <title>SFRO Roof Safety Monitor Setup</title>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; background-color: #f5f5f5; }}
        .container {{ max-width: 800px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px; }}
        h2 {{ color: #34495e; margin-top: 30px; }}
        .status {{ padding: 15px; margin: 15px 0; border-radius: 5px; font-weight: bold; text-align: center; }}
        .safe {{ background-color: #d4edda; color: #155724; border: 1px solid #c3e6cb; }}
        .unsafe {{ background-color: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }}
        .unknown {{ background-color: #fff3cd; color: #856404; border: 1px solid #ffeaa7; }}
        #solar-status {{ text-align: left; font-weight: normal; background-color: #e8f4fd; color: #0c5460; border: 1px solid #bee5eb; }}
        .config-section {{ background-color: #f8f9fa; padding: 20px; margin: 20px 0; border-left: 4px solid #007bff; }}
        pre {{ background-color: #f4f4f4; padding: 15px; border-radius: 5px; overflow-x: auto; }}
        code {{ background-color: #f4f4f4; padding: 2px 4px; border-radius: 3px; }}
        .roof-selection {{ background-color: #e8f4fd; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        select, button {{ padding: 10px; margin: 5px; border: 1px solid #ddd; border-radius: 4px; }}
        button {{ background-color: #007bff; color: white; cursor: pointer; }}
        button:hover {{ background-color: #0056b3; }}
        .success {{ color: #28a745; font-weight: bold; }}
        .error {{ color: #dc3545; font-weight: bold; }}
        .status-indicator {{ padding: 4px 8px; border-radius: 3px; font-weight: bold; }}
        .status-safe {{ background-color: #d4edda; color: #155724; }}
        .status-unsafe {{ background-color: #f8d7da; color: #721c24; }}
        .status-unknown {{ background-color: #fff3cd; color: #856404; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>SFRO Roof Safety Monitor Setup</h1>
        
        <div id='current-status' class='status unknown'>
            Current Status: Loading...
        </div>

        <div class='roof-selection'>
            <h2>Roof Selection</h2>
            {selectedRoofInfo}
            
            <div id='roof-status' style='margin-top: 10px; padding: 10px; border-radius: 5px;'>
                <span id='roofStatusDetails'>Loading...</span>
            </div>
            
            <form id='roofForm'>
                <label for='roofSelect'>Select Roof to Monitor:</label><br>
                <select id='roofSelect' name='roofName'>
                    <option value=''>-- Select a Roof --</option>
                    {roofOptions}
                </select><br><br>
                <button type='submit'>Save Selection</button>
            </form>
            
            <div id='saveMessage'></div>
        </div>

        {manualOverrideSection}

        {locationSection}

        <script>
        // Refresh status function
        async function refreshStatus() {{
            try {{
                const response = await fetch('/api/v1/safetymonitor/0/issafe');
                const data = await response.json();
                const isSafe = data.value; // lowercase 'value' to match API response
                const statusDiv = document.getElementById('current-status');
                statusDiv.className = 'status ' + (isSafe ? 'safe' : 'unsafe');
                statusDiv.innerHTML = 'Current Status: ' + (isSafe ? 'SAFE' : 'UNSAFE');
            }} catch (error) {{
                const statusDiv = document.getElementById('current-status');
                statusDiv.className = 'status unknown';
                statusDiv.innerHTML = 'Current Status: Unknown (Error checking roof status)';
            }}
        }}

        // Initial status check and periodic updates
        refreshStatus();
        setInterval(refreshStatus, 10000); // Update every 10 seconds

        // Update roof status details
        async function updateRoofStatus() {{
            try {{
                const response = await fetch('/api/v1/safetymonitor/0/roofstatus');
                const data = await response.json();
                const roof = data.value;
                
                // Update roof status background based on roof state
                const roofDiv = document.getElementById('roof-status');
                
                // Check if the roof message indicates it's open and safe
                const isRoofSafe = roof.message && (roof.message.includes('OPEN (SAFE)') || roof.message.includes('OPEN') && roof.message.includes('SAFE'));
                
                if (isRoofSafe) {{
                    roofDiv.style.backgroundColor = '#d4edda';
                }} else {{
                    roofDiv.style.backgroundColor = '#f8d7da';
                }}
                
                let statusText = `${{roof.message}}`;
                if (roof.lastUpdateTime) {{
                    const updateTime = new Date(roof.lastUpdateTime).toLocaleString();
                    statusText += ` <br /> Last Update: ${{updateTime}}`;
                }}
                
                document.getElementById('roofStatusDetails').innerHTML = statusText;
            }} catch (error) {{
                const roofDiv = document.getElementById('roof-status');
                roofDiv.style.backgroundColor = '#fff3cd';
                document.getElementById('roofStatusDetails').innerHTML = 'Error loading roof status details';
            }}
        }}

        updateRoofStatus();
        setInterval(updateRoofStatus, 10000); // Update every 10 seconds

        // Update solar status
        async function updateSolarStatus() {{
            try {{
                // Get current solar status
                const response = await fetch('/api/v1/safetymonitor/0/solarstatus');
                const data = await response.json();
                const solar = data.value;
                
                // Update solar status indicator
                const solarIndicator = document.getElementById('solarStatusIndicator');
                const solarDiv = document.getElementById('solar-status');
                if (solar.isLocked) {{
                    solarIndicator.textContent = 'UNSAFE (Locked)';
                    solarIndicator.className = 'status-indicator status-unsafe';
                    solarDiv.style.backgroundColor = '#f8d7da';
                }} else if (!solar.enabled) {{
                    solarIndicator.textContent = 'Disabled';
                    solarIndicator.className = 'status-indicator status-safe';
                    solarDiv.style.backgroundColor = '#d4edda';
                }} else {{
                    solarIndicator.textContent = 'SAFE (Not Locked)';
                    solarIndicator.className = 'status-indicator status-safe';
                    solarDiv.style.backgroundColor = '#d4edda';
                }}
                
                let statusText = '';
                
                // Always show current sun angle if available
                if (solar.currentAltitude !== null) {{
                    statusText = `Current Sun Angle: ${{solar.currentAltitude.toFixed(1)}}°`;
                }} else {{
                    statusText = 'Current Sun Angle: Unknown';
                }}
                
                // Get lockout period information
                try {{
                    const lockoutResponse = await fetch('/api/v1/safetymonitor/0/lockoutperiod');
                    const lockoutData = await lockoutResponse.json();
                    const lockout = lockoutData.value;
                    
                    if (lockout.enabled) {{
                        if (lockout.hasLockout) {{
                            if (lockout.isCurrentlyInLockout) {{
                                // Currently locked
                                const endTime = new Date(lockout.lockoutEnd).toLocaleTimeString([], {{hour: '2-digit', minute:'2-digit'}});
                                statusText += `<br>Currently Solar Locked: Yes<br>Solar unlock time: ${{endTime}}`;
                            }} else {{
                                // Not currently locked, show next lock time
                                const startTime = new Date(lockout.lockoutStart).toLocaleTimeString([], {{hour: '2-digit', minute:'2-digit'}});
                                statusText += `<br>Currently Solar Locked: No<br>Solar lock time: ${{startTime}}`;
                            }}
                        }} else {{
                            // Solar lockout enabled but no lockout period (sun stays below threshold)
                            statusText += `<br>Currently Locked: No<br>(Sun stays below ${{lockout.threshold}}° today)`;
                        }}
                    }}
                    // If solar lockout is disabled, we don't show the lockout status lines
                }} catch (lockoutError) {{
                    console.log('Could not fetch lockout period:', lockoutError);
                }}
                
                document.getElementById('currentSolarInfo').innerHTML = statusText;
            }} catch (error) {{
                const solarIndicator = document.getElementById('solarStatusIndicator');
                const solarDiv = document.getElementById('solar-status');
                solarIndicator.textContent = 'Unknown';
                solarIndicator.className = 'status-indicator status-unknown';
                solarDiv.style.backgroundColor = '#fff3cd';
                document.getElementById('currentSolarInfo').textContent = 'Error loading solar status';
            }}
        }}

        updateSolarStatus();
        setInterval(updateSolarStatus, 30000); // Update every 30 seconds

        // Update manual override status
        function updateOverrideStatus() {{
            const enabled = document.getElementById('overrideEnabled').checked;
            const overrideIndicator = document.getElementById('overrideStatusIndicator');
            const overrideDiv = document.getElementById('override-status');
            
            if (!enabled) {{
                overrideIndicator.textContent = 'SAFE (Disabled)';
                overrideIndicator.className = 'status-indicator status-safe';
                overrideDiv.style.backgroundColor = '#d4edda';
            }} else {{
                const valueRadios = document.getElementsByName('overrideValue');
                let value = false;
                
                for (const radio of valueRadios) {{
                    if (radio.checked) {{
                        value = radio.value === 'true';
                        break;
                    }}
                }}
                
                if (value) {{
                    overrideIndicator.textContent = 'SAFE (Forced)';
                    overrideIndicator.className = 'status-indicator status-safe';
                    overrideDiv.style.backgroundColor = '#d4edda';
                }} else {{
                    overrideIndicator.textContent = 'UNSAFE (Forced)';
                    overrideIndicator.className = 'status-indicator status-unsafe';
                    overrideDiv.style.backgroundColor = '#f8d7da';
                }}
            }}
        }}

        // Initial override status check
        updateOverrideStatus();

        // Manual override form handler
        document.getElementById('overrideEnabled').addEventListener('change', function() {{
            const controls = document.getElementById('overrideControls');
            controls.style.display = this.checked ? 'block' : 'none';
            updateOverrideStatus();
        }});

        // Add event listeners to radio buttons to update status
        document.getElementsByName('overrideValue').forEach(radio => {{
            radio.addEventListener('change', updateOverrideStatus);
        }});

        document.getElementById('overrideForm').addEventListener('submit', async function(e) {{
            e.preventDefault();
            
            const enabled = document.getElementById('overrideEnabled').checked;
            const valueRadios = document.getElementsByName('overrideValue');
            let value = false;
            
            for (const radio of valueRadios) {{
                if (radio.checked) {{
                    value = radio.value === 'true';
                    break;
                }}
            }}

            try {{
                const response = await fetch('/api/v1/safetymonitor/0/override', {{
                    method: 'POST',
                    headers: {{
                        'Content-Type': 'application/json',
                    }},
                    body: JSON.stringify({{ 
                        enabled: enabled,
                        value: value
                    }})
                }});

                if (response.ok) {{
                    document.getElementById('overrideMessage').innerHTML = '<p class=""success"">Override settings saved successfully!</p>';
                    setTimeout(() => {{
                        refreshStatus();
                        updateOverrideStatus();
                    }}, 1000);
                }} else {{
                    document.getElementById('overrideMessage').innerHTML = '<p class=""error"">Failed to save override settings.</p>';
                }}
            }} catch (error) {{
                document.getElementById('overrideMessage').innerHTML = '<p class=""error"">Error saving override settings: ' + error.message + '</p>';
            }}
        }});

        // Solar form handler
        document.getElementById('solarForm').addEventListener('submit', async function(e) {{
            e.preventDefault();
            
            const solarEnabled = document.getElementById('solarEnabled').checked;
            const maxAltitude = parseFloat(document.getElementById('maxAltitude').value);

            try {{
                // Get current settings first
                const currentResponse = await fetch('/setup/v1/safetymonitor/0/settings');
                let currentSettings = {{}};
                if (currentResponse.ok) {{
                    currentSettings = await currentResponse.json();
                }}

                const updatedSettings = {{
                    ...currentSettings,
                    solarLockoutEnabled: solarEnabled,
                    maxSolarAltitude: maxAltitude
                }};

                const response = await fetch('/setup/v1/safetymonitor/0/settings', {{
                    method: 'POST',
                    headers: {{
                        'Content-Type': 'application/json',
                    }},
                    body: JSON.stringify(updatedSettings)
                }});

                if (response.ok) {{
                    document.getElementById('solarMessage').innerHTML = '<p class=""success"">Solar settings saved successfully!</p>';
                    setTimeout(() => updateSolarStatus(), 1000); // Refresh solar status
                }} else {{
                    document.getElementById('solarMessage').innerHTML = '<p class=""error"">Failed to save solar settings.</p>';
                }}
            }} catch (error) {{
                document.getElementById('solarMessage').innerHTML = '<p class=""error"">Error saving solar settings: ' + error.message + '</p>';
            }}
        }});

        // Handle roof selection form
        document.getElementById('roofForm').addEventListener('submit', async function(e) {{
            e.preventDefault();
            const formData = new FormData(e.target);
            const roofName = formData.get('roofName');
            
            if (!roofName) {{
                document.getElementById('saveMessage').innerHTML = '<p class=""error"">Please select a roof.</p>';
                return;
            }}

            try {{
                const response = await fetch('/setup/v1/safetymonitor/0/selectroof', {{
                    method: 'POST',
                    headers: {{
                        'Content-Type': 'application/json',
                    }},
                    body: JSON.stringify({{ roofName: roofName }})
                }});

                if (response.ok) {{
                    document.getElementById('saveMessage').innerHTML = '<p class=""success"">Roof selection saved successfully!</p>';
                    setTimeout(() => location.reload(), 1500); // Reload to show updated selection
                }} else {{
                    document.getElementById('saveMessage').innerHTML = '<p class=""error"">Failed to save roof selection.</p>';
                }}
            }} catch (error) {{
                document.getElementById('saveMessage').innerHTML = '<p class=""error"">Error saving roof selection: ' + error.message + '</p>';
            }}
        }});
        </script>
    </div>
</body>
</html>";

        return Content(html, "text/html");
    }

    [HttpPost("selectroof")]
    public async Task<IActionResult> SelectRoof([FromBody] RoofSelectionRequest request)
    {
        try
        {
            await _configService.SaveSelectedRoofAsync(request.RoofName);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to save roof selection: {ex.Message}");
        }
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var settings = await _configService.GetSettingsAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to get settings: {ex.Message}");
        }
    }

    [HttpPost("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] SettingsUpdateRequest request)
    {
        try
        {
            var settings = await _configService.GetSettingsAsync();
            
            // Update the settings from the request
            if (request.SolarLockoutEnabled.HasValue)
                settings.SolarLockoutEnabled = request.SolarLockoutEnabled.Value;
            if (request.MaxSolarAltitude.HasValue)
                settings.MaxSolarAltitude = request.MaxSolarAltitude.Value;
            if (request.ObservatoryLatitude.HasValue)
                settings.ObservatoryLatitude = request.ObservatoryLatitude.Value;
            if (request.ObservatoryLongitude.HasValue)
                settings.ObservatoryLongitude = request.ObservatoryLongitude.Value;
            
            await _configService.SaveSettingsAsync(settings);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to save settings: {ex.Message}");
        }
    }
}

public class RoofSelectionRequest
{
    public string RoofName { get; set; } = string.Empty;
}

public class SettingsUpdateRequest
{
    public bool? SolarLockoutEnabled { get; set; }
    public double? MaxSolarAltitude { get; set; }
    public double? ObservatoryLatitude { get; set; }
    public double? ObservatoryLongitude { get; set; }
}
