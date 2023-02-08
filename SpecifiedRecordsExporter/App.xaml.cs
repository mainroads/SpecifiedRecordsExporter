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

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        if(window != null)
        {
            window.Title = Title;
        }
        return window;
    }
}

