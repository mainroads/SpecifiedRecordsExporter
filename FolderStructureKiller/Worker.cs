using System;
using System.IO;
using System.Threading.Tasks;

namespace SpecifiedRecordsExporter
{
    public class Worker
    {
        public delegate void ProgressChangedEventHandler(float progress);
        public event ProgressChangedEventHandler FileMoveProgressChanged;

        public int FilesCount { get; private set; }
        public int MovedFilesCount { get; private set; }
        public string Error { get; private set; }

        private TaskEx<float> task;
        private string rootDir;
        private string freeText;

        public Worker(string rootDir, string freeText)
        {
            task = new TaskEx<float>();
            task.ProgressChanged += OnProgressChanged;

            if (Directory.Exists(rootDir))
            {
                if (!rootDir.EndsWith(@"\"))
                {
                    rootDir += @"\";
                }

                this.rootDir = rootDir;
                this.freeText = freeText;
            }
        }

        public async Task Run()
        {
            await task.Run(Work);
        }

        private void OnProgressChanged(float progress)
        {
            FileMoveProgressChanged?.Invoke(progress);
        }

        public void Stop()
        {
            task.Cancel();
        }

        private void Work()
        {
            MovedFilesCount = 0;

            if (Directory.Exists(rootDir))
            {
                string[] files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
                FilesCount = files.Length;

                foreach (string fp in files)
                {
                    if (MoveFile(fp))
                    {
                        MovedFilesCount++;
                        task.Report(MovedFilesCount);
                    }

                    task.ThrowIfCancellationRequested();
                }
            }
        }

        private bool MoveFile(string origPath)
        {
            string path2 = origPath.Split(rootDir)[1];
            string fn = freeText + " - " + path2.Replace(@"\", " - ");
            string destPath = Path.Combine(rootDir, fn);

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
    }
}