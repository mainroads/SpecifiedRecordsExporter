using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpecifiedRecordsExporter
{
    public class MainPageViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public new event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string RootDir => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}Downloads{Path.DirectorySeparatorChar}Specified Records";
        public string FreeText => "";
        public ICommand PrepareCommand { private set; get; }

        private ObservableCollection<string> filesColl = new ObservableCollection<string>();
        public ObservableCollection<string> FilesCollection
        {
            get { return filesColl; }
            set
            {
                if (value != this.filesColl)
                    filesColl = value;
                this.SetPropertyChanged("FileObjectCollection");
            }
        }

        public MainPageViewModel()
        {
          
        }


    }
}

