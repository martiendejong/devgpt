using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
namespace DevGPT {
    public partial class StoresCardsWindow : Window
    {
        private StoresCardsBindingModel viewModel;
        public List<StoreConfig> ResultStores = null;

        public StoresCardsWindow(StoresCardsBindingModel model)
        {
            InitializeComponent();
            viewModel = model;
            DataContext = viewModel;
        }

        private void AddNewStoreButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddNewStoreCard();
        }

        private void RemoveStoreCardButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var card = btn?.CommandParameter as StoreCardModel;
            if (card != null) viewModel.Cards.Remove(card);
        }

        private void OpslaanButton_Click(object sender, RoutedEventArgs e)
        {
            ResultStores = viewModel.Cards.Select(card => new StoreConfig {
                Name = card.Name,
                Description = card.Description,
                Path = card.Path,
                SubDirectory = card.SubDirectory,
                ExcludePattern = StringSplitter.Split(card.ExcludePattern),
                FileFilters = StringSplitter.Split(card.FileFilters)
            }).ToList();
            DialogResult = true;
            Close();
        }

        private void AnnuleerButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

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

    public class StoresCardsBindingModel : INotifyPropertyChanged
    {
        ObservableCollection<StoreCardModel> cards;
        public ObservableCollection<StoreCardModel> Cards { get => cards; set { cards = value; NotifyPropertyChanged(nameof(Cards)); } }
        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        public void AddNewStoreCard() => Cards.Add(new StoreCardModel());
    }
}
