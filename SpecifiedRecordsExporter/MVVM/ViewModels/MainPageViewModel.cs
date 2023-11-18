using ShareX.HelpersLib;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;

namespace SpecifiedRecordsExporter
{
    public class MainPageViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public StringBuilder DebugLog { get; private set; } = new StringBuilder();
        public new event PropertyChangedEventHandler PropertyChanged;

        private Worker worker;

        private AppDataModel _appData = new AppDataModel();
        public AppDataModel AppData
        {
            get { return _appData; }
            set
            {
                _appData = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OpenFolderText)); // Notify that OpenFolderText has changed
            }
        }

        public Command PrepareCommand { private set; get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand OpenLogCommand { get; }

        public string OpenFolderText
        {
            get
            {
                var folderName = Path.GetFileName(AppData.RootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return string.IsNullOrWhiteSpace(folderName) ? "Open folder" : $"Open {folderName} folder";
            }
        }


        public MainPageViewModel()
        {
            PrepareCommand = new Command(
                execute: () =>
                {
                    try
                    {
                        Prepare();
                        RefreshCanExecutes();
                    }
                    catch (Exception ex)
                    {
                        DebugLog.AppendLine(ex.Message);
                        DebugLog.AppendLine(ex.StackTrace);

                        if (ex.InnerException != null)
                        {
                            DebugLog.AppendLine(ex.InnerException.Message);
                            DebugLog.AppendLine(ex.InnerException.StackTrace);
                        }
                    }
                }, canExecute: () =>
                {
                    return true;
                });

            OpenFolderCommand = new Command(OpenFolder);
            OpenLogCommand = new Command(OpenLog);
        }

        private void OpenLog()
        {
            Helpers.OpenFile(SettingsManager.LogFilePath);
        }

        public void OpenFolder()
        {
            Helpers.OpenFolder(AppData.RootDir);
        }

        void RefreshCanExecutes()
        {
            (PrepareCommand as Command).ChangeCanExecute();
        }

        private async void Prepare()
        {
            AppData.IsIdle = false;

            if (!Directory.Exists(AppData.RootDir))
            {
                ShareX.HelpersLib.Helpers.CreateDirectoryFromDirectoryPath(AppData.RootDir);
            }

            if (!AppData.IsFilesCopied)
            {
                await App.Current.MainPage.DisplayAlert(App.Title, "You have not completed Step 2 above!", "OK");
                AppData.IsIdle = true;
            }
            else if (GetFiles(AppData.RootDir, App.JunkFilesList, SearchOption.TopDirectoryOnly).Count() > 0)
            {
                await App.Current.MainPage.DisplayAlert(App.Title, "Files detected in the Specified Records folder. \n\nPlease remove all the files, and copy only folders!\n\n" + GetFiles(AppData.RootDir, App.JunkFilesList, SearchOption.TopDirectoryOnly).First().ToString(), "OK");
                AppData.IsIdle = true;
            }
            else if (string.IsNullOrEmpty(AppData.FreeText))
            {
                await App.Current.MainPage.DisplayAlert(App.Title, "Free Text is empty!", "OK");
                AppData.IsIdle = true;
            }
            else
            {
                try
                {
                    if (!string.IsNullOrEmpty(AppData.RootDir))
                    {
                        AppData.FilesCollection.Clear();
                        worker = new Worker(AppData.RootDir, AppData.FreeText);
                        worker.PreviewProgressChanged += Worker_PreviewProgressChanged;
                        await worker.PrepareAndRenameAsync();
                    }
                }
                finally
                {
                    SettingsManager.SaveLog(worker.DebugLog);
                    AppData.IsIdle = true;
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
                AppData.Status = progress.Status;
            }

            if (progress.ProgressType == ProgressType.PreviewFileNames)
            {
                AppData.FilesCollection.Add(new SpecifiedRecord() { FilePath = progress.CurrentFilePath });
            }

            if (progress.ProgressType == ProgressType.RemoveJunkFiles)
            {
                AppData.Progress = (double)progress.CurrentFileId / (double)worker.MaxFilesCount;
            }

            if (progress.ProgressType == ProgressType.ReadyToRename)
            {
                if (progress.HasLongFileNames)
                {
                    AppData.Status = "Long file names were detected and shortened. Preparation complete!";
                }
                else
                {
                    AppData.Status = "Preparation complete!";
                }
            }

            if (progress.ProgressType == ProgressType.Renaming)
            {
                if (!string.IsNullOrEmpty(worker.Error))
                {
                    AppData.Status = worker.Error;
                }
                else
                {
                    AppData.Status = $"Renaming {progress.CurrentFilePath}";
                }

                AppData.Progress = (double)progress.CurrentFileId / (double)worker.MaxFilesCount;

                if (progress.CurrentFileId == worker.MaxFilesCount)
                {
                    AppData.Status = "Rename complete!";
                }
            }
        }

    }
}

