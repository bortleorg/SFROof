using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SFROofsSafetyMonitor.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure as Windows Service if applicable
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson();

// Add configuration service
builder.Services.AddScoped<ConfigurationService>();

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

app.Run("http://0.0.0.0:11111");

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
            _udpClient.EnableBroadcast = true;
            
            // Bind to all network interfaces
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            _logger.LogInformation("Alpaca Discovery Service started on port {Port}", ALPACA_DISCOVERY_PORT);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    var message = Encoding.UTF8.GetString(result.Buffer);
                    
                    _logger.LogInformation("Received discovery request: '{Message}' from {Endpoint}", message, result.RemoteEndPoint);
                    
                    // Handle both "alpacadiscovery1" and "alpacadiscovery:1" formats
                    if (message.Equals("alpacadiscovery1", StringComparison.OrdinalIgnoreCase) || 
                        message.StartsWith("alpacadiscovery:1", StringComparison.OrdinalIgnoreCase))
                    {
                        // Get local IP address
                        var localIp = GetLocalIPAddress();
                        
                        // Send discovery response - ASCOM Alpaca format
                        var response = JsonConvert.SerializeObject(new
                        {
                            AlpacaPort = ALPACA_DEVICE_PORT
                        });
                        
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        
                        // Send response back to requester
                        await _udpClient.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                        
                        _logger.LogInformation("Sent discovery response to {Endpoint}: {Response} (Local IP: {LocalIP})", 
                            result.RemoteEndPoint, response, localIp);
                    }
                    else
                    {
                        _logger.LogDebug("Ignored non-Alpaca discovery message: '{Message}'", message);
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
            _logger.LogError(ex, "Failed to start Alpaca Discovery Service on port {Port}. Make sure no other service is using this port.", ALPACA_DISCOVERY_PORT);
        }
    }

    private string GetLocalIPAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint?.Address.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    public override void Dispose()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        base.Dispose();
    }
}
