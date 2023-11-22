using ShareX.HelpersLib;

namespace SpecifiedRecordsExporter
{
    public class SettingsManager
    {

        public static readonly string PersonalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Specified Records Exporter");
        public static Settings Settings { get; private set; }

        public static IEnumerable<string> GetCadFileSearchPatterns()
        {
            return Settings.CadFileExtensions.Select(ext => $"*.{ext}");
        }

        public static string SettingsFilePath
        {
            get
            {
                return Path.Combine(PersonalFolder, "Settings.json");
            }
        }

        public static string LogFilePath
        {
            get
            {
                string logsFolder = Path.Combine(PersonalFolder, "Logs");
                string filename = string.Format("SRE-Log-{0:yyyy-MM}.log", DateTime.Now);
                return Path.Combine(logsFolder, filename);
            }
        }

        public static void LoadSettings()
        {
            Settings = Settings.Load(SettingsFilePath);
        }

        public static void SaveSettings()
        {
            if (Settings != null)
            {
                Settings.Save(SettingsFilePath);
            }

        }

        public static void SaveLog()
        {
            Helpers.CreateDirectoryFromFilePath(LogFilePath);
            File.AppendAllText(LogFilePath, App.DebugLog.ToString());
        }
    }
}
