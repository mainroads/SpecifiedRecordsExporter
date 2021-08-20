using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SpecifiedRecordsExporter
{
    public class Worker
    {
        string _rootDir;
        string _freeText;

        string[] _files;
        float currentProgress;

        public delegate void ProgressChanged(float progress);
        public event ProgressChanged FileMoveProgressChanged;
        private CancellationTokenSource cts;

        public Worker(string rootDir, string freeText)
        {
            if (Directory.Exists(rootDir))
            {
                if (!rootDir.EndsWith(@"\"))
                    rootDir = rootDir + @"\";

                _rootDir = rootDir;
                _freeText = freeText;

                _files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
            }
        }

        public float ProgressTotal
        {
            get { return _files.Length; }
        }

        public float ProgressCurrent
        {
            get { return currentProgress; }
        }

        public async Task RunAsync()
        {
            await Task.Run(() => Run());
        }

        public void Run()
        {
            if (Directory.Exists(_rootDir))
            {
                Parallel.ForEach(_files, fp => MoveFile(fp));
            }
        }

        public void Stop()
        {
            if (cts != null)
            {
                cts.Cancel();
            }
        }

        private async void MoveFile(string origPath)
        {
            string path2 = origPath.Split(_rootDir)[1];
            string fn = _freeText + " - " + path2.Replace(@"\", " - ");
            string destPath = Path.Combine(_rootDir, fn);

            Progress<float> progress = new Progress<float>(OnProgressChanged);
            cts = new CancellationTokenSource();
            await Task.Run(() =>
            {
                MoveFileThread(origPath, destPath, progress, cts.Token);
            });

        }

        private void MoveFileThread(string origPath, string destPath, IProgress<float> progress, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
            }

            File.Move(origPath, destPath);
            progress.Report(currentProgress++);
        }

        private void OnProgressChanged(float progress)
        {
            FileMoveProgressChanged?.Invoke(progress);
        }
    }
}
