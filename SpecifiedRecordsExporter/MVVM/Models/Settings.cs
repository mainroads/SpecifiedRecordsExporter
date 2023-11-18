using ShareX.HelpersLib;

namespace SpecifiedRecordsExporter
{
    public class Settings : SettingsBase<Settings>
    {
        public string RootDir { get; set; }
        public string[] FileExtensions { get; set; } = new string[] { "pdf", "docx" };
    }


}
