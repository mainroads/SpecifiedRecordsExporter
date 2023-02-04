using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
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

        private Worker worker;

        #region AppDataModel Properties

        // TODO: Try AppData
        /*
        private AppDataModel _appData = new AppDataModel();
        public AppDataModel AppData
        {
            get { return _appData; }
            set
            {
                _appData = value;
                OnPropertyChanged(nameof(AppData));
            }
        }
        */

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
                OnPropertyChanged(nameof(Status));
            }
        }

        private double _progress;
        public double Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        private bool _isIdle;
        public bool IsIdle
        {
            get { return _isIdle; }
            set
            {
                _isIdle = value;
                OnPropertyChanged(nameof(IsIdle));
            }
        }

        private ObservableCollection<SpecifiedRecord> filesColl = new ObservableCollection<SpecifiedRecord>();
        public ObservableCollection<SpecifiedRecord> FilesCollection
        {
            get { return filesColl; }
            set
            {
                if (value != this.filesColl)
                    filesColl = value;
                OnPropertyChanged(nameof(FilesCollection));
            }
        }

        #endregion

        public Command PrepareCommand { private set; get; }

        public MainPageViewModel()
        {
            PrepareCommand = new Command(
                execute: () =>
                {
                    Prepare();
                    RefreshCanExecutes();
                }, canExecute: () =>
                {
                    return true;
                });
        }

        void RefreshCanExecutes()
        {
            (PrepareCommand as Command).ChangeCanExecute();
        }

        private async void Prepare()
        {
            if (!Directory.Exists(RootDir))
            {
                ShareX.HelpersLib.Helpers.CreateDirectoryFromDirectoryPath(RootDir);
            }

            if (!IsFilesCopied)
            {
                await App.Current.MainPage.DisplayAlert(App.Title, "You have not completed Step 1 above!", "OK");
            }
            else if (GetFiles(RootDir, App.JunkFilesList, SearchOption.TopDirectoryOnly).Count() > 0)
            {
                await App.Current.MainPage.DisplayAlert(App.Title, "Files detected in the Specified Records folder. \n\nPlease remove all the files, and copy only folders!\n\n" + GetFiles(RootDir, App.JunkFilesList, SearchOption.TopDirectoryOnly).First().ToString(), "OK");
            }
            else if (string.IsNullOrEmpty(FreeText))
            {
                await App.Current.MainPage.DisplayAlert(App.Title, "Free Text is empty!", "OK");
            }
            else
            {
                try
                {
                    if (!string.IsNullOrEmpty(RootDir))
                    {
                        FilesCollection.Clear();
                        worker = new Worker(RootDir, FreeText);
                        worker.PreviewProgressChanged += Worker_PreviewProgressChanged;
                        await worker.PrepareAndRenameAsync();
                    }
                }
                finally
                {
                    SettingsManager.SaveLog(worker.DebugLog);
                }
            }
        }

        public static IEnumerable<string> GetFiles(string path, IEnumerable<string> excludedFiles, SearchOption searchOption = SearchOption.AllDirectories)
        {
            var filePaths = Directory.EnumerateFiles(path, "*.*", searchOption);

            foreach (var fp in filePaths)
            {
                if (excludedFiles.Contains(Path.GetFileName(fp)))
                {
                    continue;
                }
                else if (excludedFiles.Contains(Path.GetExtension(fp)))
                {
                    continue;
                }

                yield return fp;
            }
        }

        private void Worker_PreviewProgressChanged(ProgressData progress)
        {
            if (!string.IsNullOrEmpty(progress.Status))
            {
                Status = progress.Status;
            }

            if (progress.ProgressType == ProgressType.PreviewFileNames)
            {
                FilesCollection.Add(new SpecifiedRecord() { FilePath = progress.CurrentFilePath });
            }

            if (progress.ProgressType == ProgressType.RemoveJunkFiles)
            {
                IsIdle = false;
                Progress = (double)progress.CurrentFileId / (double)worker.MaxFilesCount;
            }

            if (progress.ProgressType == ProgressType.ReadyToRename)
            {
                if (progress.HasLongFileNames)
                {
                    Status = "Long file names were detected and shortened. Preparation complete!";
                }
                else
                {
                    Status = "Preparation complete!";
                }
            }

            if (progress.ProgressType == ProgressType.Renaming)
            {
                if (!string.IsNullOrEmpty(worker.Error))
                {
                    Status = worker.Error;
                }
                else
                {
                    Status = $"Renaming {progress.CurrentFilePath}";
                }

                Progress = (double)progress.CurrentFileId / (double)worker.MaxFilesCount;

                if (progress.CurrentFileId == worker.MaxFilesCount)
                {

                    Status = "Rename complete!";
                    IsIdle = true;
                }
            }
        }

    }
}

