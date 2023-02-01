namespace SpecifiedRecordsExporter
{
    public enum ProgressType
    {
        RemoveJunkFiles,
        UnzipNonCadFiles,
        ZipCadFiles,
        PreviewFileNames,
        ReadyToRename
    }

    public struct RenameProgressData
    {
        public float CurrentFileId { get; set; }
    }

    public struct PrepareProgressData
    {
        public ProgressType ProgressType { get; set; }
        public float CurrentFileId;
        public bool IsJunkFile;
        public string CurrentFilePath;
        public string Status;
        public bool HasLongFileNames { get; set; }
    }
}
