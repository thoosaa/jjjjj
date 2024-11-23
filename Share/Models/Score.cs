using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Models
{
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
}
