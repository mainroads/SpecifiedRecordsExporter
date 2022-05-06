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
        public delegate void RenameProgressChangedEventHandler(float progress);
        public event RenameProgressChangedEventHandler RenameProgressChanged;

        public delegate void PreviewProgressChangedEventHandler(string progress);
        public event PreviewProgressChangedEventHandler PreviewProgressChanged;

        public int FilesCount { get; private set; }
        public int MovedFilesCount { get; private set; }
        public string Error { get; private set; }

        private TaskEx<float> taskRename;
        private TaskEx<string> taskPreview;

        private string rootDir;
        private string freeText;

        public Worker(string rootDir, string freeText)
        {
            taskPreview = new TaskEx<string>();
            taskPreview.ProgressChanged += OnPreviewProgressChanged;

            taskRename = new TaskEx<float>();
            taskRename.ProgressChanged += OnRenameProgressChanged;


            if (Directory.Exists(rootDir))
            {
                if (!rootDir.EndsWith(@"\"))
                {
                    rootDir += @"\";
                }

                this.rootDir = rootDir;
                this.freeText = freeText.Trim();
            }
        }

        private void OnPreviewProgressChanged(string progress)
        {
            PreviewProgressChanged?.Invoke(progress);
        }

        public async Task PreviewAsync()
        {
            await taskPreview.Run(Preview);
        }

        public async Task RenameAsync()
        {
            await taskRename.Run(Rename);
        }

        private void OnRenameProgressChanged(float progress)
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

        private void Preview()
        {
            if (Directory.Exists(rootDir))
            {
                string[] files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
                foreach (string fp in files)
                {
                    if (DeleteUnWantedFile(fp))
                    {
                        Helpers.WaitWhile(() => DeleteFile(fp), 250, 5000);
                    }
                    else
                    {
                        taskPreview.Report(GetDestPath(fp));
                        taskPreview.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        private void Rename()
        {
            MovedFilesCount = 0;

            if (Directory.Exists(rootDir))
            {
                string[] zipFiles = Directory.GetFiles(rootDir, "*.zip", SearchOption.AllDirectories);
                foreach (string fpZipFile in zipFiles)
                {
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

                ZipCadFolders(rootDir);

                var files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);

                FilesCount = files.Count();

                foreach (string fp in files)
                {
                    if (MoveFile(fp))
                    {
                        MovedFilesCount++;
                        taskRename.Report(MovedFilesCount);
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

        private void ZipCadFolders(string dwgFolder)
        {
            string[] dwgFiles = Directory.GetFiles(dwgFolder, "*.dwg", SearchOption.TopDirectoryOnly);
            if (dwgFiles.Length > 0)
            {
                string zipFileName = Path.GetFileName(dwgFolder);
                if (!Path.GetFileNameWithoutExtension(dwgFolder).Contains("CAD"))
                {
                    zipFileName += " CAD";
                }
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