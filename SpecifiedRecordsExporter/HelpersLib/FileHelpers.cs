using System.Text.RegularExpressions;

namespace ShareX.HelpersLib
{
    public class FileHelpers
    {
        public static readonly string[] ImageFileExtensions = new string[] { "jpg", "jpeg", "png", "gif", "bmp", "ico", "tif", "tiff" };
        public static readonly string[] TextFileExtensions = new string[] { "txt", "log", "nfo", "c", "cpp", "cc", "cxx", "h", "hpp", "hxx", "cs", "vb",
            "html", "htm", "xhtml", "xht", "xml", "css", "js", "php", "bat", "java", "lua", "py", "pl", "cfg", "ini", "dart", "go", "gohtml" };
        public static readonly string[] VideoFileExtensions = new string[] { "mp4", "webm", "mkv", "avi", "vob", "ogv", "ogg", "mov", "qt", "wmv", "m4p",
            "m4v", "mpg", "mp2", "mpeg", "mpe", "mpv", "m2v", "m4v", "flv", "f4v" };

        public static string GetFileNameExtension(string filePath, bool includeDot = false, bool checkSecondExtension = true)
        {
            string extension = "";

            if (!string.IsNullOrEmpty(filePath))
            {
                int pos = filePath.LastIndexOf('.');

                if (pos >= 0)
                {
                    extension = filePath.Substring(pos + 1);

                    if (checkSecondExtension)
                    {
                        filePath = filePath.Remove(pos);
                        string extension2 = GetFileNameExtension(filePath, false, false);

                        if (!string.IsNullOrEmpty(extension2))
                        {
                            foreach (string knownExtension in new string[] { "tar" })
                            {
                                if (extension2.Equals(knownExtension, StringComparison.OrdinalIgnoreCase))
                                {
                                    extension = extension2 + "." + extension;
                                    break;
                                }
                            }
                        }
                    }

                    if (includeDot)
                    {
                        extension = "." + extension;
                    }
                }
            }

            return extension;
        }

        public static bool CheckExtension(string filePath, IEnumerable<string> extensions)
        {
            string ext = GetFileNameExtension(filePath);

            if (!string.IsNullOrEmpty(ext))
            {
                return extensions.Any(x => ext.Equals(x, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        public static string GetCleanFileName(string fileName)
        {
            // Separate the file name from its extension
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            // Remove percentage encodings from the file name
            nameWithoutExtension = Uri.UnescapeDataString(nameWithoutExtension);

            // Replace multiple spaces with a single space
            nameWithoutExtension = Regex.Replace(nameWithoutExtension, @"\s+", " ");

            // Remove or replace special characters in the file name
            nameWithoutExtension = Regex.Replace(nameWithoutExtension, @"[^a-zA-Z0-9\s\-()]", "_");

            // Trim excessive underscores and dashes
            nameWithoutExtension = Regex.Replace(nameWithoutExtension, @"[_-]{2,}", "_");
            nameWithoutExtension = nameWithoutExtension.Trim('_', '-');

            // Return the cleaned file name with its original extension
            return nameWithoutExtension + extension;
        }

        public static bool IsImageFile(string filePath)
        {
            return CheckExtension(filePath, ImageFileExtensions);
        }

        public static bool IsTextFile(string filePath)
        {
            return CheckExtension(filePath, TextFileExtensions);
        }

        public static bool IsVideoFile(string filePath)
        {
            return CheckExtension(filePath, VideoFileExtensions);
        }

        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    fs.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }

        public static long GetFileSize(string filePath)
        {
            try
            {
                return new FileInfo(filePath).Length;
            }
            catch
            {
            }

            return -1;
        }

        public static bool DeleteFile(string filePath, bool sendToRecycleBin = false)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    if (sendToRecycleBin)
                    {
                        // TODO FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                    else
                    {
                        File.Delete(filePath);
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }

            return false;
        }

        public static string GetPathRoot(string path)
        {
            int separator = path.IndexOf(":\\");

            if (separator > 0)
            {
                return path.Substring(0, separator + 2);
            }

            return "";
        }

        public static string SanitizeFileName(string fileName, string replaceWith = "")
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return SanitizeFileName(fileName, replaceWith, invalidChars);
        }

        private static string SanitizeFileName(string fileName, string replaceWith, char[] invalidChars)
        {
            fileName = fileName.Trim();

            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c.ToString(), replaceWith);
            }

            return fileName;
        }

        public static string SanitizePath(string path, string replaceWith = "")
        {
            string root = GetPathRoot(path);

            if (!string.IsNullOrEmpty(root))
            {
                path = path.Substring(root.Length);
            }

            char[] invalidChars = Path.GetInvalidFileNameChars().Except(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).ToArray();
            path = SanitizeFileName(path, replaceWith, invalidChars);

            return root + path;
        }


        public static void CreateDirectory(string directoryPath)
        {
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch (Exception e)
                {
                    DebugHelper.WriteException(e);
                }
            }
        }

        public static void CreateDirectoryFromFilePath(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                string directoryPath = Path.GetDirectoryName(filePath);
                CreateDirectory(directoryPath);
            }
        }

    }
}
