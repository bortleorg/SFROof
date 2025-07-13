using System;
using System.IO;

namespace SFROofsSafetyMonitor
{
    public static class Settings
    {
        private static string settingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SFROofsSafetyMonitor",
            "settings.config");

        public static string SelectedRoofName
        {
            get
            {
                try
                {
                    if (File.Exists(settingsFile))
                    {
                        return File.ReadAllText(settingsFile).Trim();
                    }
                }
                catch { }
                return null;
            }
            set
            {
                try
                {
                    var dir = Path.GetDirectoryName(settingsFile);
                    if (dir != null && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    
                    if (value != null)
                        File.WriteAllText(settingsFile, value);
                    else if (File.Exists(settingsFile))
                        File.Delete(settingsFile);
                }
                catch { }
            }
        }
    }
}
