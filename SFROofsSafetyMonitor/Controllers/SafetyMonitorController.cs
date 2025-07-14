using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SFROofsSafetyMonitor.Services;
using SFROofsSafetyMonitor.Models;

namespace SFROofsSafetyMonitor.Controllers;

public class AlpacaResponse<T>
{
    public uint ClientTransactionID { get; set; }
    public uint ServerTransactionID { get; set; }
    public int ErrorNumber { get; set; }
    public string ErrorMessage { get; set; } = "";
    public T? Value { get; set; }
}

public class AlpacaResponse
{
    public uint ClientTransactionID { get; set; }
    public uint ServerTransactionID { get; set; }
    public int ErrorNumber { get; set; }
    public string ErrorMessage { get; set; } = "";
}

[ApiController]
[Route("api/v1/safetymonitor/{deviceNumber:int}")]
public class SafetyMonitorController : ControllerBase
{
    private static uint _serverTransactionId = 0;
    private static readonly HttpClient _httpClient = new();
    private readonly ConfigurationService _configService;
    private readonly SolarCalculationService _solarService;
    private readonly ILogger<SafetyMonitorController> _logger;

    public SafetyMonitorController(ConfigurationService configService, SolarCalculationService solarService, ILogger<SafetyMonitorController> logger)
    {
        _configService = configService;
        _solarService = solarService;
        _logger = logger;
    }

    private AlpacaResponse<T> CreateResponse<T>(T value, uint clientTransactionId = 0)
    {
        return new AlpacaResponse<T>
        {
            ClientTransactionID = clientTransactionId,
            ServerTransactionID = ++_serverTransactionId,
            ErrorNumber = 0,
            ErrorMessage = "",
            Value = value
        };
    }

    private AlpacaResponse CreateResponse(uint clientTransactionId = 0)
    {
        return new AlpacaResponse
        {
            ClientTransactionID = clientTransactionId,
            ServerTransactionID = ++_serverTransactionId,
            ErrorNumber = 0,
            ErrorMessage = ""
        };
    }

    // Common ASCOM properties
    [HttpGet("connected")]
    public IActionResult GetConnected([FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse(true, clienttransactionid));
    }

