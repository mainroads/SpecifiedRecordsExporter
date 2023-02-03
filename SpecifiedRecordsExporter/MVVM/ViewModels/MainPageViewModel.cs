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
        private bool previewOnce;

        #region AppDataModel Properties
        // TODO: AppDataModel AppData
        public string Title => $"{App.Title} v{Assembly.GetExecutingAssembly().GetName().Version}";
        public bool IsFilesCopied { get; set; }
        public string RootDir => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}Downloads{Path.DirectorySeparatorChar}Specified Records";
        public string FreeText { get; set; }
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
                IsIdle = false;

                try
                {
                    if (!string.IsNullOrEmpty(RootDir))
                    {
                        // lvFiles.Items.Clear();
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
                // ListViewItem lvi = new ListViewItem();
                // lvi.Foreground = progress.CurrentFilePath.Length > 260 ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.Green);
                // lvi.Content = progress.CurrentFilePath;
                // lvFiles.Items.Add(lvi);
            }

            if (progress.ProgressType == ProgressType.RemoveJunkFiles)
            {
                Progress = (double)progress.CurrentFileId / (double)worker.MaxFilesCount;
            }

            if (progress.ProgressType == ProgressType.ReadyToRename)
            {
                if (progress.HasLongFileNames)
                {
                    if (!previewOnce)
                    {
                        previewOnce = true;
                        Prepare();
                    }
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

