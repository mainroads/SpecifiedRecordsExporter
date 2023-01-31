namespace SpecifiedRecordsExporter;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();


        txtRootDir.Text = $@"C:\Users\{Environment.UserName}\Downloads\Specified Records";

    }


}


