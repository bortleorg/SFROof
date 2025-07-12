using ASCOM.Alpaca.SafetyMonitor;
using ASCOM.DeviceInterface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SFROofsSafetyMonitor
{
    public class AlpacaSafetyMonitor : SafetyMonitorBase
    {
        private string selectedRoofName;
        private string selectedRoofUrl;
        private static readonly string ConfigFile = "roofs.json";

        public AlpacaSafetyMonitor() : base()
        {
            LoadSelectedRoof();
        }

        public override string Name => $"SFROofs Safety Monitor";

        public override string Description => $"ASCOM Alpaca Safety Monitor for SFRO roof status files over HTTP.";

        public override bool IsSafe
        {
            get
            {
                try
                {
                    var status = GetRoofStatus().GetAwaiter().GetResult();
                    return status == "CLOSED";
                }
                catch
                {
                    // If can't contact, consider unsafe
                    return false;
                }
            }
        }

        private async Task<string> GetRoofStatus()
        {
            if (string.IsNullOrEmpty(selectedRoofUrl))
                throw new InvalidOperationException("No roof selected!");

            using var client = new HttpClient();
            var res = await client.GetStringAsync(selectedRoofUrl);
            // Expecting status as first line: "OPEN" or "CLOSED"
            var line = res.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim().ToUpperInvariant();
            return line;
        }

        // Settings dialog launches here
        public override void Action(string ActionName, string ActionParameters)
        {
            if (ActionName == "Configure")
            {
                using var form = new SettingsForm();
                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LoadSelectedRoof();
                }
            }
        }

        private void LoadSelectedRoof()
        {
            selectedRoofName = Properties.Settings.Default.SelectedRoofName;
            var roofs = LoadRoofs();
            var roof = roofs.Find(r => r.Name == selectedRoofName);
            selectedRoofUrl = roof?.Url;
        }

        public static List<RoofConfig> LoadRoofs()
        {
            if (!File.Exists(ConfigFile))
                throw new FileNotFoundException($"Could not find {ConfigFile}!");
            var json = File.ReadAllText(ConfigFile);
            return JsonConvert.DeserializeObject<List<RoofConfig>>(json);
        }

        public override string DriverInfo => $"SFROofs Alpaca Safety Monitor {Name}";
        public override string DriverVersion => "1.0.0";
    }

    public class RoofConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}