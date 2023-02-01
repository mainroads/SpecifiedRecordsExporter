namespace SpecifiedRecordsExporter;

public partial class App : Application
{
	public static string Title = "Specified Records Exporter";
    public static string[] JunkFilesList = { ".DS_Store", "TRIMfiles.dat" };

    public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
	}
}

