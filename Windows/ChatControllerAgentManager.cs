using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevGPT.ChatShared;

namespace DevGPT
{
    public class ChatControllerAgentManager : IChatController
    {
        private readonly AgentManager _agentManager;
        private readonly ObservableCollection<string> _items = new();
        private readonly ReadOnlyObservableCollection<string> _itemsReadOnly;
        private Action<string, string, string> _onInterim;

        public ChatControllerAgentManager(AgentManager agentManager)
        {
            _agentManager = agentManager ?? throw new ArgumentNullException(nameof(agentManager));
            // Build list
            foreach (var agent in _agentManager.Agents)
            {
                _items.Add("AGENT: " + agent.Name);
            }
            foreach (var flow in _agentManager.Flows)
            {
                _items.Add("FLOW: " + flow.Name);
            }
            _itemsReadOnly = new ReadOnlyObservableCollection<string>(_items);
        }

        public ReadOnlyObservableCollection<string> AgentsAndFlows => _itemsReadOnly;

        public string DefaultAgentOrFlow => _items.FirstOrDefault() ?? string.Empty;

        public void AttachStreaming(Action<string, string, string> onInterimMessage)
        {
            _onInterim = onInterimMessage;
            // Wire interim outputs from tools
            foreach (var a in _agentManager.Agents)
            {
                a.Tools.SendMessage = (string id, string agent, string output) =>
                {
                    _onInterim?.Invoke(id, agent, output);
                };
            }
        }

        public async Task<string> SendMessageAsync(string text, CancellationToken token, string agentOrFlow)
        {
            if (string.IsNullOrEmpty(agentOrFlow))
            {
                return await _agentManager.SendMessage(text, token);
            }
            var lower = agentOrFlow.ToLowerInvariant();
            if (lower.StartsWith("agent"))
            {
                var agent = agentOrFlow.Length > 7 ? agentOrFlow.Substring(7) : string.Empty;
                return await _agentManager.SendMessage(text, token, agent);
            }
            if (lower.StartsWith("flow"))
            {
                var flow = agentOrFlow.Length > 6 ? agentOrFlow.Substring(6) : string.Empty;
                return await _agentManager.SendMessage_Flow(text, token, flow);
            }
            return await _agentManager.SendMessage(text, token);
        }
    }
}

