using System.Reflection;

namespace SpecifiedRecordsExporter;

public partial class MainPage : ContentPage
{
    private Worker worker;
    private bool previewOnce;

    string dirSpecifiedRecords = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}Downloads{Path.DirectorySeparatorChar}Specified Records";

    public MainPage()
    {
        InitializeComponent();

        btnExport.IsEnabled = false;
        Title = $"{App.Title} v{Assembly.GetExecutingAssembly().GetName().Version}";
        txtRootDir.Text = dirSpecifiedRecords;
        if (!Directory.Exists(dirSpecifiedRecords))
        {
            Directory.CreateDirectory(dirSpecifiedRecords);
        }
    }

    public static IEnumerable<string> GetFiles(string path, IEnumerable<string> excludedFiles, SearchOption searchOption = SearchOption.AllDirectories)
    {
        var filePaths = Directory.EnumerateFiles(path, "*.*", searchOption);

        foreach (var fp in filePaths)
        {
            if (excludedFiles.Contains(Path.GetFileName(fp)))
            {
                continue;
            }
            else if (excludedFiles.Contains(Path.GetExtension(fp)))
            {
                continue;
            }

            yield return fp;
        }
    }

    void btnPrepare_Clicked(System.Object sender, System.EventArgs e)
    {
        Prepare();
    }

    async void btnExport_Clicked(System.Object sender, System.EventArgs e)
    {
        if (!chkCopyFiles.IsChecked)
        {
            await DisplayAlert(App.Title, "You have not completed Step 1 above!", "OK");
        }
        else if (GetFiles(dirSpecifiedRecords, App.JunkFilesList, SearchOption.TopDirectoryOnly).Count() > 0)
        {
            await DisplayAlert(App.Title, "Files detected in the Specified Records folder. \n\nPlease remove all the files, and copy only folders!\n\n" + GetFiles(dirSpecifiedRecords, App.JunkFilesList, SearchOption.TopDirectoryOnly).First().ToString(), "OK");
        }
        else if (string.IsNullOrEmpty(txtFreeText.Text))
        {
            await DisplayAlert(App.Title, "Free Text is empty!", "OK");
        }
        else
        {
            Rename();
        }
    }

    private async void Prepare()
    {
        btnExport.IsEnabled = false;
        try
        {
            if (!string.IsNullOrEmpty(txtRootDir.Text))
            {
                btnExport.IsEnabled = false;
                // lvFiles.Items.Clear();
                worker = new Worker(txtRootDir.Text, txtFreeText.Text);
                worker.PreviewProgressChanged += Worker_PreviewProgressChanged;
                await worker.PreviewAsync();
            }
        }
        finally
        {
            btnExport.IsEnabled = true;
            SettingsManager.SaveLog(worker.DebugLog);
        }
    }

    private async void Rename()
    {
        btnPrepare.IsEnabled = false;
        pBar.Progress = 0;

        worker = new Worker(txtRootDir.Text, txtFreeText.Text);
        worker.RenameProgressChanged += Worker_FileMoveProgressChanged;
        await worker.RenameAsync();
    }

    private void Worker_PreviewProgressChanged(PrepareProgressData progress)
    {
        if (!string.IsNullOrEmpty(progress.Status))
        {
            lblStatus.Text = progress.Status;
        }

        if (progress.ProgressType == ProgressType.PreviewFileNames)
        {
            // ListViewItem lvi = new ListViewItem();
            // lvi.Foreground = progress.CurrentFilePath.Length > 260 ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.Green);
            // lvi.Content = progress.CurrentFilePath;
            // lvFiles.Items.Add(lvi);
        }

        if (progress.ProgressType == ProgressType.RemoveJunkFiles)
        {
            pBar.Progress = (double)progress.CurrentFileId / (double)worker.MaxFilesCount;
        }

        if (progress.ProgressType == ProgressType.ReadyToRename)
        {
            if (progress.HasLongFileNames)
            {
                if (!previewOnce)
                {
                    previewOnce = true;
                    Prepare();
                }
                lblStatus.Text = "Long file names were detected and shortened. Preparation complete!";
            }
            else
            {
                lblStatus.Text = "Preparation complete!";
            }
        }
    }

    private void Worker_FileMoveProgressChanged(RenameProgressData progress)
    {
        if (!string.IsNullOrEmpty(worker.Error))
        {
            lblStatus.Text = worker.Error;
        }

        pBar.Progress = (double)progress.CurrentFileId / (double)worker.MaxFilesCount;

        if (progress.CurrentFileId == worker.MaxFilesCount)
        {
            lblStatus.Text = "Rename complete!";
            btnPrepare.IsEnabled = true;
        }
    }


}


