using System.Collections.Generic;
using System.Threading.Tasks;
using MultiplayerHost.Abstract;
using MultiplayerHost.Domain;

namespace MultiplayerHost.ReferenceGame
{
    /// <summary>
    /// IRepository implementation that uses a dictionary to fake a data store.
    /// </summary>
    public class DummyRepository : IRepository
    {
        private readonly Dictionary<int, Player> playerList = [];

        public Task DeleteUserAsync(User user)
        {
            playerList.Remove(user.Id);
            return Task.CompletedTask;
        }

        public Task<User?> GetUserAsync(int userId)
        {
            return Task.FromResult<User?>(playerList[userId]);
        }

        public Task<IEnumerable<User>> GetUsers()
        {
            var list = playerList.Values;
            return Task.FromResult<IEnumerable<User>>(list);
        }

        public Task SaveUserAsync(User user)
        {
            return Task.CompletedTask;
        }
    }
}
