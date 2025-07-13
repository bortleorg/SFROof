using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

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
