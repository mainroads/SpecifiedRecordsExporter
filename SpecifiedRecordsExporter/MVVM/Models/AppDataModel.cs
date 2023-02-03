using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpecifiedRecordsExporter
{
    public class AppDataModel : ObservableModel
    {
        public bool IsFilesCopied { get; set; }
        public string RootDir => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}Downloads{Path.DirectorySeparatorChar}Specified Records";
        public string FreeText { get; set; } = "Test";
        public string Status { get; set; }
        public double Progress { get; set; }
        public bool IsIdle { get; set; }

        private ObservableCollection<string> filesColl = new ObservableCollection<string>();
        public ObservableCollection<string> FilesCollection
        {
            get { return filesColl; }
            set
            {
                if (value != this.filesColl)
                    filesColl = value;
                this.SetPropertyChanged("FilesCollection");
            }
        }

        public AppDataModel()
        {

        }
    }
}

