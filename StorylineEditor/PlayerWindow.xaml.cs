using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StorylineEditor
{
    /// <summary>
    /// Interaction logic for PlayerWindow.xaml
    /// </summary>
    public partial class PlayerWindow : Window, INotifyPropertyChanged
    {
        public required StoryNode CurrentNode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SelectChoiceCommand => new RelayCommand(choiceObj =>
        {
            if (choiceObj is not Choice choice) return;
            var targetNode = StoryNodeManager.Instance.GetNodeById(choice.TargetNodeId);
            if (targetNode is null) return;
            CurrentNode = targetNode;
        });

        public PlayerWindow()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
