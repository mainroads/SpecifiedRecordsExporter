using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SpecifiedRecordsExporter
{
    public class AppDataModel : ObservableModel
    {
        public string Title => $"{App.Title} v{Assembly.GetExecutingAssembly().GetName().Version}";
        public bool IsFilesCopied { get; set; }
        public string RootDir => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}Downloads{Path.DirectorySeparatorChar}Specified Records";
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

        private bool _isIdle;
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

