using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DevGPT;
public partial class ChatWindow : Window, INotifyPropertyChanged
    {
        private AgentManager _agentManager;
        private bool _isSending = false;
        private ObservableCollection<ChatDisplayMessage> _messages = new ObservableCollection<ChatDisplayMessage>();

        private CancellationTokenSource _cts;
        private bool _isAgentRunning = false;
        public bool IsAgentRunning
        {
            get => _isAgentRunning;
            set
            {
                if (_isAgentRunning != value)
                {
                    _isAgentRunning = value;
                    OnPropertyChanged(nameof(IsAgentRunning));
                }
            }
        }

        public bool IsSending
        {
            get => _isSending;
            set
            {
                if (_isSending != value)
                {
                    _isSending = value;
                    OnPropertyChanged(nameof(IsSending));
                }
            }
        }

        public ObservableCollection<ChatDisplayMessage> Messages => _messages;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string currentMessageId = "";

        public ChatWindow(AgentManager agentManager)
        {
            _agentManager = agentManager;
            InitializeComponent();
            DataContext = this;
            ChatMessagesList.ItemsSource = _messages;
            // Explicitly set alignment for MessageEditor and SendButton to ensure proper placement
            MessageEditor.VerticalAlignment = VerticalAlignment.Center;
            SendButton.VerticalAlignment = VerticalAlignment.Center;
            ChatMessagesList.VerticalAlignment = VerticalAlignment.Stretch;
            // End alignment settings.
            MessageEditor.Focus();

            // VEILIGHEID: verwijdert oude SendMessage-link
            foreach (var a in _agentManager.Agents)
            {
                a.Tools.SendMessage = (string id, string agent, string output) =>
                {
                    // This logic is for background interim messages:
                    Dispatcher.Invoke(() =>
                    {
                        if(currentMessageId == id)
                        {
                            _messages.Last().Response = output;
                        }
                        else
                        {
                            if(Messages.Any(m => m.Id == id))
                            {
                                _messages.Add(new ChatDisplayMessage
                                {
                                    Id = id,
                                    Author = agent,
                                    Response = output,
                                    IsAsyncOnMessage = true // Interims always as expander
                                });
                            }
                            _messages.Add(new ChatDisplayMessage
                            {
                                Id = id,
                                Author = agent,
                                Text = output,
                                IsAsyncOnMessage = true // Interims always as expander
                            });
                            currentMessageId = id;
                        }
                        ScrollToEnd();
                    });
                };
            }
        }

        private void SetSendingState(bool isSending)
        {
            IsSending = isSending;
            SendButton.IsEnabled = !isSending;
            SendingProgress.Visibility = isSending ? Visibility.Visible : Visibility.Collapsed;
            MessageEditor.IsEnabled = !isSending; // (optional: block typing too)
            if (!isSending) { IsAgentRunning = false; }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            SetSendingState(false);
            MessageEditor.Focus();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsSending) return; // extra double-click guard
            SendMessage();
        }

        private async void SendMessage()
        {
            if (IsSending) return;
            var text = MessageEditor.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                SetSendingState(true);
                IsAgentRunning = true;
                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                _messages.Add(new ChatDisplayMessage { Author = "Gebruiker", Text = text, IsAsyncOnMessage = false });
                MessageEditor.Text = string.Empty;
                ScrollToEnd();
                try
                {
                    // Eindreply ophalen; interim berichten (onmessage) worden via SendMessage-delegate hierboven uitgezonden
                    var response = await Task.Run(async () =>
                    {
                        // FIX: replace passing 'token' (CancellationToken) as argument 2 with 'text' (string) which is the correct argument
                        return await _agentManager.SendMessage(text);
                    }, token);
                    
                    // Voeg het eindreply toe als gewone tekst (geen expander)
                    _messages.Add(new ChatDisplayMessage { Author = "Assistent", Text = response, IsAsyncOnMessage = false });
                    ScrollToEnd();
                }
                catch (OperationCanceledException)
                {
                    _messages.Add(new ChatDisplayMessage { Author = "Systeem", Text = "⏹️ Versturen gestopt.", IsAsyncOnMessage = false });
                    ScrollToEnd();
                }
                catch (Exception ex)
                {
                    _messages.Add(new ChatDisplayMessage { Author = "Systeem", Text = $"❌ Fout: {ex.Message}", IsAsyncOnMessage = false });
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
            if (_messages.Count == 0)
                return;
            ChatMessagesList.UpdateLayout(); // Ensure latest list state
            if (ChatMessagesList.ItemContainerGenerator.ContainerFromIndex(_messages.Count - 1) is FrameworkElement lastItem)
            {
                lastItem.BringIntoView();
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                if (MessageEditor.IsFocused && !IsSending)
                {
                    e.Handled = true;
                    SendMessage();
                }
            }
            base.OnPreviewKeyDown(e);
        }
    }