    [HttpPut("connected")]
    public IActionResult SetConnected([FromForm] bool connected, [FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        // For Alpaca, we're always "connected" since it's HTTP-based
        return Ok(CreateResponse(clienttransactionid));
    }

    [HttpGet("description")]
    public IActionResult GetDescription([FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse("ASCOM Alpaca Safety Monitor for SFROof observatories", clienttransactionid));
    }

    [HttpGet("driverinfo")]
    public IActionResult GetDriverInfo([FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse("SFROofs Safety Monitor - Alpaca Driver v1.0.0", clienttransactionid));
    }

    [HttpGet("driverversion")]
    public IActionResult GetDriverVersion([FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse("1.0.0", clienttransactionid));
    }

    [HttpGet("interfaceversion")]
    public IActionResult GetInterfaceVersion([FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse((short)1, clienttransactionid));
    }

    [HttpGet("name")]
    public IActionResult GetName([FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse("SFROofs Safety Monitor", clienttransactionid));
    }

    [HttpGet("supportedactions")]
    public IActionResult GetSupportedActions([FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse(Array.Empty<string>(), clienttransactionid));
    }

    // Action method
    [HttpPut("action")]
    public IActionResult Action([FromForm] string action, [FromForm] string parameters, [FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse("", clienttransactionid));
    }

    // Command completed
    [HttpPut("commandblind")]
    public IActionResult CommandBlind([FromForm] string command, [FromForm] bool raw, [FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse(clienttransactionid));
    }

    [HttpPut("commandbool")]
    public IActionResult CommandBool([FromForm] string command, [FromForm] bool raw, [FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse(false, clienttransactionid));
    }

    [HttpPut("commandstring")]
    public IActionResult CommandString([FromForm] string command, [FromForm] bool raw, [FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        return Ok(CreateResponse("", clienttransactionid));
    }

    // Safety Monitor specific property
    [HttpGet("issafe")]
    public async Task<IActionResult> GetIsSafe([FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        try
        {
            var settings = await _configService.GetSettingsAsync();
            
            // Check manual override first
            if (settings.ManualOverrideEnabled)
            {
                _logger.LogInformation("Manual override is enabled, returning: {Value}", settings.ManualOverrideValue);
                return Ok(CreateResponse(settings.ManualOverrideValue, clienttransactionid));
            }
            
            // Check solar altitude lockout
            if (settings.SolarLockoutEnabled && settings.ObservatoryLatitude != 0 && settings.ObservatoryLongitude != 0)
            {
                var currentSolarAltitude = _solarService.CalculateSolarAltitude(
                    settings.ObservatoryLatitude, 
                    settings.ObservatoryLongitude, 
                    DateTime.UtcNow);
                
                if (currentSolarAltitude > settings.MaxSolarAltitude)
                {
                    _logger.LogInformation("Solar lockout active - Sun altitude {Altitude:F2}° is above limit {Limit:F2}°", 
                        currentSolarAltitude, settings.MaxSolarAltitude);
                    return Ok(CreateResponse(false, clienttransactionid)); // Not safe due to sun altitude
                }
                
                _logger.LogDebug("Solar altitude check passed - Sun altitude {Altitude:F2}° is below limit {Limit:F2}°", 
                    currentSolarAltitude, settings.MaxSolarAltitude);
            }
            
            // Get the selected roof configuration
            var selectedRoof = await _configService.GetSelectedRoofAsync();
            if (selectedRoof == null)
            {
                _logger.LogWarning("No roof selected, returning unsafe");
                return Ok(CreateResponse(false, clienttransactionid)); // No roof selected, not safe
            }

            var response = await _httpClient.GetStringAsync(selectedRoof.Url);
            var isSafe = ParseSkyAlertStatus(response);
            
            _logger.LogDebug("Roof status check: {RoofName} = {Status}", selectedRoof.Name, isSafe ? "SAFE" : "UNSAFE");

            return Ok(CreateResponse(isSafe, clienttransactionid));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking safety status");
            // If can't contact roof, consider unsafe
            return Ok(CreateResponse(false, clienttransactionid));
        }
    }

    private static bool ParseSkyAlertStatus(string response)
    {
        if (string.IsNullOrEmpty(response))
            return false;

        // SkyAlert format: "???2025-07-11 10:47:42PM Roof Status: CLOSED"
        // Look for "Roof Status:" followed by the status
        // OPEN = safe (telescope can operate), CLOSED = unsafe (telescope would hit roof)
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Look for "Roof Status:" in the line
            var roofStatusIndex = trimmedLine.IndexOf("Roof Status:", StringComparison.OrdinalIgnoreCase);
            if (roofStatusIndex >= 0)
            {
                // Extract everything after "Roof Status:"
                var statusPart = trimmedLine.Substring(roofStatusIndex + "Roof Status:".Length).Trim();
                
                // Check if it contains "OPEN" (safe for telescope operation)
                return statusPart.IndexOf("OPEN", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        // If no "Roof Status:" found, fall back to looking for "OPEN" anywhere in the response
        return response.IndexOf("OPEN", StringComparison.OrdinalIgnoreCase) >= 0;
    }
    
    // Manual override endpoints
    [HttpPost("override")]
    public async Task<IActionResult> SetManualOverride([FromBody] ManualOverrideRequest request, [FromQuery] uint clienttransactionid = 0)
    {
        try
        {
            var settings = await _configService.GetSettingsAsync();
            settings.ManualOverrideEnabled = request.Enabled;
            settings.ManualOverrideValue = request.Value;
            await _configService.SaveSettingsAsync(settings);
            
            _logger.LogInformation("Manual override updated - Enabled: {Enabled}, Value: {Value}", 
                request.Enabled, request.Value);
            
            return Ok(CreateResponse(clienttransactionid));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting manual override");
            return BadRequest($"Failed to set manual override: {ex.Message}");
        }
    }
    
    [HttpGet("override")]
    public async Task<IActionResult> GetManualOverride([FromQuery] uint clienttransactionid = 0)
    {
        try
        {
            var settings = await _configService.GetSettingsAsync();
            var result = new
            {
                Enabled = settings.ManualOverrideEnabled,
                Value = settings.ManualOverrideValue
            };
            return Ok(CreateResponse(result, clienttransactionid));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manual override status");
            return BadRequest($"Failed to get manual override: {ex.Message}");
        }
    }
    
    [HttpGet("solarstatus")]
    public async Task<IActionResult> GetSolarStatus([FromQuery] uint clienttransactionid = 0)
    {
        try
        {
            var settings = await _configService.GetSettingsAsync();
            
            if (!settings.SolarLockoutEnabled || settings.ObservatoryLatitude == 0 || settings.ObservatoryLongitude == 0)
            {
                var disabledResult = new
                {
                    Enabled = settings.SolarLockoutEnabled,
                    CurrentAltitude = (double?)null,
                    MaxAltitude = settings.MaxSolarAltitude,
                    IsLocked = false,
                    Message = "Solar lockout disabled or coordinates not set"
                };
                return Ok(CreateResponse(disabledResult, clienttransactionid));
            }
            
            var currentSolarAltitude = _solarService.CalculateSolarAltitude(
                settings.ObservatoryLatitude, 
                settings.ObservatoryLongitude, 
                DateTime.UtcNow);
            
            var isLocked = currentSolarAltitude > settings.MaxSolarAltitude;
            
            var result = new
            {
                Enabled = settings.SolarLockoutEnabled,
                CurrentAltitude = Math.Round(currentSolarAltitude, 2),
                MaxAltitude = settings.MaxSolarAltitude,
                IsLocked = isLocked,
                Message = isLocked ? "Solar lockout active - Sun too high" : "Solar lockout OK"
            };
            
            return Ok(CreateResponse(result, clienttransactionid));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting solar status");
            return BadRequest($"Failed to get solar status: {ex.Message}");
        }
    }

    [HttpGet("lockoutperiod")]
    public async Task<IActionResult> GetLockoutPeriod([FromQuery] uint clienttransactionid = 0)
    {
        try
        {
            var settings = await _configService.GetSettingsAsync();
            
            if (!settings.SolarLockoutEnabled || settings.ObservatoryLatitude == 0 || settings.ObservatoryLongitude == 0)
            {
                return Ok(CreateResponse(new
                {
                    enabled = false,
                    message = "Solar lockout is not enabled or location not configured"
                }, clienttransactionid));
            }

            var today = DateTime.Today;
            (DateTime? lockoutStart, DateTime? lockoutEnd) = _solarService.GetLockoutPeriod(
                today, settings.ObservatoryLatitude, settings.ObservatoryLongitude, settings.MaxSolarAltitude);

            // If no lockout today, check tomorrow
            if (lockoutStart == null)
            {
                var tomorrow = today.AddDays(1);
                (lockoutStart, lockoutEnd) = _solarService.GetLockoutPeriod(
                    tomorrow, settings.ObservatoryLatitude, settings.ObservatoryLongitude, settings.MaxSolarAltitude);
            }

            var result = new
            {
                enabled = true,
                threshold = settings.MaxSolarAltitude,
                lockoutStart = lockoutStart?.ToString("yyyy-MM-dd HH:mm:ss"),
                lockoutEnd = lockoutEnd?.ToString("yyyy-MM-dd HH:mm:ss"),
                hasLockout = lockoutStart != null,
                isCurrentlyInLockout = lockoutStart != null && lockoutEnd != null && 
                    DateTime.Now >= lockoutStart && DateTime.Now <= lockoutEnd,
                message = lockoutStart == null ? "No lockout period found (sun stays below threshold)" :
                    $"Lockout period: {lockoutStart:HH:mm} to {lockoutEnd:HH:mm}"
            };

            return Ok(CreateResponse(result, clienttransactionid));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lockout period");
            return Ok(CreateResponse(new
            {
                enabled = false,
                error = "Error calculating lockout period"
            }, clienttransactionid));
        }
    }
}

public class ManualOverrideRequest
{
    public bool Enabled { get; set; }
    public bool Value { get; set; }
}
