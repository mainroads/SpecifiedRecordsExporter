using System;
using System.ComponentModel;

namespace SpecifiedRecordsExporter
{
	public abstract class ViewModelBase
	{
        public event PropertyChangedEventHandler PropertyChanged;

        public void SetPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

