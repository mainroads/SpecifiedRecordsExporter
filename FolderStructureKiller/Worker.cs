using System.IO;
using System.Threading.Tasks;

namespace SpecifiedRecordsExporter
{
    public class Worker
    {
        public delegate void ProgressChanged(float progress);
        public event ProgressChanged FileMoveProgressChanged;
        private float progress;
        private float totalFiles;

        public void Run(string rootDir, string freeText)
        {
            if (Directory.Exists(rootDir))
            {
                var files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
                totalFiles = files.Length;
                Parallel.ForEach(files, fp => MoveFile(rootDir, freeText, fp));
            }
        }

        private void MoveFile(string rootDir, string freeText, string origPath)
        {
            if (!rootDir.EndsWith(@"\"))
                rootDir = rootDir + @"\";

            string path2 = origPath.Split(rootDir)[1];
            string fn = freeText + " - " + path2.Replace(@"\", " - ");
            string destPath = Path.Combine(rootDir, fn);

            File.Move(origPath, destPath);
            OnProgressChanged(progress++);
        }

        private void OnProgressChanged(float percentage)
        {
            FileMoveProgressChanged?.Invoke(percentage);
        }
    }
}
