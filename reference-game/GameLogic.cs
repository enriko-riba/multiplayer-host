using System;
using System.Threading.Tasks;
using MultiplayerHost.Abstract;
using MultiplayerHost.Domain;
using MultiplayerHost.Messages;

namespace MultiplayerHost.ReferenceGame
{
    class GameLogic : ITurnProcessor
    {
        public Task OnTurnComplete()
        {
            return Task.CompletedTask;
        }

        public Task OnTurnStart(ulong tick, int ellapsedMilliseconds)
        {
            return Task.CompletedTask;
        }

        public void ProcessClientMessage(User user, in ClientMessage msg)
        {
            throw new NotImplementedException();
        }

        public Task ProcessUserTurn(User user, int ellapsedMilliseconds)
        {
            throw new NotImplementedException();
        }
    }
}
