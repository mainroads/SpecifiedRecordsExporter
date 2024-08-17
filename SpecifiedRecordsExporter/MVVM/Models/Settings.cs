using ShareX.HelpersLib;

namespace SpecifiedRecordsExporter
{
    public class Settings : SettingsBase<Settings>
    {
        public string RootDir { get; set; }
        public string[] NonCadFileExtensions { get; set; } = new string[] { "pdf", "docx" };
        public string[] CadFileExtensions { get; set; } = new string[] { "dwg", "shp", "sor" };
    }


}
