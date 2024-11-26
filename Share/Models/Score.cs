using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Share.Models
{
    public class Score : INotifyPropertyChanged
    {
        private int _totalScore;
        private int _turnScore;

        public int TotalScore
        {
            get => _totalScore;
            private set
            {
                if (_totalScore != value)
                {
                    _totalScore = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TurnScore
        {
            get => _turnScore;
            private set
            {
                if (_turnScore != value)
                {
                    _turnScore = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
}
