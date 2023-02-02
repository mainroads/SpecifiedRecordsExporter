using System;
namespace SpecifiedRecordsExporter
{
    public interface ICommand
    {
        public void Execute(Object parameter);
        public bool CanExecute(Object parameter);
        public event EventHandler CanExecuteChanged;
    }
}

