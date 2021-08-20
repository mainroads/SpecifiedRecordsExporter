using System.Windows;

namespace SpecifiedRecordsExporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Worker worker;

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
                pBar.Value = 0;
                btnGo.IsEnabled = false;

                worker = new Worker(txtRootDir.Text, txtFreeText.Text);
                worker.FileMoveProgressChanged += Worker_FileMoveProgressChanged;
                await worker.Run();

                btnGo.IsEnabled = true;
            }
        }

        private void Worker_FileMoveProgressChanged(float progress)
        {
            if (!string.IsNullOrEmpty(worker.Error))
            {
                tbError.Text = worker.Error;
                // tbError.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => { tbError.Text = worker.Error; }));
            }
            if (worker.ProgressTotal > 0)
            {
                pBar.Maximum = worker.ProgressTotal;
                // pBar.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => { pBar.Maximum = worker.ProgressTotal; }));
            }
            pBar.Value = progress;
            // pBar.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => { pBar.Value = progress; }));
        }
    }
}