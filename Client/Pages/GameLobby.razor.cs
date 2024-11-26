using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Services;
using System;
using System.Linq;

namespace Client.Pages
{
    public partial class GameLobby : IDisposable
    {
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        [Inject]
        private IGameService GameService { get; set; }

        private List<string> players = new();
        private string currentGame = string.Empty;
        private string currentPlayer = string.Empty;
        private IDisposable playerJoinedSubscription;

        protected override async Task OnInitializedAsync()
        {
            var uri = new Uri(NavigationManager.Uri);
            var queryParams = QueryHelpers.ParseQuery(uri.Query);

            // Get player name first
            if (!queryParams.TryGetValue("player", out var playerParam) || string.IsNullOrWhiteSpace(playerParam))
            {
                NavigationManager.NavigateTo("/");
                return;
            }
            currentPlayer = playerParam;

            // Connect to hub before any game operations
            await GameService.ConnectToHub();

            try
            {
                // Check if we have a game parameter
                if (!queryParams.TryGetValue("game", out var gameParam) || string.IsNullOrWhiteSpace(gameParam))
                {
                    NavigationManager.NavigateTo("/");
                    return;
                }

                currentGame = gameParam;

                // Get current players first
                players = await GameService.GetPlayerNames(currentGame) ?? new List<string>();

                // Set up player joined subscription after getting the initial list
                playerJoinedSubscription = GameService.CreateConnection("PlayerJoined", async (string player) =>
                {
                    if (!players.Contains(player, StringComparer.OrdinalIgnoreCase))
                    {
                        players.Add(player);
                        await InvokeAsync(StateHasChanged);
                    }
                });

                // Only join if we're not the creator
                if (!queryParams.TryGetValue("isCreator", out var isCreator) || isCreator != "true")
                {
                    var success = await GameService.JoinGame(currentGame, currentPlayer);
                    if (!success)
                    {
                        throw new Exception("Failed to join game");
                    }
                    // Add ourselves to the list if we're not already there
                    if (!players.Contains(currentPlayer, StringComparer.OrdinalIgnoreCase))
                    {
                        players.Add(currentPlayer);
                    }
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in lobby: {ex.Message}");
                NavigationManager.NavigateTo("/");
            }
        }

        private async Task StartGame()
        {
            if (players.Count >= 2 && players.Count <= 5)
            {
                try
                {
                    var success = await GameService.StartGame(currentGame);
                    if (success)
                    {
                        NavigationManager.NavigateTo($"/play?game={currentGame}&player={currentPlayer}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error starting game: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            playerJoinedSubscription?.Dispose();
        }
    }
}
