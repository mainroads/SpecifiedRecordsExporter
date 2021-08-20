using System.Windows;

namespace SpecifiedRecordsExporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Worker worker;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserForWPF.Dialog dlg = new FolderBrowserForWPF.Dialog();
            dlg.Title = "Browse for the Specified Records folder...";

            if (dlg.ShowDialog() == true)
            {
                txtRootDir.Text = dlg.FileName;
            }
        }

        private async void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFreeText.Text))
            {
                MessageBox.Show("Free Text is empty!");
            }
            else
            {
                btnGo.IsEnabled = false;

                worker = new Worker(txtRootDir.Text, txtFreeText.Text);
                pBar.Maximum = worker.ProgressTotal;

                worker.FileMoveProgressChanged += Worker_FileMoveProgressChanged;
                await worker.RunAsync();

                btnGo.IsEnabled = true;
            }
        }

        private void Worker_FileMoveProgressChanged(float progress)
        {
            pBar.Value = worker.ProgressCurrent;
        }
    }
}