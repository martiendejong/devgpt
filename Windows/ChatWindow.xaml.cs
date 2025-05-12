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

        public ChatWindow(AgentManager agentManager)
        {
            _agentManager = agentManager;
            InitializeComponent();
            ChatHistoryTextBox.Text = "";
            MessageEditor.Focus();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }
        private async void SendMessage()
        {
            var text = MessageEditor.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                ChatHistoryTextBox.AppendText($"Gebruiker: {text}\n");
                MessageEditor.Text = string.Empty;

                foreach(var a in _agentManager.Agents)
                {
                    a.Tools.SendMessage = (string output) => 
                    {
                        ChatHistoryTextBox.AppendText($"{output}\n");
                    };
                }

                var response = await _agentManager.SendMessage(text);
                ChatHistoryTextBox.AppendText($"{response}\n");
            }
        }
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                if (MessageEditor.IsFocused)
                {
                    e.Handled = true;
                    SendMessage();
                }
            }
            base.OnPreviewKeyDown(e);
        }
    }
}
