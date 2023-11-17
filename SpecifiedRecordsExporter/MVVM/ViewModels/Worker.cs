using ShareX.HelpersLib;
using System.Text;

namespace SpecifiedRecordsExporter
{
    public class Worker
    {
        public delegate void PreviewProgressChangedEventHandler(ProgressData progress);
        public event PreviewProgressChangedEventHandler PreviewProgressChanged;

        public int MaxFilesCount { get; private set; }
        public ProgressData Progress = new ProgressData();

        public string Error { get; private set; }

        private TaskEx<ProgressData> taskPrepare;

        private string rootDir;
        private string freeText;
        public StringBuilder DebugLog { get; private set; } = new StringBuilder();

        public Worker(string rootDir, string freeText)
        {
            Progress = new ProgressData();

            taskPrepare = new TaskEx<ProgressData>();
            taskPrepare.ProgressChanged += OnPreviewProgressChanged;

            if (Directory.Exists(rootDir))
            {
                if (!rootDir.EndsWith(Path.DirectorySeparatorChar))
                {
                    rootDir += Path.DirectorySeparatorChar;
                }

                this.rootDir = rootDir;
                this.freeText = Helpers.GetValidFileName(freeText, ".");
            }
        }

        private void OnPreviewProgressChanged(ProgressData progress)
        {
            PreviewProgressChanged?.Invoke(progress);
        }

        public async Task PrepareAndRenameAsync()
        {
            await taskPrepare.Run(PrepareAndRename);
        }

        public void Stop()
        {
            taskPrepare.Cancel();
        }

        private string GetDestPath(string origPath)
        {
            string path2 = origPath.Split(rootDir)[1];
            StringBuilder fn = new StringBuilder();
            if (!string.IsNullOrEmpty(freeText))
            {
                fn.Append(freeText + " - ");
            }
            fn.Append(path2.Replace(Path.DirectorySeparatorChar.ToString(), " - "));
            return Path.Combine(rootDir, fn.ToString());
        }

        private bool IsJunkFile(string origPath)
        {
            foreach (string fileName in App.JunkFilesList)
            {
                if ((Path.GetFileName(origPath) == fileName) || Path.GetExtension(origPath) == fileName)
                    return true;
            }

            return false;
        }

        private void PrepareAndRename()
        {
            Prepare();
            Rename();
        }
        private void Prepare()
        {
            if (Directory.Exists(rootDir))
            {
                DebugLog.AppendLine($"{DateTime.Now.ToString("yyyyMMddTHHmmss")} Prepare started.");
                RemoveJunkFiles();
                if (!Progress.HasLongFileNames)
                {
                    UnzipNonCadFiles();
                    ZipCadFolders(rootDir);
                }

                Progress.ProgressType = ProgressType.ReadyToRename;
                taskPrepare.Report(Progress);
            }
        }

        private void RemoveJunkFiles()
        {
            string[] files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
            foreach (string fp in files)
            {
                if (GetDestPath(fp).Length > 260)
                {
                    Helpers.WaitWhile(() => ShortenFilePath(fp), 250, 5000);
                    Progress.HasLongFileNames = true;
                }
            }

            string[] filesValid = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
            MaxFilesCount = filesValid.Length;
            foreach (string fp in filesValid)
            {
                Progress.IsJunkFile = IsJunkFile(fp);
                Progress.CurrentFileId++;
                if (Progress.IsJunkFile)
                {
                    Progress.ProgressType = ProgressType.RemoveJunkFiles;
                    Progress.CurrentFilePath = fp;
                    Progress.Status = $"Removing {fp}";
                }
                else
                {
                    Progress.ProgressType = ProgressType.PreviewFileNames;
                    Progress.CurrentFilePath = GetDestPath(fp);
                }

                taskPrepare.Report(Progress);

                if (Progress.IsJunkFile)
                {
                    Helpers.WaitWhile(() => DeleteFile(fp), 250, 5000);
                    DebugLog.AppendLine($"{DateTime.Now.ToString("yyyyMMddTHHmmss")} Removed {fp}");
                }

                taskPrepare.ThrowIfCancellationRequested();
            }
        }

        public void UnzipNonCadFiles()
        {
            try
            {
                UnzipNonCadFilesRecursive(rootDir);
            }
            catch (Exception ex)
            {
                DebugLog.AppendLine($"Error while unzipping files: {ex.Message}");
            }
        }

