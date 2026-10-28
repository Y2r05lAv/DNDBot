using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot.Goverment.Data
{
    public class EconomyTurnData
    {
        public int TurnNumber { get; set; }

        public long Income { get; set; }
        public long Expenses { get; set; }
        public long NetResult { get; set; }

        public float CorruptionLoss { get; set; }
    }
}
