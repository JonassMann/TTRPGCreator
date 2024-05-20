using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTRPGCreator.System
{
    internal class DataCache
    {
        // <serverID, game name>
        public static Dictionary<ulong, string> gameList = new Dictionary<ulong, string>();

        // <game name, Ruleset>
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
        public List<Item> items = null;
        public List<Status> statuses = null;
    }

    public class  Item
    {
        public long? id;
        public string name;
        public string description;
        public int quantity = 1;
        public bool equipped = false;
        public List<Status> statuses = null;
    }

    public class Status
    {
        public long? id;
        public string name;
        public string description;
        public string type;
        public List<Status> statuses = null;
        public List<Effect> effects = null;
    }

    public class Effect
    {
        public long? id;
        public string effect;
        public List<string> tags = null;
        public int level = 1;
    }

    public enum NullBool
    {
        None,
        True,
        False
    }
}
