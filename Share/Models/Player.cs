using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Models
{
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
}
