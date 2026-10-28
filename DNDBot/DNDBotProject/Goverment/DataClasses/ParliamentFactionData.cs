using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNDBot.Goverment.Data
{
    public class ParliamentFactionData
    {
        public int FactionId { get; set; }
        public string Name { get; set; }

        public int Seats { get; set; }

        public float Loyalty { get; set; }
        public float Corruption { get; set; }

        public float IdeologyAlignment { get; set; }  
        public float Resistance { get; set; }         
    }
}
