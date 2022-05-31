using System.Windows;

namespace SpecifiedRecordsExporter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[] ExcludedFilesList = { ".DS_Store", "TRIM.dat" };
    }
}
