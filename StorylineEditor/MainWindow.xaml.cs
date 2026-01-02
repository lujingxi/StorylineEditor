using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace StorylineEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly StoryNodeManager _storyNodeManager = StoryNodeManager.Instance;
        private Point _startPoint;
        private object? _draggedItem;
        private bool _isDraggingReorder;
        private DragAdorner? _overlay;
        private AdornerLayer? _adornerLayer;
        private StoryNode? _selectedNode;
        private string? _filePath;

        public ObservableCollection<StoryNode> Nodes => _storyNodeManager.Nodes;
        public StoryNode? SelectedNode { get => _selectedNode; set { _selectedNode = value; OnPropertyChanged(); } }
        public string? FilePath { get => _filePath; set { _filePath = value; OnPropertyChanged(); } }
        public ICommand SaveCommand => new RelayCommand(_ => Save());
        public ICommand LoadCommand => new RelayCommand(_ => Load());
        public ICommand PlayCommand => new RelayCommand(_ => Play());

        public MainWindow()
        {
            InitializeComponent();
        }

        // --- Scene Management ---
        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            var node = new StoryNode();
            Nodes.Add(node);
            SelectedNode = node;
        }

        private void DeleteNode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not StoryNode node)
                return;
            
            Nodes.Remove(node);
            SelectedNode = null;
        }

        // --- Choice Management ---
        private void AddChoice_Click(object sender, RoutedEventArgs e) => SelectedNode?.Choices.Add(new Choice());

        private void DeleteChoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Choice choice)
                SelectedNode?.Choices.Remove(choice);
        }

        // --- Drag & Drop Reordering (Scenes) ---
        private void SceneList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            // Find the ListBoxItem container
            var container = ItemsControl.ContainerFromElement(SceneListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            _draggedItem = container?.DataContext;
        }

        private void SceneList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedItem is StoryNode node && !_isDraggingReorder)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDraggingReorder = true;

                    // Find the visual container (ListBoxItem)
                    var lbi = SceneListBox.ItemContainerGenerator.ContainerFromItem(node) as FrameworkElement;

                    if (lbi != null)
                    {
                        // Calculate where inside the LBI the user clicked
                        Point relativeMousePos = e.GetPosition(lbi);

                        // Use the Window's content Grid as the AdornedElement
                        // This prevents the ghost from being clipped by the ListBox
                        var root = (Content as UIElement)!;
                        _adornerLayer = AdornerLayer.GetAdornerLayer(root);

                        if (_adornerLayer != null)
                        {
                            _overlay = new DragAdorner(root, lbi, relativeMousePos);
                            _adornerLayer.Add(_overlay);
                        }
                    }

                    DragDrop.AddGiveFeedbackHandler(SceneListBox, OnGiveFeedback);

                    DataObject data = new(typeof(StoryNode), node);
                    DragDrop.DoDragDrop(SceneListBox, data, DragDropEffects.Move);

                    DragDrop.RemoveGiveFeedbackHandler(SceneListBox, OnGiveFeedback);

                    // Cleanup
                    if (_overlay != null)
                    {
                        _adornerLayer?.Remove(_overlay);
                        _overlay = null;
                    }
                    _isDraggingReorder = false;
                    _draggedItem = null;
                }
            }
        }

        private void SceneList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingReorder && _draggedItem is StoryNode node)
            {
                SelectedNode = node;
                SceneListBox.SelectedItem = node;
            }
            _draggedItem = null;
        }

        private void SceneList_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(StoryNode)) is StoryNode sourceNode)
            {
                // Find what we are hovering over
                var targetNode = (ItemsControl.ContainerFromElement(SceneListBox, e.OriginalSource as DependencyObject) as ListBoxItem)?.DataContext as StoryNode;

                if (targetNode != null && sourceNode != targetNode)
                {
                    int oldIndex = Nodes.IndexOf(sourceNode);
                    int newIndex = Nodes.IndexOf(targetNode);

                    if (oldIndex != -1 && newIndex != -1)
                    {
                        Nodes.Move(oldIndex, newIndex);
                    }
                }
            }
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void SceneList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(StoryNode)) is StoryNode droppedNode)
            {
                int oldIndex = Nodes.IndexOf(droppedNode);
                var targetNode = (ItemsControl.ContainerFromElement(SceneListBox, e.OriginalSource as DependencyObject) as ListBoxItem)?.DataContext as StoryNode;

                if (targetNode != null)
                {
                    int newIndex = Nodes.IndexOf(targetNode);
                    if (oldIndex != -1) Nodes.Move(oldIndex, newIndex);
                }
            }
        }

        // --- Drag & Drop Reordering (Choices) ---
        private void ChoiceItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            if (sender is FrameworkElement fe)
            {
                _draggedItem = fe.DataContext; // This is the Choice object
            }
        }

        private void ChoiceItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedItem is Choice choice && !_isDraggingReorder)
            {
                Point pos = e.GetPosition(null);
                if (Math.Abs(pos.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(pos.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDraggingReorder = true;

                    // 1. Find the Card Visual
                    // sender is the handle. We find the first Border going up that represents the card.
                    var handle = sender as DependencyObject;
                    var choiceCard = FindParent<Border>(handle);

                    // 2. Resolve Root and Adorner Layer
                    // We use the root Grid of the window to ensure the ghost isn't clipped
                    var root = Content as FrameworkElement;
                    if (choiceCard != null && root != null)
                    {
                        _adornerLayer = AdornerLayer.GetAdornerLayer(root);
                        if (_adornerLayer != null)
                        {
                            // Calculate offset so ghost stays under the mouse precisely
                            Point relativeMousePos = e.GetPosition(choiceCard);

                            _overlay = new DragAdorner(root, choiceCard, relativeMousePos);
                            _adornerLayer.Add(_overlay);
                        }
                    }

                    // 3. Initiate Drag
                    DataObject data = new DataObject(typeof(Choice), choice);

                    // Hook the feedback handler to the Window to ensure global tracking
                    DragDrop.AddGiveFeedbackHandler(this, OnGiveFeedback);

                    DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);

                    DragDrop.RemoveGiveFeedbackHandler(this, OnGiveFeedback);

                    // Cleanup
                    if (_overlay != null)
                    {
                        _adornerLayer?.Remove(_overlay);
                        _overlay = null;
                    }
                    _isDraggingReorder = false;
                    _draggedItem = null;
                }
            }
        }

        private void ChoiceItem_DragOver(object sender, DragEventArgs e)
        {
            // Real-time reordering of choices
            if (e.Data.GetData(typeof(Choice)) is Choice sourceChoice && SelectedNode != null)
            {
                var targetChoice = (sender as FrameworkElement)?.DataContext as Choice;

                if (targetChoice != null && sourceChoice != targetChoice)
                {
                    int oldIndex = SelectedNode.Choices.IndexOf(sourceChoice);
                    int newIndex = SelectedNode.Choices.IndexOf(targetChoice);

                    if (oldIndex != -1 && newIndex != -1)
                    {
                        SelectedNode.Choices.Move(oldIndex, newIndex);
                    }
                }
            }

            // We also need to check if we are dragging a StoryNode (for linking IDs)
            // If it's a StoryNode, we allow the Link effect
            if (e.Data.GetDataPresent(typeof(StoryNode)))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.Move;
            }

            e.Handled = true;
        }

        private void ChoiceItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Choice)) is Choice droppedChoice && SelectedNode != null)
            {
                int oldIndex = SelectedNode.Choices.IndexOf(droppedChoice);

                if (sender is FrameworkElement { DataContext: Choice targetChoice })
                {
                    int newIndex = SelectedNode.Choices.IndexOf(targetChoice);
                    if (oldIndex != -1) SelectedNode.Choices.Move(oldIndex, newIndex);
                }
            }
            if (e.Data.GetData(typeof(StoryNode)) is StoryNode node)
            {
                // Handle dropping a StoryNode onto a Choice for linking
                if (sender is FrameworkElement fe && fe.DataContext is Choice choice)
                {
                    choice.IsTargetHovered = false;
                    choice.TargetNodeId = node.Id;
                }
            }
        }

        // --- Linking Drag/Drop (The ID link logic) ---
        private void ChoiceTarget_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Check if what we are dragging is a StoryNode (or the Node ID string)
            bool isNode = e.Data.GetDataPresent(typeof(StoryNode));
            bool isString = e.Data.GetDataPresent(DataFormats.StringFormat);

            if (isNode || isString)
            {
                // Highlight the box
                if (sender is FrameworkElement fe && fe.DataContext is Choice choice)
                {
                    choice.IsTargetHovered = true;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            // CRITICAL: Set Handled to true to override the TextBox's default "forbidden" cursor
            e.Handled = true;
        }

        //private void ChoiceTarget_DragEnter(object sender, DragEventArgs e) { if (sender is FrameworkElement fe && fe.DataContext is Choice c) c.IsTargetHovered = true; }
        private void ChoiceTarget_DragLeave(object sender, DragEventArgs e) { if (sender is FrameworkElement fe && fe.DataContext is Choice c) c.IsTargetHovered = false; }
        private void ChoiceTarget_Drop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Choice choice)
            {
                choice.IsTargetHovered = false;

                // Try to get the data as a StoryNode first, then as a string
                if (e.Data.GetData(typeof(StoryNode)) is StoryNode node)
                {
                    choice.TargetNodeId = node.Id;
                }
                else if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    choice.TargetNodeId = (string)e.Data.GetData(DataFormats.StringFormat);
                }
            }
            e.Handled = true;
        }

        // --- Persistence ---
        //private void Save_Click(object sender, RoutedEventArgs e)
        //{
        //    var dialog = new SaveFileDialog { Filter = "JSON|*.json" };
        //    if (dialog.ShowDialog() == false) return;
        //    FilePath = dialog.FileName;
        //    _storyNodeManager.Save(FilePath);
        //}

        //private void Load_Click(object sender, RoutedEventArgs e)
        //{
        //    var dialog = new OpenFileDialog { Filter = "JSON|*.json" };
        //    if (dialog.ShowDialog() == false) return;
        //    _storyNodeManager.Read(dialog.FileName);
        //    FilePath = dialog.FileName;
        //}

        //private void Play_Click(object sender, RoutedEventArgs e)
        //{
        //    new PlayerWindow
        //    {
        //        CurrentNode = SelectedNode ?? (_storyNodeManager.Nodes.Count > 0 ? _storyNodeManager.Nodes[0] : new StoryNode()),
        //    }.ShowDialog();
        //}

        // --- Commands ---
        private void Save()
        {
            var dialog = new SaveFileDialog { Filter = "JSON|*.json" };
            if (dialog.ShowDialog() == false) return;
            FilePath = dialog.FileName;
            _storyNodeManager.Save(FilePath);
        }

        private void Load()
        {
            var dialog = new OpenFileDialog { Filter = "JSON|*.json" };
            if (dialog.ShowDialog() == false) return;
            _storyNodeManager.Read(dialog.FileName);
            FilePath = dialog.FileName;
        }

        private void Play()
        {
            new PlayerWindow
            {
                CurrentNode = SelectedNode ?? (_storyNodeManager.Nodes.Count > 0 ? _storyNodeManager.Nodes[0] : new StoryNode()),
            }.ShowDialog();
        }

        // --- Ghost Follow Logic ---

        private void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (_overlay != null)
            {
                // Get global mouse position from Win32 to be 100% accurate during drag
                Point screenMousePos = GetMousePosition();
                _overlay.UpdatePosition(screenMousePos);
            }

            e.UseDefaultCursors = false;
            e.Handled = true;
        }

        // Use P/Invoke for the most reliable mouse tracking during blocking drag operations
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out Win32Point lpPoint);

        private struct Win32Point { public int X; public int Y; }

        private Point GetMousePosition()
        {
            GetCursorPos(out Win32Point w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        // Generic helper to find parent of specific type in visual tree
        private T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            if (child == null) return null;
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }

        // --- System Title Bar Logic ---

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // To make the icon switch between "Box" and "Double Box" when maximizing
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            // We find the icon in the template (if you didn't name it, just skip this)
            var icon = this.FindName("MaximizeIcon") as TextBlock;
            // Unicode for Maximize is E922, Restore is E923
            icon?.Text = this.WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}