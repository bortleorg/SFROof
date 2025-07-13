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

        var roofOptions = string.Join("", roofs.Select(r => 
            $"<option value='{r.Name}' {(r.Name == settings.SelectedRoofName ? "selected" : "")}>{r.Name}</option>"));

        var selectedRoofInfo = selectedRoof != null 
            ? $"<p><strong>Selected Roof:</strong> {selectedRoof.Name}</p><p><strong>URL:</strong> {selectedRoof.Url}</p>"
            : "<p><strong>No roof selected!</strong> Please select a roof below.</p>";

        var html = $@"<!DOCTYPE html>
<html>
<head>
    <title>SFR Roof Safety Monitor Setup</title>
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
        .config-section {{ background-color: #f8f9fa; padding: 20px; margin: 20px 0; border-left: 4px solid #007bff; }}
        pre {{ background-color: #f4f4f4; padding: 15px; border-radius: 5px; overflow-x: auto; }}
        code {{ background-color: #f4f4f4; padding: 2px 4px; border-radius: 3px; }}
        .roof-selection {{ background-color: #e8f4fd; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        select, button {{ padding: 10px; margin: 5px; border: 1px solid #ddd; border-radius: 4px; }}
        button {{ background-color: #007bff; color: white; cursor: pointer; }}
        button:hover {{ background-color: #0056b3; }}
        .success {{ color: #28a745; font-weight: bold; }}
        .error {{ color: #dc3545; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>SFR Roof Safety Monitor Setup</h1>
        
        <div id='current-status' class='status unknown'>
            Current Status: Loading...
        </div>

        <div class='roof-selection'>
            <h2>Roof Selection</h2>
            {selectedRoofInfo}
            
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

        <h2>Device Information</h2>
        <p><strong>Device Type:</strong> Safety Monitor</p>
        <p><strong>Interface Version:</strong> v1</p>
        <p><strong>Driver Version:</strong> 1.0</p>
        <p><strong>Description:</strong> ASCOM Alpaca Safety Monitor for SFR Observatory Roof System</p>

        <h2>Configuration</h2>
        <div class='config-section'>
            <p>This safety monitor monitors the roof status of SFR Observatory through HTTP requests.</p>
            <p>The device will report:</p>
            <ul>
                <li><strong>SAFE</strong> when the roof is open (telescope can operate safely)</li>
                <li><strong>UNSAFE</strong> when the roof is closed or status cannot be determined</li>
            </ul>
        </div>

        <h2>Network Settings</h2>
        <p><strong>Server Address:</strong> <span id='server-address'>Loading...</span></p>
        <p><strong>Port:</strong> 11111</p>
        <p><strong>Base URL:</strong> <span id='base-url'>Loading...</span></p>

        <h2>Available Roofs Configuration</h2>
        <div class='config-section'>
            <p>To add or modify available roofs, edit the <code>roofs.json</code> file in the application directory:</p>
            <pre>{{
  ""roofs"": [
    {{
      ""name"": ""Your Roof Name"",
      ""url"": ""http://your-roof-server/api/roof/status""
    }}
  ]
}}</pre>
        </div>

        <script>
        // Update server info
        const protocol = window.location.protocol;
        const hostname = window.location.hostname;
        const port = window.location.port || (protocol === 'https:' ? '443' : '80');
        document.getElementById('server-address').textContent = hostname + ':' + port;
        document.getElementById('base-url').textContent = protocol + '//' + hostname + ':' + port;

        // Refresh status function
        async function refreshStatus() {{
            try {{
                const response = await fetch('/api/v1/safetymonitor/0/issafe');
                const data = await response.json();
                const isSafe = data.Value;
                const statusDiv = document.getElementById('current-status');
                statusDiv.className = 'status ' + (isSafe ? 'safe' : 'unsafe');
                statusDiv.innerHTML = 'Current Status: ' + (isSafe ? 'SAFE (Roof Open - Telescope can operate)' : 'UNSAFE (Roof Closed or Unknown)');
            }} catch (error) {{
                const statusDiv = document.getElementById('current-status');
                statusDiv.className = 'status unknown';
                statusDiv.innerHTML = 'Current Status: Unknown (Error checking roof status)';
            }}
        }}

        // Initial status check and periodic updates
        refreshStatus();
        setInterval(refreshStatus, 10000); // Update every 10 seconds

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
}

public class RoofSelectionRequest
{
    public string RoofName { get; set; } = string.Empty;
}
