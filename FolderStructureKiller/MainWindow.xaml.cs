using System.Threading.Tasks;
using System.Windows;

namespace SpecifiedRecordsExporter
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

        private async void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFreeText.Text))
            {
                MessageBox.Show("Free Text is empty!");
            }
            else
            {
                btnGo.IsEnabled = false;

                string rootDir = txtRootDir.Text;
                string freeText = txtFreeText.Text;

                Worker worker = new Worker();
                await Task.Run(() => worker.Run(rootDir, freeText));

                btnGo.IsEnabled = true;
            }
        }


    }
}