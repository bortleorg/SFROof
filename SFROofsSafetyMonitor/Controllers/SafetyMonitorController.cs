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

    public SafetyMonitorController(ConfigurationService configService)
    {
        _configService = configService;
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
            // Get the selected roof configuration
            var selectedRoof = await _configService.GetSelectedRoofAsync();
            if (selectedRoof == null)
            {
                return Ok(CreateResponse(false, clienttransactionid)); // No roof selected, not safe
            }

            var response = await _httpClient.GetStringAsync(selectedRoof.Url);
            var isSafe = ParseSkyAlertStatus(response);

            return Ok(CreateResponse(isSafe, clienttransactionid));
        }
        catch
        {
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
}
