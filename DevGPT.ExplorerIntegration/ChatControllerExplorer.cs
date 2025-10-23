using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using DevGPT.ChatShared;

namespace DevGPT.ExplorerIntegration
{
    public class ChatControllerExplorer : IChatController
    {
        private readonly DevGPTAgent _agent;
        private readonly ReadOnlyObservableCollection<string> _empty = new(new ObservableCollection<string>());

        public ChatControllerExplorer(DevGPTAgent agent)
        {
            _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        }

        public ReadOnlyObservableCollection<string> AgentsAndFlows => _empty;

        public string DefaultAgentOrFlow => string.Empty;

        public void AttachStreaming(Action<string, string, string> onInterimMessage)
        {
            // No streaming wiring for this simple controller
        }

        public async Task<string> SendMessageAsync(string text, CancellationToken token, string agentOrFlow)
        {
            // Use UpdateStore so modifications are applied when needed (mirror existing behavior)
            var response = await _agent.Generator.UpdateStore(text, token, null, true, true, _agent.Tools, null);
            return response;
        }
    }
}

