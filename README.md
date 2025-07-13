# SFROof Safety Monitor

This project provides ASCOM Safety Monitor implementations for SFROof observatories. **The Alpaca version is now the recommended solution.**

## 🚀 Alpaca Safety Monitor (RECOMMENDED)

A modern HTTP-based ASCOM Alpaca Safety Monitor that works with NINA and other ASCOM clients **without COM registration issues**.

### Features
- ✅ HTTP-based (no COM registration required)
- ✅ Works with NINA, SGP, and other ASCOM clients
- ✅ Simple configuration via JSON file
- ✅ Automatic builds and releases via GitHub Actions
- ✅ **No more 80131040 errors!**

### Installation Options

#### Option 1: Inno Setup Installer (Recommended)
1. Download `SFROofAlpacaSafetyMonitorSetup.exe` from the latest release
2. Run the installer - it's that simple!
3. No COM registration, no dependencies, no hassle

#### Option 2: Manual Installation
1. Download the release ZIP file
2. Extract to a folder of your choice
3. Run `AlpacaSafetyMonitor.exe`

### ASCOM Client Configuration
In NINA or other ASCOM clients:
1. Add an **Alpaca** device (not COM)
2. Server: `localhost`
3. Port: `11111`  
4. Device Type: `SafetyMonitor`
5. Device Number: `0`

### Configuration
Edit `roofs.json` in the installation directory:
```json
[
  {
    "name": "Roof1",
    "url": "https://devnull.sfo3.digitaloceanspaces.com/starfront/RoofStatusFile_1.txt"
  },
  {
    "name": "Roof2", 
    "url": "https://devnull.sfo3.digitaloceanspaces.com/starfront/RoofStatusFile_2.txt"
  }
]
```

The safety monitor expects the roof status URL to return:
- `OPEN` - Roof is open (unsafe)
- `CLOSED` - Roof is closed (safe)

---

## ⚠️ Legacy COM Safety Monitor (DEPRECATED)

The original COM-based implementation had persistent registration issues and is **no longer recommended**.

### Known Issues with COM Version
- ❌ COM registration errors (80131040)
- ❌ Assembly manifest conflicts  
- ❌ Complex dependency management
- ❌ WiX installer complexity

**Please use the Alpaca version instead!**

---

## Development

### Building Alpaca Version
```bash
cd AlpacaSafetyMonitor
dotnet build
dotnet run
```

### Testing Alpaca Endpoints
```bash
# Test device discovery
curl http://localhost:11111/management/apiversions

# Test safety status
curl http://localhost:11111/api/v1/safetymonitor/0/issafe

# Test device info
curl http://localhost:11111/api/v1/safetymonitor/0/description
```

### Building Installer
The project includes an **Inno Setup** installer script (`AlpacaInstaller.iss`) that creates a simple, reliable installer without COM registration complexity.

## Contributing

1. Clone the repository
2. Make changes to the `AlpacaSafetyMonitor` project  
3. Test locally with `dotnet run`
4. Create a pull request

## Architecture Decision

We switched from COM-based to HTTP-based Alpaca because:
- **Reliability**: No COM registration issues
- **Simplicity**: Much easier installation and deployment
- **Compatibility**: Works with all modern ASCOM clients
- **Maintainability**: Standard web API patterns

## License

MIT License