using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace StorylineEditor
{
    public class Choice : BindableBase
    {
        public string Text { get; set => Set(ref field, value); } = "New Choice";
        public string TargetNodeId { get; set => Set(ref field, value); } = string.Empty;
        [field: JsonIgnore][JsonIgnore] public bool IsTargetHovered { get; set => Set(ref field, value); } = false;
    }
}
