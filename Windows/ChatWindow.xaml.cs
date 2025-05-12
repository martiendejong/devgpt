using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Text.Json;
using DevGPT;

namespace DevGPT
{
    public partial class ChatWindow : Window
    {
        private AgentManager _agentManager;
        private bool _isSending = false;

        public ChatWindow(AgentManager agentManager)
        {
            _agentManager = agentManager;
            InitializeComponent();
            ChatHistoryTextBox.Text = "";
            MessageEditor.Focus();

            foreach (var a in _agentManager.Agents)
            {
                a.Tools.SendMessage = (string output) =>
                {
                    ChatHistoryTextBox.AppendText($"{output}\n");
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
                ChatHistoryTextBox.AppendText($"Gebruiker: {text}\n");
                MessageEditor.Text = string.Empty;

                try
                {
                    var response = await _agentManager.SendMessage(text);
                    ChatHistoryTextBox.AppendText($"{response}\n");
                }
                finally
                {
                    SetSendingState(false);
                    MessageEditor.Focus();
                }
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
