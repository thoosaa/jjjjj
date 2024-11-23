using Share.Models;
using System;
using System.Threading.Tasks;

namespace Client.Services
{
    public interface IGameService
    {
        Task ConnectToHub();
        IDisposable CreateConnection(string method, Action handler);
        IDisposable CreateConnection(string method, Action<string> handler);
        IDisposable CreateConnection(string method, Action<int, int, bool> handler);
        void RemoveConnections(string method);
        Task<bool> CreateGame(string game, string username);
        Task<bool> JoinGame(string game, string username);
        Task<string> RollDice(string game);
        Task<string> BankPoints(string game);
        Task EndGame(string game, string username);
        Task DeleteGame(string game);

        Task<PigGame> GetGameState(string game);
    }
}