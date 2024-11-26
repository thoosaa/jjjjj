using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Share.Models;

namespace Client.Services
{
    public interface IGameService
    {
        Task ConnectToHub();
        IDisposable CreateConnection(string method, Action handler);
        IDisposable CreateConnection(string method, Action<string> handler);
        IDisposable CreateConnection(string method, Action<int, int, bool> handler);
        IDisposable CreateConnection(string method, Action<PigGame> handler);
        void RemoveConnections(string method);
        Task<bool> CreateGame(string game, string username);
        Task<bool> JoinGame(string game, string username);
        Task<List<string>> GetPlayerNames(string game);
        Task<bool> StartGame(string game);
        Task UpdateGameState(string game, PigGame gameState);
        Task<PigGame> GetGameState(string game);
        Task EndGame(string game, string username);
        Task DeleteGame(string game);
    }
}
