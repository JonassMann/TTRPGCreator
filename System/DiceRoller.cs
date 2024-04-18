using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TTRPGCreator.Functionality
{
    public class DiceRoller
    {
        public static int Roll(string diceString, int adv = 0)
        {
            string[] diceParts = Regex.Split(diceString, @"d|\+");
            int diceCount = int.Parse(diceParts[0]);
            int diceSize = int.Parse(diceParts[1]);
            int diceMod = diceParts.Length > 2 ? int.Parse(diceParts[2]) : 0;

            int total = 0;
            Random random = new Random();

            for (int i = -1; i < Math.Abs(adv); i++)
            {
                int tempTotal = 0;
                for (int j = 0; j < diceCount; j++)
                {
                    tempTotal += random.Next(1, diceSize + 1);
                }

                if (total == 0 || (tempTotal > total && adv > 0) || (tempTotal < total && adv < 0))
                    total = tempTotal;
            }

            return total + diceMod;
        }
    }
}
