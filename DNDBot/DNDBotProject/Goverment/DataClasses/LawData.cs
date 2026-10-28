using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot.Goverment.Data
{
    public class LawData
    {
        public int LawId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }

        public float StabilityEffect { get; set; }
        public float EconomyEffect { get; set; }
        public float LoyaltyEffect { get; set; }

        public int DurationTurns { get; set; }  
        public bool IsEmergency { get; set; }
    }
}
