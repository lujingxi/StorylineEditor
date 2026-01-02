using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;

namespace StorylineEditor
{
    public class StoryNodeManager
    {
        // --- Singleton Implementation ---
        public static StoryNodeManager Instance => field ??= new StoryNodeManager();

        private StoryNodeManager()
        {
            // Private constructor to prevent instantiation
        }

        // --- Properties ---
        public ObservableCollection<StoryNode> Nodes { get; } = [StoryNode.DefaultStart, StoryNode.DefaultEnd];

        // --- Methods ---
        public void Read(string filePath)
        {
            var data = JsonSerializer.Deserialize<ObservableCollection<StoryNode>>(File.ReadAllText(filePath));
            if (data is null) return;
            Nodes.Clear();
            foreach (var n in data) Nodes.Add(n);
        }

        public void Save(string filePath) => File.WriteAllText(filePath, JsonSerializer.Serialize(Nodes, new JsonSerializerOptions { WriteIndented = true }));

        public StoryNode? GetNodeById(string id) => Nodes.FirstOrDefault(node => node.Id == id);
        public void AddNode(StoryNode node) => Nodes.Add(node);
        public void RemoveNode(StoryNode node) => Nodes.Remove(node);
    }
}
