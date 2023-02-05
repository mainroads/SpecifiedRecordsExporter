using System;
namespace SpecifiedRecordsExporter
{
    public class SpecifiedRecord
    {
        public string FilePath { get; set; }

        public override string ToString()
        {
            return FilePath;
        }
    }
}

