﻿using System;
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
            if (!Directory.Exists(AppData.RootDir))
            {
                ShareX.HelpersLib.Helpers.CreateDirectoryFromDirectoryPath(AppData.RootDir);
            }

            if (!AppData.IsFilesCopied)
            {
                await App.Current.MainPage.DisplayAlert(App.Title, "You have not completed Step 1 above!", "OK");
            }
            else if (GetFiles(AppData.RootDir, App.JunkFilesList, SearchOption.TopDirectoryOnly).Count() > 0)
            {
                await App.Current.MainPage.DisplayAlert(App.Title, "Files detected in the Specified Records folder. \n\nPlease remove all the files, and copy only folders!\n\n" + GetFiles(AppData.RootDir, App.JunkFilesList, SearchOption.TopDirectoryOnly).First().ToString(), "OK");
            }
            else if (string.IsNullOrEmpty(AppData.FreeText))
            {
                await App.Current.MainPage.DisplayAlert(App.Title, "Free Text is empty!", "OK");
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
                AppData.IsIdle = false;
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
                    AppData.IsIdle = true;
                }
            }
        }

    }
}

