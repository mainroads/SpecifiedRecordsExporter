using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShareX.HelpersLib;

namespace SpecifiedRecordsExporter
{
    public class Worker
    {
        public delegate void RenameProgressChangedEventHandler(RenameProgressData progress);
        public event RenameProgressChangedEventHandler RenameProgressChanged;

        public delegate void PreviewProgressChangedEventHandler(PrepareProgressData progress);
        public event PreviewProgressChangedEventHandler PreviewProgressChanged;

        public int MaxFilesCount { get; private set; }
        public RenameProgressData RenameProgress = new RenameProgressData();
        public PrepareProgressData PrepareProgress = new PrepareProgressData();

        public string Error { get; private set; }

        private TaskEx<RenameProgressData> taskRename;
        private TaskEx<PrepareProgressData> taskPreview;

        private string rootDir;
        private string freeText;
        public StringBuilder DebugLog { get; private set; } = new StringBuilder();

        public Worker(string rootDir, string freeText)
        {
            RenameProgress = new RenameProgressData();
            PrepareProgress = new PrepareProgressData();

            taskPreview = new TaskEx<PrepareProgressData>();
            taskPreview.ProgressChanged += OnPreviewProgressChanged;

            taskRename = new TaskEx<RenameProgressData>();
            taskRename.ProgressChanged += OnRenameProgressChanged;


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

        private void OnPreviewProgressChanged(PrepareProgressData progress)
        {
            PreviewProgressChanged?.Invoke(progress);
        }

        public async Task PreviewAsync()
        {
            await taskPreview.Run(Prepare);
        }

        public async Task RenameAsync()
        {
            await taskRename.Run(Rename);
        }

        private void OnRenameProgressChanged(RenameProgressData progress)
        {
            RenameProgressChanged?.Invoke(progress);
        }

        public void Stop()
        {
            taskRename.Cancel();
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

        private void Prepare()
        {
            if (Directory.Exists(rootDir))
            {
                DebugLog.AppendLine($"{DateTime.Now.ToString("yyyyMMddTHHmmss")} Prepare started.");
                RemoveJunkFiles();
                if (!PrepareProgress.HasLongFileNames)
                {
                    UnzipNonCadFiles();
                    ZipCadFolders(rootDir);
                }

                PrepareProgress.ProgressType = ProgressType.ReadyToRename;
                taskPreview.Report(PrepareProgress);
            }
        }

        private void RemoveJunkFiles()
        {
            string[] files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
            foreach(string fp in files)
            {
                if (GetDestPath(fp).Length > 260)
                {
                    ShortenFilePath(fp);
                    PrepareProgress.HasLongFileNames = true;
                }
            }

            string[] filesValid = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
            MaxFilesCount = filesValid.Length;
            foreach (string fp in filesValid)
            {
                PrepareProgress.IsJunkFile = IsJunkFile(fp);
                PrepareProgress.CurrentFileId++;
                if (PrepareProgress.IsJunkFile)
                {
                    PrepareProgress.ProgressType = ProgressType.RemoveJunkFiles;
                    PrepareProgress.CurrentFilePath = fp;
                    PrepareProgress.Status = $"Removing {fp}";
                }
                else
                {
                    PrepareProgress.ProgressType = ProgressType.PreviewFileNames;
                    PrepareProgress.CurrentFilePath = GetDestPath(fp);
                }

                taskPreview.Report(PrepareProgress);

                if (PrepareProgress.IsJunkFile)
                {
                    Helpers.WaitWhile(() => DeleteFile(fp), 250, 5000);
                    DebugLog.AppendLine($"{DateTime.Now.ToString("yyyyMMddTHHmmss")} Removed {fp}");
                }

                taskPreview.ThrowIfCancellationRequested();
            }
        }

        private string ShortenFilePath(string fp)
        {
            int diff = GetDestPath(fp).Length - 260;
            string sfn = Path.GetFileNameWithoutExtension(fp).Substring(0, Path.GetFileNameWithoutExtension(fp).Length - diff);
            string sfp = Path.Combine(Path.GetDirectoryName(fp), sfn) + Path.GetExtension(fp);
            File.Move(fp, sfp);
            DebugLog.AppendLine($"Renamed {fp} to {sfp}");
            return sfp;
        }

        private void UnzipNonCadFiles()
        {
            PrepareProgress.ProgressType = ProgressType.UnzipNonCadFiles;
            string[] zipFiles = Directory.GetFiles(rootDir, "*.zip", SearchOption.AllDirectories);
            MaxFilesCount = zipFiles.Length;
            foreach (string fpZipFile in zipFiles)
            {
                PrepareProgress.Status = $"Checking zip file {fpZipFile}";
                taskPreview.Report(PrepareProgress);
                string zipDir = Path.Combine(Path.GetDirectoryName(fpZipFile), Path.GetFileNameWithoutExtension(fpZipFile));
                try
                {
                    ZipManager.Extract(fpZipFile, zipDir);
                }
                catch(Exception ex)
                {
                    string corruptedRecords = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}Downloads{Path.DirectorySeparatorChar}Corrupted Records";
                    Helpers.CreateDirectoryFromDirectoryPath(corruptedRecords);
                    File.Move(fpZipFile, Path.Combine(corruptedRecords, Path.GetFileName(fpZipFile)));
                    DebugLog.AppendLine(ex.Message);
                }

                if (Directory.Exists(zipDir))
                {
                    string[] longFileNames = Directory.GetFiles(zipDir, "*.*", SearchOption.AllDirectories);
                    foreach (string fp in longFileNames)
                    {
                        if (GetDestPath(fp).Length > 260)
                        {
                            ShortenFilePath(fp);
                            PrepareProgress.HasLongFileNames = true;
                            break;
                        }
                    }

                    DebugLog.AppendLine($"{DateTime.Now.ToString("yyyyMMddTHHmmss")} Unzipped {fpZipFile}");
                    string[] cadFiles = Directory.GetFiles(zipDir, "*.dwg", SearchOption.AllDirectories);
                    if (cadFiles.Length > 0)
                    {
                        Helpers.WaitWhile(() => DeleteFolder(zipDir), 250, 5000);
                    }
                    else
                    {
                        Helpers.WaitWhile(() => DeleteFile(fpZipFile), 250, 5000);
                    }
                }
            }
            DebugLog.AppendLine($"{DateTime.Now.ToString("yyyyMMddTHHmmss")} Unzipped {zipFiles.Length} non-CAD files");
        }

        private void ZipCadFolders(string dwgFolder)
        {
            PrepareProgress.ProgressType = ProgressType.ZipCadFiles;
            string[] dwgFiles = Directory.GetFiles(dwgFolder, "*.dwg", SearchOption.TopDirectoryOnly);
            if (dwgFiles.Length > 0)
            {
                string zipFileName = Path.GetFileName(dwgFolder);
                if (!Path.GetFileNameWithoutExtension(dwgFolder).Contains("CAD"))
                {
                    zipFileName += " CAD";
                }
                PrepareProgress.Status = $"Zipping {dwgFolder}";
                taskPreview.Report(PrepareProgress);
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
            RenameProgress.CurrentFileId = 1;

            if (Directory.Exists(rootDir))
            {
                var files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);

                MaxFilesCount = files.Count();

                foreach (string fp in files)
                {
                    if (Helpers.WaitWhile(() => MoveFile(fp), 250, 5000))
                    {
                        RenameProgress.CurrentFileId++;
                        taskRename.Report(RenameProgress);
                    }

                    taskRename.ThrowIfCancellationRequested();
                }

                string[] dirs = Directory.GetDirectories(rootDir);
                foreach (string dir in dirs)
                {
                    DeleteEmptyFolders(dir);
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


        private bool MoveFile(string origPath)
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

        private void DeleteEmptyFolders(string dirPath)
        {
            foreach (string subdirPath in Directory.GetDirectories(dirPath))
            {
                DeleteEmptyFolders(subdirPath);
            }

            if (EmptyFolderHelper.CheckDirectoryEmpty(dirPath))
            {
                new DirectoryInfo(dirPath).Delete();
            }
        }
    }
}
