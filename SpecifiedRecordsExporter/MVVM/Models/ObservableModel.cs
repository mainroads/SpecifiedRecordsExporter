using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpecifiedRecordsExporter
{
	public abstract class ObservableModel : INotifyPropertyChanged
    {
        public new event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableModel()
		{
		}
	}
}

