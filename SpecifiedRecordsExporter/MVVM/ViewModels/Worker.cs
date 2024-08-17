using ShareX.HelpersLib;
using System.Text;
using System.Text.RegularExpressions;

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
                this.freeText = Regex.Replace(Helpers.GetValidFileName(freeText, "."), @"\s*-\s*$", "").Trim();
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
            // Split the original path based on rootDir and construct the new file name
            string path2 = origPath.Split(rootDir)[1];
            StringBuilder fn = new StringBuilder();

            if (!string.IsNullOrEmpty(freeText))
            {
                fn.Append(freeText + " - ");
            }
            fn.Append(path2.Replace(Path.DirectorySeparatorChar.ToString(), " - "));

            // Combine the rootDir and the new file name to get the full path
            string fp = Path.Combine(rootDir, fn.ToString());

            // Adjust the file name if it exceeds 260 characters
            if (fp.Length > 260)
            {
                int diff = fp.Length - 260;
                string sfn = Path.GetFileNameWithoutExtension(fp).Substring(0, Path.GetFileNameWithoutExtension(fp).Length - diff);
                fp = Path.Combine(Path.GetDirectoryName(fp), sfn) + Path.GetExtension(fp);
            }

            // Return a unique file path
            return Helpers.GetUniqueFilePath(
                Path.Combine(
                    Path.GetDirectoryName(fp),
                    FileHelpers.GetCleanFileName(Path.GetFileName(fp))
                )
            );

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
                App.DebugLog.WriteLine($"Prepare started.");
                RemoveJunkFiles();
                UnzipNonCadFiles();
                ZipCadFolders(rootDir);

                Progress.ProgressType = ProgressType.ReadyToRename;
                taskPrepare.Report(Progress);
            }
        }

        private void RemoveJunkFiles()
        {
            string[] files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
            Progress.Status = $"Analysing {files.Length} files";
            taskPrepare.Report(Progress);

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
                    Progress.CurrentFilePath = fp;
                }

                taskPrepare.Report(Progress);

                if (Progress.IsJunkFile)
                {
                    Helpers.WaitWhile(() => DeleteFile(fp), 250, 5000);
                    App.DebugLog.WriteLine($"Removed {fp}");
                }

                taskPrepare.ThrowIfCancellationRequested();
            }
        }

        public void UnzipNonCadFiles()
        {
            int zipFileCount = Directory.GetFiles(rootDir, "*.zip", SearchOption.AllDirectories).Length;

            // Continue processing while there are zip files in the rootDir
            while (zipFileCount > 0)
            {
                try
                {
                    // Process the zip files in rootDir
                    UnzipNonCadFilesRecursive(rootDir);
                }
                catch (Exception ex)
                {
                    App.DebugLog.WriteException($"Error while unzipping files in {rootDir}: {ex}");
                }

                // Recalculate the number of zip files after each iteration
                zipFileCount = Directory.GetFiles(rootDir, "*.zip", SearchOption.AllDirectories).Length;
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
                    // Determine extraction path and options based on zipFilePath length
                    if (zipFilePath.Length > 200)
                    {
                        // Attempt 1: Try with rootDir and overwrite set to true
                        ZipManager.Extract(zipFilePath, rootDir, true);
                    }
                    else
                    {
                        // Attempt 1: Try with zipDir and overwrite set to true
                        ZipManager.Extract(zipFilePath, zipDir, true);
                    }
                }
                catch (Exception ex1)
                {
                    App.DebugLog.WriteLine(zipFilePath);
                    App.DebugLog.WriteLine("Failed Attempt 1: First extraction attempt failed.");
                    App.DebugLog.WriteException(ex1);

                    try
                    {
                        // Determine second extraction path and options based on zipFilePath length
                        if (zipFilePath.Length > 200)
                        {
                            // Attempt 2: Try with rootDir and overwrite set to false
                            ZipManager.Extract(zipFilePath, rootDir, false);
                        }
                        else
                        {
                            // Attempt 2: Try with zipDir and overwrite set to false
                            ZipManager.Extract(zipFilePath, zipDir, false);
                        }
                    }
                    catch (Exception ex2)
                    {
                        App.DebugLog.WriteLine("Failed Attempt 2: Second extraction attempt failed.");
                        App.DebugLog.WriteException(ex2);

                        // Move the file to "Corrupted Records" directory if all attempts fail
                        string corruptedRecords = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Corrupted Records");
                        Helpers.CreateDirectoryFromDirectoryPath(corruptedRecords);
                        App.DebugLog.WriteLine($"Moved {zipFilePath} to {corruptedRecords}");
                        File.Move(zipFilePath, Path.Combine(corruptedRecords, Path.GetFileName(zipFilePath)));
                    }
                }

                if (Directory.Exists(zipDir))
                {
                    // Check for any zip files within this directory
                    string[] innerZipFiles = Directory.GetFiles(zipDir, "*.zip", SearchOption.AllDirectories);
                    if (innerZipFiles.Length > 0)
                    {
                        foreach (string innerZipFile in innerZipFiles)
                        {
                            string destinationPath = Path.Combine(rootDir, Path.GetFileName(innerZipFile));
                            File.Move(innerZipFile, destinationPath);
                        }
                        App.DebugLog.WriteLine($"Moved {innerZipFiles.Length} nested zip files to {rootDir} for later processing.");
                    }

                    bool hasCadFiles = SettingsManager.GetCadFileSearchPatterns()
                        .Any(pattern => Directory.GetFiles(zipDir, pattern, SearchOption.TopDirectoryOnly).Any());

                    if (hasCadFiles)
                    {
                        if (MoveNonCadFilesOutOfCadFolder(zipDir))
                        {
                            Helpers.WaitWhile(() => DeleteFile(zipFilePath), 250, 5000);
                            ZipManager.Compress(zipDir, zipFilePath);
                        }
                        Helpers.WaitWhile(() => DeleteFolder(zipDir), 250, 5000);
                    }
                    else
                    {
                        Helpers.WaitWhile(() => DeleteFile(zipFilePath), 250, 5000);
                    }

                    if (Directory.Exists(zipDir))
                    {
                        UnzipNonCadFilesRecursive(zipDir);
                    }
                }
            }

            if (zipFiles.Length > 0)
            {
                App.DebugLog.WriteLine($"Unzipped {zipFiles.Length} non-CAD files in {directoryPath}");
            }
        }

        private void ZipCadFolders(string cadFolder)
        {
            bool hasCadFiles = SettingsManager.GetCadFileSearchPatterns()
                .Any(pattern => Directory.GetFiles(cadFolder, pattern, SearchOption.TopDirectoryOnly).Any());

            if (hasCadFiles)
            {
                MoveNonCadFilesOutOfCadFolder(cadFolder); // Move non-CAD files before zipping
                string zipFileName = Path.GetFileName(cadFolder);
                if (!Path.GetFileNameWithoutExtension(cadFolder).Contains("CAD"))
                {
                    zipFileName += " CAD";
                }
                Progress.Status = $"Zipping {cadFolder}";
                taskPrepare.Report(Progress);
                ZipManager.Compress(cadFolder, Path.Combine(Path.GetDirectoryName(cadFolder), $"{zipFileName}.zip"));
                App.DebugLog.WriteLine($"Zipped {cadFolder} folder");
                Helpers.WaitWhile(() => DeleteFolder(cadFolder), 250, 5000);
            }
            else
            {
                string[] cadSubFolders = Directory.GetDirectories(cadFolder);
                foreach (string cadSubFolder in cadSubFolders)
                {
                    ZipCadFolders(cadSubFolder);
                }
            }
        }

        private bool MoveNonCadFilesOutOfCadFolder(string cadFolder)
        {
            bool filesMoved = false;
            var fileExtensions = SettingsManager.Settings.NonCadFileExtensions;

            foreach (var extension in fileExtensions)
            {
                string[] files = Directory.GetFiles(cadFolder, $"*.{extension}", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    string parentFolderPath = Directory.GetParent(cadFolder).FullName;

                    foreach (string file in files)
                    {
                        string destFile = Path.Combine(parentFolderPath, Path.GetFileName(file));
                        File.Move(file, destFile);
                        filesMoved = true;
                    }
                }
            }

            return filesMoved;
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
                    try
                    {
                        if (Helpers.WaitWhile(() => RenameFile(fp), 250, 5000))
                        {
                            Progress.CurrentFilePath = fp;
                            Progress.CurrentFileId++;
                            taskPrepare.Report(Progress);
                        }

                        taskPrepare.ThrowIfCancellationRequested();
                    }
                    catch (Exception ex)
                    {
                        App.DebugLog.WriteLine(fp);
                        App.DebugLog.WriteException(ex);
                    }
                }

                string[] dirs = Directory.GetDirectories(rootDir);
                foreach (string dir in dirs)
                {
                    Helpers.WaitWhile(() => DeleteEmptyFolders(dir), 250, 5000);
                }
                App.DebugLog.WriteLine($"Renamed {files.Length} files");
                App.DebugLog.WriteLine("");
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
                File.Delete(fp);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool RenameFile(string origPath)
        {
            string destPath = GetDestPath(origPath);

            if (File.Exists(origPath))
            {
                try
                {
                    File.Move(origPath, destPath);
                    return true;
                }
                catch (Exception ex)
                {
                    Error = $"Renaming {origPath}";
                    App.DebugLog.WriteLine($"origPath: {origPath}");
                    App.DebugLog.WriteLine($"destPath: {destPath}");
                    App.DebugLog.WriteException(ex);
                }
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
