# SFROofs Safety Monitor

SFROofs Safety Monitor is an ASCOM Alpaca Safety Monitor driver for Windows. It allows NINA and other Alpaca-compatible astronomy software to monitor the status of observatory roofs based on status files available over HTTP.

## Features

- Selectable roof endpoints, configured in `roofs.json`
- Reports "safe" when the selected roof's status file reports "CLOSED"
- Simple configuration UI for roof selection

## Configuration

Edit `roofs.json` to specify available roofs and their corresponding URLs. Example:

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

## Usage

1. Build and install the driver.
2. Open the driver settings to select the roof you wish to monitor.
3. Use the driver as a Safety Monitor device in NINA or any other ASCOM Alpaca client.

## Building

- .NET 6.0 or later is required.
- Restore dependencies and build with Visual Studio or using `dotnet build`.

## Installer

A sample WiX installer script and a GitHub Actions workflow are included to help build and distribute an MSI installer.