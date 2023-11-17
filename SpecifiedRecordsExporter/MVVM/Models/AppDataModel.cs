using System.Collections.ObjectModel;
using System.Reflection;

namespace SpecifiedRecordsExporter
{
    public class AppDataModel : ObservableModel
    {
        public string Title => $"{App.Title} v{Assembly.GetExecutingAssembly().GetName().Version}";
        public bool IsFilesCopied { get; set; }
        public string RootDir
        {
            get
            {
                if (!Directory.Exists(SettingsManager.Settings.RootDir))
                {
                    SettingsManager.Settings.RootDir = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}Downloads{Path.DirectorySeparatorChar}Specified Records";
                }
                return SettingsManager.Settings.RootDir;
            }
        }

        public string FreeText { get; set; }

        private string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        private double _progress;
        public double Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        private bool _isIdle = true;
        public bool IsIdle
        {
            get { return _isIdle; }
            set
            {
                _isIdle = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<SpecifiedRecord> filesColl = new ObservableCollection<SpecifiedRecord>();
        public ObservableCollection<SpecifiedRecord> FilesCollection
        {
            get { return filesColl; }
            set
            {
                filesColl = value;
                OnPropertyChanged();
            }
        }

        public AppDataModel()
        {

        }
    }
}

