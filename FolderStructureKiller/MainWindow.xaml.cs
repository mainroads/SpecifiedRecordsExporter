using ShareX.HelpersLib;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpecifiedRecordsExporter
{
    public partial class MainWindow : Window
    {
        private Worker worker;

        public MainWindow()
        {
            InitializeComponent();
            SettingsManager.LoadSettings();
            Title = $"{Title} v{Assembly.GetExecutingAssembly().GetName().Version}";
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SettingsManager.Settings.TRIMdatabaseID))
            {
                TRIMHelper trim = new TRIMHelper(SettingsManager.Settings.TRIMdatabaseID, SettingsManager.Settings.TRIMdatabaseWorkgroupServerName);
                MessageBox.Show(trim.Success.ToString());
            }

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
                        btnPreview.IsEnabled = true;
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

        private async void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtRootDir.Text))
            {
                btnPreview.IsEnabled = false;
                lvFiles.Items.Clear();
                worker = new Worker(txtRootDir.Text, txtFreeText.Text);
                worker.PreviewProgressChanged += Worker_PreviewProgressChanged;
                await worker.PreviewAsync();
            }
        }

        private void Worker_PreviewProgressChanged(PrepareProgressData progress)
        {
            if (!string.IsNullOrEmpty(progress.Status))
            {
                tbStatus.Text = progress.Status;
            }

            if (progress.ProgressType == ProgressType.PreviewFileNames)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Foreground = progress.CurrentFilePath.Length > 260 ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.Green);
                lvi.Content = progress.CurrentFilePath;
                lvFiles.Items.Add(lvi);
            }

            if (progress.ProgressType == ProgressType.RemoveJunkFiles)
            {
                pBar.Value = progress.CurrentFileId;
                if (worker.MaxFilesCount > 0)
                {
                    pBar.Maximum = worker.MaxFilesCount;
                }
            }

            if (progress.ProgressType == ProgressType.ReadyToRename)
            {
                tbStatus.Text = "Preparation complete!";
                btnGo.IsEnabled = true;
            }
        }

        private async void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFreeText.Text))
            {
                MessageBox.Show("Free Text is empty!", Application.Current.MainWindow.Title);
            }
            else if (chkCopyFiles.IsChecked == false)
            {
                MessageBox.Show("You have not completed Step 1 above!", Application.Current.MainWindow.Title);
            }
            else if (lvFiles.Items.Count == 0)
            {
                MessageBox.Show("Please press the Preview button before trying to rename.", Application.Current.MainWindow.Title);
            }
            else
            {
                btnGo.IsEnabled = false;
                pBar.Value = 0;

                worker = new Worker(txtRootDir.Text, txtFreeText.Text);
                worker.RenameProgressChanged += Worker_FileMoveProgressChanged;
                await worker.RenameAsync();

                btnPreview.IsEnabled = true;
                btnGo.IsEnabled = false;
            }
        }

        private void Worker_FileMoveProgressChanged(RenameProgressData progress)
        {
            if (!string.IsNullOrEmpty(worker.Error))
            {
                tbStatus.Text = worker.Error;
            }

            pBar.Value = progress.CurrentFileId;
            if (worker.MaxFilesCount > 0)
            {
                pBar.Maximum = worker.MaxFilesCount;

                if (pBar.Value == pBar.Maximum)
                    tbStatus.Text = "Rename complete!";
            }
        }

        private void lvFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string fp = lvFiles.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(fp))
            {
                string dir = Path.GetDirectoryName(fp);
                if (Directory.Exists(dir))
                {
                    Helpers.OpenFolder(dir);
                }
            }
        }

        private void txtFreeText_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnPreview.IsEnabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SettingsManager.SaveSettings();
        }
    }
}