using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace StorylineEditor
{
    internal class RelayCommand(Action<object?> execute) : ICommand
    {
        private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
