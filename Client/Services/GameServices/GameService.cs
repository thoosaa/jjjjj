using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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

		public IDisposable CreateConnection(string method, Action<PigGame> handler)
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
			catch
			{
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
			catch
			{
				return false;
			}
		}

		public async Task<List<string>> GetPlayerNames(string game)
		{
			try
			{
				return await _connection.InvokeAsync<List<string>>("GetPlayerNames", game);
			}
			catch
			{
				return new List<string>();
			}
		}

		public async Task<bool> StartGame(string game)
		{
			try
			{
				await _connection.InvokeAsync("StartGame", game);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public async Task UpdateGameState(string game, PigGame gameState)
		{
			try
			{
				await _connection.InvokeAsync("UpdateGameState", game, gameState);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error updating game state: {ex.Message}");
			}
		}

		public async Task<PigGame> GetGameState(string game)
		{
			try
			{
				return await _connection.InvokeAsync<PigGame>("GetGameState", game);
			}
			catch
			{
				return null;
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
				Console.WriteLine($"Error ending game: {ex.Message}");
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
				Console.WriteLine($"Error deleting game: {ex.Message}");
			}
		}
	}
}
