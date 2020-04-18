using System;
using System.Windows.Input;

namespace SpectrumMonitor.ViewModel
{
    /// <summary>
	/// Implement commands from the XAML controls
	/// </summary>
	public class RelayCommand : ICommand
    {
        #region Members
        private Func<Boolean> _canExecute;
        private readonly Action _execute;
        #endregion Members

        #region Construct
        public RelayCommand(Action execute)
            : this(execute, null)
        {

        }

        public RelayCommand(Action execute, Func<Boolean> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            _execute = execute;
            _canExecute = canExecute;
        }
        #endregion Construct

        #region ICommand Methods

        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        #endregion ICommand Methods
    }

    /// <summary>
    /// Implement commands from the XAML controls
    /// </summary>
    public class RelayCommandWithParameter : ICommand
    {
        #region Members
        private Func<Boolean> _canExecute;
        private readonly Action<object> _execute;
        #endregion Members

        #region Construct
        public RelayCommandWithParameter(Action<object> execute)
            : this(execute, null)
        {

        }

        public RelayCommandWithParameter(Action<object> execute, Func<Boolean> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            _execute = execute;
            _canExecute = canExecute;
        }
        #endregion Construct

        #region ICommand Methods

        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        #endregion ICommand Methods
    }
}
