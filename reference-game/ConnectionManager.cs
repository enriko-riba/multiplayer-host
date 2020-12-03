using System;
using System.Threading.Tasks;
using MultiplayerHost.Abstract;
using MultiplayerHost.Messages;

namespace MultiplayerHost.ReferenceGame
{
    public class ConnectionManager : IConnectionManager
    {
        public event PlayerConnectingEventHandler PlayerConnecting;
        public event PlayerDisconnectedEventHandler PlayerDisconnected;

        public void DisconnectPlayer(int playerId)
        {
            throw new NotImplementedException();
        }

        public Task SendMessage(in ServerMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
