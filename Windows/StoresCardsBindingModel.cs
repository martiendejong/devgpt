using System.Collections.ObjectModel;
using System.ComponentModel;
namespace DevGPT {
    public class StoresCardsBindingModel : INotifyPropertyChanged
    {
        ObservableCollection<StoreCardModel> cards;
        public ObservableCollection<StoreCardModel> Cards { get => cards; set { cards = value; NotifyPropertyChanged(nameof(Cards)); } }
        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        public void AddNewStoreCard() => Cards.Add(new StoreCardModel());
    }
}
