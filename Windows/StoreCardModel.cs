using System.ComponentModel;
namespace DevGPT {
    public class StoreCardModel : INotifyPropertyChanged
    {
        string name, description, path, subDirectory, excludePattern, fileFilters;
        public string Name { get => name; set { name = value; NotifyPropertyChanged(nameof(Name)); } }
        public string Description { get => description; set { description = value; NotifyPropertyChanged(nameof(Description)); } }
        public string Path { get => path; set { path = value; NotifyPropertyChanged(nameof(Path)); } }
        public string SubDirectory { get => subDirectory; set { subDirectory = value; NotifyPropertyChanged(nameof(SubDirectory)); } }
        public string ExcludePattern { get => excludePattern; set { excludePattern = value; NotifyPropertyChanged(nameof(ExcludePattern)); } }
        public string FileFilters { get => fileFilters; set { fileFilters = value; NotifyPropertyChanged(nameof(FileFilters)); } }
        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
