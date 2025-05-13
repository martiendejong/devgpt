using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;
using System.Globalization;
using DevGPT;

namespace DevGPT
{
    public class ChatDisplayMessage : INotifyPropertyChanged
    {
        public string Author { get; set; } // e.g. "Gebruiker" or "Assistent"
        public string Text { get; set; }
        public bool IsAsyncOnMessage { get; set; }
        public event PropertyChangedEventHandler PropertyChanged; // Needed for future binding extensibility
    }

    // Converter for inverse boolean to visibility
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            return (!b) ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    public partial class ChatWindow : Window
    {
        private AgentManager _agentManager;
        private bool _isSending = false;
        private ObservableCollection<ChatDisplayMessage> _messages = new ObservableCollection<ChatDisplayMessage>();

        public ChatWindow(AgentManager agentManager)
        {
            _agentManager = agentManager;
            InitializeComponent();
            ChatMessagesList.ItemsSource = _messages;
            MessageEditor.Focus();

            // VEILIGHEID: verwijdert oude SendMessage-link
            foreach (var a in _agentManager.Agents)
            {
                a.Tools.SendMessage = (string output) =>
                {
                    // This logic is for background interim messages:
                    Dispatcher.Invoke(() =>
                    {
                        _messages.Add(new ChatDisplayMessage
                        {
                            Author = "Assistent",
                            Text = output,
                            IsAsyncOnMessage = true // Interims always as expander
                        });
                        ScrollToEnd();
                    });
                };
            }
        }

        private void SetSendingState(bool isSending)
        {
            _isSending = isSending;
            SendButton.IsEnabled = !isSending;
            SendingProgress.Visibility = isSending ? Visibility.Visible : Visibility.Collapsed;
            MessageEditor.IsEnabled = !isSending; // (optional: block typing too)
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSending) return; // extra double-click guard
            SendMessage();
        }

        private async void SendMessage()
        {
            if (_isSending) return;
            var text = MessageEditor.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                SetSendingState(true);
                _messages.Add(new ChatDisplayMessage { Author = "Gebruiker", Text = text, IsAsyncOnMessage = false });
                MessageEditor.Text = string.Empty;
                ScrollToEnd();
                try
                {
                    // Eindreply ophalen; interim berichten (onmessage) worden via SendMessage-delegate hierboven uitgezonden
                    var response = await _agentManager.SendMessage(text);

                    // Voeg het eindreply toe als gewone tekst (geen expander)
                    _messages.Add(new ChatDisplayMessage { Author = "Assistent", Text = response, IsAsyncOnMessage = false });
                    ScrollToEnd();
                }
                finally
                {
                    SetSendingState(false);
                    MessageEditor.Focus();
                }
            }
        }

        private void ScrollToEnd()
        {
            // Forceer scroll naar laatste bericht
            if (ChatMessagesList.ItemContainerGenerator.ContainerFromIndex(_messages.Count - 1) is FrameworkElement lastItem)
            {
                lastItem.BringIntoView();
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                if (MessageEditor.IsFocused && !_isSending)
                {
                    e.Handled = true;
                    SendMessage();
                }
            }
            base.OnPreviewKeyDown(e);
        }
    }
}