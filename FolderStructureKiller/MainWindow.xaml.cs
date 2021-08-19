using System.IO;
using System.Threading.Tasks;
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

            string rootDir = txtRootDir.Text;
            string freeText = txtFreeText.Text;

            Task.Run(() => Run(rootDir, freeText)).ContinueWith(t => { }, TaskScheduler.FromCurrentSynchronizationContext());

        }

        private void Run(string rootDir, string freeText)
        {
            if (Directory.Exists(rootDir))
            {

                var files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
                Parallel.ForEach(files, fp => MoveFile(rootDir, freeText, fp));
            }
        }

        private void MoveFile(string rootDir, string freeText, string origPath)
        {
            if (!rootDir.EndsWith(@"\"))
                rootDir = rootDir + @"\";

            string path2 = origPath.Split(rootDir)[1];
            string fn = freeText + " - " + path2.Replace(@"\", " - ");
            string destPath = Path.Combine(rootDir, fn);

            File.Move(origPath, destPath);
        }
    }
}
