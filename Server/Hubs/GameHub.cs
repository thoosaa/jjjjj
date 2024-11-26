using Microsoft.AspNetCore.SignalR;
using Share.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Hubs
{
    public class GameHub : Hub
    {
        private static readonly Dictionary<string, List<string>> GamePlayers = new();
        private static readonly Dictionary<string, PigGame> GameStates = new();
        private static readonly Dictionary<string, (string Game, string Username)> ConnectionMap = new();

        public async Task CreateGame(string game, string username)
        {
            if (GamePlayers.ContainsKey(game))
            {
                throw new HubException("This game already exists!");
            }

            GamePlayers.Add(game, new List<string> { username });
            ConnectionMap[Context.ConnectionId] = (game, username);
            await Groups.AddToGroupAsync(Context.ConnectionId, game);
            await Clients.Group(game).SendAsync("PlayerJoined", username);
        }

        public async Task JoinGame(string game, string username)
        {
            if (!GamePlayers.ContainsKey(game))
            {
                throw new HubException("This game doesn't exist!");
            }

            if (GamePlayers[game].Count >= 5)
            {
                throw new HubException("This game is full! Maximum 5 players allowed.");
            }

            if (!GamePlayers[game].Contains(username, StringComparer.OrdinalIgnoreCase))
            {
                GamePlayers[game].Add(username);
                ConnectionMap[Context.ConnectionId] = (game, username);
                await Groups.AddToGroupAsync(Context.ConnectionId, game);
                // Only notify others about the new player
                await Clients.OthersInGroup(game).SendAsync("PlayerJoined", username);
            }
        }

        public Task<List<string>> GetPlayerNames(string game)
        {
            if (!GamePlayers.ContainsKey(game))
            {
                return Task.FromResult(new List<string>());
            }
            return Task.FromResult(GamePlayers[game]);
        }

        public async Task StartGame(string game)
        {
            if (!GamePlayers.ContainsKey(game))
            {
                throw new HubException("Game not found!");
            }

            var players = GamePlayers[game];
            if (players.Count < 2)
            {
                throw new HubException("Need at least 2 players to start!");
            }

            var gameState = new PigGame(players);
            GameStates[game] = gameState;
            await Clients.Group(game).SendAsync("GameStarted");
        }

        public async Task UpdateGameState(string game, PigGame gameState)
        {
            if (!GameStates.ContainsKey(game))
            {
                throw new HubException("Game not found!");
            }

            GameStates[game] = gameState;
            await Clients.Group(game).SendAsync("GameStateUpdated", gameState);
        }

        public Task<PigGame> GetGameState(string game)
        {
            if (!GameStates.ContainsKey(game))
            {
                return Task.FromResult<PigGame>(null);
            }
            return Task.FromResult(GameStates[game]);
        }

        public async Task EndGame(string game, string winner)
        {
            if (!GameStates.ContainsKey(game))
            {
                throw new HubException("Game not found!");
            }

            await Clients.Group(game).SendAsync("GameEnded", winner);
            GameStates.Remove(game);
            GamePlayers.Remove(game);
        }

        public async Task DeleteGame(string game)
        {
            if (GameStates.ContainsKey(game))
            {
                GameStates.Remove(game);
            }
            if (GamePlayers.ContainsKey(game))
            {
                GamePlayers.Remove(game);
            }
            await Clients.Group(game).SendAsync("GameDeleted");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (ConnectionMap.TryGetValue(Context.ConnectionId, out var connection))
            {
                var (game, username) = connection;
                
                if (GamePlayers.ContainsKey(game))
                {
                    GamePlayers[game].RemoveAll(player => player.Equals(username, StringComparison.OrdinalIgnoreCase));
                    
                    // If this was the last player, clean up the game
                    if (GamePlayers[game].Count == 0)
                    {
                        GamePlayers.Remove(game);
                        if (GameStates.ContainsKey(game))
                        {
                            GameStates.Remove(game);
                        }
                        await Clients.Group(game).SendAsync("GameDeleted");
                    }
                    else
                    {
                        // Notify others that player left
                        await Clients.Group(game).SendAsync("PlayerLeft", username);
                    }
                }
                
                ConnectionMap.Remove(Context.ConnectionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, game);
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}