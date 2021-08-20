using System;
using System.Windows;

namespace SpecifiedRecordsExporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Worker _worker;

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

                _worker = new Worker(txtRootDir.Text, txtFreeText.Text);
                _worker.FileMoveProgressChanged += Worker_FileMoveProgressChanged;
                await _worker.RunAsync();

                btnGo.IsEnabled = true;
            }
        }

        private void Worker_FileMoveProgressChanged(float progress)
        {
            if (!string.IsNullOrEmpty(_worker.Error))
            {
                tbError.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => { tbError.Text = _worker.Error; }));
            }
            if (_worker.ProgressTotal > 0)
            {
                pBar.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => { pBar.Maximum = _worker.ProgressTotal; }));
            }
            pBar.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => { pBar.Value = progress; }));
        }
    }
}