        private void UnzipNonCadFilesRecursive(string directoryPath)
        {
            string[] zipFiles = Directory.GetFiles(directoryPath, "*.zip", SearchOption.AllDirectories);
            foreach (string zipFilePath in zipFiles)
            {
                Progress.Status = $"Checking zip file {zipFilePath}";
                taskPrepare.Report(Progress);
                string zipDir = Path.Combine(Path.GetDirectoryName(zipFilePath), Path.GetFileNameWithoutExtension(zipFilePath));

                try
                {
                    ZipManager.Extract(zipFilePath, zipDir);
                }
                catch (Exception ex)
                {
                    string corruptedRecords = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}Downloads{Path.DirectorySeparatorChar}Corrupted Records";
                    Helpers.CreateDirectoryFromDirectoryPath(corruptedRecords);
                    File.Move(zipFilePath, Path.Combine(corruptedRecords, Path.GetFileName(zipFilePath)));
                    DebugLog.AppendLine(ex.Message);
                }

                if (Directory.Exists(zipDir))
                {
                    string[] longFileNames = Directory.GetFiles(zipDir, "*.*", SearchOption.AllDirectories);
                    foreach (string fp in longFileNames)
                    {
                        if (GetDestPath(fp).Length > 260)
                        {
                            Helpers.WaitWhile(() => ShortenFilePath(fp), 250, 5000);
                            Progress.HasLongFileNames = true;
                            break;
                        }
                    }

                    DebugLog.AppendLine($"{DateTime.Now.ToString("yyyyMMddTHHmmss")} Unzipped {zipFilePath}");
                    string[] cadFiles = Directory.GetFiles(zipDir, "*.dwg", SearchOption.AllDirectories);
                    if (cadFiles.Length > 0)
                    {
                        Helpers.WaitWhile(() => DeleteFolder(zipDir), 250, 5000);
                    }
                    else
                    {
                        Helpers.WaitWhile(() => DeleteFile(zipFilePath), 250, 5000);
                    }

                    UnzipNonCadFilesRecursive(zipDir);
                }
            }
            DebugLog.AppendLine($"{DateTime.Now.ToString("yyyyMMddTHHmmss")} Unzipped {zipFiles.Length} non-CAD files");
        }


        private void ZipCadFolders(string dwgFolder)
        {
            Progress.ProgressType = ProgressType.ZipCadFiles;
            string[] dwgFiles = Directory.GetFiles(dwgFolder, "*.dwg", SearchOption.TopDirectoryOnly);
            if (dwgFiles.Length > 0)
            {
                string zipFileName = Path.GetFileName(dwgFolder);
                if (!Path.GetFileNameWithoutExtension(dwgFolder).Contains("CAD"))
                {
                    zipFileName += " CAD";
                }
                Progress.Status = $"Zipping {dwgFolder}";
                taskPrepare.Report(Progress);
                ZipManager.Compress(dwgFolder, Path.Combine(Path.GetDirectoryName(dwgFolder), $"{zipFileName}.zip"));
                DebugLog.AppendLine($"{DateTime.Now.ToString("yyyyMMddTHHmmss")} Zipped {dwgFolder} folder");
                Helpers.WaitWhile(() => DeleteFolder(dwgFolder), 250, 5000);
            }
            else
            {
                string[] dwgSubFolders = Directory.GetDirectories(dwgFolder);
                foreach (string dwgSubFolder in dwgSubFolders)
                {
                    ZipCadFolders(dwgSubFolder);
                }
            }
        }

        private void Rename()
        {
            Progress.ProgressType = ProgressType.Renaming;
            Progress.CurrentFileId = 0;

            if (Directory.Exists(rootDir))
            {
                var files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);

                MaxFilesCount = files.Count();

                foreach (string fp in files)
                {
                    if (Helpers.WaitWhile(() => RenameFile(fp), 250, 5000))
                    {
                        Progress.CurrentFilePath = fp;
                        Progress.CurrentFileId++;
                        taskPrepare.Report(Progress);
                    }

                    taskPrepare.ThrowIfCancellationRequested();
                }

                string[] dirs = Directory.GetDirectories(rootDir);
                foreach (string dir in dirs)
                {
                    Helpers.WaitWhile(() => DeleteEmptyFolders(dir), 250, 5000);
                }
                DebugLog.AppendLine($"{DateTime.Now.ToString("yyyyMMddTHHmmss")} Renamed {files.Length} files");
                DebugLog.AppendLine();
            }
        }

        private bool DeleteFolder(string dir)
        {
            try
            {
                Directory.Delete(dir, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private bool DeleteFile(string fp)
        {
            try
            {
                System.IO.File.Delete(fp);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ShortenFilePath(string fp)
        {
            int diff = GetDestPath(fp).Length - 260;
            string sfn = Path.GetFileNameWithoutExtension(fp).Substring(0, Path.GetFileNameWithoutExtension(fp).Length - diff);
            string sfp = Path.Combine(Path.GetDirectoryName(fp), sfn) + Path.GetExtension(fp);

            try
            {
                System.IO.File.Move(fp, sfp);
                return true;
            }
            catch (Exception ex)
            {
                Error = $"Renaming {fp}";
                DebugLog.AppendLine(ex.Message);
            }

            return false;
        }

        private bool RenameFile(string origPath)
        {
            string destPath = GetDestPath(origPath);

            try
            {
                System.IO.File.Move(origPath, destPath);
                return true;
            }
            catch (Exception ex)
            {
                Error = $"Renaming {origPath}";
                DebugLog.AppendLine(ex.Message);
            }

            return false;
        }

        private bool DeleteEmptyFolders(string dirPath)
        {
            try
            {
                foreach (string subdirPath in Directory.GetDirectories(dirPath))
                {
                    DeleteEmptyFolders(subdirPath);
                }

                if (EmptyFolderHelper.CheckDirectoryEmpty(dirPath))
                {
                    new DirectoryInfo(dirPath).Delete();
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}
