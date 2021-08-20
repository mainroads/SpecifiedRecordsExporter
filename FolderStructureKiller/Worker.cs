using System;
using System.IO;
using System.Threading.Tasks;

namespace SpecifiedRecordsExporter
{
    public class Worker
    {
        public delegate void ProgressChangedEventHandler(float progress);
        public event ProgressChangedEventHandler FileMoveProgressChanged;

        public float ProgressTotal
        {
            get
            {
                return files.Length;
            }
        }

        public string Error { get; private set; }

        private TaskEx<float> task;
        private string rootDir;
        private string freeText;
        private string[] files;
        private float currentProgress;

        public Worker(string rootDir, string freeText)
        {
            task = new TaskEx<float>();
            task.ProgressChanged += OnProgressChanged;

            if (Directory.Exists(rootDir))
            {
                if (!rootDir.EndsWith(@"\"))
                    rootDir = rootDir + @"\";

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
            if (Directory.Exists(rootDir))
            {
                files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);

                foreach (string fp in files)
                {
                    MoveFile(fp);

                    task.ThrowIfCancellationRequested();
                }
            }
        }

        private void MoveFile(string origPath)
        {
            string path2 = origPath.Split(rootDir)[1];
            string fn = freeText + " - " + path2.Replace(@"\", " - ");
            string destPath = Path.Combine(rootDir, fn);

            try
            {
                File.Move(origPath, destPath);

                task.Report(++currentProgress);
            }
            catch (Exception ex)
            {
                Error = $"{destPath} ({Path.GetFileName(destPath).Length} characters): {ex.Message}";

                task.Report(currentProgress);
            }
        }
    }
}
