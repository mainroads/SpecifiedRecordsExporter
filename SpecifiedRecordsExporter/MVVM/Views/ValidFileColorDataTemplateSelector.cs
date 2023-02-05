using System;
using System.Collections.ObjectModel;

namespace SpecifiedRecordsExporter
{
	public class ValidFileColorDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LongFilePathTemplate { get; set; }
        public DataTemplate ValidFilePathTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            SpecifiedRecord sr = item as SpecifiedRecord;
            return sr.FilePath.Length > 100 ? LongFilePathTemplate : ValidFilePathTemplate;
        }
    }
}

