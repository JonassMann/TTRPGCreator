using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTRPGCreator.System
{
    internal class DataCache
    {
        public static Dictionary<ulong, string> gameList = new Dictionary<ulong, string>();

        public static Dictionary<string, Ruleset> gameRules = new Dictionary<string, Ruleset>();
    }

    public class Ruleset
    {
        public string diceRoll { get; set; }
        public string statFormula { get; set; }

        public List<(string, string)> stats = new List<(string, string)>();
    }

    public class Character
    {
        public long? id;
        public string name;
        public string description;
        public long? discord_id;
    }

    public class  Item
    {
        public long? id;
        public string name;
        public string description;
    }

    public class Status
    {
        public long? id;
        public string name;
        public string description;
        public string type;
    }

    public class Effect
    {
        public long? id;
        public string effect;
    }
}
