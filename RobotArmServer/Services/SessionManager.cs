using System.Collections.Concurrent;

namespace RobotArmServer.Services
{
    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, string> _activeSessions = new();

        public bool TryRegisterClient(string clientType, string sessionId)
        {
            // Check if a client type is already registered
            return !_activeSessions.ContainsKey(clientType) && _activeSessions.TryAdd(clientType, sessionId);
        }

        public bool UnregisterClient(string clientType, string sessionId)
        {
            return _activeSessions.TryRemove(clientType, out var existing)
                   && existing == sessionId;
        }

        public bool IsClientRegistered(string clientType)
        {
            return _activeSessions.ContainsKey(clientType);
        }
    }
}