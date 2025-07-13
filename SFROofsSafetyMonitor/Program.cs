using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure as Windows Service if applicable
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson();

// Add CORS to allow connections from any ASCOM client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add UDP discovery service
builder.Services.AddHostedService<AlpacaDiscoveryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors();
app.UseRouting();
app.MapControllers();

// Alpaca device discovery endpoint
app.MapGet("/management/apiversions", () => new { Value = new[] { 1 } });
app.MapGet("/management/v1/description", () => new 
{ 
    Value = new 
    {
        ServerName = "SFROofs Safety Monitor",
        Manufacturer = "SFROof Development",
        ManufacturerVersion = "1.0.0",
        Location = "Local"
    }
});

app.MapGet("/management/v1/configureddevices", () => new 
{ 
    Value = new[] 
    {
        new 
        {
            DeviceName = "SFROof Safety Monitor",
            DeviceType = "SafetyMonitor",
            DeviceNumber = 0,
            UniqueID = "SFROof-SafetyMonitor-001"
        }
    }
});

app.Run("http://localhost:11111");

// ASCOM Alpaca Discovery Service
public class AlpacaDiscoveryService : BackgroundService
{
    private readonly ILogger<AlpacaDiscoveryService> _logger;
    private UdpClient? _udpClient;
    private const int ALPACA_DISCOVERY_PORT = 32227;
    private const int ALPACA_DEVICE_PORT = 11111;

    public AlpacaDiscoveryService(ILogger<AlpacaDiscoveryService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _udpClient = new UdpClient(ALPACA_DISCOVERY_PORT);
            _logger.LogInformation("Alpaca Discovery Service started on port {Port}", ALPACA_DISCOVERY_PORT);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    var message = Encoding.ASCII.GetString(result.Buffer);
                    
                    _logger.LogDebug("Received discovery request: {Message} from {Endpoint}", message, result.RemoteEndPoint);
                    
                    if (message.StartsWith("alpacadiscovery"))
                    {
                        // Parse the discovery version
                        var parts = message.Split(':');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int version) && version == 1)
                        {
                            // Send discovery response
                            var response = JsonConvert.SerializeObject(new
                            {
                                AlpacaPort = ALPACA_DEVICE_PORT
                            });
                            
                            var responseBytes = Encoding.ASCII.GetBytes(response);
                            await _udpClient.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                            
                            _logger.LogInformation("Sent discovery response to {Endpoint}", result.RemoteEndPoint);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error in discovery service");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Alpaca Discovery Service");
        }
    }

    public override void Dispose()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        base.Dispose();
    }
}
