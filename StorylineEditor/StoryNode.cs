using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace StorylineEditor
{
    public class StoryNode : BindableBase
    {
        public string Id { get; set => Set(ref field, value); } = Guid.NewGuid().ToString()[..4];
        public string Title { get; set => Set(ref field, value); } = "New Scene";
        public string StoryText { get; set => Set(ref field, value); } = string.Empty;
        public ObservableCollection<Choice> Choices { get; set; } = [];

        // Predefined start and end nodes
        public static StoryNode DefaultStart => new() { Id = "START", Title = "Start", StoryText = "This is the beginning of your story.", Choices = [new Choice() { Text = "Go to the end.", TargetNodeId = "END" }] };
        public static StoryNode DefaultEnd => new() { Id = "END", Title = "The End", StoryText = "Thank you for playing!" };
    }
}
