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
            }
        }

        public float ProgressTotal
        {
            get { return _files.Length; }
        }

        public string Error { get; private set; }

        public async Task RunAsync()
        {
            await Task.Run(() => Run());
        }

        private void Run()
        {
            if (Directory.Exists(_rootDir))
            {
                _files = Directory.GetFiles(_rootDir, "*.*", SearchOption.AllDirectories);
                Progress<float> progress = new Progress<float>(OnProgressChanged);

                foreach (string fp in _files)
                {
                    MoveFile(fp, progress);
                }
            }
        }

        private void MoveFile(string origPath, IProgress<float> progress)
        {
            string path2 = origPath.Split(_rootDir)[1];
            string fn = _freeText + " - " + path2.Replace(@"\", " - ");
            string destPath = Path.Combine(_rootDir, fn);

            cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            if (ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
            }

            try
            {
                File.Move(origPath, destPath);
                progress.Report(++currentProgress);
            }
            catch (Exception ex)
            {
                Error = $"{destPath} ({Path.GetFileName(destPath).Length} characters): {ex.Message}";
                progress.Report(currentProgress);
                ct.ThrowIfCancellationRequested();
            }
        }

        private void OnProgressChanged(float progress)
        {
            FileMoveProgressChanged?.Invoke(progress);
        }

        public void Stop()
        {
            if (cts != null)
            {
                cts.Cancel();
            }
        }
    }
}
