using ASCOM.DeviceInterface;
using ASCOM;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace SFROofsSafetyMonitor
{
    [Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    [ProgId("ASCOM.SFROofsSafetyMonitor.SafetyMonitor")]
    [ComSourceInterfaces(typeof(ISafetyMonitor))]
    public class AlpacaSafetyMonitor : ISafetyMonitor
    {
        private string selectedRoofName;
        private string selectedRoofUrl;
        private static readonly string ConfigFile = "roofs.json";
        private bool connected = false;

        public AlpacaSafetyMonitor()
        {
            LoadSelectedRoof();
        }

        public string Name => "SFROofs Safety Monitor";

        public string Description => "ASCOM Safety Monitor for SFRO roof status files over HTTP.";

        public bool IsSafe
        {
            get
            {
                if (!connected)
                    throw new ASCOM.NotConnectedException("Safety Monitor is not connected");

                try
                {
                    var status = GetRoofStatus();
                    return status == "CLOSED";
                }
                catch
                {
                    // If can't contact, consider unsafe
                    return false;
                }
            }
        }

        public bool Connected
        {
            get => connected;
            set
            {
                if (value)
                {
                    if (string.IsNullOrEmpty(selectedRoofUrl))
                        throw new ASCOM.InvalidOperationException("No roof selected! Please configure the driver first.");
                    connected = true;
                }
                else
                {
                    connected = false;
                }
            }
        }

        public string DriverInfo => "SFROofs Safety Monitor Driver v1.0.0";
        public string DriverVersion => "1.0.0";
        public short InterfaceVersion => 1;

        public void SetupDialog()
        {
            using (var form = new SettingsForm())
            {
                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LoadSelectedRoof();
                }
            }
        }

        public void Dispose()
        {
            Connected = false;
        }

        public ArrayList SupportedActions => new ArrayList();

        public string Action(string ActionName, string ActionParameters)
        {
            throw new ASCOM.ActionNotImplementedException($"Action {ActionName} is not supported by this driver");
        }

        public void CommandBlind(string Command, bool Raw)
        {
            throw new ASCOM.MethodNotImplementedException("CommandBlind is not implemented");
        }

        public bool CommandBool(string Command, bool Raw)
        {
            throw new ASCOM.MethodNotImplementedException("CommandBool is not implemented");
        }

        public string CommandString(string Command, bool Raw)
        {
            throw new ASCOM.MethodNotImplementedException("CommandString is not implemented");
        }

        private string GetRoofStatus()
        {
            if (string.IsNullOrEmpty(selectedRoofUrl))
                throw new System.InvalidOperationException("No roof selected!");

            using (var client = new WebClient())
            {
                var res = client.DownloadString(selectedRoofUrl);
                // Expecting status as first line: "OPEN" or "CLOSED"
                var line = res.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim().ToUpperInvariant();
                return line;
            }
        }

        private void LoadSelectedRoof()
        {
            selectedRoofName = Settings.SelectedRoofName;
            var roofs = LoadRoofs();
            var roof = roofs.Find(r => r.Name == selectedRoofName);
            selectedRoofUrl = roof?.Url;
        }

        public static List<RoofConfig> LoadRoofs()
        {
            if (!File.Exists(ConfigFile))
                throw new FileNotFoundException($"Could not find {ConfigFile}!");
            var json = File.ReadAllText(ConfigFile);
            return JsonConvert.DeserializeObject<List<RoofConfig>>(json) ?? new List<RoofConfig>();
        }
    }

    public class RoofConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";
        [JsonProperty("url")]
        public string Url { get; set; } = "";
    }
}