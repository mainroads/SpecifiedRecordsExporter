namespace SpecifiedRecordsExporter;

public partial class App : Application
{
    public static string Title = "Specified Records Exporter";
    public static string[] JunkFilesList = { ".DS_Store", "TRIMfiles.dat" };

    public App()
    {
        InitializeComponent();
        SettingsManager.LoadSettings();
        MainPage = new AppShell();
    }

    protected override Window
        CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        if (window != null)
        {
            window.Title = Title;
        }
        window.Destroying += OnWindowDestroying;
        return window;
    }

    private void OnWindowDestroying(object sender, EventArgs e)
    {
        SettingsManager.SaveSettings();
    }
}

