using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;
using Share.Models;

namespace Client.Pages
{
    public partial class PlayGame : IDisposable
    {
        [SupplyParameterFromQuery]
        [Parameter]
        public string GameId { get; set; } = "";
        [SupplyParameterFromQuery]
        [Parameter]
        public string Username { get; set; } = "";

        private PigGame game;
        private string message = ""; // This is the only definition of message

        private IDisposable? _rollResult;
        private IDisposable? _bankResult;
        private IDisposable? _finish;

        protected async override Task OnInitializedAsync()
        {
            // Connect to the game hub
            await GameService.ConnectToHub();

            // Subscribe to events for receiving results
            _rollResult = GameService.CreateConnection("ReceiveRollResult", (result) =>
            {
                message = result;
                StateHasChanged();
            });

            _bankResult = GameService.CreateConnection("ReceiveBankResult", (result) =>
            {
                message = result;
                StateHasChanged();
            });

            _finish = GameService.CreateConnection("Finish", (winner) =>
            {
                NavigationManager.NavigateTo($"/results?GameId={GameId}&Username={winner}");
            });

            // Fetch the initial game state
            game = await GameService.GetGameState(GameId);
        }

        private async Task RollDice()
        {
            var result = await GameService.RollDice(GameId);
            message = result;

            if (game.IsGameOver)
            {
                await GameService.EndGame(GameId, game.GetCurrentPlayer().Name);
                NavigationManager.NavigateTo($"/results?GameId={GameId}&Username={game.GetCurrentPlayer().Name}");
            }

            StateHasChanged(); // Refresh the UI
        }

        private async Task BankPoints()
        {
            var result = await GameService.BankPoints(GameId);
            message = result;

            if (game.IsGameOver)
            {
                await GameService.EndGame(GameId, game.GetCurrentPlayer().Name);
                NavigationManager.NavigateTo($"/results?GameId={GameId}&Username={game.GetCurrentPlayer().Name}");
            }

            StateHasChanged(); // Refresh the UI
        }

        public void Dispose()
        {
            _rollResult?.Dispose();
            _bankResult?.Dispose();
            _finish?.Dispose();
        }
    }
}