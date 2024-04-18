using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTRPGCreator.Database
{
    public class DUser
    {
        public string userName { get; set; }
        public string serverName { get; set; }
        public ulong serverID { get; set; }
    }
}
