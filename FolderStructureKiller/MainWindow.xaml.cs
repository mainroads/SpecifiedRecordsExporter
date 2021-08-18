using System.IO;
using System.Windows;

namespace FolderStructureKiller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFreeText.Text))
                MessageBox.Show("Free Text is empty!");

            if (Directory.Exists(txtRootDir.Text))
            {
                var rootDir = txtRootDir.Text;
                var files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
                foreach (var fp in files)
                {
                    MoveFile(rootDir, fp);
                }
            }
        }

        private void MoveFile(string rootDir, string origPath)
        {
            if (!rootDir.EndsWith(@"\"))
                rootDir = rootDir + @"\";

            string path2 = origPath.Split(rootDir)[1];
            string fn = txtFreeText.Text + " - " + path2.Replace(@"\", " - ");
            string destPath = Path.Combine(rootDir, fn);
            File.Move(origPath, destPath);
        }
    }
}
