using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Share.Models; // Ensure this namespace is included to access PigGame class

namespace Server.Hubs
{
    public class GameHub : Hub
    {
        private static readonly Dictionary<string, PigGame> GameGroups = new();

        public async Task CreateGame(string game, string username)
        {
            if (GameGroups.ContainsKey(game))
            {
                throw new HubException("This game already exists!");
            }

            GameGroups.Add(game, new PigGame(new List<string> { username }));

            await JoinGame(game, username);
        }

        public async Task JoinGame(string game, string username)
        {
            if (!GameGroups.ContainsKey(game))
            {
                throw new HubException("This game doesn't exist!");
            }

            if (GameGroups[game].GetPlayers().Count >= 5)
            {
                throw new HubException("This game is full!");
            }

            GameGroups[game].GetPlayers().Add(new Player(username));

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, game);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            await Clients.Group(game).SendAsync("Receive", $"{username} has joined the game.");

            if (GameGroups[game].GetPlayers().Count == 2)
            {
                await Clients.Group(game).SendAsync("Notify", "The game is ready to start!");
            }
        }

        public async Task RollDice(string game)
        {
            if (!GameGroups.ContainsKey(game))
            {
                throw new HubException("This game doesn't exist!");
            }

            var result = GameGroups[game].PlayTurn();
            await Clients.Group(game).SendAsync("ReceiveRollResult", result.Message);

            if (GameGroups[game].IsGameOver)
            {
                await Clients.Group(game).SendAsync("Finish", GameGroups[game].GetCurrentPlayer().Name);
            }
        }

        public async Task BankPoints(string game)
        {
            if (!GameGroups.ContainsKey(game))
            {
                throw new HubException("This game doesn't exist!");
            }

            var message = GameGroups[game].BankPoints();
            await Clients.Group(game).SendAsync("ReceiveBankResult", message);

            if (GameGroups[game].IsGameOver)
            {
                await Clients.Group(game).SendAsync("Finish", GameGroups[game].GetCurrentPlayer().Name);
            }
        }

        public async Task EndGame(string game, string username)
        {
            if (GameGroups.ContainsKey(game))
            {
                await Clients.Group(game).SendAsync("Finish", username);
                GameGroups.Remove(game);
            }
        }

        public async Task DeleteGame(string game)
        {
            if (GameGroups.ContainsKey(game))
            {
                await Clients.Group(game).SendAsync("End");
                GameGroups.Remove(game);
            }
        }
    }
}