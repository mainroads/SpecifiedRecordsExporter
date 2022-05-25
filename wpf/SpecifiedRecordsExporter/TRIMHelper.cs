using System;
using TRIM.SDK;

namespace SpecifiedRecordsExporter
{
    internal class TRIMHelper
    {
        private Database objDB;
        public bool Success { get; set; }

        public TRIMHelper(string id, string workGroupServerName)
        {
            try
            {
                TrimApplication.Initialize();
                using (Database objDB = new Database())
                {
                    objDB.Id = id;
                    objDB.WorkgroupServerName = workGroupServerName;
                    objDB.Connect();
                    Success = true;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fp">Path o the file to save in TRIM</param>
        /// <param name="dir">TRIM folder e.g. 21/10269</param>
        public void CreateRecord(string fp, string dir)
        {
            RecordType objRecTypeDir = new RecordType(objDB, dir);
            Record objContainer = new Record(objDB, objRecTypeDir);
            RecordType objRecTypeDoc = new RecordType(objDB, "Document");
            Record objRec = new Record(objDB, objRecTypeDoc);
            InputDocument objDoc = new InputDocument();
            objDoc.SetAsFile(fp);
            objRec.SetDocument(objDoc, false, false, "Created via Specified Records Exporter");
            objRec.SetContainer(objContainer, true);
            objRec.Save();
        }
    }
}
