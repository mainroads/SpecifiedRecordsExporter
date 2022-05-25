using ShareX.HelpersLib;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                if (!rootDir.EndsWith(@"\"))
                {
                    rootDir += @"\";
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
            fn.Append(path2.Replace(@"\", " - "));
            return Path.Combine(rootDir, fn.ToString());
        }

        private bool DeleteUnWantedFile(string origPath)
        {
            if (Path.GetExtension(origPath) == ".DS_Store")
            {
                return true;
            }
            else if (Path.GetFileName(origPath) == "TRIM.dat")
            {
                return true;
            }

            return false;
        }

        private void Prepare()
        {
            if (Directory.Exists(rootDir))
            {
                RemoveJunkFiles();
                UnzipNonCadFiles();
                ZipCadFolders(rootDir);

                PrepareProgress.ProgressType = ProgressType.ReadyToRename;
                taskPreview.Report(PrepareProgress);
            }
        }

        private void RemoveJunkFiles()
        {
            string[] files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
            MaxFilesCount = files.Length;
            foreach (string fp in files)
            {
                PrepareProgress.IsJunkFile = DeleteUnWantedFile(fp);
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
                }

                taskPreview.ThrowIfCancellationRequested();
            }
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
                ZipManager.Extract(fpZipFile, zipDir);
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
            RenameProgress.CurrentFileId = 0;

            if (Directory.Exists(rootDir))
            {
                var files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);

                MaxFilesCount = files.Count();

                foreach (string fp in files)
                {
                    if (MoveFile(fp))
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



        private bool MoveFile(string origPath)
        {
            string destPath = GetDestPath(origPath);

            try
            {
                if (destPath.Length < 260)
                {
                    File.Move(origPath, destPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Error = $"{destPath} ({Path.GetFileName(destPath).Length} characters): {ex.Message}";
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