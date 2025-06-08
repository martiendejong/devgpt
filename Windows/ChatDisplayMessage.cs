using System.ComponentModel;

namespace DevGPT;
public class ChatDisplayMessage : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Author { get; set; } // e.g. "Gebruiker" or "Assistent"
        public string Text { get; set; }
        public string Response { get; set; }
        public bool IsAsyncOnMessage { get; set; }
        public event PropertyChangedEventHandler PropertyChanged; // Needed for future binding extensibility
    }
