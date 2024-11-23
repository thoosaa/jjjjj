using Client.Pages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Share.Models;

namespace Client.Services
{
    public class GameService : IGameService
    {
        private readonly HubConnection _connection;
        private readonly IJSRuntime _jsRuntime;

        public GameService(IJSRuntime jsRuntime)
        {
            _connection = new HubConnectionBuilder()
                        .WithUrl("https://localhost:5001/gamehub")
                        .Build();

            _jsRuntime = jsRuntime;
        }

        public async Task ConnectToHub()
        {
            try
            {
                await _connection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public IDisposable CreateConnection(string method, Action handler)
        {
            return _connection.On(method, handler);
        }

        public IDisposable CreateConnection(string method, Action<string> handler)
        {
            return _connection.On(method, handler);
        }

        public IDisposable CreateConnection(string method, Action<int, int, bool> handler)
        {
            return _connection.On(method, handler);
        }

        public void RemoveConnections(string method)
        {
            _connection.Remove(method);
        }

        public async Task<bool> CreateGame(string game, string username)
        {
            try
            {
                await _connection.InvokeAsync("CreateGame", game, username);
                return true;
            }
            catch (HubException ex)
            {
                await _jsRuntime.InvokeVoidAsync("alert", ex.Message);
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> JoinGame(string game, string username)
        {
            try
            {
                await _connection.InvokeAsync("JoinGame", game, username);
                return true;
            }
            catch (HubException ex)
            {
                await _jsRuntime.InvokeVoidAsync("alert", ex.Message);
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<string> RollDice(string game)
        {
            try
            {
                var result = await _connection.InvokeAsync<string>("RollDice", game);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        public async Task<string> BankPoints(string game)
        {
            try
            {
                var result = await _connection.InvokeAsync<string>("BankPoints", game);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        public async Task EndGame(string game, string username)
        {
            try
            {
                await _connection.InvokeAsync("EndGame", game, username);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        public async Task DeleteGame(string game)
        {
            try
            {
                await _connection.InvokeAsync("DeleteGame", game);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        public async Task<PigGame> GetGameState(string game)
        {
            try
            {
                var result = await _connection.InvokeAsync<string>("GetGameState", game);

                return JsonConvert.DeserializeObject<PigGame>(result) ?? new PigGame(new List<string>());
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching game state: {ex.Message}");
                return new PigGame(new List<string>());
            }
        }
    }
}