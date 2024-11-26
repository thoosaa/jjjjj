using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Share.Models
{
    public class PigGame
    {
        public List<string> PlayerNames { get; private set; }

        private List<Player> _players { get; set; }

        private int _currentPlayerIndex { get; set; }

        private readonly Dice _dice;

        private const int WinningScore = 100;

        public bool IsGameOver { get; private set; }

        [JsonInclude]
        public string LastMessage { get; private set; }

        [JsonInclude]
        public string CurrentPlayerName => GetCurrentPlayer()?.Name;

        private Dictionary<Action<string>, Action>? _eventsDictionary;

        public event Action<string>? OnGameEnd
        {
            add
            {
                if (value is not null)
                {
                    Action onGameEnd = () => value(GetCurrentPlayer()?.Name ?? "Unknown");
                    _eventsDictionary ??= new();
                    _eventsDictionary[value] = onGameEnd;
                    OnGameOver += onGameEnd;
                }
            }
            remove
            {
                if (value is not null && _eventsDictionary is not null)
                {
                    OnGameOver -= _eventsDictionary[value];
                    _eventsDictionary.Remove(value);
                }
            }
        }

        private event Action? OnGameOver;

        [JsonConstructor]
        public PigGame(List<string> playerNames)
        {
            if (playerNames.Count > 5)
            {
                throw new ArgumentException("Game must have between 2 and 5 players.");
            }

            PlayerNames = playerNames;
            _players = new List<Player>();
            foreach (var name in playerNames)
            {
                _players.Add(new Player(name));
            }

            _dice = new Dice();
            _currentPlayerIndex = 0;
            IsGameOver = false;
            LastMessage = "Game started! Roll the dice or bank your points.";
        }

        public Player GetCurrentPlayer()
        {
            if (_players.Count == 0)
            {
                throw new InvalidOperationException("No players in the game.");
            }

            return _players[_currentPlayerIndex];
        }

        public List<Player> GetPlayers()
        {
            return _players;
        }

        public bool IsPlayerTurn(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
                return false;
                
            var currentPlayer = GetCurrentPlayer();
            return currentPlayer != null && currentPlayer.Name == playerName && !IsGameOver;
        }

        public TurnResult PlayTurn()
        {
            Console.WriteLine($"[PigGame] Starting turn for player {CurrentPlayerName}");
            
            if (IsGameOver)
            {
                Console.WriteLine("[PigGame] Game is already over");
                return new TurnResult
                {
                    Message = "Game is over.",
                    CurrentPlayerName = CurrentPlayerName,
                    Roll = null,
                    TotalScore = null,
                    IsGameOver = true
                };
            }

            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer == null)
            {
                Console.WriteLine("[PigGame] Error: No current player found");
                return new TurnResult
                {
                    Message = "Invalid game state: No current player.",
                    IsGameOver = true
                };
            }

            string currentPlayerName = currentPlayer.Name;
            int roll = _dice.Roll();
            Console.WriteLine($"[PigGame] Player {currentPlayerName} rolled: {roll}");

            if (roll == 1)
            {
                Console.WriteLine($"[PigGame] Player {currentPlayerName} rolled 1 - Switching turns");
                currentPlayer.Score.ResetTurnScore();
                var previousPlayerName = currentPlayerName;
                
                NextPlayer();
                var nextPlayer = GetCurrentPlayer();
                
                LastMessage = $"{previousPlayerName} rolled a 1. Turn score is lost. {nextPlayer.Name}'s turn.";
                
                return new TurnResult
                {
                    Message = LastMessage,
                    CurrentPlayerName = nextPlayer.Name,
                    PreviousPlayerName = previousPlayerName,
                    Roll = roll,
                    TotalScore = currentPlayer.Score.TotalScore,
                    IsNextTurn = true,
                    TurnScore = 0
                };
            }

            currentPlayer.Score.AddTurnScore(roll);

            if (currentPlayer.Score.TotalScore + currentPlayer.Score.TurnScore >= WinningScore)
            {
                currentPlayer.Score.BankScore();
                IsGameOver = true;
                OnGameOver?.Invoke();
                LastMessage = $"{currentPlayer.Name} wins with {currentPlayer.Score.TotalScore} points!";
                return new TurnResult
                {
                    Message = LastMessage,
                    CurrentPlayerName = currentPlayer.Name,
                    Roll = roll,
                    TotalScore = currentPlayer.Score.TotalScore,
                    IsGameOver = true
                };
            }

            LastMessage = $"{currentPlayer.Name} rolled {roll}. Current turn score: {currentPlayer.Score.TurnScore}.";
            return new TurnResult
            {
                Message = LastMessage,
                CurrentPlayerName = currentPlayer.Name,
                Roll = roll,
                TotalScore = currentPlayer.Score.TotalScore,
                TurnScore = currentPlayer.Score.TurnScore,
                IsNextTurn = false
            };
        }

        public TurnResult BankPoints()
        {
            var currentPlayer = GetCurrentPlayer();
            string currentPlayerName = currentPlayer.Name;  // Store the name before any changes
            currentPlayer.Score.BankScore();
            var totalScore = currentPlayer.Score.TotalScore;

            if (totalScore >= WinningScore)
            {
                IsGameOver = true;
                OnGameOver?.Invoke();
                LastMessage = $"{currentPlayerName} wins with {totalScore} points!";
                return new TurnResult
                {
                    Message = LastMessage,
                    CurrentPlayerName = currentPlayerName,
                    TotalScore = totalScore,
                    IsGameOver = true
                };
            }

            NextPlayer();
            var nextPlayer = GetCurrentPlayer();
            LastMessage = $"{currentPlayerName} banked {totalScore} points. {nextPlayer.Name}'s turn.";
            
            return new TurnResult
            {
                Message = LastMessage,
                CurrentPlayerName = nextPlayer.Name,
                PreviousPlayerName = currentPlayerName,
                TotalScore = totalScore,
                IsNextTurn = true
            };
        }

        private void NextPlayer()
        {
            var previousPlayer = GetCurrentPlayer()?.Name ?? "Unknown";
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
            var nextPlayer = GetCurrentPlayer()?.Name ?? "Unknown";
            Console.WriteLine($"[PigGame] Turn switched from {previousPlayer} to {nextPlayer}");
        }

        public override string ToString()
        {
            var playerInfo = string.Join("\n", _players);
            return $"Players:\n{playerInfo}\nCurrent Turn: {GetCurrentPlayer().Name}";
        }
    }

    public class Dice
    {
        private readonly Random _random = new Random();

        public int Roll()
        {
            return _random.Next(1, 7);
        }
    }

    public class TurnResult
    {
        public string Message { get; set; }
        public string CurrentPlayerName { get; set; }
        public string PreviousPlayerName { get; set; }
        public int? Roll { get; set; }
        public int? TotalScore { get; set; }
        public int? TurnScore { get; set; }
        public bool IsNextTurn { get; set; }
        public bool IsGameOver { get; set; }
        public bool ForceTurnSwitch { get; set; }
    }

    public enum GameAction
    {
        RollDice,
        BankPoints
    }
}