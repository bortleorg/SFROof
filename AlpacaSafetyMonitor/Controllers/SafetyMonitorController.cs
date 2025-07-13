using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AlpacaSafetyMonitor.Controllers;

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

    // Safety Monitor specific property
    [HttpGet("issafe")]
    public async Task<IActionResult> GetIsSafe([FromQuery] uint clienttransactionid = 0, [FromQuery] uint clientid = 0)
    {
        try
        {
            // Load roof configuration (simplified - just use first roof for now)
            var roofs = LoadRoofs();
            if (roofs.Count == 0)
            {
                return Ok(CreateResponse(false, clienttransactionid)); // No roofs configured, not safe
            }

            var roof = roofs[0]; // Use first roof
            var response = await _httpClient.GetStringAsync(roof.Url);
            var status = response.Split('\n')[0].Trim().ToUpperInvariant();
            var isSafe = status == "CLOSED";

            return Ok(CreateResponse(isSafe, clienttransactionid));
        }
        catch
        {
            // If can't contact roof, consider unsafe
            return Ok(CreateResponse(false, clienttransactionid));
        }
    }

    private List<RoofConfig> LoadRoofs()
    {
        const string configFile = "roofs.json";
        if (!System.IO.File.Exists(configFile))
            return new List<RoofConfig>();

        try
        {
            var json = System.IO.File.ReadAllText(configFile);
            return JsonConvert.DeserializeObject<List<RoofConfig>>(json) ?? new List<RoofConfig>();
        }
        catch
        {
            return new List<RoofConfig>();
        }
    }
}

public class RoofConfig
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";
    
    [JsonProperty("url")]
    public string Url { get; set; } = "";
}
