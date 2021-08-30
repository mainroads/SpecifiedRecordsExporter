using System.IO;
using System.Windows;

namespace SpecifiedRecordsExporter
{
    public partial class MainWindow : Window
    {
        private Worker worker;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (chkCopyFiles.IsChecked == true)
            {
                FolderBrowserForWPF.Dialog dlg = new FolderBrowserForWPF.Dialog();
                dlg.Title = "Browse for the Specified Records folder...";

                if (dlg.ShowDialog() == true)
                {
                    string dir = dlg.FileName;
                    if (Directory.GetParent(dir).Name == "Downloads")
                    {
                        txtRootDir.Text = dlg.FileName;
                    }
                    else
                    {
                        MessageBox.Show("Specified Records subfolder is not in your Downloads folder!", Application.Current.MainWindow.Title);
                    }
                }
            }
            else
            {
                MessageBox.Show("You have not completed Step 1 above!", Application.Current.MainWindow.Title);
            }

        }

        private async void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFreeText.Text))
            {
                MessageBox.Show("Free Text is empty!", Application.Current.MainWindow.Title);
            }
            else if (chkCopyFiles.IsChecked == true)
            {
                MessageBox.Show("You have not completed Step 1 above!", Application.Current.MainWindow.Title);
            }
            else
            {
                btnGo.IsEnabled = false;
                pBar.Value = 0;

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
            }

            if (worker.FilesCount > 0)
            {
                pBar.Maximum = worker.FilesCount;
            }

            pBar.Value = progress;
        }
    }
}