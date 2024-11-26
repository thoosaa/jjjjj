using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using Share.Models;
using Client.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Client.Pages
{
    public partial class PlayPig : IDisposable
    {
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        [Inject]
        private IGameService GameService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        private string currentGame;
        private PigGame gameState;
        private Player currentPlayer;
        private List<Player> players;
        private string gameMessage = "Game started! Roll the dice or bank your points.";
        private int? lastRoll;
        private bool isGameOver;
        private bool isRolling;
        private string currentUserName;
        private IDisposable gameStateUpdatedSubscription;
        private IDisposable gameEndedSubscription;

        protected override async Task OnInitializedAsync()
        {
            var uri = new Uri(NavigationManager.Uri);
            var queryParams = QueryHelpers.ParseQuery(uri.Query);
            
            if (queryParams.TryGetValue("game", out var gameParam))
            {
                currentGame = gameParam;
            }

            if (queryParams.TryGetValue("player", out var playerParam))
            {
                currentUserName = playerParam;
            }

            await GameService.ConnectToHub();

            gameStateUpdatedSubscription = GameService.CreateConnection("GameStateUpdated", async (PigGame updatedState) =>
            {
                if (updatedState != null)
                {
                    Console.WriteLine($"[PlayPig] Received game state update from hub - Old: {currentPlayer?.Name}, New: {updatedState.CurrentPlayerName}");
                    
                    // Always apply state updates from server
                    await InvokeAsync(() => 
                    {
                        UpdateGameState(updatedState);
                        StateHasChanged();
                    });
                }
            });

            gameEndedSubscription = GameService.CreateConnection("GameEnded", async (string winner) =>
            {
                gameMessage = $"Game Over! {winner} wins!";
                isGameOver = true;
                await InvokeAsync(StateHasChanged);
                await Task.Delay(2000);
                NavigationManager.NavigateTo($"/results?winner={winner}");
            });

            // Get initial game state
            gameState = await GameService.GetGameState(currentGame);
            if (gameState != null)
            {
                players = gameState.GetPlayers();
                currentPlayer = gameState.GetCurrentPlayer();
                isGameOver = gameState.IsGameOver;
                gameMessage = gameState.LastMessage;
                await InvokeAsync(StateHasChanged);
            }
        }

        private bool CanTakeTurn()
        {
            var canTake = currentPlayer != null && 
                          currentUserName == currentPlayer.Name && 
                          !isGameOver;
                          
            Console.WriteLine($"[PlayPig] CanTakeTurn: {currentUserName} can take turn: {canTake}, Current player: {currentPlayer?.Name}");
            return canTake;
        }

        private async Task RollDice()
        {
            if (!CanTakeTurn())
            {
                Console.WriteLine($"[PlayPig] Cannot take turn - Current player: {currentPlayer?.Name}, User: {currentUserName}");
                await JSRuntime.InvokeVoidAsync("alert", "It's not your turn!");
                return;
            }

            try
            {
                isRolling = true;
                await InvokeAsync(StateHasChanged);

                Console.WriteLine($"[PlayPig] {currentUserName} is rolling the dice");
                var result = gameState.PlayTurn();
                
                // Store the result locally before sending to server
                lastRoll = result.Roll;
                gameMessage = result.Message;
                
                // Send the updated state to server
                await GameService.UpdateGameState(currentGame, gameState);
                Console.WriteLine("[PlayPig] Sent game state update to server");
                
                // Update local game state based on the turn result
                if (result.IsNextTurn)
                {
                    Console.WriteLine($"[PlayPig] Turn is changing to {result.CurrentPlayerName}");
                    currentPlayer = players.FirstOrDefault(p => p.Name == result.CurrentPlayerName);
                }
                
                isGameOver = result.IsGameOver;
                await InvokeAsync(StateHasChanged);
            }
            finally
            {
                isRolling = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task BankPoints()
        {
            if (!CanTakeTurn())
            {
                await JSRuntime.InvokeVoidAsync("alert", "It's not your turn!");
                return;
            }

            try
            {
                var result = gameState.BankPoints();
                
                // Store the result locally before sending to server
                lastRoll = null;
                gameMessage = result.Message;
                
                // Send the updated state to server
                await GameService.UpdateGameState(currentGame, gameState);
                Console.WriteLine("[PlayPig] Sent game state update to server after banking points");
                
                // Update local game state based on the turn result
                if (result.IsNextTurn)
                {
                    Console.WriteLine($"[PlayPig] Turn is changing to {result.CurrentPlayerName} after banking points");
                    currentPlayer = players.FirstOrDefault(p => p.Name == result.CurrentPlayerName);
                }
                
                isGameOver = result.IsGameOver;
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlayPig] Error in BankPoints: {ex.Message}");
                gameMessage = "Error occurred while banking points. Please try again.";
                await InvokeAsync(StateHasChanged);
            }
        }

        private void UpdateGameState(PigGame newState)
        {
            if (newState == null) return;

            var oldPlayer = currentPlayer?.Name;
            var newPlayer = newState.CurrentPlayerName;
            
            Console.WriteLine($"[PlayPig] UpdateGameState - Old: {oldPlayer}, New: {newPlayer}");
            
            // Update the game state from server
            gameState = newState;
            players = gameState.GetPlayers();
            
            // Only update current player if it's different
            if (oldPlayer != newPlayer)
            {
                Console.WriteLine($"[PlayPig] Player changing from {oldPlayer} to {newPlayer}");
                currentPlayer = gameState.GetCurrentPlayer();
                Console.WriteLine($"[PlayPig] Player change completed - Current player is now: {currentPlayer?.Name}");
            }
            
            isGameOver = gameState.IsGameOver;
            gameMessage = gameState.LastMessage;
        }

        private string GetPlayerClass(Player player)
        {
            var classes = "player-card";
            if (player.Name == currentPlayer?.Name)
            {
                classes += " current-player";
            }
            if (player.Name == currentUserName)
            {
                classes += " your-player";
            }
            return classes;
        }

        public void Dispose()
        {
            gameStateUpdatedSubscription?.Dispose();
            gameEndedSubscription?.Dispose();
        }
    }
}
