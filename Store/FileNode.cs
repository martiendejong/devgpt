using System.Collections.ObjectModel;

namespace DevGPT.NewAPI
{
    public class FileNode : DocumentInfo
    {
        public FileNode Parent { get; set; }
        public ObservableCollection<FileNode> Children { get; set; }

        public FileNode(string name, ObservableCollection<FileNode> children = null)
        {
            Name = name;
            Children = children ?? new ObservableCollection<FileNode>();
        }
    }
}