using System;
using System.Collections.Generic;

namespace Share.Models
{
    public class PigGame
    {
        public List<string> PlayerNames { get; }
        private readonly List<Player> _players;
        private int _currentPlayerIndex;
        private readonly Dice _dice;
        private const int WinningScore = 100;
        public bool IsGameOver { get; private set; }
        private Dictionary<Action<string>, Action>? _eventsDictionary;

        public event Action<string>? OnGameEnd
        {
            add
            {
                if (value is not null)
                {
                    Action onGameEnd = () => value(GetCurrentPlayer().Name);
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

        public PigGame(List<string> playerNames)
        {
            if (playerNames.Count < 2 || playerNames.Count > 5)
            {
                throw new ArgumentException("Игра должна иметь от 2 до 5 игроков.");
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
        }

        public Player GetCurrentPlayer()
        {
            return _players[_currentPlayerIndex];
        }

        public List<Player> GetPlayers()
        {
            return _players;
        }

        public TurnResult PlayTurn()
        {
            if (IsGameOver)
            {
                return new TurnResult
                {
                    Message = "Игра завершена.",
                    CurrentPlayerName = null,
                    Roll = null,
                    TotalScore = null
                };
            }

            var currentPlayer = GetCurrentPlayer();
            int roll = _dice.Roll();

            if (roll == 1)
            {
                currentPlayer.Score.ResetTurnScore();
                NextPlayer();
                return new TurnResult
                {
                    Message = $"{currentPlayer.Name} выбросил 1. Очки за ход теряются. Переход хода.",
                    CurrentPlayerName = currentPlayer.Name,
                    Roll = roll,
                    TotalScore = currentPlayer.Score.TotalScore
                };
            }

            currentPlayer.Score.AddTurnScore(roll);

            if (currentPlayer.Score.TotalScore + currentPlayer.Score.TurnScore >= WinningScore)
            {
                currentPlayer.Score.BankScore();
                IsGameOver = true;
                OnGameOver?.Invoke();
                return new TurnResult
                {
                    Message = $"{currentPlayer.Name} выиграл с {currentPlayer.Score.TotalScore} очками!",
                    CurrentPlayerName = currentPlayer.Name,
                    Roll = roll,
                    TotalScore = currentPlayer.Score.TotalScore
                };
            }

            return new TurnResult
            {
                Message = $"{currentPlayer.Name} выбросил {roll}. Текущий счет за ход: {currentPlayer.Score.TurnScore}.",
                CurrentPlayerName = currentPlayer.Name,
                Roll = roll,
                TotalScore = currentPlayer.Score.TotalScore
            };
        }

        public string BankPoints()
        {
            if (IsGameOver)
            {
                return "Игра завершена.";
            }

            var currentPlayer = GetCurrentPlayer();
            currentPlayer.Score.BankScore();
            if (currentPlayer.Score.TotalScore >= WinningScore)
            {
                IsGameOver = true;
                OnGameOver?.Invoke();
                return $"{currentPlayer.Name} выиграл с {currentPlayer.Score.TotalScore} очками!";
            }

            NextPlayer();
            return $"{currentPlayer.Name} сохранил очки. Общий счет: {currentPlayer.Score.TotalScore}. Переход хода.";
        }

        private void NextPlayer()
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        }

        public override string ToString()
        {
            var playerInfo = string.Join("\n", _players);
            return $"Игроки:\n{playerInfo}\nХод: {GetCurrentPlayer().Name}";
        }
    }

    public class Player
    {
        public string Name { get; }
        public Score Score { get; }

        public Player(string name)
        {
            Name = name;
            Score = new Score();
        }

        public override string ToString()
        {
            return $"{Name} (Общий счет: {Score.TotalScore})";
        }
    }

    public class Score
    {
        public int TotalScore { get; private set; }
        public int TurnScore { get; private set; }

        public void AddTurnScore(int score)
        {
            TurnScore += score;
        }

        public void ResetTurnScore()
        {
            TurnScore = 0;
        }

        public void BankScore()
        {
            TotalScore += TurnScore;
            ResetTurnScore();
        }
    }

    public class Dice
    {
        private readonly Random _random;

        public Dice()
        {
            _random = new Random();
        }

        public int Roll()
        {
            return _random.Next(1, 7); // Returns a number between 1 and 6
        }
    }

    public class TurnResult
    {
        public string Message { get; set; }
        public string? CurrentPlayerName { get; set; }
        public int? Roll { get; set; }
        public int? TotalScore { get; set; }
    }
}