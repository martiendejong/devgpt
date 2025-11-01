using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using DevGPT.ChatShared;

namespace DevGPT.App.ExplorerIntegration
{
    public class ChatControllerExplorer : IChatController
    {
        private readonly DevGPTAgent _agent;
        private readonly System.Collections.Generic.List<DevGPTChatMessage> _history = new();
        private readonly ReadOnlyObservableCollection<string> _empty = new(new ObservableCollection<string>());

        public ChatControllerExplorer(DevGPTAgent agent)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }

        public ReadOnlyObservableCollection<string> AgentsAndFlows => _empty;

        public string DefaultAgentOrFlow => string.Empty;

        public void AttachStreaming(Action<string, string, string> onInterimMessage)
        {
            if (_agent?.Tools != null)
            {
                _agent.Tools.SendMessage = (string id, string agent, string output) =>
                {
                    onInterimMessage?.Invoke(id, agent, output);
                };
            }
        }

        public async Task<string> SendMessageAsync(string text, CancellationToken token, string agentOrFlow)
        {
            // Maintain chat history so LLM retains context across turns.
            _history.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.User, Text = text });
            var response = await _agent.Generator.UpdateStore(text, token, _history, true, true, _agent.Tools, null);
            _history.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = response });
            return response;
        }
    }
}

