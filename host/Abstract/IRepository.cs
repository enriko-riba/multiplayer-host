namespace MultiplayerHost.Abstract;

using System.Collections.Generic;
using System.Threading.Tasks;
using MultiplayerHost.Domain;

/// <summary>
/// Defines user persistence.
/// The game implementation should derive their player entities from the <see cref="User"/> class.
/// </summary>
public interface IRepository
{
    /// <summary>
    /// Returns a collection off all users from the persistence layer.
    /// Note: the implementations can and should return a filtered collection of only users eligible for playing the game e.g. without deactivated, banned or any other inactive user types.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<User>> GetUsers();

    /// <summary>
    /// Returns a user from the persistence layer.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<User?> GetUserAsync(int userId);

    /// <summary>
    /// Saves the user to the persistence layer.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task SaveUserAsync(User user);

    /// <summary>
    /// Permanently deletes the user from the persistence layer.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task DeleteUserAsync(User user);
}
