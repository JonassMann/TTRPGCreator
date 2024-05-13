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
        public static int? Roll(string diceString)
        {
            try
            {
                Console.WriteLine($"Rolling: {diceString}");

                string[] diceParts = diceString.Split(new char[] { 'd', '+', '!' }, StringSplitOptions.RemoveEmptyEntries);
                int diceCount = int.Parse(diceParts[0]);
                int diceSize = int.Parse(diceParts[1]);
                int diceMod = 0;
                int diceAdv = 0;

                if (diceParts.Length == 4)
                {
                    diceMod = int.Parse(diceParts[2]);
                    diceAdv = int.Parse(diceParts[3]);
                }
                else if (diceString.Contains('+'))
                {
                    diceMod = int.Parse(diceParts[2]);
                }
                else if (diceString.Contains('!'))
                {
                    diceAdv = int.Parse(diceParts[2]);
                }

                int total = 0;
                string rollString = "";

                Random random = new Random();

                for (int i = -1; i < Math.Abs(diceAdv); i++)
                {
                    int tempTotal = 0;
                    string tempRollString = "";
                    for (int j = 0; j < diceCount; j++)
                    {
                        int roll = random.Next(1, diceSize + 1);
                        tempRollString += $"{roll} + ";
                        tempTotal += roll;
                    }

                    if (total == 0 || (tempTotal > total && diceAdv > 0) || (tempTotal < total && diceAdv < 0))
                    {
                        total = tempTotal;
                        rollString = tempRollString;
                    }
                }

                rollString += $"({diceMod})";
                Console.WriteLine($"Rolled: {rollString} = {total + diceMod}");

                return total + diceMod;
            }
            catch (Exception)
            {
                Console.WriteLine($"Bad roll syntax");
                return null;
            }
        }
    }
}
